using System.Collections.Generic;
using UnityEngine;

namespace StudioLilac.Dialogue {
    [CreateAssetMenu(fileName = "ActorConfig", menuName = "StudioLilac.Dialogue/ActorConfig")]
    public class ActorConfig : ScriptableObject {
        [SerializeField] private Dictionary<string, Sprite> sprites = new();
        public bool TryGetValue(string key, out Sprite value) => sprites.TryGetValue(key, out value);
    }
}
