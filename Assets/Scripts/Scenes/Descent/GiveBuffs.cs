using System.Collections.Generic;
using Players.Buff;
using UnityEngine;

namespace Scenes.Descent {
    public class GiveBuffs : MonoBehaviour {
        [SerializeField] private List<Buff> buffs;

        public void Start() {
            foreach (Buff buff in buffs) {
                BuffManager.Instance.ApplyBuff(buff);
            }
        }
    }
}
