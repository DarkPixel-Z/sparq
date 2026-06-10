using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// One-click scene setup tool. Appears in the "Sparq" top menu.
    /// Reimports art with correct settings + spawns Karu + builds XP bar UI.
    /// </summary>
    public static class SparqSetup
    {
        private const string MENU_ROOT = "Sparq/";

        // ──────────────────────────────────────────────────────────────────────
        // Step 1: Reimport all art as pixel-art sprites (correct import settings)
        // ──────────────────────────────────────────────────────────────────────
        [MenuItem(MENU_ROOT + "1. Fix Sprite Import Settings")]
        public static void FixSpriteImports()
        {
            string[] folders = {
                "Assets/Art/Characters",
                "Assets/Art/Enemies",
                "Assets/Art/UI",
                "Assets/Art/Icons",
                "Assets/Art/Items",
            };

            int total = 0;
            foreach (var folder in folders)
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    if (importer == null) continue;

                    importer.textureType       = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 16;
                    importer.filterMode        = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled     = false;
                    importer.SaveAndReimport();
                    total++;
                }
            }
            Debug.Log($"[Sparq Setup] Fixed {total} sprites across {folders.Length} folders.");
            EditorUtility.DisplayDialog("Sparq Setup",
                $"Reimported {total} sprites as pixel-art Sprites (2D and UI).\nYou can now drag them into scenes.",
                "OK");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Step 2: Spawn Karu + XP bar UI in the currently open scene
        // ──────────────────────────────────────────────────────────────────────
        [MenuItem(MENU_ROOT + "2. Build Home Scene")]
        public static void BuildHomeScene()
        {
            // Make sure we have a 2D-friendly camera
            var cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null)
            {
                var camGO = new GameObject("Main Camera");
                cam = camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
                camGO.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            ColorUtility.TryParseHtmlString("#1A0830", out var bg);
            cam.backgroundColor = bg;
            cam.transform.position = new Vector3(0, 0, -10);

            // Game Manager
            var gm = GameObject.Find("GameManager");
            if (gm == null) gm = new GameObject("GameManager");
            var gmComp = gm.GetComponent<Sparq.Core.GameManager>();
            if (gmComp == null) gm.AddComponent<Sparq.Core.GameManager>();

            // Karu — use tile_0085 if it exists, otherwise pick first character sprite
            var karuSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/tile_0085.png");
            if (karuSprite == null)
            {
                var guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Art/Characters" });
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    karuSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }

            var karu = GameObject.Find("Karu");
            if (karu == null) karu = new GameObject("Karu");
            karu.transform.position = new Vector3(0, 0, 0);
            karu.transform.localScale = new Vector3(3, 3, 1);

            var sr = karu.GetComponent<SpriteRenderer>();
            if (sr == null) sr = karu.AddComponent<SpriteRenderer>();
            if (karuSprite != null) sr.sprite = karuSprite;
            sr.sortingOrder = 1;

            var petDisp = karu.GetComponent<Sparq.UI.PetDisplay>();
            if (petDisp == null) petDisp = karu.AddComponent<Sparq.UI.PetDisplay>();
            // Assign sprites via reflection because fields are private SerializeField
            var t = typeof(Sparq.UI.PetDisplay);
            var karuField = t.GetField("karuSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            karuField?.SetValue(petDisp, karuSprite);

            if (karu.GetComponent<BoxCollider2D>() == null)
                karu.AddComponent<BoxCollider2D>();

            // Canvas for UI
            var canvasGO = GameObject.Find("UI Canvas");
            Canvas canvas;
            if (canvasGO == null)
            {
                canvasGO = new GameObject("UI Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
                canvasGO.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvas = canvasGO.GetComponent<Canvas>();
            }

            // EventSystem for UI clicks
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // XP Bar container
            var barContainer = GameObject.Find("XPBarContainer");
            if (barContainer == null)
            {
                barContainer = new GameObject("XPBarContainer");
                barContainer.transform.SetParent(canvas.transform, false);
                var rt = barContainer.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0, 80);
                rt.sizeDelta = new Vector2(800, 120);
            }

            // Bar Background
            var bgGO = GameObject.Find("XPBarBG");
            if (bgGO == null)
            {
                bgGO = new GameObject("XPBarBG");
                bgGO.transform.SetParent(barContainer.transform, false);
                var bgRT = bgGO.AddComponent<RectTransform>();
                bgRT.anchorMin = new Vector2(0, 0);
                bgRT.anchorMax = new Vector2(1, 0);
                bgRT.pivot = new Vector2(0.5f, 0);
                bgRT.sizeDelta = new Vector2(0, 30);
                bgRT.anchoredPosition = new Vector2(0, 0);
                var bgImg = bgGO.AddComponent<Image>();
                ColorUtility.TryParseHtmlString("#1A0F3D", out var bgCol);
                bgImg.color = bgCol;
            }

            // Bar Fill
            var fillGO = GameObject.Find("XPBarFill");
            Image fillImg;
            if (fillGO == null)
            {
                fillGO = new GameObject("XPBarFill");
                fillGO.transform.SetParent(bgGO.transform, false);
                var fillRT = fillGO.AddComponent<RectTransform>();
                fillRT.anchorMin = Vector2.zero;
                fillRT.anchorMax = Vector2.one;
                fillRT.offsetMin = Vector2.zero;
                fillRT.offsetMax = Vector2.zero;
                fillImg = fillGO.AddComponent<Image>();
                ColorUtility.TryParseHtmlString("#FF6A00", out var fillCol);
                fillImg.color = fillCol;
                fillImg.type = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Horizontal;
                fillImg.fillAmount = 0.3f;
            }
            else
            {
                fillImg = fillGO.GetComponent<Image>();
            }

            // Level Text
            var lvlGO = GameObject.Find("LevelText");
            TMP_Text lvlText;
            if (lvlGO == null)
            {
                lvlGO = new GameObject("LevelText");
                lvlGO.transform.SetParent(barContainer.transform, false);
                var lvlRT = lvlGO.AddComponent<RectTransform>();
                lvlRT.anchorMin = new Vector2(0, 1);
                lvlRT.anchorMax = new Vector2(0, 1);
                lvlRT.pivot = new Vector2(0, 1);
                lvlRT.anchoredPosition = new Vector2(10, -5);
                lvlRT.sizeDelta = new Vector2(200, 40);
                lvlText = lvlGO.AddComponent<TextMeshProUGUI>();
                lvlText.text = "Lv.1";
                lvlText.fontSize = 32;
                lvlText.color = Color.white;
                lvlText.fontStyle = FontStyles.Bold;
            }
            else
            {
                lvlText = lvlGO.GetComponent<TMP_Text>();
            }

            // XP Text
            var xpGO = GameObject.Find("XPText");
            TMP_Text xpText;
            if (xpGO == null)
            {
                xpGO = new GameObject("XPText");
                xpGO.transform.SetParent(barContainer.transform, false);
                var xpRT = xpGO.AddComponent<RectTransform>();
                xpRT.anchorMin = new Vector2(1, 1);
                xpRT.anchorMax = new Vector2(1, 1);
                xpRT.pivot = new Vector2(1, 1);
                xpRT.anchoredPosition = new Vector2(-10, -5);
                xpRT.sizeDelta = new Vector2(300, 40);
                xpText = xpGO.AddComponent<TextMeshProUGUI>();
                xpText.text = "0 / 100 XP";
                xpText.fontSize = 28;
                xpText.color = new Color(0.9f, 0.85f, 1f);
                xpText.alignment = TextAlignmentOptions.Right;
            }
            else
            {
                xpText = xpGO.GetComponent<TMP_Text>();
            }

            // XPBarDisplay controller
            var xpBarCtrl = barContainer.GetComponent<Sparq.UI.XPBarDisplay>();
            if (xpBarCtrl == null) xpBarCtrl = barContainer.AddComponent<Sparq.UI.XPBarDisplay>();

            // Wire up via reflection (fields are private SerializeField)
            var xpCtrlType = typeof(Sparq.UI.XPBarDisplay);
            var fillField = xpCtrlType.GetField("fillImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lvlField  = xpCtrlType.GetField("levelText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var xpField   = xpCtrlType.GetField("xpText",    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fillField?.SetValue(xpBarCtrl, fillImg);
            lvlField?.SetValue(xpBarCtrl, lvlText);
            xpField?.SetValue(xpBarCtrl, xpText);

            EditorUtility.SetDirty(karu);
            EditorUtility.SetDirty(canvasGO);
            EditorUtility.SetDirty(barContainer);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[Sparq Setup] Home scene built: Karu + XP bar + Game Manager.");
            EditorUtility.DisplayDialog("Sparq Setup",
                "✅ Home scene is ready!\n\n• Karu is in the scene center\n• XP bar at bottom\n• GameManager is running\n\nHit ▶ Play to see it alive.",
                "Let's go!");
        }

        [MenuItem(MENU_ROOT + "Do EVERYTHING (1 + 2)")]
        public static void DoEverything()
        {
            FixSpriteImports();
            BuildHomeScene();
        }

        // ──────────────────────────────────────────────────────────────────────
        // Force-assign Karu sprite (use this if Karu is invisible)
        // ──────────────────────────────────────────────────────────────────────
        [MenuItem(MENU_ROOT + "3. Fix Karu Sprite")]
        public static void FixKaruSprite()
        {
            // Force re-import tile_0085 as a proper single sprite
            string path = "Assets/Art/Characters/tile_0085.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                importer.textureType         = TextureImporterType.Sprite;
                importer.spriteImportMode    = SpriteImportMode.Single; // force SINGLE not Multiple
                importer.spritePixelsPerUnit = 16;
                importer.filterMode          = FilterMode.Point;
                importer.textureCompression  = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled       = false;
                importer.SaveAndReimport();
            }

            // Also force all Character sprites to Single mode (import script might have left them Multiple)
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Characters" });
            foreach (var guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (TextureImporter)AssetImporter.GetAtPath(p);
                if (imp == null) continue;
                if (imp.spriteImportMode != SpriteImportMode.Single)
                {
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.SaveAndReimport();
                }
            }
            AssetDatabase.Refresh();

            // Now load the sprite (after reimport, it's actually a Sprite asset)
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                EditorUtility.DisplayDialog("Sparq Setup", "Still couldn't load tile_0085 as a Sprite. Try Sparq → 1. Fix Sprite Import Settings first.", "OK");
                return;
            }

            // Find Karu and assign
            var karu = GameObject.Find("Karu");
            if (karu == null)
            {
                EditorUtility.DisplayDialog("Sparq Setup", "No 'Karu' GameObject in the current scene.\nRun Sparq → 2. Build Home Scene first.", "OK");
                return;
            }

            var sr = karu.GetComponent<SpriteRenderer>();
            if (sr == null) sr = karu.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 10;

            // Also assign to PetDisplay
            var pd = karu.GetComponent<Sparq.UI.PetDisplay>();
            if (pd != null)
            {
                var t = typeof(Sparq.UI.PetDisplay);
                var karuField = t.GetField("karuSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                karuField?.SetValue(pd, sprite);
            }

            // Make sure Karu is big enough to see
            karu.transform.localScale = new Vector3(10f, 10f, 1f);
            karu.transform.position = new Vector3(0f, 1.5f, 0f);

            EditorUtility.SetDirty(karu);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"[Sparq] Assigned sprite {sprite.name} to Karu. Scale=10, Y=1.5.");
            EditorUtility.DisplayDialog("Sparq Setup", "✅ Fixed! Karu now has tile_0085 as sprite, scaled 10x, positioned above center.\n\nHit ▶ Play to see.", "OK");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Fix Karu's collider so tapping works
        // ──────────────────────────────────────────────────────────────────────
        [MenuItem(MENU_ROOT + "4. Fix Karu Tap")]
        public static void FixKaruTap()
        {
            var karu = GameObject.Find("Karu");
            if (karu == null)
            {
                EditorUtility.DisplayDialog("Sparq Setup", "No Karu in scene.", "OK");
                return;
            }

            // Remove old tiny collider
            var oldCol = karu.GetComponent<BoxCollider2D>();
            if (oldCol != null) Object.DestroyImmediate(oldCol);

            // Add a fresh one — now the sprite IS assigned, so it'll auto-size to sprite bounds
            var col = karu.AddComponent<BoxCollider2D>();
            // Force correct size (the sprite is 16x16 pixels at 16 PPU = 1x1 world unit, pre-scale)
            col.size = new Vector2(1f, 1f);
            col.offset = Vector2.zero;

            // Ensure camera has Physics2DRaycaster so clicks work through UI
            var cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null && cam.GetComponent<UnityEngine.EventSystems.Physics2DRaycaster>() == null)
            {
                cam.gameObject.AddComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
            }

            EditorUtility.SetDirty(karu);
            if (cam != null) EditorUtility.SetDirty(cam.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[Sparq] Karu collider resized to 1x1. Physics2DRaycaster added to camera.");
            EditorUtility.DisplayDialog("Sparq Setup", "✅ Fixed tap detection!\n\nHit ▶ Play and click Karu — should log [Pet] messages in Console.", "OK");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Replace pixel placeholder with the REAL Karu SVG (matches WebView)
        // ──────────────────────────────────────────────────────────────────────
        [MenuItem(MENU_ROOT + "5. Use Real Karu (SVG)")]
        public static void UseRealKaru()
        {
            // The SVGs were copied to Assets/Art/Sparq/
            string karuPath  = "Assets/Art/Sparq/red-panda.svg";
            string mochiPath = "Assets/Art/Sparq/mochi.svg";
            string unaPath   = "Assets/Art/Sparq/una.svg";

            // Force re-import so Vector Graphics package processes them
            AssetDatabase.ImportAsset(karuPath,  ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(mochiPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(unaPath,   ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            var karuSprite  = AssetDatabase.LoadAssetAtPath<Sprite>(karuPath);
            var mochiSprite = AssetDatabase.LoadAssetAtPath<Sprite>(mochiPath);

            if (karuSprite == null)
            {
                EditorUtility.DisplayDialog("Sparq Setup",
                    "Couldn't load red-panda.svg as a Sprite.\n\n" +
                    "Unity's Vector Graphics package may still be installing.\n" +
                    "Wait 30 seconds for package resolution, then try again.\n\n" +
                    "If it persists, check Window → Package Manager for 'Vector Graphics'.", "OK");
                return;
            }

            var karu = GameObject.Find("Karu");
            if (karu == null)
            {
                EditorUtility.DisplayDialog("Sparq Setup", "Run 'Build Home Scene' first — no Karu in scene.", "OK");
                return;
            }

            // Swap sprite
            var sr = karu.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = karuSprite;

            // Assign both Karu + Mochi sprites to PetDisplay
            var pd = karu.GetComponent<Sparq.UI.PetDisplay>();
            if (pd != null)
            {
                var t = typeof(Sparq.UI.PetDisplay);
                var karuField  = t.GetField("karuSprite",  System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var mochiField = t.GetField("mochiSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                karuField?.SetValue(pd, karuSprite);
                if (mochiSprite != null) mochiField?.SetValue(pd, mochiSprite);
            }

            // SVGs render at larger native size — reduce scale
            karu.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            karu.transform.position   = new Vector3(0f, 1f, 0f);

            EditorUtility.SetDirty(karu);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[Sparq] Swapped Karu to real SVG red-panda.");
            EditorUtility.DisplayDialog("Sparq Setup",
                "✅ Karu is now the real red panda from your WebView!\n\n" +
                "Scale reduced to 1.5 (SVGs import at 100 PPU by default).\n" +
                "Hit ▶ Play to see.", "OK");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Batch-import the 7 core asset packs from the Asset Store cache
        // ──────────────────────────────────────────────────────────────────────
        [MenuItem(MENU_ROOT + "6. Import All 7 Core Assets")]
        public static void ImportAllCore()
        {
            string appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            string root = System.IO.Path.Combine(appData, "Unity", "Asset Store-5.x");

            // Order matters — lighter packs first so early compile errors don't block later ones.
            string[] relPaths = new[]
            {
                @"LAYERLAB\Textures MaterialsIcons UI\2D Icons - Reward Chest Pack.unitypackage",
                @"MiMU STUDIO\Textures MaterialsIcons UI\2D Potion Icon Pack.unitypackage",
                @"RavenmoreArt\Textures MaterialsIcons UI\Painterly Fantasy Icon Pack.unitypackage",
                @"Alien Nude LTD\Textures Materials2D Isometric Tiles\Free Asset - 2D Handcrafted Art.unitypackage",
                @"Luma Forge\Textures Materials2D Characters\2D Fantasy Monster Sprite Pack.unitypackage",
                @"LAYERLAB\Textures MaterialsGUI Skins\GUI Pro - Fantasy Hero.unitypackage",
                @"More Mountains\ScriptingEffects\Feel.unitypackage",
            };

            int imported = 0, missing = 0;
            System.Text.StringBuilder missingLog = new System.Text.StringBuilder();

            foreach (var rel in relPaths)
            {
                string full = System.IO.Path.Combine(root, rel);
                if (!System.IO.File.Exists(full))
                {
                    missing++;
                    missingLog.AppendLine("  • " + rel);
                    Debug.LogWarning("[Sparq] Not found: " + full);
                    continue;
                }
                Debug.Log("[Sparq] Importing: " + System.IO.Path.GetFileName(full));
                // interactiveMode=false means no dialog; Unity imports silently
                AssetDatabase.ImportPackage(full, interactive: false);
                imported++;
            }

            AssetDatabase.Refresh();
            string msg = $"Triggered import on {imported} packages.\n";
            if (missing > 0) msg += $"\nMissing ({missing}):\n{missingLog}";
            msg += "\nUnity is now processing imports in the background — watch the bottom-right progress bar.\nThis can take 5-15 minutes for Feel + GUI Pro (large packs).\n\nOnce quiet, you'll see new folders in Assets/.";
            EditorUtility.DisplayDialog("Sparq Import", msg, "OK");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Swap Karu from Kenney knight to adorable Playful-Pup from Fantasy Monsters
        // ──────────────────────────────────────────────────────────────────────
        [MenuItem(MENU_ROOT + "7. Use Playful-Pup Karu")]
        public static void UsePlayfulPupKaru()
        {
            string pupPath = "Assets/2D Fantasy Monster Sprite Pack/Monsters/Pup/Playful-Pup.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pupPath);
            if (sprite == null)
            {
                // Fallback — maybe import type isn't set to Sprite yet
                var importer = (TextureImporter)AssetImporter.GetAtPath(pupPath);
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                    AssetDatabase.Refresh();
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pupPath);
                }
            }
            if (sprite == null)
            {
                EditorUtility.DisplayDialog("Sparq", "Couldn't find Playful-Pup.png — is the Fantasy Monster pack imported?", "OK");
                return;
            }

            var karu = GameObject.Find("Karu");
            if (karu == null) { EditorUtility.DisplayDialog("Sparq", "No Karu in scene.", "OK"); return; }

            var sr = karu.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = sprite;

            var pd = karu.GetComponent<Sparq.UI.PetDisplay>();
            if (pd != null)
            {
                var karuField = typeof(Sparq.UI.PetDisplay).GetField("karuSprite",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                karuField?.SetValue(pd, sprite);
            }

            // Pup is a much larger native sprite than Kenney tiles — scale way down
            karu.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            karu.transform.position   = new Vector3(0f, 1f, 0f);

            EditorUtility.SetDirty(karu);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[Sparq] Karu is now Playful-Pup.");
            EditorUtility.DisplayDialog("Sparq", "✅ Karu is now the Playful-Pup!\n\nHit ▶ Play to see the cute version.", "OK");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Use the SVG characters (Karu, Mochi, Una, Volt)
        // ──────────────────────────────────────────────────────────────────────
        [MenuItem(MENU_ROOT + "8. Use Real SVG Karu + Spawn Una")]
        public static void UseSvgCharacters()
        {
            // Force SVG Importer to generate Sprites (not UI Toolkit Vector Images)
            string[] paths = {
                "Assets/Art/Sparq/red-panda.svg",
                "Assets/Art/Sparq/mochi.svg",
                "Assets/Art/Sparq/una.svg",
                "Assets/Art/Sparq/fitch.svg"
            };
            foreach (var p in paths)
            {
                var importer = AssetImporter.GetAtPath(p);
                if (importer == null) continue;
                // Use reflection to flip the "Generated Asset Type" field on SVGImporter
                var svgImporterType = importer.GetType();
                var generatedTypeField = svgImporterType.GetField("SvgType",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (generatedTypeField != null)
                {
                    // SVGType enum: 0 = VectorSprite, 1 = TexturedSprite, 2 = UIToolkitImage, 3 = UISVGImage
                    // We want VectorSprite (0) for world-space SpriteRenderer
                    generatedTypeField.SetValue(importer, 0);
                }
                AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
            }
            AssetDatabase.Refresh();

            // Some Vector Graphics imports make the sprite a SUB-asset of the SVGImporter root,
            // so LoadAssetAtPath<Sprite> returns null. Use LoadAllAssetsAtPath to find the sprite.
            Sprite LoadSvgSprite(string path)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) return sprite;
                // Try sub-assets
                var all = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var obj in all)
                {
                    if (obj is Sprite s) return s;
                }
                return null;
            }

            var karuSprite  = LoadSvgSprite("Assets/Art/Sparq/red-panda.svg");
            var mochiSprite = LoadSvgSprite("Assets/Art/Sparq/mochi.svg");
            var unaSprite   = LoadSvgSprite("Assets/Art/Sparq/una.svg");

            if (karuSprite == null)
            {
                string msg = "Couldn't load red-panda.svg as a Sprite.\n\n";
                msg += "Try: Select red-panda.svg in Project panel, then in Inspector:\n";
                msg += "• Top dropdown: change 'Default' to 'SVG Importer' (if present)\n";
                msg += "• Click Apply\n\n";
                msg += "Or manually drag red-panda.svg onto the Karu GameObject's Sprite field in Inspector.";
                EditorUtility.DisplayDialog("Sparq", msg, "OK");
                return;
            }

            // ── Swap Karu ────────────────────────────────────────
            var karu = GameObject.Find("Karu");
            if (karu != null)
            {
                var sr = karu.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = karuSprite;
                var pd = karu.GetComponent<Sparq.UI.PetDisplay>();
                if (pd != null)
                {
                    var t = typeof(Sparq.UI.PetDisplay);
                    t.GetField("karuSprite",  System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(pd, karuSprite);
                    if (mochiSprite != null)
                        t.GetField("mochiSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(pd, mochiSprite);
                }
                // SVGs import at 100 PPU — reduce scale so Karu isn't giant
                karu.transform.localScale = new Vector3(1f, 1f, 1f);
                karu.transform.position   = new Vector3(0f, 1f, 0f);
                EditorUtility.SetDirty(karu);
            }

            // ── Spawn Una (the guide, off to the side) ──────────
            var una = GameObject.Find("Una");
            if (una == null)
            {
                una = new GameObject("Una");
            }
            una.transform.position = new Vector3(-2.5f, -1f, 0f);
            una.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            var unaSR = una.GetComponent<SpriteRenderer>();
            if (unaSR == null) unaSR = una.AddComponent<SpriteRenderer>();
            if (unaSprite != null) unaSR.sprite = unaSprite;
            unaSR.sortingOrder = 5;
            EditorUtility.SetDirty(una);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[Sparq] Swapped to SVG characters. Karu=red-panda, Una spawned.");
            EditorUtility.DisplayDialog("Sparq",
                "✅ Done!\n\n" +
                "• Karu is now your real red panda SVG\n" +
                "• Una the axolotl is in the bottom-left corner\n" +
                "• Mochi sprite also assigned (for future swap)\n\n" +
                "Hit ▶ Play to see them.", "OK");
        }
    }
}
