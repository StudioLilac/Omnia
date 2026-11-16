using System;
using Inventory;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

namespace Players {
    public class PlayerDataManager : PersistentSingleton<PlayerDataManager> {
        [SerializeField] private bool devMode;

        [SerializeField] private Trinket[] trinkets;

        public void Start() {
            foreach (Trinket trinket in trinkets) {
                trinket.IsLocked = !devMode;
            }
        }

        public int playerSelectedWeapon = 0;
        public void OnEnable() {
            Player.OnWeaponChanged += HandleWeaponChange;
        }

        public void OnDisable() {
            Player.OnWeaponChanged -= HandleWeaponChange;
        }


        // Trinket data

        public void AddTrinket(Trinket trinket) {
            foreach (var t in trinkets) {
                if (t.trinketName == trinket.trinketName) {
                    t.IsLocked = false;
                    return;
                }
            }
        }



        // Current weapon

        public void HandleWeaponChange(int targetWeapon) {
            playerSelectedWeapon = targetWeapon;
        }
    }
}
