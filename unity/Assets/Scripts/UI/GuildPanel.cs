// GuildPanel.cs — minimal Guild landing screen. Shows the player's
// current guild (or an empty state) plus quick actions for browsing or
// creating one. Real guild membership / chat / members live in
// WorldPanel's Guilds + Guild Chat tabs — this panel is the focused
// "what's my guild" landing the right-rail hex opens.
//
// Visual: instantiated Layer Lab Popup_01_Basic_White.prefab as the
// polished card shell (same pattern RemindPanel uses), tinted charcoal,
// with a cream title and an orange "BROWSE GUILDS" CTA.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    public static class GuildPanel
    {
        private static readonly Color CARD_BG   = new Color(0.17f, 0.17f, 0.20f, 1f);
        private static readonly Color CREAM     = new Color(1f, 0.97f, 0.85f, 1f);
        private static readonly Color INK       = new Color(0.11f, 0.13f, 0.16f, 1f);
        private static readonly Color INK_SOFT  = new Color(0.65f, 0.65f, 0.72f, 1f);
        private static readonly Color BTN_ORANGE = new Color(1.00f, 0.55f, 0.15f, 1f);
        private static readonly Color BTN_GREEN  = new Color(0.40f, 0.85f, 0.55f, 1f);
        private static readonly Color BTN_GREY   = new Color(0.45f, 0.45f, 0.50f, 1f);

        private const string POPUP_PREFAB = "Assets/Layer Lab/GUI Pro-FantasyRPG/Prefabs/Prefabs_Component_Popups/Popup_01_Basic_White.prefab";

        private static GameObject _root;
        private static Transform  _cardTransform;
        private static Transform  _body;

        public static void Show()
        {
            if (_root != null) { Hide(); return; }
            EnsureEventSystem();

            _root = new GameObject("Sparq_GuildPanel",
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

            // Dim — tap to close
            var dim = NewGO("Dim", _root.transform, typeof(Image), typeof(Button));
            Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.08f, 0.92f);
            dim.GetComponent<Button>().onClick.AddListener(Hide);

            // Card shell — instantiate the polished Layer Lab popup prefab.
            // Falls back to a flat card if the prefab can't load (build-mode).
            GameObject card;
            var popupPrefab = LoadLayerLabPrefab(POPUP_PREFAB);
            if (popupPrefab != null)
            {
                var inst = UnityEngine.Object.Instantiate(popupPrefab, _root.transform);
                inst.name = "Card";
                card = inst;
                var crt = inst.GetComponent<RectTransform>() ?? inst.AddComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = Vector2.zero;
                crt.sizeDelta = new Vector2(940, 1240);

                // Hide unused prefab children, retype title, tint bg.
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
                        tmp.text = "Guild";
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
                crt.sizeDelta = new Vector2(940, 1240);
                card.GetComponent<Image>().color = CARD_BG;
                var fbTitle = MakeText(card.transform, "Title", "Guild", 64, FontStyles.Bold, CREAM);
                var fbRT = fbTitle.rectTransform;
                fbRT.anchorMin = new Vector2(0, 1); fbRT.anchorMax = new Vector2(1, 1);
                fbRT.pivot = new Vector2(0.5f, 1);
                fbRT.offsetMin = new Vector2(48, -140); fbRT.offsetMax = new Vector2(-48, -40);
                fbTitle.alignment = TextAlignmentOptions.MidlineLeft;
            }

            _cardTransform = card.transform;
            BuildBody();

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

            Debug.Log("[GuildPanel] Opened.");
        }

        public static void Hide()
        {
            if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
        }

        // ─────────────────────────────────────────────────────────────────
        // BODY
        // ─────────────────────────────────────────────────────────────────

        // Top-level body builder — chooses between empty state, joined
        // state, and the create-guild form based on PlayerData.guildName.
        private static void BuildBody()
        {
            if (_cardTransform == null) return;
            // Tear down any previous body content before rebuilding.
            if (_body != null) UnityEngine.Object.Destroy(_body.gameObject);
            var bodyGO = NewGO("Body", _cardTransform, typeof(RectTransform));
            var bRT = bodyGO.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0, 0); bRT.anchorMax = new Vector2(1, 1);
            bRT.offsetMin = new Vector2(0, 0); bRT.offsetMax = new Vector2(0, -160);
            _body = bodyGO.transform;

            string current = "";
            try { current = Sparq.Core.SaveService.Data?.guildName ?? ""; } catch {}

            if (string.IsNullOrEmpty(current)) BuildEmptyState(_body);
            else                                BuildJoinedState(_body, current);
        }

        // ── Empty state: not in a guild ───────────────────────────────────
        private static void BuildEmptyState(Transform body)
        {
            var hdr = MakeText(body, "Hdr", "You're not in a guild yet", 50, FontStyles.Bold, CREAM);
            var hRT = hdr.rectTransform;
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.offsetMin = new Vector2(40, -180); hRT.offsetMax = new Vector2(-40, -80);
            hdr.alignment = TextAlignmentOptions.Center;
            hdr.textWrappingMode = TextWrappingModes.Normal;

            // Bigger, brighter sub-text so it's actually readable.
            var sub = MakeText(body, "Sub",
                "Team up with other players, share progress, and chat together.",
                36, FontStyles.Normal, new Color(0.88f, 0.88f, 0.94f, 1f));
            var sRT = sub.rectTransform;
            sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(1, 1);
            sRT.pivot = new Vector2(0.5f, 1);
            sRT.offsetMin = new Vector2(50, -380); sRT.offsetMax = new Vector2(-50, -200);
            sub.alignment = TextAlignmentOptions.Center;
            sub.textWrappingMode = TextWrappingModes.Normal;

            BuildBigButton(body, "BROWSE GUILDS", new Vector2(0, -500), BTN_ORANGE, INK, () => {
                Hide();
                try { Sparq.UI.WorldPanel.Show(); }
                catch (System.Exception ex)
                { Debug.LogError($"[GuildPanel] WorldPanel.Show failed: {ex.Message}"); }
            });

            // CREATE GUILD now actually works — opens the inline create form.
            BuildBigButton(body, "CREATE GUILD", new Vector2(0, -660), BTN_GREEN, INK, () => {
                BuildCreateForm(body);
            });
        }

        // ── Joined state: show the guild dashboard ────────────────────────
        private static void BuildJoinedState(Transform body, string guildName)
        {
            string tag = "";
            try { tag = Sparq.Core.SaveService.Data?.guildTag ?? ""; } catch {}

            // Big guild name
            var nm = MakeText(body, "Name",
                string.IsNullOrEmpty(tag) ? guildName : $"[{tag}] {guildName}",
                56, FontStyles.Bold, CREAM);
            var nRT = nm.rectTransform;
            nRT.anchorMin = new Vector2(0, 1); nRT.anchorMax = new Vector2(1, 1);
            nRT.pivot = new Vector2(0.5f, 1);
            nRT.offsetMin = new Vector2(40, -200); nRT.offsetMax = new Vector2(-40, -80);
            nm.alignment = TextAlignmentOptions.Center;
            nm.textWrappingMode = TextWrappingModes.Normal;

            // Role + member count placeholder
            var role = MakeText(body, "Role",
                "Leader  •  1 member",
                32, FontStyles.Bold, BTN_ORANGE);
            var rRT = role.rectTransform;
            rRT.anchorMin = new Vector2(0, 1); rRT.anchorMax = new Vector2(1, 1);
            rRT.pivot = new Vector2(0.5f, 1);
            rRT.offsetMin = new Vector2(40, -250); rRT.offsetMax = new Vector2(-40, -210);
            role.alignment = TextAlignmentOptions.Center;

            var sub = MakeText(body, "Sub",
                "Invite friends to join, or browse other guilds to compare.",
                30, FontStyles.Normal, new Color(0.88f, 0.88f, 0.94f, 1f));
            var sRT = sub.rectTransform;
            sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(1, 1);
            sRT.pivot = new Vector2(0.5f, 1);
            sRT.offsetMin = new Vector2(50, -420); sRT.offsetMax = new Vector2(-50, -290);
            sub.alignment = TextAlignmentOptions.Center;
            sub.textWrappingMode = TextWrappingModes.Normal;

            BuildBigButton(body, "OPEN GUILD CHAT", new Vector2(0, -520), BTN_ORANGE, INK, () => {
                Hide();
                try { Sparq.UI.WorldPanel.Show(); }
                catch (System.Exception ex)
                { Debug.LogError($"[GuildPanel] WorldPanel.Show failed: {ex.Message}"); }
            });

            BuildBigButton(body, "BROWSE GUILDS", new Vector2(0, -680), BTN_GREY, CREAM, () => {
                Hide();
                try { Sparq.UI.WorldPanel.Show(); }
                catch (System.Exception ex)
                { Debug.LogError($"[GuildPanel] WorldPanel.Show failed: {ex.Message}"); }
            });

            BuildBigButton(body, "Leave Guild", new Vector2(0, -840), new Color(0.55f, 0.30f, 0.30f, 1f),
                new Color(1f, 0.9f, 0.9f, 1f), () => {
                try
                {
                    var d = Sparq.Core.SaveService.Data;
                    if (d != null)
                    {
                        d.guildName = ""; d.guildTag = ""; d.guildFoundedUnix = 0;
                        Sparq.Core.SaveService.Save();
                    }
                }
                catch (System.Exception ex)
                { Debug.LogError($"[GuildPanel] Leave guild failed: {ex.Message}"); }
                BuildBody();
            });
        }

        // ── Inline Create Guild form ──────────────────────────────────────
        private static void BuildCreateForm(Transform body)
        {
            // Wipe current body content and build the form in-place.
            for (int i = body.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(body.GetChild(i).gameObject);

            var hdr = MakeText(body, "Hdr", "Create your guild", 50, FontStyles.Bold, CREAM);
            var hRT = hdr.rectTransform;
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.offsetMin = new Vector2(40, -160); hRT.offsetMax = new Vector2(-40, -60);
            hdr.alignment = TextAlignmentOptions.Center;

            // Name input
            var nameLbl = MakeText(body, "NL", "GUILD NAME",
                26, FontStyles.Bold, new Color(0.88f, 0.88f, 0.94f, 0.85f));
            var nlRT = nameLbl.rectTransform;
            nlRT.anchorMin = new Vector2(0, 1); nlRT.anchorMax = new Vector2(1, 1);
            nlRT.pivot = new Vector2(0, 1);
            nlRT.offsetMin = new Vector2(70, -220); nlRT.offsetMax = new Vector2(-70, -190);
            nameLbl.alignment = TextAlignmentOptions.MidlineLeft;

            var nameInput = MakeInput(body, "NameInput", "e.g. Calm Knights", 38);
            var niRT = nameInput.GetComponent<RectTransform>();
            niRT.anchorMin = new Vector2(0, 1); niRT.anchorMax = new Vector2(1, 1);
            niRT.pivot = new Vector2(0.5f, 1);
            niRT.offsetMin = new Vector2(60, -330); niRT.offsetMax = new Vector2(-60, -230);

            // Tag input
            var tagLbl = MakeText(body, "TL", "TAG  (2–4 chars)",
                26, FontStyles.Bold, new Color(0.88f, 0.88f, 0.94f, 0.85f));
            var tlRT = tagLbl.rectTransform;
            tlRT.anchorMin = new Vector2(0, 1); tlRT.anchorMax = new Vector2(1, 1);
            tlRT.pivot = new Vector2(0, 1);
            tlRT.offsetMin = new Vector2(70, -380); tlRT.offsetMax = new Vector2(-70, -350);
            tagLbl.alignment = TextAlignmentOptions.MidlineLeft;

            var tagInput = MakeInput(body, "TagInput", "CALM", 38);
            tagInput.characterLimit = 4;
            var tiRT = tagInput.GetComponent<RectTransform>();
            tiRT.anchorMin = new Vector2(0, 1); tiRT.anchorMax = new Vector2(0, 1);
            tiRT.pivot = new Vector2(0, 1);
            tiRT.offsetMin = new Vector2(60, -490); tiRT.offsetMax = new Vector2(60, -390);
            tiRT.sizeDelta = new Vector2(260, 100);

            var errLbl = MakeText(body, "Err", "",
                26, FontStyles.Bold, new Color(0.95f, 0.4f, 0.45f, 1f));
            var elRT = errLbl.rectTransform;
            elRT.anchorMin = new Vector2(0, 1); elRT.anchorMax = new Vector2(1, 1);
            elRT.pivot = new Vector2(0.5f, 1);
            elRT.offsetMin = new Vector2(40, -540); elRT.offsetMax = new Vector2(-40, -500);
            errLbl.alignment = TextAlignmentOptions.Center;

            // Create + Cancel buttons
            BuildBigButton(body, "CREATE", new Vector2(0, -620), BTN_GREEN, INK, () => {
                string name = (nameInput.text ?? "").Trim();
                string tag  = (tagInput.text  ?? "").Trim().ToUpper();
                if (name.Length < 3 || name.Length > 24) { errLbl.text = "Name must be 3–24 characters."; return; }
                if (tag.Length  < 2 || tag.Length  > 4)  { errLbl.text = "Tag must be 2–4 characters."; return; }

                // Guild name + tag are visible to every other player in the
                // guild browser, so they go through the strict username
                // inspector (blocks PII, slurs, URLs). Either field failing
                // surfaces the moderator's reason inline.
                var nameVerdict = Sparq.Safety.ContentModerator.InspectUsername(name);
                if (!nameVerdict.Allowed)
                { errLbl.text = string.IsNullOrEmpty(nameVerdict.UserFacingMessage)
                    ? "Pick a different guild name." : nameVerdict.UserFacingMessage; return; }
                var tagVerdict = Sparq.Safety.ContentModerator.InspectUsername(tag);
                if (!tagVerdict.Allowed)
                { errLbl.text = string.IsNullOrEmpty(tagVerdict.UserFacingMessage)
                    ? "Pick a different tag." : tagVerdict.UserFacingMessage; return; }
                name = nameVerdict.SanitizedText ?? name;
                tag  = tagVerdict.SanitizedText ?? tag;
                try
                {
                    var d = Sparq.Core.SaveService.Data;
                    if (d != null)
                    {
                        d.guildName = name;
                        d.guildTag  = tag;
                        d.guildFoundedUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        Sparq.Core.SaveService.Save();
                    }
                }
                catch (System.Exception ex)
                { Debug.LogError($"[GuildPanel] Save guild failed: {ex.Message}"); }
                BuildBody();   // re-renders into joined-state view
            });

            BuildBigButton(body, "Cancel", new Vector2(0, -780), BTN_GREY, CREAM, () => BuildBody());
        }

        // ── Procedural TMP_InputField helper ──────────────────────────────
        private static TMP_InputField MakeInput(Transform parent, string name, string placeholder, float fontSize)
        {
            var go = NewGO(name, parent, typeof(Image));
            var img = go.GetComponent<Image>();
            img.color = new Color(0.10f, 0.10f, 0.12f, 1f);

            var area = NewGO("TextArea", go.transform, typeof(RectMask2D));
            var aRT = area.GetComponent<RectTransform>();
            Stretch(aRT);
            aRT.offsetMin = new Vector2(20, 14); aRT.offsetMax = new Vector2(-20, -14);

            var ph = MakeText(area.transform, "Placeholder", placeholder, fontSize, FontStyles.Italic,
                new Color(0.6f, 0.6f, 0.68f, 1f));
            Stretch(ph.rectTransform);
            ph.alignment = TextAlignmentOptions.MidlineLeft;

            var txt = MakeText(area.transform, "Text", "", fontSize, FontStyles.Bold, Color.white);
            Stretch(txt.rectTransform);
            txt.alignment = TextAlignmentOptions.MidlineLeft;

            var input = go.AddComponent<TMP_InputField>();
            input.targetGraphic = img;
            input.textViewport  = aRT;
            input.textComponent = txt;
            input.placeholder   = ph;
            input.lineType      = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 24;
            input.text = "";
            return input;
        }

        private static void BuildBigButton(Transform parent, string label, Vector2 anchoredPos,
            Color bg, Color textColor, System.Action onClick)
        {
            var go = NewGO("Btn_" + label, parent, typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(620, 130);
            var img = go.GetComponent<Image>();
            img.color = bg; img.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img; btn.interactable = onClick != null;
            var lbl = MakeText(go.transform, "L", label, 36, FontStyles.Bold, textColor);
            Stretch(lbl.rectTransform); lbl.alignment = TextAlignmentOptions.Center;
            if (onClick != null) btn.onClick.AddListener(() => onClick.Invoke());
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
            var existing = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
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
