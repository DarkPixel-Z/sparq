using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sparq.Core;
using Sparq.Systems;

namespace Sparq.UI
{
    /// <summary>
    /// Drives the Home screen: XP bar, streak, quest list, rival card.
    /// Attach to the HomeScreen canvas root.
    /// </summary>
    public class HomeUI : MonoBehaviour
    {
        [Header("XP Bar")]
        [SerializeField] private Image      xpBarFill;
        [SerializeField] private TMP_Text   xpLabel;       // "55 / 100 XP"
        [SerializeField] private TMP_Text   levelBadge;    // "✦ Lv.3"

        [Header("Stats")]
        [SerializeField] private TMP_Text   questCountText;
        [SerializeField] private TMP_Text   streakCountText;
        [SerializeField] private TMP_Text   totalXPText;

        [Header("Rival Card")]
        [SerializeField] private TMP_Text   fitchXPText;
        [SerializeField] private TMP_Text   rivalGapText;
        [SerializeField] private Image      rivalCardBorder;
        [SerializeField] private Color      behindColor;
        [SerializeField] private Color      aheadColor;

        [Header("Streak")]
        [SerializeField] private TMP_Text   streakDaysText;

        [Header("Damage Number")]
        [SerializeField] private GameObject damageNumPrefab; // floating "+XP" popup
        [SerializeField] private Canvas     overlayCanvas;

        private void OnEnable()
        {
            XPSystem.Instance.OnXPGained.AddListener(OnXPGained);
            XPSystem.Instance.OnLevelUp.AddListener(OnLevelUp);
            TaskManager.Instance.OnTaskCompleted.AddListener(OnTaskCompleted);
        }

        private void OnDisable()
        {
            XPSystem.Instance.OnXPGained.RemoveListener(OnXPGained);
            XPSystem.Instance.OnLevelUp.RemoveListener(OnLevelUp);
            TaskManager.Instance.OnTaskCompleted.RemoveListener(OnTaskCompleted);
        }

        private void Start() => Refresh();

        public void Refresh()
        {
            var xp = XPSystem.Instance;
            xpBarFill.fillAmount = xp.XPProgress;
            xpLabel.text         = $"{xp.CurrentXP} / {xp.XPToNext} XP";
            levelBadge.text      = $"✦ Lv.{xp.Level}";
            totalXPText.text     = xp.TotalXP.ToString();

            int remaining = 0;
            foreach (var t in TaskManager.Instance.Tasks)
                if (!t.completed) remaining++;
            questCountText.text  = remaining.ToString();

            RefreshRival();
        }

        private void RefreshRival()
        {
            int gap = RivalSystem.Instance.XPGap;
            fitchXPText.text = RivalSystem.Instance.RivalXP.ToString();

            if (gap > 0)
            {
                rivalGapText.text  = $"You're {gap} XP behind — catch up!";
                rivalCardBorder.color = behindColor;
            }
            else if (gap < 0)
            {
                rivalGapText.text  = $"You're {Mathf.Abs(gap)} XP AHEAD of Fitch!";
                rivalCardBorder.color = aheadColor;
            }
            else
            {
                rivalGapText.text = "Dead even! Push harder!";
            }
        }

        private void OnXPGained(int amount)
        {
            Refresh();
            SpawnDamageNum(amount);
        }

        private void OnLevelUp(int level)
        {
            levelBadge.text = $"✦ Lv.{level}";
        }

        private void OnTaskCompleted(QuestTask task)
        {
            Refresh();
        }

        private void SpawnDamageNum(int amount)
        {
            if (damageNumPrefab == null || overlayCanvas == null) return;
            var go  = Instantiate(damageNumPrefab, overlayCanvas.transform);
            var txt = go.GetComponentInChildren<TMP_Text>();
            if (txt) txt.text = $"+{amount} XP";

            // Random position offset
            var rt   = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(
                Random.Range(-80f, 80f),
                Random.Range(-40f, 40f)
            );
            Destroy(go, 1.4f);
        }
    }
}
