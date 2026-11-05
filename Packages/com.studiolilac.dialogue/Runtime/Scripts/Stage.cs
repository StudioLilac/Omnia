#nullable enable

using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using UnityEngine;
using Yarn.Unity;

namespace StudioLilac.Dialogue {
    public class Stage : DialoguePresenterBase {
        [SerializeField] private Dictionary<string, ActorConfig> actorConfig = new();
        [SerializeField] private Dictionary<string, RectTransform> locations = new();
        [SerializeField] private AudioConfig? audioConfig;
        [SerializeField] private Actor? actorPrefab;
        [SerializeField] private float transitionDuration = 0.25f;

        private readonly Dictionary<string, Actor> actors = new();

        private static Stage? stage;

        private void OnEnable() {
            if (stage is not null && stage != this) {
                Debug.LogError("stage: multiple stages are enabled");
            }

            stage = this;
        }

        private void OnDisable() {
            if (stage != this) {
                return;
            }

            stage = null;
        }

        [YarnCommand("enter")]
        public static async YarnTask Enter(string character, string location, string expression) {
            if (stage is null) {
                Debug.LogWarning("stage: no stage is enabled");
                return;
            }

            await stage.EnterActor(character, location, expression);
        }

        private async YarnTask EnterActor(string character, string location, string expression) {
            if (!actorConfig.TryGetValue(character, out var expressions)) {
                Debug.LogWarning($"stage: unknown character: {character}");
                return;
            }

            if (!locations.TryGetValue(location, out var anchor)) {
                Debug.LogWarning($"stage: unknown location: {location}");
                return;
            }

            if (!expressions.TryGetValue(expression, out var sprite)) {
                Debug.LogWarning($"stage: unknown expression: {expression}");
                return;
            }

            if (!actors.TryGetValue(character, out var actor)) {
                if (actorPrefab is null) {
                    Debug.LogWarning("stage: no actor prefab is assigned");
                    return;
                }

                actor = Instantiate(actorPrefab, transform, false);
                actor.Init(character, anchor.anchoredPosition, sprite);
                actors.Add(character, actor);
                await actor.FadeTo(1f, transitionDuration);
                return;
            }

            actor.SetSprite(sprite);
            await actor.SlideTo(anchor.anchoredPosition, transitionDuration);
        }

        [YarnCommand("move")]
        public static async YarnTask Move(string character, string location) {
            if (stage is null) {
                Debug.LogWarning("stage: no stage is enabled");
                return;
            }

            await stage.MoveActor(character, location);
        }

        private async YarnTask MoveActor(string character, string location) {
            if (!locations.TryGetValue(location, out var anchor)) {
                Debug.LogWarning($"stage: unknown location: {location}");
                return;
            }

            if (!actors.TryGetValue(character, out var actor)) {
                return;
            }

            await actor.SlideTo(anchor.anchoredPosition, transitionDuration);
        }

        [YarnCommand("exit")]
        public static async YarnTask Exit(string character) {
            if (stage is null) {
                Debug.LogWarning("stage: no stage is enabled");
                return;
            }

            await stage.ExitActor(character);
        }

        private async YarnTask ExitActor(string character) {
            if (!actors.Remove(character, out var actor)) {
                return;
            }

            await actor.FadeTo(0f, transitionDuration);
            Destroy(actor.gameObject);
        }

        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token) {
            foreach (var meta in line.Metadata) {
                if (TryParse(meta, "expr", out var expression)) {
                    if (line.CharacterName is null) {
                        continue;
                    }

                    if (!actorConfig.TryGetValue(line.CharacterName, out var ac)) {
                        Debug.LogWarning($"stage: unknown character: {line.CharacterName}");
                        continue;
                    }

                    if (!ac.TryGetValue(expression, out var sprite)) {
                        Debug.LogWarning($"stage: unknown expression: {expression}");
                        continue;
                    }

                    if (!actors.TryGetValue(line.CharacterName, out var actor)) {
                        continue;
                    }

                    actor.SetSprite(sprite);
                } else if (TryParse(meta, "sfx", out var sfx)) {
                    if (audioConfig is null) {
                        Debug.LogWarning("stage: no audio config is assigned");
                        continue;
                    }

                    if (!audioConfig.TryGetValue(sfx, out var effect)) {
                        Debug.LogWarning($"stage: unknown sound effect: {sfx}");
                        continue;
                    }

                    RuntimeManager.PlayOneShot(effect);
                }
            }

            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;

        public override async YarnTask OnDialogueCompleteAsync() {
            var instances = new List<Actor>(actors.Values);
            actors.Clear();
            await YarnTask.WhenAll(instances.Select(actor => actor.FadeTo(0f, transitionDuration)));
            foreach (var actor in instances) {
                Destroy(actor.gameObject);
            }
        }

        private static bool TryParse(string meta, string key, out string value) {
            var prefix = key + ":";

            if (!meta.StartsWith(prefix)) {
                value = "";
                return false;
            }

            value = meta[prefix.Length..];
            return true;
        }
    }
}
