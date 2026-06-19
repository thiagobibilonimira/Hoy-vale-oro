using UnityEngine;

public class SonidoMovimiento : MonoBehaviour
{
    [Header("Configuración")]
    public float umbralVelocidad = 0.1f; // velocidad mínima para que suene

    private AudioSource audioSource;
    private Vector3 posicionAnterior;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        posicionAnterior = transform.position;
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        float velocidad = (transform.position - posicionAnterior).magnitude / Time.deltaTime;

        if (velocidad > umbralVelocidad)
        {
            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        else
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
        }

        posicionAnterior = transform.position;
    }
}