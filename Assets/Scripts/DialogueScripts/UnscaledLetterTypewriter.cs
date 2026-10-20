using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Unity;

public class UnscaledLetterTypewriter : MonoBehaviour, IAsyncTypewriter {
    [Min(0)] public float charactersPerSecond = 60f;
    public TMP_Text TextElement { get; set; }
    public List<IActionMarkupHandler> ActionMarkupHandlers { get; } = new();

    public async YarnTask RunTypewriter(Yarn.Markup.MarkupParseResult line, CancellationToken cancellationToken) {
        if (TextElement == null) return;

        TextElement.maxVisibleCharacters = 0;
        TextElement.text = line.Text;
        foreach (var handler in ActionMarkupHandlers) handler.OnLineDisplayBegin(line, TextElement);

        var secondsPerCharacter = charactersPerSecond > 0 ? 1d / charactersPerSecond : 0d;
        var elapsed = secondsPerCharacter;
        var characterCount = TextElement.GetTextInfo(line.Text).characterCount;

        for (var i = 0; i < characterCount; i++) {
            while (!cancellationToken.IsCancellationRequested && elapsed < secondsPerCharacter) {
                var before = Time.unscaledTimeAsDouble;
                await YarnTask.Yield();
                elapsed += Time.unscaledTimeAsDouble - before;
            }

            foreach (var handler in ActionMarkupHandlers) {
                await handler.OnCharacterWillAppear(i, line, cancellationToken).SuppressCancellationThrow();
            }

            TextElement.maxVisibleCharacters++;
            elapsed -= secondsPerCharacter;
        }

        TextElement.maxVisibleCharacters = characterCount;
        foreach (var handler in ActionMarkupHandlers) handler.OnLineDisplayComplete();
    }

    public void PrepareForContent(Yarn.Markup.MarkupParseResult line) {
        if (TextElement == null) return;
        TextElement.maxVisibleCharacters = 0;
        TextElement.text = line.Text;
        foreach (var handler in ActionMarkupHandlers) handler.OnPrepareForLine(line, TextElement);
    }

    public void ContentWillDismiss() {
        foreach (var handler in ActionMarkupHandlers) handler.OnLineWillDismiss();
    }

    public void ContentDidDismiss() {
        if (TextElement != null) TextElement.maxVisibleCharacters = 0;
    }
}
