using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Drives the rival card on the home screen.
    /// Shows the current enemy's name, title, and HP (= gap between player totalXP and enemy fitchXP).
    /// HP empties as player closes the XP gap. When it hits 0, player defeats this enemy.
    /// </summary>
    public class RivalDisplay : MonoBehaviour
    {
        private void SwapPortrait(Sparq.Systems.RivalRoster.Rival rival)
        {
            var portraitGO = GameObject.Find("VoltPortrait");
            if (portraitGO == null) return;
            var img = portraitGO.GetComponent<Image>();
            if (img == null) return;

            // Animated rival? Load idle frames + attach UISpriteAnimator
            if (!string.IsNullOrEmpty(rival.folderName))
            {
                #if UNITY_EDITOR
                string dir = $"Assets/Fantasy Monster Pack 5 Handcrafted 2D Creatures/{rival.folderName}/{rival.animSubfolder}";
                if (System.IO.Directory.Exists(dir))
                {
                    var files = System.IO.Directory.GetFiles(dir, "*.png");
                    System.Array.Sort(files);
                    var frames = new System.Collections.Generic.List<Sprite>();
                    foreach (var f in files)
                    {
                        string ap = f.Replace('\\','/');
                        int idx = ap.IndexOf("Assets/");
                        if (idx >= 0) ap = ap.Substring(idx);
                        var sp = Sparq.Core.SpriteLoader.Load(ap);
                        if (sp != null) frames.Add(sp);
                    }
                    if (frames.Count > 0)
                    {
                        var anim = portraitGO.GetComponent<UISpriteAnimator>();
                        if (anim == null) anim = portraitGO.AddComponent<UISpriteAnimator>();
                        anim.SetFrames(frames.ToArray(), 8f);
                        img.sprite = frames[0];
                        img.preserveAspect = true;
                        return;
                    }
                }
                #endif
            }

            // Static sprite
            if (!string.IsNullOrEmpty(rival.staticSpritePath))
            {
                var anim = portraitGO.GetComponent<UISpriteAnimator>();
                if (anim != null) Destroy(anim); // stop any running animation
                #if UNITY_EDITOR
                var sp = Sparq.Core.SpriteLoader.Load(rival.staticSpritePath);
                if (sp != null)
                {
                    img.sprite = sp;
                    img.preserveAspect = true;
                }
                #endif
            }
        }

        [Header("Refs")]
        [SerializeField] private Image    hpFillImage;
        [SerializeField] private Slider   hpSlider;       // Fantasy Hero prefab uses Slider
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Image    portraitImage;   // rival avatar

        [Header("Animation")]
        [SerializeField] private float hpLerpSpeed = 5f;

        // For now we have just one "enemy" — Volt. Future: swap to a current enemy table.
        private const string DEFAULT_NAME  = "Volt";
        private const string DEFAULT_TITLE = "Electric Wolf";
        private const int    VOLT_START_XP = 72;  // matches WebView initial fitchXP

        private float _displayedHp = 1f;
        private string _lastRivalName = "";

        private void Start()
        {
            // Auto-bind missing refs from children
            if (hpSlider == null) hpSlider = GetComponentInChildren<Slider>(true);
            if (hpFillImage == null && hpSlider != null && hpSlider.fillRect != null)
                hpFillImage = hpSlider.fillRect.GetComponent<Image>();
            if (hpSlider != null) hpSlider.interactable = false;

            if (nameText  != null) nameText.text  = DEFAULT_NAME;
            if (titleText != null) titleText.text = DEFAULT_TITLE;
        }

        private void Update()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;

            // Read current rival from roster
            var rival = (Sparq.Systems.RivalManager.Instance != null)
                ? Sparq.Systems.RivalManager.Instance.GetCurrentRival()
                : new Sparq.Systems.RivalRoster.Rival { name = DEFAULT_NAME, title = DEFAULT_TITLE, baseHpXP = VOLT_START_XP };

            if (nameText  != null) nameText.text  = rival.name;
            if (titleText != null) titleText.text = rival.title;

            // Auto-swap portrait when rival changes
            if (rival.name != _lastRivalName)
            {
                _lastRivalName = rival.name;
                SwapPortrait(rival);
            }

            // HP = rival's max HP pool - amount whittled down
            int gap = Mathf.Max(0, data.fitchXP - data.totalXP);
            float maxGap = Mathf.Max(1f, rival.baseHpXP);
            float targetHp = Mathf.Clamp01((float)gap / maxGap);

            _displayedHp = Mathf.MoveTowards(_displayedHp, targetHp, Time.deltaTime * hpLerpSpeed);

            // Drive both possible bar systems
            if (hpSlider != null) hpSlider.value = _displayedHp;
            if (hpFillImage != null && hpFillImage.type == Image.Type.Filled)
                hpFillImage.fillAmount = _displayedHp;
            if (hpText != null)
            {
                int pct = Mathf.RoundToInt(_displayedHp * 100f);
                hpText.text = $"HP {pct}%";
            }
        }
    }
}
