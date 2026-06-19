using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class NPCInteraction : MonoBehaviour
{
    public GameObject interactionText;
    public string sceneToLoad = "escenatrueque";

    private bool playerNear = false;
    private GameObject customPromptCanvas;
    private GameObject promptPanel;

    // Track last interacted NPC name
    public static string lastInteractedNPC = "";

    // Track if the deal with each NPC has been completed
    public static bool tratoTerminado = false;
    public static bool tratoVagabundoTerminado = false;
    public static bool tratoNpc3Terminado = false;

    // Reset all static flags on every game start (handles editor Domain Reload disabled + builds)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        lastInteractedNPC = "";
        tratoTerminado = false;
        tratoVagabundoTerminado = false;
        tratoNpc3Terminado = false;
    }

    void Start()
    {
        // Deactivate based on which NPC deal was finished
        if (gameObject.name == "combativa_0" && tratoTerminado)
        {
            GameObject bidon = GameObject.Find("bidon");
            if (bidon != null)
            {
                bidon.SetActive(false);
            }
            gameObject.SetActive(false);
            return;
        }
        else if (gameObject.name == "vagabundo" && tratoVagabundoTerminado)
        {
            GameObject brujula = GameObject.Find("brujula");
            if (brujula != null)
            {
                brujula.SetActive(false);
            }
            gameObject.SetActive(false);
            return;
        }
        else if (gameObject.name == "npc3" && tratoNpc3Terminado)
        {
            GameObject objeto9 = GameObject.Find("objeto9");
            if (objeto9 != null)
            {
                objeto9.SetActive(false);
            }
            gameObject.SetActive(false);
            return;
        }

        // Hide original canvas text if assigned
        if (interactionText != null)
        {
            interactionText.SetActive(false);
        }

        // Programmatically build the gorgeous post-apocalyptic interaction prompt
        BuildInteractionPrompt();
    }

    void Update()
    {
        if (playerNear && Input.GetKeyDown(KeyCode.F))
        {
            lastInteractedNPC = gameObject.name;
            if (gameObject.name == "combativa_0")
            {
                tratoTerminado = true;
            }
            else if (gameObject.name == "vagabundo")
            {
                tratoVagabundoTerminado = true;
            }
            else if (gameObject.name == "npc3")
            {
                tratoNpc3Terminado = true;
            }

            // Guardar posición del auto antes de cambiar de escena
            Movimiento car = FindAnyObjectByType<Movimiento>();
            if (car != null)
            {
                Movimiento.GuardarPosicion(car.transform.position);
            }
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = true;

            if (promptPanel != null)
            {
                promptPanel.SetActive(true);
            }

            if (interactionText != null)
            {
                interactionText.SetActive(false);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = false;

            if (promptPanel != null)
            {
                promptPanel.SetActive(false);
            }

            if (interactionText != null)
            {
                interactionText.SetActive(false);
            }
        }
    }

    private void BuildInteractionPrompt()
    {
        Vector3 parentScale = transform.lossyScale;
        float parentScaleX = parentScale.x != 0 ? parentScale.x : 1f;
        float parentScaleY = parentScale.y != 0 ? parentScale.y : 1f;

        // The baseline scale factor is based on combativa_0's scale (approx 0.25f)
        float baselineScale = 0.25098053f;

        // Calculate Y position above sprite bounds
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float localY = 3.3f;
        if (sr != null && sr.sprite != null)
        {
            float halfSpriteHeight = sr.sprite.bounds.size.y * 0.5f;
            float worldOffset = 2.3f * baselineScale;
            localY = halfSpriteHeight + (worldOffset / Mathf.Abs(parentScaleY));
        }

        // Create Canvas in World Space
        customPromptCanvas = new GameObject("CustomInteractionPromptCanvas");
        customPromptCanvas.transform.SetParent(this.transform, false);
        customPromptCanvas.transform.localPosition = new Vector3(0, localY, 0);

        // Scale it so that the prompt has the exact same world scale as it does on combativa_0.
        // We divide by parentScale (including sign) so the world scale is always positive,
        // which prevents the text from being mirrored if the NPC is flipped.
        float targetLocalScaleX = (0.04f * baselineScale) / parentScaleX;
        float targetLocalScaleY = (0.04f * baselineScale) / parentScaleY;
        customPromptCanvas.transform.localScale = new Vector3(targetLocalScaleX, targetLocalScaleY, 1f);

        Canvas canvas = customPromptCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50; // Render above other sprites

        // 1. Create Balloon Container (holds circle + triangle + !)
        GameObject balloonObj = new GameObject("Balloon");
        balloonObj.transform.SetParent(customPromptCanvas.transform, false);
        
        RectTransform balloonRect = balloonObj.AddComponent<RectTransform>();
        balloonRect.sizeDelta = new Vector2(60, 60);
        balloonRect.anchoredPosition = Vector2.zero;

        // Draw filled yellow circle with black outline
        Texture2D circleTex = new Texture2D(128, 128);
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float dx = x - 64f;
                float dy = y - 64f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= 62f && dist > 54f)
                {
                    circleTex.SetPixel(x, y, new Color(0.06f, 0.05f, 0.05f, 1f)); // Dark outline
                }
                else if (dist <= 54f)
                {
                    circleTex.SetPixel(x, y, new Color(0.92f, 0.78f, 0.18f, 1f)); // Golden warning yellow
                }
                else
                {
                    circleTex.SetPixel(x, y, Color.clear);
                }
            }
        }
        circleTex.Apply();
        Sprite circleSprite = Sprite.Create(circleTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));

        Image circleImg = balloonObj.AddComponent<Image>();
        circleImg.sprite = circleSprite;

        // Add downward triangle (stem)
        GameObject triObj = new GameObject("Stem");
        triObj.transform.SetParent(balloonObj.transform, false);
        RectTransform triRect = triObj.AddComponent<RectTransform>();
        triRect.sizeDelta = new Vector2(24, 24);
        triRect.anchoredPosition = new Vector2(0, -38);

        Texture2D triTex = new Texture2D(64, 64);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float limit = (64 - y) * 0.5f;
                if (x >= 32 - limit && x <= 32 + limit)
                {
                    if (x < 32 - limit + 4 || x > 32 + limit - 4 || y < 4)
                    {
                        triTex.SetPixel(x, y, new Color(0.06f, 0.05f, 0.05f, 1f)); // Dark outline
                    }
                    else
                    {
                        triTex.SetPixel(x, y, new Color(0.92f, 0.78f, 0.18f, 1f)); // Yellow fill
                    }
                }
                else
                {
                    triTex.SetPixel(x, y, Color.clear);
                }
            }
        }
        triTex.Apply();
        Sprite triSprite = Sprite.Create(triTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));

        Image triImg = triObj.AddComponent<Image>();
        triImg.sprite = triSprite;

        // Add Exclamation Mark Text inside circle
        GameObject exclamationObj = new GameObject("ExclamationText");
        exclamationObj.transform.SetParent(balloonObj.transform, false);
        TextMeshProUGUI exclText = exclamationObj.AddComponent<TextMeshProUGUI>();
        exclText.text = "!";
        exclText.fontStyle = FontStyles.Bold;
        exclText.fontSize = 42;
        exclText.alignment = TextAlignmentOptions.Center;
        exclText.color = new Color(0.08f, 0.08f, 0.08f, 1f); // Dark fill for !

        RectTransform exclRect = exclamationObj.GetComponent<RectTransform>();
        exclRect.anchorMin = Vector2.zero;
        exclRect.anchorMax = Vector2.one;
        exclRect.offsetMin = Vector2.zero;
        exclRect.offsetMax = Vector2.zero;

        // 2. Create Options Panel (floats to the right when player is near, except for npc3 which floats to the left)
        promptPanel = new GameObject("PromptPanel");
        promptPanel.transform.SetParent(customPromptCanvas.transform, false);
        
        RectTransform panelRect = promptPanel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(180, 70); // Increased height to fit NPC name header
        if (gameObject.name == "npc3")
        {
            panelRect.pivot = new Vector2(1f, 0.5f);
            panelRect.anchoredPosition = new Vector2(-45, 0); // Offset to the left to avoid screen cut-off
        }
        else
        {
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(45, 0); // Offset to the right
        }

        Image panelImg = promptPanel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.08f, 0.9f); // Dark translucent background

        Outline panelOutline = promptPanel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.5f, 0.25f, 0.1f, 0.8f); // Saddle brown/rust border
        panelOutline.effectDistance = new Vector2(1.5f, 1.5f);

        // Add NPC Name Header Text at the top of the panel
        GameObject nameObj = new GameObject("NPCNameHeader");
        nameObj.transform.SetParent(promptPanel.transform, false);
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        
        // Helper inline map for NPC names
        string npcDisplayName = gameObject.name;
        if (gameObject.name == "combativa_0") npcDisplayName = "ROXANA";
        else if (gameObject.name == "vagabundo") npcDisplayName = "JACINTO";
        else if (gameObject.name == "npc3") npcDisplayName = "HÉCTOR";
        else npcDisplayName = gameObject.name.ToUpper();

        nameText.text = npcDisplayName;
        nameText.fontStyle = FontStyles.Bold;
        nameText.fontSize = 15;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = new Color(1f, 0.55f, 0f, 1f); // Orange / Amber color for name header

        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.5f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 0.5f);
        nameRect.offsetMin = new Vector2(5, 0);
        nameRect.offsetMax = new Vector2(-5, -5);

        // Add text "INTERACTUAR [F]" at the bottom
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(promptPanel.transform, false);
        TextMeshProUGUI promptText = textObj.AddComponent<TextMeshProUGUI>();
        promptText.text = "INTERACTUAR <color=#FFD700>[F]</color>";
        promptText.fontStyle = FontStyles.Bold;
        promptText.fontSize = 13; // Slightly smaller to ensure fit
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = new Color(0.9f, 0.9f, 0.85f, 1f);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = new Vector2(5, 5);
        textRect.offsetMax = new Vector2(-5, 0);

        // Apply custom post-apocalyptic fonts
        ApplyFont(nameText, true);
        ApplyFont(exclText, false);
        ApplyFont(promptText, true);

        // Start prompt panel hidden until player is near
        promptPanel.SetActive(false);
    }

    private void ApplyFont(TextMeshProUGUI tmpText, bool applyEffects)
    {
        if (tmpText == null) return;
        TMP_FontAsset stencilFont = Resources.Load<TMP_FontAsset>("Capture It SDF");
        if (stencilFont == null) stencilFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Capture It SDF");
        if (stencilFont == null) stencilFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");
        if (stencilFont == null) stencilFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Anton SDF");
        
        if (stencilFont != null)
        {
            tmpText.font = stencilFont;
        }

        if (applyEffects)
        {
            Material fontMat = tmpText.fontMaterial;
            if (fontMat != null)
            {
                fontMat.SetFloat("_FaceDilate", -0.06f);
                fontMat.SetFloat("_OutlineSoftness", 0.2f);
                fontMat.SetFloat("_OutlineWidth", 0.18f);
                fontMat.SetColor("_OutlineColor", new Color(0.05f, 0.05f, 0.05f, 0.95f));
            }
        }
    }
}