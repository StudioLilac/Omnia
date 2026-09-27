using Players.Buff;
using Players.Fragments;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

namespace Scenes.Descent {
    public class FragmentChoice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
        [SerializeField] private Image fragmentImage;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI description;
        private Fragment fragment;
        private Color originalColor;

        public void SetFragment(Fragment frag) {
            fragment = frag;
            title.text = fragment.fragmentName;
            description.text = fragment.description;
        }

        private void Start() {
            if (fragmentImage == null) {
                fragmentImage = GetComponent<Image>();
            }

            originalColor = fragmentImage.color;
        }

        public void OnPointerEnter(PointerEventData eventData) {
            if (fragmentImage != null) {
                fragmentImage.color = new Color(originalColor.r * 0.7f, originalColor.g * 0.7f, originalColor.b * 0.7f, originalColor.a); // Darken by 30%
            }
        }

        public void OnPointerExit(PointerEventData eventData) {
            if (fragmentImage != null) {
                fragmentImage.color = originalColor;
            }
        }

        public void OnPointerClick(PointerEventData eventData) {
            if (fragment != null) {
                BuffManager.Instance.ApplyBuff(fragment);
                LevelManager.Instance.NextLevel();
            }
        }
    }
}
