using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// A real working chat tab — not the static prefab.
    /// Header + scrollable message list + working input + Send.
    /// Send appends a new bubble at the bottom; auto-scrolls to show it.
    /// </summary>
    public class LiveChatTab : MonoBehaviour
    {
        [SerializeField] private TMP_InputField input;
        [SerializeField] private Button         sendBtn;
        [SerializeField] private RectTransform  messageList;     // VerticalLayoutGroup parent
        [SerializeField] private ScrollRect     scrollRect;
        [SerializeField] private TMP_FontAsset  font;

        // Palette
        private static readonly Color GOLD       = new Color(1.00f, 0.78f, 0.22f);
        private static readonly Color CREAM      = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.08f, 0.18f);
        private static readonly Color BUBBLE_BG  = new Color(0.22f, 0.16f, 0.34f);

        private void Start()
        {
            if (sendBtn != null) sendBtn.onClick.AddListener(OnSend);
            if (font == null) font = TMP_Settings.defaultFontAsset;
        }

        public void OnSend()
        {
            if (input == null) return;
            string txt = input.text == null ? "" : input.text.Trim();
            if (string.IsNullOrEmpty(txt)) return;

            // ── RATE LIMIT + CONTENT MODERATION ──────────────────────────
            // Same pipeline as ChatSender / WorldPanel / ChatPanel — every
            // outgoing chat surface goes through the same gates so a player
            // can't bypass moderation by picking a different chat surface.
            if (!Sparq.Safety.RateLimiter.CanSend(out string rateReason))
            {
                Debug.LogWarning($"[LiveChatTab] Rate-limited: {rateReason}");
                return;
            }
            var verdict = Sparq.Safety.ContentModerator.Inspect(txt, "chat");
            if (!verdict.Allowed)
            {
                Debug.LogWarning($"[LiveChatTab] Blocked outgoing message: {verdict.UserFacingMessage}");
                if (verdict.Reasons.Contains(Sparq.Safety.ContentModerator.Category.ThreatViolence)
                    && !Sparq.UI.ThreatResponsePanel.RecentlyDismissed())
                { try { Sparq.UI.ThreatResponsePanel.Show(); } catch {} }
                if (verdict.Reasons.Contains(Sparq.Safety.ContentModerator.Category.SelfHarmIdeation)
                    && !Sparq.UI.CrisisResourcesPanel.RecentlyDismissed())
                { try { Sparq.UI.CrisisResourcesPanel.Show(); } catch {} }
                // Don't clear — let user revise. Hide PII via sanitized text.
                input.text = verdict.SanitizedText ?? "";
                return;
            }
            // Warn-level: send sanitized version (profanity → ***).
            txt = verdict.SanitizedText ?? txt;

            AppendMessage("You", txt, true);
            input.text = "";
            input.ActivateInputField();
            Sparq.Safety.RateLimiter.RecordSend();

            // Crisis-resources auto-popup on a Clean message that still
            // contained self-harm ideation (ideation alone is Clean — the
            // panel is supportive, not punitive).
            if (verdict.Reasons.Contains(Sparq.Safety.ContentModerator.Category.SelfHarmIdeation)
                && !Sparq.UI.CrisisResourcesPanel.RecentlyDismissed())
            { try { Sparq.UI.CrisisResourcesPanel.Show(); } catch {} }

            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
        }

        public void AppendMessage(string author, string text, bool fromMe)
        {
            if (messageList == null) return;

            // ── BLOCK LIST ───────────────────────────────────────────────
            // Hide messages from anyone the user has blocked. Outgoing
            // messages (fromMe=true) are never blocked.
            if (!fromMe && Sparq.Safety.BlockList.IsBlocked(author))
            {
                Debug.Log($"[LiveChatTab] Suppressed message from blocked user '{author}'.");
                return;
            }

            // ── MULTI-TURN GROOMING ──────────────────────────────────────
            // Record every incoming message into the per-sender conversation
            // window. A patient predator keeps each message individually
            // clean — the risk only shows up in the aggregate. The warning
            // (if any) is spawned below, AFTER this message bubble, so the
            // chat reads in order.
            Sparq.Safety.ConversationTracker.ConversationVerdict convoVerdict = null;
            if (!fromMe)
                convoVerdict = Sparq.Safety.ConversationTracker.RecordIncoming(author, text);

            var row = new GameObject($"Msg_{author}", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(messageList, false);
            row.GetComponent<LayoutElement>().preferredHeight = 60;

            // Bubble
            var bubble = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
            bubble.transform.SetParent(row.transform, false);
            var brt = bubble.GetComponent<RectTransform>();
            float w = Mathf.Min(560f, 100f + text.Length * 11f);
            brt.anchorMin = new Vector2(fromMe ? 1 : 0, 0); brt.anchorMax = new Vector2(fromMe ? 1 : 0, 1);
            brt.pivot     = new Vector2(fromMe ? 1 : 0, 0.5f);
            brt.anchoredPosition = new Vector2(fromMe ? -16 : 16, 0);
            brt.sizeDelta = new Vector2(w, -8);
            bubble.GetComponent<Image>().color = fromMe ? GOLD : BUBBLE_BG;

            // Author tag
            var auth = new GameObject("Author", typeof(RectTransform));
            auth.transform.SetParent(bubble.transform, false);
            var art = auth.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0, 1); art.anchorMax = new Vector2(1, 1);
            art.pivot = new Vector2(0.5f, 1);
            art.anchoredPosition = new Vector2(0, -2);
            art.sizeDelta = new Vector2(-16, 14);
            var atm = auth.AddComponent<TextMeshProUGUI>();
            atm.text = author;
            atm.fontSize = 11;
            atm.fontStyle = FontStyles.Bold;
            atm.color = fromMe ? new Color(0.3f, 0.2f, 0.05f) : GOLD;
            atm.alignment = TextAlignmentOptions.MidlineLeft;
            atm.font = font;
            atm.raycastTarget = false;

            // Body
            var body = new GameObject("Body", typeof(RectTransform));
            body.transform.SetParent(bubble.transform, false);
            var bdrt = body.GetComponent<RectTransform>();
            bdrt.anchorMin = new Vector2(0, 0); bdrt.anchorMax = new Vector2(1, 1);
            bdrt.offsetMin = new Vector2(12, 4); bdrt.offsetMax = new Vector2(-12, -16);
            var btm = body.AddComponent<TextMeshProUGUI>();
            btm.text = text;
            btm.fontSize = 16;
            btm.color = fromMe ? DEEP_NAVY : CREAM;
            btm.alignment = TextAlignmentOptions.MidlineLeft;
            btm.font = font;
            btm.textWrappingMode = TextWrappingModes.Normal;
            btm.raycastTarget = false;

            // Report button — only on INCOMING messages (don't report yourself)
            if (!fromMe)
            {
                var reportBtn = new GameObject("Report",
                    typeof(RectTransform), typeof(Image), typeof(Button));
                reportBtn.transform.SetParent(bubble.transform, false);
                var rbRT = reportBtn.GetComponent<RectTransform>();
                rbRT.anchorMin = new Vector2(1, 1); rbRT.anchorMax = new Vector2(1, 1);
                rbRT.pivot = new Vector2(1, 1);
                rbRT.anchoredPosition = new Vector2(-2, -2);
                rbRT.sizeDelta = new Vector2(28, 18);
                reportBtn.GetComponent<Image>().color = new Color(0, 0, 0, 0.35f);
                var dotsGO = new GameObject("Dots", typeof(RectTransform));
                dotsGO.transform.SetParent(reportBtn.transform, false);
                var dRT = dotsGO.GetComponent<RectTransform>();
                dRT.anchorMin = Vector2.zero; dRT.anchorMax = Vector2.one;
                dRT.offsetMin = Vector2.zero; dRT.offsetMax = Vector2.zero;
                var dTm = dotsGO.AddComponent<TextMeshProUGUI>();
                dTm.text = "⋯";
                dTm.fontSize = 14;
                dTm.fontStyle = FontStyles.Bold;
                dTm.color = Color.white;
                dTm.font = font;
                dTm.alignment = TextAlignmentOptions.Center;
                dTm.raycastTarget = false;

                // Capture for closure
                string capturedAuthor = author;
                string capturedText   = text;
                reportBtn.GetComponent<Button>().onClick.AddListener(() => {
                    try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                    try { Sparq.UI.ReportPanel.Show(capturedAuthor, capturedText); }
                    catch (System.Exception ex) { Debug.LogError($"[LiveChatTab] Report open failed: {ex.Message}"); }
                });
            }

            // ── MULTI-TURN GROOMING WARNING ──────────────────────────────
            // First time this sender's CONVERSATION (not any single message)
            // crosses HIGH grooming risk: auto-flag for moderators + warn the
            // user inline. FirstTimeHigh de-dupes so we warn once per sender.
            if (convoVerdict != null &&
                convoVerdict.Level == Sparq.Safety.ConversationTracker.RiskLevel.High &&
                convoVerdict.FirstTimeHigh)
            {
                Debug.LogWarning($"[LiveChatTab] Conversation-level HIGH risk from '{author}': " +
                                 $"risk={convoVerdict.CompositeRisk:F2} " +
                                 $"signals=[{string.Join(",", convoVerdict.TopSignals)}]");
                try
                {
                    Sparq.Safety.ModerationQueue.AutoFlag(author,
                        "[multi-message grooming pattern] latest: " + text,
                        "GroomingPattern(conversation):" + string.Join("|", convoVerdict.TopSignals));
                }
                catch (System.Exception ex)
                { Debug.LogError($"[LiveChatTab] Conversation auto-flag failed: {ex.Message}"); }
                SpawnConversationWarning(author);
            }

            // Auto-scroll to bottom
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
        }

        // A full-width warning banner inserted into the chat stream when a
        // sender's CONVERSATION (across messages) crosses HIGH grooming risk —
        // the multi-turn pattern that single-message keyword checks miss.
        // Tapping it opens the report dialog for that sender.
        private void SpawnConversationWarning(string author)
        {
            if (messageList == null) return;

            var row = new GameObject("SafetyWarning", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(messageList, false);
            row.GetComponent<LayoutElement>().preferredHeight = 96;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Button));
            panel.transform.SetParent(row.transform, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 0); prt.anchorMax = new Vector2(1, 1);
            prt.offsetMin = new Vector2(16, 4); prt.offsetMax = new Vector2(-16, -4);
            panel.GetComponent<Image>().color = new Color(0.85f, 0.28f, 0.24f, 0.97f);

            var txtGO = new GameObject("Txt", typeof(RectTransform));
            txtGO.transform.SetParent(panel.transform, false);
            var trt = txtGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(14, 6); trt.offsetMax = new Vector2(-14, -6);
            var tm = txtGO.AddComponent<TextMeshProUGUI>();
            tm.text = $"Safety check: messages from \"{author}\" show an unsafe pattern " +
                      "over this conversation. Tap to report — or block them.";
            tm.fontSize = 14;
            tm.fontStyle = FontStyles.Bold;
            tm.color = Color.white;
            tm.alignment = TextAlignmentOptions.MidlineLeft;
            tm.font = font;
            tm.textWrappingMode = TextWrappingModes.Normal;
            tm.raycastTarget = false;

            string capturedAuthor = author;
            panel.GetComponent<Button>().onClick.AddListener(() => {
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                try { Sparq.UI.ReportPanel.Show(capturedAuthor, "(multi-message conversation pattern)"); }
                catch (System.Exception ex)
                { Debug.LogError($"[LiveChatTab] Report open failed: {ex.Message}"); }
            });

            Debug.LogWarning($"[LiveChatTab] Spawned conversation-safety warning for '{author}'.");
        }
    }
}
