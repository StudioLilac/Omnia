using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

namespace Omnia.Rendering {
    public sealed class SpriteOutlineManager : PersistentSingleton<SpriteOutlineManager> {
        private static readonly Color DefaultColor = Color.black;

        private readonly Dictionary<GameObject, (Stack<Color> Colors, List<PixelOutliner> Outliners)> entities = new();
        private readonly List<SpriteRenderer> rendererBuffer = new();

        public void PushColor(Component target, Color? outlineColor = null) {
            if (target == null) throw new ArgumentNullException(nameof(target));
            GameObject gameObject = target.gameObject;

            if (!entities.TryGetValue(gameObject, out var entity)) {
                entity = (new Stack<Color>(), new List<PixelOutliner>());
                entities.Add(gameObject, entity);
            }

            if (outlineColor.HasValue) entity.Colors.Push(outlineColor.Value);
            AttachOutliners(gameObject, entity.Outliners);
        }

        public void PopColor(Component target) {
            if (target == null || !entities.TryGetValue(target.gameObject, out var entity)) return;
            if (entity.Colors.Count > 0) entity.Colors.Pop();
        }

        public void Update() {
            foreach (var (owner, entity) in entities) {
                // This line is kinda goon but basically its for enemies tagged by the harpoon. 
                // Enemies tagged by the harpoon are "pulled", but I don't think it's needed anymore for now. Imma just leave this line here
                // but that tagged code should be refactored in another PR.
                entity.Outliners.RemoveAll(outliner => outliner == null || !outliner.transform.IsChildOf(owner.transform));
                // rechecks for newly instantiated children
                AttachOutliners(owner, entity.Outliners);
                var rootColors = entities[GetRootOwner(owner.transform)].Colors;
                Color color = rootColors.Count > 0 ? rootColors.Peek() : DefaultColor;
                entity.Outliners
                    .Where(outliner => outliner != null)
                    .ToList()
                    .ForEach(outliner => outliner.OutlineColor = color);
            }
        }

        internal void OnEntityDestroyed(Component target) {
            if (ReferenceEquals(target, null)) return;
            GameObject gameObject = target.gameObject;
            if (!entities.TryGetValue(gameObject, out var entity)) return;
            entities.Remove(gameObject);

            entity.Outliners
                .Where(outliner => outliner != null && !entities.Values.Any(other => other.Outliners.Contains(outliner)))
                .ToList()
                .ForEach(outliner => outliner.enabled = false);
        }

        // prefabs that have multiple sprites will need multiple outliners
        private void AttachOutliners(GameObject target, List<PixelOutliner> outliners) {
            target.GetComponentsInChildren(true, rendererBuffer);
            foreach (var renderer in rendererBuffer) {
                if (PixelOutliner.IsOutlineRenderer(renderer)) continue;

                var outliner = renderer.TryGetComponent(out PixelOutliner existing)
                    ? existing
                    : PixelOutliner.Attach(renderer);
                outliner.enabled = true;
                if (!outliners.Contains(outliner)) outliners.Add(outliner);
            }
        }

        // topmost subscribed ancestor (or self); nested subscribers inherit its colour
        private GameObject GetRootOwner(Transform transform) {
            return transform.GetComponentsInParent<Transform>(true)
                .Select(t => t.gameObject)
                .LastOrDefault(entities.ContainsKey);
        }
    }
}
