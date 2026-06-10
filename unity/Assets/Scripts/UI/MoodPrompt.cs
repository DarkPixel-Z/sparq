using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sparq.Systems;

namespace Sparq.UI
{
    /// <summary>
    /// Floating "How fares your spirit?" prompt with 5 mood crystal buttons.
    /// Auto-hides for the rest of the day after a mood is logged.
    /// </summary>
    public class MoodPrompt : MonoBehaviour
    {
        [SerializeField] private GameObject crystalsRoot; // parent of the 5 crystal buttons
        [SerializeField] private TMP_Text   promptLabel;
        [SerializeField] private TMP_Text   streakLabel;

        private void Start()
        {
            Refresh();
        }

        public void Refresh()
        {
            bool logged = MoodService.LoggedToday();
            int streak  = MoodService.StreakDays();

            if (promptLabel != null)
                promptLabel.text = logged ? "Spirit logged for today" : "How fares your spirit?";

            if (streakLabel != null)
            {
                streakLabel.gameObject.SetActive(streak > 0);
                streakLabel.text = $"{streak}-day streak";
            }

            if (crystalsRoot != null)
                crystalsRoot.SetActive(!logged);
        }

        public void OnCrystalTapped(int moodIndex)
        {
            if (moodIndex < 0 || moodIndex >= MoodService.Crystals.Length) return;
            var (m, label, _) = MoodService.Crystals[moodIndex];
            MoodService.Log(m);

            // Floating toast
            try
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                    Sparq.UI.XPFloater.Spawn(canvas.transform,
                        transform.position + new Vector3(0, 80, 0),
                        $"Spirit logged: {label}",
                        new Color(0.85f, 0.95f, 1f));
            }
            catch {}

            Refresh();
        }
    }
}
