using UnityEngine;

/// <summary>
/// Reproduce sonido de ambiente en loop en la escena nivel_1_ypf.
/// El clip se carga automáticamente desde Resources/ — no requiere asignación manual.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AmbienteSonido : MonoBehaviour
{
    [Header("Audio ambiente")]
    [Tooltip("Arrastrá aquí el clip 'sonidoambiente' desde Assets. Si está vacío se carga automáticamente.")]
    public AudioClip clipAmbiente;

    [Range(0f, 1f)]
    [Tooltip("Volumen del ambiente (0-1)")]
    public float volumenObjetivo = 0.25f;

    [Tooltip("Segundos de fade in al iniciar")]
    public float tiempoFadeIn = 2f;

    private AudioSource src;
    private float tiempoTranscurrido = 0f;

    void Awake()
    {
        src = GetComponent<AudioSource>();

        // Intentar cargar clip si no fue asignado en el Inspector
        if (clipAmbiente == null)
        {
            clipAmbiente = Resources.Load<AudioClip>("sonidoambiente");

            if (clipAmbiente == null)
                Debug.LogError("[AmbienteSonido] No se encontró 'sonidoambiente' en Assets/Resources/");
            else
                Debug.Log("[AmbienteSonido] Clip cargado desde Resources OK.");
        }

        if (clipAmbiente == null) return;

        src.clip        = clipAmbiente;
        src.loop        = true;
        src.spatialBlend = 0f;      // 2D — se escucha en toda la escena
        src.volume      = 0f;       // Empieza en silencio
        src.playOnAwake = false;

        // Verificar AudioListener
        AudioListener listener = FindFirstObjectByType<AudioListener>();
        if (listener == null)
            Debug.LogError("[AmbienteSonido] No hay AudioListener en la escena. Creando uno de emergencia.");
        else
            Debug.Log("[AmbienteSonido] AudioListener encontrado en: " + listener.gameObject.name +
                      " | volume global: " + AudioListener.volume);

        src.Play();
        Debug.Log("[AmbienteSonido] Play() llamado. isPlaying=" + src.isPlaying + " clip=" + src.clip.name);
    }

    void Update()
    {
        if (src == null || !src.isPlaying) return;

        if (tiempoTranscurrido < tiempoFadeIn)
        {
            tiempoTranscurrido += Time.deltaTime;
            src.volume = Mathf.Lerp(0f, volumenObjetivo, tiempoTranscurrido / tiempoFadeIn);
        }
        else
        {
            src.volume = volumenObjetivo;
        }
    }
}
