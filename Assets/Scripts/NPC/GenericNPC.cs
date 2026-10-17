using System.Collections;
using UnityEngine;
using Yarn.Unity;

public class GenericNPC : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueReference npcDialogue;
    public virtual void Interact() {
        StartCoroutine(InteractCoroutine());
    }

    private IEnumerator InteractCoroutine() {
        yield return DialogueManager.Instance.StartDialogue(npcDialogue);
    }

}
