using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ZombieApocalypse.Player;
using ZombieApocalypse.Progression;

namespace ZombieApocalypse.UI
{
    /// <summary>
    /// UI overlay controller for Player Level, XP progress bar, Available Skill Points, and Perk upgrade cards.
    /// Accessible via hotkey (P key) or menu button. Manages gameplay input gating, cursor locking,
    /// and immediate visual state updates when perks are upgraded.
    /// 
    /// ATTACH TO: Gameplay Canvas Progression UI root GameObject.
    /// </summary>
    public class ProgressionUIController : MonoBehaviour
    {
        public static ProgressionUIController Instance { get; private set; }

        [Header("Main Panels")]
        [SerializeField] private GameObject progressionPanel;
        [SerializeField] private Button closeButton;

        [Header("Header & Level UI")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI xpText;
        [SerializeField] private Slider xpProgressBar;
        [SerializeField] private TextMeshProUGUI skillPointsText;

        [Header("Perk Upgrade Cards")]
        [SerializeField] private TextMeshProUGUI combatLevelText;
        [SerializeField] private TextMeshProUGUI combatEffectText;
        [SerializeField] private Button combatUpgradeButton;

        [SerializeField] private TextMeshProUGUI survivalLevelText;
        [SerializeField] private TextMeshProUGUI survivalEffectText;
        [SerializeField] private Button survivalUpgradeButton;

        [SerializeField] private TextMeshProUGUI scavengingLevelText;
        [SerializeField] private TextMeshProUGUI scavengingEffectText;
        [SerializeField] private Button scavengingUpgradeButton;

        [SerializeField] private TextMeshProUGUI craftingLevelText;
        [SerializeField] private TextMeshProUGUI craftingEffectText;
        [SerializeField] private Button craftingUpgradeButton;

        // Player & System references
        private PlayerProgressionSystem progressionSystem;
        private PlayerInputHandler inputHandler;
        private PlayerHealth playerHealth;

        private bool isOpen = false;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            FindAndBindPlayer();
            EnsureUIStructure();

            if (closeButton != null) closeButton.onClick.AddListener(CloseUI);

            if (combatUpgradeButton != null) combatUpgradeButton.onClick.AddListener(() => UpgradeSkill(SkillCategory.Combat));
            if (survivalUpgradeButton != null) survivalUpgradeButton.onClick.AddListener(() => UpgradeSkill(SkillCategory.Survival));
            if (scavengingUpgradeButton != null) scavengingUpgradeButton.onClick.AddListener(() => UpgradeSkill(SkillCategory.Scavenging));
            if (craftingUpgradeButton != null) craftingUpgradeButton.onClick.AddListener(() => UpgradeSkill(SkillCategory.Crafting));

            if (progressionPanel != null)
            {
                progressionPanel.SetActive(false);
            }
        }

        public void FindAndBindPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                inputHandler = player.GetComponent<PlayerInputHandler>();
                playerHealth = player.GetComponent<PlayerHealth>();
            }

            progressionSystem = PlayerProgressionSystem.Instance;
            if (progressionSystem == null) progressionSystem = FindObjectOfType<PlayerProgressionSystem>();

            if (progressionSystem != null)
            {
                progressionSystem.OnProgressionUpdated -= RefreshUI;
                progressionSystem.OnProgressionUpdated += RefreshUI;
            }
        }

        private void OnDestroy()
        {
            if (progressionSystem != null)
            {
                progressionSystem.OnProgressionUpdated -= RefreshUI;
            }
        }

        private void Update()
        {
            if (playerHealth != null && playerHealth.IsDead)
            {
                if (isOpen) CloseUI();
                return;
            }

            // Check P hotkey to toggle Progression UI
            if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            {
                ToggleUI();
            }

            if (!isOpen) return;

            // Close hotkey: Escape
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseUI();
            }
        }

        public void ToggleUI()
        {
            if (isOpen) CloseUI();
            else OpenUI();
        }

        public void OpenUI()
        {
            if (playerHealth != null && playerHealth.IsDead) return;

            FindAndBindPlayer();

            isOpen = true;
            if (progressionPanel != null) progressionPanel.SetActive(true);

            if (inputHandler != null)
            {
                inputHandler.IsInventoryOpen = true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            RefreshUI();
        }

        public void CloseUI()
        {
            isOpen = false;
            if (progressionPanel != null) progressionPanel.SetActive(false);

            if (inputHandler != null)
            {
                inputHandler.IsInventoryOpen = false;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void RefreshUI()
        {
            if (progressionSystem == null) FindAndBindPlayer();
            if (progressionSystem == null) return;

            int level = progressionSystem.CurrentLevel;
            int curXP = progressionSystem.CurrentXP;
            int reqXP = progressionSystem.RequiredXPForNextLevel;
            int totalXP = progressionSystem.TotalXP;
            int points = progressionSystem.SkillPoints;

            if (levelText != null) levelText.text = $"PLAYER LEVEL {level}";
            if (xpText != null) xpText.text = reqXP == int.MaxValue ? "MAX LEVEL" : $"{curXP} / {reqXP} XP  (Total: {totalXP})";
            if (xpProgressBar != null && reqXP > 0)
            {
                xpProgressBar.value = reqXP == int.MaxValue ? 1.0f : Mathf.Clamp01((float)curXP / reqXP);
            }

            if (skillPointsText != null) skillPointsText.text = $"SKILL POINTS AVAILABLE: <color=#FFC107>{points}</color>";

            // Update Perk Cards
            UpdateSkillCard(SkillCategory.Combat, combatLevelText, combatEffectText, combatUpgradeButton);
            UpdateSkillCard(SkillCategory.Survival, survivalLevelText, survivalEffectText, survivalUpgradeButton);
            UpdateSkillCard(SkillCategory.Scavenging, scavengingLevelText, scavengingEffectText, scavengingUpgradeButton);
            UpdateSkillCard(SkillCategory.Crafting, craftingLevelText, craftingEffectText, craftingUpgradeButton);
        }

        private void UpdateSkillCard(SkillCategory category, TextMeshProUGUI levelTxt, TextMeshProUGUI effectTxt, Button btn)
        {
            if (progressionSystem == null) return;

            int lvl = progressionSystem.GetSkillLevel(category);
            int maxLvl = 5;
            bool isMax = lvl >= maxLvl;

            if (levelTxt != null) levelTxt.text = isMax ? $"Level {lvl} / {maxLvl} (MAX)" : $"Level {lvl} / {maxLvl}";

            if (effectTxt != null)
            {
                switch (category)
                {
                    case SkillCategory.Combat:
                        float dmgPct = lvl * 5f;
                        effectTxt.text = $"Weapon Damage: +{dmgPct:F0}%";
                        break;
                    case SkillCategory.Survival:
                        float decayPct = lvl * 5f;
                        effectTxt.text = $"Hunger/Thirst Decay: -{decayPct:F0}%";
                        break;
                    case SkillCategory.Scavenging:
                        float lootPct = lvl * 10f;
                        effectTxt.text = $"Extra Resource Yield: +{lootPct:F0}%";
                        break;
                    case SkillCategory.Crafting:
                        float craftPct = lvl * 5f;
                        effectTxt.text = $"Crafting Efficiency: +{craftPct:F0}%";
                        break;
                }
            }

            if (btn != null)
            {
                btn.interactable = (progressionSystem.SkillPoints > 0 && !isMax);
            }
        }

        private void UpgradeSkill(SkillCategory category)
        {
            if (progressionSystem == null) FindAndBindPlayer();
            if (progressionSystem == null) return;

            progressionSystem.TryUpgradeSkill(category);
            RefreshUI();
        }

        // ==========================================
        // DYNAMIC UI LAYOUT GENERATION
        // ==========================================

        private void EnsureUIStructure()
        {
            if (progressionPanel != null) return;

            GameObject canvasObj = GameObject.Find("GameplayCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("GameplayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            progressionPanel = new GameObject("ProgressionPanel", typeof(RectTransform), typeof(Image));
            progressionPanel.transform.SetParent(canvasObj.transform, false);

            RectTransform mainRect = progressionPanel.GetComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0.15f, 0.1f);
            mainRect.anchorMax = new Vector2(0.85f, 0.9f);
            mainRect.offsetMin = Vector2.zero;
            mainRect.offsetMax = Vector2.zero;

            Image mainBg = progressionPanel.GetComponent<Image>();
            mainBg.color = new Color(0.08f, 0.08f, 0.1f, 0.96f);

            // Header Bar
            GameObject headerObj = new GameObject("Header", typeof(RectTransform), typeof(Image));
            headerObj.transform.SetParent(progressionPanel.transform, false);
            RectTransform hRect = headerObj.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 0.88f);
            hRect.anchorMax = new Vector2(1f, 1f);
            hRect.offsetMin = Vector2.zero;
            hRect.offsetMax = Vector2.zero;
            headerObj.GetComponent<Image>().color = new Color(0.14f, 0.14f, 0.18f, 1f);

            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(headerObj.transform, false);
            TextMeshProUGUI tTxt = titleObj.AddComponent<TextMeshProUGUI>();
            tTxt.text = "PLAYER PROGRESSION & PERKS";
            tTxt.fontSize = 20;
            tTxt.fontStyle = FontStyles.Bold;
            tTxt.color = new Color(1f, 0.85f, 0.2f);
            tTxt.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.03f, 0f);
            tRect.anchorMax = new Vector2(0.8f, 1f);

            // Close button
            GameObject closeObj = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObj.transform.SetParent(headerObj.transform, false);
            RectTransform cRect = closeObj.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.92f, 0.15f);
            cRect.anchorMax = new Vector2(0.98f, 0.85f);
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;
            closeObj.GetComponent<Image>().color = new Color(0.6f, 0.2f, 0.2f, 1f);

            GameObject cTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            cTxtObj.transform.SetParent(closeObj.transform, false);
            TextMeshProUGUI cTxt = cTxtObj.AddComponent<TextMeshProUGUI>();
            cTxt.text = "X";
            cTxt.fontSize = 16;
            cTxt.fontStyle = FontStyles.Bold;
            cTxt.color = Color.white;
            cTxt.alignment = TextAlignmentOptions.Center;
            cTxtObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            cTxtObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
            closeButton = closeObj.GetComponent<Button>();

            // Level & XP Bar Container
            GameObject levelInfoObj = new GameObject("LevelInfoContainer", typeof(RectTransform));
            levelInfoObj.transform.SetParent(progressionPanel.transform, false);
            RectTransform lRect = levelInfoObj.GetComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0.05f, 0.70f);
            lRect.anchorMax = new Vector2(0.95f, 0.86f);
            lRect.offsetMin = Vector2.zero;
            lRect.offsetMax = Vector2.zero;

            levelText = CreateTextElement(levelInfoObj.transform, "LevelText", "PLAYER LEVEL 1", 20, FontStyles.Bold, Color.white, new Vector2(0f, 0.5f), new Vector2(0.5f, 1f));
            skillPointsText = CreateTextElement(levelInfoObj.transform, "SkillPointsText", "SKILL POINTS AVAILABLE: 0", 16, FontStyles.Bold, new Color(1f, 0.85f, 0.2f), new Vector2(0.5f, 0.5f), new Vector2(1f, 1f));
            xpText = CreateTextElement(levelInfoObj.transform, "XPText", "0 / 100 XP", 13, FontStyles.Normal, Color.white, new Vector2(0f, 0f), new Vector2(1f, 0.45f));

            // XP Progress Bar
            GameObject sliderObj = new GameObject("XPProgressBar", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(levelInfoObj.transform, false);
            RectTransform sRect = sliderObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 0.2f);
            sRect.anchorMax = new Vector2(1f, 0.45f);
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;

            xpProgressBar = sliderObj.GetComponent<Slider>();
            xpProgressBar.interactable = false;

            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(sliderObj.transform, false);
            bgObj.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            bgObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bgObj.GetComponent<RectTransform>().anchorMax = Vector2.one;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            fillArea.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            fillArea.GetComponent<RectTransform>().anchorMax = Vector2.one;

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(fillArea.transform, false);
            fillObj.GetComponent<Image>().color = new Color(0.2f, 0.7f, 0.9f, 1f);
            fillObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            fillObj.GetComponent<RectTransform>().anchorMax = Vector2.one;

            xpProgressBar.targetGraphic = bgObj.GetComponent<Image>();
            xpProgressBar.fillRect = fillObj.GetComponent<RectTransform>();

            // Perks Container (2x2 Grid)
            GameObject perksContainer = new GameObject("PerksContainer", typeof(RectTransform));
            perksContainer.transform.SetParent(progressionPanel.transform, false);
            RectTransform pRect = perksContainer.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.05f, 0.05f);
            pRect.anchorMax = new Vector2(0.95f, 0.65f);
            pRect.offsetMin = Vector2.zero;
            pRect.offsetMax = Vector2.zero;

            CreatePerkCard(perksContainer.transform, "CombatCard", "COMBAT PERK", new Vector2(0f, 0.52f), new Vector2(0.48f, 1f), out combatLevelText, out combatEffectText, out combatUpgradeButton);
            CreatePerkCard(perksContainer.transform, "SurvivalCard", "SURVIVAL PERK", new Vector2(0.52f, 0.52f), new Vector2(1f, 1f), out survivalLevelText, out survivalEffectText, out survivalUpgradeButton);
            CreatePerkCard(perksContainer.transform, "ScavengingCard", "SCAVENGING PERK", new Vector2(0f, 0f), new Vector2(0.48f, 0.48f), out scavengingLevelText, out scavengingEffectText, out scavengingUpgradeButton);
            CreatePerkCard(perksContainer.transform, "CraftingCard", "CRAFTING PERK", new Vector2(0.52f, 0f), new Vector2(1f, 0.48f), out craftingLevelText, out craftingEffectText, out craftingUpgradeButton);
        }

        private void CreatePerkCard(Transform parent, string name, string title, Vector2 anchorMin, Vector2 anchorMax, out TextMeshProUGUI levelTxt, out TextMeshProUGUI effectTxt, out Button upgradeBtn)
        {
            GameObject cardObj = new GameObject(name, typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(parent, false);

            RectTransform rect = cardObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            cardObj.GetComponent<Image>().color = new Color(0.14f, 0.14f, 0.17f, 0.9f);

            CreateTextElement(cardObj.transform, "CardTitle", title, 16, FontStyles.Bold, new Color(1f, 0.85f, 0.2f), new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.92f));
            levelTxt = CreateTextElement(cardObj.transform, "LevelText", "Level 0 / 5", 13, FontStyles.Normal, Color.white, new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.72f));
            effectTxt = CreateTextElement(cardObj.transform, "EffectText", "+0%", 12, FontStyles.Italic, new Color(0.8f, 0.8f, 0.8f), new Vector2(0.05f, 0.26f), new Vector2(0.95f, 0.48f));

            // Upgrade Button
            GameObject btnObj = new GameObject("UpgradeBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(cardObj.transform, false);

            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.05f, 0.06f);
            btnRect.anchorMax = new Vector2(0.60f, 0.25f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            btnObj.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.3f, 1f);

            GameObject bTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            bTxtObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI bTxt = bTxtObj.AddComponent<TextMeshProUGUI>();
            bTxt.text = "UPGRADE (+1)";
            bTxt.fontSize = 12;
            bTxt.fontStyle = FontStyles.Bold;
            bTxt.color = Color.white;
            bTxt.alignment = TextAlignmentOptions.Center;
            bTxtObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bTxtObj.GetComponent<RectTransform>().anchorMax = Vector2.one;

            upgradeBtn = btnObj.GetComponent<Button>();
        }

        private TextMeshProUGUI CreateTextElement(Transform parent, string name, string defaultText, float fontSize, FontStyles style, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject txtObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(parent, false);

            RectTransform rect = txtObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
            txt.text = defaultText;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = TextAlignmentOptions.TopLeft;

            return txt;
        }
    }
}
