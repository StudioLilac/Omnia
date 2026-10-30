using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Omnia.Rendering {
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PixelOutliner : MonoBehaviour {
        private static readonly int SpriteUVRectId = Shader.PropertyToID("_SpriteUVRect");
        private static readonly int PixelUVId = Shader.PropertyToID("_PixelUV");
        private static readonly int DirectionId = Shader.PropertyToID("_Direction");

        private static readonly Vector2[] Directions = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };
        private static readonly ConditionalWeakTable<Sprite, Geometry> GeometryCache = new();
        private static MaterialPropertyBlock block;

        [SerializeField] private Material outlineMaterial;
        [SerializeField, Range(0f, 1f)] private float outlineStrength = 0.65f;
        [SerializeField] private Color outlineColor = new(1f, 0f, 0f, 1f);

        private SpriteRenderer source;
        private SpriteRenderer[] outlines;
        private Sprite lastSprite;
        private bool lastFlipX, lastFlipY;

        public float OutlineStrength {
            get => outlineStrength;
            set => outlineStrength = Mathf.Clamp01(value);
        }

        public Color OutlineColor {
            get => outlineColor;
            set => outlineColor = value;
        }

        public static PixelOutliner Attach(SpriteRenderer renderer, float strength = 1f, Color? color = null) {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            var outliner = renderer.TryGetComponent(out PixelOutliner existing)
                ? existing
                : renderer.gameObject.AddComponent<PixelOutliner>();
            outliner.OutlineStrength = strength;
            if (color.HasValue) outliner.outlineColor = color.Value;
            outliner.enabled = true;
            return outliner;
        }

        // Lets the manager skip the renderers this component creates when scanning an entity.
        internal static bool IsOutlineRenderer(SpriteRenderer renderer) {
            Transform parent = renderer.transform.parent;
            return parent != null && parent.TryGetComponent(out PixelOutliner outliner)
                && outliner.outlines != null && Array.IndexOf(outliner.outlines, renderer) >= 0;
        }

        private void Awake() {
            source = GetComponent<SpriteRenderer>();
            if (outlineMaterial == null)
                outlineMaterial = Resources.Load<Material>("PixelOutliner/SpritePixelSilhouette");
        }

        // Validated here (not Awake) so re-enabling via Attach can't bypass a missing material.
        private void OnEnable() {
            if (outlineMaterial != null) return;
            Debug.LogError("PixelOutliner material is missing from Resources/PixelOutliner.", this);
            enabled = false;
        }

        private void OnDisable() => SetVisible(false);

        private void OnDestroy() {
            if (outlines == null) return;
            foreach (var renderer in outlines.Where(r => r != null)) Destroy(renderer.gameObject);
        }

        private void LateUpdate() {
            Sprite sprite = source.sprite;
            if (!source.enabled || sprite == null || outlineStrength <= 0f || source.drawMode != SpriteDrawMode.Simple) {
                SetVisible(false);
                return;
            }

            outlines ??= Directions.Select(CreateRenderer).ToArray();
            Color color = outlineColor;
            color.a *= outlineStrength * source.color.a;
            foreach (var renderer in outlines) Sync(renderer, sprite, color);

            if (sprite == lastSprite && source.flipX == lastFlipX && source.flipY == lastFlipY) return;
            lastSprite = sprite;
            lastFlipX = source.flipX;
            lastFlipY = source.flipY;

            var geometry = GeometryCache.GetValue(sprite, s => new Geometry(s));
            Vector2 pixelX = geometry.uvPerPixelX * (lastFlipX ? -1f : 1f);
            Vector2 pixelY = geometry.uvPerPixelY * (lastFlipY ? -1f : 1f);
            block ??= new MaterialPropertyBlock();
            block.SetVector(SpriteUVRectId, geometry.uvRect);
            block.SetVector(PixelUVId, new Vector4(pixelX.x, pixelX.y, pixelY.x, pixelY.y));
            for (int i = 0; i < outlines.Length; i++) {
                outlines[i].transform.localPosition = Directions[i] / sprite.pixelsPerUnit;
                block.SetFloat(DirectionId, i);
                outlines[i].SetPropertyBlock(block);
            }
        }

        private SpriteRenderer CreateRenderer(Vector2 direction) {
            var child = new GameObject("Pixel Outline " + direction) { layer = gameObject.layer };
            child.transform.SetParent(transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = outlineMaterial;
            return renderer;
        }

        // Same sorting order as the source: the shader already masks out pixels the source covers.
        private void Sync(SpriteRenderer renderer, Sprite sprite, Color color) {
            if (renderer.sprite != sprite) renderer.sprite = sprite;
            renderer.flipX = source.flipX;
            renderer.flipY = source.flipY;
            renderer.color = color;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder;
            renderer.maskInteraction = source.maskInteraction;
            renderer.enabled = true;
        }

        private void SetVisible(bool visible) {
            if (outlines == null) return;
            foreach (var renderer in outlines)
                if (renderer != null && renderer.enabled != visible) renderer.enabled = visible;
        }

        private sealed class Geometry {
            public readonly Vector4 uvRect;
            public readonly Vector2 uvPerPixelX, uvPerPixelY;

            public Geometry(Sprite sprite) {
                Vector2[] v = sprite.vertices, uv = sprite.uv;
                uvRect = new Vector4(uv.Min(p => p.x), uv.Min(p => p.y), uv.Max(p => p.x), uv.Max(p => p.y));
                var basis = Enumerable.Range(1, v.Length - 1)
                    .SelectMany(j => Enumerable.Range(j + 1, v.Length - j - 1),
                        (j, k) => (a: v[j] - v[0], b: v[k] - v[0], uvA: uv[j] - uv[0], uvB: uv[k] - uv[0]))
                    .Select(t => (t.a, t.b, t.uvA, t.uvB, det: t.a.x * t.b.y - t.a.y * t.b.x))
                    .FirstOrDefault(t => Mathf.Abs(t.det) > 1e-6f);

                if (basis.det == 0f) {
                    uvPerPixelX = new Vector2(1f / sprite.texture.width, 0f);
                    uvPerPixelY = new Vector2(0f, 1f / sprite.texture.height);
                    return;
                }
                float scale = 1f / (basis.det * sprite.pixelsPerUnit);
                uvPerPixelX = (basis.uvA * basis.b.y - basis.uvB * basis.a.y) * scale;
                uvPerPixelY = (basis.uvB * basis.a.x - basis.uvA * basis.b.x) * scale;
            }
        }
    }
}
