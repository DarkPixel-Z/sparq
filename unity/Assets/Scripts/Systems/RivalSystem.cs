using UnityEngine;
using UnityEngine.Events;

namespace Sparq.Systems
{
    /// <summary>
    /// Tracks Fitch (the rival). Simulates Fitch's progress over time so he
    /// stays competitive. In a live build this would sync to a backend.
    /// </summary>
    public class RivalSystem : MonoBehaviour
    {
        public static RivalSystem Instance { get; private set; }

        [Header("Rival Config")]
        [SerializeField] private string rivalName      = "Fitch";
        [SerializeField] private int    startingXP     = 72;
        [SerializeField] private int    xpPerHour      = 3;  // Fitch gains XP while you're offline

        public UnityEvent<int>  OnRivalXPChanged;     // arg: current rival XP
        public UnityEvent       OnPlayerAhead;
        public UnityEvent       OnRivalAhead;

        private int   _rivalXP;
        private bool  _playerWasAhead = false;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            _rivalXP = startingXP;
            SimulateOfflineProgress();
        }

        private void OnEnable()
        {
            Sparq.Core.XPSystem.Instance.OnXPGained.AddListener(OnPlayerXPGained);
        }

        private void OnDisable()
        {
            Sparq.Core.XPSystem.Instance.OnXPGained.RemoveListener(OnPlayerXPGained);
        }

        /// <summary>Simulate Fitch gaining XP while the player was offline.</summary>
        private void SimulateOfflineProgress()
        {
            string lastSession = PlayerPrefs.GetString("sparq_last_session", "");
            if (string.IsNullOrEmpty(lastSession)) return;

            if (System.DateTime.TryParse(lastSession, out var last))
            {
                double hoursGone = (System.DateTime.UtcNow - last).TotalHours;
                int gained = Mathf.RoundToInt((float)hoursGone * xpPerHour);
                _rivalXP += Mathf.Clamp(gained, 0, 50); // cap offline gains at 50 XP
            }

            PlayerPrefs.SetString("sparq_last_session", System.DateTime.UtcNow.ToString("o"));
            PlayerPrefs.Save();
        }

        private void OnPlayerXPGained(int amount)
        {
            int playerXP = Sparq.Core.XPSystem.Instance.TotalXP;
            bool playerAheadNow = playerXP > _rivalXP;

            if (playerAheadNow && !_playerWasAhead)
                OnPlayerAhead?.Invoke();
            else if (!playerAheadNow && _playerWasAhead)
                OnRivalAhead?.Invoke();

            _playerWasAhead = playerAheadNow;
        }

        public int  RivalXP    => _rivalXP;
        public string RivalName => rivalName;
        public int  XPGap      => _rivalXP - Sparq.Core.XPSystem.Instance.TotalXP;
    }
}
