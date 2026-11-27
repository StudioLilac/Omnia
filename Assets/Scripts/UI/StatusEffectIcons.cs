#nullable enable

using System.Collections.Generic;
using StudioLilac.StatusEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class StatusEffectIcons : MonoBehaviour {
        [SerializeField] private GameObject statusEffectIconPrefab = null!;

        public void Render(IEnumerable<IStatusEffect> effects) {
            foreach (Transform child in transform) {
                Destroy(child.gameObject);
            }

            foreach (var effect in effects) {
                var instance = Instantiate(statusEffectIconPrefab, transform);
                instance.GetComponent<Image>().sprite = effect.Icon;
                instance.GetComponentInChildren<TextMeshProUGUI>().text = effect.StackCount.ToString();
            }
        }
    }
}
