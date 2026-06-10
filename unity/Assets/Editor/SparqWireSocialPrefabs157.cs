using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Sparq.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 157: Wire the WORLD top button to a fullscreen tabbed panel that
    /// hosts the Layer Lab social prefabs (Chat / Clan / Ranking / Profile).
    /// Uses PanelToggle + TabGroup MonoBehaviours so references survive Play mode.
    /// </summary>
    public static class SparqWireSocialPrefabs157
    {
        private const string PFX_DIR = "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_DemoScene_Panels/";

        private static readonly Color GOLD     = new Color(1f, 0.82f, 0.32f);
        private static readonly Color CREAM    = new Color(1f, 0.95f, 0.82f);
        private static readonly Color INACT_BG = new Color(1f, 1f, 1f, 0.10f);

        [MenuItem("Sparq/157. Wire WORLD → tabbed prefabs (Chat/Clan/Ranking/Profile)")]
        public static void Apply()
        {
            // Wipe any prior runtime panels
            foreach (var n in new[] { "SocialPanel", "WorldRoot", "PrefabPreviewRoot" })
            {
                var prev = GameObject.Find(n);
                if (prev != null) Object.DestroyImmediate(prev);
            }

            var social = BuildSocialPanel();

            // Wire WORLD button via PanelToggle component (survives Play mode)
            var bar = GameObject.Find("HomeNavButtons");
            if (bar != null)
            {
                Transform world = null;
                for (int i = 0; i < bar.transform.childCount; i++)
                {
                    var c = bar.transform.GetChild(i);
                    if (c.name.ToLower().Contains("world")) { world = c; break; }
                }
                if (world != null)
                {
                    var btn = world.GetComponent<Button>();
                    if (btn != null) btn.onClick.RemoveAllListeners();

                    var toggle = world.GetComponent<PanelToggle>();
                    if (toggle == null) toggle = world.gameObject.AddComponent<PanelToggle>();
                    var so = new SerializedObject(toggle);
                    so.FindProperty("target").objectReferenceValue = social;
                    so.FindProperty("setActiveOnClick").boolValue = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Social panel wired with real prefabs:\n\n" +
                "• Tap WORLD → SocialPanel opens\n" +
                "• 4 tabs: Chat · Clan · Ranking · Profile (real Layer Lab prefabs)\n" +
                "• X button + dim tap close it back to home\n\n" +
                "References use MonoBehaviour serialization → survives Play.\n\n" +
                "Hit ▶ Play.", "OK");
        }

        // ───────── Build the SocialPanel object ─────────
        private static GameObject BuildSocialPanel()
        {
            var root = new GameObject("SocialPanel",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var c = root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 9999;
            var sc = root.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1080, 1920);
            sc.matchWidthOrHeight = 0.5f;
            root.SetActive(false);

            // Dim (close on tap) — uses PanelToggle to set root inactive
            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            dim.transform.SetParent(root.transform, false);
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.88f);
            AttachCloseToggle(dim, root);

            // Top tab bar
            var tabBar = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabBar.transform.SetParent(root.transform, false);
            var trt = tabBar.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1);
            trt.anchoredPosition = new Vector2(0, -16);
            trt.sizeDelta = new Vector2(-180, 76);
            var hlg = tabBar.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // Close X (top-right)
            var close = MakeBtn(root.transform, "Close", "X",
                new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-22, -16), new Vector2(64, 64),
                new Color(0.45f, 0.20f, 0.55f), Color.white, 30);
            AttachCloseToggle(close.gameObject, root);

            // Content area (below tab bar)
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(root.transform, false);
            var conRT = content.GetComponent<RectTransform>();
            conRT.anchorMin = new Vector2(0, 0); conRT.anchorMax = new Vector2(1, 1);
            conRT.offsetMin = new Vector2(0, 0); conRT.offsetMax = new Vector2(0, -100);

            // Spawn the 4 prefabs as tab content (all inactive initially)
            var chat    = SpawnPrefab(content.transform, PFX_DIR + "Chat.prefab",             "Chat");
            var clan    = SpawnPrefab(content.transform, PFX_DIR + "Clan.prefab",             "Clan");
            var ranking = SpawnPrefab(content.transform, PFX_DIR + "Ranking.prefab",          "Ranking");
            var profile = SpawnPrefab(content.transform, PFX_DIR + "Player_Profile_1.prefab", "Profile");

            // Tab buttons
            Button bChat    = MakeTabBtn(tabBar.transform, "TabChat",    "Chat");
            Button bClan    = MakeTabBtn(tabBar.transform, "TabClan",    "Clan");
            Button bRanking = MakeTabBtn(tabBar.transform, "TabRanking", "Ranking");
            Button bProfile = MakeTabBtn(tabBar.transform, "TabProfile", "Profile");

            // Wire TabGroup component
            var tabGroup = root.AddComponent<TabGroup>();
            var tgSO = new SerializedObject(tabGroup);
            var tabsProp = tgSO.FindProperty("tabs");
            tabsProp.arraySize = 4;
            FillTab(tabsProp, 0, bChat,    chat);
            FillTab(tabsProp, 1, bClan,    clan);
            FillTab(tabsProp, 2, bRanking, ranking);
            FillTab(tabsProp, 3, bProfile, profile);
            tgSO.FindProperty("defaultIndex").intValue = 0;
            tgSO.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static void FillTab(SerializedProperty arr, int idx, Button btn, GameObject content)
        {
            var elem = arr.GetArrayElementAtIndex(idx);
            elem.FindPropertyRelative("button").objectReferenceValue   = btn;
            elem.FindPropertyRelative("content").objectReferenceValue = content;
        }

        private static void AttachCloseToggle(GameObject btnGO, GameObject panelRoot)
        {
            var t = btnGO.GetComponent<PanelToggle>();
            if (t == null) t = btnGO.AddComponent<PanelToggle>();
            var so = new SerializedObject(t);
            so.FindProperty("target").objectReferenceValue = panelRoot;
            so.FindProperty("setActiveOnClick").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ───────── helpers ─────────
        private static GameObject SpawnPrefab(Transform parent, string path, string label)
        {
            var pfx = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (pfx == null) return null;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(pfx, parent);
            inst.name = label + "_Tab";
            var rt = inst.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }
            inst.SetActive(false);
            return inst;
        }

        private static Button MakeTabBtn(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = INACT_BG;
            var t = new GameObject("Lbl", typeof(RectTransform));
            t.transform.SetParent(go.transform, false);
            var trt = t.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var tm = t.AddComponent<TextMeshProUGUI>();
            tm.text = label;
            tm.fontSize = 22;
            tm.fontStyle = FontStyles.Bold;
            tm.color = CREAM;
            tm.alignment = TextAlignmentOptions.Center;
            tm.raycastTarget = false;
            return go.GetComponent<Button>();
        }

        private static Button MakeBtn(Transform parent, string name, string label,
            Vector2 amin, Vector2 amax, Vector2 anch, Vector2 sd,
            Color bg, Color fg, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.pivot = new Vector2((amin.x + amax.x) * 0.5f, (amin.y + amax.y) * 0.5f);
            rt.anchoredPosition = anch;
            rt.sizeDelta = sd;
            go.GetComponent<Image>().color = bg;

            var t = new GameObject("Lbl", typeof(RectTransform));
            t.transform.SetParent(go.transform, false);
            var trt = t.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var tm = t.AddComponent<TextMeshProUGUI>();
            tm.text = label;
            tm.fontSize = fontSize;
            tm.fontStyle = FontStyles.Bold;
            tm.color = fg;
            tm.alignment = TextAlignmentOptions.Center;
            tm.raycastTarget = false;
            return go.GetComponent<Button>();
        }
    }
}
