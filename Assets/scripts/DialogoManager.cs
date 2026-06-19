using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class DialogoManager : MonoBehaviour
{
    public GameObject panelDialogo;
    public GameObject panelBotones;
    public GameObject panelResultado;
    public GameObject botonContinuar;

    public TextMeshProUGUI textoNPC;
    public TextMeshProUGUI textoResultado;

    private bool dialogoAbierto = false;

    private enum State
    {
        Inicio,            // NPC starts: "Tengo combustible..."
        JugadorHablo,      // Player box shown with response, NPC box still showing original question
        NpcContesto,       // NPC box updates to react, player box remains
        FinResultado       // Showing deal results in centered popup
    }

    private enum TipoRespuesta
    {
        Honesto,
        Chamuyero,
        Mentiroso
    }

    private State currentState = State.Inicio;
    private TipoRespuesta chosenType;
    private bool exito = false;
    private string npcReactionText; // Stores computed reaction of NPC

    // Original coordinates of the text boxes
    private Vector2 originalNpcPos;
    private Vector2 originalPlayerPos;

    // Custom UI Containers (post-apocalyptic design)
    private GameObject npcBoxObjeto;
    private TextMeshProUGUI npcBoxTexto;
    private TextMeshProUGUI npcNameHeader;

    private GameObject playerBoxObjeto;
    private TextMeshProUGUI playerBoxTexto;
    private TextMeshProUGUI playerNameHeader;

    private GameObject btnContinuarDialogoObjeto;

    // Center Popup Elements
    private GameObject popupObjeto;
    private TextMeshProUGUI popupTexto;

    private Sprite boxSprite;

    private List<EscenarioDialogo> escenarios = new List<EscenarioDialogo>();
    private EscenarioDialogo escenarioActual;
    private OpcionDialogo opcionSeleccionada;
    private int randomFuelAmount = 0;

    void Start()
    {
        // Swap background based on interacted NPC
        GameObject bgObj = GameObject.Find("trueque1_0");
        if (bgObj != null)
        {
            SpriteRenderer sr = bgObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (NPCInteraction.lastInteractedNPC == "npc3")
                {
                    Sprite trueque3Sprite = CargarSpriteDesdeResources("Sprites/trueque3");
                    if (trueque3Sprite != null)
                    {
                        Sprite originalSprite = sr.sprite;
                        if (originalSprite != null)
                        {
                            float origWidth = originalSprite.bounds.size.x;
                            float origHeight = originalSprite.bounds.size.y;
                            float newWidth = trueque3Sprite.bounds.size.x;
                            float newHeight = trueque3Sprite.bounds.size.y;

                            if (newWidth > 0f && newHeight > 0f)
                            {
                                Vector3 currentScale = sr.transform.localScale;
                                sr.transform.localScale = new Vector3(
                                    currentScale.x * (origWidth / newWidth),
                                    currentScale.y * (origHeight / newHeight),
                                    currentScale.z
                                );
                            }
                        }
                        sr.sprite = trueque3Sprite;
                    }
                    else
                    {
                        Debug.LogError("No se pudo cargar el sprite de trueque3 desde Resources");
                    }
                }
                else if (NPCInteraction.lastInteractedNPC == "vagabundo")
                {
                    Sprite trueque2Sprite = CargarSpriteDesdeResources("Sprites/trueque2(1)");
                    if (trueque2Sprite != null)
                    {
                        Sprite originalSprite = sr.sprite;
                        if (originalSprite != null)
                        {
                            float origWidth = originalSprite.bounds.size.x;
                            float origHeight = originalSprite.bounds.size.y;
                            float newWidth = trueque2Sprite.bounds.size.x;
                            float newHeight = trueque2Sprite.bounds.size.y;

                            if (newWidth > 0f && newHeight > 0f)
                            {
                                Vector3 currentScale = sr.transform.localScale;
                                sr.transform.localScale = new Vector3(
                                    currentScale.x * (origWidth / newWidth),
                                    currentScale.y * (origHeight / newHeight),
                                    currentScale.z
                                );
                            }
                        }
                        sr.sprite = trueque2Sprite;
                    }
                    else
                    {
                        Debug.LogError("No se pudo cargar el sprite de trueque2(1) desde Resources");
                    }
                }
                else
                {
                    Sprite trueque1Sprite = CargarSpriteDesdeResources("Sprites/trueque1");
                    if (trueque1Sprite != null)
                    {
                        sr.sprite = trueque1Sprite;
                    }
                }
            }
        }

        // Auto-assign references if they were lost during reload or edit
        if (panelDialogo == null || panelBotones == null || panelResultado == null || botonContinuar == null || textoNPC == null || textoResultado == null)
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
                {
                    if (panelDialogo == null && t.name == "paneldialogo") panelDialogo = t.gameObject;
                    if (panelBotones == null && t.name == "panelbotones") panelBotones = t.gameObject;
                    if (panelResultado == null && t.name == "panelresultado") panelResultado = t.gameObject;
                    if (botonContinuar == null && t.name == "botoncontinuar") botonContinuar = t.gameObject;
                    if (textoNPC == null && t.name == "textonpc") textoNPC = t.GetComponent<TextMeshProUGUI>();
                    if (textoResultado == null && t.name == "textoresultado") textoResultado = t.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        // Safety fallback to prevent crashes if something remains unassigned
        if (panelDialogo == null || panelResultado == null || textoNPC == null || textoResultado == null)
        {
            Debug.LogError("Error: Algunos componentes del DialogoManager no pudieron ser asignados.");
            return;
        }

        // Configure CanvasScaler to scale with screen size (reference resolution 1920x1080)
        Canvas canvasObj = panelDialogo.GetComponentInParent<Canvas>();
        if (canvasObj != null)
        {
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasObj.gameObject.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        PopularEscenarios();
        if (escenarios.Count > 0)
        {
            if (NPCInteraction.lastInteractedNPC == "vagabundo")
            {
                escenarioActual = null;
                foreach (var e in escenarios)
                {
                    if (e.npcNombre == "Jacinto")
                    {
                        escenarioActual = e;
                        break;
                    }
                }
                if (escenarioActual == null) escenarioActual = escenarios[escenarios.Count - 1];
            }
            else if (NPCInteraction.lastInteractedNPC == "combativa_0")
            {
                escenarioActual = null;
                foreach (var e in escenarios)
                {
                    if (e.npcNombre == "Roxana")
                    {
                        escenarioActual = e;
                        break;
                    }
                }
                if (escenarioActual == null) escenarioActual = escenarios[0];
            }
            else if (NPCInteraction.lastInteractedNPC == "npc3")
            {
                escenarioActual = null;
                foreach (var e in escenarios)
                {
                    if (e.npcNombre == "Héctor")
                    {
                        escenarioActual = e;
                        break;
                    }
                }
                if (escenarioActual == null) escenarioActual = escenarios[escenarios.Count - 1];
            }
            else
            {
                List<EscenarioDialogo> randomPool = new List<EscenarioDialogo>();
                foreach (var e in escenarios)
                {
                    if (e.npcNombre != "Jacinto" && e.npcNombre != "Roxana" && e.npcNombre != "Héctor")
                    {
                        randomPool.Add(e);
                    }
                }
                if (randomPool.Count > 0)
                {
                    escenarioActual = randomPool[Random.Range(0, randomPool.Count)];
                }
                else
                {
                    escenarioActual = escenarios[Random.Range(0, escenarios.Count)];
                }
            }
        }
        else
        {
            escenarioActual = new EscenarioDialogo();
        }

        if (escenarioActual != null && 
            (escenarioActual.npcNombre == "Roxana" || 
             escenarioActual.npcNombre == "Gervasio" || 
             escenarioActual.npcNombre == "Beto" || 
             escenarioActual.npcNombre == "Flavia"))
        {
            randomFuelAmount = Random.Range(5, 21); // 5 to 20 inclusive
        }

        // Temporarily activate panels to force layout calculations and update canvases
        bool dialogWasActive = panelDialogo.activeSelf;
        bool resultWasActive = panelResultado.activeSelf;
        
        panelDialogo.SetActive(true);
        panelResultado.SetActive(true);
        Canvas.ForceUpdateCanvases();

        // 1. Capture design-time anchored positions of the original text components
        if (textoNPC != null)
        {
            originalNpcPos = textoNPC.GetComponent<RectTransform>().anchoredPosition;
        }
        else
        {
            originalNpcPos = new Vector2(431, 416);
        }

        if (textoResultado != null)
        {
            originalPlayerPos = textoResultado.GetComponent<RectTransform>().anchoredPosition;
        }
        else
        {
            originalPlayerPos = new Vector2(-665, 24);
        }

        // 2. Find and store the original background box sprite, then deactivate the old background boxes
        boxSprite = null;
        Transform oldNpcBoxTrans = panelDialogo != null ? panelDialogo.transform.Find("cuadrodetexto_0") : null;
        if (oldNpcBoxTrans != null)
        {
            SpriteRenderer sr = oldNpcBoxTrans.GetComponent<SpriteRenderer>();
            if (sr != null) boxSprite = sr.sprite;
            oldNpcBoxTrans.gameObject.SetActive(false);
        }
        else
        {
            GameObject go = GameObject.Find("cuadrodetexto_0");
            if (go != null)
            {
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) boxSprite = sr.sprite;
                go.SetActive(false);
            }
        }

        Transform oldPlayerBoxTrans = panelResultado != null ? panelResultado.transform.Find("cuadrodetexto_0 (1)") : null;
        if (oldPlayerBoxTrans != null)
        {
            if (boxSprite == null)
            {
                SpriteRenderer sr = oldPlayerBoxTrans.GetComponent<SpriteRenderer>();
                if (sr != null) boxSprite = sr.sprite;
            }
            oldPlayerBoxTrans.gameObject.SetActive(false);
        }
        else
        {
            GameObject go = GameObject.Find("cuadrodetexto_0 (1)");
            if (go != null)
            {
                if (boxSprite == null)
                {
                    SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                    if (sr != null) boxSprite = sr.sprite;
                }
                go.SetActive(false);
            }
        }

        // Fallback to load from Resources if not found in the scene
        if (boxSprite == null)
        {
            boxSprite = Resources.Load<Sprite>("Sprites/cuadrodetexto");
        }

        // Restore original active states of the panels
        panelDialogo.SetActive(dialogWasActive);
        panelResultado.SetActive(resultWasActive);

        // Hide original text fields (they will be reactivated and reparented inside the new boxes)
        textoNPC.gameObject.SetActive(false);
        textoResultado.gameObject.SetActive(false);
        panelResultado.SetActive(false);

        // Create new custom containers that match the post-apocalyptic style
        CrearContenedoresDialogo();
        CrearPopupTrato();

        // Adjust response buttons: keep text and prevent stretching
        if (panelBotones != null)
        {
            foreach (Transform button in panelBotones.transform)
            {
                // Ensure Text (TMP) child is active so the label is visible
                Transform textTmp = button.Find("Text (TMP)");
                if (textTmp != null)
                {
                    textTmp.gameObject.SetActive(true);
                    TextMeshProUGUI tmpText = textTmp.GetComponent<TextMeshProUGUI>();
                    if (tmpText != null)
                    {
                        // White color with 75% opacity, bold, uppercase
                        tmpText.color = new Color(1f, 1f, 1f, 0.75f);
                        tmpText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
                        tmpText.text = tmpText.text.ToUpper();

                        // Make it a bit larger
                        tmpText.fontSize = 21f;

                        // Load Capture It SDF if available, fallback to Oswald Bold SDF or Anton SDF
                        TMP_FontAsset stencilFont = Resources.Load<TMP_FontAsset>("Capture It SDF");
                        if (stencilFont == null) stencilFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Capture It SDF");
                        if (stencilFont == null) stencilFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");
                        if (stencilFont == null) stencilFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Anton SDF");
                        
                        if (stencilFont != null)
                        {
                            tmpText.font = stencilFont;
                        }

                        // Apply weathered/eroded effects directly to the material
                        Material fontMat = tmpText.fontMaterial;
                        if (fontMat != null)
                        {
                            fontMat.SetFloat("_FaceDilate", -0.06f); // Slight erosion/wear
                            fontMat.SetFloat("_OutlineSoftness", 0.2f); // Faded outline edges
                            fontMat.SetFloat("_OutlineWidth", 0.18f); // Outline boundary
                            fontMat.SetColor("_OutlineColor", new Color(0.05f, 0.05f, 0.05f, 0.95f)); // Soot dark outline
                        }

                        // Add dark drop shadow outline for stencil paint look
                        Shadow shadow = textTmp.GetComponent<Shadow>();
                        if (shadow == null)
                        {
                            shadow = textTmp.gameObject.AddComponent<Shadow>();
                        }
                        shadow.effectColor = new Color(0.08f, 0.08f, 0.08f, 0.95f);
                        shadow.effectDistance = new Vector2(2f, -2f);
                    }

                    // Shift the text down a bit to match post-apocalyptic layouts
                    RectTransform textRect = textTmp.GetComponent<RectTransform>();
                    if (textRect != null)
                    {
                        textRect.anchorMin = Vector2.zero;
                        textRect.anchorMax = Vector2.one;
                        textRect.pivot = new Vector2(0.5f, 0.5f);
                        textRect.anchoredPosition = new Vector2(0f, -15f); // Lowered by 15 units
                    }
                }

                // Adjust size to match the sprite's ~1.55:1 aspect ratio
                RectTransform btnRect = button.GetComponent<RectTransform>();
                if (btnRect != null)
                {
                    float currentWidth = btnRect.sizeDelta.x;
                    float newHeight = currentWidth / 1.55f;
                    btnRect.sizeDelta = new Vector2(currentWidth, newHeight);
                }

                // Preserve aspect ratio to prevent stretching
                Image img = button.GetComponent<Image>();
                if (img != null)
                {
                    img.preserveAspect = true;
                }

                // Check if player has the items required for this button's trade type
                TipoRespuesta tipo = TipoRespuesta.Honesto;
                if (button.name.ToLower() == "botonhonesto") tipo = TipoRespuesta.Honesto;
                else if (button.name.ToLower() == "botonchamuyero") tipo = TipoRespuesta.Chamuyero;
                else if (button.name.ToLower() == "botonmentiroso") tipo = TipoRespuesta.Mentiroso;

                Button btnComp = button.GetComponent<Button>();
                if (btnComp != null)
                {
                    bool hasItems = TieneObjetosRequeridos(tipo);
                    btnComp.interactable = hasItems;

                    if (!hasItems)
                    {
                        Image btnImg = button.GetComponent<Image>();
                        if (btnImg != null)
                        {
                            btnImg.color = new Color(btnImg.color.r, btnImg.color.g, btnImg.color.b, 0.4f);
                        }

                        Transform missingTextTransform = button.Find("Text (TMP)");
                        if (missingTextTransform != null)
                        {
                            TextMeshProUGUI missingTextComp = missingTextTransform.GetComponent<TextMeshProUGUI>();
                            if (missingTextComp != null)
                            {
                                missingTextComp.text += "\n<color=red><size=11>(FALTAN OBJETOS)</size></color>";
                            }
                        }
                    }
                }
            }
        }

        AbrirDialogo();
    }

    private Vector2 GetCanvasPosFromWorldObject(Transform worldObj)
    {
        if (panelDialogo != null)
        {
            RectTransform parentRect = panelDialogo.GetComponent<RectTransform>();
            if (parentRect != null)
            {
                // Convert world position of the object directly into the local space of panelDialogo
                Vector3 worldPos = worldObj.position;
                Vector3 localPos = parentRect.InverseTransformPoint(worldPos);
                return (Vector2)localPos;
            }
        }
        return Vector2.zero;
    }

    public void AbrirDialogo()
    {
        if (dialogoAbierto)
            return;

        dialogoAbierto = true;
        currentState = State.Inicio;

        panelDialogo.SetActive(true);
        panelBotones.SetActive(true);
        panelResultado.SetActive(false);
        botonContinuar.SetActive(false);

        // Setup visibility for custom containers
        npcBoxObjeto.SetActive(true);
        playerBoxObjeto.SetActive(false);
        btnContinuarDialogoObjeto.SetActive(false);
        popupObjeto.SetActive(false);

        if (npcNameHeader != null)
        {
            string displayName = escenarioActual.npcNombre;
            if (displayName == "combativa_0") displayName = "Roxana";
            npcNameHeader.text = displayName.ToUpper();
        }

        npcBoxTexto.text = "\"" + escenarioActual.npcPregunta + "\"";
    }

    public void RespuestaHonesto()
    {
        SeleccionarRespuesta(TipoRespuesta.Honesto);
    }

    public void RespuestaChamuyero()
    {
        SeleccionarRespuesta(TipoRespuesta.Chamuyero);
    }

    public void RespuestaMentiroso()
    {
        SeleccionarRespuesta(TipoRespuesta.Mentiroso);
    }

    private void SeleccionarRespuesta(TipoRespuesta tipo)
    {
        chosenType = tipo;
        panelBotones.SetActive(false);
        
        // Hide NPC box, show player box and continue button
        npcBoxObjeto.SetActive(false);
        playerBoxObjeto.SetActive(true);
        btnContinuarDialogoObjeto.SetActive(true);

        // Position continue button below Player box
        RectTransform btnRect = btnContinuarDialogoObjeto.GetComponent<RectTransform>();
        if (btnRect != null)
        {
            btnRect.anchoredPosition = new Vector2(originalPlayerPos.x + 195f, originalPlayerPos.y - 320f);
        }
        
        currentState = State.JugadorHablo;

        // Determine success based on probabilities and choose a random dialogue option
        exito = false;
        opcionSeleccionada = null;

        switch (tipo)
        {
            case TipoRespuesta.Honesto:
                exito = Random.value < 0.8f;
                if (escenarioActual.honestoOptions.Count > 0)
                {
                    // Filter options to those whose required items the player actually has
                    var validOptions = new List<OpcionDialogo>();
                    foreach (var opt in escenarioActual.honestoOptions)
                    {
                        bool tieneItems = true;
                        if (opt.itemsNecesarios != null && opt.itemsNecesarios.Length > 0
                            && InventoryManager.Instance != null)
                        {
                            foreach (string item in opt.itemsNecesarios)
                            {
                                if (!InventoryManager.Instance.HasItem(item, 1))
                                {
                                    tieneItems = false;
                                    break;
                                }
                            }
                        }
                        if (tieneItems) validOptions.Add(opt);
                    }

                    if (validOptions.Count > 0)
                        opcionSeleccionada = validOptions[Random.Range(0, validOptions.Count)];
                    else
                        opcionSeleccionada = escenarioActual.honestoOptions[Random.Range(0, escenarioActual.honestoOptions.Count)];
                }
                break;

            case TipoRespuesta.Chamuyero:
                exito = Random.value < 0.5f;
                if (escenarioActual.chamuyeroOptions.Count > 0)
                {
                    int idx = Random.Range(0, escenarioActual.chamuyeroOptions.Count);
                    opcionSeleccionada = escenarioActual.chamuyeroOptions[idx];
                }
                break;

            case TipoRespuesta.Mentiroso:
                exito = Random.value < 0.3f;
                if (escenarioActual.mentirosoOptions.Count > 0)
                {
                    int idx = Random.Range(0, escenarioActual.mentirosoOptions.Count);
                    opcionSeleccionada = escenarioActual.mentirosoOptions[idx];
                }
                break;
        }

        if (opcionSeleccionada != null)
        {
            if (playerNameHeader != null)
            {
                playerNameHeader.text = "JUGADOR PRINCIPAL";
            }
            playerBoxTexto.text = "\"" + opcionSeleccionada.jugadorTexto + "\"";
            if (exito)
                npcReactionText = "\"" + opcionSeleccionada.npcExito + "\"";
            else
                npcReactionText = "\"" + opcionSeleccionada.npcFallo + "\"";
        }
        else
        {
            playerBoxTexto.text = "...";
            npcReactionText = "...";
        }
    }

    public void OnContinuarClicked()
    {
        if (currentState == State.JugadorHablo)
        {
            // Player box disappears, NPC box reappears with reaction
            playerBoxObjeto.SetActive(false);
            npcBoxObjeto.SetActive(true);
            npcBoxTexto.text = npcReactionText;

            // Position continue button below NPC box
            RectTransform btnRect = btnContinuarDialogoObjeto.GetComponent<RectTransform>();
            if (btnRect != null)
            {
                btnRect.anchoredPosition = new Vector2(originalNpcPos.x + 195f, originalNpcPos.y - 320f);
            }
            
            currentState = State.NpcContesto;
        }
        else if (currentState == State.NpcContesto)
        {
            // Transition to result summary popup
            CerrarDialogo();
        }
    }

    public void CerrarDialogo()
    {
        if (currentState == State.NpcContesto)
        {
            currentState = State.FinResultado;
            
            // Hide the active dialogue boxes
            npcBoxObjeto.SetActive(false);
            playerBoxObjeto.SetActive(false);
            btnContinuarDialogoObjeto.SetActive(false);

            // Configure and display the centered popup
            string header = exito ? "<color=#4CAF50>¡TRATO HECHO!</color>" : "<color=#F44336>¡TRATO NO HECHO!</color>";
            string details = "";

            if (exito)
            {
                ProcesarTruequeInventario();
                details = opcionSeleccionada != null ? opcionSeleccionada.detalleExito : "";
                if (randomFuelAmount > 0 && !string.IsNullOrEmpty(details))
                {
                    details = System.Text.RegularExpressions.Regex.Replace(details, @"Conseguiste \d+L de nafta", "Conseguiste " + randomFuelAmount + "L de nafta");
                }
            }
            else
            {
                details = opcionSeleccionada != null ? opcionSeleccionada.detalleFallo : "";
            }

            popupTexto.text = $"{header}\n\n{details}";
            popupObjeto.SetActive(true);
        }
    }

    void CrearContenedoresDialogo()
    {
        // 1. Create NPC dialogue box
        npcBoxObjeto = new GameObject("CustomNpcBox");
        npcBoxObjeto.transform.SetParent(panelDialogo.transform, false);

        RectTransform npcRect = npcBoxObjeto.AddComponent<RectTransform>();
        npcRect.anchorMin = new Vector2(0.5f, 0.5f);
        npcRect.anchorMax = new Vector2(0.5f, 0.5f);
        npcRect.pivot = new Vector2(0f, 1f); // TOP-LEFT PIVOT!
        npcRect.anchoredPosition = originalNpcPos + new Vector2(-30f, 35f); // Shift to align text precisely
        npcRect.sizeDelta = new Vector2(450, 330); // 450x330 matches the aspect ratio of cuadrodetexto

        Image npcImg = npcBoxObjeto.AddComponent<Image>();
        if (boxSprite != null)
        {
            npcImg.sprite = boxSprite;
            npcImg.type = Image.Type.Simple;
            npcImg.color = Color.white;
        }
        else
        {
            npcImg.color = new Color(0.09f, 0.08f, 0.08f, 0.98f);
        }

        // Create NPC name header (consistent title/header area)
        GameObject npcHeaderObj = new GameObject("NpcNameHeader");
        npcHeaderObj.transform.SetParent(npcBoxObjeto.transform, false);
        npcNameHeader = npcHeaderObj.AddComponent<TextMeshProUGUI>();
        npcNameHeader.fontStyle = FontStyles.Bold;
        npcNameHeader.fontSize = 20;
        npcNameHeader.alignment = TextAlignmentOptions.Center;
        npcNameHeader.color = new Color(1f, 0.55f, 0f, 1f); // Orange / Amber
        ApplyFont(npcNameHeader, true);

        RectTransform npcHeaderRect = npcHeaderObj.GetComponent<RectTransform>();
        npcHeaderRect.anchorMin = new Vector2(0f, 1f);
        npcHeaderRect.anchorMax = new Vector2(1f, 1f);
        npcHeaderRect.pivot = new Vector2(0.5f, 1f);
        npcHeaderRect.anchoredPosition = new Vector2(0, -32);
        npcHeaderRect.sizeDelta = new Vector2(-60, 30);

        // Reparent original NPC text component
        textoNPC.transform.SetParent(npcBoxObjeto.transform, false);
        textoNPC.gameObject.SetActive(true);
        npcBoxTexto = textoNPC;
        npcBoxTexto.alignment = TextAlignmentOptions.TopLeft;
        npcBoxTexto.enableWordWrapping = true;
        npcBoxTexto.enableAutoSizing = true;
        npcBoxTexto.fontSizeMin = 13f;
        npcBoxTexto.fontSizeMax = 20f;
        npcBoxTexto.margin = new Vector4(0, 0, 0, 0);
        npcBoxTexto.color = new Color(0.95f, 0.95f, 0.9f, 1f); // Weathered paper white/light bone
        ApplyFont(npcBoxTexto, true);

        RectTransform npcTxtRect = textoNPC.GetComponent<RectTransform>();
        npcTxtRect.anchorMin = Vector2.zero;
        npcTxtRect.anchorMax = Vector2.one;
        npcTxtRect.pivot = new Vector2(0.5f, 0.5f);
        npcTxtRect.offsetMin = new Vector2(30, 30); // Clear margins for the yellow border
        npcTxtRect.offsetMax = new Vector2(-30, -65); // Margins inside the box (clearing the header)

        // 2. Create Player dialogue box
        playerBoxObjeto = new GameObject("CustomPlayerBox");
        playerBoxObjeto.transform.SetParent(panelDialogo.transform, false);

        RectTransform playerRect = playerBoxObjeto.AddComponent<RectTransform>();
        playerRect.anchorMin = new Vector2(0.5f, 0.5f);
        playerRect.anchorMax = new Vector2(0.5f, 0.5f);
        playerRect.pivot = new Vector2(0f, 1f); // TOP-LEFT PIVOT!
        playerRect.anchoredPosition = originalPlayerPos + new Vector2(-30f, 35f); // Shift to align text precisely
        playerRect.sizeDelta = new Vector2(450, 330);

        Image playerImg = playerBoxObjeto.AddComponent<Image>();
        if (boxSprite != null)
        {
            playerImg.sprite = boxSprite;
            playerImg.type = Image.Type.Simple;
            playerImg.color = Color.white;
        }
        else
        {
            playerImg.color = new Color(0.09f, 0.08f, 0.08f, 0.98f);
        }

        // Create Player name header (consistent title/header area)
        GameObject playerHeaderObj = new GameObject("PlayerNameHeader");
        playerHeaderObj.transform.SetParent(playerBoxObjeto.transform, false);
        playerNameHeader = playerHeaderObj.AddComponent<TextMeshProUGUI>();
        playerNameHeader.fontStyle = FontStyles.Bold;
        playerNameHeader.fontSize = 20;
        playerNameHeader.alignment = TextAlignmentOptions.Center;
        playerNameHeader.color = new Color(1f, 0.55f, 0f, 1f); // Orange / Amber
        ApplyFont(playerNameHeader, true);

        RectTransform playerHeaderRect = playerHeaderObj.GetComponent<RectTransform>();
        playerHeaderRect.anchorMin = new Vector2(0f, 1f);
        playerHeaderRect.anchorMax = new Vector2(1f, 1f);
        playerHeaderRect.pivot = new Vector2(0.5f, 1f);
        playerHeaderRect.anchoredPosition = new Vector2(0, -32);
        playerHeaderRect.sizeDelta = new Vector2(-60, 30);

        // Reparent original Player/Result text component
        textoResultado.transform.SetParent(playerBoxObjeto.transform, false);
        textoResultado.gameObject.SetActive(true);
        playerBoxTexto = textoResultado;
        playerBoxTexto.alignment = TextAlignmentOptions.TopLeft;
        playerBoxTexto.enableWordWrapping = true;
        playerBoxTexto.enableAutoSizing = true;
        playerBoxTexto.fontSizeMin = 13f;
        playerBoxTexto.fontSizeMax = 20f;
        playerBoxTexto.margin = new Vector4(0, 0, 0, 0);
        playerBoxTexto.color = new Color(0.95f, 0.95f, 0.9f, 1f); // Weathered paper white/light bone
        ApplyFont(playerBoxTexto, true);

        RectTransform playerTxtRect = textoResultado.GetComponent<RectTransform>();
        playerTxtRect.anchorMin = Vector2.zero;
        playerTxtRect.anchorMax = Vector2.one;
        playerTxtRect.pivot = new Vector2(0.5f, 0.5f);
        playerTxtRect.offsetMin = new Vector2(30, 30); // Clear margins for the yellow border
        playerTxtRect.offsetMax = new Vector2(-30, -65); // Margins inside the box (clearing the header)

        // 3. Create Dialogue Continue Button
        btnContinuarDialogoObjeto = new GameObject("CustomBtnContinuarDialogo");
        btnContinuarDialogoObjeto.transform.SetParent(panelDialogo.transform, false);

        RectTransform btnRect = btnContinuarDialogoObjeto.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(originalPlayerPos.x + 195f, originalPlayerPos.y - 320f);
        btnRect.sizeDelta = new Vector2(180, 45);

        Image btnImg = btnContinuarDialogoObjeto.AddComponent<Image>();
        btnImg.color = new Color(0.25f, 0.32f, 0.2f, 1f); // Military Olive Green

        Outline btnOutline = btnContinuarDialogoObjeto.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.12f, 0.16f, 0.1f, 0.8f);
        btnOutline.effectDistance = new Vector2(1.5f, 1.5f);

        Button btn = btnContinuarDialogoObjeto.AddComponent<Button>();
        btn.onClick.AddListener(OnContinuarClicked);

        // Hover/press transitions (Military green transitions)
        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.25f, 0.32f, 0.2f, 1f);
        cb.highlightedColor = new Color(0.32f, 0.4f, 0.26f, 1f);
        cb.pressedColor = new Color(0.18f, 0.24f, 0.14f, 1f);
        btn.colors = cb;

        // Button Text
        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnContinuarDialogoObjeto.transform, false);
        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "CONTINUAR >>";
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.fontSize = 16;
        btnText.color = new Color(0.9f, 0.9f, 0.85f, 1f);
        ApplyFont(btnText, true);

        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;
    }

    private void ApplyFont(TextMeshProUGUI tmpText, bool applyEffects = false)
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
                fontMat.SetFloat("_FaceDilate", -0.06f); // Slight erosion/wear
                fontMat.SetFloat("_OutlineSoftness", 0.2f); // Faded outline edges
                fontMat.SetFloat("_OutlineWidth", 0.18f); // Outline boundary
                fontMat.SetColor("_OutlineColor", new Color(0.05f, 0.05f, 0.05f, 0.95f)); // Soot dark outline
            }
        }
    }

    void CrearPopupTrato()
    {
        // 1. Create Popup Background Panel
        popupObjeto = new GameObject("PopupTratoCentrado");
        popupObjeto.transform.SetParent(panelDialogo.transform, false);

        RectTransform rect = popupObjeto.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(500, 320);

        AplicarEstiloPostApocaliptico(popupObjeto);

        // 2. Create Title & Detail Text inside the popup
        GameObject textoObj = new GameObject("PopupTexto");
        textoObj.transform.SetParent(popupObjeto.transform, false);

        popupTexto = textoObj.AddComponent<TextMeshProUGUI>();
        popupTexto.alignment = TextAlignmentOptions.Center;
        popupTexto.fontSize = 22;
        popupTexto.color = new Color(0.9f, 0.88f, 0.85f, 1f);

        RectTransform textRect = textoObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(30, 95);  // Space for button
        textRect.offsetMax = new Vector2(-30, -35); // Space for top bar

        // 3. Create Continue Button inside the popup
        GameObject buttonObj = new GameObject("PopupBoton");
        buttonObj.transform.SetParent(popupObjeto.transform, false);

        RectTransform btnRect = buttonObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = new Vector2(0, 30);
        btnRect.sizeDelta = new Vector2(180, 45);

        Image btnImg = buttonObj.AddComponent<Image>();
        btnImg.color = new Color(0.25f, 0.32f, 0.2f, 1f); // Military green

        Outline btnOutline = buttonObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.12f, 0.16f, 0.1f, 0.8f);
        btnOutline.effectDistance = new Vector2(1.5f, 1.5f);

        Button btn = buttonObj.AddComponent<Button>();
        btn.onClick.AddListener(CerrarPopupYContinuar);

        // Button hover/press states
        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.25f, 0.32f, 0.2f, 1f);
        cb.highlightedColor = new Color(0.32f, 0.4f, 0.26f, 1f);
        cb.pressedColor = new Color(0.18f, 0.24f, 0.14f, 1f);
        btn.colors = cb;

        // Button Text
        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(buttonObj.transform, false);

        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "CONFIRMAR >>";
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.fontSize = 16;
        btnText.color = new Color(0.9f, 0.9f, 0.85f, 1f);

        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;

        popupObjeto.SetActive(false);
    }

    private void AplicarEstiloPostApocaliptico(GameObject panel)
    {
        // 1. Dark rusted steel background
        Image img = panel.GetComponent<Image>();
        if (img == null)
        {
            img = panel.AddComponent<Image>();
        }
        img.color = new Color(0.09f, 0.08f, 0.08f, 0.98f); // Dark scrap iron

        // 2. Dual layer border (Rust orange + soot shadow)
        Outline outline = panel.GetComponent<Outline>();
        if (outline == null)
        {
            outline = panel.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0.5f, 0.25f, 0.1f, 0.8f); // Rust saddle brown
        outline.effectDistance = new Vector2(2, 2);

        Shadow shadow = panel.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = panel.AddComponent<Shadow>();
        }
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f); // dark soot shadow
        shadow.effectDistance = new Vector2(4, -4);

        // 3. Add yellow/black warning hazard stripes at the top
        AgregarCintaPeligro(panel);

        // 4. Add corner rivets (remaches)
        AgregarRemaches(panel);
    }

    private void AgregarCintaPeligro(GameObject panel)
    {
        // Create the yellow background strip
        GameObject yellowStrip = new GameObject("HazardStrip");
        yellowStrip.transform.SetParent(panel.transform, false);

        RectTransform rect = yellowStrip.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -6);
        rect.sizeDelta = new Vector2(-20, 10); // Slightly smaller than container width

        Image img = yellowStrip.AddComponent<Image>();
        img.color = new Color(0.85f, 0.65f, 0.1f, 0.9f); // Caution Yellow

        // Add the black slash stripes over the yellow strip
        GameObject stripesObj = new GameObject("StripesText");
        stripesObj.transform.SetParent(yellowStrip.transform, false);

        TextMeshProUGUI stripesText = stripesObj.AddComponent<TextMeshProUGUI>();
        stripesText.text = "<b>/ / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / /</b>";
        stripesText.alignment = TextAlignmentOptions.Center;
        stripesText.fontSize = 8;
        stripesText.color = new Color(0.05f, 0.05f, 0.05f, 0.85f); // Matte black slashes

        RectTransform stripesRect = stripesObj.GetComponent<RectTransform>();
        stripesRect.anchorMin = Vector2.zero;
        stripesRect.anchorMax = Vector2.one;
        stripesRect.offsetMin = Vector2.zero;
        stripesRect.offsetMax = Vector2.zero;
    }

    private void AgregarRemaches(GameObject panel)
    {
        // Add 4 corner rivets (remaches de metal oxidado)
        Vector2[] offsets = new Vector2[] {
            new Vector2(12, -18),    // Top-Left (shifted down to clear warning tape)
            new Vector2(-12, -18),   // Top-Right
            new Vector2(12, 12),     // Bottom-Left
            new Vector2(-12, 12)     // Bottom-Right
        };

        Vector2[] anchors = new Vector2[] {
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject rivet = new GameObject("Rivet_" + i);
            rivet.transform.SetParent(panel.transform, false);

            RectTransform r = rivet.AddComponent<RectTransform>();
            r.anchorMin = anchors[i];
            r.anchorMax = anchors[i];
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = offsets[i];
            r.sizeDelta = new Vector2(8, 8); // small circular rivet

            Image img = rivet.AddComponent<Image>();
            img.color = new Color(0.35f, 0.32f, 0.3f, 1f); // Dark metal rivet color

            Outline o = rivet.AddComponent<Outline>();
            o.effectColor = new Color(0.1f, 0.08f, 0.05f, 0.8f);
            o.effectDistance = new Vector2(1, -1);
        }
    }

    private void CerrarPopupYContinuar()
    {
        SceneManager.LoadScene("nivel_1_ypf");
    }    public class OpcionDialogo
    {
        public string jugadorTexto = "";
        public string npcExito = "";
        public string npcFallo = "";
        public string detalleExito = "";
        public string detalleFallo = "";
        // Items the player must have in inventory for this text to make sense.
        // Leave null/empty for options that don't mention specific items.
        public string[] itemsNecesarios = null;
    }

    private class EscenarioDialogo
    {
        public string npcNombre = "NPC";
        public string npcPregunta = "";
        
        public List<OpcionDialogo> honestoOptions = new List<OpcionDialogo>();
        public List<OpcionDialogo> chamuyeroOptions = new List<OpcionDialogo>();
        public List<OpcionDialogo> mentirosoOptions = new List<OpcionDialogo>();
    }

    private void PopularEscenarios()
    {
        escenarios.Clear();

        // 1. Escenario Roxana
        EscenarioDialogo e0 = new EscenarioDialogo();
        e0.npcNombre = "Roxana";
        e0.npcPregunta = "Tengo combustible, pero la situación está difícil. ¿Qué me das a cambio?";

        // Honesto
        e0.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "No voy a mentirte. Me estoy quedando sin nafta y necesito llegar al próximo pueblo. Tengo algunas provisiones para intercambiar.",
            npcExito = "Se agradece la sinceridad. Ya casi nadie habla de frente estos días. Mostrame lo que tenés.",
            npcFallo = "Entiendo tu situación, pero yo también necesito sobrevivir. No puedo ayudarte.",
            detalleExito = "Conseguiste 3L de nafta\nRoxana confía en vos",
            detalleFallo = "No obtuviste combustible"
            // No itemsNecesarios: text is generic, always valid
        });
        e0.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Siendo honesto, el tanque del Rastrojero está en las últimas. Tengo unas pitusas guardadas que te pueden servir.",
            npcExito = "Un trato justo y sin vueltas. Me viene bien para la guardia. Trato hecho.",
            npcFallo = "Un paquete de galletitas no me sirve para calentar mi refugio. Lo lamento.",
            detalleExito = "Conseguiste 3L de nafta\nBuen trato con Roxana",
            detalleFallo = "Roxana rechazó tus provisiones",
            itemsNecesarios = new[] { "pitusas" }
        });
        e0.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Te digo la verdad: si no consigo combustible me quedo varado acá. Ofrezco mis galletitas pitusas a cambio de nafta.",
            npcExito = "Prefiero la verdad a una promesa falsa. Tomá el bidón, viaja seguro.",
            npcFallo = "Necesito algo más valioso para soltar mi nafta. Suerte en el camino.",
            detalleExito = "Conseguiste 3L de nafta\nIntercambio honesto completado",
            detalleFallo = "No se concretó el trueque",
            itemsNecesarios = new[] { "pitusas" }
        });

        // Chamuyero
        e0.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = "Si me ayudás ahora, cuando llegue a destino te puedo poner en contacto con gente que comercia comida y medicamentos.",
            npcExito = "Bueno... no sé qué tan cierto será eso, pero suena como una oportunidad. Hagamos el intercambio.",
            npcFallo = "No me convencés. Escuché demasiadas promesas vacías.",
            detalleExito = "Conseguiste 2L de nafta\nRoxana queda con algunas dudas",
            detalleFallo = "Roxana desconfía de vos"
        });
        e0.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = "Soy socio de un grupo grande del sur. Si me das nafta, en el próximo viaje te traigo un cargamento completo de raciones militares.",
            npcExito = "Raciones militares... suena tentador. Te voy a creer por esta vez. Llevate esto.",
            npcFallo = "Ese verso de los socios ya me lo conozco. No hay trato.",
            detalleExito = "Conseguiste 2L de nafta\nRoxana espera tu convoy",
            detalleFallo = "Roxana no cree tus cuentos"
        });
        e0.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = "Este bidón de nafta me permitirá abrir una ruta comercial segura. Te aseguro que serás la primera en beneficiarte de los envíos.",
            npcExito = "Me gusta cómo pensás en el futuro. Hacemos el trueque.",
            npcFallo = "La ruta comercial es una fantasía hoy en día. No te doy nada.",
            detalleExito = "Conseguiste 2L de nafta\nTrato cerrado con promesas",
            detalleFallo = "Trueque rechazado"
        });

        // Mentiroso
        e0.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Más adelante encontré un camión abandonado lleno de suministros. Si me das combustible ahora, te digo dónde está.",
            npcExito = "¿Un camión lleno de suministros? Si eso es cierto, vale la pena intentarlo. Está bien, trato hecho.",
            npcFallo = "Eso suena inventado. No nací ayer.",
            detalleExito = "Conseguiste 5L de nafta\nLa mentira podría descubrirse más adelante",
            detalleFallo = "No obtuviste combustible\nTu reputación baja"
        });
        e0.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Vengo de una base segura donde tienen refinerías activas. Dame nafta para llegar y te doy un pase de acceso premium para vos.",
            npcExito = "¿Refinerías activas? Increíble. Tomá el bidón, dame ese pase.",
            npcFallo = "No existen refinerías activas en 500 kilómetros a la redonda. Mentiroso.",
            detalleExito = "Conseguiste 5L de nafta con un pase falso",
            detalleFallo = "Roxana se enoja por el intento de estafa"
        });
        e0.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Hay un pozo de agua potable escondido a dos kilómetros de acá. Pasame la nafta y te marco el mapa exacto.",
            npcExito = "¡Agua limpia! Eso vale oro. Trato hecho, dame el mapa.",
            npcFallo = "Conozco esta zona como la palma de mi mano y no hay agua potable ahí. Fuera.",
            detalleExito = "Conseguiste 5L de nafta a cambio de un mapa de agua inventado",
            detalleFallo = "Intento de engaño fallido"
        });

        escenarios.Add(e0);

        // 2. Escenario Gervasio (Chatarrero Desconfiado)
        EscenarioDialogo e1 = new EscenarioDialogo();
        e1.npcNombre = "Gervasio";
        e1.npcPregunta = "Tengo un par de bidones guardados, pero me costó un huevo sacarlos de los fierros viejos. La calle está dura y ya me cagaron un par de veces. ¿Por qué debería confiar en vos y qué tenés para darme?";

        // Honesto — todas mencionan items que el jugador quizás no tiene; se agrega un fallback genérico
        e1.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Gervasio, no te voy a mentir. Solo tengo lo que llevo encima y ando corto de recursos. Lo que tengo es tuyo si me ayudás con algo de nafta.",
            npcExito = "Al menos no me venís con cuentos. Un poco de honestidad vale en estos tiempos. Tomá.",
            npcFallo = "Si no tenés nada que ofrecerme, no puedo darte nafta. Así de simple.",
            detalleExito = "Conseguiste 3L de nafta\nGervasio valora tu franqueza",
            detalleFallo = "Gervasio desconfía de tu oferta"
            // No itemsNecesarios: siempre disponible como fallback
        });
        e1.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "No te voy a mentir, Gervasio. La verdad es que ando corto de todo. Te puedo dar mis herramientas de repuesto y unos fósforos secos.",
            npcExito = "Herramientas y fósforos... Bueno, al menos no me venís con cuentos de hadas. Trato hecho.",
            npcFallo = "Las herramientas no me sirven si no tengo comida, pibe. Buscate a otro.",
            detalleExito = "Conseguiste 3L de nafta\nGervasio valora tu franqueza",
            detalleFallo = "Gervasio desconfía de tu oferta",
            itemsNecesarios = new[] { "caja de herramientas", "fosforitos" }
        });
        e1.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Mirá, no tengo mucho para comerciar. Te doy mi única caja de herramientas y 10 de mis fósforos. Es lo único que me queda de repuesto.",
            npcExito = "Fósforos y herramientas... trato directo y honesto. Tomá la nafta, te ganaste mi respeto.",
            npcFallo = "Necesito otras cosas en el taller, herramientas tengo de sobra. No hay trato.",
            detalleExito = "Conseguiste 3L de nafta\nGervasio acepta las herramientas",
            detalleFallo = "Gervasio rechaza el intercambio",
            itemsNecesarios = new[] { "caja de herramientas", "fosforitos" }
        });
        e1.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Gervasio, te hablo con el corazón en la mano. La camioneta se me muere y solo tengo esta caja de herramientas y fósforos para ofrecerte.",
            npcExito = "Valorar la verdad es importante en la ruta. Trato hecho, pibe.",
            npcFallo = "Tus fósforos están un poco gastados y la caja oxidada. Buscá en otro lado.",
            detalleExito = "Conseguiste 3L de nafta\nIntercambio honesto completado",
            detalleFallo = "Gervasio rechaza la oferta",
            itemsNecesarios = new[] { "caja de herramientas", "fosforitos" }
        });

        // Chamuyero
        e1.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = "Mirá, este Rastrojero que ves acá parece baqueteado, pero tiene un motor noble. Si me bancás con la nafta, te aseguro que cuando vuelva del pueblo del norte te traigo repuestos originales de alternador que tengo encajonados allá.",
            npcExito = "¡Ja! ¿Repuestos de alternador en el norte? Son más difíciles de ver que un político honesto. Pero me cae bien tu optimismo, pibe. Te voy a dar un poco.",
            npcFallo = "Esos cuentos del norte ya los escuché cien veces. No te creo nada.",
            detalleExito = "Conseguiste 2L de nafta\nGervasio decide darte una oportunidad",
            detalleFallo = "Gervasio no te cree y se niega a comerciar"
        });
        e1.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = "Tengo una red de contactos que me provee de repuestos militares. Dame nafta ahora y te aseguro prioridad en el próximo lote de cables de bujía blindados.",
            npcExito = "¿Cables blindados? Serían un golazo para mi camioneta vieja. Dale, te doy una parte.",
            npcFallo = "Cables blindados... no me hagas reír. Esos ya no existen.",
            detalleExito = "Conseguiste 2L de nafta por tus promesas de cables",
            detalleFallo = "Gervasio rechaza tu chamuyo"
        });
        e1.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = "Si me facilitás nafta hoy, prometo que en mi regreso te traigo un set completo de llaves tubo francesas cromadas que tengo guardadas.",
            npcExito = "Las llaves tubo cromadas son excelentes. Ojalá cumplas. Trato hecho.",
            npcFallo = "Las llaves francesas que prometés seguro son de plástico. No caigo en esa.",
            detalleExito = "Conseguiste 2L de nafta\nGervasio acepta la promesa",
            detalleFallo = "Trato no realizado"
        });

        // Mentiroso
        e1.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Justo un par de kilómetros atrás pasé por un galpón abandonado que tenía un generador intacto y varias baterías de gel. Si cerramos trato ahora, te paso la ubicación exacta antes de irme.",
            npcExito = "¿Baterías de gel y un generador entero? La pucha, eso me vendría al pelo para el taller. Trato hecho, pero más vale que no me estés verseando.",
            npcFallo = "¿Un generador intacto? A esta altura ya saquearon todo. No me mientas que te va a ir mal.",
            detalleExito = "Conseguiste 5L de nafta\nGervasio acepta la ubicación falsa",
            detalleFallo = "Gervasio se da cuenta de la mentira y te echa"
        });
        e1.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Se acerca una tormenta de arena ácida del oeste, me lo avisaron por radio. Dame nafta y te doy mi máscara de gas de repuesto para que no te quemes los pulmones.",
            npcExito = "¿Tormenta ácida? ¡Dios santo! Tomá el combustible y dame la máscara rápido.",
            npcFallo = "¿Tormenta ácida hoy? El cielo está despejado y mi radio no dice nada. Mentiroso de cuarta.",
            detalleExito = "Conseguiste 5L de nafta asustando a Gervasio",
            detalleFallo = "Gervasio descubre tu mentira sobre el clima"
        });
        e1.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Soy el dueño original del depósito de combustible de la YPF de la zona. Si me ayudás a cargar hoy, te daré la clave de la caja fuerte oculta de la oficina.",
            npcExito = "¿La caja fuerte oculta? Eso debe tener herramientas caras. Trato hecho, dame la clave.",
            npcFallo = "Ese depósito está vacío y la oficina quemada hace años. No me mientas en la cara.",
            detalleExito = "Conseguiste 5L de nafta dando una clave inventada",
            detalleFallo = "Gervasio te echa a patadas de su taller"
        });

        escenarios.Add(e1);

        // 3. Escenario Beto (Viajero buscando abrigo)
        EscenarioDialogo e2 = new EscenarioDialogo();
        e2.npcNombre = "Beto";
        e2.npcPregunta = "Qué hacés, che. Te veo a paso de hombre con esa cafetera. Yo tengo una garrafa cargada y un bidón de súper que me sobró de la moto. Estoy buscando abrigo para el invierno que se viene. ¿Hacemos trato?";

        // Honesto
        e2.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Hola, Beto. Sí, el frío de la noche cala los huesos. Tengo una frazada de lana gruesa en el asiento de atrás, está un poco gastada pero abriga en serio. No me queda otra cosa de valor, pero necesito esa nafta para seguir viaje.",
            npcExito = "A ver... toca la frazada... Sí, está áspera pero es lana pura, calienta al toque. Hacemos trato directo: te doy el bidón entero y la frazada me la quedo yo.",
            npcFallo = "Esa frazada está muy rota, pibe. No me va a tapar del viento helado del sur. Buscate otra cosa.",
            detalleExito = "Conseguiste 4L de nafta a cambio de 1 Frazada\nTrato muy amigable con Beto",
            detalleFallo = "Beto rechaza tu frazada desgastada",
            itemsNecesarios = new[] { "frasada" }
        });
        e2.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Qué tal, Beto. Te doy mi frazada abrigada para pasar la noche. Está limpia y sin roturas. Necesito tu combustible para avanzar.",
            npcExito = "Una frazada en buen estado... justo lo que buscaba. Tomá el combustible.",
            npcFallo = "Tengo frazadas finas, busco algo de lana pesada. Lo siento.",
            detalleExito = "Conseguiste 4L de nafta\nBeto se abriga con tu frazada",
            detalleFallo = "Beto rechaza la oferta",
            itemsNecesarios = new[] { "frasada" }
        });
        e2.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Beto, no te miento, es mi última frazada pero el frío aprieta y prefiero moverme. Hagamos el cambio directo por tu bidón.",
            npcExito = "Es un trato justo. El frío no perdona y nos ayudamos mutuamente. Dale.",
            npcFallo = "Tu frazada está húmeda, no me sirve en este estado. Suerte.",
            detalleExito = "Conseguiste 4L de nafta\nTrueque de frazada exitoso",
            detalleFallo = "Beto no acepta la frazada húmeda",
            itemsNecesarios = new[] { "frasada" }
        });
        // Fallback genérico si no tiene frasada
        e2.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Qué tal, Beto. Ando sin mucho pero soy directo: necesito nafta para seguir viaje. ¿Qué te puedo ofrecer?",
            npcExito = "Se nota que sos honesto. Está bien, hacemos algo.",
            npcFallo = "Si no tenés abrigo para ofrecerme, no puedo darte el bidón. Suerte.",
            detalleExito = "Conseguiste 4L de nafta\nBeto aprecia la honestidad",
            detalleFallo = "Beto no puede ayudarte"
            // No itemsNecesarios: siempre disponible
        });

        // Chamuyero
        e2.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = "Tengo una campera térmica militar que pertenecía a un comandante de las fuerzas especiales. Te protege del viento, la nieve y hasta dicen que frena la radiación. Vale diez veces más que tu nafta vieja.",
            npcExito = "¿Fuerzas especiales? Parece una campera común con dos parches cosidos... pero el material se la banca. Dale, hagamos el cambalache.",
            npcFallo = "¡Qué fuerzas especiales ni qué ochenta cuartos! Eso es un rompeviento de feria. No me caminees.",
            detalleExito = "Conseguiste 3L de nafta\nBeto se queda con la campera militar",
            detalleFallo = "Beto se ríe de tu supuesta campera militar"
        });
        e2.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = "Esta prenda que tengo acá fue usada por montañistas profesionales. Tiene tecnología de retención de calor reflectiva. Es lo mejor del mercado.",
            npcExito = "Se la ve abrigada y reflectiva. Me sirve para andar en moto de noche. Hacemos trato.",
            npcFallo = "Esta tela sintética da frío solo de verla. No trates de convencerme.",
            detalleExito = "Conseguiste 3L de nafta a cambio de la prenda",
            detalleFallo = "Beto prefiere algo de lana"
        });
        e2.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = "Te doy un poncho andino tejido a mano por artesanos. Es impermeable y repele el viento helado como ninguna campera moderna.",
            npcExito = "Me gusta el estilo y se siente bastante pesado. Hacemos trueque.",
            npcFallo = "Ese poncho está deshilachado. No me sirve de nada.",
            detalleExito = "Conseguiste 3L de nafta\nBeto se abriga con tu poncho",
            detalleFallo = "Beto rechaza el poncho"
        });

        // Mentiroso
        e2.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Beto, escuchame bien. Una banda de motoqueros pesados viene barriendo la ruta desde el oeste. Están sacando todo a la fuerza. Dame el combustible para el Rastrojero, subite conmigo y escapemos antes de que nos alcancen.",
            npcExito = "¿¡Motoqueros!? ¡La gran flauta, sabía que este lugar no era seguro! Tomá, tomá el bidón, cargá rápido y arranquemos. ¡Metete en el monte!",
            npcFallo = "¿Motoqueros? Vengo de esa dirección y no vi más que un par de vacas flacas. No me quieras asustar para robarme.",
            detalleExito = "Conseguiste 3L de nafta gratis por sembrar pánico\nBeto huye asustado",
            detalleFallo = "Beto descubre que inventaste la banda de motoqueros"
        });
        e2.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Beto, la YPF del norte está regalando raciones de comida y frazadas a todos los viajeros hoy. Si me das nafta ahora, te digo el atajo para llegar antes de que se agoten.",
            npcExito = "¿Raciones y frazadas gratis hoy? ¡Salgo volando para allá! Tomá el bidón y pasame la ruta.",
            npcFallo = "Esa YPF cerró hace meses y el camino está bloqueado. No me chamuyes con cuentos.",
            detalleExito = "Conseguiste 3L de nafta a cambio de un atajo falso",
            detalleFallo = "Beto se da cuenta de que la YPF no funciona"
        });
        e2.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Un convoy militar viene en camino y va a confiscar todo el combustible suelto por razones de emergencia. Te conviene dármelo a mí antes de que te lo saquen por nada.",
            npcExito = "¿Confiscación militar? ¡Qué malaria! Prefiero que te lo lleves vos. Tomá el bidón.",
            npcFallo = "Los militares no patrullan esta zona desde el año pasado. Dejate de inventar historias.",
            detalleExito = "Conseguiste 3L de nafta gratis simulando requisa militar",
            detalleFallo = "Beto rechaza tu falsa requisa"
        });

        escenarios.Add(e2);

        // 4. Escenario Flavia (Centinela de la cooperativa)
        EscenarioDialogo e3 = new EscenarioDialogo();
        e3.npcNombre = "Flavia";
        e3.npcPregunta = "¡Alto ahí! Esta zona está bajo control de la cooperativa de defensa. Si querés pasar o abastecerte de combustible, vas a tener que pagar el peaje o colaborar con la guardia. ¿Qué intenciones tenés?";

        // Honesto
        e3.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Buenas tardes. Miren, no soy de ninguna facción ni busco problemas. Solo quiero cargar nafta para el Rastrojero para pasar la noche lejos de aquí. Les ofrezco mis últimas provisiones de comida.",
            npcExito = "Al menos no sos otro espía de la banda vecina. Se nota que venís cansado y que no mentís. Dejanos las provisiones y cargá el bidón de la guardia. Podés pasar en paz.",
            npcFallo = "No estamos para beneficencia. Si no hay suficiente pago para la cooperativa, no hay nafta.",
            detalleExito = "Conseguiste 3L de nafta\nLa cooperativa te permite el paso",
            detalleFallo = "Flavia te prohíbe pasar sin pagar un peaje real"
            // No itemsNecesarios: genérico, siempre válido
        });
        e3.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Hola. Vengo solo y cansado. Ofrezco entregar un paquete de pitusas para la guardia a cambio de algo de nafta. No tengo mala intención.",
            npcExito = "Se agradece la actitud. Unas galletitas para la guardia nocturna vienen bien. Cargá el bidón y pasá.",
            npcFallo = "Un simple paquete de galletitas no cubre las tarifas de peaje de la cooperativa.",
            detalleExito = "Conseguiste 3L de nafta\nFlavia acepta tus pitusas",
            detalleFallo = "Flavia rechaza las pitusas",
            itemsNecesarios = new[] { "pitusas" }
        });
        e3.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "La verdad es que no tengo intenciones hostiles, soy solo un viajero solitario. Ofrezco mis galletitas pitusas de forma honesta.",
            npcExito = "La honestidad vale en nuestro puesto. Hacemos el trueque. Pasá.",
            npcFallo = "Tus provisiones no son suficientes para los estándares del peaje hoy. Lo lamento.",
            detalleExito = "Conseguiste 3L de nafta\nTrueque honesto completado",
            detalleFallo = "Peaje denegado",
            itemsNecesarios = new[] { "pitusas" }
        });

        // Chamuyero
        e3.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = "Vengo de parte del almacén central del bajo. Les traigo un mensaje de alianza y el mes que viene vamos a mandar un convoy con convoy con munición y repuestos para sus puestos de guardia.",
            npcExito = "¿Del almacén del bajo? Bueno, hace rato que no tenemos novedades de allá. Si es verdad lo del convoy, nos viene de diez. Llevate la nafta.",
            npcFallo = "El almacén del bajo fue saqueado hace dos semanas. No trates de vernos la cara de giles.",
            detalleExito = "Conseguiste 2L de nafta\nFlavia queda a la espera del convoy",
            detalleFallo = "Flavia te detiene temporalmente por sospechoso"
        });
        e3.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = "Tengo influencia con los líderes del norte. Si me dejás pasar con combustible, haré que reduzcan los impuestos comerciales para esta estación.",
            npcExito = "Bajar los impuestos nos vendría excelente. Te daremos el bidón por la gestión.",
            npcFallo = "Nadie del norte tiene poder sobre este peaje independiente. Tu chamuyo no sirve.",
            detalleExito = "Conseguiste 2L de nafta prometiendo rebajas fiscales",
            detalleFallo = "Flavia rechaza tus promesas comerciales"
        });
        e3.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = "Este Rastrojero lleva documentos comerciales de alta prioridad. Si me abastecen de combustible, reportaré su puesto como ejemplar para que reciban ascensos.",
            npcExito = "Un ascenso a la capital de la cooperativa sería genial. Tomá el bidón de nafta.",
            npcFallo = "No nos importan los informes de ejemplaridad. Pagá el peaje en efectivo o nada.",
            detalleExito = "Conseguiste 2L de nafta por informe favorable",
            detalleFallo = "Paso denegado"
        });

        // Mentiroso
        e3.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Soy inspector de la red de pozos petroleros del sur. Vengo a certificar la calidad de los combustibles de esta estación. Si me cargás nafta ahora, pondré una buena calificación para que les envíen más suministros.",
            npcExito = "¿Inspector? Hace años que la red colapsó, pero a veces algunos grupos intentan reactivarla. Está bien, hacé tu inspección y llevate este bidón para las muestras.",
            npcFallo = "¿Calificación? ¿Inspector? Dejate de joder, el petróleo se acabó hace una década. Quedás bajo advertencia.",
            detalleExito = "Conseguiste 5L de nafta de muestra\nFlavia cree tu falsa identidad",
            detalleFallo = "Descubren tu mentira y te quitan el combustible"
        });
        e3.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Vengo huyendo de un brote de peste en la estación anterior. Tengo que llevar este combustible al laboratorio del norte para la cura. Cada segundo cuenta.",
            npcExito = "¿¡Peste!? ¡Dios mío, pasa rápido y no dejes que el aire se contamine! Tomá la nafta y andate.",
            npcFallo = "No hay noticias de peste en los alrededores y tu cara se ve perfectamente sana. No mientas con algo así.",
            detalleExito = "Conseguiste 5L de nafta fingiendo una emergencia médica",
            detalleFallo = "Flavia te bloquea el paso por mentiroso"
        });
        e3.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Tengo órdenes directas del Comandante de la Cooperativa para requisar este combustible para la defensa fronteriza. Acá está el acta firmada.",
            npcExito = "¿Órdenes del Comandante? El acta parece real... Está bien, lleve el bidón oficial.",
            npcFallo = "Esa firma es fotocopiada y el Comandante fue retirado hace meses. Quedás detenido.",
            detalleExito = "Conseguiste 5L de nafta con acta requisitoria falsa",
            detalleFallo = "Te descubren con documentación adulterada"
        });

        escenarios.Add(e3);

        // 5. Escenario Vagabundo (Brújula)
        EscenarioDialogo e4 = new EscenarioDialogo();
        e4.npcNombre = "Jacinto";
        e4.npcPregunta = "Hola, viajero. Encontré esta brújula militar en perfecto estado, ideal para no perderse en estas rutas devastadas. ¿Qué me ofrecés a cambio?";

        // Honesto
        e4.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "No te voy a mentir, ando corto de recursos pero tengo un paquete de pitusas y un mate para ofrecerte. Te va a dar compañía y calor en el camino.",
            npcExito = "Un mate y unas pitusas... Hace meses que no tomo un buen mate. Trato hecho, viajero. Tomá la brújula, te va a servir más que a mí.",
            npcFallo = "El mate tienta, pero el frío aprieta y las pitusas no me llenan la panza. Necesito algo más sustancial.",
            detalleExito = "Conseguiste 1 Brújula militar\nLe entregaste 1 Mate y 1 Pitusas",
            detalleFallo = "Jacinto rechazó el mate y las pitusas"
        });
        e4.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Tengo poco y nada, che. Pero te puedo convidar un buen mate caliente y el paquete de galletitas pitusas que me queda para pasar el rato.",
            npcExito = "Un mate calentito en esta soledad vale más que cualquier chatarra. Hacemos trato, tomá la brújula.",
            npcFallo = "El mate viene bien, pero las pitusas están húmedas y necesito algo más contundente para el viaje.",
            detalleExito = "Conseguiste 1 Brújula militar\nLe entregaste 1 Mate y 1 Pitusas",
            detalleFallo = "Jacinto rechazó el mate y las pitusas"
        });
        e4.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "La verdad es que ando seco de recursos, pero te ofrezco compartir mi mate y el último paquete de pitusas. Es lo único real que me queda de valor.",
            npcExito = "Tu honestidad es refrescante. Compartamos el mate y llevate la brújula, te va a guiar bien.",
            npcFallo = "Te agradezco el gesto, pero un mate no me va a dar de comer mañana. Prefiero conservar la brújula.",
            detalleExito = "Conseguiste 1 Brújula militar\nLe entregaste 1 Mate y 1 Pitusas",
            detalleFallo = "Jacinto rechazó el mate y las pitusas"
        });

        // Chamuyero
        e4.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = "Mirá, te ofrezco una botella de gancia y un anafe portátil. Con esto podés calentarte la comida como un rey y disfrutar una noche espectacular.",
            npcExito = "¿Gancia y un anafe portátil? ¡Qué lujo! Hace años que no veo un anafe funcionando. Acepto el trato, tomá la brújula.",
            npcFallo = "El gancia tienta, pero no tengo gas para el anafe. Sin garrafa no me sirve de nada.",
            detalleExito = "Conseguiste 1 Brújula militar\nLe entregaste 1 Gancia y 1 Anafe",
            detalleFallo = "Jacinto no tiene cómo usar el anafe"
        });
        e4.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = "Mirá lo que es esta botella de gancia premium de edición limitada y el anafe portátil de alta montaña. Vas a poder cocinar y celebrar como un magnate.",
            npcExito = "¿Edición limitada? ¡Qué elegancia! Hacemos trato, la brújula es tuya.",
            npcFallo = "Ese gancia es común y el anafe está perdiendo gas. No me vas a convencer con palabrerío.",
            detalleExito = "Conseguiste 1 Brújula militar\nLe entregaste 1 Gancia y 1 Anafe",
            detalleFallo = "Jacinto no tiene cómo usar el anafe"
        });
        e4.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = "Te doy un anafe importado súper compacto que calienta en segundos y una botella del mejor gancia de colección. Una oferta irrepetible.",
            npcExito = "Me convenciste. Ese anafe importado me va a facilitar la vida. Trato hecho.",
            npcFallo = "No tiene chispero el anafe y la botella está por la mitad. Guardate el chamuyo para otro.",
            detalleExito = "Conseguiste 1 Brújula militar\nLe entregaste 1 Gancia y 1 Anafe",
            detalleFallo = "Jacinto no tiene cómo usar el anafe"
        });

        // Mentiroso
        e4.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Tengo una caja de herramientas completa y una batería nueva en el auto. Si cerramos trato ya, te dejo llevarte las dos cosas.",
            npcExito = "¿Herramientas y batería nueva? ¡Espectacular! Con eso puedo reparar mi refugio. Trato hecho, tomá la brújula antes de que te arrepientas.",
            npcFallo = "Esa caja de herramientas se ve vacía y la batería está sulfatada. No me quieras estafar, viajero.",
            detalleExito = "Conseguiste 1 Brújula militar gratis\nJacinto descubrirá después que la batería estaba muerta",
            detalleFallo = "Jacinto se dio cuenta del engaño y se niega a comerciar"
        });
        e4.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Esta brújula que tenés es parecida a una que perdí. Te doy a cambio una caja de herramientas de aviación y una batería sellada que encontré en un hangar.",
            npcExito = "¡Herramientas de aviación! Eso es calidad de primera. Acepto el trato, viajero. Tomá la brújula.",
            npcFallo = "¿De aviación? Esas son herramientas comunes de ferretería barata y la batería no tiene carga. Me querés pasar gato por liebre.",
            detalleExito = "Conseguiste 1 Brújula militar gratis\nJacinto descubrirá después que las herramientas no eran de aviación",
            detalleFallo = "Jacinto se dio cuenta del engaño y se niega a comerciar"
        });
        e4.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Te cambio la brújula por una batería militar de ciclo profundo de mi vehículo y un set de herramientas profesionales de alta gama.",
            npcExito = "Una batería de ciclo profundo vale oro puro para mis paneles solares. Hacemos trato. Acá tenés la brújula.",
            npcFallo = "Esta batería tiene los bornes rotos y a la caja le faltan la mitad de las llaves. No me vas a engañar tan fácil.",
            detalleExito = "Conseguiste 1 Brújula militar gratis\nJacinto descubrirá después que la batería no era militar",
            detalleFallo = "Jacinto se dio cuenta del engaño y se niega a comerciar"
        });

        escenarios.Add(e4);

        // 6. Escenario npc3 (Caja de herramientas)
        EscenarioDialogo e5 = new EscenarioDialogo();
        e5.npcNombre = "Héctor";
        e5.npcPregunta = "Hola, amigo. Tengo esta caja de herramientas pesada que no uso, ideal para reparar tu vehículo en la ruta. ¿Qué me ofrecés a cambio?";

        // Honesto
        e5.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Te ofrezco mi termo para el viaje y un paquete de galletitas pitusas para que pases la tarde de manera honesta.",
            npcExito = "El termo me viene al pelo para calentar agua en la ruta y las galletitas están ricas. Trato hecho, tomá la caja de herramientas.",
            npcFallo = "Un termo no me abriga lo suficiente y busco algo de más valor. Prefiero quedarme con las herramientas.",
            detalleExito = "Conseguiste 1 Caja de herramientas\nLe entregaste 1 Termo",
            detalleFallo = "Héctor rechazó tu oferta"
        });
        e5.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Siendo sincero, tengo un termo metálico resistente y unas pitusas que me quedan de repuesto. Te ofrezco eso.",
            npcExito = "Trato justo y honesto. Me viene perfecto. Llevate la caja de herramientas.",
            npcFallo = "El termo está abollado y no me convence. Trato no hecho.",
            detalleExito = "Conseguiste 1 Caja de herramientas\nLe entregaste 1 Termo",
            detalleFallo = "Héctor rechazó tu oferta"
        });
        e5.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Te puedo convidar mi termo térmico y un paquete de pitusas. Es un trato simple, directo y honesto.",
            npcExito = "Me sirve el termo térmico. Hacemos trato. Tomá la caja de herramientas.",
            npcFallo = "El termo pierde calor y no me sirve en este frío. Dejalo ahí.",
            detalleExito = "Conseguiste 1 Caja de herramientas\nLe entregaste 1 Termo",
            detalleFallo = "Héctor rechazó tu oferta"
        });

        // Chamuyero
        e5.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = "Esta botella de gancia artesanal es una joya oculta de colección. Te la cambio por tus herramientas y vas a brindar como un rey.",
            npcExito = "¡Un gancia artesanal! Hace mucho que no me doy un gusto así. Trato hecho, llevate la caja.",
            npcFallo = "Esa botella está abierta y tiene gusto a rebajado. No me chamuyes.",
            detalleExito = "Conseguiste 1 Caja de herramientas\nLe entregaste 1 Gancia",
            detalleFallo = "Héctor rechazó tu oferta"
        });
        e5.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = "Te doy esta botella de licor gancia de edición especial, perfecta para amenizar tus noches frías al costado del camino.",
            npcExito = "Una copa de gancia reconforta el alma. Acepto el cambio, tomá las herramientas.",
            npcFallo = "No me interesa el alcohol en este momento, prefiero provisiones reales. Trato cancelado.",
            detalleExito = "Conseguiste 1 Caja de herramientas\nLe entregaste 1 Gancia",
            detalleFallo = "Héctor rechazó tu oferta"
        });
        e5.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = "Mirá este gancia premium importado. Con esto te hacés el rey de los campamentos post-apocalípticos. Oferta de oro.",
            npcExito = "Me convenciste con el gancia premium. Tomá la caja de herramientas y que te sea de ayuda.",
            npcFallo = "Ese gancia es de la marca más barata, se nota la etiqueta falsa. No caigo en tu trampa.",
            detalleExito = "Conseguiste 1 Caja de herramientas\nLe entregaste 1 Gancia",
            detalleFallo = "Héctor rechazó tu oferta"
        });

        // Mentiroso
        e5.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Te cambio esa caja de herramientas por esta frazada térmica militar de última tecnología que repele la humedad por completo.",
            npcExito = "¡Frazada militar contra humedad! Excelente para este invierno. Acepto, tomá la caja de herramientas.",
            npcFallo = "Esta frazada está deshilachada y es de tela común de algodón. Me estás mintiendo.",
            detalleExito = "Conseguiste 1 Caja de herramientas gratis\nHéctor descubrirá después que la frazada era común y corriente",
            detalleFallo = "Héctor se dio cuenta del engaño y rechazó el trueque"
        });
        e5.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Tengo esta frazada térmica que mantiene el calor corporal incluso bajo cero, ideal para la estepa patagónica.",
            npcExito = "Bajo cero es justo lo que necesito para las heladas. Trato hecho, acá tenés la caja.",
            npcFallo = "Esta frazada es fina y está rota en los costados. Me querés engañar.",
            detalleExito = "Conseguiste 1 Caja de herramientas gratis\nHéctor descubrirá después que la frazada no era térmica",
            detalleFallo = "Héctor se dio cuenta del engaño y rechazó el trueque"
        });
        e5.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = "Te doy una manta térmica ultra ligera de rescate que utilizan los servicios de emergencia de la montaña.",
            npcExito = "Si es de rescate, debe ser buena. Hacemos trato, llevate la caja de herramientas.",
            npcFallo = "Esto es un mantel de plástico pintado de plateado. No me mientas en la cara.",
            detalleExito = "Conseguiste 1 Caja de herramientas gratis\nHéctor descubrirá después que era un plástico común",
            detalleFallo = "Héctor se dio cuenta del engaño y rechazó el trueque"
        });

        escenarios.Add(e5);
    }

    private Sprite CargarSpriteDesdeDisco(string relativePath)
    {
#if UNITY_EDITOR
        object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/" + relativePath);
        foreach (object asset in assets)
        {
            if (asset is Sprite)
            {
                return (Sprite)asset;
            }
        }
        return null;
#else
        string fullPath = System.IO.Path.Combine(Application.dataPath, relativePath);
        if (System.IO.File.Exists(fullPath))
        {
            byte[] fileData = System.IO.File.ReadAllBytes(fullPath);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(fileData))
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
        return null;
#endif
    }

    private Sprite CargarSpriteDesdeResources(string resourcePath)
    {
        Sprite s = Resources.Load<Sprite>(resourcePath);
        if (s != null)
        {
            return s;
        }
        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
        if (sprites != null && sprites.Length > 0)
        {
            return sprites[0];
        }
        return null;
    }

    private bool TieneObjetosRequeridos(TipoRespuesta tipo)
    {
        if (InventoryManager.Instance == null || escenarioActual == null) return true;

        // Chamuyero and Mentiroso represent bluffing/lying, so they do not require
        // actually possessing the items in inventory. Only Honesto requires them.
        if (tipo != TipoRespuesta.Honesto) return true;

        if (escenarioActual.npcNombre == "Roxana")
        {
            return InventoryManager.Instance.HasItem("pitusas", 1);
        }
        else if (escenarioActual.npcNombre == "Gervasio")
        {
            return InventoryManager.Instance.HasItem("caja de herramientas", 1) &&
                   InventoryManager.Instance.HasItem("fosforitos", 10);
        }
        else if (escenarioActual.npcNombre == "Beto")
        {
            return InventoryManager.Instance.HasItem("frasada", 1);
        }
        else if (escenarioActual.npcNombre == "Flavia")
        {
            return InventoryManager.Instance.HasItem("pitusas", 1);
        }
        else if (escenarioActual.npcNombre == "Jacinto")
        {
            return InventoryManager.Instance.HasItem("mate", 1) &&
                   InventoryManager.Instance.HasItem("pitusas", 1);
        }
        else if (escenarioActual.npcNombre == "H\u00e9ctor" || escenarioActual.npcNombre == "Héctor" || escenarioActual.npcNombre == "npc3")
        {
            return InventoryManager.Instance.HasItem("termo", 1);
        }

        return true;
    }

    private void ProcesarTruequeInventario()
    {
        if (InventoryManager.Instance == null || escenarioActual == null) return;

        if (escenarioActual.npcNombre == "Roxana")
        {
            if (chosenType == TipoRespuesta.Honesto)
            {
                if (InventoryManager.Instance.HasItem("pitusas", 1))
                    InventoryManager.Instance.RemoveItem("pitusas", 1);
                InventoryManager.Instance.AddItem("nafta", randomFuelAmount);
            }
            else if (chosenType == TipoRespuesta.Chamuyero)
            {
                InventoryManager.Instance.AddItem("nafta", randomFuelAmount);
            }
            else if (chosenType == TipoRespuesta.Mentiroso)
            {
                InventoryManager.Instance.AddItem("nafta", randomFuelAmount);
            }
        }
        else if (escenarioActual.npcNombre == "Gervasio")
        {
            if (chosenType == TipoRespuesta.Honesto)
            {
                if (InventoryManager.Instance.HasItem("caja de herramientas", 1))
                    InventoryManager.Instance.RemoveItem("caja de herramientas", 1);
                if (InventoryManager.Instance.HasItem("fosforitos", 10))
                    InventoryManager.Instance.RemoveItem("fosforitos", 10);
                InventoryManager.Instance.AddItem("nafta", randomFuelAmount);
            }
            else if (chosenType == TipoRespuesta.Chamuyero)
            {
                InventoryManager.Instance.AddItem("nafta", randomFuelAmount);
            }
            else if (chosenType == TipoRespuesta.Mentiroso)
            {
                InventoryManager.Instance.AddItem("nafta", randomFuelAmount);
            }
        }
        else if (escenarioActual.npcNombre == "Beto")
        {
            if (chosenType == TipoRespuesta.Honesto)
            {
                if (InventoryManager.Instance.HasItem("frasada", 1))
                    InventoryManager.Instance.RemoveItem("frasada", 1);
                InventoryManager.Instance.AddItem("nafta", randomFuelAmount);
            }
            else if (chosenType == TipoRespuesta.Chamuyero)
            {
                InventoryManager.Instance.AddItem("nafta", randomFuelAmount);
            }
            else if (chosenType == TipoRespuesta.Mentiroso)
            {
                InventoryManager.Instance.AddItem("nafta", randomFuelAmount);
            }
        }
        else if (escenarioActual.npcNombre == "Flavia")
        {
            if (chosenType == TipoRespuesta.Honesto)
            {
                if (InventoryManager.Instance.HasItem("pitusas", 1))
                    InventoryManager.Instance.RemoveItem("pitusas", 1);
                InventoryManager.Instance.AddItem("nafta", randomFuelAmount);
            }
            else if (chosenType == TipoRespuesta.Chamuyero)
            {
                InventoryManager.Instance.AddItem("nafta", randomFuelAmount);
            }
            else if (chosenType == TipoRespuesta.Mentiroso)
            {
                InventoryManager.Instance.AddItem("nafta", randomFuelAmount);
            }
        }
        else if (escenarioActual.npcNombre == "Jacinto")
        {
            if (chosenType == TipoRespuesta.Honesto)
            {
                if (InventoryManager.Instance.HasItem("mate", 1))
                    InventoryManager.Instance.RemoveItem("mate", 1);
                if (InventoryManager.Instance.HasItem("pitusas", 1))
                    InventoryManager.Instance.RemoveItem("pitusas", 1);
                InventoryManager.Instance.AddItem("brujula", 1);
            }
            else if (chosenType == TipoRespuesta.Chamuyero)
            {
                if (InventoryManager.Instance.HasItem("gancia", 1))
                    InventoryManager.Instance.RemoveItem("gancia", 1);
                if (InventoryManager.Instance.HasItem("anafe", 1))
                    InventoryManager.Instance.RemoveItem("anafe", 1);
                InventoryManager.Instance.AddItem("brujula", 1);
            }
            else if (chosenType == TipoRespuesta.Mentiroso)
            {
                if (InventoryManager.Instance.HasItem("caja de herramientas", 1))
                    InventoryManager.Instance.RemoveItem("caja de herramientas", 1);
                if (InventoryManager.Instance.HasItem("bateria", 1))
                    InventoryManager.Instance.RemoveItem("bateria", 1);
                InventoryManager.Instance.AddItem("brujula", 1);
            }
        }
        else if (escenarioActual.npcNombre == "H\u00e9ctor")
        {
            if (chosenType == TipoRespuesta.Honesto)
            {
                if (InventoryManager.Instance.HasItem("termo", 1))
                    InventoryManager.Instance.RemoveItem("termo", 1);
                InventoryManager.Instance.AddItem("caja de herramientas", 1);
            }
            else if (chosenType == TipoRespuesta.Chamuyero)
            {
                if (InventoryManager.Instance.HasItem("gancia", 1))
                    InventoryManager.Instance.RemoveItem("gancia", 1);
                InventoryManager.Instance.AddItem("caja de herramientas", 1);
            }
            else if (chosenType == TipoRespuesta.Mentiroso)
            {
                if (InventoryManager.Instance.HasItem("frasada", 1))
                    InventoryManager.Instance.RemoveItem("frasada", 1);
                InventoryManager.Instance.AddItem("caja de herramientas", 1);
            }
        }
    }
}
