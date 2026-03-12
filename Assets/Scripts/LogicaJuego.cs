using UnityEngine;

public class LogicaJuego : MonoBehaviour
{
    // Esta función devuelve 'true' si la cartaAtacante gana a la cartaDefensora
    public bool GanaLaCarta(Carta cartaAtacante, Carta cartaDefensora, Carta.Palo triunfo)
    {
        // Caso 1: Ambas son del mismo palo
        if (cartaAtacante.palo == cartaDefensora.palo)
        {
            return CompararPuntos(cartaAtacante, cartaDefensora);
        }

        // Caso 2: La defensora es triunfo y la atacante no
        if (cartaDefensora.palo == triunfo && cartaAtacante.palo != triunfo)
        {
            return false; // Gana la defensora porque es triunfo
        }

        // Caso 3: La atacante es triunfo y la defensora no
        if (cartaAtacante.palo == triunfo && cartaDefensora.palo != triunfo)
        {
            return true; // Gana la atacante porque es triunfo
        }

        // Caso 4: Son de palos distintos y ninguna es triunfo
        // En el Guiñote, si no eres del palo de salida ni eres triunfo, pierdes.
        return true;
    }

    private bool CompararPuntos(Carta c1, Carta c2)
    {
        // Comparamos por puntos (As=11, Tres=10, etc.)
        if (c1.puntos > c2.puntos) return true;
        if (c1.puntos < c2.puntos) return false;

        // Si tienen los mismos puntos (ej: un 7 y un 6), gana el número más alto
        return c1.numero > c2.numero;
    }
}