using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqQuestTools
    {
        [MenuItem("Sparq/21. Force Refresh Daily Quests (new pool)")]
        public static void ForceRefresh()
        {
            if (!Application.isPlaying)
            {
                // Edit-mode: directly wipe lastQuestResetDate so next Play regenerates.
                var data = Sparq.Core.SaveService.Data;
                if (data == null)
                {
                    // Nudge a load by calling SaveService if present
                    Sparq.Core.SaveService.Load();
                    data = Sparq.Core.SaveService.Data;
                }
                if (data != null)
                {
                    data.lastQuestResetDate = "";
                    data.completedToday = 0;
                    if (data.customTasks != null) data.customTasks.Clear();
                    Sparq.Core.SaveService.Save();
                    Debug.Log("[Sparq] Next Play will regenerate daily quests.");
                    EditorUtility.DisplayDialog("Sparq Quests",
                        "✅ Cleared daily reset date.\n\n" +
                        "Hit ▶ Play — QuestManager will detect the day change and pull 4 fresh quests from the pool of 20.",
                        "OK");
                }
                return;
            }

            // Play-mode: call the manager directly
            var mgr = Sparq.Systems.QuestManager.Instance;
            if (mgr != null)
            {
                mgr.ForceRefresh();
                Debug.Log("[Sparq] Quests refreshed.");
            }
        }
    }
}
