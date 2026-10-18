using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace UI
{
    public class HighlightManager : PersistentSingleton<HighlightManager>
    {
        [SerializeField] private Material highlightMaterial;
        [SerializeField] private GameObject reticlePrefab;
        [SerializeField] private float rotationSpeed = 30f;
        [SerializeField] private float edgeSmoothness = 20f;
        [SerializeField] private Color dimColor = new (0f, 0f, 0f, 0.9f);
        [SerializeField] private float fadeDuration = 0.3f;

        private int cutoutPosID;
        private int cutoutSizeID;
        private int dimColorID;
        private int edgeSmoothnessID;

        private List<GameObject> highlightedObjects = new List<GameObject>();
        private List<Transform> reticles = new List<Transform>();

        [SerializeField] private Image dimOverlay;

        private Coroutine fadeCoroutine;

        protected override void OnAwake()
        {
            cutoutPosID = Shader.PropertyToID("_CutoutPos");
            cutoutSizeID = Shader.PropertyToID("_CutoutSize");
            dimColorID = Shader.PropertyToID("_DimColor");
            edgeSmoothnessID = Shader.PropertyToID("_EdgeSmoothness");
        }

        private void Update()
        {
            RotateReticles();
        }

        public void CrosshairGameObject(GameObject target)
        {
            if (!highlightedObjects.Contains(target))
            {
                highlightedObjects.Add(target);

                GameObject reticleObj = Instantiate(reticlePrefab, target.transform);
                Transform reticleTransform = reticleObj.transform;

                reticles.Add(reticleTransform);
                reticleTransform.localPosition = Vector3.zero;

                ReticleCleanup cleanup = reticleObj.AddComponent<ReticleCleanup>();
                cleanup.Init(target, this);
            }
        }

        public void UncrosshairGameObject(GameObject target)
        {
            int index = highlightedObjects.IndexOf(target);
            if (index >= 0)
            {
                highlightedObjects.RemoveAt(index);

                if (index < reticles.Count)
                {
                    if (reticles[index] != null)
                    {
                        Destroy(reticles[index].gameObject);
                    }
                    reticles.RemoveAt(index);
                }
            }
        }

        private void RotateReticles()
        {
            for (int i = reticles.Count - 1; i >= 0; i--)
            {
                if (reticles[i] == null)
                {
                    reticles.RemoveAt(i);
                }
                else
                {
                    reticles[i].Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
                }
            }
        }

        public void ClearAllHighlights()
        {
            foreach (var reticle in reticles)
            {
                if (reticle != null) Destroy(reticle.gameObject);
            }

            highlightedObjects.Clear();
            reticles.Clear();

            Unhighlight();
        }

        public void Highlight(RectTransform uiTarget)
        {
            var corners = new Vector3[4];
            var screenPoints = uiTarget.GetComponentsInChildren<RectTransform>()
                .SelectMany(rt => { rt.GetWorldCorners(corners); return corners; })
                .Select(c => RectTransformUtility.WorldToScreenPoint(null, c));

            ShowCutout(screenPoints);
        }

        public void Highlight(Transform worldTarget)
        {
            Camera cam = Camera.main;
            var spriteRenderers = worldTarget.GetComponentsInChildren<SpriteRenderer>();
            if (spriteRenderers.Length == 0 || cam == null) return;

            Bounds bounds = spriteRenderers[0].bounds;
            foreach (var sr in spriteRenderers.Skip(1))
                bounds.Encapsulate(sr.bounds);

            Vector2 screenMin = cam.WorldToScreenPoint(bounds.min);
            Vector2 screenMax = cam.WorldToScreenPoint(bounds.max);

            ShowCutout(new[] { screenMin, screenMax });
        }

        private void ShowCutout(IEnumerable<Vector2> screenPoints)
        {
            Vector2 min = screenPoints.Aggregate(Vector2.Min);
            Vector2 max = screenPoints.Aggregate(Vector2.Max);
            Vector2 center = (min + max) * 0.5f;
            Vector2 halfSize = (max - min) * 0.5f;

            highlightMaterial.SetVector(cutoutPosID, new Vector4(center.x, center.y, 0f, 0f));
            highlightMaterial.SetVector(cutoutSizeID, new Vector4(halfSize.x, halfSize.y, 0f, 0f));
            highlightMaterial.SetFloat(edgeSmoothnessID, edgeSmoothness);

            dimOverlay.gameObject.SetActive(true);

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOverlay(0f, dimColor.a));
        }

        public void Unhighlight()
        {
            if (dimOverlay == null || !dimOverlay.gameObject.activeSelf) return;
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOverlay(highlightMaterial.GetColor(dimColorID).a, 0f));
        }

        private IEnumerator FadeOverlay(float startAlpha, float endAlpha)
        {
            yield return FadeHelpers.FadeMaterialColor(highlightMaterial, dimColorID, dimColor, startAlpha, endAlpha, fadeDuration);
            if (endAlpha <= 0f && dimOverlay != null) dimOverlay.gameObject.SetActive(false);
            fadeCoroutine = null;
        }
    }

    public class ReticleCleanup : MonoBehaviour
    {
        private GameObject target;
        private HighlightManager manager;

        public void Init(GameObject target, HighlightManager manager)
        {
            this.target = target;
            this.manager = manager;
        }

        private void OnDestroy()
        {
            if (target != null)
            {
                manager?.UncrosshairGameObject(target);
            }
        }
    }
}
