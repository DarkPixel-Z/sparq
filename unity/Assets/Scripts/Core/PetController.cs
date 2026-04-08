using UnityEngine;
using UnityEngine.UI;

namespace Sparq.Core
{
    /// <summary>
    /// Controls Karu the Red Panda — animations, mood, tap reactions.
    /// Designed for a 2D sprite rig exported from Unreal Engine / Spine.
    /// </summary>
    public class PetController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator   petAnimator;
        [SerializeField] private Image      moodBubble;
        [SerializeField] private Sprite[]   moodSprites;  // 0=happy, 1=excited, 2=tired, 3=sad

        [Header("Tap Settings")]
        [SerializeField] private int        tapsForBonus = 5;

        private int     _tapCount;
        private float   _idleTimer;
        private const float IDLE_THRESHOLD = 120f; // 2 min without interaction → tired

        // Animator parameter hashes
        private static readonly int AnimTap      = Animator.StringToHash("Tap");
        private static readonly int AnimLevelUp  = Animator.StringToHash("LevelUp");
        private static readonly int AnimIdle     = Animator.StringToHash("Idle");
        private static readonly int AnimHappy    = Animator.StringToHash("Happy");
        private static readonly int AnimSad      = Animator.StringToHash("Sad");

        private void OnEnable()
        {
            XPSystem.Instance.OnXPGained.AddListener(OnXPGained);
            XPSystem.Instance.OnLevelUp.AddListener(OnLevelUp);
            XPSystem.Instance.OnRivalBeaten.AddListener(OnRivalBeaten);
        }

        private void OnDisable()
        {
            XPSystem.Instance.OnXPGained.RemoveListener(OnXPGained);
            XPSystem.Instance.OnLevelUp.RemoveListener(OnLevelUp);
            XPSystem.Instance.OnRivalBeaten.RemoveListener(OnRivalBeaten);
        }

        private void Update()
        {
            _idleTimer += Time.deltaTime;
            if (_idleTimer >= IDLE_THRESHOLD)
                SetMood("tired");
        }

        /// <summary>Called when user taps Karu on screen.</summary>
        public void OnTapped()
        {
            _tapCount++;
            _idleTimer = 0f;
            petAnimator.SetTrigger(AnimTap);

            if (_tapCount % tapsForBonus == 0)
                XPSystem.Instance.Award(5);  // bonus XP for petting
        }

        private void OnXPGained(int amount)
        {
            _idleTimer = 0f;
            petAnimator.SetTrigger(AnimHappy);
            SetMood("happy");
        }

        private void OnLevelUp(int newLevel)
        {
            petAnimator.SetTrigger(AnimLevelUp);
            SetMood("excited");
        }

        private void OnRivalBeaten()
        {
            petAnimator.SetTrigger(AnimHappy);
            SetMood("excited");
        }

        private void SetMood(string mood)
        {
            if (moodBubble == null) return;
            int idx = mood switch {
                "happy"   => 0,
                "excited" => 1,
                "tired"   => 2,
                "sad"     => 3,
                _         => 0,
            };
            if (idx < moodSprites.Length)
                moodBubble.sprite = moodSprites[idx];
        }
    }
}
