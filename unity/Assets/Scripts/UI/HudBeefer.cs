using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Runtime polish pass on the Karu/Wisp HP card. Finds existing TMP_Text
    /// labels (level pills, names) and bumps their size + adds outlines so
    /// they read clearly on screen. Idempotent — safe to call repeatedly.
    /// </summary>
    public static class HudBeefer
    {
        public static void Beef()
        {
            // Walk every TMP_Text in the scene
            foreach (var tm in Object.FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tm == null) continue;
                string txt = tm.text ?? "";

                // Level badges — "Lv.2", "Lv 2", "LVL 2"
                if (LooksLikeLevel(txt))
                {
                    if (tm.fontSize < 26f) tm.fontSize = 26f;
                    tm.fontStyle = FontStyles.Bold;
                    tm.outlineWidth = 0.30f;
                    tm.outlineColor = new Color(0.10f, 0.05f, 0.20f, 1f);
                    tm.color = new Color(0.10f, 0.08f, 0.18f);
                    BumpLevelBackground(tm);
                    continue;
                }

                // Character names — "Karu" or pet nicknames (Wisp, Drip, etc., plain text, short)
                if ((txt == "Karu" || txt == "Wisp" || txt == "Drip") && tm.fontSize < 30f)
                {
                    tm.fontSize = 30f;
                    tm.fontStyle = FontStyles.Bold;
                    tm.outlineWidth = 0.30f;
                    tm.outlineColor = new Color(0.10f, 0.05f, 0.20f, 1f);
                    tm.color = new Color(1f, 0.97f, 0.85f);
                }
            }

            Debug.Log("[HudBeefer] Beef-up pass applied to HP card labels.");
        }

        // Make the level badge's background clearly readable
        private static void BumpLevelBackground(TMP_Text tm)
        {
            // The level pill is usually the first parent Image of the level text
            Transform t = tm.transform.parent;
            int hops = 0;
            while (t != null && hops < 3)
            {
                var img = t.GetComponent<Image>();
                if (img != null && !img.gameObject.name.ToLower().Contains("hp"))
                {
                    // Brighten — gold-pill look
                    img.color = new Color(1f, 0.82f, 0.30f, 1f);
                    return;
                }
                t = t.parent;
                hops++;
            }
        }

        private static bool LooksLikeLevel(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length > 8) return false;
            string up = s.ToUpper();
            return up.StartsWith("LV.") || up.StartsWith("LV ") || up.StartsWith("LVL")
                   || (up.StartsWith("LV") && s.Length <= 5);
        }
    }
}
