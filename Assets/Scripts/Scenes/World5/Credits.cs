using System.Collections;
using System.Collections.Generic;
using Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Utils;

public class Credits : MonoBehaviour
{
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private float scrollSpeed = 50f;
    [SerializeField] private float endPositionY = 1000;

    [SerializeField] private FadeScreenHandler fadeScreen;

    void Start() {
        fadeScreen.SetDarkScreen();
        StartCoroutine(fadeScreen.FadeInLightScreen(2f));

        StartCoroutine(EndSequence());
    }

    void Update()
    {
        creditsPanel.transform.Translate(Vector3.up * (scrollSpeed * Time.deltaTime));
    }

    private IEnumerator EndSequence() {
        yield return new WaitUntil(() => creditsPanel.transform.localPosition.y >= endPositionY);
        yield return StartCoroutine(fadeScreen.FadeInDarkScreen(3f));
        LevelManager.Instance.NextLevel();
    }
}
