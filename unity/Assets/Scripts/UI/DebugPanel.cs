using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Hidden tester debug menu. Reached via 7-tap on the Version row in
    /// SettingsPanel — same affordance as Android Dev Options. Not advertised
    /// to regular users; safe to leave in shipping builds because the trigger
    /// is obscure.
    ///
    /// Tools (each requires a second confirmation tap to avoid butter-fingers):
    ///   - Reset Save     wipe PlayerPrefs save + clear FTUE + floor-XP keys
    ///   - +1000 Coins    quick economy bump for testing the shop / forge
    ///   - +5 Gems        (only if PlayerData has a gems field — gracefully skipped otherwise)
    ///   - Replay FTUE    re-trigger the WelcomePanel sequence
    ///   - Force Daily Reset    blank lastQuestResetDate / lastDailyBonusDate so next launch rolls
    ///   - Reset Migration Latch   re-run Progression.MigrateLegacyThreshold on next lobby open
    ///
    /// Diagnostic header shows live state (level/XP/streak/save size) so testers
    /// can report a clean snapshot if something looks off.
    /// </summary>
    public static class DebugPanel
    {
        private static GameObject _root;
        // Two-tap confirmation timing — first tap arms, second tap (within 2.5s) commits.
        private const float CONFIRM_WINDOW_SECONDS = 2.5f;

        public static void Show()
        {
            if (_root != null) Object.Destroy(_root);
            EnsureEventSystem();

            // Top-sort canvas — render above SettingsPanel (which opens us).
            _root = new GameObject("Sparq_DebugPanel",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var c = _root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 16000;
            foreach (var other in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort)
                    maxSort = other.sortingOrder;
            c.sortingOrder = maxSort + 20;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Dim backdrop — tap outside closes
            var dim = NewGO("Dim", _root.transform, typeof(Image), typeof(Button));
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.82f);
            dim.GetComponent<Button>().onClick.AddListener(Hide);

            // Card — red-tinted so testers can never confuse it with regular UI
            var card = NewGO("Card", _root.transform, typeof(Image));
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(940, 1700);
            card.GetComponent<Image>().color = new Color(0.20f, 0.06f, 0.08f, 1f);
            card.GetComponent<Image>().raycastTarget = true;

            // Title bar (deep red)
            var titleBar = NewGO("TitleBar", card.transform, typeof(Image));
            var tbRT = titleBar.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0, 1); tbRT.anchorMax = new Vector2(1, 1);
            tbRT.pivot = new Vector2(0.5f, 1);
            tbRT.anchoredPosition = Vector2.zero;
            tbRT.sizeDelta = new Vector2(0, 140);
            titleBar.GetComponent<Image>().color = new Color(0.75f, 0.18f, 0.18f, 1f);
            var titleTm = MakeText(titleBar.transform, "Title", "TESTER TOOLS",
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
            closeBtn.GetComponent<Image>().color = new Color(0.30f, 0.10f, 0.10f, 1f);
            var xLbl = MakeText(closeBtn.transform, "X", "X",
                52, FontStyles.Bold, Color.white);
            var xRT = xLbl.rectTransform;
            xRT.anchorMin = Vector2.zero; xRT.anchorMax = Vector2.one;
            xRT.offsetMin = Vector2.zero; xRT.offsetMax = Vector2.zero;
            xLbl.alignment = TextAlignmentOptions.Center;
            closeBtn.GetComponent<Button>().onClick.AddListener(Hide);

            // Subtitle warning strip
            var warn = MakeText(card.transform, "Warn",
                "Not for shipping users.  Tap an action TWICE to confirm.",
                22, FontStyles.Italic, new Color(1f, 0.80f, 0.40f));
            var wRT = warn.rectTransform;
            wRT.anchorMin = new Vector2(0, 1); wRT.anchorMax = new Vector2(1, 1);
            wRT.pivot = new Vector2(0.5f, 1);
            wRT.anchoredPosition = new Vector2(0, -150);
            wRT.sizeDelta = new Vector2(-60, 40);
            warn.alignment = TextAlignmentOptions.Center;

            // ── Diagnostic block ────────────────────────────────────────────
            float y = -210;
            BuildSectionLabel(card.transform, "STATE", y);
            y -= 56;
            var d = Sparq.Core.SaveService.Data;
            int canon = 0; try { canon = Sparq.Systems.Progression.XpToNext(d?.level ?? 1); } catch {}
            BuildInfoRow(card.transform, y, "Level / XP",
                d == null ? "(no save)" : $"L{d.level} — {d.currentXP}/{d.xpToNextLevel} (canon: {canon})");
            y -= 60;
            BuildInfoRow(card.transform, y, "Currency",
                d == null ? "—" : $"{d.sparqCoins} coins");
            y -= 60;
            BuildInfoRow(card.transform, y, "Streak",
                d == null ? "—" : $"{d.streak} day(s) — {d.streakShields} shields");
            y -= 60;
            int saveBytes = PlayerPrefs.GetString("sparq.save", "").Length;
            BuildInfoRow(card.transform, y, "Save size", $"{saveBytes} chars in PlayerPrefs");
            y -= 60;
            BuildInfoRow(card.transform, y, "Version", Application.version);
            y -= 78;

            // ── Action block ────────────────────────────────────────────────
            BuildSectionLabel(card.transform, "ACTIONS", y);
            y -= 56;
            BuildActionRow(card.transform, y, "Reset Save",
                "Wipes PlayerPrefs save + FTUE flag + floor-XP key. App may need to be relaunched.",
                ResetEverything);
            y -= 130;
            BuildActionRow(card.transform, y, "+1000 Coins",
                "Adds 1000 sparqCoins for shop / forge testing.",
                () => GrantCoins(1000));
            y -= 130;
            BuildActionRow(card.transform, y, "Replay FTUE",
                "Clears the FTUE-seen flag — next launch shows WelcomePanel.",
                ReplayFtue);
            y -= 130;
            BuildActionRow(card.transform, y, "Force Daily Reset",
                "Blanks lastQuestResetDate / lastDailyBonusDate so next CheckDailyReset rolls.",
                ForceDailyReset);
            y -= 130;
            BuildActionRow(card.transform, y, "Re-run Migration",
                "Resets the per-session migration latch so MigrateLegacyThreshold runs again.",
                () => { try { Sparq.Systems.Progression.ResetMigrationLatch(); Toast("Migration latch cleared — open lobby."); } catch (System.Exception ex) { Toast("Failed: " + ex.Message); } });

            Debug.Log("[DebugPanel] Opened.");
        }

        public static void Hide()
        {
            if (_root != null) { Object.Destroy(_root); _root = null; }
        }

        // ── Actions ──────────────────────────────────────────────────────────

        private static void ResetEverything()
        {
            try
            {
                Sparq.Core.SaveService.Clear();
                PlayerPrefs.DeleteKey("sparq.ftue.welcomed");
                PlayerPrefs.DeleteKey("sparq.quests.floorXpDate");
                PlayerPrefs.Save();
                try { Sparq.Systems.Progression.ResetMigrationLatch(); } catch {}
                Toast("Save reset. Relaunch the app for a clean slate.");
                Debug.Log("[DebugPanel] Save + tester PlayerPrefs keys wiped.");
            }
            catch (System.Exception ex)
            {
                Toast("Reset failed: " + ex.Message);
                Debug.LogError("[DebugPanel] Reset failed: " + ex);
            }
        }

        private static void GrantCoins(int amount)
        {
            try
            {
                var d = Sparq.Core.SaveService.Data;
                if (d == null) { Toast("No save loaded."); return; }
                d.sparqCoins += amount;
                Sparq.Core.SaveService.Save();
                Toast($"+{amount} coins → {d.sparqCoins}");
            }
            catch (System.Exception ex) { Toast("Failed: " + ex.Message); }
        }

        private static void ReplayFtue()
        {
            try
            {
                PlayerPrefs.DeleteKey("sparq.ftue.welcomed");
                PlayerPrefs.Save();
                Toast("FTUE flag cleared. Relaunch to see WelcomePanel.");
            }
            catch (System.Exception ex) { Toast("Failed: " + ex.Message); }
        }

        private static void ForceDailyReset()
        {
            try
            {
                var d = Sparq.Core.SaveService.Data;
                if (d == null) { Toast("No save loaded."); return; }
                d.lastQuestResetDate = "";
                d.lastDailyBonusDate = "";
                Sparq.Core.SaveService.Save();
                Toast("Daily reset cleared. Next CheckDailyReset will roll.");
            }
            catch (System.Exception ex) { Toast("Failed: " + ex.Message); }
        }

        // ── UI helpers ──────────────────────────────────────────────────────

        private static void BuildSectionLabel(Transform parent, string label, float y)
        {
            var tm = MakeText(parent, "Section", label,
                28, FontStyles.Bold, new Color(1f, 0.75f, 0.35f));
            var rt = tm.rectTransform;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(-60, 40);
            tm.alignment = TextAlignmentOptions.Left;
            tm.margin = new Vector4(30, 0, 0, 0);
        }

        private static void BuildInfoRow(Transform parent, float y, string label, string value)
        {
            var row = NewGO("InfoRow", parent, typeof(Image));
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(-60, 52);
            row.GetComponent<Image>().color = new Color(0.10f, 0.04f, 0.06f, 0.85f);
            row.GetComponent<Image>().raycastTarget = false;

            var lbl = MakeText(row.transform, "Label", label,
                24, FontStyles.Bold, new Color(0.95f, 0.85f, 0.85f));
            var lrt = lbl.rectTransform;
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(0, 1);
            lrt.pivot = new Vector2(0, 0.5f);
            lrt.anchoredPosition = new Vector2(20, 0);
            lrt.sizeDelta = new Vector2(280, 0);
            lbl.alignment = TextAlignmentOptions.MidlineLeft;

            var val = MakeText(row.transform, "Value", value,
                22, FontStyles.Normal, new Color(0.85f, 0.85f, 0.95f));
            var vrt = val.rectTransform;
            vrt.anchorMin = new Vector2(0, 0); vrt.anchorMax = new Vector2(1, 1);
            vrt.pivot = new Vector2(0, 0.5f);
            vrt.anchoredPosition = new Vector2(310, 0);
            vrt.sizeDelta = new Vector2(-330, 0);
            val.alignment = TextAlignmentOptions.MidlineLeft;
        }

        /// <summary>
        /// Action row with a two-tap-to-confirm pattern. First tap arms (label
        /// changes to "TAP AGAIN" for CONFIRM_WINDOW_SECONDS); second tap commits.
        /// If the window expires, the button re-arms. Each row owns its own
        /// state via a per-row MonoBehaviour because static state would cross-
        /// pollute across the multiple rows in the panel.
        /// </summary>
        private static void BuildActionRow(Transform parent, float y, string action, string subtitle, System.Action onConfirm)
        {
            var row = NewGO("ActionRow", parent, typeof(Image), typeof(Button));
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(-60, 116);
            var bg = row.GetComponent<Image>();
            bg.color = new Color(0.35f, 0.10f, 0.12f, 1f);

            var actionLbl = MakeText(row.transform, "ActionLbl", action,
                32, FontStyles.Bold, new Color(1f, 0.95f, 0.55f));
            var arRT = actionLbl.rectTransform;
            arRT.anchorMin = new Vector2(0, 0.5f); arRT.anchorMax = new Vector2(1, 1);
            arRT.pivot = new Vector2(0.5f, 1);
            arRT.anchoredPosition = new Vector2(0, -8);
            arRT.sizeDelta = new Vector2(-30, 0);
            actionLbl.alignment = TextAlignmentOptions.Center;

            var subLbl = MakeText(row.transform, "Sub", subtitle,
                18, FontStyles.Normal, new Color(0.92f, 0.82f, 0.82f));
            var srt = subLbl.rectTransform;
            srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(1, 0.5f);
            srt.pivot = new Vector2(0.5f, 0);
            srt.anchoredPosition = new Vector2(0, 8);
            srt.sizeDelta = new Vector2(-30, 0);
            subLbl.alignment = TextAlignmentOptions.Center;
            subLbl.textWrappingMode = TextWrappingModes.Normal;

            // Per-row confirmation state lives in a tiny tracker MonoBehaviour
            // attached to the row GO. The tracker times out the arm after
            // CONFIRM_WINDOW_SECONDS and resets the label.
            var tracker = row.AddComponent<ConfirmTapTracker>();
            tracker.Init(actionLbl, bg, action, onConfirm);
            row.GetComponent<Button>().onClick.AddListener(tracker.OnTap);
        }

        private static void Toast(string message)
        {
            // Toasts the user inside the debug panel — quick visual feedback
            // so the tester knows the action ran. Auto-dismiss after 3s.
            if (_root == null) return;
            var toast = NewGO("Toast", _root.transform, typeof(Image));
            var tRT = toast.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0.5f, 0); tRT.anchorMax = new Vector2(0.5f, 0);
            tRT.pivot = new Vector2(0.5f, 0);
            tRT.anchoredPosition = new Vector2(0, 180);
            tRT.sizeDelta = new Vector2(820, 100);
            toast.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.20f, 0.95f);
            var lbl = MakeText(toast.transform, "TLbl", message,
                26, FontStyles.Bold, new Color(0.95f, 1f, 0.85f));
            var lRT = lbl.rectTransform;
            lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
            lRT.offsetMin = new Vector2(20, 0); lRT.offsetMax = new Vector2(-20, 0);
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.textWrappingMode = TextWrappingModes.Normal;
            // Schedule destruction. Use a coroutine on a temp runner so we don't
            // depend on the toast itself staying alive (Hide() destroys _root).
            var runner = _root.GetComponent<ToastRunner>() ?? _root.AddComponent<ToastRunner>();
            runner.Schedule(toast, 3f);
        }

        /// <summary>
        /// Tracks two-tap confirmation on a single action row. Arms on first
        /// tap, commits on second within window, re-arms when window expires.
        /// </summary>
        private class ConfirmTapTracker : MonoBehaviour
        {
            private TMP_Text _label;
            private Image _bg;
            private string _baseText;
            private Color _baseColor;
            private System.Action _onConfirm;
            private bool _armed;
            private float _armTimeout;

            public void Init(TMP_Text label, Image bg, string baseText, System.Action onConfirm)
            {
                _label = label;
                _bg = bg;
                _baseText = baseText;
                _baseColor = bg.color;
                _onConfirm = onConfirm;
            }

            public void OnTap()
            {
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                if (_armed)
                {
                    // Commit
                    _armed = false;
                    if (_label != null) _label.text = _baseText;
                    if (_bg != null) _bg.color = _baseColor;
                    try { _onConfirm?.Invoke(); }
                    catch (System.Exception ex) { Debug.LogError("[DebugPanel] action failed: " + ex); }
                    return;
                }
                // Arm
                _armed = true;
                _armTimeout = CONFIRM_WINDOW_SECONDS;
                if (_label != null) _label.text = "TAP AGAIN TO CONFIRM";
                if (_bg != null) _bg.color = new Color(0.65f, 0.20f, 0.20f, 1f);
            }

            private void Update()
            {
                if (!_armed) return;
                _armTimeout -= Time.unscaledDeltaTime;
                if (_armTimeout <= 0f)
                {
                    _armed = false;
                    if (_label != null) _label.text = _baseText;
                    if (_bg != null) _bg.color = _baseColor;
                }
            }
        }

        /// <summary>Schedules destruction of toast GOs after a delay.</summary>
        private class ToastRunner : MonoBehaviour
        {
            public void Schedule(GameObject go, float seconds) { StartCoroutine(Run(go, seconds)); }
            private System.Collections.IEnumerator Run(GameObject go, float seconds)
            {
                yield return new WaitForSecondsRealtime(seconds);
                if (go != null) Destroy(go);
            }
        }

        // ── Procedural-UI scaffolding (mirrors the SettingsPanel pattern) ──

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
        }
    }
}
