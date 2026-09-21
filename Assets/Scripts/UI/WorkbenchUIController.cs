using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ZombieApocalypse.Crafting;
using ZombieApocalypse.Inventory;
using ZombieApocalypse.Player;
using ZombieApocalypse.Save;
using ZombieApocalypse.Weapons;
using ZombieApocalypse.World;

namespace ZombieApocalypse.UI
{
    public enum WorkbenchTab
    {
        Crafting,
        Upgrades
    }

    /// <summary>
    /// Central controller for Crafting & Workbench UI overlay.
    /// Handles physical Workbench interaction events, tab switching, recipe listing, atomic crafting calls,
    /// effective weapon stat displays, material-based weapon upgrades, immediate UI refreshing,
    /// input locking, and cursor management.
    /// 
    /// ATTACH TO: Gameplay Canvas Workbench UI root GameObject.
    /// </summary>
    public class WorkbenchUIController : MonoBehaviour
    {
        public static WorkbenchUIController Instance { get; private set; }

        [Header("Main Panels")]
        [SerializeField] private GameObject workbenchPanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button craftingTabButton;
        [SerializeField] private Button upgradesTabButton;
        [SerializeField] private GameObject craftingSection;
        [SerializeField] private GameObject upgradesSection;

        [Header("Crafting UI Elements")]
        [SerializeField] private Transform recipeGridContainer;
        [SerializeField] private TextMeshProUGUI recipeTitleText;
        [SerializeField] private TextMeshProUGUI recipeDescText;
        [SerializeField] private TextMeshProUGUI recipeRequirementsText;
        [SerializeField] private TextMeshProUGUI recipeOutputText;
        [SerializeField] private Button craftButton;
        [SerializeField] private TextMeshProUGUI craftingFeedbackText;

        [Header("Weapon Upgrade UI Elements")]
        [SerializeField] private Transform weaponGridContainer;
        [SerializeField] private TextMeshProUGUI weaponTitleText;
        [SerializeField] private TextMeshProUGUI currentStatsText;
        [SerializeField] private TextMeshProUGUI nextStatsText;
        [SerializeField] private TextMeshProUGUI upgradeCostText;
        [SerializeField] private Button damageUpgradeButton;
        [SerializeField] private Button magazineUpgradeButton;
        [SerializeField] private Button fireRateUpgradeButton;
        [SerializeField] private Button recoilUpgradeButton;
        [SerializeField] private TextMeshProUGUI upgradeFeedbackText;

        // Player & Systems references
        private CraftingSystem craftingSystem;
        private WeaponUpgradeSystem upgradeSystem;
        private InventorySystem inventorySystem;
        private WeaponController weaponController;
        private PlayerInputHandler inputHandler;
        private PlayerHealth playerHealth;

        // Runtime UI state
        private bool isOpen = false;
        private WorkbenchTab activeTab = WorkbenchTab.Crafting;
        private int selectedRecipeIndex = 0;
        private int selectedWeaponIndex = 0;
        private WeaponUpgradeType selectedUpgradeType = WeaponUpgradeType.Damage;

        private List<CraftingRecipeUIItem> recipeUIList = new List<CraftingRecipeUIItem>();
        private List<WeaponUpgradeUIItem> weaponUIList = new List<WeaponUpgradeUIItem>();

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
            Workbench.OnWorkbenchInteracted += HandleWorkbenchInteracted;
        }

        private void OnDisable()
        {
            Workbench.OnWorkbenchInteracted -= HandleWorkbenchInteracted;
        }

        private void Start()
        {
            FindAndBindPlayer();
            EnsureUIStructure();

            if (closeButton != null) closeButton.onClick.AddListener(CloseWorkbench);
            if (craftingTabButton != null) craftingTabButton.onClick.AddListener(() => SwitchTab(WorkbenchTab.Crafting));
            if (upgradesTabButton != null) upgradesTabButton.onClick.AddListener(() => SwitchTab(WorkbenchTab.Upgrades));

            if (craftButton != null) craftButton.onClick.AddListener(OnCraftButtonClicked);

            if (damageUpgradeButton != null) damageUpgradeButton.onClick.AddListener(() => SelectAndApplyUpgrade(WeaponUpgradeType.Damage));
            if (magazineUpgradeButton != null) magazineUpgradeButton.onClick.AddListener(() => SelectAndApplyUpgrade(WeaponUpgradeType.MagazineSize));
            if (fireRateUpgradeButton != null) fireRateUpgradeButton.onClick.AddListener(() => SelectAndApplyUpgrade(WeaponUpgradeType.FireRate));
            if (recoilUpgradeButton != null) recoilUpgradeButton.onClick.AddListener(() => SelectAndApplyUpgrade(WeaponUpgradeType.Recoil));

            if (workbenchPanel != null)
            {
                workbenchPanel.SetActive(false);
            }
        }

        public void FindAndBindPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                craftingSystem = player.GetComponent<CraftingSystem>();
                if (craftingSystem == null) craftingSystem = FindObjectOfType<CraftingSystem>();

                inventorySystem = player.GetComponent<InventorySystem>();
                weaponController = player.GetComponent<WeaponController>();
                inputHandler = player.GetComponent<PlayerInputHandler>();
                playerHealth = player.GetComponent<PlayerHealth>();

                if (craftingSystem != null && inventorySystem != null)
                {
                    craftingSystem.SetTargetInventory(inventorySystem);
                }
            }

            upgradeSystem = WeaponUpgradeSystem.Instance;
            if (upgradeSystem == null) upgradeSystem = FindObjectOfType<WeaponUpgradeSystem>();
        }

        private void Update()
        {
            if (playerHealth != null && playerHealth.IsDead)
            {
                if (isOpen) CloseWorkbench();
                return;
            }

            if (!isOpen) return;

            // Close UI hotkeys: Escape or E
            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)
                {
                    CloseWorkbench();
                }
            }
        }

        private void HandleWorkbenchInteracted(GameObject player)
        {
            if (playerHealth != null && playerHealth.IsDead) return;

            FindAndBindPlayer();
            OpenWorkbench();
        }

        public void OpenWorkbench()
        {
            if (playerHealth != null && playerHealth.IsDead) return;

            isOpen = true;
            if (workbenchPanel != null) workbenchPanel.SetActive(true);

            if (inputHandler != null)
            {
                inputHandler.IsInventoryOpen = true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SwitchTab(activeTab);
        }

        public void CloseWorkbench()
        {
            isOpen = false;
            if (workbenchPanel != null) workbenchPanel.SetActive(false);

            if (inputHandler != null)
            {
                inputHandler.IsInventoryOpen = false;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void SwitchTab(WorkbenchTab tab)
        {
            activeTab = tab;

            if (craftingSection != null) craftingSection.SetActive(activeTab == WorkbenchTab.Crafting);
            if (upgradesSection != null) upgradesSection.SetActive(activeTab == WorkbenchTab.Upgrades);

            if (craftingTabButton != null)
            {
                ColorBlock colors = craftingTabButton.colors;
                colors.normalColor = activeTab == WorkbenchTab.Crafting ? new Color(0.9f, 0.65f, 0.15f) : new Color(0.3f, 0.3f, 0.3f);
                craftingTabButton.colors = colors;
            }

            if (upgradesTabButton != null)
            {
                ColorBlock colors = upgradesTabButton.colors;
                colors.normalColor = activeTab == WorkbenchTab.Upgrades ? new Color(0.9f, 0.65f, 0.15f) : new Color(0.3f, 0.3f, 0.3f);
                upgradesTabButton.colors = colors;
            }

            if (activeTab == WorkbenchTab.Crafting)
            {
                RefreshCraftingTab();
            }
            else
            {
                RefreshUpgradesTab();
            }
        }

        // ==========================================
        // CRAFTING TAB LOGIC
        // ==========================================

        public void RefreshCraftingTab()
        {
            if (craftingSystem == null) FindAndBindPlayer();

            List<CraftingRecipeData> recipes = craftingSystem != null ? craftingSystem.GetAvailableRecipes() : new List<CraftingRecipeData>();

            BuildRecipeList(recipes);

            if (recipes.Count > 0)
            {
                if (selectedRecipeIndex < 0 || selectedRecipeIndex >= recipes.Count)
                {
                    selectedRecipeIndex = 0;
                }
                UpdateRecipeDetails(recipes[selectedRecipeIndex]);
            }
            else
            {
                ClearRecipeDetails("No crafting recipes available.");
            }
        }

        private void BuildRecipeList(List<CraftingRecipeData> recipes)
        {
            if (recipeGridContainer == null) return;

            foreach (Transform child in recipeGridContainer)
            {
                Destroy(child.gameObject);
            }
            recipeUIList.Clear();

            for (int i = 0; i < recipes.Count; i++)
            {
                int index = i;
                CraftingRecipeData recipe = recipes[i];

                GameObject cardObj = new GameObject($"RecipeCard_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
                cardObj.transform.SetParent(recipeGridContainer, false);

                Image bg = cardObj.GetComponent<Image>();
                bg.color = new Color(0.2f, 0.2f, 0.2f, 0.85f);

                GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
                titleObj.transform.SetParent(cardObj.transform, false);
                TextMeshProUGUI titleTxt = titleObj.GetComponent<TextMeshProUGUI>();
                titleTxt.fontSize = 15;
                titleTxt.fontStyle = FontStyles.Bold;
                titleTxt.color = Color.white;
                RectTransform titleRect = titleObj.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0.05f, 0.5f);
                titleRect.anchorMax = new Vector2(0.95f, 0.95f);
                titleRect.offsetMin = Vector2.zero;
                titleRect.offsetMax = Vector2.zero;

                GameObject outObj = new GameObject("Output", typeof(RectTransform), typeof(TextMeshProUGUI));
                outObj.transform.SetParent(cardObj.transform, false);
                TextMeshProUGUI outTxt = outObj.GetComponent<TextMeshProUGUI>();
                outTxt.fontSize = 12;
                outTxt.color = new Color(0.8f, 0.8f, 0.8f);
                RectTransform outRect = outObj.GetComponent<RectTransform>();
                outRect.anchorMin = new Vector2(0.05f, 0.05f);
                outRect.anchorMax = new Vector2(0.95f, 0.5f);
                outRect.offsetMin = Vector2.zero;
                outRect.offsetMax = Vector2.zero;

                CraftingRecipeUIItem itemUI = cardObj.AddComponent<CraftingRecipeUIItem>();
                itemUI.BindElements(bg, titleTxt, outTxt);
                itemUI.Init(index, recipe, OnRecipeCardClicked);
                itemUI.SetData(recipe, index == selectedRecipeIndex);

                recipeUIList.Add(itemUI);
            }
        }

        private void OnRecipeCardClicked(int index)
        {
            selectedRecipeIndex = index;
            if (craftingFeedbackText != null) craftingFeedbackText.text = "";
            RefreshCraftingTab();
        }

        private void UpdateRecipeDetails(CraftingRecipeData recipe)
        {
            if (recipe == null)
            {
                ClearRecipeDetails("Invalid recipe.");
                return;
            }

            if (recipeTitleText != null) recipeTitleText.text = recipe.displayName.ToUpper();
            if (recipeDescText != null) recipeDescText.text = recipe.description;

            // Requirements List
            if (recipeRequirementsText != null)
            {
                string reqStr = "<b>REQUIRED MATERIALS:</b>\n";
                if (recipe.ingredients != null && recipe.ingredients.Count > 0)
                {
                    foreach (var ing in recipe.ingredients)
                    {
                        if (ing == null || ing.item == null) continue;
                        int owned = inventorySystem != null ? inventorySystem.GetItemQuantity(ing.item) : 0;
                        bool hasEnough = owned >= ing.quantity;
                        string colorHex = hasEnough ? "#4CAF50" : "#F44336";
                        reqStr += $"• <color={colorHex}>{ing.item.itemName}: {owned} / {ing.quantity}</color>\n";
                    }
                }
                else
                {
                    reqStr += "None\n";
                }
                recipeRequirementsText.text = reqStr;
            }

            // Output List
            if (recipeOutputText != null)
            {
                string outStr = "<b>PRODUCES:</b>\n";
                if (recipe.outputs != null && recipe.outputs.Count > 0)
                {
                    foreach (var outItem in recipe.outputs)
                    {
                        if (outItem == null || outItem.item == null) continue;
                        outStr += $"• <color=#FFC107>{outItem.item.itemName} x{outItem.quantity}</color>\n";
                    }
                }
                recipeOutputText.text = outStr;
            }

            // Validate craft button state
            CraftingResult check = craftingSystem != null ? craftingSystem.CanCraft(recipe) : CraftingResult.CreateFailed(recipe.recipeId, "CraftingSystem unavailable.");
            if (craftButton != null)
            {
                craftButton.interactable = check.Success;
            }
        }

        private void ClearRecipeDetails(string message)
        {
            if (recipeTitleText != null) recipeTitleText.text = "SELECT A RECIPE";
            if (recipeDescText != null) recipeDescText.text = message;
            if (recipeRequirementsText != null) recipeRequirementsText.text = "";
            if (recipeOutputText != null) recipeOutputText.text = "";
            if (craftButton != null) craftButton.interactable = false;
        }

        private void OnCraftButtonClicked()
        {
            if (craftingSystem == null) FindAndBindPlayer();
            if (craftingSystem == null) return;

            var recipes = craftingSystem.GetAvailableRecipes();
            if (selectedRecipeIndex < 0 || selectedRecipeIndex >= recipes.Count) return;

            CraftingRecipeData recipe = recipes[selectedRecipeIndex];
            CraftingResult result = craftingSystem.TryCraft(recipe);

            if (craftingFeedbackText != null)
            {
                craftingFeedbackText.text = result.Success
                    ? $"<color=#4CAF50>Crafted {recipe.displayName} successfully!</color>"
                    : $"<color=#F44336>{result.message}</color>";
            }

            RefreshCraftingTab();
        }

        // ==========================================
        // WEAPON UPGRADES TAB LOGIC
        // ==========================================

        public void RefreshUpgradesTab()
        {
            if (weaponController == null || upgradeSystem == null) FindAndBindPlayer();

            List<WeaponData> weapons = GetAvailableWeapons();
            BuildWeaponList(weapons);

            if (weapons.Count > 0)
            {
                if (selectedWeaponIndex < 0 || selectedWeaponIndex >= weapons.Count)
                {
                    selectedWeaponIndex = 0;
                }
                UpdateWeaponDetails(weapons[selectedWeaponIndex]);
            }
            else
            {
                ClearWeaponDetails("No weapons equipped or available.");
            }
        }

        private List<WeaponData> GetAvailableWeapons()
        {
            List<WeaponData> list = new List<WeaponData>();

            if (weaponController != null)
            {
                var slotSave = weaponController.GetWeaponSaveData();
                if (slotSave != null && slotSave.slots != null)
                {
                    foreach (var s in slotSave.slots)
                    {
                        if (s == null || string.IsNullOrEmpty(s.weaponName)) continue;
                        WeaponData data = FindWeaponDataByName(s.weaponName);
                        if (data != null && !list.Contains(data))
                        {
                            list.Add(data);
                        }
                    }
                }
            }

            // Fallback load default weapons if list is empty
            if (list.Count == 0)
            {
                WeaponData[] allWeapons = Resources.FindObjectsOfTypeAll<WeaponData>();
                foreach (var w in allWeapons)
                {
                    if (w != null && !list.Contains(w)) list.Add(w);
                }
            }

            return list;
        }

        private WeaponData FindWeaponDataByName(string name)
        {
            WeaponData[] allWeapons = Resources.FindObjectsOfTypeAll<WeaponData>();
            return Array.Find(allWeapons, w => w != null && string.Equals(w.weaponName, name, StringComparison.OrdinalIgnoreCase));
        }

        private void BuildWeaponList(List<WeaponData> weapons)
        {
            if (weaponGridContainer == null) return;

            foreach (Transform child in weaponGridContainer)
            {
                Destroy(child.gameObject);
            }
            weaponUIList.Clear();

            for (int i = 0; i < weapons.Count; i++)
            {
                int index = i;
                WeaponData weapon = weapons[i];

                GameObject cardObj = new GameObject($"WeaponCard_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
                cardObj.transform.SetParent(weaponGridContainer, false);

                Image bg = cardObj.GetComponent<Image>();
                bg.color = new Color(0.2f, 0.2f, 0.2f, 0.85f);

                GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
                titleObj.transform.SetParent(cardObj.transform, false);
                TextMeshProUGUI titleTxt = titleObj.GetComponent<TextMeshProUGUI>();
                titleTxt.fontSize = 15;
                titleTxt.fontStyle = FontStyles.Bold;
                titleTxt.color = Color.white;
                RectTransform titleRect = titleObj.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0.05f, 0.5f);
                titleRect.anchorMax = new Vector2(0.95f, 0.95f);
                titleRect.offsetMin = Vector2.zero;
                titleRect.offsetMax = Vector2.zero;

                GameObject sumObj = new GameObject("Summary", typeof(RectTransform), typeof(TextMeshProUGUI));
                sumObj.transform.SetParent(cardObj.transform, false);
                TextMeshProUGUI sumTxt = sumObj.GetComponent<TextMeshProUGUI>();
                sumTxt.fontSize = 12;
                sumTxt.color = new Color(0.8f, 0.8f, 0.8f);
                RectTransform sumRect = sumObj.GetComponent<RectTransform>();
                sumRect.anchorMin = new Vector2(0.05f, 0.05f);
                sumRect.anchorMax = new Vector2(0.95f, 0.5f);
                sumRect.offsetMin = Vector2.zero;
                sumRect.offsetMax = Vector2.zero;

                string summary = GetWeaponUpgradeSummary(weapon);

                WeaponUpgradeUIItem itemUI = cardObj.AddComponent<WeaponUpgradeUIItem>();
                itemUI.BindElements(bg, titleTxt, sumTxt);
                itemUI.Init(index, weapon, OnWeaponCardClicked);
                itemUI.SetData(weapon, summary, index == selectedWeaponIndex);

                weaponUIList.Add(itemUI);
            }
        }

        private string GetWeaponUpgradeSummary(WeaponData weapon)
        {
            if (weapon == null || upgradeSystem == null) return "Lvl 0";

            int dmg = upgradeSystem.GetUpgradeLevel(weapon.weaponName, WeaponUpgradeType.Damage);
            int mag = upgradeSystem.GetUpgradeLevel(weapon.weaponName, WeaponUpgradeType.MagazineSize);
            int rate = upgradeSystem.GetUpgradeLevel(weapon.weaponName, WeaponUpgradeType.FireRate);
            int rec = upgradeSystem.GetUpgradeLevel(weapon.weaponName, WeaponUpgradeType.Recoil);

            return $"Dmg Lvl {dmg} | Mag Lvl {mag} | Rate Lvl {rate} | Rec Lvl {rec}";
        }

        private void OnWeaponCardClicked(int index)
        {
            selectedWeaponIndex = index;
            if (upgradeFeedbackText != null) upgradeFeedbackText.text = "";
            RefreshUpgradesTab();
        }

        private void UpdateWeaponDetails(WeaponData weapon)
        {
            if (weapon == null || upgradeSystem == null)
            {
                ClearWeaponDetails("Invalid weapon data.");
                return;
            }

            if (weaponTitleText != null) weaponTitleText.text = weapon.weaponName.ToUpper();

            // Effective Stats Calculation
            float effDmg = upgradeSystem.GetEffectiveDamage(weapon);
            int effMag = upgradeSystem.GetEffectiveMagazineSize(weapon);
            float effRate = upgradeSystem.GetEffectiveFireRate(weapon);
            float effRec = upgradeSystem.GetEffectiveRecoil(weapon);

            if (currentStatsText != null)
            {
                currentStatsText.text = $"<b>CURRENT EFFECTIVE STATS:</b>\n" +
                                        $"Damage: {effDmg:F1}\n" +
                                        $"Magazine: {effMag}\n" +
                                        $"Fire Rate: {effRate:F2}s\n" +
                                        $"Recoil: {effRec:F2}";
            }

            // Stat Preview & Upgrade Cost for Selected Category
            int currentLevel = upgradeSystem.GetUpgradeLevel(weapon.weaponName, selectedUpgradeType);
            bool isMaxLevel = currentLevel >= 3;

            if (nextStatsText != null)
            {
                if (isMaxLevel)
                {
                    nextStatsText.text = $"<b>UPGRADE PREVIEW ({selectedUpgradeType}):</b>\n<color=#FFC107>MAX LEVEL REACHED (Lvl 3)</color>";
                }
                else
                {
                    string previewStr = $"<b>UPGRADE PREVIEW ({selectedUpgradeType}):</b>\nLevel {currentLevel} → Level {currentLevel + 1}\n";

                    switch (selectedUpgradeType)
                    {
                        case WeaponUpgradeType.Damage:
                            float nextDmg = weapon.damage + ((currentLevel + 1) * 5.0f);
                            previewStr += $"Damage: {effDmg:F1} → <color=#4CAF50>{nextDmg:F1}</color>";
                            break;
                        case WeaponUpgradeType.MagazineSize:
                            int nextMag = weapon.magazineSize + ((currentLevel + 1) * 4);
                            previewStr += $"Magazine: {effMag} → <color=#4CAF50>{nextMag}</color>";
                            break;
                        case WeaponUpgradeType.FireRate:
                            float nextRate = Mathf.Max(0.05f, weapon.fireRate - ((currentLevel + 1) * 0.02f));
                            previewStr += $"Fire Rate: {effRate:F2}s → <color=#4CAF50>{nextRate:F2}s</color>";
                            break;
                        case WeaponUpgradeType.Recoil:
                            float nextRec = Mathf.Max(0.1f, weapon.recoilAmount - ((currentLevel + 1) * 0.15f));
                            previewStr += $"Recoil: {effRec:F2} → <color=#4CAF50>{nextRec:F2}</color>";
                            break;
                    }
                    nextStatsText.text = previewStr;
                }
            }

            // Upgrade Costs
            List<WeaponUpgradeCost> costs = GetUpgradeCost(selectedUpgradeType, currentLevel);
            if (upgradeCostText != null)
            {
                if (isMaxLevel)
                {
                    upgradeCostText.text = "";
                }
                else
                {
                    string costStr = "<b>UPGRADE COST:</b>\n";
                    foreach (var c in costs)
                    {
                        if (c == null || c.item == null) continue;
                        int owned = inventorySystem != null ? inventorySystem.GetItemQuantity(c.item) : 0;
                        bool hasEnough = owned >= c.quantity;
                        string colorHex = hasEnough ? "#4CAF50" : "#F44336";
                        costStr += $"• <color={colorHex}>{c.item.itemName}: {owned} / {c.quantity}</color>\n";
                    }
                    upgradeCostText.text = costStr;
                }
            }

            // Validate Upgrade Buttons
            UpdateButtonState(damageUpgradeButton, weapon, WeaponUpgradeType.Damage);
            UpdateButtonState(magazineUpgradeButton, weapon, WeaponUpgradeType.MagazineSize);
            UpdateButtonState(fireRateUpgradeButton, weapon, WeaponUpgradeType.FireRate);
            UpdateButtonState(recoilUpgradeButton, weapon, WeaponUpgradeType.Recoil);
        }

        private void UpdateButtonState(Button btn, WeaponData weapon, WeaponUpgradeType type)
        {
            if (btn == null) return;
            int level = upgradeSystem != null ? upgradeSystem.GetUpgradeLevel(weapon.weaponName, type) : 0;
            List<WeaponUpgradeCost> costs = GetUpgradeCost(type, level);
            WeaponUpgradeResult check = upgradeSystem != null
                ? upgradeSystem.CanUpgrade(weapon, type, inventorySystem, costs)
                : WeaponUpgradeResult.CreateFailed(weapon.weaponName, type, "UpgradeSystem unavailable.");

            btn.interactable = check.Success;
        }

        private List<WeaponUpgradeCost> GetUpgradeCost(WeaponUpgradeType type, int currentLevel)
        {
            List<WeaponUpgradeCost> costs = new List<WeaponUpgradeCost>();
            int requiredQty = currentLevel + 1;
            ItemData costItem = null;

            switch (type)
            {
                case WeaponUpgradeType.Damage:
                    costItem = ItemRegistry.GetItem("item_rifle_ammo") ?? ItemRegistry.GetItem("item_bandage");
                    break;
                case WeaponUpgradeType.MagazineSize:
                    costItem = ItemRegistry.GetItem("item_pistol_ammo") ?? ItemRegistry.GetItem("item_canned_food");
                    break;
                case WeaponUpgradeType.FireRate:
                    costItem = ItemRegistry.GetItem("item_shotgun_shells") ?? ItemRegistry.GetItem("item_water_bottle");
                    break;
                case WeaponUpgradeType.Recoil:
                    costItem = ItemRegistry.GetItem("item_water_bottle") ?? ItemRegistry.GetItem("item_medkit");
                    break;
            }

            if (costItem != null)
            {
                costs.Add(new WeaponUpgradeCost { item = costItem, quantity = requiredQty });
            }

            return costs;
        }

        private void SelectAndApplyUpgrade(WeaponUpgradeType type)
        {
            selectedUpgradeType = type;

            if (weaponController == null || upgradeSystem == null) FindAndBindPlayer();
            List<WeaponData> weapons = GetAvailableWeapons();

            if (selectedWeaponIndex < 0 || selectedWeaponIndex >= weapons.Count) return;
            WeaponData weapon = weapons[selectedWeaponIndex];

            int level = upgradeSystem.GetUpgradeLevel(weapon.weaponName, type);
            List<WeaponUpgradeCost> costs = GetUpgradeCost(type, level);

            WeaponUpgradeResult result = upgradeSystem.TryUpgrade(weapon, type, inventorySystem, costs);

            if (upgradeFeedbackText != null)
            {
                upgradeFeedbackText.text = result.Success
                    ? $"<color=#4CAF50>Upgraded {weapon.weaponName} {type} to Level {result.message}!</color>"
                    : $"<color=#F44336>{result.message}</color>";
            }

            RefreshUpgradesTab();
        }

        private void ClearWeaponDetails(string message)
        {
            if (weaponTitleText != null) weaponTitleText.text = "SELECT A WEAPON";
            if (currentStatsText != null) currentStatsText.text = message;
            if (nextStatsText != null) nextStatsText.text = "";
            if (upgradeCostText != null) upgradeCostText.text = "";

            if (damageUpgradeButton != null) damageUpgradeButton.interactable = false;
            if (magazineUpgradeButton != null) magazineUpgradeButton.interactable = false;
            if (fireRateUpgradeButton != null) fireRateUpgradeButton.interactable = false;
            if (recoilUpgradeButton != null) recoilUpgradeButton.interactable = false;
        }

        // ==========================================
        // DYNAMIC UI SETUP & FALLBACK CREATION
        // ==========================================

        private void EnsureUIStructure()
        {
            if (workbenchPanel != null) return;

            // Dynamically construct Workbench UI canvas overlay if elements not assigned
            GameObject canvasObj = GameObject.Find("GameplayCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("GameplayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            workbenchPanel = new GameObject("WorkbenchPanel", typeof(RectTransform), typeof(Image));
            workbenchPanel.transform.SetParent(canvasObj.transform, false);

            RectTransform mainRect = workbenchPanel.GetComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0.1f, 0.08f);
            mainRect.anchorMax = new Vector2(0.9f, 0.92f);
            mainRect.offsetMin = Vector2.zero;
            mainRect.offsetMax = Vector2.zero;

            Image mainBg = workbenchPanel.GetComponent<Image>();
            mainBg.color = new Color(0.08f, 0.08f, 0.1f, 0.96f);

            // Header bar
            GameObject headerObj = new GameObject("Header", typeof(RectTransform), typeof(Image));
            headerObj.transform.SetParent(workbenchPanel.transform, false);
            RectTransform headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0.9f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
            headerObj.GetComponent<Image>().color = new Color(0.14f, 0.14f, 0.18f, 1f);

            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(headerObj.transform, false);
            TextMeshProUGUI titleTxt = titleObj.GetComponent<TextMeshProUGUI>();
            titleTxt.text = "SAFE HOUSE WORKBENCH";
            titleTxt.fontSize = 22;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.color = new Color(1f, 0.85f, 0.2f);
            titleTxt.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.02f, 0f);
            tRect.anchorMax = new Vector2(0.5f, 1f);

            // Tabs Header Buttons
            GameObject craftTabObj = CreateHeaderButton(headerObj.transform, "CraftingTabBtn", "CRAFTING", new Vector2(0.5f, 0.1f), new Vector2(0.7f, 0.9f), out craftingTabButton);
            GameObject upgTabObj = CreateHeaderButton(headerObj.transform, "UpgradesTabBtn", "WEAPON UPGRADES", new Vector2(0.71f, 0.1f), new Vector2(0.91f, 0.9f), out upgradesTabButton);
            GameObject closeObj = CreateHeaderButton(headerObj.transform, "CloseBtn", "X", new Vector2(0.93f, 0.15f), new Vector2(0.98f, 0.85f), out closeButton);

            // Crafting Section
            craftingSection = new GameObject("CraftingSection", typeof(RectTransform));
            craftingSection.transform.SetParent(workbenchPanel.transform, false);
            RectTransform craftRect = craftingSection.GetComponent<RectTransform>();
            craftRect.anchorMin = new Vector2(0f, 0f);
            craftRect.anchorMax = new Vector2(1f, 0.9f);
            craftRect.offsetMin = Vector2.zero;
            craftRect.offsetMax = Vector2.zero;

            // Recipe List Container (Left side)
            GameObject recipeListObj = new GameObject("RecipeListContainer", typeof(RectTransform), typeof(Image));
            recipeListObj.transform.SetParent(craftingSection.transform, false);
            RectTransform rListRect = recipeListObj.GetComponent<RectTransform>();
            rListRect.anchorMin = new Vector2(0.02f, 0.05f);
            rListRect.anchorMax = new Vector2(0.35f, 0.95f);
            rListRect.offsetMin = Vector2.zero;
            rListRect.offsetMax = Vector2.zero;
            recipeListObj.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.8f);
            recipeGridContainer = recipeListObj.transform;

            VerticalLayoutGroup vlgCraft = recipeListObj.AddComponent<VerticalLayoutGroup>();
            vlgCraft.spacing = 8;
            vlgCraft.childControlHeight = false;
            vlgCraft.childControlWidth = true;
            vlgCraft.childForceExpandHeight = false;

            // Recipe Details (Right side)
            GameObject recipeDetailsObj = new GameObject("RecipeDetailsContainer", typeof(RectTransform), typeof(Image));
            recipeDetailsObj.transform.SetParent(craftingSection.transform, false);
            RectTransform rDetRect = recipeDetailsObj.GetComponent<RectTransform>();
            rDetRect.anchorMin = new Vector2(0.37f, 0.05f);
            rDetRect.anchorMax = new Vector2(0.98f, 0.95f);
            rDetRect.offsetMin = Vector2.zero;
            rDetRect.offsetMax = Vector2.zero;
            recipeDetailsObj.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.8f);

            recipeTitleText = CreateTextElement(recipeDetailsObj.transform, "RecipeTitle", "SELECT A RECIPE", 20, FontStyles.Bold, new Color(1f, 0.85f, 0.2f), new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.95f));
            recipeDescText = CreateTextElement(recipeDetailsObj.transform, "RecipeDesc", "", 14, FontStyles.Normal, Color.white, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.85f));
            recipeRequirementsText = CreateTextElement(recipeDetailsObj.transform, "RecipeReqs", "", 14, FontStyles.Normal, Color.white, new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.72f));
            recipeOutputText = CreateTextElement(recipeDetailsObj.transform, "RecipeOutput", "", 14, FontStyles.Normal, Color.white, new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.42f));
            craftingFeedbackText = CreateTextElement(recipeDetailsObj.transform, "CraftFeedback", "", 14, FontStyles.Bold, Color.yellow, new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.22f));
            craftButton = CreateButtonElement(recipeDetailsObj.transform, "CraftBtn", "CRAFT ITEM", new Vector2(0.05f, 0.03f), new Vector2(0.45f, 0.13f));

            // Upgrades Section
            upgradesSection = new GameObject("UpgradesSection", typeof(RectTransform));
            upgradesSection.transform.SetParent(workbenchPanel.transform, false);
            RectTransform upgRect = upgradesSection.GetComponent<RectTransform>();
            upgRect.anchorMin = new Vector2(0f, 0f);
            upgRect.anchorMax = new Vector2(1f, 0.9f);
            upgRect.offsetMin = Vector2.zero;
            upgRect.offsetMax = Vector2.zero;

            // Weapon List Container (Left side)
            GameObject weaponListObj = new GameObject("WeaponListContainer", typeof(RectTransform), typeof(Image));
            weaponListObj.transform.SetParent(upgradesSection.transform, false);
            RectTransform wListRect = weaponListObj.GetComponent<RectTransform>();
            wListRect.anchorMin = new Vector2(0.02f, 0.05f);
            wListRect.anchorMax = new Vector2(0.35f, 0.95f);
            wListRect.offsetMin = Vector2.zero;
            wListRect.offsetMax = Vector2.zero;
            weaponListObj.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.8f);
            weaponGridContainer = weaponListObj.transform;

            VerticalLayoutGroup vlgUpg = weaponListObj.AddComponent<VerticalLayoutGroup>();
            vlgUpg.spacing = 8;
            vlgUpg.childControlHeight = false;
            vlgUpg.childControlWidth = true;
            vlgUpg.childForceExpandHeight = false;

            // Weapon Details Container (Right side)
            GameObject weaponDetailsObj = new GameObject("WeaponDetailsContainer", typeof(RectTransform), typeof(Image));
            weaponDetailsObj.transform.SetParent(upgradesSection.transform, false);
            RectTransform wDetRect = weaponDetailsObj.GetComponent<RectTransform>();
            wDetRect.anchorMin = new Vector2(0.37f, 0.05f);
            wDetRect.anchorMax = new Vector2(0.98f, 0.95f);
            wDetRect.offsetMin = Vector2.zero;
            wDetRect.offsetMax = Vector2.zero;
            weaponDetailsObj.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.8f);

            weaponTitleText = CreateTextElement(weaponDetailsObj.transform, "WeaponTitle", "SELECT A WEAPON", 20, FontStyles.Bold, new Color(1f, 0.85f, 0.2f), new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.95f));
            currentStatsText = CreateTextElement(weaponDetailsObj.transform, "CurrentStats", "", 13, FontStyles.Normal, Color.white, new Vector2(0.05f, 0.55f), new Vector2(0.48f, 0.85f));
            nextStatsText = CreateTextElement(weaponDetailsObj.transform, "NextStats", "", 13, FontStyles.Normal, Color.white, new Vector2(0.50f, 0.55f), new Vector2(0.95f, 0.85f));
            upgradeCostText = CreateTextElement(weaponDetailsObj.transform, "UpgradeCost", "", 13, FontStyles.Normal, Color.white, new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.55f));
            upgradeFeedbackText = CreateTextElement(weaponDetailsObj.transform, "UpgradeFeedback", "", 13, FontStyles.Bold, Color.yellow, new Vector2(0.05f, 0.26f), new Vector2(0.95f, 0.35f));

            // Upgrade Category Action Buttons
            damageUpgradeButton = CreateButtonElement(weaponDetailsObj.transform, "DmgUpgBtn", "UPGRADE DAMAGE", new Vector2(0.05f, 0.14f), new Vector2(0.48f, 0.24f));
            magazineUpgradeButton = CreateButtonElement(weaponDetailsObj.transform, "MagUpgBtn", "UPGRADE MAGAZINE", new Vector2(0.50f, 0.14f), new Vector2(0.95f, 0.24f));
            fireRateUpgradeButton = CreateButtonElement(weaponDetailsObj.transform, "RateUpgBtn", "UPGRADE FIRE RATE", new Vector2(0.05f, 0.03f), new Vector2(0.48f, 0.13f));
            recoilUpgradeButton = CreateButtonElement(weaponDetailsObj.transform, "RecoilUpgBtn", "UPGRADE RECOIL", new Vector2(0.50f, 0.03f), new Vector2(0.95f, 0.13f));

            upgradesSection.SetActive(false);
        }

        private GameObject CreateHeaderButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, out Button button)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            btnObj.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f, 1f);

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 14;
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;

            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;

            button = btnObj.GetComponent<Button>();
            return btnObj;
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

        private Button CreateButtonElement(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            btnObj.GetComponent<Image>().color = new Color(0.25f, 0.65f, 0.35f, 1f);

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 14;
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;

            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;

            return btnObj.GetComponent<Button>();
        }
    }
}
