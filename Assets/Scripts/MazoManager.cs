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

    public Vector3 escalaBaza = new Vector3(0.7f, 0.7f, 1f);

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
        muestraGO.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
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

        // 1. Sacamos la lógica del mazo
        Carta logica = mazoPrincipal[0];
        mazoPrincipal.RemoveAt(0);

        // 2. Creamos la carta visual (ahora prefab es un GameObject)
        GameObject nueva = Instantiate(prefab, contenedor);
        CartaVisual visual = nueva.GetComponent<CartaVisual>();

        if (visual != null)
        {
            // 3. Le pasamos los datos y configuramos los sprites
            visual.ConfigurarCarta(logica);

            // 4. Decidimos qué se ve (Cara o Reverso)
            bool esJugador = (contenedor == manoJugador);

            // Buscamos los hijos para encender/apagar según quién recibe la carta
            foreach (Transform hijo in nueva.transform)
            {
                if (hijo.name.ToLower().Contains("reverso"))
                    hijo.gameObject.SetActive(!esJugador);

                if (hijo.name.ToLower().Contains("cara"))
                    hijo.gameObject.SetActive(esJugador);
            }
        }
    }

    // Función auxiliar para no repetir código de activar/desactivar hijos
    private void SetEstadoCarta(GameObject carta, bool mostrarCara)
    {
        foreach (Transform hijo in carta.transform)
        {
            if (hijo.name.ToLower().Contains("reverso")) hijo.gameObject.SetActive(!mostrarCara);
            if (hijo.name.ToLower().Contains("cara")) hijo.gameObject.SetActive(mostrarCara);
        }
    }

    public void CartaLanzada(GameObject cartaGO)
    {
        if (!cartasEnBaza.Contains(cartaGO))
        {
            cartasEnBaza.Add(cartaGO);
        }

        // Forzar Boca Arriba para el jugador (si no lo estuviera)
        CartaVisual cv = cartaGO.GetComponent<CartaVisual>();
        if (cv != null && cv.cartaLogica != null) cv.ConfigurarCarta(cv.cartaLogica);

        // Mover a la posición del jugador en la cruz y escalar
        StartCoroutine(AnimarCarta(cartaGO, posJugador.position, 0.4f));

        if (cartasEnBaza.Count == 1)
        {
            StartCoroutine(TurnoRivales());
        }
    }

    IEnumerator TurnoRivales()
    {
        // Listas emparejadas: Mano -> Su sitio -> Su giro en la cruz
        Transform[] manosRivales = { manoRival1, manoCompañero, manoRival2 };
        Transform[] posicionesBaza = { posRival1, posCompañero, posRival2 };
        float[] giros = { 90f, 0f, 90f }; // Rival 1 y 2 giran 90 grados

        for (int i = 0; i < manosRivales.Length; i++)
        {
            yield return new WaitForSeconds(0.8f);

            if (manosRivales[i].childCount > 0)
            {
                GameObject cartaRival = manosRivales[i].GetChild(0).gameObject;

                CartaVisual cv = cartaRival.GetComponent<CartaVisual>();
                if (cv != null)
                {
                    cv.ConfigurarCarta(cv.cartaLogica); // Esto pone la imagen real

                    // Buscar el reverso sin importar si se llama "reverso" o "Reverso"
                    foreach (Transform hijo in cartaRival.transform)
                    {
                        if (hijo.name.ToLower().Contains("reverso"))
                        {
                            hijo.gameObject.SetActive(false);
                        }
                        if (hijo.name.ToLower().Contains("cara"))
                        {
                            hijo.gameObject.SetActive(true); // Asegúrate de encender la cara
                        }
                    }
                }

                // Mover a su posición asignada con su giro y tamaño uniforme
                StartCoroutine(AnimarCartaConGiro(cartaRival, posicionesBaza[i].position, 0.4f, giros[i]));

                yield return new WaitForSeconds(0.4f);

                cartaRival.transform.SetParent(zonaBaza);
                if (!cartasEnBaza.Contains(cartaRival)) cartasEnBaza.Add(cartaRival);
            }
        }

        if (cartasEnBaza.Count == 4) Invoke("FinalizarBaza", 1.5f);
    }

    // Nueva versión de AnimarCarta que también controla la Escala (Tamaño)
    public IEnumerator AnimarCarta(GameObject carta, Vector3 destino, float duracion)
    {
        float tiempo = 0;
        Vector3 posIni = carta.transform.position;
        Vector3 escalaIni = carta.transform.localScale;

        while (tiempo < duracion)
        {
            carta.transform.position = Vector3.Lerp(posIni, destino, tiempo / duracion);
            // Escalamos hacia el tamaño uniforme de la baza
            carta.transform.localScale = Vector3.Lerp(escalaIni, escalaBaza, tiempo / duracion);

            tiempo += Time.deltaTime;
            yield return null;
        }
        carta.transform.position = destino;
        carta.transform.localScale = escalaBaza;
    }

    public IEnumerator AnimarCartaConGiro(GameObject carta, Vector3 destino, float duracion, float giroZFinal)
    {
        float tiempo = 0;
        Vector3 posIni = carta.transform.position;
        Vector3 escalaIni = carta.transform.localScale;
        Quaternion rotIni = carta.transform.rotation;
        Quaternion rotFin = Quaternion.Euler(0, 0, giroZFinal);

        while (tiempo < duracion)
        {
            carta.transform.position = Vector3.Lerp(posIni, destino, tiempo / duracion);
            carta.transform.localScale = Vector3.Lerp(escalaIni, escalaBaza, tiempo / duracion);
            carta.transform.rotation = Quaternion.Lerp(rotIni, rotFin, tiempo / duracion);

            tiempo += Time.deltaTime;
            yield return null;
        }
        carta.transform.position = destino;
        carta.transform.localScale = escalaBaza;
        carta.transform.rotation = rotFin;
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