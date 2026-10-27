using UnityEngine;

namespace Omnia.Fire {
    [CreateAssetMenu(menuName = "Omnia/Flame Palette")]
    public sealed class FlamePalette : ScriptableObject {
        // Four broad, saturated bands give the flame a cartoon silhouette.
        public Color32[] colors = {
            new Color32(0, 0, 0, 0), new Color32(222, 46, 35, 255),
            new Color32(255, 116, 30, 255), new Color32(255, 204, 40, 255),
            new Color32(255, 244, 176, 255)
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
