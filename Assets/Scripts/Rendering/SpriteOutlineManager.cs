using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Omnia.Rendering {
    public readonly struct SpriteOutlineUpdated : IEvent {
        public Transform Root { get; }

        public SpriteOutlineUpdated(Transform root) => Root = root;
    }

    public sealed class SpriteOutlineManager : MonoBehaviour {
        private static readonly Color DefaultColor = Color.black;

        private readonly Dictionary<GameObject, Stack<Color>> entities = new();
        private readonly List<SpriteRenderer> rendererBuffer = new();

        private void Awake() {
            this.Subscribe<SpriteOutlineUpdated>(OnHierarchyChanged);
        }

        private void OnEnable() {
            entities.Keys.Where(owner => owner != null).ToList().ForEach(owner => RefreshHierarchy(owner.transform));
        }

        private void OnDisable() {
            entities.Keys
                .Where(owner => owner != null)
                .SelectMany(owner => owner.GetComponentsInChildren<SpriteRenderer>(true))
                .Select(renderer => renderer.GetComponent<PixelOutliner>())
                .Where(outliner => outliner != null && outliner.Manager == this)
                .ToList()
                .ForEach(outliner => {
                    outliner.Manager = null;
                    outliner.enabled = false;
                });
        }

        private void OnDestroy() {
            OnDisable();
            entities.Clear();
        }

        public void PushColor(Component target, Color? outlineColor = null) {
            if (target == null) throw new ArgumentNullException(nameof(target));
            GameObject gameObject = target.gameObject;

            if (!entities.TryGetValue(gameObject, out var colors)) {
                colors = new Stack<Color>();
                entities.Add(gameObject, colors);
            }

            if (outlineColor.HasValue) colors.Push(outlineColor.Value);
            RefreshHierarchy(gameObject.transform);
        }

        public void PopColor(Component target) {
            if (target == null || !entities.TryGetValue(target.gameObject, out var colors) || colors.Count == 0) return;
            colors.Pop();
            RefreshHierarchy(target.transform);
        }

        internal void OnEntityDestroyed(Component target) {
            if (ReferenceEquals(target, null)) return;
            GameObject gameObject = target.gameObject;
            if (entities.Remove(gameObject)) RefreshHierarchy(gameObject.transform);
        }

        private void OnHierarchyChanged(SpriteOutlineUpdated change) {
            if (change.Root != null) RefreshHierarchy(change.Root);
        }

        private void RefreshHierarchy(Transform root) {
            if (!isActiveAndEnabled) return;
            root.GetComponentsInChildren(true, rendererBuffer);
            foreach (var renderer in rendererBuffer) {
                if (renderer == null) continue;
                bool registered = false;
                if (!TryResolveColor(renderer.transform, null, ref registered, out Color color)) color = DefaultColor;
                renderer.TryGetComponent(out PixelOutliner outliner);
                if (!registered) {
                    // Preserve independently attached outliners when a subtree leaves an entity.
                    if (outliner != null && ReferenceEquals(outliner.Manager, this)) {
                        outliner.Manager = null;
                        outliner.enabled = false;
                    }
                    continue;
                }

                if (outliner == null) outliner = PixelOutliner.Attach(renderer);
                outliner.Manager = this;
                outliner.OutlineColor = color;
                outliner.enabled = true;
            }
            rendererBuffer.Clear();
        }

        // Walks ancestors from `from` up to (excluding) `stop`; nearest pushed colour wins.
        private bool TryResolveColor(Transform from, Transform stop, ref bool registered, out Color color) {
            for (Transform t = from; !ReferenceEquals(t, stop); t = t.parent) {
                if (!entities.TryGetValue(t.gameObject, out var colors)) continue;
                registered = true;
                if (colors.Count == 0) continue;
                color = colors.Peek();
                return true;
            }
            color = DefaultColor;
            return false;
        }
    }
}
