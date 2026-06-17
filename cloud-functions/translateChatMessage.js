/**
 * translateChatMessage.js — Firebase Cloud Function for Sparq AI Auto-Translate.
 *
 * TRIGGER:
 *   HTTPS callable (POST). Called by Assets/Scripts/Systems/ChatTranslator.cs
 *   when a tester has the AI Auto-Translate toggle ON and a new incoming
 *   chat bubble is rendered.
 *
 * FLOW:
 *   1. Validate auth (caller must be a signed-in Sparq user)
 *   2. Validate body { text, targetLang }
 *   3. Call Anthropic Messages API with a translation system prompt
 *   4. Return { translatedText, sourceLang }
 *
 * WHY SERVER-SIDE:
 *   The Anthropic API key MUST NOT ship inside the APK — once the binary is
 *   reverse-engineered, anyone could rack up your bill. The key lives only
 *   on this server, set via `firebase functions:config:set anthropic.key=…`.
 *
 * DEPLOY:
 *   1. cd cloud-functions/
 *   2. npm install firebase-functions firebase-admin @anthropic-ai/sdk
 *   3. firebase functions:config:set anthropic.key="sk-ant-…"
 *   4. firebase deploy --only functions:translateChatMessage
 *   5. Copy the emitted Function URL into ChatTranslator.cs (PROXY_URL)
 *   6. Flip USE_LIVE_BACKEND = true in ChatTranslator.cs
 *
 * COST:
 *   Claude Haiku is ~$0.25 per 1M input tokens. A typical chat message
 *   plus the system prompt is ~150 tokens in / ~80 tokens out. With even
 *   the smallest model, 10k translations a day costs <$1.
 *
 * SECURITY:
 *   - Auth-gated: anonymous callers get 401
 *   - Body capped at 600 chars to prevent prompt-injection blowups
 *   - Output capped at 1500 chars
 *   - Cloud Function execution timeout set to 8s (translations should be sub-2s)
 */

const functions = require("firebase-functions");
const admin     = require("firebase-admin");
const Anthropic = require("@anthropic-ai/sdk");

if (!admin.apps.length) admin.initializeApp();

const MAX_INPUT_CHARS  = 600;
const MAX_OUTPUT_CHARS = 1500;
const MODEL_ID         = "claude-haiku-4-5-20251001"; // fast + cheap; good enough for chat

const SYSTEM_PROMPT = [
  "You are a chat-message translator for a wellness game played by young teens.",
  "Translate the user-supplied text into the target language.",
  "Return ONLY the translated text — no preamble, no quotes, no explanation.",
  "If the text is already in the target language, return it unchanged.",
  "Preserve emoji and tone (casual, friendly).",
].join(" ");

exports.translateChatMessage = functions
  .runWith({ timeoutSeconds: 8, memory: "256MB" })
  .https.onCall(async (data, context) => {
    // ── Auth gate ──
    if (!context.auth) {
      throw new functions.https.HttpsError(
        "unauthenticated",
        "Sign in before translating chat messages."
      );
    }

    // ── Input validation ──
    const text       = (data && typeof data.text === "string")       ? data.text.trim()       : "";
    const targetLang = (data && typeof data.targetLang === "string") ? data.targetLang.trim() : "en";
    if (!text) {
      throw new functions.https.HttpsError("invalid-argument", "text is required");
    }
    if (text.length > MAX_INPUT_CHARS) {
      throw new functions.https.HttpsError(
        "invalid-argument",
        `text exceeds ${MAX_INPUT_CHARS} characters`
      );
    }

    // ── Anthropic call ──
    const apiKey = functions.config().anthropic && functions.config().anthropic.key;
    if (!apiKey) {
      console.error("[translateChatMessage] anthropic.key not configured");
      throw new functions.https.HttpsError("internal", "translation service not configured");
    }
    const client = new Anthropic({ apiKey });

    let response;
    try {
      response = await client.messages.create({
        model:      MODEL_ID,
        max_tokens: 400,
        system:     SYSTEM_PROMPT,
        messages: [
          {
            role: "user",
            content: `Target language: ${targetLang}\n\n${text}`,
          },
        ],
      });
    } catch (err) {
      console.error("[translateChatMessage] Anthropic error:", err.message);
      throw new functions.https.HttpsError("internal", "translation failed");
    }

    // Extract text from response.content[0].text
    let translated = "";
    if (response && Array.isArray(response.content)) {
      for (const block of response.content) {
        if (block.type === "text" && typeof block.text === "string") {
          translated += block.text;
        }
      }
    }
    translated = translated.trim();
    if (translated.length > MAX_OUTPUT_CHARS) {
      translated = translated.substring(0, MAX_OUTPUT_CHARS);
    }
    if (!translated) {
      // Fall back to the original so the client UI is never blank.
      translated = text;
    }

    return {
      translatedText: translated,
      sourceLang:     "auto", // Anthropic doesn't return detected language; can be added by prompting it to.
    };
  });
