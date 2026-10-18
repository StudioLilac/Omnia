using UnityEngine;
using UnityEngine.InputSystem;
using Yarn.Unity;

[RequireComponent(typeof(LineAdvancer))]
public class YarnInputSystemLineAdvancer : MonoBehaviour, ILineAdvancerInput {
    [SerializeField] private LineAdvancer lineAdvancer;
    [SerializeField] private InputActionReference advanceAction;
    [SerializeField] private InputActionReference fastForwardAction;

    public LineAdvancer LineAdvancer {
        get => lineAdvancer;
        set => lineAdvancer = value;
    }

    private void Reset() {
        lineAdvancer = GetComponent<LineAdvancer>();
    }

    private void Update() {
        if (lineAdvancer == null) return;

        bool advance = (advanceAction != null && advanceAction.action != null && advanceAction.action.WasPressedThisFrame())
                       || (fastForwardAction != null && fastForwardAction.action != null && fastForwardAction.action.WasPressedThisFrame());
        if (advance) {
            lineAdvancer.OnInputHurryUpLines();
        }
    }

    public void OnDialogueStarted() { }
    public void OnDialogueComplete() { }
}
