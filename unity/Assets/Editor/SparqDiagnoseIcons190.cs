using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 190: Diagnose every catalog icon — shows which paths exist,
    /// which load as sprite, which don't.
    /// </summary>
    public static class SparqDiagnoseIcons190
    {
        private const string FH_ICON  = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/256/ItemIcon_";
        private const string FH_PICTO = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_PictoIcons/256/PictoIcon_";

        [MenuItem("Sparq/190. Diagnose icon loading per catalog item")]
        public static void Apply()
        {
            var catalog = Sparq.Systems.EquipmentService.Catalog;
            var sb = new StringBuilder();
            sb.AppendLine("Per-item icon load report:\n");

            int ok = 0, fail = 0;
            var seen = new HashSet<string>();

            foreach (var item in catalog)
            {
                if (seen.Contains(item.iconPath)) continue;
                seen.Add(item.iconPath);

                string[] candidates = item.iconPath.StartsWith("PICTO_")
                    ? new[] {
                        FH_PICTO + item.iconPath.Substring(6) + ".Png",
                        FH_PICTO + item.iconPath.Substring(6) + ".png",
                      }
                    : new[] {
                        FH_ICON + item.iconPath + ".png",
                        FH_ICON + item.iconPath + ".Png",
                      };

                bool loaded = false;
                string usedPath = "";
                foreach (var p in candidates)
                {
                    var sp = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                    if (sp != null) { loaded = true; usedPath = p; break; }
                }

                if (loaded) { sb.Append("✓  "); ok++; }
                else        { sb.Append("✗  "); fail++; }
                sb.AppendLine($"{item.iconPath,-20} → {(loaded ? usedPath : "NOT FOUND")}");
            }

            sb.AppendLine($"\n{ok} OK, {fail} failed.");
            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("Sparq Icon Diagnose",
                $"{ok} icons load OK\n{fail} icons FAILED\n\nFull report in Console (open Window → General → Console).", "OK");
        }
    }
}
