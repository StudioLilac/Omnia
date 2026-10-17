using System.Collections.Generic;
using UnityEngine;
using Yarn;
using Yarn.Unity;
using TMPro;

/// <summary>Project-owned equivalent of Yarn's LinePresenter using unscaled time.</summary>
public class UnscaledLinePresenter : DialoguePresenterBase {
    public CanvasGroup canvasGroup;
    public TMP_Text lineText;
    public bool showCharacterNameInLine = true;
    public TMP_Text characterNameText;
    public GameObject characterNameContainer;
    public bool useFadeEffect = true;
    public float fadeUpDuration = .25f;
    public float fadeDownDuration = .1f;
    public bool autoAdvance;
    public float autoAdvanceDelay = 1f;
    [SerializeField] private List<ActionMarkupHandler> eventHandlers = new();
    [SerializeField] private UnscaledLetterTypewriter typewriter;

    private List<IActionMarkupHandler> Handlers {
        get {
            var handlers = new List<IActionMarkupHandler> { new PauseEventProcessor() };
            handlers.AddRange(eventHandlers);
            return handlers;
        }
    }

    private void Awake() {
        characterNameContainer ??= characterNameText?.gameObject;
        typewriter ??= GetComponent<UnscaledLetterTypewriter>();
        typewriter.ActionMarkupHandlers.AddRange(Handlers);
        typewriter.TextElement = lineText;
        Typewriter = typewriter;
    }

    public override YarnTask OnDialogueStartedAsync() {
        if (canvasGroup != null) canvasGroup.alpha = 0;
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync() {
        if (canvasGroup != null) canvasGroup.alpha = 0;
        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token) {
        if (lineText == null) return;
        var text = characterNameText == null && showCharacterNameInLine ? line.Text : line.TextWithoutCharacterName;
        if (characterNameText != null) {
            var hasName = !string.IsNullOrWhiteSpace(line.CharacterName);
            characterNameContainer?.SetActive(hasName);
            if (hasName) characterNameText.text = line.CharacterName;
        }

        typewriter.PrepareForContent(text);
        if (canvasGroup != null) {
            if (useFadeEffect) await Fade(canvasGroup, 0, 1, fadeUpDuration, token.HurryUpToken);
            else canvasGroup.alpha = 1;
        }
        await typewriter.RunTypewriter(text, token.HurryUpToken).SuppressCancellationThrow();
        if (autoAdvance) await YarnTask.Delay((int)(autoAdvanceDelay * 1000), token.NextContentToken).SuppressCancellationThrow();
        else await YarnTask.WaitUntilCanceled(token.NextContentToken).SuppressCancellationThrow();
        typewriter.ContentWillDismiss();
        if (canvasGroup != null) {
            if (useFadeEffect) await Fade(canvasGroup, 1, 0, fadeDownDuration, token.HurryUpToken).SuppressCancellationThrow();
            else canvasGroup.alpha = 0;
        }
        typewriter.ContentDidDismiss();
    }

    private static async YarnTask Fade(CanvasGroup group, float from, float to, float duration, System.Threading.CancellationToken token) {
        if (duration <= 0) { group.alpha = to; return; }
        var elapsed = 0f;
        while (!token.IsCancellationRequested && elapsed < duration) {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            await YarnTask.Yield();
        }
        group.alpha = to;
    }
}
