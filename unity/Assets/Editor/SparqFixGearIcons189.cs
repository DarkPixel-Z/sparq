using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 189: Force-import every gear icon used by EquipmentService as Sprite.
    /// Fixes white-box icons in the inventory.
    /// </summary>
    public static class SparqFixGearIcons189
    {
        private const string FH_ICON  = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/256/ItemIcon_";
        private const string FH_PICTO = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_PictoIcons/256/PictoIcon_";

        private static readonly string[] PATHS = new[]
        {
            FH_ICON  + "Gear_Sword.png",
            FH_ICON  + "Gear_Bow.png",
            FH_ICON  + "Gear_Hammer.png",
            FH_ICON  + "Gear_Helmet.png",
            FH_ICON  + "Gear_Armor.png",
            FH_ICON  + "Gear_Ring.png",
            FH_ICON  + "Gear_Shield_Metal.png",
            FH_ICON  + "Crown_1.png",
            FH_ICON  + "Crown_2.png",
            FH_PICTO + "Boots.Png",
            FH_PICTO + "Boots.png",
        };

        [MenuItem("Sparq/189. Force-import all gear icons as Sprite")]
        public static void Apply()
        {
            int fixedCount = 0, missing = 0;
            foreach (var path in PATHS)
            {
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) { missing++; continue; }
                bool changed = false;
                if (imp.textureType != TextureImporterType.Sprite)
                { imp.textureType = TextureImporterType.Sprite; changed = true; }
                if (imp.spriteImportMode != SpriteImportMode.Single)
                { imp.spriteImportMode = SpriteImportMode.Single; changed = true; }
                if (!imp.alphaIsTransparency)
                { imp.alphaIsTransparency = true; changed = true; }
                if (changed)
                {
                    imp.SaveAndReimport();
                    fixedCount++;
                }
            }
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Sparq",
                $"✅ Force-imported {fixedCount} icon(s) as Sprite.\n• {missing} path(s) not found (may not exist)\n\nReopen BAG to see icons render.", "OK");
        }
    }
}
