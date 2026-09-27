using Players.Buff;
using UnityEngine;

namespace Scenes {
    public class ArrowTest : MonoBehaviour {
        [SerializeField] Buff arrowbuff;
        private void Start() {
            BuffManager.Instance.ApplyBuff(arrowbuff);
        }
    }
}
