using System;
using System.Linq;
using System.Reflection;
using Omnia.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// Run with: unity command run_script --file Tests/PixelOutlinerVerification.cs --entry PixelOutlinerVerification.Run
public static class PixelOutlinerVerification {
    public static string Run() {
        var scene = EditorSceneManager.NewPreviewScene();
        var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point };
        var pixels = Enumerable.Repeat(Color.white, 256).ToArray();
        // Opaque neighboring atlas entries must never bleed into the outline.
        var pattern = new[] { "1010", "1110", "0011", "1001" };
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++) pixels[(y + 6) * 16 + x + 6] = pattern[y][x] == '1' ? Color.white : Color.clear;
        texture.SetPixels(pixels);
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(6, 6, 4, 4), new Vector2(0.25f, 0.75f), 2f, 0, SpriteMeshType.FullRect);
        var second = Sprite.Create(texture, new Rect(6, 6, 2, 3), new Vector2(0.5f, 0.5f), 4f, 0, SpriteMeshType.FullRect);
        var target = new RenderTexture(16, 16, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        var readback = new Texture2D(16, 16, TextureFormat.RGBA32, false, true);
        var previousTarget = RenderTexture.active;
        Mesh mesh = null;
        MeshRenderer outline = null;
        int checks = 0;
        void Check(bool condition, string message) {
            checks++;
            if (!condition) throw new Exception(message);
        }
        void Invoke(PixelOutliner outliner, string name) => typeof(PixelOutliner)
            .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(outliner, null);
        try {
            var root = new GameObject("Pixel outline verification");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            var source = root.AddComponent<SpriteRenderer>();
            source.sprite = sprite;
            source.sortingOrder = 7;
            source.color = new Color(1, 1, 1, 0.8f);
            var outliner = PixelOutliner.Attach(source, 0.5f, new Color(1, 1, 1, 0.75f));
            // MonoBehaviour lifecycle does not run automatically in an edit-mode preview scene.
            Invoke(outliner, "Awake");
            Invoke(outliner, "LateUpdate");
            outline = root.GetComponentInChildren<MeshRenderer>();
            mesh = outline.GetComponent<MeshFilter>().sharedMesh;
            Check(root.GetComponentsInChildren<Renderer>().Length == 2, "Expected one source and one outline renderer");
            Check(root.GetComponentsInChildren<SpriteRenderer>().Length == 1, "Outline introduced a SpriteRenderer");
            Check(mesh.vertexCount == 4 && mesh.triangles.Length == 6, "Expected a single quad");
            Check(outline.sortingOrder == 7, "Sorting order was not copied");
            Check(!ShaderUtil.ShaderHasError(outline.sharedMaterial.shader), "Outline shader has compilation errors");
            void CheckBounds(Sprite current) {
                var expected = current.bounds;
                expected.Expand(new Vector3(2f / current.pixelsPerUnit, 2f / current.pixelsPerUnit, 0));
                Check(Vector2.Distance(mesh.bounds.min, expected.min) < 1e-5f &&
                      Vector2.Distance(mesh.bounds.max, expected.max) < 1e-5f,
                    $"Quad must extend exactly one sprite pixel per edge: expected {expected}, got {mesh.bounds}");
            }
            CheckBounds(sprite);
            var properties = new MaterialPropertyBlock();
            outline.GetPropertyBlock(properties);
            Check(Mathf.Abs(properties.GetColor("_OutlineColor").a - 0.3f) < 1e-5f, "Outline alpha changed");

            bool Filled(int x, int y) => x >= 0 && y >= 0 && x < 4 && y < 4 && pattern[y][x] == '1';
            target.Create();
            for (int flip = 0; flip < 4; flip++) {
                source.flipX = (flip & 1) != 0;
                source.flipY = (flip & 2) != 0;
                Invoke(outliner, "LateUpdate");
                outline.GetPropertyBlock(properties);
                using (var commands = new CommandBuffer()) {
                    commands.SetRenderTarget(target);
                    commands.ClearRenderTarget(true, true, Color.clear);
                    commands.SetViewProjectionMatrices(Matrix4x4.identity,
                        Matrix4x4.Ortho(-4, 4, -4, 4, -10, 10));
                    commands.DrawMesh(mesh, outline.transform.localToWorldMatrix, outline.sharedMaterial, 0, 0, properties);
                    Graphics.ExecuteCommandBuffer(commands);
                }
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, 16, 16), 0, 0);
                readback.Apply();
                for (int y = 0; y < 16; y++) {
                    for (int x = 0; x < 16; x++) {
                        int sx = Mathf.FloorToInt((x - 7.5f) * (source.flipX ? -1 : 1) + 1);
                        int sy = Mathf.FloorToInt((y - 7.5f) * (source.flipY ? -1 : 1) + 3);
                        bool edge = !Filled(sx, sy) && (Filled(sx - 1, sy) || Filled(sx + 1, sy) || Filled(sx, sy - 1) || Filled(sx, sy + 1));
                        float expected = edge ? 0.3f : 0;
                        Check(Mathf.Abs(readback.GetPixel(x, y).r - expected) < 0.01f,
                            $"Pixel mismatch for flip {flip} at ({x}, {y}); expected {expected}, got {readback.GetPixel(x, y)}");
                    }
                }
            }

            source.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
            Invoke(outliner, "LateUpdate");
            Check(outline.GetSpriteMaskInteraction() == source.maskInteraction, "Sprite mask interaction was not copied");
            source.sprite = second;
            Invoke(outliner, "LateUpdate");
            Check(outline.GetComponent<MeshFilter>().sharedMesh == mesh, "Animation allocated another mesh");
            CheckBounds(second);
            source.enabled = false;
            Invoke(outliner, "LateUpdate");
            Check(!outline.enabled, "Disabled source retained its outline");
            source.enabled = true;
            source.sprite = null;
            Invoke(outliner, "LateUpdate");
            Check(!outline.enabled, "Missing sprite retained its outline");
            source.sprite = sprite;
            outliner.OutlineStrength = 0;
            Invoke(outliner, "LateUpdate");
            Check(!outline.enabled, "Zero strength retained its outline");
            outliner.OutlineStrength = 1;
            source.drawMode = SpriteDrawMode.Sliced;
            Invoke(outliner, "LateUpdate");
            Check(!outline.enabled, "Sliced source retained its outline");
            source.drawMode = SpriteDrawMode.Simple;
            Invoke(outliner, "LateUpdate");
            Check(outline.enabled, "Outline did not recover after becoming visible");
            Invoke(outliner, "OnDisable");
            Check(!outline.enabled, "Disabling the component retained its outline");
            var manager = root.AddComponent<SpriteOutlineManager>();
            manager.PushColor(source);
            Check(root.GetComponentsInChildren<PixelOutliner>().Length == 1, "Manager recursively outlined its generated child");
            return $"PASS: {checks} checks covering one renderer, padded bounds, GPU pixels for all flips, atlas isolation, alpha, animation reuse, sorting, masking state and visibility.";
        } finally {
            RenderTexture.active = previousTarget;
            // Release generated objects before the edit-mode scene closes (runtime uses deferred Destroy).
            if (outline != null) Object.DestroyImmediate(outline.gameObject);
            if (mesh != null) Object.DestroyImmediate(mesh);
            EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(readback);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }
    }
}
