using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableCarta : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private Transform parentReturnTo = null;
    private GameObject placeholder = null;
    private MazoManager mazoManager;
    private int nuevoIndice;

    void Start()
    {
        mazoManager = FindFirstObjectByType<MazoManager>();
    }

    // --- ACCIÓN DE TIRAR CARTA (SOLO POR CLIC) ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!this.enabled) return;

        // Si el usuario ha movido el ratón (arrastrado), no disparamos el clic
        if (eventData.dragging) return;

        if (!mazoManager.puedeJugar)
        {
            Debug.Log("No es tu turno todavía.");
            return;
        }

        TirarCartaALaBaza();
    }

    public void TirarCartaALaBaza()
    {
        this.enabled = false; // Desactivamos para evitar doble envío

        // La sacamos al padre de la zona de baza para que vuele libremente
        this.transform.SetParent(mazoManager.zonaBaza.parent);
        StartCoroutine(AnimarTirada(mazoManager.zonaBaza.position, 0.4f));

        // Notificamos al manager
        mazoManager.CartaLanzada(this.gameObject);
    }

    IEnumerator AnimarTirada(Vector3 destino, float duracion)
    {
        float tiempo = 0;
        Vector3 posIni = this.transform.position;
        Vector3 escalaIni = this.transform.localScale;
        Vector3 escalaFin = mazoManager.escalaBaza;

        while (tiempo < duracion)
        {
            this.transform.position = Vector3.Lerp(posIni, destino, tiempo / duracion);
            this.transform.localScale = Vector3.Lerp(escalaIni, escalaFin, tiempo / duracion);
            tiempo += Time.deltaTime;
            yield return null;
        }

        this.transform.position = destino;
        this.transform.localScale = escalaFin;
        this.transform.SetParent(mazoManager.zonaBaza);
    }

    // --- LÓGICA DE REORDENAR (SOLO ARRASTRE) ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!this.enabled) return;

        parentReturnTo = this.transform.parent; // La ManoJugador
        nuevoIndice = this.transform.GetSiblingIndex();

        // Creamos el hueco (placeholder) para que las cartas no bailen
        placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(parentReturnTo);

        LayoutElement le = placeholder.AddComponent<LayoutElement>();
        RectTransform rt = GetComponent<RectTransform>();
        le.preferredWidth = rt.rect.width;
        le.preferredHeight = rt.rect.height;

        placeholder.transform.SetSiblingIndex(nuevoIndice);

        // Sacamos la carta al Canvas para que se vea por encima de todo
        this.transform.SetParent(this.transform.parent.parent);

        // IMPORTANTE: Bloqueamos raycasts para que el ratón "atraviese" la carta y detecte las de debajo
        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!this.enabled) return;

        // La carta sigue al ratón
        this.transform.position = eventData.position;

        // Lógica de reordenamiento visual en la mano
        int indiceTemporal = parentReturnTo.childCount;

        for (int i = 0; i < parentReturnTo.childCount; i++)
        {
            if (this.transform.position.x < parentReturnTo.GetChild(i).position.x)
            {
                indiceTemporal = i;
                if (placeholder.transform.GetSiblingIndex() != indiceTemporal)
                {
                    placeholder.transform.SetSiblingIndex(indiceTemporal);
                }
                break;
            }
        }
        nuevoIndice = indiceTemporal;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!this.enabled) return;

        // Restauramos la detección del ratón
        GetComponent<CanvasGroup>().blocksRaycasts = true;

        // HE ELIMINADO LA CONDICIÓN DE TIRAR CARTA AQUÍ.
        // Al soltar, la carta SIEMPRE vuelve a la mano en su nueva posición.
        StartCoroutine(VolverALaMano());
    }

    IEnumerator VolverALaMano()
    {
        float tiempo = 0;
        Vector3 posIni = this.transform.position;

        // Animación suave de regreso al hueco del placeholder
        while (tiempo < 0.15f)
        {
            this.transform.position = Vector3.Lerp(posIni, placeholder.transform.position, tiempo / 0.15f);
            tiempo += Time.deltaTime;
            yield return null;
        }

        // Devolvemos la carta al Layout de la mano
        this.transform.SetParent(parentReturnTo);
        this.transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex());

        // Eliminamos el hueco temporal
        Destroy(placeholder);
    }
}