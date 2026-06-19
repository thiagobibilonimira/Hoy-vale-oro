using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VideoSceneManager : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string nextSceneName = "mapa";
    
    private bool isPrepared = false;
    
    // Fade Transition elements
    private Image fadeOverlay;
    private float fadeDuration = 1.2f;
    private bool isFadingIn = true;
    private float fadeTimer = 0f;

    void Start()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        // Programmatically build the Canvas and fade overlay panel
        CreateFadeOverlay();

        if (videoPlayer != null)
        {
            // If the VideoPlayer already has a VideoClip assigned, use it (essential for standalone builds)
            if (videoPlayer.clip != null)
            {
                videoPlayer.source = VideoSource.VideoClip;
                Debug.Log("Reproduciendo video desde VideoClip asignado: " + videoPlayer.clip.name);
            }
            else
            {
                // Fallback to URL path resolution if no clip is assigned
                string[] possiblePaths = new string[]
                {
                    System.IO.Path.Combine(Application.dataPath, "Sprites/videofade.mp4"),
                    System.IO.Path.Combine(Application.dataPath, "Sprites/videoface.mp4"),
                    System.IO.Path.Combine(Application.dataPath, "Sprites/introprevia.mp4"),
                    System.IO.Path.Combine(Application.dataPath, "Sprites/antesdeljuego.mp4")
                };

                string resolvedPath = "";
                foreach (var path in possiblePaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        resolvedPath = path;
                        break;
                    }
                }

                videoPlayer.source = VideoSource.Url;
                if (!string.IsNullOrEmpty(resolvedPath))
                {
                    videoPlayer.url = resolvedPath;
                    Debug.Log("Video cargado desde URL: " + resolvedPath);
                }
                else
                {
                    // Fallback to absolute or relative path
                    videoPlayer.url = System.IO.Path.Combine(Application.dataPath, "Sprites/videoface.mp4");
                    Debug.LogWarning("No se encontró videoface.mp4 en Assets/Sprites/. Usando fallback.");
                }
            }

            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.errorReceived += OnVideoError;
            
            // Start preparing
            videoPlayer.Prepare();
        }
        else
        {
            Debug.LogWarning("VideoPlayer no asignado en VideoSceneManager. Cargando escena directamente.");
            LoadNextScene();
        }
    }

    void Update()
    {
        // Handle Fade In
        if (isFadingIn)
        {
            fadeTimer += Time.deltaTime;
            float t = fadeTimer / fadeDuration;
            if (fadeOverlay != null)
            {
                fadeOverlay.color = new Color(0, 0, 0, 1f - Mathf.Clamp01(t));
            }
            if (t >= 1f)
            {
                isFadingIn = false;
                fadeTimer = 0f;
                if (fadeOverlay != null)
                {
                    fadeOverlay.gameObject.SetActive(false);
                }
            }
        }

        // Handle Skip Input (Space, Enter, Escape or Left Click)
        if (isPrepared && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0)))
        {
            LoadNextScene();
        }
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        isPrepared = true;
        source.Play();
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        LoadNextScene();
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError("Error en VideoPlayer: " + message);
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }

    private void CreateFadeOverlay()
    {
        // Create Canvas for the transition
        GameObject canvasObj = GameObject.Find("VideoFadeCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("VideoFadeCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Create overlay Image
        GameObject overlayObj = new GameObject("FadeImage");
        overlayObj.transform.SetParent(canvasObj.transform, false);
        
        fadeOverlay = overlayObj.AddComponent<Image>();
        fadeOverlay.color = Color.black; // Starts fully black for Fade-In

        RectTransform rect = overlayObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
