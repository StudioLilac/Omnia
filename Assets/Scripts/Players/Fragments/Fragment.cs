using System.Collections.Generic;
using Players.Buff;
using UnityEngine;

namespace Players.Fragments {
    public abstract class Fragment : Buff.Buff {
        [SerializeField] protected List<Fragment> nextFragments;
        [SerializeField] public string fragmentName;
        [SerializeField, TextArea] public string description;

        public override void ApplyBuff() {
            BuffManager.Instance.AddFragmentsToPool(nextFragments);
            BuffManager.Instance.RemoveFragmentFromPool(this);
        }

        public override bool Equals(object obj) {
            if (obj is Fragment otherFragment) {
                return fragmentName == otherFragment.fragmentName;
            }
            return false;
        }

        public override int GetHashCode() {
            return fragmentName.GetHashCode();
        }
    }
}
