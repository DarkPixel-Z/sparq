using UnityEngine;

namespace Sparq.UI
{
    /// <summary>
    /// Una is the onboarding axolotl.
    /// Shown only while onboardingComplete == false.
    /// Once the user completes their first quest (or finishes the tutorial),
    /// Una fades out and stays hidden on future launches.
    ///
    /// She can be re-summoned later via Settings → "Replay tutorial".
    /// </summary>
    public class UnaController : MonoBehaviour
    {
        [SerializeField] private float fadeDuration = 0.8f;

        private SpriteRenderer _sr;
        private bool _fadingOut = false;
        private float _fadeT = 0f;
        private bool _hiddenLastFrame = false;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            // Una is now the persistent HELP button — never auto-hide.
            // (HelpButton component handles tap → open help popup.)
            _hiddenLastFrame = true; // suppress legacy fade-out trigger
        }

        private void Update()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;

            // If onboarding just flipped to true this session, start fade out
            if (data.onboardingComplete && !_fadingOut && !_hiddenLastFrame)
            {
                _fadingOut = true;
                _fadeT = 0f;
            }

            if (_fadingOut)
            {
                _fadeT += Time.deltaTime;
                float k = Mathf.Clamp01(_fadeT / fadeDuration);
                if (_sr != null)
                {
                    var c = _sr.color;
                    c.a = 1f - k;
                    _sr.color = c;
                }
                // Drift up and away
                transform.position += Vector3.up * Time.deltaTime * 0.8f;
                transform.localScale *= (1f - Time.deltaTime * 0.2f);

                if (k >= 1f)
                {
                    gameObject.SetActive(false);
                    _fadingOut = false;
                    _hiddenLastFrame = true;
                    Debug.Log("[Una] Tutorial complete — farewell 🦎");
                }
            }
        }

        /// <summary>Call this to complete the tutorial and trigger Una's goodbye fade.</summary>
        public static void CompleteOnboarding()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null || data.onboardingComplete) return;
            data.onboardingComplete = true;
            Sparq.Core.SaveService.ScheduleSave();
            Debug.Log("[Una] Onboarding marked complete.");
        }

        /// <summary>Settings → Replay tutorial: resets and brings Una back.</summary>
        public static void RestartOnboarding()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;
            data.onboardingComplete = false;
            Sparq.Core.SaveService.ScheduleSave();
            Debug.Log("[Una] Tutorial restarted.");
        }
    }
}
