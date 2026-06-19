using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MapSceneManager : MonoBehaviour
{
    [Header("UI Reference")]
    public Button continuarButton;

    void Start()
    {
        // 1. Ensure EventSystem is present in the scene so clicks are registered
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            Debug.Log("MapSceneManager: EventSystem was missing! Creating one...");
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // 2. Fallback to find the Continuar Button dynamically if the serialized reference is null
        if (continuarButton == null)
        {
            continuarButton = GetComponentInChildren<Button>();
            if (continuarButton == null)
            {
                GameObject btnObj = GameObject.Find("BotonContinuar");
                if (btnObj != null)
                {
                    continuarButton = btnObj.GetComponent<Button>();
                }
            }
        }

        // 3. Register click handler
        if (continuarButton != null)
        {
            continuarButton.onClick.RemoveAllListeners();
            continuarButton.onClick.AddListener(OnContinuarClicked);
            Debug.Log("MapSceneManager: Button listener successfully registered.");
        }
        else
        {
            Debug.LogError("MapSceneManager: 'Continuar' Button could not be found!");
        }
    }

    public void OnContinuarClicked()
    {
        Debug.Log("MapSceneManager: Continuar button clicked. Transitioning to 'nivel_1_ypf'.");
        SceneManager.LoadScene("nivel_1_ypf");
    }

    void Update()
    {
        // Support keyboard shortcuts (Space / Enter) for quick progression
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnContinuarClicked();
        }
    }
}
