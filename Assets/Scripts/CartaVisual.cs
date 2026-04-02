using UnityEngine;
using UnityEngine.UI;

public class CartaVisual : MonoBehaviour
{
    public Carta cartaLogica;
    [SerializeField]  private Image imagenComponente;
    private GameObject objetoReverso;

    public void ConfigurarCarta(Carta nuevaCarta)
    {
        if (nuevaCarta == null) return;
        cartaLogica = nuevaCarta;

        // Buscamos los hijos manualmente para no fallar
        if (imagenComponente == null)
        {
            Transform tCara = transform.Find("cara");
            if (tCara != null) imagenComponente = tCara.GetComponent<Image>();
        }

        if (imagenComponente != null && nuevaCarta.imagenCarta != null)
        {
            imagenComponente.sprite = nuevaCarta.imagenCarta;
            imagenComponente.gameObject.SetActive(true); // Encendemos la cara
        }

        // Apagamos el reverso explícitamente
        Transform tReverso = transform.Find("reverso");
        if (tReverso != null && transform.parent == null) // Solo si no está en mano
        {
            tReverso.gameObject.SetActive(false);
        }
    }
}