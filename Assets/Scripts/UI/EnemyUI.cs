using Enemies;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Serialization;

namespace UI {
    public class EnemyUI : MonoBehaviour {
        [SerializeField] internal RectTransform rect;
        [SerializeField] internal Canvas canvas;
        [SerializeField] internal Slider slider;  // Health bar
        [SerializeField] internal Slider shield;  // Shield bar
        [SerializeField] internal Enemy subject;
        [SerializeField] internal Canvas parentCanvas;

        public void Start() {
            shield.gameObject.SetActive(subject is ShieldedEnemy && ((ShieldedEnemy)subject).maxShieldHealth > 0);
            ShowParent(false);
        }

        public void Update() {
            UpdateHiddenVisibility();
            UpdateHealthBar();
            UpdateShieldBar();
        }

        public EnemyUI Of(Enemy e) {
            subject = e;
            return this;
        }

        public void OnCameraUpdate() {
            rect.anchoredPosition = WorldToUIPoint(subject.transform.position, canvas.worldCamera);
        }

        private Vector2 WorldToUIPoint(Vector2 position, Camera c) {
            if (!canvas.enabled) return Vector2.zero;
            var p = c.WorldToViewportPoint(position);
            var d = 2 * canvas.scaleFactor;
            return new Vector2(c.pixelWidth * (p.x * 2 - 1) / d, c.pixelHeight * (p.y * 2 - 1) / d);
        }

        private void UpdateHiddenVisibility() {
            if (subject is not HiddenEnemy) return;
            canvas.enabled = !((HiddenEnemy) subject).hidden;
        }

        private void UpdateHealthBar() {
            slider.value = subject.maximumHealth == 0 ? 0 : subject.currentHealth / subject.maximumHealth;
        }

        private void UpdateShieldBar() {
            if (subject is not ShieldedEnemy) return;
            ShieldedEnemy shieldedEnemy = (ShieldedEnemy) subject;
            shield.value = shieldedEnemy.shieldHealth == 0 ? 0 : shieldedEnemy.shieldHealth / shieldedEnemy.maxShieldHealth;
        }

        public void ShowParent(bool show) {
            parentCanvas.enabled = show;
        }
    }
}
