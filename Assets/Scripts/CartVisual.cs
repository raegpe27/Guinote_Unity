using UnityEngine;
using UnityEngine.UI;

public class CartaVisual : MonoBehaviour
{
    public Carta cartaLogica;
    public Image imagenComponente;

    void Awake()
    {
        // Esto buscará automáticamente el componente Image en el mismo objeto
        if (imagenComponente == null)
        {
            imagenComponente = GetComponent<Image>();
        }
    }

    public void ConfigurarCarta(Carta nuevaCarta)
    {
        if (nuevaCarta == null) return;
        cartaLogica = nuevaCarta;

        if (imagenComponente == null) imagenComponente = GetComponent<Image>();

        if (imagenComponente != null && nuevaCarta.imagenCarta != null)
        {
            imagenComponente.sprite = nuevaCarta.imagenCarta;
        }
    }
}