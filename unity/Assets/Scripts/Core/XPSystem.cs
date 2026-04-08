using UnityEngine;
using UnityEngine.Events;

namespace Sparq.Core
{
    /// <summary>
    /// Central XP and leveling system. Raises events consumed by UI and pet.
    /// </summary>
    public class XPSystem : MonoBehaviour
    {
        public static XPSystem Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private int baseXPPerLevel = 100;
        [SerializeField] private float xpScaleFactor = 1.25f; // each level costs more XP

        // Events
        public UnityEvent<int>  OnXPGained;         // arg: amount gained
        public UnityEvent<int>  OnLevelUp;           // arg: new level
        public UnityEvent       OnRivalBeaten;

        private PlayerData _data;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _data = PlayerData.Load();
        }

        /// <summary>Award XP and handle level-up.</summary>
        public void Award(int amount)
        {
            _data.currentXP += amount;
            _data.totalXP   += amount;
            OnXPGained?.Invoke(amount);

            // Check level-up
            while (_data.currentXP >= _data.xpToNextLevel)
            {
                _data.currentXP    -= _data.xpToNextLevel;
                _data.level        += 1;
                _data.xpToNextLevel = Mathf.RoundToInt(baseXPPerLevel * Mathf.Pow(xpScaleFactor, _data.level - 1));
                OnLevelUp?.Invoke(_data.level);
            }

            // Check rival
            if (_data.totalXP > _data.rivalXP)
                OnRivalBeaten?.Invoke();

            _data.Save();
        }

        public int  TotalXP   => _data.totalXP;
        public int  CurrentXP => _data.currentXP;
        public int  Level     => _data.level;
        public int  XPToNext  => _data.xpToNextLevel;
        public int  RivalXP   => _data.rivalXP;
        public float XPProgress => (float)_data.currentXP / _data.xpToNextLevel;
    }
}
