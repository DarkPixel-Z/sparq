using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Lists all players the user has blocked. Each row has an "Unblock"
    /// button that removes the player from BlockList. "Clear All" at bottom.
    /// Opened from SettingsPanel → SAFETY → Blocked Players.
    /// </summary>
    public static class BlockedPlayersPanel
    {
        private static GameObject _root;
        private static Transform  _listParent;
        private static TMP_Text   _emptyLabel;

        public static void Show()
        {
            if (_root != null) Object.Destroy(_root);
            EnsureEventSystem();

            _root = new GameObject("Sparq_BlockedPlayersPanel",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var canv = _root.GetComponent<Canvas>();
            canv.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 15000;
            foreach (var other in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort)
                    maxSort = other.sortingOrder;
            canv.sortingOrder = maxSort + 25;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Dim backdrop
            var dim = NewGO("Dim", _root.transform, typeof(Image), typeof(Button));
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);
            dim.GetComponent<Button>().onClick.AddListener(Hide);

            // Card
            var card = NewGO("Card", _root.transform, typeof(Image));
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(900, 1400);
            card.GetComponent<Image>().color = new Color(0.16f, 0.10f, 0.28f, 1f);

            // Title bar
            var titleBar = NewGO("TitleBar", card.transform, typeof(Image));
            var tbRT = titleBar.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0, 1); tbRT.anchorMax = new Vector2(1, 1);
            tbRT.pivot = new Vector2(0.5f, 1);
            tbRT.anchoredPosition = Vector2.zero;
            tbRT.sizeDelta = new Vector2(0, 130);
            titleBar.GetComponent<Image>().color = new Color(0.55f, 0.40f, 0.85f, 1f);
            var titleTm = MakeText(titleBar.transform, "Title", "BLOCKED PLAYERS",
                48, FontStyles.Bold, new Color(1f, 0.95f, 0.55f));
            var titleRT = titleTm.rectTransform;
            titleRT.anchorMin = Vector2.zero; titleRT.anchorMax = Vector2.one;
            titleRT.offsetMin = new Vector2(20, 0); titleRT.offsetMax = new Vector2(-120, 0);
            titleTm.alignment = TextAlignmentOptions.Center;

            // Close X
            var closeBtn = NewGO("CloseBtn", card.transform, typeof(Image), typeof(Button));
            var cbRT = closeBtn.GetComponent<RectTransform>();
            cbRT.anchorMin = new Vector2(1, 1); cbRT.anchorMax = new Vector2(1, 1);
            cbRT.pivot = new Vector2(1, 1);
            cbRT.anchoredPosition = new Vector2(-25, -25);
            cbRT.sizeDelta = new Vector2(85, 85);
            closeBtn.GetComponent<Image>().color = new Color(0.85f, 0.25f, 0.25f, 1f);
            var xLbl = MakeText(closeBtn.transform, "X", "X",
                48, FontStyles.Bold, Color.white);
            var xRT = xLbl.rectTransform;
            xRT.anchorMin = Vector2.zero; xRT.anchorMax = Vector2.one;
            xRT.offsetMin = Vector2.zero; xRT.offsetMax = Vector2.zero;
            xLbl.alignment = TextAlignmentOptions.Center;
            closeBtn.GetComponent<Button>().onClick.AddListener(Hide);

            // Scrollable list — viewport at top, "Clear All" pinned at bottom
            var scrollGO = NewGO("Scroll", card.transform,
                typeof(Image), typeof(ScrollRect));
            var srRT = scrollGO.GetComponent<RectTransform>();
            srRT.anchorMin = new Vector2(0, 0); srRT.anchorMax = new Vector2(1, 1);
            srRT.offsetMin = new Vector2(40, 180); srRT.offsetMax = new Vector2(-40, -160);
            scrollGO.GetComponent<Image>().color = new Color(0.06f, 0.04f, 0.12f, 0.85f);
            var sr = scrollGO.GetComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;

            // Viewport (with mask)
            var viewport = NewGO("Viewport", scrollGO.transform,
                typeof(Image), typeof(RectMask2D));
            var vpRT = viewport.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = new Vector2(10, 10); vpRT.offsetMax = new Vector2(-10, -10);
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0); // invisible

            // Content (VerticalLayoutGroup)
            var content = NewGO("Content", viewport.transform,
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var ctRT = content.GetComponent<RectTransform>();
            ctRT.anchorMin = new Vector2(0, 1); ctRT.anchorMax = new Vector2(1, 1);
            ctRT.pivot = new Vector2(0.5f, 1);
            ctRT.anchoredPosition = Vector2.zero;
            ctRT.sizeDelta = new Vector2(0, 0);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.spacing = 12;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = vpRT;
            sr.content  = ctRT;

            _listParent = content.transform;

            // Empty-state label (overlay over scroll viewport)
            _emptyLabel = MakeText(scrollGO.transform, "Empty",
                "No blocked players.",
                32, FontStyles.Italic, new Color(0.70f, 0.65f, 0.85f));
            var elRT = _emptyLabel.rectTransform;
            elRT.anchorMin = Vector2.zero; elRT.anchorMax = Vector2.one;
            elRT.offsetMin = Vector2.zero; elRT.offsetMax = Vector2.zero;
            _emptyLabel.alignment = TextAlignmentOptions.Center;

            // Clear All button (bottom)
            var clearBtn = NewGO("ClearAll", card.transform, typeof(Image), typeof(Button));
            var clRT = clearBtn.GetComponent<RectTransform>();
            clRT.anchorMin = new Vector2(0.5f, 0); clRT.anchorMax = new Vector2(0.5f, 0);
            clRT.pivot = new Vector2(0.5f, 0);
            clRT.anchoredPosition = new Vector2(0, 40);
            clRT.sizeDelta = new Vector2(560, 100);
            clearBtn.GetComponent<Image>().color = new Color(0.85f, 0.30f, 0.30f, 1f);
            var clLbl = MakeText(clearBtn.transform, "Lbl", "Unblock All",
                34, FontStyles.Bold, Color.white);
            var cllRT = clLbl.rectTransform;
            cllRT.anchorMin = Vector2.zero; cllRT.anchorMax = Vector2.one;
            cllRT.offsetMin = Vector2.zero; cllRT.offsetMax = Vector2.zero;
            clLbl.alignment = TextAlignmentOptions.Center;
            clearBtn.GetComponent<Button>().onClick.AddListener(() => {
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                Sparq.Safety.BlockList.ClearAll();
                RefreshList();
            });

            RefreshList();

            Debug.Log("[BlockedPlayersPanel] Opened.");
        }

        public static void Hide()
        {
            if (_root != null) { Object.Destroy(_root); _root = null; }
            _listParent = null;
            _emptyLabel = null;
            // Return to settings
            try { SettingsPanel.Show(); } catch {}
        }

        // ─────────────────────────────────────────────────────────────────
        // LIST
        // ─────────────────────────────────────────────────────────────────

        private static void RefreshList()
        {
            if (_listParent == null) return;
            for (int i = _listParent.childCount - 1; i >= 0; i--)
                Object.Destroy(_listParent.GetChild(i).gameObject);

            List<string> all = null;
            try { all = Sparq.Safety.BlockList.All(); } catch { all = new List<string>(); }

            if (_emptyLabel != null) _emptyLabel.gameObject.SetActive(all.Count == 0);

            foreach (var name in all) BuildRow(name);
        }

        private static void BuildRow(string playerName)
        {
            var row = NewGO("Row_" + playerName, _listParent, typeof(Image), typeof(LayoutElement));
            row.GetComponent<LayoutElement>().preferredHeight = 110;
            row.GetComponent<Image>().color = new Color(0.20f, 0.14f, 0.36f, 1f);

            var nm = MakeText(row.transform, "Name", playerName,
                32, FontStyles.Bold, Color.white);
            var nmRT = nm.rectTransform;
            nmRT.anchorMin = new Vector2(0, 0); nmRT.anchorMax = new Vector2(0.65f, 1);
            nmRT.offsetMin = new Vector2(24, 0); nmRT.offsetMax = Vector2.zero;
            nm.alignment = TextAlignmentOptions.MidlineLeft;

            var unblock = NewGO("Unblock", row.transform, typeof(Image), typeof(Button));
            var ubRT = unblock.GetComponent<RectTransform>();
            ubRT.anchorMin = new Vector2(1, 0.5f); ubRT.anchorMax = new Vector2(1, 0.5f);
            ubRT.pivot = new Vector2(1, 0.5f);
            ubRT.anchoredPosition = new Vector2(-20, 0);
            ubRT.sizeDelta = new Vector2(220, 76);
            unblock.GetComponent<Image>().color = new Color(1f, 0.78f, 0.22f, 1f);
            var ubLbl = MakeText(unblock.transform, "Lbl", "Unblock",
                28, FontStyles.Bold, new Color(0.10f, 0.05f, 0.18f));
            var ublRT = ubLbl.rectTransform;
            ublRT.anchorMin = Vector2.zero; ublRT.anchorMax = Vector2.one;
            ublRT.offsetMin = Vector2.zero; ublRT.offsetMax = Vector2.zero;
            ubLbl.alignment = TextAlignmentOptions.Center;

            string capturedName = playerName;
            unblock.GetComponent<Button>().onClick.AddListener(() => {
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                Sparq.Safety.BlockList.Unblock(capturedName);
                RefreshList();
            });
        }

        // ─────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────

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
            tm.text = text;
            tm.fontSize = size;
            tm.fontStyle = style;
            tm.color = color;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            return tm;
        }

        private static void EnsureEventSystem()
        {
            var existing = Object.FindFirstObjectByType<EventSystem>();
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
