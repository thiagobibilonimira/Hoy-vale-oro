#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class AmbienteSetup
{
    [MenuItem("Tools/Agregar Sonido Ambiente a nivel_1_ypf")]
    public static void AgregarSonidoAmbiente()
    {
        string scenePath = "Assets/Scenes/nivel_1_ypf.unity";

        if (!System.IO.File.Exists(scenePath))
        {
            EditorUtility.DisplayDialog("Error", "No se encontró la escena 'nivel_1_ypf' en Assets/Scenes/.", "OK");
            return;
        }

        string escenaAnterior = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.SaveOpenScenes();
        var escena = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Eliminar instancias previas del objeto ambiente
        GameObject existente = GameObject.Find("SonidoAmbiente");
        if (existente != null)
        {
            GameObject.DestroyImmediate(existente);
            Debug.Log("[AmbienteSetup] Objeto anterior eliminado.");
        }

        // Cargar el clip directamente desde Assets/audio/
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/audio/sonidoambiente.wav");
        if (clip == null)
        {
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/sonidoambiente.wav");
        }
        if (clip == null)
        {
            Debug.LogError("[AmbienteSetup] No se encontró 'sonidoambiente.wav' en Assets/audio/ ni Assets/Resources/.");
            EditorUtility.DisplayDialog("Error", "No se encontró 'sonidoambiente.wav'.", "OK");
            return;
        }

        // Crear GameObject con AudioSource + AmbienteSonido
        GameObject ambienteObj = new GameObject("SonidoAmbiente");

        // Agregar AudioSource con configuración base
        AudioSource src   = ambienteObj.AddComponent<AudioSource>();
        src.clip          = clip;
        src.loop          = true;
        src.playOnAwake   = true;
        src.spatialBlend  = 0f;
        src.volume        = 0.25f;
        src.priority      = 64;

        // Agregar AmbienteSonido con clip ya asignado
        AmbienteSonido script   = ambienteObj.AddComponent<AmbienteSonido>();
        script.clipAmbiente     = clip;
        script.volumenObjetivo  = 0.25f;
        script.tiempoFadeIn     = 2f;

        // Verificar que haya un AudioListener en la escena
        AudioListener listener = Object.FindFirstObjectByType<AudioListener>();
        if (listener == null)
        {
            // Buscar la cámara y agregarle el AudioListener
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.gameObject.AddComponent<AudioListener>();
                Debug.Log("[AmbienteSetup] AudioListener añadido a la cámara principal.");
            }
            else
            {
                // Crearlo en el propio objeto de ambiente como fallback
                ambienteObj.AddComponent<AudioListener>();
                Debug.LogWarning("[AmbienteSetup] No se encontró cámara principal. AudioListener añadido al objeto SonidoAmbiente.");
            }
        }
        else
        {
            Debug.Log("[AmbienteSetup] AudioListener ya existe en: " + listener.gameObject.name);
        }

        // Guardar
        EditorSceneManager.MarkSceneDirty(escena);
        bool ok = EditorSceneManager.SaveScene(escena, scenePath);

        if (ok)
        {
            EditorUtility.DisplayDialog("Listo",
                "Sonido ambiente agregado a 'nivel_1_ypf':\n\n" +
                "• Clip: " + clip.name + "\n" +
                "• Volumen: 25%\n" +
                "• Fade in: 2 segundos\n" +
                "• Loop: activado\n\n" +
                "Presioná Play para escucharlo.", "OK");
        }
        else
        {
            Debug.LogError("[AmbienteSetup] Error al guardar la escena.");
        }

        if (!string.IsNullOrEmpty(escenaAnterior) && escenaAnterior != scenePath)
            EditorSceneManager.OpenScene(escenaAnterior);
    }
}
#endif
