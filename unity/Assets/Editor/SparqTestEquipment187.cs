using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>Menu 187: Force-open the Equipment panel + grant test loot.</summary>
    public static class SparqTestEquipment187
    {
        [MenuItem("Sparq/187. Open Equipment panel")]
        public static void Open()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "Hit ▶ Play first, then run this menu while playing.\n\n(Or tap the BAG button at the top of the home screen.)", "OK");
                return;
            }
            Sparq.UI.EquipmentPanel.Show();
        }

        [MenuItem("Sparq/187a. Grant 5 random loot drops")]
        public static void Grant5()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Sparq", "Run while in Play mode.", "OK"); return;
            }
            for (int i = 0; i < 5; i++)
            {
                var item = Sparq.Systems.EquipmentService.RollLoot(3);
                Sparq.Systems.EquipmentService.Grant(item.id);
            }
            EditorUtility.DisplayDialog("Sparq", "Granted 5 random items.\nOpen BAG to see them.", "OK");
        }

        [MenuItem("Sparq/187b. Grant Legendary set (test high-tier)")]
        public static void GrantLegend()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Sparq", "Run while in Play mode.", "OK"); return;
            }
            string[] ids = { "sword_legend", "helm_crowned", "chest_dragon", "boots_giant", "ring_dragon" };
            foreach (var id in ids) Sparq.Systems.EquipmentService.Grant(id);
            Sparq.Systems.EquipmentService.EquipBest();
            EditorUtility.DisplayDialog("Sparq", "Legendary set granted + auto-equipped.\nOpen BAG to see, then fight to feel the power.", "OK");
        }

        [MenuItem("Sparq/187c. Reset inventory")]
        public static void Reset()
        {
            PlayerPrefs.DeleteKey("sparq.equip.owned");
            PlayerPrefs.DeleteKey("sparq.equip.equipped");
            PlayerPrefs.Save();
            EditorUtility.DisplayDialog("Sparq", "Inventory reset.\nNext open will give you starter cloth set.", "OK");
        }
    }
}
