using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sparq.Core;

namespace Sparq.UI
{
    /// <summary>
    /// Visual layer for the pet (Karu). Handles tap, mood bubble, evolution badge,
    /// and connecting the pet sprite to the PetController animator.
    /// </summary>
    public class PetUI : MonoBehaviour
    {
        [Header("Pet Visuals")]
        [SerializeField] private Button     petTapButton;
        [SerializeField] private Image      petSprite;
        [SerializeField] private TMP_Text   petNameText;
        [SerializeField] private TMP_Text   moodText;         // "Happy Today ✿"
        [SerializeField] private Image      evolutionGlow;    // set color per evo tier
        [SerializeField] private TMP_Text   levelBadgeText;   // "✦ Lv.3"

        [Header("Evolution Sprites")]
        [SerializeField] private Sprite[]   evolutionSprites; // 0=baby, 1=teen, 2=adult, 3=legendary

        [Header("Evolution Glow Colors")]
        [SerializeField] private Color      babyGlowColor;
        [SerializeField] private Color      teenGlowColor      = Color.yellow;
        [SerializeField] private Color      adultGlowColor     = Color.cyan;
        [SerializeField] private Color      legendaryGlowColor = Color.magenta;

        [Header("Mood Messages")]
        [SerializeField] private string[]   tapMessages = {
            "Karu loves you! 💕",
            "So happy! 🌸",
            "*happy red panda noises* 🐾",
            "More quests plz! ⚡",
            "Best human ever! 🥰",
            "Karu wants a nap... 😴",
        };

        private int _tapCount;

        private void OnEnable()
        {
            petTapButton.onClick.AddListener(OnPetTapped);
            XPSystem.Instance.OnLevelUp.AddListener(OnLevelUp);
            XPSystem.Instance.OnXPGained.AddListener(OnXPGained);
        }

        private void OnDisable()
        {
            petTapButton.onClick.RemoveListener(OnPetTapped);
            XPSystem.Instance.OnLevelUp.RemoveListener(OnLevelUp);
            XPSystem.Instance.OnXPGained.RemoveListener(OnXPGained);
        }

        private void Start()
        {
            Refresh();
        }

        public void Refresh()
        {
            int level = XPSystem.Instance.Level;
            levelBadgeText.text = $"✦ Lv.{level}";
            ApplyEvolution(level);
        }

        private void OnPetTapped()
        {
            PetController.Instance?.OnTapped();
            _tapCount++;
            moodText.text = tapMessages[_tapCount % tapMessages.Length];
        }

        private void OnLevelUp(int newLevel)
        {
            levelBadgeText.text = $"✦ Lv.{newLevel}";
            ApplyEvolution(newLevel);
        }

        private void OnXPGained(int _) { /* pet reacts via PetController animator */ }

        private void ApplyEvolution(int level)
        {
            int tier = level >= 15 ? 3 : level >= 10 ? 2 : level >= 5 ? 1 : 0;

            if (evolutionSprites != null && tier < evolutionSprites.Length && evolutionSprites[tier] != null)
                petSprite.sprite = evolutionSprites[tier];

            if (evolutionGlow != null)
            {
                evolutionGlow.color = tier switch {
                    1 => teenGlowColor,
                    2 => adultGlowColor,
                    3 => legendaryGlowColor,
                    _ => babyGlowColor,
                };
            }
        }
    }
}
