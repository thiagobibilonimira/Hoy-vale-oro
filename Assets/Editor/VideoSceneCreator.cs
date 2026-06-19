#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[InitializeOnLoad]
public static class VideoSceneCreator
{
    static VideoSceneCreator()
    {
        // Automatically check and generate the video scene if it is missing
        EditorApplication.delayCall += () =>
        {
            if (!System.IO.File.Exists("Assets/Scenes/videointro.unity"))
            {
                CrearEscenaVideoSilencioso();
            }
        };
    }

    [MenuItem("Tools/Generar Escena Video Intro")]
    public static void CrearEscenaVideoMenu()
    {
        CrearEscenaVideo(false);
    }

    private static void CrearEscenaVideoSilencioso()
    {
        CrearEscenaVideo(true);
    }

    private static void CrearEscenaVideo(bool silencioso)
    {
        string scenePath = "Assets/Scenes/videointro.unity";

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

        // 3. Create VideoPlayer GameObject
        GameObject videoObj = new GameObject("VideoPlayerObject");
        VideoPlayer videoPlayer = videoObj.AddComponent<VideoPlayer>();
        
        // Configure VideoPlayer
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        videoPlayer.targetCamera = mainCam;

        // Load and assign VideoClip to ensure it compiles into standalone builds
        string[] possibleVideoPaths = new string[]
        {
            "Assets/Sprites/videoface.mp4",
            "Assets/Sprites/introprevia.mp4",
            "Assets/Sprites/antesdeljuego.mp4"
        };
        VideoClip clip = null;
        foreach (string path in possibleVideoPaths)
        {
            if (System.IO.File.Exists(path))
            {
                clip = AssetDatabase.LoadAssetAtPath<VideoClip>(path);
                if (clip != null)
                {
                    break;
                }
            }
        }
        if (clip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = clip;
            Debug.Log($"Intro VideoClip assigned to VideoPlayer: {clip.name}");
        }
        else
        {
            Debug.LogWarning("No se encontró ningún clip de video de intro en Assets/Sprites.");
        }

        // Add VideoSceneManager and link VideoPlayer reference
        VideoSceneManager sceneManager = videoObj.AddComponent<VideoSceneManager>();
        sceneManager.videoPlayer = videoPlayer;

        // Save the newly generated scene
        EditorSceneManager.SaveScene(newScene, scenePath);
        Debug.Log("Escena de video creada con éxito en: " + scenePath);

        // 4. Add the scene to Build Settings
        List<EditorBuildSettingsScene> editorScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool exists = false;
        foreach (var s in editorScenes)
        {
            if (s.path == scenePath)
            {
                exists = true;
                break;
            }
        }
        if (!exists)
        {
            editorScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = editorScenes.ToArray();
            Debug.Log("Escena videointro añadida a Build Settings.");
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
}
#endif
