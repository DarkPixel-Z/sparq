using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 998: Diagnose why buttons aren't responding. Reports the state of
    /// EventSystem, Canvas raycasters, time scale, and any overlays.
    /// </summary>
    public static class SparqDiagnoseInput
    {
        [MenuItem("Sparq/998. Diagnose: why don't buttons work?")]
        public static void Diagnose()
        {
            var report = new System.Text.StringBuilder();

            // 1. Time scale
            report.AppendLine($"Time.timeScale = {Time.timeScale}  (should be 1)");

            // 2. EventSystem
            var es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null) report.AppendLine("❌ NO EventSystem in scene — buttons can't receive clicks!");
            else if (!es.isActiveAndEnabled) report.AppendLine($"❌ EventSystem '{es.name}' is DISABLED — enable it.");
            else report.AppendLine($"✅ EventSystem '{es.name}' active and enabled");

            // 3. Standalone input module
            var sim = Object.FindAnyObjectByType<StandaloneInputModule>();
            if (sim == null && es != null && es.GetComponent<BaseInputModule>() == null)
                report.AppendLine("❌ No InputModule on EventSystem — clicks won't fire.");

            // 4. Canvases + raycasters
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            report.AppendLine($"\nCanvases in scene: {canvases.Length}");
            int blockerCount = 0;
            foreach (var c in canvases)
            {
                bool active = c.gameObject.activeInHierarchy;
                bool hasRaycaster = c.GetComponent<GraphicRaycaster>() != null;
                int sortOrder = c.sortingOrder;
                report.AppendLine($"  - '{c.name}'  active={active}  raycaster={hasRaycaster}  sort={sortOrder}");
                if (active && hasRaycaster && sortOrder > 5000)
                    blockerCount++;
            }
            if (blockerCount > 0)
                report.AppendLine($"⚠ {blockerCount} canvas(es) at sortOrder >5000 — possible overlay blocker");

            // 5. CanvasGroups disabling interaction
            var groups = Object.FindObjectsByType<CanvasGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int blockingGroups = 0;
            foreach (var g in groups)
            {
                if (!g.interactable || !g.blocksRaycasts)
                {
                    report.AppendLine($"⚠ CanvasGroup on '{g.name}' interactable={g.interactable}, blocksRaycasts={g.blocksRaycasts}");
                    blockingGroups++;
                }
            }
            if (blockingGroups == 0) report.AppendLine("✅ All CanvasGroups have interactable+blocksRaycasts on");

            // 6. Sample button check
            var btns = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            report.AppendLine($"\nActive Buttons: {btns.Length}");
            int withListeners = 0;
            foreach (var b in btns)
            {
                if (b.onClick.GetPersistentEventCount() > 0) withListeners++;
            }
            report.AppendLine($"  with persistent listeners: {withListeners}  (runtime AddListener count not visible)");

            string text = report.ToString();
            Debug.Log(text);
            EditorUtility.DisplayDialog("Sparq Diagnosis", text, "OK");
        }
    }
}
