#nullable enable

using UnityEngine;

namespace StudioLilac.StatusEffects {
    [CreateAssetMenu(fileName = "StatusEffectConfig", menuName = "StudioLilac.StatusEffects/StatusEffectConfig")]
    public class StatusEffectConfig : ScriptableObject {
        // TODO: i18n

        [SerializeField] private string statusEffectName = null!;
        [SerializeField] private Sprite statusEffectIcon = null!;

        public string Name => statusEffectName;
        public Sprite Icon => statusEffectIcon;
    }
}
