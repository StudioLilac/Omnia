using System.Collections;
using Background;
using Initializers;
using Players;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utils;


namespace Scenes {
    public class Results : WarpedDepths
    {
        [SerializeField] TextMeshProUGUI resultText;
        protected void Start() {
            base.Start();
            resultText.text = $"{PlayerDataManager.Instance.warpedDepthsProgress}";

            PlayerDataManager.Instance.warpedDepthsProgress = 0;
        }
    }


}
