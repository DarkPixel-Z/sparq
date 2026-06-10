using UnityEditor;
using UnityEngine;

namespace Sparq.Editor
{
    /// <summary>Menu 195: Quick test triggers for the polish pass.</summary>
    public static class SparqTestPolish195
    {
        [MenuItem("Sparq/195. Test Boss Intro")]
        public static void TestBossIntro()
        {
            if (!Application.isPlaying)
            { EditorUtility.DisplayDialog("Sparq", "Hit ▶ Play first.", "OK"); return; }
            Sparq.UI.BossIntro.Show("Chapter Boss", () => {
                Sparq.Systems.BattleScene.CurrentStageIdx = 8;
                Sparq.Systems.BattleScene.StageHpMul = 280;
                Sparq.Systems.BattleScene.StageDmgMul = 160;
                Sparq.Systems.BattleScene.StageXpReward = 200;
                Sparq.Systems.BattleScene.StageGoldReward = 300;
                Sparq.Systems.BattleScene.StageOverrideName = "Chapter Boss";
                Sparq.Systems.BattleScene.Start("Stone Brute");
            });
        }

        [MenuItem("Sparq/195a. Test Screen Shake (in battle)")]
        public static void TestShake()
        {
            if (!Application.isPlaying)
            { EditorUtility.DisplayDialog("Sparq", "Hit ▶ Play, start a battle first.", "OK"); return; }
            Sparq.UI.ScreenShake.Shake(20f, 0.3f);
        }
    }
}
