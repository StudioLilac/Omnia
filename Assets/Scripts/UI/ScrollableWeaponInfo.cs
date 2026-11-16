using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class ScrollableWeaponInfo : MonoBehaviour {
        [SerializeField, TextArea] private string harpoonDescription;
        [SerializeField, TextArea] private string harpoonSkill;
        [SerializeField, TextArea] private string harpoonIntro;

        [SerializeField, TextArea] private string flamethrowerDescription;
        [SerializeField, TextArea] private string flamethrowerSkill;
        [SerializeField, TextArea] private string flamethrowerIntro;

        [SerializeField] private TextMeshProUGUI description;
        [SerializeField] private TextMeshProUGUI skill;
        [SerializeField] private TextMeshProUGUI intro;

        [SerializeField] private Image weaponImage;

        [SerializeField] private Sprite harpoonGun;
        [SerializeField] private Sprite flamethrower;

        private bool viewingHarpoon = true;

        private void Start() {
            UpdateWeaponInfo();
        }

        public void SeeNext() {
            viewingHarpoon = !viewingHarpoon;
            UpdateWeaponInfo();
        }

        private void UpdateWeaponInfo() {
            if (viewingHarpoon) {
                description.text = harpoonDescription;
                skill.text = harpoonSkill;
                intro.text = harpoonIntro;
                weaponImage.sprite = harpoonGun;
            } else {
                description.text = flamethrowerDescription;
                skill.text = flamethrowerSkill;
                intro.text = flamethrowerIntro;
                weaponImage.sprite = flamethrower;
            }
        }
    }
}
