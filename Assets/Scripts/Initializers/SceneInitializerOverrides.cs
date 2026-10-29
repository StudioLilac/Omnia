using Initializers;
using System.Collections.Generic;
using UnityEngine;

namespace Initializers {
    public static class SceneInitializerOverrides {
        public static List<MonoBehaviour> Init(this LevelData level, SceneInitializerPrefabs prefabs) {
            return level switch {
                Title =>
                    prefabs.AllExcept(prefabs.HUDManager, prefabs.pauseMenu, prefabs.inventoryManager),

                Level_Credits =>
                    prefabs.AllExcept(prefabs.HUDManager, prefabs.pauseMenu, prefabs.inventoryManager, prefabs.screenShakeManager),

                Tutorial or City =>
                    prefabs.AllExcept(prefabs.inventoryManager),

                _ => prefabs.All,
            };
        }

    }
}
