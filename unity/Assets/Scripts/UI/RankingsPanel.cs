// RankingsPanel.cs — global leaderboard view. Single-player local for
// now (no network), so it shows a synthetic roster (RivalRoster names +
// some seeded fakes) sorted by XP, with the real player inserted at
// their actual rank. Wire-up: right-rail trophy hex on the lobby.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    public static class RankingsPanel
    {
        private static readonly Color CARD_BG   = new Color(0.17f, 0.17f, 0.20f, 1f);
        private static readonly Color ROW_BG    = new Color(0.22f, 0.22f, 0.26f, 1f);
        private static readonly Color ROW_PLAYER = new Color(0.96f, 0.66f, 0.10f, 1f);
        private static readonly Color CREAM     = new Color(1f, 0.97f, 0.85f, 1f);
        private static readonly Color INK       = new Color(0.11f, 0.13f, 0.16f, 1f);
        private static readonly Color GOLD      = new Color(0.99f, 0.78f, 0.20f, 1f);
        private static readonly Color SILVER    = new Color(0.78f, 0.82f, 0.86f, 1f);
        private static readonly Color BRONZE    = new Color(0.85f, 0.55f, 0.35f, 1f);

        private const string POPUP_PREFAB = "Assets/Layer Lab/GUI Pro-FantasyRPG/Prefabs/Prefabs_Component_Popups/Popup_01_Basic_White.prefab";

        // Three boards: XP totals, Pet collection score, Mythic eggs found.
        public enum Board { XP, Pets, Eggs }
        private static Board _currentBoard = Board.XP;

        private static GameObject _root;
        private static Transform  _listParent;
        private static Image _tabXpBg, _tabPetsBg, _tabEggsBg;
        private static TMP_Text _tabXpLbl, _tabPetsLbl, _tabEggsLbl;

        public static void Show()
        {
            if (_root != null) { Hide(); return; }
            EnsureEventSystem();

            _root = new GameObject("Sparq_RankingsPanel",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>(); Stretch(rrt);
            var canv = _root.GetComponent<Canvas>();
            canv.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 15000;
            foreach (var other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort)
                    maxSort = other.sortingOrder;
            canv.sortingOrder = maxSort + 20;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            var dim = NewGO("Dim", _root.transform, typeof(Image), typeof(Button));
            Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.08f, 0.92f);
            dim.GetComponent<Button>().onClick.AddListener(Hide);

            // Card shell — try Layer Lab polished prefab, fallback to flat card.
            GameObject card;
            var prefab = LoadLayerLabPrefab(POPUP_PREFAB);
            if (prefab != null)
            {
                var inst = UnityEngine.Object.Instantiate(prefab, _root.transform);
                inst.name = "Card";
                card = inst;
                var crt = inst.GetComponent<RectTransform>() ?? inst.AddComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = Vector2.zero;
                crt.sizeDelta = new Vector2(940, 1500);
                foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                {
                    if (t == null) continue;
                    var n = t.gameObject.name;
                    if (n == "Text_Info" || n == "Button_OK" || n == "Content_Demo")
                        t.gameObject.SetActive(false);
                }
                foreach (var tmp in inst.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp != null && tmp.gameObject.name == "Text_Title")
                    {
                        tmp.text = "Rankings";
                        tmp.fontSize = 64;
                        tmp.alignment = TextAlignmentOptions.MidlineLeft;
                        tmp.color = CREAM;
                        try { tmp.outlineWidth = 0.18f; tmp.outlineColor = new Color(0.05f, 0.03f, 0.10f); } catch {}
                    }
                }
                foreach (var img in inst.GetComponentsInChildren<Image>(true))
                    if (img != null && img.gameObject.name == "Bg") img.color = CARD_BG;
            }
            else
            {
                card = NewGO("Card", _root.transform, typeof(Image));
                var crt = card.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = Vector2.zero;
                crt.sizeDelta = new Vector2(940, 1500);
                card.GetComponent<Image>().color = CARD_BG;
                var fbT = MakeText(card.transform, "Title", "Rankings", 64, FontStyles.Bold, CREAM);
                var fbR = fbT.rectTransform;
                fbR.anchorMin = new Vector2(0, 1); fbR.anchorMax = new Vector2(1, 1);
                fbR.pivot = new Vector2(0.5f, 1);
                fbR.offsetMin = new Vector2(48, -140); fbR.offsetMax = new Vector2(-48, -40);
                fbT.alignment = TextAlignmentOptions.MidlineLeft;
            }

            // Tab strip — XP / Pets / Eggs
            BuildTabStrip(card.transform);

            // Back chevron top-right
            var back = NewGO("Back", card.transform, typeof(Image), typeof(Button));
            var bRT = back.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(1, 1); bRT.anchorMax = new Vector2(1, 1);
            bRT.pivot = new Vector2(1, 1);
            bRT.anchoredPosition = new Vector2(-30, -30);
            bRT.sizeDelta = new Vector2(96, 96);
            back.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            back.GetComponent<Image>().raycastTarget = true;
            var bBtn = back.GetComponent<Button>();
            bBtn.targetGraphic = back.GetComponent<Image>(); bBtn.interactable = true;
            var bLbl = MakeText(back.transform, "L", "<", 56, FontStyles.Bold, CREAM);
            Stretch(bLbl.rectTransform); bLbl.alignment = TextAlignmentOptions.Center;
            bBtn.onClick.AddListener(Hide);

            BuildLeaderboard(card.transform);
            Debug.Log("[RankingsPanel] Opened.");
        }

        public static void Hide()
        {
            if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
        }

        // ─────────────────────────────────────────────────────────────────
        // LEADERBOARD
        // ─────────────────────────────────────────────────────────────────

        private class Entry { public string name; public int score; public bool isPlayer; }

        private static void BuildTabStrip(Transform card)
        {
            var strip = NewGO("TabStrip", card, typeof(HorizontalLayoutGroup));
            var rt = strip.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -160);
            rt.sizeDelta = new Vector2(-80, 90);
            var hlg = strip.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true; hlg.childControlHeight = true;

            (_tabXpBg,   _tabXpLbl)   = BuildTab(strip.transform, "XP",     Board.XP);
            (_tabPetsBg, _tabPetsLbl) = BuildTab(strip.transform, "Pets",   Board.Pets);
            (_tabEggsBg, _tabEggsLbl) = BuildTab(strip.transform, "Eggs",   Board.Eggs);
            ApplyTabStyles();
        }

        private static (Image bg, TMP_Text lbl) BuildTab(Transform parent, string label, Board b)
        {
            var go = NewGO("Tab_" + b, parent, typeof(Image), typeof(Button));
            var img = go.GetComponent<Image>();
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img; btn.interactable = true;
            btn.onClick.AddListener(() => SetBoard(b));
            var lbl = MakeText(go.transform, "L", label, 30, FontStyles.Bold, CREAM);
            Stretch(lbl.rectTransform); lbl.alignment = TextAlignmentOptions.Center;
            return (img, lbl);
        }

        private static void SetBoard(Board b)
        {
            if (_currentBoard == b) return;
            _currentBoard = b;
            ApplyTabStyles();
            // Rebuild list contents in-place
            if (_listParent != null)
                for (int i = _listParent.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.Destroy(_listParent.GetChild(i).gameObject);
            FillBoard(_listParent);
        }

        private static void ApplyTabStyles()
        {
            void Style(Image bg, TMP_Text lbl, bool on)
            {
                if (bg  != null) bg.color  = on ? GOLD : new Color(0.24f, 0.24f, 0.30f, 1f);
                if (lbl != null) lbl.color = on ? INK : CREAM;
            }
            Style(_tabXpBg,   _tabXpLbl,   _currentBoard == Board.XP);
            Style(_tabPetsBg, _tabPetsLbl, _currentBoard == Board.Pets);
            Style(_tabEggsBg, _tabEggsLbl, _currentBoard == Board.Eggs);
        }

        private static void BuildLeaderboard(Transform card)
        {
            // Build the scroll list shell once — content is rebuilt per tab.
            var scrollGO = NewGO("Scroll", card, typeof(Image), typeof(ScrollRect));
            var sRT = scrollGO.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 1);
            sRT.offsetMin = new Vector2(40, 110); sRT.offsetMax = new Vector2(-40, -270);
            scrollGO.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var sr = scrollGO.GetComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 30f;

            var vp = NewGO("VP", scrollGO.transform, typeof(Image), typeof(RectMask2D));
            Stretch(vp.GetComponent<RectTransform>());
            vp.GetComponent<Image>().color = new Color(0, 0, 0, 0);

            var content = NewGO("Content", vp.transform,
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var ctRT = content.GetComponent<RectTransform>();
            ctRT.anchorMin = new Vector2(0, 1); ctRT.anchorMax = new Vector2(1, 1);
            ctRT.pivot = new Vector2(0.5f, 1);
            ctRT.anchoredPosition = Vector2.zero;
            ctRT.sizeDelta = new Vector2(0, ctRT.sizeDelta.y);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            var fit = content.GetComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = vp.GetComponent<RectTransform>();
            sr.content = ctRT;
            _listParent = content.transform;

            FillBoard(_listParent);
        }

        // Populate the current board's rows. Called on Show() and on
        // every SetBoard() tab switch.
        private static void FillBoard(Transform listParent)
        {
            if (listParent == null) return;

            string playerName = "You";
            int playerScore = 0;
            try
            {
                var d = Sparq.Core.SaveService.Data;
                if (!string.IsNullOrEmpty(d?.playerName)) playerName = d.playerName;
                playerScore = ComputePlayerScore(d, _currentBoard);
            }
            catch {}

            var roster = SeedRoster(_currentBoard);
            roster.Add(new Entry { name = playerName, score = playerScore, isPlayer = true });
            roster.Sort((a, b) => b.score.CompareTo(a.score));

            int playerRank = -1;
            for (int i = 0; i < roster.Count; i++) if (roster[i].isPlayer) { playerRank = i + 1; break; }

            int show = Mathf.Min(10, roster.Count);
            for (int i = 0; i < show; i++)
                BuildRow(listParent, i + 1, roster[i]);

            if (playerRank > 10)
            {
                var dot = MakeText(listParent, "Dot", "•  •  •",
                    36, FontStyles.Bold, new Color(0.6f, 0.6f, 0.68f, 1f));
                var dle = dot.gameObject.AddComponent<LayoutElement>();
                dle.preferredHeight = 36;
                dot.alignment = TextAlignmentOptions.Center;
                BuildRow(listParent, playerRank, roster[playerRank - 1]);
            }
        }

        private static int ComputePlayerScore(Sparq.Core.PlayerData d, Board b)
        {
            if (d == null) return 0;
            switch (b)
            {
                case Board.XP:
                    return d.totalXP;
                case Board.Pets:
                    // pet_score = roster_size × 100 + sum(pet_level × 10) + careStreakDays
                    int petScore = 0;
                    try
                    {
                        var roster = Sparq.Systems.PetService.Roster();
                        petScore += roster.Count * 100;
                        foreach (var p in roster) petScore += p.level * 10;
                    }
                    catch {}
                    petScore += d.petCareStreakDays;
                    return petScore;
                case Board.Eggs:
                    return d.mythicEggsFound?.Count ?? 0;
            }
            return 0;
        }

        // Seed competitor rosters per board — synthetic players with
        // plausible score curves so the leaderboard feels populated.
        private static List<Entry> SeedRoster(Board b)
        {
            switch (b)
            {
                case Board.XP:
                    return new List<Entry> {
                        new Entry { name = "Aria",  score = 9420 },
                        new Entry { name = "Slym",  score = 8760 },
                        new Entry { name = "Plip",  score = 8210 },
                        new Entry { name = "Will",  score = 7400 },
                        new Entry { name = "Pecky", score = 6650 },
                        new Entry { name = "Bram",  score = 5800 },
                        new Entry { name = "Echo",  score = 4900 },
                        new Entry { name = "Lila",  score = 4120 },
                        new Entry { name = "Mochi", score = 3500 },
                        new Entry { name = "Kade",  score = 2980 },
                        new Entry { name = "Juno",  score = 2100 },
                        new Entry { name = "Veil",  score = 1450 },
                        new Entry { name = "Tally", score = 980  },
                        new Entry { name = "Rook",  score = 520  },
                    };
                case Board.Pets:
                    // 3 pets × 100 = 300 baseline; top players have higher-level pets
                    return new List<Entry> {
                        new Entry { name = "Aria",  score = 720 },   // 3 pets, level avg ~12
                        new Entry { name = "Mochi", score = 640 },
                        new Entry { name = "Kade",  score = 580 },
                        new Entry { name = "Bram",  score = 510 },
                        new Entry { name = "Pecky", score = 460 },
                        new Entry { name = "Echo",  score = 410 },
                        new Entry { name = "Slym",  score = 360 },
                        new Entry { name = "Plip",  score = 320 },
                        new Entry { name = "Lila",  score = 280 },
                        new Entry { name = "Juno",  score = 240 },
                        new Entry { name = "Will",  score = 200 },
                        new Entry { name = "Veil",  score = 160 },
                        new Entry { name = "Tally", score = 120 },
                        new Entry { name = "Rook",  score = 80  },
                    };
                case Board.Eggs:
                    // Max 3 (the hidden mythic stages) — tight clustering at the top
                    return new List<Entry> {
                        new Entry { name = "Aria",  score = 3 },
                        new Entry { name = "Mochi", score = 3 },
                        new Entry { name = "Bram",  score = 2 },
                        new Entry { name = "Pecky", score = 2 },
                        new Entry { name = "Echo",  score = 2 },
                        new Entry { name = "Slym",  score = 1 },
                        new Entry { name = "Plip",  score = 1 },
                        new Entry { name = "Kade",  score = 1 },
                        new Entry { name = "Lila",  score = 1 },
                        new Entry { name = "Juno",  score = 0 },
                        new Entry { name = "Will",  score = 0 },
                        new Entry { name = "Veil",  score = 0 },
                        new Entry { name = "Tally", score = 0 },
                        new Entry { name = "Rook",  score = 0 },
                    };
            }
            return new List<Entry>();
        }

        private static void BuildRow(Transform parent, int rank, Entry e)
        {
            var row = NewGO("Row_" + rank, parent, typeof(Image), typeof(LayoutElement));
            row.GetComponent<LayoutElement>().preferredHeight = 110;
            row.GetComponent<Image>().color = e.isPlayer ? ROW_PLAYER : ROW_BG;

            // Rank badge (1-3 get medal-coloured circles)
            var rankBg = NewGO("RankBg", row.transform, typeof(Image));
            var rkRT = rankBg.GetComponent<RectTransform>();
            rkRT.anchorMin = new Vector2(0, 0.5f); rkRT.anchorMax = new Vector2(0, 0.5f);
            rkRT.pivot = new Vector2(0, 0.5f);
            rkRT.anchoredPosition = new Vector2(14, 0);
            rkRT.sizeDelta = new Vector2(76, 76);
            Color rankColor = rank == 1 ? GOLD : rank == 2 ? SILVER : rank == 3 ? BRONZE
                              : new Color(0.30f, 0.30f, 0.35f, 1f);
            rankBg.GetComponent<Image>().color = rankColor;
            rankBg.GetComponent<Image>().raycastTarget = false;
            var rkTxt = MakeText(rankBg.transform, "L", rank.ToString(),
                36, FontStyles.Bold, rank <= 3 ? INK : CREAM);
            Stretch(rkTxt.rectTransform); rkTxt.alignment = TextAlignmentOptions.Center;

            // Name
            var nm = MakeText(row.transform, "N", e.name,
                34, FontStyles.Bold, e.isPlayer ? INK : CREAM);
            var nRT = nm.rectTransform;
            nRT.anchorMin = new Vector2(0, 0); nRT.anchorMax = new Vector2(1, 1);
            nRT.offsetMin = new Vector2(108, 0); nRT.offsetMax = new Vector2(-220, 0);
            nm.alignment = TextAlignmentOptions.MidlineLeft;
            nm.textWrappingMode = TextWrappingModes.NoWrap;
            nm.overflowMode = TextOverflowModes.Ellipsis;
            try { if (e.isPlayer) { nm.outlineWidth = 0; } else { nm.outlineWidth = 0.18f; nm.outlineColor = new Color(0, 0, 0, 0.7f); } } catch {}

            // Score — format depends on the active board.
            string scoreText;
            switch (_currentBoard)
            {
                case Board.XP:    scoreText = $"{e.score:N0} XP"; break;
                case Board.Pets:  scoreText = $"{e.score} pts";   break;
                case Board.Eggs:  scoreText = e.score == 1 ? "1 egg" : $"{e.score} eggs"; break;
                default:          scoreText = e.score.ToString(); break;
            }
            var xp = MakeText(row.transform, "Score", scoreText,
                30, FontStyles.Bold, e.isPlayer ? INK : GOLD);
            var xRT = xp.rectTransform;
            xRT.anchorMin = new Vector2(1, 0); xRT.anchorMax = new Vector2(1, 1);
            xRT.pivot = new Vector2(1, 0.5f);
            xRT.anchoredPosition = new Vector2(-20, 0);
            xRT.sizeDelta = new Vector2(220, 0);
            xp.alignment = TextAlignmentOptions.MidlineRight;
        }

        // ─────────────────────────────────────────────────────────────────
        // PRIMITIVES
        // ─────────────────────────────────────────────────────────────────

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static GameObject NewGO(string name, Transform parent, params System.Type[] comps)
        {
            var go = new GameObject(name, new System.Type[] { typeof(RectTransform) });
            go.transform.SetParent(parent, false);
            foreach (var c in comps) go.AddComponent(c);
            return go;
        }

        private static TMP_Text MakeText(Transform parent, string name, string text,
            float size, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text; tm.fontSize = size; tm.fontStyle = style; tm.color = color;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            return tm;
        }

        private static GameObject LoadLayerLabPrefab(string path)
        {
            // Try Resources first (works in APK + Editor). Strip "Assets/" prefix
            // and ".prefab" suffix to get Resources-relative path.
            string r = path;
            if (r.StartsWith("Assets/")) r = r.Substring(7);
            if (r.EndsWith(".prefab")) r = r.Substring(0, r.Length - 7);
            var go = Resources.Load<GameObject>(r);
            if (go != null) return go;
#if UNITY_EDITOR
            try { return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path); } catch {}
#endif
            return null;
        }

        private static void EnsureEventSystem()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (existing != null && existing.isActiveAndEnabled) return;
            var go = existing != null ? existing.gameObject : new GameObject("EventSystem");
            if (existing == null)
            {
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();   // Old Input Manager only — Input System package not installed.
            }
            go.SetActive(true);
            var es = go.GetComponent<EventSystem>();
            if (es != null) es.enabled = true;
        }
    }
}
