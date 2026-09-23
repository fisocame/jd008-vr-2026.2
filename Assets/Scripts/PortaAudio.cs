using UnityEngine;

public class PortaAudio : MonoBehaviour
{
    public AudioSource audioSource2; // A caixinha JBL dentro do jogo
    public AudioClip somRangido2; // A música que vai tocar na caixinha

    public void TocarSom()
    {
        audioSource2.PlayOneShot(somRangido2);
    }

    public void DispararEvento()
    {
        Debug.Log("Porta abriu!");

        // Você pode colocar um evento de luz acender ou apagar com a animação
    }
}
