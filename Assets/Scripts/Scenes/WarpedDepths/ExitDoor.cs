using Inventory;
using System.Collections;
using Players;
using UnityEngine;

namespace Scenes.Descent {
    public class ExitDoor : MonoBehaviour, IInteractable
    {
        public void Interact() {
            PlayerDataManager.Instance.warpedDepthsProgress++;
            LevelManager.Instance.NextLevel();
        }
    }

}
