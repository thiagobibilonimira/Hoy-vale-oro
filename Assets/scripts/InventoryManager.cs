using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;

public class InventoryManager : MonoBehaviour
{
    private static InventoryManager instance;

    public static InventoryManager Instance
    {
        get { return instance; }
    }

    [System.Serializable]
    public class Item
    {
        public string code;        // Military code or quantity label (e.g. 200, BOX)
        public string name;        // Clean display name
        public float weight;       // Weight in KG
        public string type;        // $, i, or 🛠
        public string description; // Detail description
        public Sprite customIcon;  // Loaded icon if available
        public int quantity;       // Quantity of the item
        public bool isLocked;      // Locked state for trades
    }

    private List<Item> items = new List<Item>();
    private static List<Item> itemTemplates = new List<Item>();
    private const float MaxWeight = 200.0f; // Adjusted to match 200 KG in screenshot
    private int selectedIndex = -1;

    // UI GameObjects
    private GameObject canvasObjeto;
    private GameObject hudPanelObjeto;
    private GameObject panelInventarioObjeto;
    private GameObject gridContainerObjeto;
    private GameObject tooltipObjeto;
    private GameObject barraNaftaObjeto;
    private GameObject panelGameOverObjeto;

    private RectTransform liquidBarRect;
    private float maxFuelWidth;
    private static float currentFuel = 1.0f;

    public static float CurrentFuel
    {
        get { return currentFuel; }
        set
        {
            currentFuel = Mathf.Clamp01(value);
            if (instance != null)
            {
                instance.UpdateFuelBarUI();
            }
        }
    }

    private Movimiento jugadorMovimiento;
    private Vector3 ultimaPosicionJugador;
    private bool trackeandoPosicion = false;
    private float fuelConsumptionRate = 0.02f; // Consumo por unidad de distancia

    public void UpdateFuelBarUI()
    {
        float currentWidth = maxFuelWidth * currentFuel;
        if (liquidBarRect != null)
        {
            liquidBarRect.sizeDelta = new Vector2(currentWidth, liquidBarRect.sizeDelta.y);
        }
    }
    
    // UI Text Fields
    private TextMeshProUGUI txtHudCapacidad;
    private TextMeshProUGUI txtCapacidad;
    private TextMeshProUGUI txtTotalWeight;
    private TextMeshProUGUI txtTooltipName;
    private TextMeshProUGUI txtTooltipDesc;
    private TextMeshProUGUI txtHudHint;
    private TextMeshProUGUI txtBtnLockLabel;

    private List<GameObject> hudSlots = new List<GameObject>();
    private List<GameObject> slotObjects = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Initialize()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("InventorySystem");
            instance = go.AddComponent<InventoryManager>();
            DontDestroyOnLoad(go);
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Pre-populate starting items and load custom sprites
        PopulateStartingItems();

        // Move all populated items to templates, then only add starting items to active inventory
        itemTemplates.Clear();
        itemTemplates.AddRange(items);
        items.Clear();

        string[] startingNames = new string[] {
            "termo", "pitusas", "mate", "frasada"
        };
        foreach (string sName in startingNames)
        {
            Item temp = GetTemplateByName(sName);
            if (temp != null)
            {
                items.Add(new Item {
                    code = temp.code,
                    name = temp.name,
                    weight = temp.weight,
                    type = temp.type,
                    description = temp.description,
                    customIcon = temp.customIcon,
                    quantity = temp.quantity
                });
            }
        }

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        VerificarEscenaMenu(scene.name);
    }

    private void VerificarEscenaMenu(string sceneName)
    {
        bool isMenuOrMap = (sceneName == "menu" || sceneName == "videointro" || sceneName == "mapa");
        if (sceneName == "menu" || sceneName == "videointro")
        {
            CurrentFuel = 1.0f;
        }

        if (hudPanelObjeto != null)
        {
            hudPanelObjeto.SetActive(!isMenuOrMap);
        }
        if (panelInventarioObjeto != null)
        {
            panelInventarioObjeto.SetActive(false);
        }
        if (barraNaftaObjeto != null)
        {
            barraNaftaObjeto.SetActive(sceneName == "nivel_1_ypf");
            UpdateFuelBarUI();
        }
        if (panelGameOverObjeto != null)
        {
            panelGameOverObjeto.SetActive(false);
        }
    }

    void Start()
    {
        // Programmatically build the entire inventory UI
        ConstruirUI();
        UpdateInventoryUI();

        // Initial check for the scene
        VerificarEscenaMenu(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void Update()
    {
        // Disable keyboard interactions on the menu scene, video scene, and map scene
        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (activeScene == "menu" || activeScene == "videointro" || activeScene == "mapa")
        {
            return;
        }

        // Listen for the "Y" key to toggle the inventory overlay without mouse input
        if (Input.GetKeyDown(KeyCode.Y))
        {
            ToggleInventario();
        }

        // Fuel depletion logic in gameplay scene
        if (activeScene == "nivel_1_ypf")
        {
            if (jugadorMovimiento == null)
            {
                jugadorMovimiento = FindFirstObjectByType<Movimiento>();
                if (jugadorMovimiento != null)
                {
                    ultimaPosicionJugador = jugadorMovimiento.transform.position;
                    trackeandoPosicion = true;
                }
            }
            else
            {
                Vector3 posicionActual = jugadorMovimiento.transform.position;
                float distancia = Vector3.Distance(posicionActual, ultimaPosicionJugador);
                if (distancia > 0f)
                {
                    CurrentFuel -= distancia * fuelConsumptionRate;
                    ultimaPosicionJugador = posicionActual;
                }
            }

            // Check for Game Over condition
            if (CurrentFuel <= 0f && panelGameOverObjeto != null && !panelGameOverObjeto.activeSelf)
            {
                panelGameOverObjeto.SetActive(true);
            }
        }
        else
        {
            jugadorMovimiento = null;
            trackeandoPosicion = false;
        }
    }

    private void PopulateStartingItems()
    {
        // Try loading custom sprites from Resources
        List<Sprite> objetoSprites = CargarSpritesObjetos();

        // Fallbacks if objects directory is empty
        Sprite fallbackBidon = CargarSpriteDesdeResources("Sprites/bidon");
        Sprite fallbackCamioneta = CargarSpriteDesdeResources("Sprites/camioneta");

        // Helper to get sprite by index with fallbacks
        System.Func<int, Sprite> GetSprite = (index) => {
            if (objetoSprites != null && index < objetoSprites.Count && objetoSprites[index] != null)
            {
                return objetoSprites[index];
            }
            return (index % 2 == 0) ? fallbackBidon : fallbackCamioneta;
        };

        // 12 Items corresponding to objeto1.png to objeto12.png
        items.Add(new Item 
        { 
            code = "NAF", 
            name = "nafta", 
            weight = 4.5f, 
            type = "i", 
            description = "Bidón con nafta, esencial para el Rastrojero.",
            customIcon = GetSprite(0),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "TER", 
            name = "termo", 
            weight = 1.8f, 
            type = "$", 
            description = "Termo para mantener el agua caliente.",
            customIcon = GetSprite(1),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "PIT", 
            name = "pitusas", 
            weight = 0.3f, 
            type = "$", 
            description = "Paquete de galletitas pitusas. Un clásico infaltable para el viaje.",
            customIcon = GetSprite(2),
            quantity = 3
        });

        items.Add(new Item 
        { 
            code = "FOS", 
            name = "fosforitos", 
            weight = 0.1f, 
            type = "$", 
            description = "Caja de fósforos pequeños para encender fuego.",
            customIcon = GetSprite(3),
            quantity = 40
        });

        items.Add(new Item 
        { 
            code = "PAV", 
            name = "pava", 
            weight = 1.2f, 
            type = "$", 
            description = "Pava metálica para calentar agua para el mate.",
            customIcon = GetSprite(4),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "GAR", 
            name = "garrafa", 
            weight = 10.0f, 
            type = "🛠", 
            description = "Garrafa de gas envasado para cocinar o calefaccionar.",
            customIcon = GetSprite(5),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "TEL", 
            name = "tela impermeable", 
            weight = 2.5f, 
            type = "🛠", 
            description = "Lona o tela impermeable para proteger la carga del viento y lluvia.",
            customIcon = GetSprite(6),
            quantity = 2
        });

        items.Add(new Item 
        { 
            code = "CIN", 
            name = "cinta aislante", 
            weight = 0.2f, 
            type = "🛠", 
            description = "Rollo de cinta aisladora para reparaciones eléctricas rápidas.",
            customIcon = GetSprite(7),
            quantity = 3
        });

        items.Add(new Item 
        { 
            code = "HER", 
            name = "caja de herramientas", 
            weight = 8.5f, 
            type = "🛠", 
            description = "Caja metálica con llaves, pinzas y destornilladores.",
            customIcon = GetSprite(8),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "MAN", 
            name = "manguera", 
            weight = 1.5f, 
            type = "🛠", 
            description = "Manguera de goma útil para traspasar combustible o agua.",
            customIcon = GetSprite(9),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "FRA", 
            name = "frasada", 
            weight = 2.0f, 
            type = "$", 
            description = "Frazada abrigada para las noches frías en la ruta.",
            customIcon = GetSprite(10),
            quantity = 2
        });

        items.Add(new Item 
        { 
            code = "BAT", 
            name = "bateria", 
            weight = 15.0f, 
            type = "🛠", 
            description = "Batería de auto de 12V, pesada pero indispensable.",
            customIcon = GetSprite(11),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "CAC", 
            name = "cacerola", 
            weight = 1.5f, 
            type = "🛠", 
            description = "Cacerola de metal para cocinar comida en el campamento.",
            customIcon = GetSprite(12),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "GAN", 
            name = "gancia", 
            weight = 1.0f, 
            type = "$", 
            description = "Aperitivo Gancia, ideal para relajarse después de un largo día.",
            customIcon = GetSprite(13),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "PIN", 
            name = "pinza", 
            weight = 0.8f, 
            type = "🛠", 
            description = "Pinza metálica fuerte para reparaciones o trabajos mecánicos.",
            customIcon = GetSprite(14),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "ALF", 
            name = "alfombra", 
            weight = 3.5f, 
            type = "$", 
            description = "Alfombra tejida pequeña para aislar del frío del suelo.",
            customIcon = GetSprite(15),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "ANA", 
            name = "anafe", 
            weight = 2.2f, 
            type = "🛠", 
            description = "Anafe portátil a gas para cocinar de forma rápida.",
            customIcon = GetSprite(16),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "SOG", 
            name = "soga", 
            weight = 1.2f, 
            type = "🛠", 
            description = "Soga de cáñamo resistente, útil para atar carga.",
            customIcon = GetSprite(17),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "MAT", 
            name = "mate", 
            weight = 0.5f, 
            type = "$", 
            description = "Mate de madera listo para tomar con yerba.",
            customIcon = GetSprite(18),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "GUA", 
            name = "guantes", 
            weight = 0.3f, 
            type = "🛠", 
            description = "Guantes de cuero reforzados para trabajo pesado.",
            customIcon = GetSprite(19),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "BRU", 
            name = "brujula", 
            weight = 0.2f, 
            type = "$", 
            description = "Brújula militar para no perder la orientación.",
            customIcon = GetSprite(20),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "FAR", 
            name = "farol", 
            weight = 1.6f, 
            type = "🛠", 
            description = "Farol a kerosene para iluminar la noche.",
            customIcon = GetSprite(21),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "BOT", 
            name = "botella", 
            weight = 1.0f, 
            type = "$", 
            description = "Botella de vidrio para almacenar agua potable.",
            customIcon = GetSprite(22),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "OLL", 
            name = "olla", 
            weight = 2.0f, 
            type = "🛠", 
            description = "Olla de chapa grande para guisos en grupo.",
            customIcon = GetSprite(23),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "MAP", 
            name = "mapa", 
            weight = 0.1f, 
            type = "$", 
            description = "Mapa de carreteras desgastado con rutas marcadas.",
            customIcon = GetSprite(24),
            quantity = 1
        });
    }

    private List<Sprite> CargarSpritesObjetos()
    {
        List<Sprite> list = new List<Sprite>();
        for (int i = 1; i <= 25; i++)
        {
            Sprite s = CargarSpriteDesdeResources("Sprites/objetos/objeto" + i);
            if (s != null)
            {
                list.Add(s);
            }
            else
            {
                Debug.LogWarning("No se pudo cargar el sprite desde Resources: Sprites/objetos/objeto" + i);
            }
        }
        return list;
    }

    private int ExtraerNumero(string text)
    {
        string numStr = "";
        foreach (char c in text)
        {
            if (char.IsDigit(c)) numStr += c;
        }
        int val;
        return int.TryParse(numStr, out val) ? val : 0;
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

    private void ConstruirUI()
    {
        // 1. Create Canvas
        canvasObjeto = new GameObject("InventoryCanvas");
        canvasObjeto.transform.SetParent(this.transform, false);
        Canvas canvas = canvasObjeto.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Always render on top

        CanvasScaler scaler = canvasObjeto.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObjeto.AddComponent<GraphicRaycaster>();

        // Ensure EventSystem is present
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 2. Create Top-Left HUD Inventory Bar (Replacing old simple button)
        hudPanelObjeto = new GameObject("HUDInventoryBar");
        hudPanelObjeto.transform.SetParent(canvasObjeto.transform, false);

        RectTransform hudPanelRect = hudPanelObjeto.AddComponent<RectTransform>();
        hudPanelRect.anchorMin = new Vector2(0f, 1f);
        hudPanelRect.anchorMax = new Vector2(0f, 1f);
        hudPanelRect.pivot = new Vector2(0f, 1f);
        hudPanelRect.anchoredPosition = new Vector2(50, -45); // Raised closed inventory
        hudPanelRect.sizeDelta = new Vector2(500, 180); // Enlarged closed inventory

        // Header: INVENTARIO [X/200 KG] (Translated to Spanish)
        GameObject hudCapObj = new GameObject("TxtHudCapacidad");
        hudCapObj.transform.SetParent(hudPanelObjeto.transform, false);
        txtHudCapacidad = hudCapObj.AddComponent<TextMeshProUGUI>();
        txtHudCapacidad.text = "INVENTARIO [0/5]";
        txtHudCapacidad.fontStyle = FontStyles.Bold;
        txtHudCapacidad.fontSize = 22; // Enlarged title
        txtHudCapacidad.color = new Color(0.9f, 0.85f, 0.8f, 1f); // Weathered paper white

        // Distressed outline for title
        Outline titleOutline = hudCapObj.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0.05f, 0.05f, 0.05f, 0.8f);
        titleOutline.effectDistance = new Vector2(1.5f, -1.5f);

        RectTransform hudCapRect = hudCapObj.GetComponent<RectTransform>();
        hudCapRect.anchorMin = new Vector2(0f, 1f);
        hudCapRect.anchorMax = new Vector2(1f, 1f);
        hudCapRect.pivot = new Vector2(0.5f, 1f);
        hudCapRect.anchoredPosition = new Vector2(10, -5);
        hudCapRect.sizeDelta = new Vector2(-20, 30);

        // Horizontal Row of 5 Slots (4 Items + 1 Arrow) - Enlarged and spaced out
        for (int i = 0; i < 5; i++)
        {
            GameObject hudSlot = new GameObject("HUDSlot_" + i);
            hudSlot.transform.SetParent(hudPanelObjeto.transform, false);

            RectTransform slotRect = hudSlot.AddComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0f, 1f);
            slotRect.anchorMax = new Vector2(0f, 1f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(50 + (i * 95), -85); // Enlarged cell spacing & centered
            slotRect.sizeDelta = new Vector2(85, 85); // Enlarged slots

            Image slotImg = hudSlot.AddComponent<Image>();
            slotImg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f); // Dark scrap iron

            Outline slotOutline = hudSlot.AddComponent<Outline>();
            slotOutline.effectColor = new Color(0.35f, 0.35f, 0.35f, 0.6f); // Grey border
            slotOutline.effectDistance = new Vector2(1.5f, 1.5f);

            Button slotBtn = hudSlot.AddComponent<Button>();
            slotBtn.onClick.AddListener(ToggleInventario);

            if (i < 4)
            {
                // Item Icon inside HUD Slot
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(hudSlot.transform, false);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
                iconImg.gameObject.SetActive(false);

                RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(8, 8);
                iconRect.offsetMax = new Vector2(-8, -8);

                // Code/Quantity label text (bottom right)
                GameObject codeObj = new GameObject("TxtCode");
                codeObj.transform.SetParent(hudSlot.transform, false);
                TextMeshProUGUI txtCode = codeObj.AddComponent<TextMeshProUGUI>();
                txtCode.text = "";
                txtCode.alignment = TextAlignmentOptions.BottomRight;
                txtCode.fontSize = 15; // Enlarged label text
                txtCode.fontStyle = FontStyles.Bold;
                txtCode.color = new Color(0.9f, 0.88f, 0.85f, 1f);

                RectTransform codeRect = codeObj.GetComponent<RectTransform>();
                codeRect.anchorMin = Vector2.zero;
                codeRect.anchorMax = Vector2.one;
                codeRect.offsetMin = new Vector2(2, 2);
                codeRect.offsetMax = new Vector2(-5, -2);
            }
            else
            {
                // Slot 5: Arrow indicating "more objects"
                GameObject arrowObj = new GameObject("ArrowText");
                arrowObj.transform.SetParent(hudSlot.transform, false);
                TextMeshProUGUI arrowTxt = arrowObj.AddComponent<TextMeshProUGUI>();
                arrowTxt.text = ">>"; // Standard arrow symbol
                arrowTxt.fontStyle = FontStyles.Bold;
                arrowTxt.alignment = TextAlignmentOptions.Center;
                arrowTxt.fontSize = 28; // Enlarged arrow
                arrowTxt.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                ApplyFont(arrowTxt, true);

                RectTransform arrowRect = arrowObj.GetComponent<RectTransform>();
                arrowRect.anchorMin = Vector2.zero;
                arrowRect.anchorMax = Vector2.one;
                arrowRect.offsetMin = Vector2.zero;
                arrowRect.offsetMax = Vector2.zero;
            }

            hudSlots.Add(hudSlot);
        }

        // Hint Text below the HUD Slots
        GameObject hintObj = new GameObject("TxtHudHint");
        hintObj.transform.SetParent(hudPanelObjeto.transform, false);
        txtHudHint = hintObj.AddComponent<TextMeshProUGUI>();
        txtHudHint.text = "[Y] ABRIR INVENTARIO";
        txtHudHint.fontStyle = FontStyles.Bold;
        txtHudHint.fontSize = 14; // Enlarged hint text
        txtHudHint.color = new Color(1f, 1f, 1f, 0.75f); // White with 75% opacity

        Outline hintOutline = hintObj.AddComponent<Outline>();
        hintOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        hintOutline.effectDistance = new Vector2(1, -1);

        RectTransform hintRect = hintObj.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(0f, 0f);
        hintRect.anchoredPosition = new Vector2(10, 8); // Anchor to bottom of panel
        hintRect.sizeDelta = new Vector2(-20, 20);

        // Apply custom fonts to HUD
        ApplyFont(txtHudCapacidad, true);
        ApplyFont(txtHudHint, true);

        // 3. Create Main Inventory Grid Panel
        panelInventarioObjeto = new GameObject("PanelInventario");
        panelInventarioObjeto.transform.SetParent(canvasObjeto.transform, false);

        RectTransform panelRect = panelInventarioObjeto.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.anchoredPosition = new Vector2(-50, 0);
        panelRect.sizeDelta = new Vector2(500, 780);

        AplicarEstiloPostApocaliptico(panelInventarioObjeto);

        // 4. Create Panel Header Elements
        GameObject capObj = new GameObject("TxtCapacidad");
        capObj.transform.SetParent(panelInventarioObjeto.transform, false);
        txtCapacidad = capObj.AddComponent<TextMeshProUGUI>();
        txtCapacidad.text = "<color=#FF8C00>CAPACIDAD:</color> RANURAS DE INVENTARIO";
        txtCapacidad.fontStyle = FontStyles.Bold;
        txtCapacidad.fontSize = 18;
        txtCapacidad.color = Color.white;

        RectTransform capRect = capObj.GetComponent<RectTransform>();
        capRect.anchorMin = new Vector2(0f, 1f);
        capRect.anchorMax = new Vector2(1f, 1f);
        capRect.pivot = new Vector2(0.5f, 1f);
        capRect.anchoredPosition = new Vector2(25, -25);
        capRect.sizeDelta = new Vector2(-50, 30);

        // Weight status label
        GameObject weightObj = new GameObject("TxtTotalWeight");
        weightObj.transform.SetParent(panelInventarioObjeto.transform, false);
        txtTotalWeight = weightObj.AddComponent<TextMeshProUGUI>();
        txtTotalWeight.text = "RANURAS OCUPADAS: 0 / 5";
        txtTotalWeight.fontStyle = FontStyles.Bold;
        txtTotalWeight.fontSize = 15;
        txtTotalWeight.color = new Color(0.75f, 0.75f, 0.75f, 1f);

        RectTransform weightRect = weightObj.GetComponent<RectTransform>();
        weightRect.anchorMin = new Vector2(0f, 1f);
        weightRect.anchorMax = new Vector2(1f, 1f);
        weightRect.pivot = new Vector2(0.5f, 1f);
        weightRect.anchoredPosition = new Vector2(25, -55);
        weightRect.sizeDelta = new Vector2(-50, 25);

        // Rastrojero Cargo Tab/Bar
        GameObject tabObj = new GameObject("CargoTab");
        tabObj.transform.SetParent(panelInventarioObjeto.transform, false);
        
        RectTransform tabRect = tabObj.AddComponent<RectTransform>();
        tabRect.anchorMin = new Vector2(0f, 1f);
        tabRect.anchorMax = new Vector2(1f, 1f);
        tabRect.pivot = new Vector2(0.5f, 1f);
        tabRect.anchoredPosition = new Vector2(0, -90);
        tabRect.sizeDelta = new Vector2(-40, 35);

        Image tabImg = tabObj.AddComponent<Image>();
        tabImg.color = new Color(0.48f, 0.24f, 0.05f, 0.9f);
        
        Outline tabOutline = tabObj.AddComponent<Outline>();
        tabOutline.effectColor = new Color(0.3f, 0.15f, 0.03f, 0.8f);
        tabOutline.effectDistance = new Vector2(1, 1);

        GameObject tabTextObj = new GameObject("Text");
        tabTextObj.transform.SetParent(tabObj.transform, false);
        TextMeshProUGUI tabText = tabTextObj.AddComponent<TextMeshProUGUI>();
        tabText.text = "Carga del Rastrojero";
        tabText.fontStyle = FontStyles.Bold;
        tabText.alignment = TextAlignmentOptions.Center;
        tabText.fontSize = 16;
        tabText.color = new Color(0.95f, 0.9f, 0.85f, 1f);

        RectTransform tabTextRect = tabTextObj.GetComponent<RectTransform>();
        tabTextRect.anchorMin = Vector2.zero;
        tabTextRect.anchorMax = Vector2.one;
        tabTextRect.offsetMin = Vector2.zero;
        tabTextRect.offsetMax = Vector2.zero;

        // 5. Create Grid Layout Container
        gridContainerObjeto = new GameObject("GridContainer");
        gridContainerObjeto.transform.SetParent(panelInventarioObjeto.transform, false);

        RectTransform gridRect = gridContainerObjeto.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 1f);
        gridRect.anchorMax = new Vector2(0.5f, 1f);
        gridRect.pivot = new Vector2(0.5f, 1f);
        gridRect.anchoredPosition = new Vector2(0, -135);
        gridRect.sizeDelta = new Vector2(440, 440);

        GridLayoutGroup gridLayout = gridContainerObjeto.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(100, 75);
        gridLayout.spacing = new Vector2(10, 10);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 4;
        gridLayout.padding = new RectOffset(10, 10, 10, 10);

        // Build 20 Slots (4x5)
        for (int i = 0; i < 20; i++)
        {
            GameObject slot = CrearSlotUI(i);
            slotObjects.Add(slot);
        }

        // 6. Create Tooltip Info Panel
        tooltipObjeto = new GameObject("TooltipPanel");
        tooltipObjeto.transform.SetParent(panelInventarioObjeto.transform, false);

        RectTransform toolRect = tooltipObjeto.AddComponent<RectTransform>();
        toolRect.anchorMin = new Vector2(0.5f, 0f);
        toolRect.anchorMax = new Vector2(0.5f, 0f);
        toolRect.pivot = new Vector2(0.5f, 0f);
        toolRect.anchoredPosition = new Vector2(0, 105);
        toolRect.sizeDelta = new Vector2(440, 95);

        Image toolImg = tooltipObjeto.AddComponent<Image>();
        toolImg.color = new Color(0.06f, 0.06f, 0.06f, 0.98f);

        Outline toolOutline = tooltipObjeto.AddComponent<Outline>();
        toolOutline.effectColor = new Color(0.18f, 0.24f, 0.28f, 0.6f);
        toolOutline.effectDistance = new Vector2(1, 1);

        // Tooltip Name
        GameObject toolNameObj = new GameObject("TxtName");
        toolNameObj.transform.SetParent(tooltipObjeto.transform, false);
        txtTooltipName = toolNameObj.AddComponent<TextMeshProUGUI>();
        txtTooltipName.text = "SELECCIONA UN OBJETO";
        txtTooltipName.fontStyle = FontStyles.Bold;
        txtTooltipName.fontSize = 15;
        txtTooltipName.color = new Color(0.0f, 0.9f, 1.0f, 1f);

        RectTransform toolNameRect = toolNameObj.GetComponent<RectTransform>();
        toolNameRect.anchorMin = new Vector2(0f, 1f);
        toolNameRect.anchorMax = new Vector2(1f, 1f);
        toolNameRect.pivot = new Vector2(0.5f, 1f);
        toolNameRect.anchoredPosition = new Vector2(15, -12);
        toolNameRect.sizeDelta = new Vector2(-30, 22);

        // Tooltip Description
        GameObject toolDescObj = new GameObject("TxtDesc");
        toolDescObj.transform.SetParent(tooltipObjeto.transform, false);
        txtTooltipDesc = toolDescObj.AddComponent<TextMeshProUGUI>();
        txtTooltipDesc.text = "Haz clic en una ranura del inventario para ver los detalles.";
        txtTooltipDesc.fontSize = 12;
        txtTooltipDesc.color = new Color(0.8f, 0.8f, 0.78f, 1f);

        RectTransform toolDescRect = toolDescObj.GetComponent<RectTransform>();
        toolDescRect.anchorMin = Vector2.zero;
        toolDescRect.anchorMax = Vector2.one;
        toolDescRect.offsetMin = new Vector2(15, 10);
        toolDescRect.offsetMax = new Vector2(-15, -35);

        // 7. Create Footer Elements
        GameObject footTitleObj = new GameObject("TxtFootTitle");
        footTitleObj.transform.SetParent(panelInventarioObjeto.transform, false);
        TextMeshProUGUI footTitle = footTitleObj.AddComponent<TextMeshProUGUI>();
        footTitle.text = "GRILLA DE INVENTARIO";
        footTitle.fontStyle = FontStyles.Bold;
        footTitle.fontSize = 13;
        footTitle.color = new Color(0.36f, 0.61f, 0.78f, 0.8f);

        RectTransform footTitleRect = footTitleObj.GetComponent<RectTransform>();
        footTitleRect.anchorMin = new Vector2(0f, 0f);
        footTitleRect.anchorMax = new Vector2(1f, 0f);
        footTitleRect.pivot = new Vector2(0.5f, 0f);
        footTitleRect.anchoredPosition = new Vector2(30, 70);
        footTitleRect.sizeDelta = new Vector2(-60, 20);

        // Apply custom fonts to Main Panel
        ApplyFont(txtCapacidad, true);
        ApplyFont(txtTotalWeight, true);
        ApplyFont(tabText, true);
        ApplyFont(txtTooltipName, true);
        ApplyFont(txtTooltipDesc, true);
        ApplyFont(footTitle, true);

        // Keep Button (A) - Removed as per simplified inventory UI requirements
        // GameObject btnKeepObj = CrearBotonAccion("BtnKeep", "MANTENER", new Color(0.2f, 0.45f, 0.2f, 1f), -165);
        // btnKeepObj.GetComponent<Button>().onClick.AddListener(CloseInventario);

        // Discard Button (X) - Removed as per simplified inventory UI requirements
        // GameObject btnDiscardObj = CrearBotonAccion("BtnDiscard", "DESCARTAR", new Color(0.18f, 0.24f, 0.35f, 1f), -55);
        // btnDiscardObj.GetComponent<Button>().onClick.AddListener(DiscardSelectedItem);

        // Reorganize Button (Y) - Removed as per simplified inventory UI requirements
        // GameObject btnReorgObj = CrearBotonAccion("BtnReorganize", "ORDENAR", new Color(0.45f, 0.4f, 0.15f, 1f), 55);
        // btnReorgObj.GetComponent<Button>().onClick.AddListener(ReorganizeInventory);

        // Lock Button (Bloquear) - Only available action button
        GameObject btnLockObj = CrearBotonAccion("BtnLock", "Bloquear objeto", new Color(0.55f, 0.15f, 0.15f, 1f), 0f);
        btnLockObj.GetComponent<Button>().onClick.AddListener(ToggleLockSelectedItem);

        // Adjust lock button width to fit long text
        RectTransform btnLockRect = btnLockObj.GetComponent<RectTransform>();
        if (btnLockRect != null)
        {
            btnLockRect.sizeDelta = new Vector2(180f, 36f);
        }

        // Find the label text inside the Lock Button
        Transform lockLabelTrans = btnLockObj.transform.Find("Text");
        txtBtnLockLabel = lockLabelTrans != null ? lockLabelTrans.GetComponent<TextMeshProUGUI>() : null;

        // 8. Create Fuel Bar (barradenafta) at bottom-right of the Screen
        barraNaftaObjeto = new GameObject("FuelBarContainer");
        RectTransform containerRect = barraNaftaObjeto.AddComponent<RectTransform>();
        barraNaftaObjeto.transform.SetParent(canvasObjeto.transform, false);
        containerRect.localScale = Vector3.one;
        containerRect.localPosition = Vector3.zero;

        containerRect.anchorMin = new Vector2(1f, 0f); // Bottom-right anchor
        containerRect.anchorMax = new Vector2(1f, 0f);
        containerRect.pivot = new Vector2(1f, 0f); // Bottom-right pivot
        containerRect.anchoredPosition = new Vector2(-50f, 50f); // 50px offset from margins

        Sprite fuelSprite = CargarSpriteDesdeResources("Sprites/barradenafta");
        float targetWidth = 350f;
        float targetHeight = 122.6f;
        if (fuelSprite != null)
        {
            float originalWidth = fuelSprite.rect.width;
            float originalHeight = fuelSprite.rect.height;
            float aspect = originalWidth / (originalHeight > 0f ? originalHeight : 1f);
            targetHeight = targetWidth / (aspect > 0f ? aspect : 1f);
        }
        containerRect.sizeDelta = new Vector2(targetWidth, targetHeight);

        // A. Create Cyan Liquid Bar (Child index 0, renders behind)
        GameObject liquidObj = new GameObject("FuelLiquid");
        liquidBarRect = liquidObj.AddComponent<RectTransform>();
        liquidObj.transform.SetParent(barraNaftaObjeto.transform, false);
        liquidBarRect.localScale = Vector3.one;
        liquidBarRect.localPosition = Vector3.zero;

        liquidBarRect.anchorMin = new Vector2(0f, 0f);
        liquidBarRect.anchorMax = new Vector2(0f, 0f);
        liquidBarRect.pivot = new Vector2(0f, 0.5f); // Left-center pivot for easy horizontal scaling

        maxFuelWidth = 242f;
        float liquidPosX = 82f;
        float liquidPosY = 33f;
        float liquidHeight = 48f;

        liquidBarRect.anchoredPosition = new Vector2(liquidPosX, liquidPosY);
        liquidBarRect.sizeDelta = new Vector2(maxFuelWidth * currentFuel, liquidHeight);

        Image liquidImg = liquidObj.AddComponent<Image>();
        liquidImg.color = new Color(0f, 0.72f, 1f, 1f); // Celeste / Cyan color
        liquidImg.type = Image.Type.Simple;

        // B. Create Frame Image (Child index 1, renders in front)
        GameObject frameObj = new GameObject("FuelFrame");
        RectTransform frameRect = frameObj.AddComponent<RectTransform>();
        frameObj.transform.SetParent(barraNaftaObjeto.transform, false);
        frameRect.localScale = Vector3.one;
        frameRect.localPosition = Vector3.zero;

        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;

        Image frameImg = frameObj.AddComponent<Image>();
        if (fuelSprite != null)
        {
            frameImg.sprite = fuelSprite;
            frameImg.type = Image.Type.Simple;
            frameImg.preserveAspect = true;
        }
        else
        {
            frameImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            Debug.LogWarning("barradenafta sprite could not be loaded from Resources/Sprites/barradenafta");
        }



        // 9. Create Game Over Panel
        panelGameOverObjeto = new GameObject("GameOverPanel");
        panelGameOverObjeto.transform.SetParent(canvasObjeto.transform, false);

        RectTransform goRect = panelGameOverObjeto.AddComponent<RectTransform>();
        goRect.anchorMin = Vector2.zero;
        goRect.anchorMax = Vector2.one;
        goRect.offsetMin = Vector2.zero;
        goRect.offsetMax = Vector2.zero;

        Image goBg = panelGameOverObjeto.AddComponent<Image>();
        goBg.color = new Color(0.08f, 0.08f, 0.08f, 0.96f); // Semi-transparent dark overlay

        // Danger Red strip at top of Game Over panel
        GameObject goStrip = new GameObject("Strip");
        goStrip.transform.SetParent(panelGameOverObjeto.transform, false);
        Image stripImg = goStrip.AddComponent<Image>();
        stripImg.color = new Color(0.85f, 0.15f, 0.15f, 0.8f); // Red strip
        RectTransform stripRect = goStrip.GetComponent<RectTransform>();
        stripRect.anchorMin = new Vector2(0f, 0.7f);
        stripRect.anchorMax = new Vector2(1f, 0.72f);
        stripRect.offsetMin = Vector2.zero;
        stripRect.offsetMax = Vector2.zero;

        // Title Text: GAME OVER
        GameObject titleObj = new GameObject("TxtTitle");
        titleObj.transform.SetParent(panelGameOverObjeto.transform, false);
        TextMeshProUGUI txtTitle = titleObj.AddComponent<TextMeshProUGUI>();
        txtTitle.text = "FIN DEL CAMINO"; // GAME OVER
        txtTitle.alignment = TextAlignmentOptions.Center;
        txtTitle.fontSize = 72;
        txtTitle.fontStyle = FontStyles.Bold;
        txtTitle.color = new Color(0.9f, 0.2f, 0.2f, 1f); // Danger Red
        ApplyFont(txtTitle, true);

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.55f);
        titleRect.anchorMax = new Vector2(1f, 0.65f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        // Subtitle Text: Te quedaste sin nafta
        GameObject subObj = new GameObject("TxtSubtitle");
        subObj.transform.SetParent(panelGameOverObjeto.transform, false);
        TextMeshProUGUI txtSub = subObj.AddComponent<TextMeshProUGUI>();
        txtSub.text = "Te quedaste sin nafta en medio del páramo.";
        txtSub.alignment = TextAlignmentOptions.Center;
        txtSub.fontSize = 28;
        txtSub.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        ApplyFont(txtSub, true);

        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0.45f);
        subRect.anchorMax = new Vector2(1f, 0.52f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;

        // Button container
        GameObject goButtons = new GameObject("ButtonsContainer");
        goButtons.transform.SetParent(panelGameOverObjeto.transform, false);
        RectTransform goButtonsRect = goButtons.AddComponent<RectTransform>();
        goButtonsRect.anchorMin = new Vector2(0.5f, 0.3f);
        goButtonsRect.anchorMax = new Vector2(0.5f, 0.3f);
        goButtonsRect.sizeDelta = new Vector2(500, 150);
        goButtonsRect.anchoredPosition = Vector2.zero;

        // Retry Button
        GameObject btnRetryObj = CrearBotonAccionGameOver("BtnRetry", "REINTENTAR", new Color(0.2f, 0.45f, 0.2f, 1f), goButtons.transform, -120);
        btnRetryObj.GetComponent<Button>().onClick.AddListener(RestartGameLevel);

        // Menu Button
        GameObject btnMenuObj = CrearBotonAccionGameOver("BtnMenu", "MENU PRINCIPAL", new Color(0.45f, 0.4f, 0.15f, 1f), goButtons.transform, 120);
        btnMenuObj.GetComponent<Button>().onClick.AddListener(GoToMainMenu);

        // Start closed
        panelInventarioObjeto.SetActive(false);
        panelGameOverObjeto.SetActive(false);
    }

    private GameObject CrearSlotUI(int index)
    {
        GameObject slot = new GameObject("Slot_" + index);
        slot.transform.SetParent(gridContainerObjeto.transform, false);

        Image slotImg = slot.AddComponent<Image>();
        slotImg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        Outline slotOutline = slot.AddComponent<Outline>();
        slotOutline.effectColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
        slotOutline.effectDistance = new Vector2(1.5f, 1.5f);

        Button btn = slot.AddComponent<Button>();
        btn.onClick.AddListener(() => SelectSlot(index));

        // Slot hover colors
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        cb.highlightedColor = new Color(0.22f, 0.22f, 0.22f, 0.95f);
        cb.pressedColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        btn.colors = cb;

        // Custom code text (inside slot)
        GameObject codeObj = new GameObject("TxtCode");
        codeObj.transform.SetParent(slot.transform, false);
        TextMeshProUGUI txtCode = codeObj.AddComponent<TextMeshProUGUI>();
        txtCode.text = "";
        txtCode.fontStyle = FontStyles.Bold;
        txtCode.alignment = TextAlignmentOptions.Center;
        txtCode.fontSize = 13;
        txtCode.color = new Color(0.85f, 0.82f, 0.8f, 1f);

        RectTransform codeRect = codeObj.GetComponent<RectTransform>();
        codeRect.anchorMin = Vector2.zero;
        codeRect.anchorMax = Vector2.one;
        codeRect.offsetMin = new Vector2(5, 18);
        codeRect.offsetMax = new Vector2(-5, -18);

        // Custom icon image
        GameObject iconObj = new GameObject("ImgIcon");
        iconObj.transform.SetParent(slot.transform, false);
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.color = Color.white;
        iconImg.preserveAspect = true;
        iconImg.gameObject.SetActive(false);

        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(12, 18);
        iconRect.offsetMax = new Vector2(-12, -18);

        // Weight text label (bottom right)
        GameObject weightObj = new GameObject("TxtWeight");
        weightObj.transform.SetParent(slot.transform, false);
        TextMeshProUGUI txtWeight = weightObj.AddComponent<TextMeshProUGUI>();
        txtWeight.text = "";
        txtWeight.alignment = TextAlignmentOptions.BottomRight;
        txtWeight.fontSize = 10;
        txtWeight.color = new Color(0.65f, 0.65f, 0.63f, 1f);

        RectTransform weightRect = weightObj.GetComponent<RectTransform>();
        weightRect.anchorMin = Vector2.zero;
        weightRect.anchorMax = Vector2.one;
        weightRect.offsetMin = new Vector2(2, 2);
        weightRect.offsetMax = new Vector2(-5, -2);

        // Quantity indicator label (top right)
        GameObject quantityObj = new GameObject("TxtQuantity");
        quantityObj.transform.SetParent(slot.transform, false);
        TextMeshProUGUI txtQuantity = quantityObj.AddComponent<TextMeshProUGUI>();
        txtQuantity.text = "";
        txtQuantity.fontStyle = FontStyles.Bold;
        txtQuantity.alignment = TextAlignmentOptions.TopRight;
        txtQuantity.fontSize = 13;
        txtQuantity.color = new Color(0.9f, 0.7f, 0.2f, 0.9f);

        RectTransform quantityRect = quantityObj.GetComponent<RectTransform>();
        quantityRect.anchorMin = Vector2.zero;
        quantityRect.anchorMax = Vector2.one;
        quantityRect.offsetMin = new Vector2(2, 2);
        quantityRect.offsetMax = new Vector2(-6, -3);

        ApplyFont(txtQuantity, true);

        // Lock Indicator label (top left / bottom left)
        GameObject lockObj = new GameObject("TxtLockIndicator");
        lockObj.transform.SetParent(slot.transform, false);
        TextMeshProUGUI txtLock = lockObj.AddComponent<TextMeshProUGUI>();
        txtLock.text = "";
        txtLock.fontStyle = FontStyles.Bold;
        txtLock.alignment = TextAlignmentOptions.TopLeft;
        txtLock.fontSize = 11;
        txtLock.color = new Color(0.9f, 0.2f, 0.2f, 1f); // Red indicator
        ApplyFont(txtLock, true);

        RectTransform lockRect = lockObj.GetComponent<RectTransform>();
        lockRect.anchorMin = Vector2.zero;
        lockRect.anchorMax = Vector2.one;
        lockRect.offsetMin = new Vector2(5, 2);
        lockRect.offsetMax = new Vector2(-5, -2);

        return slot;
    }

    private GameObject CrearBotonAccion(string name, string text, Color cbNormal, float xOffset)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(panelInventarioObjeto.transform, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(xOffset, 20);
        rect.sizeDelta = new Vector2(105, 36);

        Image img = buttonObj.AddComponent<Image>();
        img.color = cbNormal;

        Outline outline = buttonObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.08f, 0.08f, 0.08f, 0.8f);
        outline.effectDistance = new Vector2(1, 1);

        Button btn = buttonObj.AddComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.normalColor = cbNormal;
        cb.highlightedColor = cbNormal + new Color(0.1f, 0.1f, 0.1f, 0f);
        cb.pressedColor = cbNormal - new Color(0.08f, 0.08f, 0.08f, 0f);
        btn.colors = cb;

        GameObject labelObj = new GameObject("Text");
        labelObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 13;
        label.color = new Color(0.95f, 0.95f, 0.9f, 1f);
        ApplyFont(label, true);

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return buttonObj;
    }

    private void ToggleInventario()
    {
        if (panelInventarioObjeto != null)
        {
            panelInventarioObjeto.SetActive(!panelInventarioObjeto.activeSelf);
            UpdateInventoryUI();
        }
    }

    private void CloseInventario()
    {
        if (panelInventarioObjeto != null)
        {
            panelInventarioObjeto.SetActive(false);
            UpdateInventoryUI();
        }
    }

    private void DiscardSelectedItem()
    {
        if (selectedIndex >= 0 && selectedIndex < items.Count)
        {
            if (items[selectedIndex].isLocked)
            {
                Debug.LogWarning("No se puede descartar un objeto bloqueado.");
                return;
            }
            items.RemoveAt(selectedIndex);
            selectedIndex = -1;
            UpdateInventoryUI();
        }
    }

    private void ReorganizeInventory()
    {
        items.Sort((x, y) => y.weight.CompareTo(x.weight));
        selectedIndex = -1;
        UpdateInventoryUI();
    }

    private void SelectSlot(int index)
    {
        if (index < items.Count)
        {
            selectedIndex = index;
        }
        else
        {
            selectedIndex = -1;
        }
        UpdateInventoryUI();
    }

    private void UpdateInventoryUI()
    {
        // 1. Calculate total weight
        float totalWeight = 0;
        foreach (var item in items)
        {
            totalWeight += item.weight;
        }
        
        // Update both HUD and Main Panel capacity texts to reflect slots instead of weight
        txtHudCapacidad.text = $"INVENTARIO [{items.Count}/5]";
        txtCapacidad.text = $"<color=#FF8C00>CAPACIDAD:</color> RANURAS DE INVENTARIO";
        txtTotalWeight.text = $"RANURAS OCUPADAS: {items.Count} / 5";

        // Update HUD hint text based on whether the main inventory panel is open
        if (txtHudHint != null && panelInventarioObjeto != null)
        {
            txtHudHint.text = panelInventarioObjeto.activeSelf ? "[Y] CERRAR INVENTARIO" : "[Y] ABRIR INVENTARIO";
        }

        // 2. Update HUD Horizontal Slots (Slots 0 to 3 for items, Slot 4 is the arrow)
        for (int i = 0; i < 4; i++)
        {
            GameObject hudSlot = hudSlots[i];
            
            Transform iconTrans = hudSlot.transform.Find("Icon");
            Image iconImg = iconTrans != null ? iconTrans.GetComponent<Image>() : null;

            Transform codeTrans = hudSlot.transform.Find("TxtCode");
            TextMeshProUGUI txtCode = codeTrans != null ? codeTrans.GetComponent<TextMeshProUGUI>() : null;

            if (i < items.Count)
            {
                Item item = items[i];
                if (item.customIcon != null)
                {
                    if (txtCode != null) txtCode.text = item.code;
                    if (iconImg != null)
                    {
                        iconImg.sprite = item.customIcon;
                        iconImg.gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (iconImg != null) iconImg.gameObject.SetActive(false);
                    if (txtCode != null) txtCode.text = item.code;
                }
            }
            else
            {
                // Slot is empty
                if (iconImg != null) iconImg.gameObject.SetActive(false);
                if (txtCode != null) txtCode.text = "";
            }
        }

        // 3. Update main panel Grid Slots
        for (int i = 0; i < 20; i++)
        {
            GameObject slot = slotObjects[i];
            Outline outline = slot.GetComponent<Outline>();
            
            Transform txtCodeTrans = slot.transform.Find("TxtCode");
            TextMeshProUGUI txtCode = txtCodeTrans != null ? txtCodeTrans.GetComponent<TextMeshProUGUI>() : null;
            
            Transform iconTrans = slot.transform.Find("ImgIcon");
            Image iconImg = iconTrans != null ? iconTrans.GetComponent<Image>() : null;
            
            Transform txtWeightTrans = slot.transform.Find("TxtWeight");
            TextMeshProUGUI txtWeight = txtWeightTrans != null ? txtWeightTrans.GetComponent<TextMeshProUGUI>() : null;
            
            Transform txtQuantityTrans = slot.transform.Find("TxtQuantity");
            TextMeshProUGUI txtQuantity = txtQuantityTrans != null ? txtQuantityTrans.GetComponent<TextMeshProUGUI>() : null;

            Transform lockIndicatorTrans = slot.transform.Find("TxtLockIndicator");
            TextMeshProUGUI txtLockIndicator = lockIndicatorTrans != null ? lockIndicatorTrans.GetComponent<TextMeshProUGUI>() : null;

            if (i < items.Count)
            {
                Item item = items[i];

                if (item.customIcon != null)
                {
                    if (txtCode != null) txtCode.gameObject.SetActive(false);
                    if (iconImg != null)
                    {
                        iconImg.sprite = item.customIcon;
                        iconImg.gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (iconImg != null) iconImg.gameObject.SetActive(false);
                    if (txtCode != null)
                    {
                        txtCode.text = item.code;
                        txtCode.gameObject.SetActive(true);
                    }
                }

                if (txtWeight != null) txtWeight.text = $"{item.weight:F1}kg";
                if (txtQuantity != null) txtQuantity.text = item.quantity.ToString();
                if (txtLockIndicator != null) txtLockIndicator.text = item.isLocked ? "BLOQ" : "";
            }
            else
            {
                if (txtCode != null) txtCode.text = "";
                if (iconImg != null) iconImg.gameObject.SetActive(false);
                if (txtWeight != null) txtWeight.text = "";
                if (txtQuantity != null) txtQuantity.text = "";
                if (txtLockIndicator != null) txtLockIndicator.text = "";
            }

            if (i == selectedIndex && i < items.Count)
            {
                if (outline != null)
                {
                    outline.effectColor = new Color(0.0f, 0.9f, 1.0f, 0.95f);
                    outline.effectDistance = new Vector2(2f, 2f);
                }
            }
            else
            {
                if (outline != null)
                {
                    if (i < items.Count && items[i].isLocked)
                    {
                        outline.effectColor = new Color(0.9f, 0.2f, 0.2f, 0.8f); // Red outline for locked
                        outline.effectDistance = new Vector2(2f, 2f);
                    }
                    else
                    {
                        outline.effectColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
                        outline.effectDistance = new Vector2(1.5f, 1.5f);
                    }
                }
            }
        }

        // 4. Update Tooltip Info Panel
        if (selectedIndex >= 0 && selectedIndex < items.Count)
        {
            Item item = items[selectedIndex];
            txtTooltipName.text = item.name.ToUpper();
            txtTooltipDesc.text = $"{item.description}\nPeso: {item.weight:F1} kg | Cantidad: {item.quantity}";
            if (txtBtnLockLabel != null)
            {
                txtBtnLockLabel.text = item.isLocked ? "Desbloquear objeto" : "Bloquear objeto";
            }
        }
        else
        {
            txtTooltipName.text = "SELECCIONA UN OBJETO";
            txtTooltipDesc.text = "Haz clic en una ranura del inventario para ver los detalles.";
            if (txtBtnLockLabel != null)
            {
                txtBtnLockLabel.text = "Bloquear objeto";
            }
        }
    }

    private void AplicarEstiloPostApocaliptico(GameObject panel)
    {
        Image img = panel.GetComponent<Image>();
        if (img == null)
        {
            img = panel.AddComponent<Image>();
        }
        img.color = new Color(0.09f, 0.08f, 0.08f, 0.98f);

        Outline outline = panel.GetComponent<Outline>();
        if (outline == null)
        {
            outline = panel.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0.5f, 0.25f, 0.1f, 0.8f);
        outline.effectDistance = new Vector2(2, 2);

        Shadow shadow = panel.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = panel.AddComponent<Shadow>();
        }
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(4, -4);

        AgregarCintaPeligro(panel);
        AgregarRemaches(panel);
    }

    private void AgregarCintaPeligro(GameObject panel)
    {
        GameObject yellowStrip = new GameObject("HazardStrip");
        yellowStrip.transform.SetParent(panel.transform, false);

        RectTransform rect = yellowStrip.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -6);
        rect.sizeDelta = new Vector2(-20, 10);

        Image img = yellowStrip.AddComponent<Image>();
        img.color = new Color(0.85f, 0.65f, 0.1f, 0.9f);

        GameObject stripesObj = new GameObject("StripesText");
        stripesObj.transform.SetParent(yellowStrip.transform, false);

        TextMeshProUGUI stripesText = stripesObj.AddComponent<TextMeshProUGUI>();
        stripesText.text = "<b>/ / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / /</b>";
        stripesText.alignment = TextAlignmentOptions.Center;
        stripesText.fontSize = 8;
        stripesText.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);

        RectTransform stripesRect = stripesObj.GetComponent<RectTransform>();
        stripesRect.anchorMin = Vector2.zero;
        stripesRect.anchorMax = Vector2.one;
        stripesRect.offsetMin = Vector2.zero;
        stripesRect.offsetMax = Vector2.zero;
    }

    private void AgregarRemaches(GameObject panel)
    {
        Vector2[] offsets = new Vector2[] {
            new Vector2(12, -18),
            new Vector2(-12, -18),
            new Vector2(12, 12),
            new Vector2(-12, 12)
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
            r.sizeDelta = new Vector2(8, 8);

            Image img = rivet.AddComponent<Image>();
            img.color = new Color(0.35f, 0.32f, 0.3f, 1f);

            Outline o = rivet.AddComponent<Outline>();
            o.effectColor = new Color(0.1f, 0.08f, 0.05f, 0.8f);
            o.effectDistance = new Vector2(1, -1);
        }
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

    private Item GetTemplateByName(string itemName)
    {
        foreach (var t in itemTemplates)
        {
            if (t.name.ToLower() == itemName.ToLower())
            {
                return t;
            }
        }
        return null;
    }

    public bool HasItem(string itemName, int minQuantity = 1)
    {
        foreach (var item in items)
        {
            if (item.name.ToLower() == itemName.ToLower())
            {
                if (item.isLocked) return false; // Locked items cannot be used for trading!
                return item.quantity >= minQuantity;
            }
        }
        return false;
    }

    public void RemoveItem(string itemName, int quantityToRemove = 1)
    {
        Item target = null;
        foreach (var item in items)
        {
            if (item.name.ToLower() == itemName.ToLower())
            {
                if (item.isLocked) continue; // Skip locked items!
                target = item;
                break;
            }
        }

        if (target != null)
        {
            target.quantity -= quantityToRemove;
            if (target.quantity <= 0)
            {
                items.Remove(target);
            }
            UpdateInventoryUI();
        }
    }

    public void AddItem(string itemName, int quantityToAdd = 1)
    {
        if (itemName.ToLower() == "nafta")
        {
            float currentLiters = CurrentFuel * 75f;
            float newLiters = Mathf.Clamp(currentLiters + quantityToAdd, 0f, 75f);
            CurrentFuel = newLiters / 75f;
            return; // Fuel is not an item, only refills the bar!
        }

        // First, check if we already have this item. If so, increase quantity.
        foreach (var item in items)
        {
            if (item.name.ToLower() == itemName.ToLower())
            {
                item.quantity += quantityToAdd;
                UpdateInventoryUI();
                return;
            }
        }

        // If not, load from template
        Item temp = GetTemplateByName(itemName);
        if (temp != null)
        {
            if (items.Count >= 5)
            {
                Debug.LogWarning("Inventario lleno. No se puede agregar: " + itemName);
                return;
            }
            items.Add(new Item
            {
                code = temp.code,
                name = temp.name,
                weight = temp.weight,
                type = temp.type,
                description = temp.description,
                customIcon = temp.customIcon,
                quantity = quantityToAdd
            });
            UpdateInventoryUI();
        }
        else
        {
            Debug.LogError("No template found for item: " + itemName);
        }
    }

    private GameObject CrearBotonAccionGameOver(string btnName, string label, Color bgColor, Transform parent, float xOffset)
    {
        GameObject btnObj = new GameObject(btnName);
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 50);
        rect.anchoredPosition = new Vector2(xOffset, 0);

        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;

        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontSize = 16;
        txt.fontStyle = FontStyles.Bold;
        txt.color = Color.white;
        ApplyFont(txt, true);

        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        return btnObj;
    }

    private void ToggleLockSelectedItem()
    {
        if (selectedIndex >= 0 && selectedIndex < items.Count)
        {
            items[selectedIndex].isLocked = !items[selectedIndex].isLocked;
            UpdateInventoryUI();
        }
    }

    private void RestartGameLevel()
    {
        CurrentFuel = 1.0f;
        NPCInteraction.tratoTerminado = false;
        NPCInteraction.tratoVagabundoTerminado = false;
        NPCInteraction.tratoNpc3Terminado = false;
        if (panelGameOverObjeto != null)
        {
            panelGameOverObjeto.SetActive(false);
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene("nivel_1_ypf");
    }

    private void GoToMainMenu()
    {
        CurrentFuel = 1.0f;
        NPCInteraction.tratoTerminado = false;
        NPCInteraction.tratoVagabundoTerminado = false;
        NPCInteraction.tratoNpc3Terminado = false;
        if (panelGameOverObjeto != null)
        {
            panelGameOverObjeto.SetActive(false);
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene("menu");
    }
}
