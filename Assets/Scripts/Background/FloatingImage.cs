using System.Collections;
using UnityEngine;
using UnityEngine.UI;


namespace Background {
    public class FloatingImage : MonoBehaviour {
        [SerializeField] private float deltaFloatSpeed = 0.1f;
        [SerializeField] private float floatStrength = 2f;

        private float floatSpeed;

        private Vector3 startPosition;
        private Image image;
        private float randomOffset;

        private void Start() {
            startPosition = transform.position;
            image = GetComponent<Image>();

            floatSpeed = deltaFloatSpeed + Random.Range(-deltaFloatSpeed, deltaFloatSpeed);


        }

        private void Update() {
            float xOffset = Mathf.Sin(Time.time * floatSpeed + 1) * floatStrength;
            float yOffset = Mathf.Cos(Time.time * floatSpeed) * floatStrength;
            transform.position = startPosition + new Vector3(xOffset, yOffset, 0);
        }


    }

}
