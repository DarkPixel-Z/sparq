using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Drives the XP bar + level text on the home screen. Also detects level-ups
    /// and spawns celebration floaters + flashes the bar.
    /// </summary>
    public class XPBarDisplay : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Image    fillImage;
        [SerializeField] private Slider   slider;       // Fantasy Hero prefab uses Slider
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text xpText;

        [Header("Animation")]
        [SerializeField] private float fillLerpSpeed = 6f;

        private float _displayedFill = 0f;
        private int   _lastSeenLevel = -1;
        private Color _fillBaseColor;
        private float _flashTimer = 0f;

        private void Start()
        {
            // Auto-bind anything missing
            if (slider == null) slider = GetComponentInChildren<Slider>(true);
            if (fillImage == null && slider != null && slider.fillRect != null)
                fillImage = slider.fillRect.GetComponent<Image>();
            if (levelText == null || xpText == null) AutoBindTexts();

            if (fillImage != null) _fillBaseColor = fillImage.color;
            // Disable Slider's interactive eat-the-tap behavior — it's a display
            if (slider != null) slider.interactable = false;
        }

        private void AutoBindTexts()
        {
            foreach (var tmp in GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null) continue;
                string t = tmp.text ?? "";
                if (levelText == null && (t.StartsWith("Lv") || t.Length < 6))
                    levelText = tmp;
                else if (xpText == null && (t.Contains("XP") || t.Contains("/")))
                    xpText = tmp;
            }
        }

        private void Update()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;

            // Detect level-up
            if (_lastSeenLevel == -1) _lastSeenLevel = data.level;
            if (data.level > _lastSeenLevel)
            {
                OnLevelUp(data.level);
                _lastSeenLevel = data.level;
            }

            float target = (data.xpToNextLevel > 0)
                ? (float)data.currentXP / data.xpToNextLevel
                : 0f;
            target = Mathf.Clamp01(target);

            _displayedFill = Mathf.MoveTowards(_displayedFill, target, Time.deltaTime * fillLerpSpeed);

            // Drive both the slider and the fill image (whichever the prefab uses)
            if (slider    != null) slider.value      = _displayedFill;
            if (fillImage != null && fillImage.type == Image.Type.Filled)
                fillImage.fillAmount = _displayedFill;

            // Level shown on Karu stats card now — keep this text empty to avoid duplicate
            if (levelText != null && levelText.gameObject.activeInHierarchy) levelText.text = "";
            if (xpText    != null) xpText.text    = $"{data.currentXP} / {data.xpToNextLevel} XP";

            // Flash fade-out after level up
            if (_flashTimer > 0f && fillImage != null)
            {
                _flashTimer -= Time.deltaTime;
                float k = Mathf.Clamp01(_flashTimer / 0.8f);
                fillImage.color = Color.Lerp(_fillBaseColor, Color.white, k * 0.8f);
                if (_flashTimer <= 0f) fillImage.color = _fillBaseColor;
            }
        }

        private void OnLevelUp(int newLevel)
        {
            Debug.Log($"[XPBar] LEVEL UP! → {newLevel}");
            _flashTimer = 0.8f;
            Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.LevelUp);

            // Show the full-screen LevelUp popup (GUI Pro Fantasy Hero prefab)
            if (Sparq.UI.PopupManager.Instance != null)
                Sparq.UI.PopupManager.Instance.ShowLevelUp(newLevel);

            // Spawn a big floater for the level up
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && fillImage != null)
            {
                var pos = fillImage.rectTransform.position + new Vector3(0, 80f, 0);
                var floater = XPFloater.Spawn(canvas.transform, pos, $"LEVEL {newLevel}!", new Color(1f, 0.85f, 0.2f));
            }

            // Fire any Feel MMF_Player on this GameObject (like screen shake) via reflection
            foreach (var mb in GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                if (mb.GetType().Name == "MMF_Player")
                {
                    var method = mb.GetType().GetMethod("PlayFeedbacks", new System.Type[0]);
                    method?.Invoke(mb, null);
                    break;
                }
            }
        }
    }
}
