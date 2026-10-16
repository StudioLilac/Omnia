using UnityEngine;
using Yarn.Unity;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/DialogueWrapper", order = 1)]
public class DialogueWrapper : ScriptableObject
{
    [SerializeField] private DialogueReference yarnDialogue = new();

    public DialogueReference Dialogue => yarnDialogue;
    public DialogueReference YarnDialogue => yarnDialogue;

    public void SetYarnDialogue(YarnProject project, string nodeName) {
        yarnDialogue = new DialogueReference(project, nodeName);
    }
}
