using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Settings panel — opened from the top-right gear icon in the lobby.
    ///
    /// Sections:
    ///   - PROFILE   → Change username
    ///   - AUDIO     → Mute/unmute SFX + BGM
    ///   - ABOUT     → Version, credits
    ///   - DANGER    → Reset save / sign out
    ///
    /// Built procedurally so we don't inherit Layer Lab's prefab quirks.
    /// </summary>
    public static class SettingsPanel
    {
        private static GameObject _root;

        public static void Show()
        {
            if (_root != null) Object.Destroy(_root);
            EnsureEventSystem();

            // Top-sort canvas — above lobby + popups
            _root = new GameObject("Sparq_SettingsPanel",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var c = _root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 15000;
            foreach (var other in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort)
                    maxSort = other.sortingOrder;
            c.sortingOrder = maxSort + 20;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Dim backdrop — tap outside to close
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
            crt.sizeDelta = new Vector2(900, 1800);
            card.GetComponent<Image>().color = new Color(0.16f, 0.10f, 0.28f, 1f);
            card.GetComponent<Image>().raycastTarget = true;

            // Title bar
            var titleBar = NewGO("TitleBar", card.transform, typeof(Image));
            var tbRT = titleBar.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0, 1); tbRT.anchorMax = new Vector2(1, 1);
            tbRT.pivot = new Vector2(0.5f, 1);
            tbRT.anchoredPosition = Vector2.zero;
            tbRT.sizeDelta = new Vector2(0, 130);
            titleBar.GetComponent<Image>().color = new Color(0.55f, 0.40f, 0.85f, 1f);
            var titleTm = MakeText(titleBar.transform, "Title", "SETTINGS",
                52, FontStyles.Bold, new Color(1f, 0.95f, 0.55f));
            var titleRT = titleTm.rectTransform;
            titleRT.anchorMin = Vector2.zero; titleRT.anchorMax = Vector2.one;
            titleRT.offsetMin = new Vector2(20, 0); titleRT.offsetMax = new Vector2(-120, 0);
            titleTm.alignment = TextAlignmentOptions.Center;

            // Close X (top-right)
            var closeBtn = NewGO("CloseBtn", card.transform, typeof(Image), typeof(Button));
            var cbRT = closeBtn.GetComponent<RectTransform>();
            cbRT.anchorMin = new Vector2(1, 1); cbRT.anchorMax = new Vector2(1, 1);
            cbRT.pivot = new Vector2(1, 1);
            cbRT.anchoredPosition = new Vector2(-25, -25);
            cbRT.sizeDelta = new Vector2(85, 85);
            closeBtn.GetComponent<Image>().color = new Color(0.85f, 0.25f, 0.25f, 1f);
            var xLbl = MakeText(closeBtn.transform, "X", "X",
                52, FontStyles.Bold, Color.white);
            var xRT = xLbl.rectTransform;
            xRT.anchorMin = Vector2.zero; xRT.anchorMax = Vector2.one;
            xRT.offsetMin = Vector2.zero; xRT.offsetMax = Vector2.zero;
            xLbl.alignment = TextAlignmentOptions.Center;
            closeBtn.GetComponent<Button>().onClick.AddListener(Hide);

            // ── Sections (top-down) ──
            float y = -180;
            const float ROW_H = 130;
            const float ROW_GAP = 30;

            // PROFILE
            BuildSectionLabel(card.transform, "PROFILE", y);
            y -= 70;
            BuildRow(card.transform, y, "Username", GetPlayerName(),
                "Change", () => { try { Sparq.UI.UsernameEditPopup.Show(onSaved: () => { Hide(); Show(); }); } catch {} });
            y -= ROW_H + ROW_GAP;
            BuildRow(card.transform, y, "Hero Class", GetHeroClassDisplay(),
                "Change", () => {
                    // Non-forced picker: user can cancel. After pick, the
                    // panel auto-closes; we reopen Settings so the new class
                    // label is reflected.
                    try {
                        Hide();
                        Sparq.UI.HeroSelectPanel.Show(forcePick: false, onPicked: () => Show());
                    } catch {}
                });
            y -= ROW_H + ROW_GAP;
            BuildRow(card.transform, y, "Squad Ally", GetActiveAllyName(),
                "Manage", () => { Hide(); try { Sparq.UI.AllyRosterPanel.Show(); } catch {} });
            y -= ROW_H + ROW_GAP;

            // AUDIO
            BuildSectionLabel(card.transform, "AUDIO", y);
            y -= 70;
            bool sfxMuted = PlayerPrefs.GetInt("sparq.audio.sfx_mute", 0) == 1;
            bool bgmMuted = PlayerPrefs.GetInt("sparq.audio.bgm_mute", 0) == 1;
            BuildToggleRow(card.transform, y, "Sound Effects", !sfxMuted, val => {
                PlayerPrefs.SetInt("sparq.audio.sfx_mute", val ? 0 : 1);
                PlayerPrefs.Save();
                try { AudioListener.volume = val ? 1f : 0.0f; } catch {}   // crude global; refine later
            });
            y -= ROW_H + ROW_GAP;
            BuildToggleRow(card.transform, y, "Music", !bgmMuted, val => {
                PlayerPrefs.SetInt("sparq.audio.bgm_mute", val ? 0 : 1);
                PlayerPrefs.Save();
            });
            y -= ROW_H + ROW_GAP;

            // ACCESSIBILITY
            BuildSectionLabel(card.transform, "ACCESSIBILITY", y);
            y -= 70;
            bool reduceMotion = false;
            try { reduceMotion = Sparq.Systems.Accessibility.ReducedMotion; } catch {}
            BuildToggleRow(card.transform, y, "Reduce Motion", reduceMotion, val => {
                try { Sparq.Systems.Accessibility.ReducedMotion = val; } catch {}
            });
            y -= ROW_H + ROW_GAP;

            // SAFETY
            BuildSectionLabel(card.transform, "SAFETY", y);
            y -= 70;
            int blockedCount = 0;
            try { blockedCount = Sparq.Safety.BlockList.Count; } catch {}
            BuildRow(card.transform, y, "Blocked Players",
                blockedCount == 0 ? "None" : (blockedCount + " blocked"),
                "Manage", () => { Hide(); BlockedPlayersPanel.Show(); });
            y -= ROW_H + ROW_GAP;

            // ABOUT
            BuildSectionLabel(card.transform, "ABOUT", y);
            y -= 70;
            BuildRow(card.transform, y, "Intro Tutorial", "How Sparq works",
                "Replay", () => { Hide(); try { Sparq.UI.WelcomePanel.Replay(); } catch {} });
            y -= ROW_H + ROW_GAP;
            // Version row — also the hidden trigger for DebugPanel (TESTER TOOLS).
            // 7 taps within VERSION_TAP_WINDOW_SECONDS opens it. Same affordance
            // as Android Dev Options; obscure enough that real users never hit it
            // and obvious enough that testers told about it can find it.
            BuildRow(card.transform, y, "Version", Application.version,
                null, null);
            AttachVersionTapTrigger(card.transform, y);
            y -= ROW_H + ROW_GAP;
            BuildRow(card.transform, y, "Made by", "Sparq Team",
                null, null);
            y -= ROW_H + ROW_GAP;

            // DANGER ZONE
            BuildSectionLabel(card.transform, "DANGER ZONE", y, new Color(1f, 0.55f, 0.55f));
            y -= 70;
            BuildRow(card.transform, y, "Sign Out", "Reset username",
                "Sign Out", () => {
                    var data = Sparq.Core.SaveService.Data;
                    if (data != null) {
                        data.playerName = "Sparq User";
                        try { Sparq.Core.SaveService.ScheduleSave(); } catch {}
                    }
                    Hide();
                    try { Sparq.UI.AuthPanel.ShowOrSkipToLobby(); } catch {}
                }, dangerColor: true);

            Debug.Log("[SettingsPanel] Opened.");
        }

        public static void Hide()
        {
            if (_root != null) { Object.Destroy(_root); _root = null; }
            // Return to lobby
            try { Sparq.UI.HomeLobbyPanel.Show(); }
            catch (System.Exception ex) { Debug.LogError($"[SettingsPanel] Lobby reopen: {ex.Message}"); }
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private static string GetPlayerName()
        {
            try { var d = Sparq.Core.SaveService.Data; return d?.playerName ?? "Sparq User"; }
            catch { return "Sparq User"; }
        }

        // Currently-active ally's display name (e.g., "Paladin", "Ranger") for
        // the Settings row, with an unlocked-count summary.
        private static string GetActiveAllyName()
        {
            try
            {
                var a = Sparq.Systems.AllyRoster.Active();
                int unlocked = Sparq.Systems.AllyRoster.UnlockedIds().Count;
                int total = Sparq.Systems.AllyRoster.ALL.Length;
                return $"{a?.name ?? "Paladin"}  ({unlocked}/{total})";
            }
            catch { return "Paladin"; }
        }

        // Friendly label for the currently-selected hero class. Stays in
        // sync with the IDs HeroSelectPanel writes to PlayerData.heroClass.
        private static string GetHeroClassDisplay()
        {
            string id = "";
            try { id = Sparq.Core.SaveService.Data?.heroClass ?? ""; } catch {}
            switch (id)
            {
                case "knight":   return "Knight";
                case "archer":   return "Elf Archer";
                case "mage":     return "Time Keeper Mage";
                case "assassin": return "Assassin";
                default:         return "Not chosen";
            }
        }

        // Section header (small uppercase label)
        private static void BuildSectionLabel(Transform parent, string text, float y, Color? color = null)
        {
            var go = NewGO("Section", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(-80, 50);
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = 28;
            tm.fontStyle = FontStyles.Bold;
            tm.color = color ?? new Color(1f, 0.82f, 0.42f);
            tm.font = TMP_Settings.defaultFontAsset;
            tm.alignment = TextAlignmentOptions.MidlineLeft;
            tm.raycastTarget = false;
        }

        // Row: label : value [optional button]
        private static void BuildRow(Transform parent, float y, string label, string value,
                                     string btnText, System.Action onTap, bool dangerColor = false)
        {
            var row = NewGO("Row", parent, typeof(Image));
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(-60, 110);
            var rowImg = row.GetComponent<Image>();
            rowImg.color = new Color(0.10f, 0.06f, 0.18f, 0.85f);
            rowImg.raycastTarget = false;

            var lbl = MakeText(row.transform, "Label", label,
                30, FontStyles.Bold, Color.white);
            var lrt = lbl.rectTransform;
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(0, 1);
            lrt.pivot = new Vector2(0, 0.5f);
            lrt.anchoredPosition = new Vector2(28, 0);
            lrt.sizeDelta = new Vector2(360, 0);
            lbl.alignment = TextAlignmentOptions.MidlineLeft;

            var val = MakeText(row.transform, "Value", value,
                26, FontStyles.Normal, new Color(0.85f, 0.80f, 0.95f));
            var vrt = val.rectTransform;
            vrt.anchorMin = new Vector2(0, 0); vrt.anchorMax = new Vector2(1, 1);
            vrt.pivot = new Vector2(0, 0.5f);
            vrt.anchoredPosition = new Vector2(400, 0);
            vrt.sizeDelta = new Vector2(-240, 0);
            val.alignment = TextAlignmentOptions.MidlineLeft;

            if (!string.IsNullOrEmpty(btnText) && onTap != null)
            {
                var btnGO = NewGO("Btn", row.transform, typeof(Image), typeof(Button));
                var brt = btnGO.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(1, 0.5f); brt.anchorMax = new Vector2(1, 0.5f);
                brt.pivot = new Vector2(1, 0.5f);
                brt.anchoredPosition = new Vector2(-20, 0);
                brt.sizeDelta = new Vector2(190, 76);
                var bImg = btnGO.GetComponent<Image>();
                bImg.color = dangerColor ? new Color(0.85f, 0.30f, 0.30f, 1f)
                                         : new Color(1f, 0.78f, 0.22f, 1f);
                var btnLbl = MakeText(btnGO.transform, "Lbl", btnText,
                    26, FontStyles.Bold, dangerColor ? Color.white : new Color(0.10f, 0.05f, 0.18f));
                var blRT = btnLbl.rectTransform;
                blRT.anchorMin = Vector2.zero; blRT.anchorMax = Vector2.one;
                blRT.offsetMin = Vector2.zero; blRT.offsetMax = Vector2.zero;
                btnLbl.alignment = TextAlignmentOptions.Center;
                btnGO.GetComponent<Button>().onClick.AddListener(() => {
                    try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                    onTap();
                });
            }
        }

        // Toggle row: label : [ON/OFF pill]
        private static void BuildToggleRow(Transform parent, float y, string label,
                                           bool on, System.Action<bool> onChange)
        {
            var row = NewGO("ToggleRow", parent, typeof(Image));
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(-60, 110);
            row.GetComponent<Image>().color = new Color(0.10f, 0.06f, 0.18f, 0.85f);

            var lbl = MakeText(row.transform, "Label", label,
                30, FontStyles.Bold, Color.white);
            var lrt = lbl.rectTransform;
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(0, 1);
            lrt.pivot = new Vector2(0, 0.5f);
            lrt.anchoredPosition = new Vector2(28, 0);
            lrt.sizeDelta = new Vector2(500, 0);
            lbl.alignment = TextAlignmentOptions.MidlineLeft;

            // Toggle pill
            var pill = NewGO("Pill", row.transform, typeof(Image), typeof(Button));
            var prt = pill.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(1, 0.5f); prt.anchorMax = new Vector2(1, 0.5f);
            prt.pivot = new Vector2(1, 0.5f);
            prt.anchoredPosition = new Vector2(-20, 0);
            prt.sizeDelta = new Vector2(150, 70);
            var pillImg = pill.GetComponent<Image>();
            pillImg.color = on ? new Color(0.45f, 0.85f, 0.55f, 1f) : new Color(0.40f, 0.30f, 0.50f, 1f);

            var pTxt = MakeText(pill.transform, "PTxt", on ? "ON" : "OFF",
                26, FontStyles.Bold, Color.white);
            var ptRT = pTxt.rectTransform;
            ptRT.anchorMin = Vector2.zero; ptRT.anchorMax = Vector2.one;
            ptRT.offsetMin = Vector2.zero; ptRT.offsetMax = Vector2.zero;
            pTxt.alignment = TextAlignmentOptions.Center;

            bool localOn = on;
            pill.GetComponent<Button>().onClick.AddListener(() => {
                localOn = !localOn;
                pillImg.color = localOn ? new Color(0.45f, 0.85f, 0.55f, 1f) : new Color(0.40f, 0.30f, 0.50f, 1f);
                pTxt.text = localOn ? "ON" : "OFF";
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                onChange(localOn);
            });
        }

        // ── Hidden DebugPanel trigger ────────────────────────────────────────
        // 7 taps on the Version row within VERSION_TAP_WINDOW_SECONDS opens
        // DebugPanel. Counter lives in static state so it survives Hide/Show
        // of SettingsPanel (a tester who closes the panel mid-burst doesn't
        // lose progress) but is reset on a successful open or window timeout.
        private const int VERSION_TAPS_REQUIRED = 7;
        private const float VERSION_TAP_WINDOW_SECONDS = 4f;
        private static int _versionTapCount;
        private static float _versionTapWindowExpiresAt;

        private static void AttachVersionTapTrigger(Transform card, float versionRowY)
        {
            // Invisible Button overlay covering the Version row. We can't reuse
            // BuildRow's row GO directly (it sets raycastTarget=false), so we
            // stack a transparent Button on top sized to the row's dimensions.
            // sizeDelta math mirrors BuildRow at line ~263: (-60, 110).
            const float ROW_H = 130;
            var hit = NewGO("VersionTapHit", card, typeof(Image), typeof(Button));
            var rt = hit.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, versionRowY);
            rt.sizeDelta = new Vector2(-60, ROW_H - 20);
            // Fully transparent — image only exists so the Button has a raycast
            // target. (Unity Buttons need a Graphic component to receive taps.)
            hit.GetComponent<Image>().color = new Color(0, 0, 0, 0f);
            hit.GetComponent<Button>().onClick.AddListener(OnVersionTapped);
        }

        private static void OnVersionTapped()
        {
            float now = Time.unscaledTime;
            // Window expired (or first tap) → restart the count from 1.
            if (now > _versionTapWindowExpiresAt) _versionTapCount = 0;
            _versionTapCount++;
            _versionTapWindowExpiresAt = now + VERSION_TAP_WINDOW_SECONDS;

            if (_versionTapCount >= VERSION_TAPS_REQUIRED)
            {
                _versionTapCount = 0;
                _versionTapWindowExpiresAt = 0f;
                Debug.Log("[SettingsPanel] DebugPanel unlocked.");
                Hide();
                try { Sparq.UI.DebugPanel.Show(); }
                catch (System.Exception ex) { Debug.LogError("[SettingsPanel] DebugPanel.Show failed: " + ex); }
            }
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
