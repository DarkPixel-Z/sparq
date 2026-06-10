using UnityEngine;

namespace Sparq.Systems
{
    /// <summary>
    /// Manages the current rival: swap to next monster when one is defeated,
    /// award victory rewards, update the rival card visuals.
    /// </summary>
    public class RivalManager : MonoBehaviour
    {
        public static RivalManager Instance { get; private set; }

        public event System.Action<int> OnRivalDefeated;   // passes index defeated
        public event System.Action<int> OnRivalChanged;    // passes new index

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Ensure fitchXP is seeded to the current rival's HP pool
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;

            int idx = Mathf.Clamp(data.currentRivalIndex, 0, RivalRoster.ROSTER.Length - 1);
            var r = RivalRoster.ROSTER[idx];

            // If fitchXP <= totalXP (rival already at 0 HP), seed fresh
            if (data.fitchXP <= data.totalXP)
            {
                data.fitchXP = data.totalXP + r.baseHpXP;
                Sparq.Core.SaveService.Save();
                Debug.Log($"[RivalManager] Seeded fresh HP for {r.name} → fitchXP={data.fitchXP}");
            }
        }

        /// <summary>Call this when a tap reduces fitchXP to 0. Awards loot + advances.</summary>
        public void CheckDefeat()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;

            // HP = fitchXP - totalXP. If that's <= 0, rival is defeated.
            int hp = data.fitchXP - data.totalXP;
            if (hp > 0) return;

            int defeatedIdx = data.currentRivalIndex;
            var defeated = RivalRoster.ROSTER[defeatedIdx];

            // Rewards
            int bonusXP    = 40 + defeatedIdx * 20;   // harder rivals pay more
            int bonusCoins = 150 + defeatedIdx * 75;
            data.sparqCoins += bonusCoins;
            data.rivalsDefeated++;
            Progression.GrantXp(data, bonusXP);   // single canonical curve

            // Advance to next rival (loop back to first if at end)
            int nextIdx = (defeatedIdx + 1) % RivalRoster.ROSTER.Length;
            var next = RivalRoster.ROSTER[nextIdx];
            data.currentRivalIndex = nextIdx;

            // Seed fresh HP for the new rival
            data.fitchXP = data.totalXP + next.baseHpXP;

            Sparq.Core.SaveService.Save();

            Debug.Log($"[RivalManager] DEFEATED {defeated.name}! +{bonusXP} XP, +{bonusCoins} coins. Next: {next.name}.");

            // Victory audio + cinematic
            Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Victory);
            Sparq.Cinematic.CombatCinematics.Shake(0.35f, 0.4f);
            Sparq.Cinematic.CombatCinematics.Flash(new Color(1f, 0.85f, 0.2f, 0.6f), 0.4f);

            // Loot drop popup (proper Super Casual reward UI)
            if (Sparq.UI.PopupManager.Instance != null
                && Sparq.UI.PopupManager.Instance.lootDropPrefab != null)
            {
                Sparq.UI.PopupManager.Instance.ShowLootDrop(defeated.name, bonusXP, bonusCoins);
            }
            else
            {
                // Fallback floater
                var portrait = GameObject.Find("VoltPortrait");
                if (portrait != null)
                {
                    var canvas = portrait.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        Sparq.UI.XPFloater.Spawn(canvas.transform,
                            portrait.transform.position + new Vector3(0, 40, 0),
                            $"DEFEATED! +{bonusXP} XP +{bonusCoins}c",
                            new Color(1f, 0.85f, 0.2f));
                    }
                }
            }

            OnRivalDefeated?.Invoke(defeatedIdx);
            OnRivalChanged?.Invoke(nextIdx);
        }

        public RivalRoster.Rival GetCurrentRival()
        {
            var data = Sparq.Core.SaveService.Data;
            int idx = data != null ? Mathf.Clamp(data.currentRivalIndex, 0, RivalRoster.ROSTER.Length - 1) : 0;
            return RivalRoster.ROSTER[idx];
        }
    }
}
