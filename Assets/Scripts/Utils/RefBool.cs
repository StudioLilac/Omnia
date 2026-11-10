using System.Collections.Generic;

namespace Utils {
    public sealed class RefBool {
        private readonly HashSet<object> sources = new();

        public bool Value => sources.Count > 0;

        public void Set(object source) {
            if (source != null) sources.Add(source);
        }

        public void Unset(object source) {
            if (source != null) sources.Remove(source);
        }

        public static implicit operator bool(RefBool r) => r.Value;
    }
}
