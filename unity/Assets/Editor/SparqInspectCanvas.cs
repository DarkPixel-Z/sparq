using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Text;

namespace Sparq.Editor
{
    public static class SparqInspectCanvas
    {
        [MenuItem("Sparq/23. DEBUG - List Canvas children")]
        public static void Inspect()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Canvas children ===\n");

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                sb.AppendLine($"\n[Canvas] '{canvas.name}' renderMode={canvas.renderMode} sortOrder={canvas.sortingOrder}");
                Walk(canvas.transform, 1, sb);
            }

            // Also list Sliders + count
            int sliderCount = 0;
            foreach (var s in Object.FindObjectsByType<Slider>(FindObjectsSortMode.None))
            {
                sliderCount++;
                sb.AppendLine($"  [Slider] {GetPath(s.transform)}  value={s.value:F2}");
            }
            sb.AppendLine($"\nTotal Sliders in scene: {sliderCount}");

            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("Sparq Inspect",
                $"Logged canvas tree to Console.\n\nTotal Sliders found: {sliderCount}\n\nOpen Console (Ctrl+Shift+C) and copy the output to Claude.",
                "OK");
        }

        private static void Walk(Transform t, int depth, StringBuilder sb)
        {
            if (t == null) return;
            string indent = new string(' ', depth * 2);
            string typeHint = "";
            if (t.GetComponent<Slider>() != null) typeHint += " <Slider>";
            if (t.GetComponent<Image>() != null) typeHint += " <Image>";
            if (t.GetComponent<TMPro.TMP_Text>() != null)
                typeHint += $" <Text:'{t.GetComponent<TMPro.TMP_Text>().text}'>";
            sb.AppendLine($"{indent}- {t.name}{typeHint}");
            for (int i = 0; i < t.childCount; i++)
                Walk(t.GetChild(i), depth + 1, sb);
        }

        private static string GetPath(Transform t)
        {
            if (t == null) return "";
            if (t.parent == null) return t.name;
            return GetPath(t.parent) + "/" + t.name;
        }
    }
}
