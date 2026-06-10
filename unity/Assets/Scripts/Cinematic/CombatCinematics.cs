using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Sparq.Cinematic
{
    /// <summary>
    /// Provides cinematic effects for combat: camera shake, hit pause,
    /// screen flash, chromatic punch. Call static methods from anywhere.
    /// </summary>
    public class CombatCinematics : MonoBehaviour
    {
        public static CombatCinematics Instance { get; private set; }

        private Camera _cam;
        private Vector3 _camBase;
        private bool   _shaking;
        private Canvas _overlayCanvas;
        private Image  _flashImage;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _cam = Camera.main;
            if (_cam != null) _camBase = _cam.transform.position;
            BuildFlashCanvas();
        }

        private void BuildFlashCanvas()
        {
            var go = new GameObject("[CombatFX Overlay]");
            go.transform.SetParent(transform, false);
            _overlayCanvas = go.AddComponent<Canvas>();
            _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _overlayCanvas.sortingOrder = 900;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>().enabled = false;

            var flashGO = new GameObject("Flash", typeof(RectTransform), typeof(Image));
            flashGO.transform.SetParent(go.transform, false);
            var rt = flashGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _flashImage = flashGO.GetComponent<Image>();
            _flashImage.color = new Color(1,1,1,0);
            _flashImage.raycastTarget = false;
        }

        public static void Shake(float intensity = 0.12f, float duration = 0.2f)
        {
            if (Instance == null) return;
            Instance.StartCoroutine(Instance.ShakeCR(intensity, duration));
        }

        private IEnumerator ShakeCR(float intensity, float duration)
        {
            if (_cam == null) yield break;
            if (_shaking) _camBase = _cam.transform.position; // reset base if re-shaking
            _shaking = true;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - t / duration;
                Vector3 offset = (Vector3)Random.insideUnitCircle * intensity * k;
                _cam.transform.position = _camBase + offset;
                yield return null;
            }
            _cam.transform.position = _camBase;
            _shaking = false;
        }

        public static void HitPause(float duration = 0.06f)
        {
            if (Instance == null) return;
            Instance.StartCoroutine(Instance.HitPauseCR(duration));
        }

        private IEnumerator HitPauseCR(float duration)
        {
            float prevScale = Time.timeScale;
            Time.timeScale = 0.001f;   // near-freeze
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = prevScale;
        }

        public static void Flash(Color color, float duration = 0.25f)
        {
            if (Instance == null) return;
            Instance.StartCoroutine(Instance.FlashCR(color, duration));
        }

        private IEnumerator FlashCR(Color color, float duration)
        {
            if (_flashImage == null) yield break;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - t / duration;
                color.a = k * 0.5f;
                _flashImage.color = color;
                yield return null;
            }
            _flashImage.color = new Color(0,0,0,0);
        }
    }
}
