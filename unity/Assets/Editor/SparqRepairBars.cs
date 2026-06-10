using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Hard-repair for XP bar + Volt HP bar wiring.
    /// Scans the scene, finds Slider components, identifies which is which,
    /// attaches/re-wires the controller scripts explicitly via reflection.
    /// </summary>
    public static class SparqRepairBars
    {
        [MenuItem("Sparq/34. REPAIR XP + HP bars (force re-wire)")]
        public static void Repair()
        {
            // Find all Sliders in the scene
            var sliders = Object.FindObjectsByType<Slider>(FindObjectsSortMode.None);
            if (sliders.Length == 0)
            {
                EditorUtility.DisplayDialog("Sparq Repair",
                    "No Slider components in scene. Run Sparq → 11 (upgrade UI) and 12 (rival card) first.", "OK");
                return;
            }

            Slider xpSlider = null;
            Slider hpSlider = null;

            // Heuristic: XP slider is a direct child of a Canvas (root-level bar),
            // HP slider lives inside the rival card (parent chain contains "Rival" or "Card")
            foreach (var s in sliders)
            {
                if (s == null) continue;
                bool insideRival = false;
                Transform t = s.transform;
                while (t != null)
                {
                    if (t.name.Contains("Rival") || t.name.Contains("Volt") || t.name.Contains("HP"))
                    { insideRival = true; break; }
                    t = t.parent;
                }
                if (insideRival) hpSlider = s;
                else xpSlider = s;
            }

            int fixedCount = 0;
            string report = "";

            // Wire XP bar
            if (xpSlider != null)
            {
                WireXPBar(xpSlider);
                report += $"• XP bar: {GetPath(xpSlider.transform)}\n";
                fixedCount++;
            }
            else report += "• XP bar: NOT FOUND\n";

            // Wire HP bar
            if (hpSlider != null)
            {
                WireHPBar(hpSlider);
                report += $"• HP bar: {GetPath(hpSlider.transform)}\n";
                fixedCount++;
            }
            else report += "• HP bar: NOT FOUND\n";

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Repair Bars",
                $"✅ Repaired {fixedCount} bar(s).\n\n" + report +
                "\nHit ▶ Play and tap the bear. Check:\n" +
                "  • Volt's HP drains\n" +
                "  • XP bar fills on quest complete or every 5th tap", "OK");
        }

        private static void WireXPBar(Slider s)
        {
            var root = s.gameObject;
            // Walk up to find the best controller host — prefer a root named FantasyXPBar,
            // otherwise attach to the slider itself.
            Transform cursor = s.transform;
            Transform best = s.transform;
            while (cursor != null)
            {
                if (cursor.name.Contains("XPBar") || cursor.name.Contains("FantasyXP")) { best = cursor; break; }
                cursor = cursor.parent;
            }

            var ctrl = best.GetComponent<Sparq.UI.XPBarDisplay>();
            if (ctrl == null) ctrl = best.gameObject.AddComponent<Sparq.UI.XPBarDisplay>();

            // Explicitly wire via SerializedObject
            var so = new SerializedObject(ctrl);
            so.FindProperty("slider").objectReferenceValue = s;
            if (s.fillRect != null)
            {
                var fillImg = s.fillRect.GetComponent<Image>();
                if (fillImg != null) so.FindProperty("fillImage").objectReferenceValue = fillImg;
            }

            // Find level + XP texts
            TMPro.TMP_Text lvlTmp = null, xpTmp = null;
            foreach (var tmp in best.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (tmp == null) continue;
                var t = tmp.text ?? "";
                if (lvlTmp == null && (t.StartsWith("Lv") || t.Length < 6)) lvlTmp = tmp;
                else if (xpTmp == null && (t.Contains("XP") || t.Contains("/"))) xpTmp = tmp;
            }
            if (lvlTmp != null) so.FindProperty("levelText").objectReferenceValue = lvlTmp;
            if (xpTmp  != null) so.FindProperty("xpText").objectReferenceValue   = xpTmp;
            so.ApplyModifiedProperties();

            s.interactable = false; // display-only, don't eat taps

            Debug.Log($"[Repair] XPBarDisplay on '{best.name}', slider={s.name}, fill={(s.fillRect != null ? s.fillRect.name : "NULL")}");
        }

        private static void WireHPBar(Slider s)
        {
            // The Rival card is the highest ancestor that makes sense for RivalDisplay
            Transform cursor = s.transform;
            Transform best = s.transform;
            while (cursor != null)
            {
                if (cursor.name.Contains("Rival") || cursor.name.Contains("VoltCard") || cursor.name == "RivalCard")
                { best = cursor; break; }
                cursor = cursor.parent;
            }

            var ctrl = best.GetComponent<Sparq.UI.RivalDisplay>();
            if (ctrl == null) ctrl = best.gameObject.AddComponent<Sparq.UI.RivalDisplay>();

            var so = new SerializedObject(ctrl);
            so.FindProperty("hpSlider").objectReferenceValue = s;
            if (s.fillRect != null)
            {
                var img = s.fillRect.GetComponent<Image>();
                if (img != null) so.FindProperty("hpFillImage").objectReferenceValue = img;
            }

            // Find HP / name / title texts
            foreach (var tmp in best.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (tmp == null) continue;
                var t = tmp.text ?? "";
                if (t.Contains("HP") || t.Contains("%")) so.FindProperty("hpText").objectReferenceValue = tmp;
            }
            so.ApplyModifiedProperties();

            s.interactable = false;

            Debug.Log($"[Repair] RivalDisplay on '{best.name}', slider={s.name}, fill={(s.fillRect != null ? s.fillRect.name : "NULL")}");
        }

        private static string GetPath(Transform t)
        {
            if (t == null) return "";
            if (t.parent == null) return t.name;
            return GetPath(t.parent) + "/" + t.name;
        }
    }
}
