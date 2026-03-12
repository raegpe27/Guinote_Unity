using UnityEngine;

[CreateAssetMenu(fileName = "Nueva Carta", menuName = "JuegoCartas/Carta")]
public class Carta : ScriptableObject
{
    public enum Palo { Oros, Copas, Espadas, Bastos }

    public Palo palo;
    public int numero; // 1 al 7, 10, 11, 12
    public int puntos; // As=11, Tres=10, Rey=4, Sota=3, Caballo=2...
    public Sprite imagenCarta;
}