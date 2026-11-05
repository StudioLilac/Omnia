using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace StudioLilac.Dialogue {
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public class Actor : MonoBehaviour {
        [SerializeField] private Image portrait;

        private RectTransform anchor;
        private CanvasGroup canvasGroup;

        private void Awake() {
            anchor = (RectTransform)transform;
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Init(string character, Vector2 position, Sprite sprite) {
            gameObject.name = character;
            anchor.anchoredPosition = position;
            canvasGroup.alpha = 0f;
            SetSprite(sprite);
        }

        public void SetSprite(Sprite sprite) {
            portrait.sprite = sprite;
        }

        public async YarnTask FadeTo(float target, float duration) {
            var from = canvasGroup.alpha;

            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime) {
                canvasGroup.alpha = Mathf.Lerp(from, target, elapsed / duration);
                await YarnTask.Yield();
            }

            canvasGroup.alpha = target;
        }

        public async YarnTask SlideTo(Vector2 target, float duration) {
            var from = anchor.anchoredPosition;

            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime) {
                var eased = 1f - Mathf.Pow(1f - elapsed / duration, 3f);
                anchor.anchoredPosition = Vector2.LerpUnclamped(from, target, eased);
                await YarnTask.Yield();
            }

            anchor.anchoredPosition = target;
        }
    }
}
