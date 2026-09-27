using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using Utils;

public class InventoryManager : PersistentSingleton<InventoryManager> {
    [SerializeField] private GameObject inventoryUI; // Reference to Inventory UI Panel
    [SerializeField] private TrinketSlot[] trinketSlots; // Grid of trinket slots
    [SerializeField] private TrinketSlot equippedTrinket;
    [SerializeField] private TextMeshProUGUI descriptionText; // Description UI text box
    [SerializeField] private Image EquipDisplay;

    public delegate void InventoryEventHandler();
    public static event InventoryEventHandler OnInventoryOpened;   // for more complex functions that cannot use isPaused
    public static event InventoryEventHandler OnInventoryClosed;

    public bool IsInventoryOpen { get; private set; }

    protected override void OnAwake() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        CloseInventory();
        InitializeTrinketSlots();
        EquipDisplay.color = new Color(1, 1, 1, 1);
    }

    private void Start() {
        InitializeTrinketSlots();
        ClearDescription();
        inventoryUI.SetActive(false);
        EquipDisplay.color = new Color(1, 1, 1, 0);
    }

    private void Update() {
        if (PauseMenu.IsPaused || DialogueManager.Instance.IsInDialogue()) return;

        if (Input.GetKeyDown(KeyCode.I)) {
            ToggleInventory();
        } else if (Input.GetKeyDown(KeyCode.Tab)) {
            CloseInventory();
        }
    }

    private void InitializeTrinketSlots() {
        foreach (var slot in trinketSlots) {
            slot.Initialize();
        }
    }

    public void ToggleInventory() {
        IsInventoryOpen = !inventoryUI.activeSelf;
        inventoryUI.SetActive(IsInventoryOpen);
        if (IsInventoryOpen) OnInventoryOpened?.Invoke();
        else OnInventoryClosed?.Invoke();
    }

    public void CloseInventory() {
        IsInventoryOpen = false;
        inventoryUI.SetActive(false);
        OnInventoryClosed?.Invoke();
    }

    public void EquipTrinket(TrinketSlot selectedTrinket) {
        if (selectedTrinket.IsEquipped)
            return;

        equippedTrinket?.SetEquipped(false);
        equippedTrinket = selectedTrinket;
        selectedTrinket.SetEquipped(true);
        EquipDisplay.sprite = selectedTrinket.trinket.icon;
        EquipDisplay.color = new Color(1, 1, 1, 1);
    }


    public void UpdateDescription(string trinketName, string description) {
        descriptionText.text = trinketName + "\n" + description;
    }

    public void ClearDescription() {
        descriptionText.text = "";
    }
}
