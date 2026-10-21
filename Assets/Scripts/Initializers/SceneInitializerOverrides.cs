using Initializers;
using System.Collections.Generic;
using UnityEngine;

namespace Initializers {
    public static class SceneInitializerOverrides {
        public static List<MonoBehaviour> Init(this LevelData level, SceneInitializerPrefabs prefabs) {
            return level switch {

                _ => prefabs.All,
            };
        }

    }
}
