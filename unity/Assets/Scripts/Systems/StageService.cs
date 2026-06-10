using UnityEngine;

namespace Sparq.Systems
{
    /// <summary>
    /// Stage progression. 1 chapter for now with 8 stages of increasing difficulty.
    /// Tracks unlock + star rating per stage in PlayerPrefs.
    /// </summary>
    public static class StageService
    {
        public class Stage
        {
            public int    index;        // 1-based for UI
            public string name;
            public string enemyKey;     // matches BattleScene enemy key
            public int    hpMul;        // %
            public int    dmgMul;       // %
            public int    xpReward;
            public int    goldReward;
        }

        public static readonly Stage[] CHAPTER1 = new[]
        {
            new Stage { index=1, name="Forest Patrol",    enemyKey="Forest Goblin", hpMul=100, dmgMul=100, xpReward=30,  goldReward=25 },
            new Stage { index=2, name="Rabid Pack",       enemyKey="Shadow Wolf",   hpMul=110, dmgMul=110, xpReward=45,  goldReward=40 },
            new Stage { index=3, name="Whisper Cave",     enemyKey="Mind Phantom",  hpMul=130, dmgMul=110, xpReward=55,  goldReward=50 },
            new Stage { index=4, name="Stone Sentinel",   enemyKey="Stone Brute",   hpMul=150, dmgMul=120, xpReward=70,  goldReward=70 },
            new Stage { index=5, name="Goblin Warlord",   enemyKey="Forest Goblin", hpMul=180, dmgMul=130, xpReward=90,  goldReward=90 },
            new Stage { index=6, name="Dark Hunt",        enemyKey="Shadow Wolf",   hpMul=200, dmgMul=140, xpReward=110, goldReward=120 },
            new Stage { index=7, name="Spectre Lord",     enemyKey="Mind Phantom",  hpMul=220, dmgMul=140, xpReward=130, goldReward=150 },
            new Stage { index=8, name="Chapter Boss",     enemyKey="Stone Brute",   hpMul=280, dmgMul=160, xpReward=200, goldReward=300 },
        };

        private const string KEY_HIGHEST  = "sparq.stage.highest";   // highest unlocked stage index (1-based)
        private const string KEY_STARS    = "sparq.stage.stars";     // "1:3,2:2,3:1" — index:stars

        public static int HighestUnlocked => Mathf.Max(1, PlayerPrefs.GetInt(KEY_HIGHEST, 1));

        public static int StarsFor(int stageIdx)
        {
            string raw = PlayerPrefs.GetString(KEY_STARS, "");
            if (string.IsNullOrEmpty(raw)) return 0;
            foreach (var seg in raw.Split(','))
            {
                var parts = seg.Split(':');
                if (parts.Length != 2) continue;
                if (int.TryParse(parts[0], out int idx) && idx == stageIdx
                    && int.TryParse(parts[1], out int stars))
                    return stars;
            }
            return 0;
        }

        public static bool IsUnlocked(int stageIdx) => stageIdx <= HighestUnlocked;
        public static bool IsCompleted(int stageIdx) => StarsFor(stageIdx) > 0;

        /// <summary>Award stars + unlock next stage. Stars = 1..3 based on HP % remaining.</summary>
        public static void RecordVictory(int stageIdx, float hpPctRemaining)
        {
            int stars = 1;
            if (hpPctRemaining >= 0.5f) stars = 2;
            if (hpPctRemaining >= 0.85f) stars = 3;

            // Save best
            int existing = StarsFor(stageIdx);
            if (stars > existing)
            {
                var dict = new System.Collections.Generic.Dictionary<int, int>();
                string raw = PlayerPrefs.GetString(KEY_STARS, "");
                foreach (var seg in raw.Split(','))
                {
                    var parts = seg.Split(':');
                    if (parts.Length != 2) continue;
                    if (int.TryParse(parts[0], out int i) && int.TryParse(parts[1], out int s)) dict[i] = s;
                }
                dict[stageIdx] = stars;

                var sb = new System.Text.StringBuilder();
                bool first = true;
                foreach (var kv in dict)
                {
                    if (!first) sb.Append(',');
                    sb.Append(kv.Key).Append(':').Append(kv.Value);
                    first = false;
                }
                PlayerPrefs.SetString(KEY_STARS, sb.ToString());
            }

            // Unlock next
            if (stageIdx >= HighestUnlocked && stageIdx + 1 <= CHAPTER1.Length)
                PlayerPrefs.SetInt(KEY_HIGHEST, stageIdx + 1);

            PlayerPrefs.Save();
        }

        public static int TotalStars()
        {
            int t = 0;
            foreach (var s in CHAPTER1) t += StarsFor(s.index);
            return t;
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(KEY_HIGHEST);
            PlayerPrefs.DeleteKey(KEY_STARS);
            PlayerPrefs.Save();
        }
    }
}
