using System.Collections.Generic;
using UnityEngine;

public class MazoManager : MonoBehaviour
{
    public List<Carta> todasLasCartas; // Arrastra aquí tus 40 archivos de carta
    private List<Carta> mazoPrincipal = new List<Carta>();

    void Start()
    {
        InicializarMazo();
        Barajar();
    }

    void InicializarMazo()
    {
        // Copiamos la lista original para no modificar los archivos base
        mazoPrincipal = new List<Carta>(todasLasCartas);
        Debug.Log("Mazo listo con " + mazoPrincipal.Count + " cartas.");
    }

    public void Barajar()
    {
        // Algoritmo de Fisher-Yates (el más efectivo para barajar)
        for (int i = 0; i < mazoPrincipal.Count; i++)
        {
            Carta temp = mazoPrincipal[i];
            int randomIndex = Random.Range(i, mazoPrincipal.Count);
            mazoPrincipal[i] = mazoPrincipal[randomIndex];
            mazoPrincipal[randomIndex] = temp;
        }
        Debug.Log("Mazo barajado.");
    }

    public Carta RobarCarta()
    {
        if (mazoPrincipal.Count <= 0) return null;

        Carta cartaRobada = mazoPrincipal[0];
        mazoPrincipal.RemoveAt(0);
        return cartaRobada;
    }
}