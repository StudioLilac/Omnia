using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Omnia.Editor
{
    public sealed class PosterizationShaderGUI : ShaderGUI
    {
        private const int MaxPaletteColors = 64;
        private const string PaletteTag = "PosterizationPaletteHexCodes";
        private static readonly Regex HexCode = new(@"^#?([0-9a-fA-F]{6})$", RegexOptions.Compiled);
        private string paletteText;
        private Material inspectedMaterial;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var material = materialEditor.target as Material;
            if (material == null)
                return;

            if (inspectedMaterial != material)
            {
                inspectedMaterial = material;
                paletteText = material.GetTag(PaletteTag, false, string.Empty);
            }

            EditorGUILayout.LabelField("Palette", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Paste one RGB hex code per line (with or without #). Colors are replaced with the closest palette color. Up to 64 colors.",
                MessageType.Info);
            paletteText = EditorGUILayout.TextArea(paletteText ?? string.Empty, GUILayout.MinHeight(110));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Palette"))
                    ApplyPalette(material, paletteText);

                EditorGUILayout.LabelField($"{Mathf.RoundToInt(material.GetFloat("_PaletteCount"))} active color(s)", GUILayout.Width(130));
            }
        }

        public static void ApplyPalette(Material material, string input)
        {
            var colors = new List<Color>();
            var invalidEntries = new List<string>();
            var entries = Regex.Split(input ?? string.Empty, @"[\s,;]+", RegexOptions.CultureInvariant);

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry))
                    continue;

                var match = HexCode.Match(entry);
                if (!match.Success)
                {
                    invalidEntries.Add(entry);
                    continue;
                }

                var hex = match.Groups[1].Value;
                colors.Add(new Color(
                    ParseChannel(hex, 0),
                    ParseChannel(hex, 2),
                    ParseChannel(hex, 4),
                    1f).linear);
            }

            if (invalidEntries.Count > 0)
            {
                EditorUtility.DisplayDialog("Invalid palette entries", $"Use six-digit RGB hex codes. Invalid: {string.Join(", ", invalidEntries)}", "OK");
                return;
            }

            if (colors.Count == 0)
            {
                EditorUtility.DisplayDialog("Palette is empty", "Paste at least one six-digit RGB hex code.", "OK");
                return;
            }

            if (colors.Count > MaxPaletteColors)
            {
                EditorUtility.DisplayDialog("Palette is too large", $"A palette can contain at most {MaxPaletteColors} colors.", "OK");
                return;
            }

            Undo.RecordObject(material, "Apply posterization palette");
            var paletteTexture = LoadOrCreatePaletteTexture();
            var palettePixels = new Color[MaxPaletteColors];
            for (var index = 0; index < colors.Count; index++)
                palettePixels[index] = colors[index];

            paletteTexture.SetPixels(palettePixels);
            paletteTexture.Apply(false, false);
            EditorUtility.SetDirty(paletteTexture);
            material.SetTexture("_PaletteTexture", paletteTexture);
            material.SetFloat("_PaletteCount", colors.Count);
            material.SetOverrideTag(PaletteTag, input.Trim());
            EditorUtility.SetDirty(material);
        }

        private static float ParseChannel(string hex, int startIndex) =>
            int.Parse(hex.Substring(startIndex, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f;

        private static Texture2D LoadOrCreatePaletteTexture()
        {
            const string paletteTexturePath = "Assets/Rendering/PosterizationPalette.asset";
            var paletteTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(paletteTexturePath);
            if (paletteTexture != null)
                return paletteTexture;

            paletteTexture = new Texture2D(MaxPaletteColors, 1, TextureFormat.RGBAFloat, false, true)
            {
                name = "PosterizationPalette",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            AssetDatabase.CreateAsset(paletteTexture, paletteTexturePath);
            return paletteTexture;
        }
    }
}
