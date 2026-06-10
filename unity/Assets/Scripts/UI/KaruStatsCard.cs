using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Top-of-home Karu stats card. Mirrors the WebView design:
    ///   • Portrait + name + level badge
    ///   • Status text ("Happy Today")
    ///   • XP bar
    ///   • 3 stat tiles: Quests | Streak | Total XP
    /// Reads from PlayerData each frame so it stays live.
    /// </summary>
    public class KaruStatsCard : MonoBehaviour
    {
        public TMP_Text nameText;
        public TMP_Text levelText;
        public TMP_Text statusText;
        public TMP_Text xpText;
        public Slider   xpSlider;

        public TMP_Text attackValue;
        public TMP_Text defenseValue;
        public TMP_Text speedValue;

        private void Update()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;

            if (nameText  != null) nameText.text  = string.IsNullOrEmpty(data.petName) ? "Karu" : data.petName;
            if (levelText != null) levelText.text = $"Lv.{data.level}";

            // Status by hunger / streak (no emoji — default TMP font lacks them)
            string status = "Happy Today";
            if (data.hunger < 30) status = "Hungry";
            else if (data.streak >= 7) status = "On Fire!";
            else if (data.streak >= 3) status = $"{data.streak}-day streak";
            if (statusText != null) statusText.text = status;

            if (xpText != null) xpText.text = $"{data.currentXP} / {data.xpToNextLevel} XP";
            if (xpSlider != null && data.xpToNextLevel > 0)
                xpSlider.value = (float)data.currentXP / data.xpToNextLevel;

            // Classic RPG stats — derived from level (raise on level up)
            int atk = 5 + data.level * 2;
            int def = 3 + data.level;
            int spd = 5 + data.level;
            if (attackValue  != null) attackValue.text  = atk.ToString();
            if (defenseValue != null) defenseValue.text = def.ToString();
            if (speedValue   != null) speedValue.text   = spd.ToString();
        }
    }
}
