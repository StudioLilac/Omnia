using System.Collections;
using System.Collections.Generic;
using Inventory;
using Players;
using UnityEngine;
using UnityEngine.Serialization;

public class TrinketPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private Yarn.Unity.DialogueReference pickupDialogue;
    [SerializeField] private GameObject makeInteractable;
    [SerializeField] private SpriteRenderer glowSprite;
    [SerializeField] private Trinket trinket;

    public static bool pickedUp = false;
    public virtual void Interact() {
        StartCoroutine(InteractCoroutine());
    }

    private IEnumerator InteractCoroutine() {
        yield return DialogueManager.Instance.StartDialogue(pickupDialogue);
        PlayerDataManager.Instance.AddTrinket(trinket);
        pickedUp = true;
        glowSprite.enabled = false;
        Destroy(this.gameObject);
    }
}
