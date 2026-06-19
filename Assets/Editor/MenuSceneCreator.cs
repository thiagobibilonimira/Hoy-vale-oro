#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MenuSceneCreator
{
    static MenuSceneCreator()
    {
        // Auto-run if the scene doesn't exist yet
        EditorApplication.delayCall += () =>
        {
            if (!System.IO.File.Exists("Assets/Scenes/menu.unity"))
            {
                CrearEscenaMenuSilencioso();
            }
        };
    }

    [MenuItem("Tools/Generar Escena Menu")]
    public static void CrearEscenaMenuMenu()
    {
        CrearEscenaMenu(false);
    }

    private static void CrearEscenaMenuSilencioso()
    {
        CrearEscenaMenu(true);
    }

    private static void CrearEscenaMenu(bool silencioso)
    {
        // Save open scenes first to prevent dialog blocking
        EditorSceneManager.SaveOpenScenes();

        // 1. Create a new scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5f;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = Color.black;
        }

        // 2. Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // 3. Create Background Image
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = Color.white;
        
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Load portada.jpeg (or fallback to portada.jpg) sprite
        string spritePath = "Assets/Sprites/menu/portada.jpeg";
        if (!System.IO.File.Exists(spritePath))
        {
            spritePath = "Assets/Sprites/menu/portada.jpg";
        }
        Sprite portadaSprite = LoadAndConfigureSprite(spritePath);
        if (portadaSprite != null)
        {
            bgImage.sprite = portadaSprite;
        }
        else
        {
            Debug.LogWarning($"No se pudo cargar la imagen {spritePath} como Sprite.");
        }

        // 4. Create Iniciar Juego Button
        GameObject iniciarBtn = CrearBotonMenu(canvasObj, "BotonIniciarJuego", "Assets/Sprites/menu/iniciarjuego.png", new Vector2(0.182234f, 0.5f), new Vector2(0, 220), 0.75f);
        BotonMenuPrincipal scriptIniciar = iniciarBtn.AddComponent<BotonMenuPrincipal>();
        scriptIniciar.isIniciarJuego = true;

        // 5. Create Opciones Button
        GameObject opcionesBtn = CrearBotonMenu(canvasObj, "BotonOpciones", "Assets/Sprites/menu/opciones.png", new Vector2(0.185531f, 0.5f), new Vector2(0, 42), 0.75f);
        BotonMenuPrincipal scriptOpciones = opcionesBtn.AddComponent<BotonMenuPrincipal>();
        scriptOpciones.isIniciarJuego = false;

        // 7. Save the scene
        string sceneDir = "Assets/Scenes";
        System.IO.Directory.CreateDirectory(sceneDir);
        string scenePath = sceneDir + "/menu.unity";
        
        bool saveSuccess = EditorSceneManager.SaveScene(newScene, scenePath);
        if (saveSuccess)
        {
            Debug.Log($"Escena 'menu' creada y guardada con éxito en {scenePath}");

            // Add menu scene to Editor Build Settings if not already present
            var scenes = EditorBuildSettings.scenes;
            bool exists = false;
            foreach (var s in scenes)
            {
                if (s.path == scenePath)
                {
                    exists = true;
                    break;
                }
            }
            if (!exists)
            {
                var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
                System.Array.Copy(scenes, newScenes, scenes.Length);
                newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
                EditorBuildSettings.scenes = newScenes;
            }

            if (!silencioso)
            {
                EditorUtility.DisplayDialog("Éxito", "La escena 'menu' de 1920x1280 con portada.jpg y sus tres botones se ha generado correctamente.", "OK");
            }
        }
        else
        {
            Debug.LogError("Error al intentar guardar la escena 'menu'.");
        }
    }

    private static Sprite LoadAndConfigureSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }

        // Try loading as single sprite first
        Sprite singleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (singleSprite != null)
        {
            return singleSprite;
        }

        // If it's Multiple, load all sub-assets and return the largest one
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        Sprite bestSprite = null;
        float maxArea = 0f;

        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite)
            {
                float area = sprite.rect.width * sprite.rect.height;
                if (area > maxArea)
                {
                    maxArea = area;
                    bestSprite = sprite;
                }
            }
        }

        return bestSprite;
    }

    private static GameObject CrearBotonMenu(GameObject parent, string name, string spritePath, Vector2 anchor, Vector2 position, float scale)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent.transform, false);

        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = anchor;
        btnRect.anchorMax = anchor;
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = position;

        Image img = btnObj.AddComponent<Image>();
        img.color = Color.white;
        img.preserveAspect = true;

        Sprite sprite = LoadAndConfigureSprite(spritePath);
        if (sprite != null)
        {
            img.sprite = sprite;
            btnRect.sizeDelta = new Vector2(sprite.rect.width * scale, sprite.rect.height * scale);
        }
        else
        {
            Debug.LogWarning($"No se pudo cargar {spritePath} como Sprite.");
            btnRect.sizeDelta = new Vector2(250 * scale, 70 * scale); // Fallback size
        }

        btnObj.AddComponent<Button>();
        return btnObj;
    }
}
#endif
