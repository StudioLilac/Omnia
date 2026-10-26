using UnityEngine;

namespace Omnia.Fire {
    [CreateAssetMenu(menuName = "Omnia/Flame Palette")]
    public sealed class FlamePalette : ScriptableObject {
        public Color32[] colors = {
            new Color32(0, 0, 0, 0), new Color32(106, 12, 10, 255),
            new Color32(213, 30, 12, 255), new Color32(255, 101, 16, 255),
            new Color32(255, 214, 56, 255), new Color32(255, 255, 222, 255)
        };

        public Color32 Evaluate(float heat) {
            if (colors == null || colors.Length == 0) return new Color32(255, 130, 20, 255);
            int index = colors.Length == 1 ? 0 : Mathf.Clamp(Mathf.RoundToInt(heat * (colors.Length - 1)), 1, colors.Length - 1);
            Color32 color = colors[index];
            color.a = 255;
            return color;
        }
    }
}
