#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sparq.EditorTools
{
    /// <summary>
    /// One-time editor pass that imports every Layer Lab PictoIcon and ItemIcon
    /// texture as a Sprite. Without this, LoadAssetAtPath&lt;Sprite&gt; returns
    /// null at runtime and icons (boots, scrolls, locks…) don't render.
    /// Runs once per editor session via SessionState marker.
    /// </summary>
    [InitializeOnLoad]
    public static class SparqLayerLabSpritePrepper
    {
        private const string MARKER = "sparq.layerlab-sprites-prepped.v15";

        private static readonly string[] FOLDERS =
        {
            "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_PictoIcons/256",
            "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/256",
            "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Demo/Demo_IconMisc",
            "Assets/FantasyIconPack/256",
            // Phase 2 packs — Battle of Heroes, Azure Wisp, BATTY BUDDIES
            "Assets/BattleOfHeroes/Characters",
            "Assets/BattleOfHeroes/UI/Png",
            "Assets/BattleOfHeroes/Backgrounds/PNG",
            "Assets/BattleOfHeroes/Animations",
            "Assets/Azure Will-o’-Wisp",
            "Assets/ArtApex Studio/BATTY BUDDIES",
            // Phase 3 packs — FantasyMaps + character + boss roster
            "Assets/FantasyMaps/_PNG",
            "Assets/FantasyKnight/_PNG",
            "Assets/Knight",
            "Assets/Dragon/_png",
            "Assets/FantasyCyclopsSprites",
            "Assets/FantasyEntCharacter",
            "Assets/FantasyVikingSprite",
            "Assets/FantasySellerSprite",
            "Assets/MedievalGameInterface",
            "Assets/FantasyRogueArmor",
            // Phase 4 — animated text effects + level map dedicated pack
            "Assets/AnimatedTextGame/PNG",
            "Assets/LevelMapAssets/Png",
            // Phase 5 — VFX packs (smoke / wind / lightning / shield)
            "Assets/CartoonSmokeFX",
            "Assets/WindLightningFX",
            "Assets/MagicShieldFX",
            // Phase 6 — debuff icons + samurai hero + belt icons + mythology bosses
            "Assets/DebuffSkillIcons",
            "Assets/FantasySamurai/_PNG",
            "Assets/BeltGameIcons",
            "Assets/MythologyBosses",
            // Phase 7 — rings/jewelry icons + Medusa boss
            "Assets/RingsJewelryIcons/PNG/without_shadow",
            "Assets/MedusaChibi",
            // Phase 8 — bulk chibi heroes
            "Assets/ElfArcherChibi",
            "Assets/NinjaAssassinChibi",
            "Assets/FrostKnightChibi",
            "Assets/AmazonChibi",
            "Assets/PaladinChibi",
            "Assets/SamuraiChibi",
            "Assets/MercenariesChibi",
            "Assets/WomenChibi",
            "Assets/PersianWarriorChibi",
            "Assets/ElementalChibi",
            "Assets/MimicChibi",
            // Phase 8 — boss + monster packs
            "Assets/BossRockEarthIce",
            "Assets/TopDownBoss4Dir",
            "Assets/TopDownMonsters",
            "Assets/MiniMonster6",
            "Assets/MonsterV5",
            // Phase 8 — tilesets
            "Assets/TileSummer",
            "Assets/TileAutumn",
            "Assets/TileWinter",
            "Assets/TileDesert",
            "Assets/TilePoisonSwamp",
            "Assets/MapLevel2D",
            "Assets/LevelMapGameAssets2",
            "Assets/TopDownDungeonTileset",
            // Phase 8 — icon packs
            "Assets/HelmetIcons48",
            "Assets/MagicPotionIcons",
            "Assets/MagicGemIcons",
            "Assets/LootIconsPack",
            "Assets/CurrencyLootIcons",
            "Assets/AlchemyHerbIcons",
            "Assets/BuffSkillIcons",
            "Assets/ArcherSkills",
            "Assets/SwordsmanSkills",
            "Assets/ThiefSkillsIcon",
            "Assets/JewelerSkillsIcon",
            "Assets/LootVectorIcon",
            "Assets/ArmorBracer",
            "Assets/ArmorSabaton",
            "Assets/CrossbowBow",
            "Assets/CoinCrystalShop",
            "Assets/FantasyIconRogueArmorPack",
            // Phase 8 — animations
            "Assets/WinLoseAnimateAssetPack",
            "Assets/HeroCharacterTeamSprites",
            "Assets/MainCharacterTeamUpSprites",
            "Assets/AdventureGameSprite",
            "Assets/AdventureGameSprite2",
            "Assets/CartoonPenguins",
            // Phase 9 — more chibi heroes + gear pieces
            "Assets/PirateChibi",
            "Assets/KingDefenderChibi",
            "Assets/ThiefRogueChibi",
            "Assets/BanditChibi",
            "Assets/TimeKeeperChibi",
            "Assets/RpgGemIcons",
            "Assets/TileTropicalCity",
            "Assets/GearCuirass",
            "Assets/GearFairyWings",
            "Assets/GearFantasyPants",
            "Assets/GearMagicGems",
            "Assets/GearSigil",
        };

        static SparqLayerLabSpritePrepper()
        {
            if (SessionState.GetBool(MARKER, false)) return;
            EditorApplication.delayCall += Prep;
        }

        private static void Prep()
        {
            int reimported = 0;
            foreach (var folder in FOLDERS)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp == null) continue;
                    bool needsFix =
                        imp.textureType        != TextureImporterType.Sprite ||
                        !imp.alphaIsTransparency ||
                        imp.spriteImportMode   != SpriteImportMode.Single;
                    if (!needsFix) continue;
                    imp.textureType      = TextureImporterType.Sprite;
                    imp.alphaIsTransparency = true;
                    imp.spriteImportMode = SpriteImportMode.Single;   // critical — Multiple makes LoadAssetAtPath<Sprite> return null
                    imp.SaveAndReimport();
                    reimported++;
                }
            }
            if (reimported > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[SparqLayerLabSpritePrepper] Reimported {reimported} Layer Lab textures as Sprites.");
            }
            SessionState.SetBool(MARKER, true);
        }
    }
}
#endif
