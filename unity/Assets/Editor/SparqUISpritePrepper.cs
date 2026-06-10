using UnityEditor;
using UnityEngine;

namespace Sparq.Editor
{
    /// <summary>
    /// Runs once on script reload to ensure all Layer Lab UI sprites we use at
    /// runtime have proper 9-slice borders baked into their importer settings.
    /// This avoids any SaveAndReimport calls during Play mode (which trigger
    /// domain reloads and kill running coroutines / animations).
    /// </summary>
    [InitializeOnLoad]
    public static class SparqUISpritePrepper
    {
        private const string MARKER_KEY = "Sparq.SpritePrepper.v1";

        // (asset path, border vector)
        private static readonly (string path, Vector4 border)[] ASSETS = new[]
        {
            // Buttons used by BattleScene + QuestsPanel
            ("Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_01_Mian_l_Bg_Yellow.png", new Vector4(40, 40, 40, 40)),
            ("Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_01_Mian_l_Bg_Red.png",    new Vector4(40, 40, 40, 40)),
            ("Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_01_Mian_l_Bg_Sky.png",    new Vector4(40, 40, 40, 40)),
            ("Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_01_Mian_l_Bg_Green.png",  new Vector4(40, 40, 40, 40)),
            ("Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_01_Mian_l_Bg_Gary.png",   new Vector4(40, 40, 40, 40)),
            ("Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_01_Mian_l_Bg_Blue.png",   new Vector4(40, 40, 40, 40)),
            ("Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_01_Mian_l_Bg_Brown.png",  new Vector4(40, 40, 40, 40)),

            // Popup frames used by QuestsPanel
            ("Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Popup/Popup_Box_Bg.png",       new Vector4(80, 80, 80, 80)),
            ("Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Popup/Popup_Box_Bg_Top.png",   new Vector4(60, 40, 60, 40)),
            ("Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Popup/Popup_Box_Deco_Bg.png",  new Vector4(40, 40, 40, 40)),
        };

        static SparqUISpritePrepper()
        {
            // Run once per project session — also re-runs after asset import/reset.
            if (SessionState.GetBool(MARKER_KEY, false)) return;
            SessionState.SetBool(MARKER_KEY, true);
            EditorApplication.delayCall += PrepareAll;
        }

        [MenuItem("Sparq/197. Prep UI Sprites Now")]
        public static void PrepareAllManual() => PrepareAll();

        private static void PrepareAll()
        {
            int prepared = 0;
            foreach (var (path, border) in ASSETS)
            {
                if (PrepareOne(path, border)) prepared++;
            }
            if (prepared > 0)
                Debug.Log($"[Sparq] Prepared {prepared} UI sprites for runtime use (9-slice borders + Sprite type).");
        }

        private static bool PrepareOne(string path, Vector4 border)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return false;
            bool changed = false;
            if (imp.textureType != TextureImporterType.Sprite)
            { imp.textureType = TextureImporterType.Sprite; changed = true; }
            if (!imp.alphaIsTransparency)
            { imp.alphaIsTransparency = true; changed = true; }
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            if (settings.spriteBorder == Vector4.zero)
            {
                settings.spriteBorder = border;
                imp.SetTextureSettings(settings);
                changed = true;
            }
            if (changed) imp.SaveAndReimport();
            return changed;
        }
    }
}
