using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Lets the player add their own quest with a name + XP value.
    /// e.g. "Pay electric bill" → +25 XP
    ///       "Schedule dentist appointment" → +30 XP
    /// </summary>
    public static class CustomQuestCreator
    {
        public static void Show()
        {
            // Top-level overlay (NOT parented under another canvas) so nothing on
            // home / popups can render above us. ScreenSpaceOverlay + high sort.
            var root = new GameObject("CustomQuestRoot",
                typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(GraphicRaycaster));
            var rrt = root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var oc = root.GetComponent<Canvas>();
            oc.renderMode = RenderMode.ScreenSpaceOverlay;
            oc.sortingOrder = 14800; // above QuestsPanel (14600) and home popups
            var cs = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            cs.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;
            // Also fall back: try to find a canvas to parent under (preserves UI scale on some scenes)
            var canvas = root.transform; // alias for downstream reference

            // Dim
            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            dim.transform.SetParent(root.transform, false);
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0,0,0,0.92f);
            var dimBtn = dim.GetComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(() => Object.Destroy(root));

            // Card
            var card = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            card.transform.SetParent(root.transform, false);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot     = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(900, 1100);
            card.GetComponent<Image>().color = new Color(0.22f, 0.20f, 0.40f, 1f); // matches QuestsPanel indigo
            var vlg = card.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(48, 48, 40, 40);
            vlg.spacing = 22;
            vlg.childForceExpandWidth = true;
            vlg.childAlignment = TextAnchor.UpperCenter;

            // Title
            AddText(card.transform, "✨  ADD YOUR OWN QUEST", 40, Color.white, FontStyles.Bold,
                    TextAlignmentOptions.Center, 70);
            AddText(card.transform, "Pay bills, schedule appointments, anything ADHD-tough.", 22,
                    new Color(1,1,1,0.75f), FontStyles.Italic, TextAlignmentOptions.Center, 50);

            // Quest name input
            AddText(card.transform, "What's the quest?", 26, new Color(1f, 0.85f, 0.4f), FontStyles.Bold,
                    TextAlignmentOptions.Left, 40);
            var inputGO = new GameObject("Input", typeof(RectTransform), typeof(Image));
            inputGO.transform.SetParent(card.transform, false);
            var ile = inputGO.AddComponent<LayoutElement>();
            ile.preferredHeight = 100;
            ile.flexibleWidth = 1;
            inputGO.GetComponent<Image>().color = new Color(0.10f, 0.08f, 0.20f, 0.85f);

            var input = inputGO.AddComponent<TMP_InputField>();
            // Build text component
            var inputText = new GameObject("Text", typeof(RectTransform));
            inputText.transform.SetParent(inputGO.transform, false);
            var irt = inputText.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(16, 8); irt.offsetMax = new Vector2(-16, -8);
            var itm = inputText.AddComponent<TextMeshProUGUI>();
            itm.fontSize = 32;
            itm.color = Color.white;
            itm.alignment = TextAlignmentOptions.Left;
            input.textComponent = itm;
            input.text = "";

            // Placeholder
            var phGO = new GameObject("Placeholder", typeof(RectTransform));
            phGO.transform.SetParent(inputGO.transform, false);
            var phrt = phGO.GetComponent<RectTransform>();
            phrt.anchorMin = Vector2.zero; phrt.anchorMax = Vector2.one;
            phrt.offsetMin = new Vector2(16, 8); phrt.offsetMax = new Vector2(-16, -8);
            var phtm = phGO.AddComponent<TextMeshProUGUI>();
            phtm.text = "e.g. Pay electric bill";
            phtm.fontSize = 32;
            phtm.color = new Color(1, 1, 1, 0.4f);
            phtm.fontStyle = FontStyles.Italic;
            phtm.alignment = TextAlignmentOptions.Left;
            phtm.raycastTarget = false;
            input.placeholder = phtm;

            // XP picker label
            AddText(card.transform, "How much XP?", 26, new Color(1f, 0.85f, 0.4f), FontStyles.Bold,
                    TextAlignmentOptions.Left, 40);

            int chosenXP = 20;
            // XP buttons row
            var xpRow = new GameObject("XPRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            xpRow.transform.SetParent(card.transform, false);
            var xprt = xpRow.GetComponent<RectTransform>();
            var xpLE = xpRow.AddComponent<LayoutElement>();
            xpLE.preferredHeight = 100;
            xpLE.flexibleWidth = 1;
            var xphlg = xpRow.GetComponent<HorizontalLayoutGroup>();
            xphlg.spacing = 14;
            xphlg.childForceExpandWidth = true;
            xphlg.childForceExpandHeight = true;

            var xpButtons = new System.Collections.Generic.List<(int xp, Image img, TMP_Text lbl)>();
            int[] xpValues = { 10, 15, 20, 25, 30 };
            Color SELECTED_BG = new Color(1f, 0.85f, 0.30f, 1f);    // gold
            Color UNSELECTED_BG = new Color(0.55f, 0.50f, 0.90f, 1f); // bright periwinkle
            Color SELECTED_FG = new Color(0.10f, 0.05f, 0.20f, 1f);   // deep navy
            Color UNSELECTED_FG = Color.white;
            foreach (var xp in xpValues)
            {
                var bGO = new GameObject($"XP_{xp}", typeof(RectTransform), typeof(Image), typeof(Button));
                bGO.transform.SetParent(xpRow.transform, false);
                var bImg = bGO.GetComponent<Image>();
                bImg.color = (xp == chosenXP) ? SELECTED_BG : UNSELECTED_BG;
                var btn = bGO.GetComponent<Button>();
                int captured = xp;
                btn.onClick.AddListener(() =>
                {
                    chosenXP = captured;
                    foreach (var (xv, img, ltm) in xpButtons)
                    {
                        bool sel = (xv == chosenXP);
                        img.color = sel ? SELECTED_BG : UNSELECTED_BG;
                        if (ltm != null)
                        {
                            ltm.color = sel ? SELECTED_FG : UNSELECTED_FG;
                            ltm.outlineWidth = sel ? 0.0f : 0.25f;
                            ltm.outlineColor = sel ? Color.clear : new Color(0.10f, 0.05f, 0.30f, 1f);
                        }
                    }
                });

                // Label
                var lbl = new GameObject("Label", typeof(RectTransform));
                lbl.transform.SetParent(bGO.transform, false);
                var lrt = lbl.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
                var ltm2 = lbl.AddComponent<TextMeshProUGUI>();
                ltm2.text = $"+{xp}";
                ltm2.fontSize = 36;
                ltm2.fontStyle = FontStyles.Bold;
                ltm2.alignment = TextAlignmentOptions.Center;
                ltm2.color = (xp == chosenXP) ? SELECTED_FG : UNSELECTED_FG;
                ltm2.outlineWidth = (xp == chosenXP) ? 0f : 0.25f;
                ltm2.outlineColor = (xp == chosenXP) ? Color.clear : new Color(0.10f, 0.05f, 0.30f, 1f);
                ltm2.raycastTarget = false;

                xpButtons.Add((xp, bImg, ltm2));
            }

            // Spacer
            var sp = new GameObject("Spacer", typeof(RectTransform));
            sp.transform.SetParent(card.transform, false);
            var sple = sp.AddComponent<LayoutElement>();
            sple.preferredHeight = 12;

            // ADD button — bright green
            var addBtn = AddButton(card.transform, "✓  ADD QUEST",
                new Color(0.30f, 0.80f, 0.42f), Color.white);
            addBtn.onClick.AddListener(() =>
            {
                string name = input.text?.Trim();
                if (string.IsNullOrEmpty(name)) return;

                // ── CONTENT MODERATION ──────────────────────────────────────
                // Quest titles are user-authored free text that PERSISTS in
                // the user's quest list — without this an unmoderated title
                // ("kill myself", "shoot up the school") would silently
                // become a permanent quest in their data. Same routes as
                // chat: SelfHarmIdeation surfaces CrisisResourcesPanel;
                // ThreatViolence surfaces ThreatResponsePanel; PII / slurs
                // / etc. block + sanitize with a floater message.
                var verdict = Sparq.Safety.ContentModerator.Inspect(name, "quest");
                if (!verdict.Allowed)
                {
                    Debug.LogWarning($"[CustomQuestCreator] Blocked quest title: {verdict.UserFacingMessage}");
                    if (verdict.Reasons.Contains(Sparq.Safety.ContentModerator.Category.ThreatViolence)
                        && !Sparq.UI.ThreatResponsePanel.RecentlyDismissed())
                    { try { Sparq.UI.ThreatResponsePanel.Show(); } catch {} }
                    else if (!string.IsNullOrEmpty(verdict.UserFacingMessage))
                    {
                        XPFloater.Spawn(canvas.transform,
                            root.transform.position + new Vector3(0, 200, 0),
                            verdict.UserFacingMessage,
                            new Color(0.85f, 0.30f, 0.30f));
                    }
                    if (verdict.Reasons.Contains(Sparq.Safety.ContentModerator.Category.SelfHarmIdeation)
                        && !Sparq.UI.CrisisResourcesPanel.RecentlyDismissed())
                    { try { Sparq.UI.CrisisResourcesPanel.Show(); } catch {} }
                    // Replace the input with the sanitized version so PII
                    // doesn't sit visible in the field; the player can edit
                    // and retry.
                    input.text = verdict.SanitizedText ?? "";
                    return;
                }
                // Warn-severity: persist the sanitized text (profanity → ***).
                name = verdict.SanitizedText ?? name;
                if (verdict.Severity == Sparq.Safety.ContentModerator.Severity.Warn
                    && !string.IsNullOrEmpty(verdict.UserFacingMessage))
                {
                    XPFloater.Spawn(canvas.transform,
                        root.transform.position + new Vector3(0, 200, 0),
                        verdict.UserFacingMessage,
                        new Color(1f, 0.78f, 0.30f));
                }

                var data = Sparq.Core.SaveService.Data;
                if (data == null) return;
                if (data.customTasks == null) data.customTasks = new System.Collections.Generic.List<Sparq.Core.CustomTask>();

                data.customTasks.Add(new Sparq.Core.CustomTask { name = name, xp = chosenXP, done = false });
                Sparq.Core.SaveService.Save();

                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Coin);
                XPFloater.Spawn(canvas.transform, root.transform.position + new Vector3(0, 200, 0),
                    $"Added: \"{name}\"", new Color(0.3f, 1f, 0.5f));

                // Refresh quest list if visible
                var ql = Object.FindAnyObjectByType<Sparq.UI.QuestListUI>();
                if (ql != null) ql.Rebuild();
                // Refresh full Quests popup if open
                Sparq.UI.QuestsPanel.RebuildIfOpen();

                Object.Destroy(root);
            });

            // Cancel button — coral red (matches QuestsPanel close)
            var cancelBtn = AddButton(card.transform, "Cancel",
                new Color(0.92f, 0.35f, 0.42f), Color.white);
            cancelBtn.onClick.AddListener(() => Object.Destroy(root));
        }

        private static void AddText(Transform parent, string text, int size, Color color, FontStyles style,
                                    TextAlignmentOptions align, float height)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 1;
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = size;
            tm.color = color;
            tm.fontStyle = style;
            tm.alignment = align;
            tm.raycastTarget = false;
        }

        private static Button AddButton(Transform parent, string label, Color bg, Color fg)
        {
            var go = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 110;
            le.flexibleWidth = 1;
            go.GetComponent<Image>().color = bg;
            var btn = go.GetComponent<Button>();

            var lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tm = lbl.AddComponent<TextMeshProUGUI>();
            tm.text = label;
            tm.fontSize = 32;
            tm.fontStyle = FontStyles.Bold;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = fg;
            tm.outlineWidth = 0.22f;
            tm.outlineColor = new Color(0.10f, 0.05f, 0.20f, 1f);
            tm.raycastTarget = false;

            return btn;
        }
    }
}
