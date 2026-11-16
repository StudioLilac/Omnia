using System.Collections;
using System.Collections.Generic;
using Players;
using UnityEngine;

public class FlamethrowerPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private Yarn.Unity.DialogueReference pickupDialogue;
    [SerializeField] private GameObject makeInteractable;
    [SerializeField] private SpriteRenderer flamethrowerSprite;
    [SerializeField] private Player player;

    public static bool pickedUp = false;
    public virtual void Interact() {
        StartCoroutine(InteractCoroutine());
    }

    private IEnumerator InteractCoroutine() {
        yield return DialogueManager.Instance.StartDialogue(pickupDialogue);
        pickedUp = true;
        flamethrowerSprite.enabled = false;
        player.hasFlamethrower = true;
        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }
    }
}
