using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqQuestRecolor
    {
        [MenuItem("Sparq/95. Quest box → yellow + title accent")]
        public static void Apply()
        {
            var ql = Object.FindAnyObjectByType<Sparq.UI.QuestListUI>();
            if (ql == null)
            {
                EditorUtility.DisplayDialog("Sparq", "Quest list not found.", "OK");
                return;
            }

            var go = ql.gameObject;

            // Background → warm parchment yellow
            var bg = go.GetComponent<Image>();
            if (bg == null) bg = go.AddComponent<Image>();
            bg.color = new Color(0.98f, 0.85f, 0.30f, 0.95f); // bright golden yellow

            // Find + style the "Today's Quests" title
            foreach (var tmp in go.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null) continue;
                if (tmp.text != null && tmp.text.Contains("Today"))
                {
                    tmp.text = "Today's Quests";
                    tmp.fontSize = 22;
                    tmp.color = new Color(0.45f, 0.10f, 0.40f);  // deep magenta accent
                    tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
                }
            }

            // Make quest row backgrounds darker + readable on yellow bg
            foreach (Transform row in go.transform)
            {
                if (row == null) continue;
                if (row.name.StartsWith("QuestRow"))
                {
                    var img = row.GetComponent<Image>();
                    if (img != null)
                    {
                        // Distinguish done vs not-done a bit
                        bool done = false;
                        foreach (var tmp in row.GetComponentsInChildren<TMP_Text>(true))
                        {
                            if (tmp != null && (tmp.fontStyle & FontStyles.Strikethrough) != 0)
                            { done = true; break; }
                        }
                        img.color = done
                            ? new Color(0.20f, 0.45f, 0.25f, 0.95f)   // dark green for done
                            : new Color(0.30f, 0.18f, 0.45f, 0.92f); // dark purple for active
                    }
                    // Quest name text → bright cream so it pops on dark row
                    foreach (var tmp in row.GetComponentsInChildren<TMP_Text>(true))
                    {
                        if (tmp == null) continue;
                        if (tmp.text != null && (tmp.text.Contains(" XP") || tmp.text.Length < 4)) continue;
                        tmp.color = new Color(1f, 0.98f, 0.85f);
                    }
                }
                if (row.name == "AddQuestRow")
                {
                    var img = row.GetComponent<Image>();
                    if (img != null) img.color = new Color(0.10f, 0.55f, 0.30f, 0.95f); // emerald green
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Quest box recolored:\n\n" +
                "• Background: bright golden yellow\n" +
                "• Title: deep magenta italic bold\n" +
                "• Quest rows: dark purple (active) / dark green (done)\n" +
                "• Quest text: bright cream\n" +
                "• 'Add quest' row: emerald green\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
