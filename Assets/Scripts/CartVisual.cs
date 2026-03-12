using UnityEngine;
using UnityEngine.UI; // Si usas UI, o SpriteRenderer para objetos 2D

public class CartaVisual : MonoBehaviour
{
    public Carta datosCarta; // Aquí guardaremos la información (As de Oros, etc.)
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Este método "dibuja" la carta con sus datos
    public void ConfigurarCarta(Carta nuevaCarta)
    {
        datosCarta = nuevaCarta;
        spriteRenderer.sprite = nuevaCarta.imagenCarta;
        gameObject.name = nuevaCarta.numero + " de " + nuevaCarta.palo;
    }
}