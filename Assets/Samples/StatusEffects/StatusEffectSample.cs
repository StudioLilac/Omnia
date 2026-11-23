#nullable enable

using Enemies;
using Players;
using StudioLilac.StatusEffects;
using StatusEffects;
using UnityEngine;

namespace Samples.StatusEffects {
    /**
     * Sample scene driver for status effects (issue #292): applies Strength to
     * the player and Weakness to every enemy in the scene when play mode
     * starts. The icons show up in the HUD (player) and under each enemy's
     * health bar; the multiplier chains apply to the player's shots and to
     * enemy attacks respectively. Zero config beyond the two effect configs.
     */
    public class StatusEffectSample : MonoBehaviour {
        [SerializeField] private StatusEffectConfig strengthConfig = null!;
        [SerializeField] private StatusEffectConfig weaknessConfig = null!;
        [SerializeField] private int playerStacks = 3;
        [SerializeField] private int enemyStacks = 2;
        [SerializeField] private float durationS = 30f;

        private void Start() {
            var player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Player>();
            if (player != null) {
                player.ApplyStatusEffect(new StrengthStatusEffect(strengthConfig, playerStacks, durationS));
            }

            foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                enemy.ApplyStatusEffect(new WeaknessStatusEffect(weaknessConfig, enemyStacks, durationS));
            }
        }
    }
}
