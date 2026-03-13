using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableCarta : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private Transform parentReturnTo = null;
    private GameObject placeholder = null;
    private MazoManager mazoManager;
    private int nuevoIndice; // ESTA ES LA VARIABLE QUE FALTABA

    void Start() { mazoManager = FindFirstObjectByType<MazoManager>(); }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!this.enabled) return;
        if (!eventData.dragging)
        {
            TirarCartaALaBaza();
        }
    }

    public void TirarCartaALaBaza()
    {
        this.enabled = false;
        // La sacamos al padre de la zona de baza para que no se vea limitada por el layout mientras vuela
        this.transform.SetParent(mazoManager.zonaBaza.parent);
        StartCoroutine(AnimarTirada(mazoManager.zonaBaza.position, 0.4f));
        mazoManager.CartaLanzada(this.gameObject);
    }

    IEnumerator AnimarTirada(Vector3 destino, float duracion)
    {
        float tiempo = 0;
        Vector3 posIni = this.transform.position;
        Vector3 escalaIni = this.transform.localScale;
        Vector3 escalaFin = new Vector3(0.7f, 0.7f, 1f);

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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!this.enabled) return;

        parentReturnTo = this.transform.parent;
        nuevoIndice = this.transform.GetSiblingIndex();

        placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(parentReturnTo);
        LayoutElement le = placeholder.AddComponent<LayoutElement>();
        RectTransform rt = GetComponent<RectTransform>();
        le.preferredWidth = rt.rect.width;
        le.preferredHeight = rt.rect.height;

        placeholder.transform.SetSiblingIndex(nuevoIndice);
        this.transform.SetParent(this.transform.parent.parent);
        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!this.enabled) return;
        this.transform.position = eventData.position;

        for (int i = 0; i < parentReturnTo.childCount; i++)
        {
            if (this.transform.position.x < parentReturnTo.GetChild(i).position.x)
            {
                nuevoIndice = i;
                if (placeholder.transform.GetSiblingIndex() != nuevoIndice)
                    placeholder.transform.SetSiblingIndex(nuevoIndice);
                break;
            }
            nuevoIndice = i;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!this.enabled) return;
        GetComponent<CanvasGroup>().blocksRaycasts = true;

        if (mazoManager != null && RectTransformUtility.RectangleContainsScreenPoint(mazoManager.zonaBaza as RectTransform, eventData.position))
        {
            TirarCartaALaBaza();
            if (placeholder != null) Destroy(placeholder);
        }
        else
        {
            StartCoroutine(VolverALaMano());
        }
    }

    IEnumerator VolverALaMano()
    {
        float tiempo = 0;
        Vector3 posIni = this.transform.position;
        while (tiempo < 0.2f)
        {
            this.transform.position = Vector3.Lerp(posIni, placeholder.transform.position, tiempo / 0.2f);
            tiempo += Time.deltaTime;
            yield return null;
        }
        this.transform.SetParent(parentReturnTo);
        this.transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex());
        Destroy(placeholder);
    }
}