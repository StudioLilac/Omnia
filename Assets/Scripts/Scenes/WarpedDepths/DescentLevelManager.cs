using System;
using System.Collections.Generic;
using Players;
using Players.Buff;
using Players.Fragments;
using UnityEngine;

namespace Scenes.Descent {
    public class DescentLevelManager : MonoBehaviour {
        public void OnEnable() {
            Player.Death += EndRun;
        }

        public void OnDisable() {
            Player.Death -= EndRun;
        }

        private void Start() {
            AudioManager.Instance.PlayBGM(AudioTracks.IntoTheWind);
        }

        private void EndRun() {
            BuffManager.Instance.ClearAllBuffs();
            BuffManager.Instance.ResetFragmentPoolToOriginal();
            LevelManager.Instance.CustomLevel(new ResultsScreen());
        }

        // for playtesting; allows "fishing" for certain fragments
        #if UNITY_EDITOR
        private void Update() {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                PlayerDataManager.Instance.warpedDepthsProgress++;
                LevelManager.Instance.NextLevel();
            }
        }
        #endif
    }
}
