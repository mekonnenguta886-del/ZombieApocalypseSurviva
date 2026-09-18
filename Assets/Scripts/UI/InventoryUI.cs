using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieApocalypse.Inventory;
using ZombieApocalypse.Player;

namespace ZombieApocalypse.UI
{
    /// <summary>
    /// Inventory UI controller handling the 20-slot grid overlay, item selection detail panel,
    /// USE and DROP button actions, gameplay input locking, cursor locking toggles, and I key toggle handling.
    /// 
    /// ATTACH TO: Inventory UI Canvas Panel GameObject.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("Main Panels")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Transform slotGridContainer;
        [SerializeField] private GameObject slotPrefab;

        [Header("Selected Item Panel")]
        [SerializeField] private Image selectedItemIcon;
        [SerializeField] private TextMeshProUGUI selectedItemTitle;
        [SerializeField] private TextMeshProUGUI selectedItemDescription;
        [SerializeField] private Button useButton;
        [SerializeField] private Button dropButton;
        [SerializeField] private Button closeButton;

        private InventorySystem inventorySystem;
        private PlayerInputHandler inputHandler;
        private PlayerHealth playerHealth;

        private List<InventorySlotUI> slotUIList = new List<InventorySlotUI>();
        private int selectedSlotIndex = -1;
        private bool isOpen = false;

        public bool IsOpen => isOpen;

        private void Start()
        {
            FindAndBindPlayer();

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseInventory);
            }

            if (useButton != null)
            {
                useButton.onClick.AddListener(OnUseButtonClicked);
            }

            if (dropButton != null)
            {
                dropButton.onClick.AddListener(OnDropButtonClicked);
            }

            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }
        }

        public void FindAndBindPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                inventorySystem = player.GetComponent<InventorySystem>();
                inputHandler = player.GetComponent<PlayerInputHandler>();
                playerHealth = player.GetComponent<PlayerHealth>();

                if (inventorySystem != null)
                {
                    inventorySystem.OnInventoryUpdated += RefreshUI;
                }
            }

            InitializeSlots();
        }

        private void Update()
        {
            if (playerHealth != null && playerHealth.IsDead)
            {
                if (isOpen) CloseInventory();
                return;
            }

            if (inputHandler != null && inputHandler.InventoryTriggered)
            {
                ToggleInventory();
                inputHandler.ResetInventoryTrigger();
            }
        }

        private void OnDestroy()
        {
            if (inventorySystem != null)
            {
                inventorySystem.OnInventoryUpdated -= RefreshUI;
            }
        }

        public void ToggleInventory()
        {
            if (isOpen)
            {
                CloseInventory();
            }
            else
            {
                OpenInventory();
            }
        }

        public void OpenInventory()
        {
            if (playerHealth != null && playerHealth.IsDead) return;

            isOpen = true;
            if (inventoryPanel != null) inventoryPanel.SetActive(true);

            if (inputHandler != null)
            {
                inputHandler.IsInventoryOpen = true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            RefreshUI();
        }

        public void CloseInventory()
        {
            isOpen = false;
            if (inventoryPanel != null) inventoryPanel.SetActive(false);

            if (inputHandler != null)
            {
                inputHandler.IsInventoryOpen = false;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void InitializeSlots()
        {
            // Clear old children if any
            if (slotGridContainer != null)
            {
                foreach (Transform child in slotGridContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            slotUIList.Clear();
            int totalSlots = inventorySystem != null ? inventorySystem.MaxSlots : 20;

            for (int i = 0; i < totalSlots; i++)
            {
                int index = i;
                GameObject slotObj = null;

                if (slotPrefab != null && slotGridContainer != null)
                {
                    slotObj = Instantiate(slotPrefab, slotGridContainer);
                }
                else
                {
                    // Fallback runtime UI creation if slotPrefab not assigned
                    slotObj = CreateDefaultSlotUI(i);
                }

                InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
                if (slotUI == null)
                {
                    slotUI = slotObj.AddComponent<InventorySlotUI>();
                }

                slotUI.Init(index, OnSlotClicked);
                slotUIList.Add(slotUI);
            }

            ClearSelection();
        }

        private GameObject CreateDefaultSlotUI(int index)
        {
            GameObject slotObj = new GameObject($"Slot_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
            slotObj.transform.SetParent(slotGridContainer, false);

            Image bg = slotObj.GetComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

            // Icon Image child
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(slotObj.transform, false);
            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.color = Color.white;
            iconImg.gameObject.SetActive(false);

            // Quantity Text child
            GameObject qtyObj = new GameObject("QtyText", typeof(RectTransform), typeof(TextMeshProUGUI));
            qtyObj.transform.SetParent(slotObj.transform, false);
            TextMeshProUGUI qtyText = qtyObj.GetComponent<TextMeshProUGUI>();
            qtyText.fontSize = 14;
            qtyText.alignment = TextAlignmentOptions.BottomRight;

            InventorySlotUI slotUI = slotObj.AddComponent<InventorySlotUI>();
            slotUI.BindElements(bg, iconImg, qtyText);

            return slotObj;
        }

        public void RefreshUI()
        {
            if (inventorySystem == null) return;

            var slotsData = inventorySystem.Slots;
            int maxSlots = inventorySystem.MaxSlots;

            for (int i = 0; i < maxSlots; i++)
            {
                if (i < slotUIList.Count)
                {
                    if (i < slotsData.Count && slotsData[i] != null && slotsData[i].itemData != null)
                    {
                        slotUIList[i].SetData(slotsData[i].itemData, slotsData[i].quantity, i == selectedSlotIndex);
                    }
                    else
                    {
                        slotUIList[i].SetEmpty(i == selectedSlotIndex);
                    }
                }
            }

            // Update selection details
            if (selectedSlotIndex >= 0 && selectedSlotIndex < slotsData.Count && slotsData[selectedSlotIndex] != null)
            {
                UpdateSelectionDetails(slotsData[selectedSlotIndex].itemData);
            }
            else
            {
                ClearSelection();
            }
        }

        private void OnSlotClicked(int index)
        {
            selectedSlotIndex = index;
            RefreshUI();
        }

        private void UpdateSelectionDetails(ItemData item)
        {
            if (item == null)
            {
                ClearSelection();
                return;
            }

            if (selectedItemTitle != null) selectedItemTitle.text = item.itemName;
            if (selectedItemDescription != null) selectedItemDescription.text = item.description;

            if (selectedItemIcon != null)
            {
                selectedItemIcon.sprite = item.itemIcon;
                selectedItemIcon.gameObject.SetActive(item.itemIcon != null);
            }

            if (useButton != null) useButton.interactable = item.isConsumable || item.category == ItemCategory.Ammunition;
            if (dropButton != null) dropButton.interactable = true;
        }

        private void ClearSelection()
        {
            if (selectedItemTitle != null) selectedItemTitle.text = "Select an Item";
            if (selectedItemDescription != null) selectedItemDescription.text = "Click any inventory slot to view item details and actions.";
            if (selectedItemIcon != null) selectedItemIcon.gameObject.SetActive(false);

            if (useButton != null) useButton.interactable = false;
            if (dropButton != null) dropButton.interactable = false;
        }

        private void OnUseButtonClicked()
        {
            if (selectedSlotIndex < 0 || inventorySystem == null) return;
            inventorySystem.UseItem(selectedSlotIndex);
        }

        private void OnDropButtonClicked()
        {
            if (selectedSlotIndex < 0 || inventorySystem == null) return;
            inventorySystem.DropItemAt(selectedSlotIndex);
        }
    }

    /// <summary>
    /// Slot UI component managing individual slot visual state, item icon, quantity badge, and click callback.
    /// </summary>
    public class InventorySlotUI : MonoBehaviour
    {
        private int slotIndex;
        private System.Action<int> onClickCallback;

        [SerializeField] private Image slotBackground;
        [SerializeField] private Image itemIconImage;
        [SerializeField] private TextMeshProUGUI quantityText;

        public void BindElements(Image bg, Image icon, TextMeshProUGUI qty)
        {
            slotBackground = bg;
            itemIconImage = icon;
            quantityText = qty;
        }

        public void Init(int index, System.Action<int> clickCallback)
        {
            slotIndex = index;
            onClickCallback = clickCallback;

            Button btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => onClickCallback?.Invoke(slotIndex));
            }
        }

        public void SetData(ItemData item, int qty, bool isSelected)
        {
            if (itemIconImage != null)
            {
                itemIconImage.sprite = item.itemIcon;
                itemIconImage.gameObject.SetActive(item.itemIcon != null);
            }

            if (quantityText != null)
            {
                quantityText.text = qty > 1 ? qty.ToString() : "";
                quantityText.gameObject.SetActive(qty > 1);
            }

            if (slotBackground != null)
            {
                slotBackground.color = isSelected 
                    ? new Color(0.9f, 0.7f, 0.2f, 0.95f) // Selected highlight
                    : new Color(0.25f, 0.25f, 0.25f, 0.85f);
            }
        }

        public void SetEmpty(bool isSelected)
        {
            if (itemIconImage != null) itemIconImage.gameObject.SetActive(false);
            if (quantityText != null) quantityText.gameObject.SetActive(false);

            if (slotBackground != null)
            {
                slotBackground.color = isSelected 
                    ? new Color(0.7f, 0.5f, 0.2f, 0.85f) 
                    : new Color(0.15f, 0.15f, 0.15f, 0.75f);
            }
        }
    }
}
