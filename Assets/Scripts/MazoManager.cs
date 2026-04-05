using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MazoManager : MonoBehaviour
{
    public List<Carta> todasLasCartas;
    private List<Carta> mazoPrincipal = new List<Carta>();
    private List<GameObject> cartasEnBaza = new List<GameObject>();
    private List<int> indicesJugadoresEnBaza = new List<int>();

    [Header("Configuración Visual")]
    public GameObject cartaPrefab;
    public GameObject prefabCartaRival;

    [Header("Contenedores")]
    public Transform manoJugador;    // ID 0
    public Transform manoRival1;     // ID 1 (Derecha)
    public Transform manoCompañero;  // ID 2 (Frente)
    public Transform manoRival2;     // ID 3 (Izquierda)
    public Transform zonaMazo;
    public Transform zonaBaza;

    [Header("Posiciones de Baza")]
    public Transform posJugador;
    public Transform posRival1;
    public Transform posCompañero;
    public Transform posRival2;

    [Header("Logica de Juego")]
    public Carta.Palo paloTriunfo;
    private Carta.Palo paloSalida;
    private bool primeraCartaDeBaza = true;

    [Header("Control de Turnos")]
    public int turnoActual;
    public bool puedeJugar = false;

    public Vector3 escalaBaza = new Vector3(0.7f, 0.7f, 1f);
    public Carta cartaTriunfo;

    void Start()
    {
        InicializarMazo();

        if (mazoPrincipal.Count > 0)
        {
            Barajar();
            DefirnirTriunfo();
            StartCoroutine(PrepararPartida());
        }
        else
        {
            Debug.LogError("¡No has asignado las cartas en el Inspector!");
        }
    }

    IEnumerator PrepararPartida()
    {
        yield return StartCoroutine(RepartirPartidaInicialAnimado());
        // El primer turno de la partida es aleatorio
        int primerTurno = Random.Range(0, 4);
        SetTurno(primerTurno);
    }

    public void SetTurno(int nuevoTurno)
    {
        // Detenemos cualquier rutina de IA previa para evitar que dos IAs jueguen a la vez
        StopCoroutine("TurnoIA");
        
        turnoActual = nuevoTurno;

        if (turnoActual == 0)
        {
            puedeJugar = true;
            Debug.Log("Es tu turno (Jugador).");
        }
        else
        {
            puedeJugar = false;
            Debug.Log("Turno del Jugador " + turnoActual);
            StartCoroutine(TurnoIA(turnoActual));
        }
    }

    public void CartaLanzada(GameObject cartaGO)
    {
        if (!cartasEnBaza.Contains(cartaGO))
        {
            cartasEnBaza.Add(cartaGO);
            indicesJugadoresEnBaza.Add(turnoActual);
        }

        CartaVisual cv = cartaGO.GetComponent<CartaVisual>();
        if (cv != null && cv.cartaLogica != null)
        {
            cv.ConfigurarCarta(cv.cartaLogica);
            if (primeraCartaDeBaza)
            {
                paloSalida = cv.cartaLogica.palo;
                primeraCartaDeBaza = false;
            }
        }

        // Sacamos la carta de la mano y la ponemos en la zona de baza (padre neutro)
        cartaGO.transform.SetParent(zonaBaza, true);

        Transform destino = ObtenerPosicionBaza(turnoActual);

        // Giro 0 para que el jugador las vea siempre rectas
        float giro = 0f;

        StartCoroutine(AnimarCartaConGiro(cartaGO, destino.position, 0.4f, giro));

        if (cartasEnBaza.Count < 4)
        {
            int siguiente = (turnoActual + 1) % 4;
            StartCoroutine(EsperarSiguienteTurno(siguiente));
        }
        else
        {
            StartCoroutine(EsperarDeterminarGanador());
        }
    }

    IEnumerator EsperarSiguienteTurno(int proximo)
    {
        yield return new WaitForSeconds(0.6f);
        SetTurno(proximo);
    }

    IEnumerator EsperarDeterminarGanador()
    {
        yield return new WaitForSeconds(1.0f);
        DeterminarGanadorBaza();
    }

    void DeterminarGanadorBaza()
    {
        int indiceGanadorEnBaza = 0;
        GameObject mejorCartaGO = cartasEnBaza[0];
        Carta mejorLogica = mejorCartaGO.GetComponent<CartaVisual>().cartaLogica;

        for (int i = 1; i < cartasEnBaza.Count; i++)
        {
            Carta nuevaLogica = cartasEnBaza[i].GetComponent<CartaVisual>().cartaLogica;

            if (nuevaLogica.palo == paloTriunfo && mejorLogica.palo != paloTriunfo)
            {
                mejorCartaGO = cartasEnBaza[i];
                mejorLogica = nuevaLogica;
                indiceGanadorEnBaza = i;
            }
            else if (nuevaLogica.palo == mejorLogica.palo)
            {
                if (nuevaLogica.puntos > mejorLogica.puntos ||
                   (nuevaLogica.puntos == mejorLogica.puntos && nuevaLogica.numero > mejorLogica.numero))
                {
                    mejorCartaGO = cartasEnBaza[i];
                    mejorLogica = nuevaLogica;
                    indiceGanadorEnBaza = i;
                }
            }
        }

        int idGanadorReal = indicesJugadoresEnBaza[indiceGanadorEnBaza];
        StartCoroutine(LimpiarYPrepararSiguiente(idGanadorReal));
    }

    // --- CORRECCIÓN AQUÍ ---
    IEnumerator LimpiarYPrepararSiguiente(int ganadorID)
    {
        yield return new WaitForSeconds(1.0f);
        
        // 1. Limpiamos las cartas físicas de la mesa
        foreach (GameObject c in cartasEnBaza) Destroy(c);
        cartasEnBaza.Clear();
        indicesJugadoresEnBaza.Clear();
        primeraCartaDeBaza = true;

        // 2. Si quedan cartas en el mazo, robamos
        if (mazoPrincipal.Count > 0)
        {
            // ESPERAMOS a que la secuencia de robo termine antes de seguir
            yield return StartCoroutine(SecuenciaRobo(ganadorID));
        }

        // 3. Pequeña pausa extra para que el jugador asimile quién ganó
        yield return new WaitForSeconds(0.5f);

        // 4. AHORA SÍ, asignamos el turno al ganador de la baza
        Debug.Log("Baza finalizada. El ganador " + ganadorID + " abre la siguiente.");
        SetTurno(ganadorID);
    }

    IEnumerator SecuenciaRobo(int ganadorID)
    {
        // El orden de robo siempre empieza por el que ganó la baza
        for (int i = 0; i < 4; i++)
        {
            int idActual = (ganadorID + i) % 4;
            Transform contenedor = ObtenerContenedorMano(idActual);

            if (mazoPrincipal.Count > 0)
            {
                // Esperamos a que cada carta llegue a la mano antes de repartir la siguiente (o lo hacemos rápido)
                yield return StartCoroutine(AnimarRoboCarta(contenedor, (idActual == 0) ? cartaPrefab : prefabCartaRival, idActual == 0));
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    Transform ObtenerContenedorMano(int id)
    {
        switch (id)
        {
            case 0: return manoJugador;
            case 1: return manoRival1;
            case 2: return manoCompañero;
            case 3: return manoRival2;
            default: return null;
        }
    }

    Transform ObtenerPosicionBaza(int id)
    {
        if (id == 0) return posJugador;
        if (id == 1) return posRival1;
        if (id == 2) return posCompañero;
        return posRival2;
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

    void DefirnirTriunfo()
    {
        if (mazoPrincipal.Count <= 0) return;
        cartaTriunfo = mazoPrincipal[0];
        mazoPrincipal.RemoveAt(0);
        mazoPrincipal.Add(cartaTriunfo);
        paloTriunfo = cartaTriunfo.palo;

        GameObject muestraGO = Instantiate(cartaPrefab, zonaMazo);
        CartaVisual visual = muestraGO.GetComponent<CartaVisual>();
        if (visual != null) visual.ConfigurarCarta(cartaTriunfo);

        muestraGO.transform.localRotation = Quaternion.Euler(0, 0, 90);
        muestraGO.transform.localPosition = new Vector3(-70, 0, 0);
        muestraGO.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

        if (muestraGO.GetComponent<DraggableCarta>() != null) muestraGO.GetComponent<DraggableCarta>().enabled = false;
        SetEstadoCarta(muestraGO, true);

        GameObject mazoVisual = Instantiate(prefabCartaRival, zonaMazo);
        muestraGO.transform.SetAsFirstSibling();
        mazoVisual.transform.SetAsLastSibling();
    }

    public IEnumerator RepartirPartidaInicialAnimado()
    {
        int[] ordenReparto = { 0, 1, 2, 3 };

        for (int ronda = 0; ronda < 2; ronda++)
        {
            foreach (int id in ordenReparto)
            {
                Transform contenedor = ObtenerContenedorMano(id);
                for (int i = 0; i < 3; i++)
                {
                    StartCoroutine(AnimarRoboCarta(contenedor, (id == 0) ? cartaPrefab : prefabCartaRival, id == 0));
                    yield return new WaitForSeconds(0.15f);
                }
                yield return new WaitForSeconds(0.2f);
            }
        }
    }

    private void SetEstadoCarta(GameObject carta, bool mostrarCara)
    {
        foreach (Transform hijo in carta.transform)
        {
            if (hijo.name.ToLower().Contains("reverso")) hijo.gameObject.SetActive(!mostrarCara);
            if (hijo.name.ToLower().Contains("cara")) hijo.gameObject.SetActive(mostrarCara);
        }
    }

    IEnumerator TurnoIA(int indiceIA)
    {
        yield return new WaitForSeconds(1.2f);
        Transform manoIA = ObtenerContenedorMano(indiceIA);

        if (manoIA != null && manoIA.childCount > 0)
        {
            GameObject cartaIA = manoIA.GetChild(0).gameObject;
            SetEstadoCarta(cartaIA, true);
            CartaLanzada(cartaIA);
        }
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

    public IEnumerator AnimarRoboCarta(Transform contenedor, GameObject prefab, bool esJugador)
    {
        if (mazoPrincipal.Count <= 0) yield break;

        Carta logica = mazoPrincipal[0];
        mazoPrincipal.RemoveAt(0);

        GameObject nueva = Instantiate(prefab, zonaMazo.position, contenedor.rotation, contenedor);

        CartaVisual visual = nueva.GetComponent<CartaVisual>();
        if (visual != null) visual.ConfigurarCarta(logica);

        SetEstadoCarta(nueva, esJugador);

        float duracion = 0.4f;
        float tiempo = 0;
        Vector3 posIni = nueva.transform.position;

        while (tiempo < duracion)
        {
            nueva.transform.position = Vector3.Lerp(posIni, contenedor.position, tiempo / duracion);
            tiempo += Time.deltaTime;
            yield return null;
        }

        nueva.transform.localPosition = Vector3.zero;
        nueva.transform.localRotation = Quaternion.identity;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contenedor.GetComponent<RectTransform>());
    }
}