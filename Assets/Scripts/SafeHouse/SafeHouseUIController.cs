using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ZombieApocalypse.Inventory;
using ZombieApocalypse.Player;
using ZombieApocalypse.World;

namespace ZombieApocalypse.SafeHouse
{
    public enum SafeHouseTab
    {
        Stash,
        Rest,
        Upgrades
    }

    /// <summary>
    /// Master UI Controller for Safe House operations (Stage 4 & Stage 5).
    /// Manages Safe House Interaction Menu, Stash UI (deposit/withdraw), Rest UI (time fast-forward),
    /// and Upgrade Terminal UI (storage, bed, fortification upgrades).
    ///
    /// Preserves strict gameplay authorities:
    /// - SafeHouseManager for safe zone state and upgrades
    /// - BaseStashContainer for stash contents and capacity
    /// - BedRestInteractable for rest logic
    /// - InventorySystem for player inventory
    /// - WorldTimeManager for time display
    ///
    /// ATTACH TO: Gameplay Canvas Safe House UI root GameObject.
    /// </summary>
    public class SafeHouseUIController : MonoBehaviour
    {
        public static SafeHouseUIController Instance { get; private set; }

        [Header("Main Panels")]
        [SerializeField] private GameObject safeHousePanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button stashTabButton;
        [SerializeField] private Button restTabButton;
        [SerializeField] private Button upgradesTabButton;

        [Header("Section Containers")]
        [SerializeField] private GameObject stashSection;
        [SerializeField] private GameObject restSection;
        [SerializeField] private GameObject upgradesSection;

        [Header("Stash UI Elements")]
        [SerializeField] private TextMeshProUGUI stashCapacityText;
        [SerializeField] private TextMeshProUGUI stashFeedbackText;
        [SerializeField] private Transform playerInventoryGrid;
        [SerializeField] private Transform stashGrid;

        [Header("Rest UI Elements")]
        [SerializeField] private TextMeshProUGUI currentTimeText;
        [SerializeField] private TextMeshProUGUI targetWakeText;
        [SerializeField] private TextMeshProUGUI currentDayText;
        [SerializeField] private TextMeshProUGUI restRecoveryText;
        [SerializeField] private TextMeshProUGUI restRestrictionText;
        [SerializeField] private Button sleepButton;
        [SerializeField] private Button cancelRestButton;

        [Header("Upgrades UI Elements")]
        [SerializeField] private TextMeshProUGUI storageLevelText;
        [SerializeField] private TextMeshProUGUI storageCostText;
        [SerializeField] private Button storageUpgradeButton;

        [SerializeField] private TextMeshProUGUI bedLevelText;
        [SerializeField] private TextMeshProUGUI bedCostText;
        [SerializeField] private Button bedUpgradeButton;

        [SerializeField] private TextMeshProUGUI fortificationLevelText;
        [SerializeField] private TextMeshProUGUI fortificationCostText;
        [SerializeField] private Button fortificationUpgradeButton;

        [SerializeField] private TextMeshProUGUI upgradeFeedbackText;

        // System References
        private GameObject playerObj;
        private PlayerInputHandler inputHandler;
        private PlayerHealth playerHealth;
        private InventorySystem playerInventory;
        private BaseStashContainer stashContainer;
        private BedRestInteractable bedInteractable;

        // Runtime UI state
        private bool isOpen = false;
        private SafeHouseTab activeTab = SafeHouseTab.Stash;

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

        private void OnEnable()
        {
            BaseStashContainer.OnStashInteracted += HandleStashInteracted;
            BedRestInteractable.OnBedInteracted += HandleBedInteracted;
            BaseUpgradeTerminal.OnTerminalInteracted += HandleTerminalInteracted;
            SafeHouseManager.OnSafeHouseStateUpdated += HandleSafeHouseStateUpdated;
        }

        private void OnDisable()
        {
            BaseStashContainer.OnStashInteracted -= HandleStashInteracted;
            BedRestInteractable.OnBedInteracted -= HandleBedInteracted;
            BaseUpgradeTerminal.OnTerminalInteracted -= HandleTerminalInteracted;
            SafeHouseManager.OnSafeHouseStateUpdated -= HandleSafeHouseStateUpdated;
        }

        private void Start()
        {
            FindAndBindPlayer();
            EnsureUIStructure();

            if (closeButton != null) closeButton.onClick.AddListener(CloseUI);
            if (stashTabButton != null) stashTabButton.onClick.AddListener(() => SwitchTab(SafeHouseTab.Stash));
            if (restTabButton != null) restTabButton.onClick.AddListener(() => SwitchTab(SafeHouseTab.Rest));
            if (upgradesTabButton != null) upgradesTabButton.onClick.AddListener(() => SwitchTab(SafeHouseTab.Upgrades));

            if (sleepButton != null) sleepButton.onClick.AddListener(OnSleepButtonClicked);
            if (cancelRestButton != null) cancelRestButton.onClick.AddListener(CloseUI);

            if (storageUpgradeButton != null) storageUpgradeButton.onClick.AddListener(OnStorageUpgradeClicked);
            if (bedUpgradeButton != null) bedUpgradeButton.onClick.AddListener(OnBedUpgradeClicked);
            if (fortificationUpgradeButton != null) fortificationUpgradeButton.onClick.AddListener(OnFortificationUpgradeClicked);

            if (safeHousePanel != null)
            {
                safeHousePanel.SetActive(false);
            }
        }

        public void FindAndBindPlayer()
        {
            playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                inputHandler = playerObj.GetComponent<PlayerInputHandler>();
                playerHealth = playerObj.GetComponent<PlayerHealth>();
                playerInventory = playerObj.GetComponent<InventorySystem>();
            }

            if (stashContainer == null) stashContainer = FindObjectOfType<BaseStashContainer>();
            if (bedInteractable == null) bedInteractable = FindObjectOfType<BedRestInteractable>();
        }

        private void Update()
        {
            if (playerHealth != null && playerHealth.IsDead)
            {
                if (isOpen) CloseUI();
                return;
            }

            if (!isOpen) return;

            // Close UI hotkeys: Escape or E
            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)
                {
                    CloseUI();
                    return;
                }
            }

            // Periodically refresh Rest panel live clock if rest tab active
            if (activeTab == SafeHouseTab.Rest)
            {
                UpdateRestDisplay();
            }
        }

        private void HandleStashInteracted()
        {
            OpenUI(SafeHouseTab.Stash);
        }

        private void HandleBedInteracted()
        {
            OpenUI(SafeHouseTab.Rest);
        }

        private void HandleTerminalInteracted()
        {
            OpenUI(SafeHouseTab.Upgrades);
        }

        private void HandleSafeHouseStateUpdated()
        {
            if (isOpen)
            {
                RefreshActiveTab();
            }
        }

        public void OpenUI(SafeHouseTab initialTab = SafeHouseTab.Stash)
        {
            FindAndBindPlayer();

            if (playerHealth != null && playerHealth.IsDead) return;

            // Verify authority restriction before opening
            if (SafeHouseManager.Instance != null && !SafeHouseManager.Instance.IsPlayerInsideSafeHouse)
            {
                Debug.LogWarning("[SafeHouseUIController] Cannot open Safe House UI outside Safe House.");
                return;
            }

            isOpen = true;
            if (safeHousePanel != null) safeHousePanel.SetActive(true);

            // Input locking & cursor activation
            if (inputHandler != null)
            {
                inputHandler.IsInventoryOpen = true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SwitchTab(initialTab);
        }

        public void CloseUI()
        {
            isOpen = false;
            if (safeHousePanel != null) safeHousePanel.SetActive(false);

            // Input unlocking & cursor locking
            if (inputHandler != null)
            {
                inputHandler.IsInventoryOpen = false;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void SwitchTab(SafeHouseTab tab)
        {
            activeTab = tab;

            if (stashSection != null) stashSection.SetActive(activeTab == SafeHouseTab.Stash);
            if (restSection != null) restSection.SetActive(activeTab == SafeHouseTab.Rest);
            if (upgradesSection != null) upgradesSection.SetActive(activeTab == SafeHouseTab.Upgrades);

            // Update Tab Button Colors
            HighlightTabButton(stashTabButton, activeTab == SafeHouseTab.Stash);
            HighlightTabButton(restTabButton, activeTab == SafeHouseTab.Rest);
            HighlightTabButton(upgradesTabButton, activeTab == SafeHouseTab.Upgrades);

            RefreshActiveTab();
        }

        private void HighlightTabButton(Button btn, bool active)
        {
            if (btn == null) return;
            ColorBlock colors = btn.colors;
            colors.normalColor = active ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.25f, 0.25f, 0.25f);
            btn.colors = colors;
        }

        private void RefreshActiveTab()
        {
            switch (activeTab)
            {
                case SafeHouseTab.Stash:
                    RefreshStashTab();
                    break;
                case SafeHouseTab.Rest:
                    RefreshRestTab();
                    break;
                case SafeHouseTab.Upgrades:
                    RefreshUpgradesTab();
                    break;
            }
        }

        // ==========================================
        // STASH TAB LOGIC (STAGE 4.3 & 4.4)
        // ==========================================

        public void RefreshStashTab()
        {
            if (playerInventory == null || stashContainer == null) FindAndBindPlayer();

            // Display Capacity from BaseStashContainer authority
            int used = stashContainer != null ? stashContainer.UsedSlots : 0;
            int cap = stashContainer != null ? stashContainer.Capacity : 20;
            if (stashCapacityText != null)
            {
                stashCapacityText.text = $"Stash: {used} / {cap}";
            }

            BuildPlayerInventoryList();
            BuildStashList();
        }

        private void BuildPlayerInventoryList()
        {
            if (playerInventoryGrid == null) return;

            foreach (Transform child in playerInventoryGrid)
            {
                Destroy(child.gameObject);
            }

            if (playerInventory == null) return;

            IReadOnlyList<InventorySlot> slots = playerInventory.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                int slotIndex = i;
                InventorySlot slot = slots[i];
                if (slot == null || slot.itemData == null || slot.quantity <= 0) continue;

                GameObject itemCard = CreateSlotCard(playerInventoryGrid, $"{slot.itemData.itemName} x{slot.quantity}", "Deposit", () => OnDepositClicked(slotIndex));
            }
        }

        private void BuildStashList()
        {
            if (stashGrid == null) return;

            foreach (Transform child in stashGrid)
            {
                Destroy(child.gameObject);
            }

            if (stashContainer == null) return;

            IReadOnlyList<InventorySlot> stashSlots = stashContainer.StashSlots;
            for (int i = 0; i < stashSlots.Count; i++)
            {
                int slotIndex = i;
                InventorySlot slot = stashSlots[i];
                if (slot == null || slot.itemData == null || slot.quantity <= 0) continue;

                GameObject itemCard = CreateSlotCard(stashGrid, $"{slot.itemData.itemName} x{slot.quantity}", "Withdraw", () => OnWithdrawClicked(slotIndex));
            }
        }

        private GameObject CreateSlotCard(Transform parent, string labelText, string btnText, UnityEngine.Events.UnityAction onClickAction)
        {
            GameObject card = new GameObject("SlotCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);

            Image cardBg = card.GetComponent<Image>();
            cardBg.color = new Color(0.18f, 0.18f, 0.22f, 0.9f);

            GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(card.transform, false);
            TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
            txt.text = labelText;
            txt.fontSize = 14;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = new Vector2(0.05f, 0f);
            txtRect.anchorMax = new Vector2(0.65f, 1f);

            GameObject btnObj = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(card.transform, false);
            btnObj.GetComponent<Image>().color = new Color(0.25f, 0.55f, 0.35f, 1f);

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(onClickAction);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.68f, 0.15f);
            btnRect.anchorMax = new Vector2(0.95f, 0.85f);

            GameObject btnTxtObj = new GameObject("BtnText", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI btnTxt = btnTxtObj.GetComponent<TextMeshProUGUI>();
            btnTxt.text = btnText;
            btnTxt.fontSize = 12;
            btnTxt.fontStyle = FontStyles.Bold;
            btnTxt.color = Color.white;
            btnTxt.alignment = TextAlignmentOptions.Center;
            RectTransform btnTxtRect = btnTxtObj.GetComponent<RectTransform>();
            btnTxtRect.anchorMin = Vector2.zero;
            btnTxtRect.anchorMax = Vector2.one;

            return card;
        }

        private void OnDepositClicked(int playerSlotIndex)
        {
            if (stashContainer == null || playerInventory == null) return;

            int deposited = stashContainer.TryDeposit(playerInventory, playerSlotIndex);
            if (stashFeedbackText != null)
            {
                stashFeedbackText.text = deposited > 0
                    ? $"<color=#4CAF50>Deposited {deposited} items to Stash.</color>"
                    : $"<color=#F44336>Deposit failed (Stash full or invalid item).</color>";
            }

            RefreshStashTab();
        }

        private void OnWithdrawClicked(int stashSlotIndex)
        {
            if (stashContainer == null || playerInventory == null) return;

            int withdrawn = stashContainer.TryWithdraw(playerInventory, stashSlotIndex);
            if (stashFeedbackText != null)
            {
                stashFeedbackText.text = withdrawn > 0
                    ? $"<color=#4CAF50>Withdrew {withdrawn} items from Stash.</color>"
                    : $"<color=#F44336>Withdraw failed (Inventory full or invalid item).</color>";
            }

            RefreshStashTab();
        }

        // ==========================================
        // REST TAB LOGIC (STAGE 4.5 & 4.6)
        // ==========================================

        public void RefreshRestTab()
        {
            if (bedInteractable == null) bedInteractable = FindObjectOfType<BedRestInteractable>();
            UpdateRestDisplay();
        }

        private void UpdateRestDisplay()
        {
            WorldTimeManager timeMgr = WorldTimeManager.Instance;
            if (currentTimeText != null)
            {
                currentTimeText.text = timeMgr != null ? $"Current Time: {timeMgr.FormattedTime}" : "Current Time: 12:00";
            }
            if (targetWakeText != null)
            {
                targetWakeText.text = "Wake Up: 06:00 (Dawn)";
            }
            if (currentDayText != null)
            {
                currentDayText.text = timeMgr != null ? $"Day: {timeMgr.DayCount}" : "Day: 1";
            }
            if (restRecoveryText != null)
            {
                restRecoveryText.text = "<b>EXPECTED RECOVERY:</b>\nHealth: Fully Restored\nStamina: Fully Restored";
            }

            // Check interactable restriction authority
            bool canRest = bedInteractable != null && playerObj != null && bedInteractable.CanInteract(playerObj);
            if (sleepButton != null)
            {
                sleepButton.interactable = canRest;
            }

            if (restRestrictionText != null)
            {
                if (playerHealth != null && playerHealth.IsDead)
                {
                    restRestrictionText.text = "<color=#F44336>Cannot rest while dead.</color>";
                }
                else if (SafeHouseManager.Instance != null && !SafeHouseManager.Instance.IsPlayerInsideSafeHouse)
                {
                    restRestrictionText.text = "<color=#F44336>Must be inside Safe House to rest.</color>";
                }
                else if (!canRest)
                {
                    restRestrictionText.text = "<color=#F44336>Bed is currently unavailable.</color>";
                }
                else
                {
                    restRestrictionText.text = "<color=#4CAF50>Safe House bed is ready for rest.</color>";
                }
            }
        }

        private void OnSleepButtonClicked()
        {
            if (bedInteractable == null || playerObj == null) return;

            bool success = bedInteractable.TryRest(playerObj);
            if (success)
            {
                if (restRestrictionText != null)
                {
                    restRestrictionText.text = "<color=#4CAF50>Rested successfully! Health & Stamina restored.</color>";
                }
                RefreshRestTab();
            }
        }

        // ==========================================
        // UPGRADES TAB LOGIC (STAGE 5.1 - 5.4)
        // ==========================================

        public void RefreshUpgradesTab()
        {
            SafeHouseManager shMgr = SafeHouseManager.Instance;
            if (shMgr == null) return;

            // 1. Storage Upgrade
            int storageLvl = shMgr.StashUpgradeLevel;
            int currentCap = 20 + (storageLvl * 10);
            int nextCap = 20 + ((storageLvl + 1) * 10);

            if (storageLevelText != null)
            {
                storageLevelText.text = storageLvl >= 3
                    ? $"<b>STORAGE</b>\nLevel {storageLvl} / 3\nCapacity: {currentCap} (MAX)"
                    : $"<b>STORAGE</b>\nLevel {storageLvl} / 3\nCapacity: {currentCap} → <color=#4CAF50>{nextCap}</color>";
            }

            if (storageLvl >= 3)
            {
                if (storageCostText != null) storageCostText.text = "Max level reached.";
                if (storageUpgradeButton != null) storageUpgradeButton.interactable = false;
            }
            else
            {
                List<InventorySlot> costs = shMgr.GetStorageUpgradeCosts(storageLvl + 1);
                FormatCostText(storageCostText, costs);
                if (storageUpgradeButton != null)
                {
                    storageUpgradeButton.interactable = shMgr.IsPlayerInsideSafeHouse && HasMaterials(costs);
                }
            }

            // 2. Bed Upgrade
            int bedLvl = shMgr.BedUpgradeLevel;
            if (bedLevelText != null)
            {
                bedLevelText.text = bedLvl >= 3
                    ? $"<b>BED</b>\nLevel {bedLvl} / 3\nRecovery: Full (MAX)"
                    : $"<b>BED</b>\nLevel {bedLvl} / 3\nRecovery: Full Health & Stamina";
            }

            if (bedLvl >= 3)
            {
                if (bedCostText != null) bedCostText.text = "Max level reached.";
                if (bedUpgradeButton != null) bedUpgradeButton.interactable = false;
            }
            else
            {
                List<InventorySlot> costs = shMgr.GetBedUpgradeCosts(bedLvl + 1);
                FormatCostText(bedCostText, costs);
                if (bedUpgradeButton != null)
                {
                    bedUpgradeButton.interactable = shMgr.IsPlayerInsideSafeHouse && HasMaterials(costs);
                }
            }

            // 3. Fortification Upgrade
            int fortLvl = shMgr.FortificationLevel;
            if (fortificationLevelText != null)
            {
                fortificationLevelText.text = fortLvl >= 3
                    ? $"<b>FORTIFICATION</b>\nLevel {fortLvl} / 3\nDefense: Level {fortLvl} (MAX)"
                    : $"<b>FORTIFICATION</b>\nLevel {fortLvl} / 3\nDefense: Level {fortLvl} → <color=#4CAF50>Level {fortLvl + 1}</color>";
            }

            if (fortLvl >= 3)
            {
                if (fortificationCostText != null) fortificationCostText.text = "Max level reached.";
                if (fortificationUpgradeButton != null) fortificationUpgradeButton.interactable = false;
            }
            else
            {
                List<InventorySlot> costs = shMgr.GetFortificationUpgradeCosts(fortLvl + 1);
                FormatCostText(fortificationCostText, costs);
                if (fortificationUpgradeButton != null)
                {
                    fortificationUpgradeButton.interactable = shMgr.IsPlayerInsideSafeHouse && HasMaterials(costs);
                }
            }
        }

        private void FormatCostText(TextMeshProUGUI labelText, List<InventorySlot> costs)
        {
            if (labelText == null) return;
            string str = "<b>REQUIRED MATERIALS:</b>\n";
            if (costs != null && costs.Count > 0)
            {
                foreach (var c in costs)
                {
                    if (c == null || c.itemData == null) continue;
                    int owned = playerInventory != null ? playerInventory.GetItemQuantity(c.itemData) : 0;
                    bool hasEnough = owned >= c.quantity;
                    string col = hasEnough ? "#4CAF50" : "#F44336";
                    str += $"• <color={col}>{c.itemData.itemName}: {owned} / {c.quantity}</color>\n";
                }
            }
            else
            {
                str += "None\n";
            }
            labelText.text = str;
        }

        private bool HasMaterials(List<InventorySlot> costs)
        {
            if (costs == null || costs.Count == 0) return true;
            if (playerInventory == null) return false;

            foreach (var c in costs)
            {
                if (c == null || c.itemData == null) continue;
                if (playerInventory.GetItemQuantity(c.itemData) < c.quantity) return false;
            }
            return true;
        }

        private void OnStorageUpgradeClicked()
        {
            if (SafeHouseManager.Instance == null || playerInventory == null) return;

            bool success = SafeHouseManager.Instance.TryUpgradeStorage(playerInventory);
            if (upgradeFeedbackText != null)
            {
                upgradeFeedbackText.text = success
                    ? "<color=#4CAF50>Storage upgraded successfully!</color>"
                    : "<color=#F44336>Storage upgrade failed (Insufficient materials or max level).</color>";
            }

            RefreshUpgradesTab();
        }

        private void OnBedUpgradeClicked()
        {
            if (SafeHouseManager.Instance == null || playerInventory == null) return;

            bool success = SafeHouseManager.Instance.TryUpgradeBed(playerInventory);
            if (upgradeFeedbackText != null)
            {
                upgradeFeedbackText.text = success
                    ? "<color=#4CAF50>Bed upgraded successfully!</color>"
                    : "<color=#F44336>Bed upgrade failed (Insufficient materials or max level).</color>";
            }

            RefreshUpgradesTab();
        }

        private void OnFortificationUpgradeClicked()
        {
            if (SafeHouseManager.Instance == null || playerInventory == null) return;

            bool success = SafeHouseManager.Instance.TryUpgradeFortification(playerInventory);
            if (upgradeFeedbackText != null)
            {
                upgradeFeedbackText.text = success
                    ? "<color=#4CAF50>Fortification upgraded successfully!</color>"
                    : "<color=#F44336>Fortification upgrade failed (Insufficient materials or max level).</color>";
            }

            RefreshUpgradesTab();
        }

        // ==========================================
        // DYNAMIC UI SETUP & FALLBACK CREATION
        // ==========================================

        private void EnsureUIStructure()
        {
            if (safeHousePanel != null) return;

            GameObject canvasObj = GameObject.Find("GameplayCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("GameplayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            safeHousePanel = new GameObject("SafeHousePanel", typeof(RectTransform), typeof(Image));
            safeHousePanel.transform.SetParent(canvasObj.transform, false);

            RectTransform mainRect = safeHousePanel.GetComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0.1f, 0.08f);
            mainRect.anchorMax = new Vector2(0.9f, 0.92f);
            mainRect.offsetMin = Vector2.zero;
            mainRect.offsetMax = Vector2.zero;

            Image mainBg = safeHousePanel.GetComponent<Image>();
            mainBg.color = new Color(0.08f, 0.1f, 0.08f, 0.96f);

            // Header bar
            GameObject headerObj = new GameObject("Header", typeof(RectTransform), typeof(Image));
            headerObj.transform.SetParent(safeHousePanel.transform, false);
            RectTransform headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0.9f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
            headerObj.GetComponent<Image>().color = new Color(0.12f, 0.18f, 0.14f, 1f);

            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(headerObj.transform, false);
            TextMeshProUGUI titleTxt = titleObj.GetComponent<TextMeshProUGUI>();
            titleTxt.text = "SAFE HOUSE TERMINAL";
            titleTxt.fontSize = 22;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.color = new Color(0.3f, 0.9f, 0.4f);
            titleTxt.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.02f, 0f);
            tRect.anchorMax = new Vector2(0.4f, 1f);

            // Tabs Header Buttons
            CreateHeaderButton(headerObj.transform, "StashTabBtn", "STASH", new Vector2(0.42f, 0.1f), new Vector2(0.58f, 0.9f), out stashTabButton);
            CreateHeaderButton(headerObj.transform, "RestTabBtn", "REST", new Vector2(0.59f, 0.1f), new Vector2(0.75f, 0.9f), out restTabButton);
            CreateHeaderButton(headerObj.transform, "UpgradesTabBtn", "UPGRADES", new Vector2(0.76f, 0.1f), new Vector2(0.92f, 0.9f), out upgradesTabButton);
            CreateHeaderButton(headerObj.transform, "CloseBtn", "X", new Vector2(0.94f, 0.15f), new Vector2(0.98f, 0.85f), out closeButton);

            // --- STASH SECTION ---
            stashSection = CreateSectionPanel("StashSection");
            GameObject pInvBox = CreateSubPanel(stashSection.transform, "PlayerInvPanel", new Vector2(0.02f, 0.1f), new Vector2(0.48f, 0.92f), out playerInventoryGrid);
            CreatePanelTitle(pInvBox.transform, "PLAYER INVENTORY");

            GameObject stashBox = CreateSubPanel(stashSection.transform, "StashPanel", new Vector2(0.52f, 0.1f), new Vector2(0.98f, 0.92f), out stashGrid);
            CreatePanelTitle(stashBox.transform, "BASE STASH", out stashCapacityText);

            stashFeedbackText = CreateFeedbackText(stashSection.transform, new Vector2(0.02f, 0.01f), new Vector2(0.98f, 0.08f));

            // --- REST SECTION ---
            restSection = CreateSectionPanel("RestSection");
            GameObject restBox = CreateSubPanel(restSection.transform, "RestContentPanel", new Vector2(0.2f, 0.15f), new Vector2(0.8f, 0.85f), out _);
            CreatePanelTitle(restBox.transform, "REST / SLEEP");

            currentTimeText = CreateTextElement(restBox.transform, "Current Time: --:--", 16, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.82f));
            targetWakeText = CreateTextElement(restBox.transform, "Wake Up: 06:00 (Dawn)", 16, new Vector2(0.1f, 0.60f), new Vector2(0.9f, 0.70f));
            currentDayText = CreateTextElement(restBox.transform, "Day: 1", 16, new Vector2(0.1f, 0.48f), new Vector2(0.9f, 0.58f));
            restRecoveryText = CreateTextElement(restBox.transform, "Expected Recovery: Health & Stamina Full", 15, new Vector2(0.1f, 0.30f), new Vector2(0.9f, 0.45f));
            restRestrictionText = CreateTextElement(restBox.transform, "", 14, new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.28f));

            CreateButton(restBox.transform, "SleepBtn", "SLEEP", new Vector2(0.15f, 0.05f), new Vector2(0.45f, 0.16f), out sleepButton);
            CreateButton(restBox.transform, "CancelBtn", "CANCEL", new Vector2(0.55f, 0.05f), new Vector2(0.85f, 0.16f), out cancelRestButton);

            // --- UPGRADES SECTION ---
            upgradesSection = CreateSectionPanel("UpgradesSection");

            // Storage Card
            GameObject storCard = CreateSubPanel(upgradesSection.transform, "StorageCard", new Vector2(0.02f, 0.18f), new Vector2(0.32f, 0.92f), out _);
            storageLevelText = CreateTextElement(storCard.transform, "STORAGE\nLevel 0 / 3", 16, new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.95f));
            storageCostText = CreateTextElement(storCard.transform, "Cost: --", 14, new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.60f));
            CreateButton(storCard.transform, "StorageUpgBtn", "UPGRADE", new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.18f), out storageUpgradeButton);

            // Bed Card
            GameObject bedCard = CreateSubPanel(upgradesSection.transform, "BedCard", new Vector2(0.35f, 0.18f), new Vector2(0.65f, 0.92f), out _);
            bedLevelText = CreateTextElement(bedCard.transform, "BED\nLevel 0 / 3", 16, new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.95f));
            bedCostText = CreateTextElement(bedCard.transform, "Cost: --", 14, new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.60f));
            CreateButton(bedCard.transform, "BedUpgBtn", "UPGRADE", new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.18f), out bedUpgradeButton);

            // Fortification Card
            GameObject fortCard = CreateSubPanel(upgradesSection.transform, "FortCard", new Vector2(0.68f, 0.18f), new Vector2(0.98f, 0.92f), out _);
            fortificationLevelText = CreateTextElement(fortCard.transform, "FORTIFICATION\nLevel 0 / 3", 16, new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.95f));
            fortificationCostText = CreateTextElement(fortCard.transform, "Cost: --", 14, new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.60f));
            CreateButton(fortCard.transform, "FortUpgBtn", "UPGRADE", new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.18f), out fortificationUpgradeButton);

            upgradeFeedbackText = CreateFeedbackText(upgradesSection.transform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.14f));
        }

        private GameObject CreateSectionPanel(string name)
        {
            GameObject sec = new GameObject(name, typeof(RectTransform));
            sec.transform.SetParent(safeHousePanel.transform, false);
            RectTransform rect = sec.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return sec;
        }

        private GameObject CreateSubPanel(Transform parent, string name, Vector2 minAnchor, Vector2 maxAnchor, out Transform contentContainer)
        {
            GameObject p = new GameObject(name, typeof(RectTransform), typeof(Image));
            p.transform.SetParent(parent, false);

            RectTransform rect = p.GetComponent<RectTransform>();
            rect.anchorMin = minAnchor;
            rect.anchorMax = maxAnchor;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            p.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.12f, 0.9f);

            GameObject scrollContainer = new GameObject("ScrollContent", typeof(RectTransform));
            scrollContainer.transform.SetParent(p.transform, false);
            RectTransform sRect = scrollContainer.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.02f, 0.02f);
            sRect.anchorMax = new Vector2(0.98f, 0.88f);
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg = scrollContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;

            contentContainer = scrollContainer.transform;
            return p;
        }

        private void CreatePanelTitle(Transform parent, string titleText, out TextMeshProUGUI titleTxt)
        {
            GameObject tObj = new GameObject("PanelTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            tObj.transform.SetParent(parent, false);
            titleTxt = tObj.GetComponent<TextMeshProUGUI>();
            titleTxt.text = titleText;
            titleTxt.fontSize = 16;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.color = new Color(0.9f, 0.9f, 0.9f);
            titleTxt.alignment = TextAlignmentOptions.MidlineCenter;

            RectTransform tRect = tObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 0.88f);
            tRect.anchorMax = new Vector2(1f, 1f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
        }

        private void CreatePanelTitle(Transform parent, string titleText)
        {
            CreatePanelTitle(parent, titleText, out _);
        }

        private TextMeshProUGUI CreateTextElement(Transform parent, string defaultText, float fontSize, Vector2 minAnchor, Vector2 maxAnchor)
        {
            GameObject txtObj = new GameObject("TextElem", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(parent, false);
            TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
            txt.text = defaultText;
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.TopLeft;

            RectTransform rect = txtObj.GetComponent<RectTransform>();
            rect.anchorMin = minAnchor;
            rect.anchorMax = maxAnchor;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return txt;
        }

        private TextMeshProUGUI CreateFeedbackText(Transform parent, Vector2 minAnchor, Vector2 maxAnchor)
        {
            GameObject txtObj = new GameObject("FeedbackText", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(parent, false);
            TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
            txt.text = "";
            txt.fontSize = 14;
            txt.alignment = TextAlignmentOptions.Center;

            RectTransform rect = txtObj.GetComponent<RectTransform>();
            rect.anchorMin = minAnchor;
            rect.anchorMax = maxAnchor;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return txt;
        }

        private GameObject CreateHeaderButton(Transform parent, string name, string text, Vector2 minAnchor, Vector2 maxAnchor, out Button btn)
        {
            return CreateButton(parent, name, text, minAnchor, maxAnchor, out btn);
        }

        private GameObject CreateButton(Transform parent, string name, string text, Vector2 minAnchor, Vector2 maxAnchor, out Button btn)
        {
            GameObject bObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            bObj.transform.SetParent(parent, false);
            bObj.GetComponent<Image>().color = new Color(0.25f, 0.55f, 0.35f, 1f);

            btn = bObj.GetComponent<Button>();
            RectTransform rect = bObj.GetComponent<RectTransform>();
            rect.anchorMin = minAnchor;
            rect.anchorMax = maxAnchor;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(bObj.transform, false);
            TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
            txt.text = text;
            txt.fontSize = 14;
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;

            RectTransform tRect = txtObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            return bObj;
        }
    }
}
