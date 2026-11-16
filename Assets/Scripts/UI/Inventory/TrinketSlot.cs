using Inventory;
using Players;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TrinketSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
    [SerializeField] public Trinket trinket;
    private bool isEquipped = false;
    private Image iconImage;
    private Player player;

    public bool IsEquipped => isEquipped;
    public Image IconImage => iconImage;
    private InventoryManager InventoryManager => this.Inject<InventoryManager>();

    public void Initialize() {
        iconImage = GetComponent<Image>();
        player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Player>();

        if (player == null) return;

        RefreshSlot(); // Set initial state

        // Subscribe to Trinket unlock event
        if (trinket != null) {
            trinket.OnTrinketLockUpdate += HandleTrinketLockUpdate;
        }
    }

    private void OnDestroy() {
        // Unsubscribe to prevent memory leaks
        if (trinket != null) {
            trinket.OnTrinketLockUpdate -= HandleTrinketLockUpdate;
        }
    }

    private void HandleTrinketLockUpdate(Trinket unlockedTrinket, bool locked) {
        if (unlockedTrinket == trinket) {
            RefreshSlot();
        }
    }

    public void RefreshSlot() {
        if (trinket == null) return;

        iconImage.sprite = trinket.icon;

        if (trinket.IsLocked) {
            iconImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Greyed out if locked
        } else if (isEquipped) {
            iconImage.color = new Color(1f, 0.84f, 0f, 1f); // Gold if equipped
        } else {
            iconImage.color = Color.white; // Normal state
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (!trinket.IsLocked) {
            InventoryManager.UpdateDescription(trinket.trinketName, trinket.description);
        }
    }

    public void OnPointerExit(PointerEventData eventData) {
        InventoryManager.ClearDescription();
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (trinket.IsLocked) return; // Prevent interaction if locked
        InventoryManager.EquipTrinket(this);
    }

    public void SetEquipped(bool equipped) {
        if (trinket.IsLocked) return; // Do nothing if locked

        isEquipped = equipped;
        RefreshSlot(); // Update UI state

        if (equipped) {
            trinket.ApplyEffect(player);
        } else {
            trinket.RemoveEffect(player);
        }
    }
}
