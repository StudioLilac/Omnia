using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Omnia.Rendering {
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PixelOutliner : MonoBehaviour {
        private static readonly int spriteUVRectId = Shader.PropertyToID("_SpriteUVRect");
        private static readonly int pixelUVId = Shader.PropertyToID("_PixelUV");
        private static readonly int mainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int outlineColorId = Shader.PropertyToID("_OutlineColor");

        private static readonly int[] renderBox = { 0, 2, 1, 0, 3, 2 };
        private static readonly ConditionalWeakTable<Sprite, Geometry> geometryCache = new();
        private static MaterialPropertyBlock block;

        [SerializeField] private Material outlineMaterial;
        [SerializeField, Range(0f, 1f)] private float outlineStrength = 0.65f;
        [SerializeField] private Color outlineColor = new(1f, 0f, 0f, 1f);

        private SpriteRenderer source;
        private MeshRenderer outline;
        private Mesh mesh;
        private Geometry geometry;
        private Sprite lastSprite;

        internal SpriteOutlineManager Manager { get; set; }

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
            if (outline != null) Destroy(outline.gameObject);
            if (mesh != null) Destroy(mesh);
        }

        private void LateUpdate() {
            Sprite sprite = source.sprite;
            if (!source.enabled || sprite == null || outlineStrength <= 0f || source.drawMode != SpriteDrawMode.Simple) {
                SetVisible(false);
                return;
            }

            if (outline == null) CreateRenderer();
            if (sprite != lastSprite) {
                lastSprite = sprite;
                geometry = geometryCache.GetValue(sprite, s => new Geometry(s));
                mesh.vertices = geometry.vertices;
                mesh.uv = geometry.uv;
                mesh.RecalculateBounds();
            }

            outline.transform.localScale = new Vector3(source.flipX ? -1f : 1f, source.flipY ? -1f : 1f, 1f);
            outline.gameObject.layer = gameObject.layer;
            outline.sortingLayerID = source.sortingLayerID;
            // The shader masks out pixels the source covers, so both can use the same sorting order.
            outline.sortingOrder = source.sortingOrder;
            outline.SetSpriteMaskInteraction(source.maskInteraction);
            outline.forceRenderingOff = source.forceRenderingOff;
            outline.enabled = true;

            Color color = outlineColor;
            color.a *= outlineStrength * source.color.a;
            block ??= new MaterialPropertyBlock();
            block.SetTexture(mainTexId, sprite.texture);
            block.SetColor(outlineColorId, color);
            block.SetVector(spriteUVRectId, geometry.uvRect);
            block.SetVector(pixelUVId, new Vector4(geometry.uvPerPixelX.x, geometry.uvPerPixelX.y,
                geometry.uvPerPixelY.x, geometry.uvPerPixelY.y));
            outline.SetPropertyBlock(block);
        }

        private void CreateRenderer() {
            var child = new GameObject("Pixel Outline") { layer = gameObject.layer };
            child.transform.SetParent(transform, false);
            mesh = new Mesh { name = "Pixel Outline", hideFlags = HideFlags.DontSave };
            mesh.MarkDynamic();
            mesh.vertices = new Vector3[4];
            mesh.triangles = renderBox;
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            outline = child.AddComponent<MeshRenderer>();
            outline.sharedMaterial = outlineMaterial;
            outline.shadowCastingMode = ShadowCastingMode.Off;
            outline.receiveShadows = false;
            outline.lightProbeUsage = LightProbeUsage.Off;
            outline.reflectionProbeUsage = ReflectionProbeUsage.Off;
            lastSprite = null;
        }

        private void SetVisible(bool visible) {
            if (outline != null && outline.enabled != visible) outline.enabled = visible;
        }

        private sealed class Geometry {
            public readonly Vector4 uvRect;
            public readonly Vector2 uvPerPixelX, uvPerPixelY;
            public readonly Vector3[] vertices;
            public readonly Vector2[] uv;

            public Geometry(Sprite sprite) {
                Vector2[] v = sprite.vertices, spriteUV = sprite.uv;
                uvRect = new Vector4(spriteUV.Min(p => p.x), spriteUV.Min(p => p.y), spriteUV.Max(p => p.x), spriteUV.Max(p => p.y));
                var basis = Enumerable.Range(1, v.Length - 1)
                    .SelectMany(j => Enumerable.Range(j + 1, v.Length - j - 1),
                        (j, k) => (a: v[j] - v[0], b: v[k] - v[0], uvA: spriteUV[j] - spriteUV[0], uvB: spriteUV[k] - spriteUV[0]))
                    .Select(t => (t.a, t.b, t.uvA, t.uvB, det: t.a.x * t.b.y - t.a.y * t.b.x))
                    .FirstOrDefault(t => Mathf.Abs(t.det) > 1e-6f);

                if (basis.det == 0f) {
                    uvPerPixelX = new Vector2(1f / sprite.texture.width, 0f);
                    uvPerPixelY = new Vector2(0f, 1f / sprite.texture.height);
                } else {
                    float scale = 1f / (basis.det * sprite.pixelsPerUnit);
                    uvPerPixelX = (basis.uvA * basis.b.y - basis.uvB * basis.a.y) * scale;
                    uvPerPixelY = (basis.uvB * basis.a.x - basis.uvA * basis.b.x) * scale;
                }

                float pixel = 1f / sprite.pixelsPerUnit;
                float left = v.Min(p => p.x) - pixel, right = v.Max(p => p.x) + pixel;
                float bottom = v.Min(p => p.y) - pixel, top = v.Max(p => p.y) + pixel;
                vertices = new[] {
                    new Vector3(left, bottom), new Vector3(right, bottom),
                    new Vector3(right, top), new Vector3(left, top)
                };
                // Extrapolate the sprite's UV basis to the padded quad, including rotated atlas entries.
                uv = vertices.Select(p => {
                    Vector2 delta = ((Vector2)p - v[0]) * sprite.pixelsPerUnit;
                    return spriteUV[0] + uvPerPixelX * delta.x + uvPerPixelY * delta.y;
                }).ToArray();
            }
        }
    }
}
