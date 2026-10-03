using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class Interactable : MonoBehaviour
{
    private bool interactable = true;
    private bool inRange = false;
    private IInteractable mainScript;
    [SerializeField] private SpriteRenderer buttonIcon;

    private Coroutine fadeCoroutine;

    [SerializeField] private InputActionReference interactAction;

    void Start() {
        buttonIcon.color = new Color(1f, 1f, 1f, 0f); // Set fully transparent
        mainScript = GetComponentInParent<IInteractable>();
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        Rigidbody2D rb = collision.attachedRigidbody;
        if (rb != null && rb.gameObject.CompareTag("Player")) {
            inRange = true;
            if (interactable) StartFade(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        Rigidbody2D rb = collision.attachedRigidbody;
        if (rb != null && rb.gameObject.CompareTag("Player")) {
            inRange = false;
            StartFade(false);
        }
    }

    public bool GetPlayerRange() {
        return inRange;
    }

    void Update() {
        if (mainScript == null || !interactable) return;
        if (inRange && interactAction.action.WasPressedThisFrame()) {
            inRange = false;
            StartFade(false);
            mainScript.Interact();
        }
    }

    void OnDestroy() {
        mainScript = null;
        buttonIcon = null;
    }

    public void SetEnable(bool enable) {
        interactable = enable;
        if (!enable) StartFade(false);
    }

    private void StartFade(bool fadeIn) {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (!gameObject.activeInHierarchy) {
            Debug.LogWarning("Interactable has been disabled");
            return;
        }
        fadeCoroutine = StartCoroutine(FadeButton(fadeIn));
    }

    private IEnumerator FadeButton(bool fadeIn) {
        float targetAlpha = fadeIn ? 1f : 0f;
        float startAlpha = buttonIcon.color.a;
        float duration = 0.3f; // Adjust speed as needed
        float elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            buttonIcon.color = new Color(1f, 1f, 1f, newAlpha);
            yield return null;
        }

        buttonIcon.color = new Color(1f, 1f, 1f, targetAlpha);
    }
}
