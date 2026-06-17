using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Wires a Send button to a TMP_InputField. On click: reads the text,
    /// clears the input, and spawns a confirmation toast.
    /// </summary>
    public class ChatSender : MonoBehaviour
    {
        [SerializeField] private TMP_InputField input;
        [SerializeField] private Button sendButton;

        private void Awake()
        {
            if (sendButton != null) sendButton.onClick.AddListener(Send);
        }

        public void Send()
        {
            if (input == null) return;
            string msg = input.text == null ? "" : input.text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            // ── RATE LIMIT ───────────────────────────────────────────────
            // Check mute + per-message throttle BEFORE running moderator
            // (so a muted user can't waste cycles trying).
            if (!Sparq.Safety.RateLimiter.CanSend(out string rateReason))
            {
                Debug.LogWarning($"[ChatSender] Rate-limited: {rateReason}");
                ShowSafetyToast(rateReason, new Color(0.85f, 0.45f, 0.30f, 1f));
                return;
            }

            // ── CONTENT MODERATION ───────────────────────────────────────
            // Inspect every outgoing message. Block predatory/phishing/
            // harassment messages outright. Sanitize PII/profanity.
            var verdict = Sparq.Safety.ContentModerator.Inspect(msg, "chat");
            if (!verdict.Allowed)
            {
                Debug.LogWarning($"[ChatSender] Blocked outgoing message: {verdict.UserFacingMessage}");
                // ThreatViolence opens the firm-tone ThreatResponsePanel
                // (911 + Crisis Text Line guidance). Everything else uses
                // the inline toast with the moderator's user-facing
                // explanation. UserFacingMessage is intentionally empty for
                // ThreatViolence — the panel speaks for itself.
                if (verdict.Reasons.Contains(Sparq.Safety.ContentModerator.Category.ThreatViolence)
                    && !Sparq.UI.ThreatResponsePanel.RecentlyDismissed())
                {
                    try { Sparq.UI.ThreatResponsePanel.Show(); } catch {}
                }
                else
                {
                    ShowSafetyToast(verdict.UserFacingMessage,
                        new Color(0.85f, 0.30f, 0.30f, 1f));
                }
                // Don't clear input — let user revise. But hide PII just in
                // case the field is screen-shared.
                input.text = verdict.SanitizedText;

                // If the BLOCKED message ALSO contained self-harm ideation,
                // the user reaching out matters more than the block — show
                // crisis resources before returning.
                if (verdict.Reasons.Contains(Sparq.Safety.ContentModerator.Category.SelfHarmIdeation)
                    && !Sparq.UI.CrisisResourcesPanel.RecentlyDismissed())
                {
                    try { Sparq.UI.CrisisResourcesPanel.Show(); } catch {}
                }
                return;
            }
            // Warn-level (profanity, etc.) — send sanitized version
            msg = verdict.SanitizedText;
            if (verdict.Severity == Sparq.Safety.ContentModerator.Severity.Warn &&
                !string.IsNullOrEmpty(verdict.UserFacingMessage))
            {
                ShowSafetyToast(verdict.UserFacingMessage,
                    new Color(1f, 0.78f, 0.30f, 1f));
            }

            input.text = "";
            input.ActivateInputField();
            Sparq.Safety.RateLimiter.RecordSend();

            // ── CRISIS RESOURCES ─────────────────────────────────────────
            // If the user's message expressed self-harm ideation, surface
            // help resources. The message has ALREADY been sent (their
            // friend might be the help they need); this is purely a
            // supportive add-on, NOT a punishment.
            if (verdict.Reasons.Contains(Sparq.Safety.ContentModerator.Category.SelfHarmIdeation)
                && !Sparq.UI.CrisisResourcesPanel.RecentlyDismissed())
            {
                try { Sparq.UI.CrisisResourcesPanel.Show(); }
                catch (System.Exception ex)
                { Debug.LogError($"[ChatSender] Crisis panel open failed: {ex.Message}"); }
            }

            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}

            // Toast confirming send
            try
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                    XPFloater.Spawn(canvas.transform,
                        sendButton.transform.position + new Vector3(0, 60, 0),
                        $"Sent: {(msg.Length > 18 ? msg.Substring(0, 18) + "…" : msg)}",
                        new Color(1f, 0.82f, 0.32f));
            }
            catch {}
        }

        private void ShowSafetyToast(string text, Color color)
        {
            try
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                    XPFloater.Spawn(canvas.transform,
                        sendButton.transform.position + new Vector3(0, 90, 0),
                        text, color);
            }
            catch {}
        }
    }
}
