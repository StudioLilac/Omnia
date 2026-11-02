using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

namespace StudioLilac.Dialogue {
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "StudioLilac.Dialogue/AudioConfig")]
    public class AudioConfig : ScriptableObject {
        [SerializeField] private Dictionary<string, EventReference> effects = new();
        public bool TryGetValue(string key, out EventReference value) => effects.TryGetValue(key, out value);
    }
}
