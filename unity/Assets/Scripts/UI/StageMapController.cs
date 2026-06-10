using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Drives the Play_Stage_Select_1 prefab:
    /// • Overrides stage name with "N. RivalName" from our roster
    /// • Prev/Next buttons browse the full roster
    /// • Current-stage indicator highlights the active rival
    /// • "Challenge" (any button in prefab) → close map + seed fresh HP for that rival
    /// </summary>
    public class StageMapController : MonoBehaviour
    {
        private int _viewedIndex;
        private TMP_Text _stageNameTMP;
        private TMP_Text _levelTMP;
        private Button _prev, _next;

        public void Init()
        {
            var data = Sparq.Core.SaveService.Data;
            _viewedIndex = data != null ? data.currentRivalIndex : 0;

            // Find the stage-name text ("1. Sandstorm Desert")
            foreach (var tmp in GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null) continue;
                if (tmp.gameObject.name == "Text_StageName") _stageNameTMP = tmp;
                else if (tmp.text != null && tmp.text.Contains("Recommended")) _levelTMP = tmp;
            }

            // Find Prev/Next buttons
            foreach (var btn in GetComponentsInChildren<Button>(true))
            {
                if (btn == null) continue;
                if (btn.gameObject.name == "Button_Prev") { _prev = btn; _prev.onClick.RemoveAllListeners(); _prev.onClick.AddListener(OnPrev); }
                else if (btn.gameObject.name == "Button_Next") { _next = btn; _next.onClick.RemoveAllListeners(); _next.onClick.AddListener(OnNext); }
                else
                {
                    // Any other button = "Challenge this rival"
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(OnChallenge);
                }
            }

            Refresh();
        }

        private void OnPrev()
        {
            _viewedIndex--;
            if (_viewedIndex < 0) _viewedIndex = Sparq.Systems.RivalRoster.ROSTER.Length - 1;
            Refresh();
            Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
        }

        private void OnNext()
        {
            _viewedIndex++;
            if (_viewedIndex >= Sparq.Systems.RivalRoster.ROSTER.Length) _viewedIndex = 0;
            Refresh();
            Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
        }

        private void OnChallenge()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;

            var r = Sparq.Systems.RivalRoster.ROSTER[_viewedIndex];
            if (data.level < r.minLevel)
            {
                // Locked — bounce-back
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Hit, volumeScale: 0.3f);
                Debug.Log($"[Map] {r.name} locked — need level {r.minLevel}, you are {data.level}.");
                return;
            }

            // Confirm rival + seed fresh HP
            data.currentRivalIndex = _viewedIndex;
            data.fitchXP = data.totalXP + r.baseHpXP;
            Sparq.Core.SaveService.Save();

            Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Whoosh);

            // Close the map
            if (PopupManager.Instance != null) PopupManager.Instance.Dismiss();
        }

        private void Refresh()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;
            var r = Sparq.Systems.RivalRoster.ROSTER[_viewedIndex];

            bool locked = data.level < r.minLevel;
            string prefix = locked ? "🔒 " : "";

            if (_stageNameTMP != null)
            {
                _stageNameTMP.text = $"{prefix}{_viewedIndex + 1}. {r.name} — {r.title}";
                _stageNameTMP.color = locked ? new Color(1, 1, 1, 0.4f) : Color.white;
            }
            if (_levelTMP != null)
            {
                _levelTMP.text = locked
                    ? $"Unlocks at Lv.{r.minLevel}"
                    : $"Tier: {r.tier} • HP {r.baseHpXP}";
            }
        }
    }
}
