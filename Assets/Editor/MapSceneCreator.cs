#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[InitializeOnLoad]
public static class MapSceneCreator
{
    static MapSceneCreator()
    {
        // Automatically check and generate the map scene if it is missing
        EditorApplication.delayCall += () =>
        {
            if (!System.IO.File.Exists("Assets/Scenes/mapa.unity"))
            {
                CrearEscenaMapaSilencioso();
            }
        };
    }

    [MenuItem("Tools/Generar Escena Mapa")]
    public static void CrearEscenaMapaMenu()
    {
        CrearEscenaMapa(false);
    }

    private static void CrearEscenaMapaSilencioso()
    {
        CrearEscenaMapa(true);
    }

    private static void CrearEscenaMapa(bool silencioso)
    {
        string scenePath = "Assets/Scenes/mapa.unity";

        // Create directory if not exists
        if (!System.IO.Directory.Exists("Assets/Scenes"))
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");
        }

        // Save current open scenes
        string previousScenePath = EditorSceneManager.GetActiveScene().path;
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

        // Load mapa.jpeg
        string spritePath = "Assets/Sprites/mapa.jpeg";
        Sprite mapaSprite = LoadAndConfigureSprite(spritePath);
        if (mapaSprite != null)
        {
            bgImage.sprite = mapaSprite;
        }
        else
        {
            Debug.LogWarning($"No se pudo cargar la imagen {spritePath} como Sprite.");
        }

        // 4. Create Continuar Button
        GameObject continuarBtn = new GameObject("BotonContinuar");
        continuarBtn.transform.SetParent(canvasObj.transform, false);

        RectTransform btnRect = continuarBtn.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(0, 100);
        btnRect.sizeDelta = new Vector2(240, 60);

        Image btnImg = continuarBtn.AddComponent<Image>();
        btnImg.color = new Color(0.12f, 0.12f, 0.12f, 0.9f); // Dark post-apocalyptic grey

        Outline btnOutline = continuarBtn.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.85f, 0.45f, 0.15f, 0.8f); // Rusty orange outline
        btnOutline.effectDistance = new Vector2(2, 2);

        Button btnComponent = continuarBtn.AddComponent<Button>();
        ColorBlock cb = btnComponent.colors;
        cb.normalColor = new Color(0.12f, 0.12f, 0.12f, 0.9f);
        cb.highlightedColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        cb.pressedColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);
        btnComponent.colors = cb;

        // Create Text inside Button
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(continuarBtn.transform, false);
        
        var txtComponent = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        txtComponent.text = "Continuar";
        txtComponent.fontSize = 26;
        txtComponent.fontStyle = TMPro.FontStyles.Bold;
        txtComponent.alignment = TMPro.TextAlignmentOptions.Center;
        txtComponent.color = new Color(0.95f, 0.9f, 0.85f, 1f); // Off-white
        
        Outline txtOutline = textObj.AddComponent<Outline>();
        txtOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        txtOutline.effectDistance = new Vector2(1, -1);

        RectTransform txtRect = textObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        // Try applying standard font to text component
        TMPro.TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Capture It SDF");
        if (font == null) font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Oswald Bold SDF");
        if (font != null)
        {
            txtComponent.font = font;
        }

        // Ensure an EventSystem is present in the scene so UI components can receive input events
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("MapSceneCreator: EventSystem created.");
        }

        // 5. Add MapSceneManager component and hook up button
        GameObject managerObj = new GameObject("MapSceneManager");
        MapSceneManager manager = managerObj.AddComponent<MapSceneManager>();
        manager.continuarButton = btnComponent;

        // 6. Save the scene
        bool saveSuccess = EditorSceneManager.SaveScene(newScene, scenePath);
        if (saveSuccess)
        {
            Debug.Log($"Escena 'mapa' creada y guardada con éxito en {scenePath}");

            // Add map scene to Editor Build Settings
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
                Debug.Log("Escena 'mapa' añadida a Build Settings.");
            }

            if (!silencioso)
            {
                EditorUtility.DisplayDialog("Éxito", "La escena 'mapa' se ha generado correctamente con el fondo 'mapa.jpeg' y el botón 'Continuar'.", "OK");
            }
        }
        else
        {
            Debug.LogError("Error al intentar guardar la escena 'mapa'.");
        }

        // Reload the previous scene if in silent generation mode, or keep it open if run from menu
        if (silencioso && !string.IsNullOrEmpty(previousScenePath) && previousScenePath != scenePath)
        {
            EditorSceneManager.OpenScene(previousScenePath);
        }
        else
        {
            EditorSceneManager.OpenScene(scenePath);
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

        Sprite singleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (singleSprite != null)
        {
            return singleSprite;
        }

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
}
#endif
