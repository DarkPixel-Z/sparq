using UnityEngine;

namespace Sparq.Core
{
    /// <summary>
    /// Persistent player save data. Survives across sessions via PlayerPrefs / JSON.
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        public string  playerName   = "Sparq User";
        public string  ndProfile    = "ADHD";        // ADHD | Autism | Dyslexia | Other

        // XP & progression
        public int     totalXP      = 0;
        public int     currentXP    = 0;             // progress toward next level
        public int     level        = 1;
        public int     xpToNextLevel = 100;

        // Streaks
        public int     currentStreak = 0;
        public int     longestStreak = 0;
        public string  lastActiveDate = "";

        // Karu the Red Panda
        public string  petName      = "Karu";
        public int     petLevel     = 1;
        public string  petMood      = "happy";       // happy | tired | excited | sad

        // Journal
        public int     journalEntryCount = 0;

        // Rival
        public int     rivalXP      = 72;            // Fitch's XP — will sync with backend

        private const string SAVE_KEY = "sparq_player_data";

        public static PlayerData Load()
        {
            string json = PlayerPrefs.GetString(SAVE_KEY, "");
            if (string.IsNullOrEmpty(json))
                return new PlayerData();
            return JsonUtility.FromJson<PlayerData>(json);
        }

        public void Save()
        {
            PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }
    }
}
