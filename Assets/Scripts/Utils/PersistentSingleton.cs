using UnityEngine;

namespace Utils {
    /**
     * A singleton game object that persists across scene changes. This object
     * destroys itself if another object with the same component type is found.
     * Upon reference, this object creates itself if it does not already exist.
     */
    public abstract class PersistentSingleton<T> : MonoBehaviour where T : Component {
        private static T _singleton;
        public static T Instance => _singleton;

        [Tooltip("This object re-parents itself to the root of the scene hierarchy.")] //
        [SerializeField]
        internal bool root = true;

        [Tooltip("This object persists across scene changes.")] //
        [SerializeField]
        internal bool persistent = true;

        public void Awake() {
            _singleton ??= GetOrCreateSelf();

            if (_singleton != this) {
                OnDestroyDuplicateInstance();
                Destroy(gameObject);
            } else {
                if (root) transform.SetParent(null);
                OnAwake();
                if (persistent) DontDestroyOnLoad(gameObject);
            }
        }

        protected virtual void OnDestroyDuplicateInstance() {
        }

        protected virtual void OnAwake() {
        }

        private static T GetOrCreateSelf() => FindAnyObjectByType<T>() ?? new GameObject(typeof(T).Name).AddComponent<T>();
    }
}
