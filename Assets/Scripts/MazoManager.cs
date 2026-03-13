using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazoManager : MonoBehaviour
{
    public List<Carta> todasLasCartas;
    private List<Carta> mazoPrincipal = new List<Carta>();
    private List<GameObject> cartasEnBaza = new List<GameObject>();

    [Header("Configuración Visual")]
    public GameObject cartaPrefab;
    public GameObject prefabCartaRival;

    [Header("Contenedores")]
    public Transform manoJugador;
    public Transform manoRival1;
    public Transform manoCompañero;
    public Transform manoRival2;
    public Transform zonaMazo;
    public Transform zonaBaza;

    [Header("Posiciones de Baza")]
    public Transform posJugador;
    public Transform posRival1;
    public Transform posCompañero;
    public Transform posRival2;

    public Carta cartaTriunfo;

    void Start()
    {
        InicializarMazo();
        Barajar();
        RepartirPartidaInicial();
        DefinirTriunfo();
    }

    void InicializarMazo() { mazoPrincipal = new List<Carta>(todasLasCartas); }

    public void Barajar()
    {
        for (int i = 0; i < mazoPrincipal.Count; i++)
        {
            Carta temp = mazoPrincipal[i];
            int randomIndex = Random.Range(i, mazoPrincipal.Count);
            mazoPrincipal[i] = mazoPrincipal[randomIndex];
            mazoPrincipal[randomIndex] = temp;
        }
    }

    public void DefinirTriunfo()
    {
        if (mazoPrincipal.Count <= 0) return;
        cartaTriunfo = mazoPrincipal[0];
        mazoPrincipal.RemoveAt(0);
        mazoPrincipal.Add(cartaTriunfo);

        GameObject muestraGO = Instantiate(cartaPrefab, zonaMazo);
        muestraGO.transform.localRotation = Quaternion.Euler(0, 0, 90);
        muestraGO.transform.localPosition = new Vector3(-70, 0, 0);
        muestraGO.GetComponent<CartaVisual>().ConfigurarCarta(cartaTriunfo);
        if (muestraGO.GetComponent<DraggableCarta>() != null) muestraGO.GetComponent<DraggableCarta>().enabled = false;

        GameObject mazoVisual = Instantiate(prefabCartaRival, zonaMazo);
        muestraGO.transform.SetAsFirstSibling();
        mazoVisual.transform.SetAsLastSibling();

        // 1. Desactivar el script de arrastre (ya lo tenías)
        if (muestraGO.GetComponent<DraggableCarta>() != null)
            muestraGO.GetComponent<DraggableCarta>().enabled = false;

        // 2. NUEVO: Desactivar la detección de ratón en el CanvasGroup
        CanvasGroup cg = muestraGO.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = false; // El ratón la atraviesa
            cg.interactable = false;   // No responde a clics
        }

        // 3. NUEVO: Desactivar Raycast Target en la imagen directamente
        if (muestraGO.GetComponent<UnityEngine.UI.Image>() != null)
            muestraGO.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
    }

    public void RepartirPartidaInicial()
    {
        Transform[] jugadores = { manoJugador, manoRival2, manoCompañero, manoRival1 };
        for (int ronda = 0; ronda < 2; ronda++)
        {
            foreach (Transform j in jugadores)
            {
                for (int i = 0; i < 3; i++) RepartirCarta(j, (j == manoJugador) ? cartaPrefab : prefabCartaRival);
            }
        }
    }

    public void RepartirCarta(Transform contenedor, GameObject prefab)
    {
        if (mazoPrincipal.Count <= 0) return;

        Carta logica = mazoPrincipal[0];
        mazoPrincipal.RemoveAt(0);

        GameObject nueva = Instantiate(prefab, contenedor);
        CartaVisual visual = nueva.GetComponent<CartaVisual>();

        if (visual != null)
        {
            // 1. IMPORTANTE: Guardamos la lógica primero
            visual.cartaLogica = logica;

            // 2. Si es para el jugador, llamamos a ConfigurarCarta para "pintar" la imagen
            if (contenedor == manoJugador)
            {
                visual.ConfigurarCarta(logica);
            }
        }
    }

    public void CartaLanzada(GameObject cartaGO)
    {
        if (!cartasEnBaza.Contains(cartaGO))
        {
            cartasEnBaza.Add(cartaGO);
        }

        // Si el jugador es quien tira la carta (o la primera carta de la baza)
        // iniciamos el turno de los demás
        if (cartasEnBaza.Count == 1)
        {
            StartCoroutine(TurnoRivales());
        }
    }

    IEnumerator TurnoRivales()
    {
        // Cambiamos el orden para que siga el sentido del reloj o el que necesites
        // Si quieres que después de ti tire el de tu derecha:
        Transform[] manosRivales = { manoRival2, manoCompañero, manoRival1 };
        Transform[] posicionesBaza = { posRival2, posCompañero, posRival1 };

        for (int i = 0; i < manosRivales.Length; i++)
        {
            yield return new WaitForSeconds(0.7f);

            if (manosRivales[i].childCount > 0)
            {
                GameObject cartaRival = manosRivales[i].GetChild(0).gameObject;
                CartaVisual cv = cartaRival.GetComponent<CartaVisual>();

                // FORZAR BOCA ARRIBA:
                if (cv != null && cv.cartaLogica != null)
                {
                    cv.ConfigurarCarta(cv.cartaLogica);
                    // Si el prefab tiene un objeto "Dorso" hijo que lo tapa, lo desactivamos:
                    Transform dorso = cartaRival.transform.Find("Dorso");
                    if (dorso != null) dorso.gameObject.SetActive(false);
                }

                StartCoroutine(AnimarCarta(cartaRival, posicionesBaza[i].position, 0.4f));

                yield return new WaitForSeconds(0.4f);
                cartaRival.transform.SetParent(zonaBaza);
                if (!cartasEnBaza.Contains(cartaRival)) cartasEnBaza.Add(cartaRival);
            }
        }

        if (cartasEnBaza.Count == 4) Invoke("FinalizarBaza", 1.5f);
    }

    void FinalizarBaza()
    {
        foreach (GameObject c in cartasEnBaza) Destroy(c);
        cartasEnBaza.Clear();
        RobarTodos();
    }

    void RobarTodos()
    {
        Transform[] orden = { manoJugador, manoRival1, manoCompañero, manoRival2 };
        foreach (Transform j in orden) RepartirCarta(j, (j == manoJugador) ? cartaPrefab : prefabCartaRival);
    }

    public IEnumerator AnimarCarta(GameObject carta, Vector3 destino, float duracion)
    {
        float tiempo = 0;
        Vector3 posIni = carta.transform.position;
        Vector3 escalaIni = carta.transform.localScale;
        Vector3 escalaFin = new Vector3(0.7f, 0.7f, 1f); // Reducción

        while (tiempo < duracion)
        {
            carta.transform.position = Vector3.Lerp(posIni, destino, tiempo / duracion);
            carta.transform.localScale = Vector3.Lerp(escalaIni, escalaFin, tiempo / duracion);
            tiempo += Time.deltaTime;
            yield return null;
        }
        carta.transform.position = destino;
        carta.transform.localScale = escalaFin;
    }

    public IEnumerator RecogerBaza(Transform ganador)
    {
        yield return new WaitForSeconds(1.0f);

        foreach (GameObject carta in cartasEnBaza) // Asegúrate que diga 'in', no 'en'
        {
            if (carta != null)
            {
                StartCoroutine(AnimarCarta(carta, ganador.position, 0.5f));
            }
        }

        yield return new WaitForSeconds(0.5f);

        foreach (GameObject carta in cartasEnBaza)
        {
            if (carta != null) Destroy(carta);
        }
        cartasEnBaza.Clear();
    }
}