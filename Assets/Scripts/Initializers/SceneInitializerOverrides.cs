using Initializers;
using System.Collections.Generic;
using UnityEngine;

namespace Initializers {
    public static class SceneInitializerOverrides {
        public static List<MonoBehaviour> Init(this LevelData level, SceneInitializerPrefabs prefabs) {
            return level switch {
                Title or Opening or Birthday or Level_Select or Warped_Depths or Fragment_Selection =>
                    prefabs.AllExcept(prefabs.HUDManager, prefabs.pauseMenu, prefabs.inventoryManager),

                Level_Credits =>
                    prefabs.AllExcept(prefabs.HUDManager, prefabs.pauseMenu, prefabs.inventoryManager, prefabs.screenShakeManager),

                Tutorial or City or ArmadilloRoom =>
                    prefabs.AllExcept(prefabs.inventoryManager),

                Level_B_1 =>
                    prefabs.AllExcept(prefabs.HUDManager),

                _ => prefabs.All,
            };
        }

    }
}
