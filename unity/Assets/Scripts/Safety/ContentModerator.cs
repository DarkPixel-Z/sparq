using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Sparq.Safety
{
    /// <summary>
    /// Central content moderation for ALL user-generated text in Sparq:
    /// chat, usernames, custom quest titles, profile fields, etc.
    ///
    /// Built for an ADHD-friendly RPG that may have young/vulnerable users.
    /// Layered defense:
    ///
    ///   LAYER 1 - PII / Personal Info Detection
    ///       Block phone numbers, emails, addresses, ages, real names
    ///       (children should never share these in chat)
    ///
    ///   LAYER 2 - Profanity / Slurs / Sexual Content
    ///       Hard-coded deny list of explicit terms
    ///
    ///   LAYER 3 - Predator Grooming Patterns
    ///       "How old are you", "secret", "don't tell", "send pics",
    ///       isolation patterns, gift-offering patterns
    ///
    ///   LAYER 4 - Phishing / Scam Detection
    ///       External URLs (only whitelisted Sparq domains allowed),
    ///       "free coins/gems/skins", credential requests, impersonation
    ///
    ///   LAYER 5 - Harassment / Bullying
    ///       Targeted insults, "kill yourself" patterns, identity-based slurs
    ///
    /// API:
    ///   var verdict = ContentModerator.Inspect("text", contextHint);
    ///   if (!verdict.Allowed) { /* show warning, log, etc. */ }
    ///   string safe = verdict.SanitizedText;   // **** replacement of bad parts
    /// </summary>
    public static class ContentModerator
    {
        public enum Severity { Clean, Warn, Block }

        public enum Category
        {
            None,
            PII_Phone, PII_Email, PII_Address, PII_Age, PII_RealName,
            Profanity, Slur, SexualContent,
            GroomingPattern, IsolationPattern, GiftLure,
            PhishingURL, ScamLure, CredentialRequest, Impersonation,
            Harassment, SelfHarmDirective,
            SelfHarmIdeation,      // USER expressing self-harm thoughts → triage, don't punish
            ThreatViolence,        // USER threatening someone else / mass-harm / weapons → block + flag
            SlangPattern, EvasiveSpelling, SuggestiveEmoji,
            OffPlatformLure,        // "add me on snap" — predator red flag
            SexualSlang,            // "dtf", "smash", "thicc", "rizz" (sexual)
            DrugSlang,              // "420", "plug", "molly", "lean"
            BullyingSlang,          // "skill issue", "cope", "touch grass", "mid"
            PredatorEndearment,     // "babygirl", "princess", "daddy" toward minors
            UnicodeBypass,          // zero-width chars, cyrillic lookalikes
        }

        public class Verdict
        {
            public bool Allowed;
            public Severity Severity;
            public List<Category> Reasons = new List<Category>();
            public string OriginalText;
            public string SanitizedText;
            public string UserFacingMessage;
        }

        // ─────────────────────────────────────────────────────────────────
        // CONFIG — domain whitelist, age limits, etc.
        // ─────────────────────────────────────────────────────────────────
        private static readonly string[] ALLOWED_DOMAINS = new[]
        {
            "sparqgame.com", "sparq.app",          // (placeholders — replace with real)
            "anthropic.com",                        // tooling, not user
        };

        // ─────────────────────────────────────────────────────────────────
        // LAYER 1 - PII Detection
        // ─────────────────────────────────────────────────────────────────
        // Phone: 10+ digits, optional formatting
        private static readonly Regex PhoneRx = new Regex(
            @"(?:\+?\d{1,3}[\s\-\.]?)?(?:\(?\d{3}\)?[\s\-\.]?)\d{3}[\s\-\.]?\d{4}",
            RegexOptions.Compiled);

        // Email
        private static readonly Regex EmailRx = new Regex(
            @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
            RegexOptions.Compiled);

        // Address: rough — numbers followed by street/avenue/road
        private static readonly Regex AddressRx = new Regex(
            @"\b\d{1,5}\s+\w+\s+(?:street|st|avenue|ave|road|rd|drive|dr|lane|ln|blvd|boulevard|way|court|ct|circle|cir|place|pl)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Age sharing: "I'm 12", "I am 13 years old"
        private static readonly Regex AgeRx = new Regex(
            @"\b(?:i'?m|i am|im)\s+(?:a\s+)?(\d{1,2})\s*(?:years?|yrs?|y\.?o\.?|year old)?\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // ─────────────────────────────────────────────────────────────────
        // LAYER 2 - Profanity / Slurs / Sexual content
        // ─────────────────────────────────────────────────────────────────
        // Curated, NOT exhaustive — production should pull from a maintained
        // list service. This catches the most common shipping-blockers.
        private static readonly HashSet<string> ProfanitySet = new HashSet<string>(
            System.StringComparer.OrdinalIgnoreCase)
        {
            // Strong profanity (kid-game context = blocked)
            "fuck", "shit", "bitch", "asshole", "bastard", "damn", "hell",
            "dick", "cock", "pussy", "tits", "boobs",
            // Sexual content
            "sex", "porn", "nude", "naked", "horny",
            // Variants
            "f*ck", "sh*t", "b*tch", "fck", "sht",
        };

        private static readonly HashSet<string> SlurSet = new HashSet<string>(
            System.StringComparer.OrdinalIgnoreCase)
        {
            // Identity-based slurs — automatic ban candidates
            // (kept minimal to avoid this comment appearing in version control;
            // the patterns are recognized via regex from a runtime-loaded list)
            "retard", "retarded", "gay" /* as slur */, "fag", "faggot",
            "tranny", "dyke",
        };

        // ─────────────────────────────────────────────────────────────────
        // LAYER 3 - Grooming patterns
        // ─────────────────────────────────────────────────────────────────
        private static readonly string[] GroomingPhrases = new[]
        {
            "how old are you", "what's your age", "are you a kid", "are you a girl",
            "are you alone", "are you home alone", "where do you live",
            "don't tell your parents", "don't tell anyone", "our secret",
            "keep this between us", "let's chat in private", "let's talk on",
            "what app do you use", "add me on snap", "add me on insta",
            "send a pic", "send pics", "send a picture", "send a selfie",
            "you're so mature", "you look older",
        };

        private static readonly string[] GiftLurePhrases = new[]
        {
            "free coins", "free gems", "free vbucks", "free robux",
            "free skin", "free skins", "free account", "i'll give you",
            "i'll buy you", "i can give you", "want some gems",
        };

        // ─────────────────────────────────────────────────────────────────
        // LAYER 4 - Phishing / Scam
        // ─────────────────────────────────────────────────────────────────
        private static readonly Regex UrlRx = new Regex(
            @"(?:https?://|www\.)\S+|\b[a-zA-Z0-9\-]+\.(?:com|net|org|io|app|gg|xyz|me|cc|tv|ly)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly string[] CredRequestPhrases = new[]
        {
            "what's your password", "tell me your password", "verify your account",
            "login link", "click this link", "send me your code",
            "what's your 2fa", "your verification code",
        };

        private static readonly string[] ImpersonationPhrases = new[]
        {
            "i'm a sparq admin", "i am from sparq", "official sparq",
            "this is moderator", "i'm a mod", "i work for sparq",
        };

        // ─────────────────────────────────────────────────────────────────
        // SLANG / SHORTHAND — common texting abbreviations that bypass filters
        // ─────────────────────────────────────────────────────────────────
        // Map abbreviation → expanded form (used for normalization before checks)
        private static readonly Dictionary<string, string> SlangExpansions = new Dictionary<string, string>
        {
            // Classic predator openers
            { "asl",   "age sex location" },     // "asl?" = classic predator opening
            { "wyd",   "what you doing" },
            { "wya",   "where you at" },
            { "hru",   "how are you" },
            // Off-platform lures
            { "snap",  "snapchat" },             // "snap me?" → off-platform move
            { "dm",    "direct message" },
            { "dms",   "direct messages" },
            // Sexual shorthand
            { "pic",   "picture" },
            { "pics",  "pictures" },
            { "pix",   "pictures" },             // evasive spelling
            { "selfie","picture" },
            { "selfies","pictures" },
            { "nudes", "nude pictures" },
            // Self-harm shorthand
            { "kys",   "kill yourself" },
            { "kms",   "kill myself" },
            // Profanity shorthand
            { "wtf",   "fuck" },
            { "stfu",  "shut the fuck up" },
            { "af",    "as fuck" },
            { "gtfo",  "fuck" },
        };

        // Phrases that strongly suggest moving to another platform (predator red flag)
        private static readonly string[] OffPlatformLurePhrases = new[]
        {
            "snapchat", "snap me", "add me on snap", "what's your snap",
            "discord", "add me on discord", "telegram", "whatsapp",
            "instagram", "insta", "kik", "wickr", "signal",
            "text me", "call me", "facetime",
            "let's talk somewhere else", "off the app", "outside the game",
        };

        // ─────────────────────────────────────────────────────────────────
        // SEXUAL SLANG — hard block (kid-safe game context)
        // ─────────────────────────────────────────────────────────────────
        // Internet/text slang that is sexually explicit or strongly suggestive.
        // Hard-blocks because there's no non-sexual reading in chat context.
        private static readonly HashSet<string> SexualSlangSet = new HashSet<string>(
            System.StringComparer.OrdinalIgnoreCase)
        {
            "dtf",       // down to fuck
            "fwb",       // friends with benefits
            "nsfw",      // not safe for work — sexual content tag
            "smash",     // have sex with (Gen Z slang)
            "smashable",
            "thicc",     // body shape (sexualized usage)
            "thick",     // same
            "hookup", "hook up", "hooking up",
            "bj", "bbj",
            "thot",      // sexually derogatory
            "milf", "dilf",
            "rizz",      // smooth-talking (often sexual context with minors)
            "rizzler",
            "freaky", "freak in the sheets",
            "horndog", "horny",
            "down bad",  // sexually desperate
            "simp",      // when used to sexualize someone
            "spicy",     // "spicy pics" / "spicy content"
            "lewd", "ecchi",
            "onlyfans", "of",     // platform for adult content
        };

        // ─────────────────────────────────────────────────────────────────
        // DRUG SLANG — hard block (kid-safe game context)
        // ─────────────────────────────────────────────────────────────────
        private static readonly HashSet<string> DrugSlangSet = new HashSet<string>(
            System.StringComparer.OrdinalIgnoreCase)
        {
            "weed", "pot", "marijuana", "ganja", "kush", "mary jane",
            "420", "blunt", "joint", "bong",
            "molly", "ecstasy", "mdma",
            "coke", "cocaine", "blow",
            "meth", "crystal", "tina",
            "lean", "sizzurp", "purple drank",
            "shrooms", "mushrooms", "psilocybin",
            "acid", "lsd", "tabs",
            "xan", "xans", "xanax", "bars",
            "perc", "percs", "percocet",
            "plug",            // dealer (when used in chat with offering context)
            "plug me",         // "got a plug" / "plug me up"
            "got a plug",
            "dealer",
        };

        // ─────────────────────────────────────────────────────────────────
        // BULLYING SLANG — flagged for harassment
        // ─────────────────────────────────────────────────────────────────
        // Modern bullying often hides behind ironic Gen Z slang. Each of these
        // is a put-down/dismissal that adds up to harassment.
        private static readonly string[] BullyingPhrases = new[]
        {
            "skill issue",     // dismissive put-down
            "cope",            // when telling someone to "cope harder"
            "cope harder",
            "touch grass",     // tells someone they're chronically online
            "ratio",           // calling out unpopular opinion
            "ratio'd",
            "you're mid",      // calling someone mediocre
            "you are mid",
            "ur mid",
            "cooked",          // "you're cooked" = you're finished/loser
            "mald",            // "you're malding" = bald with anger
            "cringe",          // when directed at a person
            "cringey", "cringy",
            "cope and seethe",
            "stay mad",
            "get rekt",
            "git gud",
            "uninstall",       // "go uninstall" — tell-them-to-quit bullying
            "go cry",
            "cry about it",
            "cry more",
            "salty",
            "no one cares",
            "nobody cares",
            "no one asked",
            "didn't ask",
            "+ ratio",
            "L bozo",
            "ratio + L",
        };

        // ─────────────────────────────────────────────────────────────────
        // PREDATOR ENDEARMENT — pet names used by predators on minors
        // ─────────────────────────────────────────────────────────────────
        // Innocent in some contexts (parents, partners). In a stranger-chat
        // toward a kid-game context, these are heavily correlated with grooming.
        private static readonly string[] PredatorEndearmentPhrases = new[]
        {
            "babygirl", "baby girl",
            "babyboy", "baby boy",
            "princess",       // when addressed to a stranger/minor
            "lil mama", "little mama",
            "lil one",
            "sweetie pie", "sweetie",
            "sweetheart",
            "honey",
            "sugar",
            "doll",
            "kitten",
            "good girl", "good boy",
            "my girl", "my boy",
            "daddy",          // when said to/from a minor in chat
            "mommy",          // same
            "your daddy",
            "call me daddy",
        };

        // ─────────────────────────────────────────────────────────────────
        // EMOJI DENY LIST — sexual/violence/substance emoji
        // ─────────────────────────────────────────────────────────────────
        // Single emoji isn't enough to block (🍆 is just an eggplant) but in
        // any chat context with grooming/sexual cues it's a red flag.
        private static readonly HashSet<string> SuggestiveEmojis = new HashSet<string>
        {
            "🍆", "🍑", "💦", "👅", "🤤", "😈", "🍒",     // commonly sexual
            "🔪", "🗡️", "🩸",                              // violence
            "💊", "💉", "🚬", "🍷", "🍺", "🍾",            // substances
        };

        // Emoji that are a HARD block (no innocent reading)
        private static readonly HashSet<string> ForbiddenEmojis = new HashSet<string>
        {
            "🖕",   // middle finger
        };

        // ─────────────────────────────────────────────────────────────────
        // LEETSPEAK / EVASIVE SPELLING — l33t and char substitution
        // ─────────────────────────────────────────────────────────────────
        // 1→i, 3→e, 4→a, 5→s, 0→o, @→a, $→s
        private static readonly Dictionary<char, char> LeetMap = new Dictionary<char, char>
        {
            { '0', 'o' }, { '1', 'i' }, { '3', 'e' }, { '4', 'a' },
            { '5', 's' }, { '7', 't' }, { '@', 'a' }, { '$', 's' },
            { '!', 'i' },
        };

        // ─────────────────────────────────────────────────────────────────
        // LAYER 5 - Harassment / Self-harm directives
        // ─────────────────────────────────────────────────────────────────
        private static readonly string[] HarassmentPhrases = new[]
        {
            "kill yourself", "kys", "go die", "you should die",
            "nobody likes you", "you're worthless", "you're trash",
            "you suck",
        };

        // ─────────────────────────────────────────────────────────────────
        // SELF-HARM IDEATION — user expressing thoughts about THEMSELVES.
        // ─────────────────────────────────────────────────────────────────
        // CRITICAL: these phrases mean the user is reaching out, NOT
        // attacking someone. Detection MUST NOT block them, count as a
        // strike, or send to moderation queue as a violation. Instead we
        // surface a crisis-resources panel. The original message still
        // sends (their friend in chat might be the help they need).
        private static readonly string[] SelfHarmIdeationPhrases = new[]
        {
            // Suicidal ideation (first-person)
            "i want to die", "i wanna die", "i want to kill myself",
            "i'm going to kill myself", "im going to kill myself",
            "kill myself", "kms",
            "end it all", "end my life", "end myself",
            "don't want to be here", "dont want to be here",
            "don't want to live", "dont want to live",
            "wish i was dead", "wish i were dead",
            "no reason to live", "nothing to live for",
            "no one would miss me", "nobody would miss me",
            "no one cares about me", "nobody cares about me",
            "better off without me", "everyone's better off without me",
            // Self-injury
            "cut myself", "cutting myself", "hurt myself", "hurting myself",
            "i'm cutting", "i cut",
            // Hopelessness markers (lower confidence — flagged but gentler)
            "can't do this anymore", "cant do this anymore",
            "can't take it anymore", "cant take it anymore",
            "i give up", "i'm giving up", "im giving up",
            "i hate myself", "i hate my life",
        };

        // ─────────────────────────────────────────────────────────────────
        // THREATS OF VIOLENCE — user threatening someone ELSE.
        // ─────────────────────────────────────────────────────────────────
        // Distinct from SelfHarmIdeation (self-directed → supportive response)
        // and Harassment (directives like "kys" → block + warn). This catches
        // mass-harm patterns + weapon-laden direct threats: "shoot up the
        // school", "I have a gun", "going to kill her".
        //
        // Design notes:
        //   - Patterns require BOTH a verb-of-harm AND a target / weapon —
        //     "kill" alone isn't a threat ("kill it on stage"); "kill him"
        //     is. Weapon nouns alone ("I have a gun") DO trigger because
        //     they're rarely benign in a teen chat context.
        //   - First-person self-harm is exempted: "going to kill myself"
        //     would also match "going to kill" but SelfHarmIdeationPhrases
        //     catches it first; the order of checks in Inspect() matters.
        //   - Targets explicitly include schools — the highest-risk public
        //     threat pattern for the teen audience.
        //   - When this fires, the message is BLOCKED (never reaches chat),
        //     flagged to ModerationQueue, and a ThreatResponsePanel surfaces
        //     a firm-but-supportive message with 988 + 911 guidance.
        private static readonly string[] ThreatViolencePhrases = new[]
        {
            // Shooting threats
            "going to shoot", "gonna shoot", "i'll shoot", "i will shoot",
            "shoot up the school", "shoot up my school", "shoot up the class",
            "shoot up a school", "school shooter", "school shooting",
            // Stabbing / cutting threats (directed)
            "going to stab", "gonna stab", "i'll stab", "i will stab",
            // Kill threats (target-directed — myself excluded via SelfHarm catch first)
            "going to kill you", "gonna kill you",
            "going to kill him", "gonna kill him",
            "going to kill her", "gonna kill her",
            "going to kill them", "gonna kill them",
            "going to kill everyone", "gonna kill everyone",
            "i'll kill you", "i will kill you",
            "i'll kill him", "i will kill him",
            "i'll kill her", "i will kill her",
            "i'll kill them", "i will kill them",
            // Hurt threats (directed)
            "going to hurt you", "gonna hurt you",
            "going to hurt him", "gonna hurt him",
            "going to hurt her", "gonna hurt her",
            "going to hurt them", "gonna hurt them",
            "i'll hurt you", "i will hurt you",
            // Bombs / explosives
            "going to blow up", "gonna blow up",
            "blow up the school", "blow up my school", "blow up the building",
            "make a bomb", "making a bomb", "build a bomb", "plant a bomb",
            "i have a bomb", "ive got a bomb", "i've got a bomb", "got a bomb",
            // Arson
            "burn down the school", "burn down my school", "burn it all down",
            // Weapon possession claims (high-signal in a teen chat context)
            "i have a gun", "ive got a gun", "i've got a gun", "i got a gun",
            "bring a gun to school", "bringing a gun to school",
            "bring a gun", "bringing a gun",
            "i have a knife", "ive got a knife", "i've got a knife",
            "bring a knife to school", "bringing a knife to school",
        };

        // ─────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Inspect text. Returns a Verdict describing what (if anything)
        /// was flagged. Caller decides what to do based on .Severity.
        /// </summary>
        public static Verdict Inspect(string text, string contextHint = "chat")
        {
            var v = new Verdict
            {
                OriginalText = text ?? "",
                SanitizedText = text ?? "",
                Allowed = true,
                Severity = Severity.Clean,
            };
            if (string.IsNullOrWhiteSpace(text)) return v;

            // ── NORMALIZE before checks ──
            // Build a normalized version that's run through ALL the same
            // checks. This catches leetspeak ("f*ck", "fck", "ph0ne") and
            // slang ("asl?", "wyd", "snap me") that would bypass plain matches.
            string normalized = Normalize(text);

            // ── L0: Slang / off-platform lure / emoji ──
            string lowerNorm = normalized.ToLower();
            foreach (var p in OffPlatformLurePhrases)
                if (lowerNorm.Contains(p)) { v.Reasons.Add(Category.OffPlatformLure); break; }

            // Emoji checks — run on ORIGINAL text (not normalized) since
            // emoji survive normalization unchanged anyway.
            int suggestiveEmojiCount = 0;
            foreach (var e in SuggestiveEmojis) if (text.Contains(e)) suggestiveEmojiCount++;
            foreach (var e in ForbiddenEmojis)
                if (text.Contains(e)) { v.Reasons.Add(Category.Harassment); v.SanitizedText = v.SanitizedText.Replace(e, "*"); }
            // Suggestive emoji ALONE isn't enough (eggplant in a recipe).
            // 2+ together OR 1+ paired with grooming text → flag.
            if (suggestiveEmojiCount >= 2) v.Reasons.Add(Category.SuggestiveEmoji);

            // Tag if normalized version contains slang expansions that surfaced new content
            // (e.g. "wyd" → "what you doing" — by itself fine, but combined with
            // PII requests or off-platform it's clearly predatory).
            foreach (var kv in SlangExpansions)
            {
                // Match slang as whole word
                if (Regex.IsMatch(text, @"\b" + Regex.Escape(kv.Key) + @"\b", RegexOptions.IgnoreCase))
                {
                    // ASL is a hard flag — its only common use is predator opener
                    if (kv.Key == "asl") { v.Reasons.Add(Category.GroomingPattern); }
                    else v.Reasons.Add(Category.SlangPattern);
                    break;
                }
            }

            // ── L1: PII ──
            if (PhoneRx.IsMatch(text))    { v.Reasons.Add(Category.PII_Phone);    v.SanitizedText = PhoneRx.Replace(v.SanitizedText, "[phone hidden]"); }
            if (EmailRx.IsMatch(text))    { v.Reasons.Add(Category.PII_Email);    v.SanitizedText = EmailRx.Replace(v.SanitizedText, "[email hidden]"); }
            if (AddressRx.IsMatch(text))  { v.Reasons.Add(Category.PII_Address);  v.SanitizedText = AddressRx.Replace(v.SanitizedText, "[address hidden]"); }
            var ageMatch = AgeRx.Match(text);
            if (ageMatch.Success)
            {
                v.Reasons.Add(Category.PII_Age);
                v.SanitizedText = AgeRx.Replace(v.SanitizedText, "[age hidden]");
            }

            // Cache the lowercased original (used by several layers below)
            string lower = text.ToLower();

            // ── L2: Profanity / slurs (check both original AND normalized) ──
            CheckWords(lower, v);
            CheckWords(normalized.ToLower(), v);

            // ── L2b: Sexual/Drug slang word checks ──
            CheckSlangSets(lower, v);
            CheckSlangSets(normalized.ToLower(), v);

            // ── L2c: Bullying / Predator endearment phrase checks ──
            foreach (var p in BullyingPhrases)
                if (lower.Contains(p) || lowerNorm.Contains(p))
                { v.Reasons.Add(Category.BullyingSlang); break; }
            foreach (var p in PredatorEndearmentPhrases)
                if (lower.Contains(p) || lowerNorm.Contains(p))
                { v.Reasons.Add(Category.PredatorEndearment); break; }

            // ── L3: Grooming + gift lure (check both original AND normalized) ──
            foreach (var g in GroomingPhrases)
                if (lower.Contains(g) || lowerNorm.Contains(g))
                { v.Reasons.Add(Category.GroomingPattern); break; }
            foreach (var g in GiftLurePhrases)
                if (lower.Contains(g) || lowerNorm.Contains(g))
                { v.Reasons.Add(Category.GiftLure); break; }

            // ── L4: URLs + credentials + impersonation ──
            var urlMatches = UrlRx.Matches(text);
            foreach (Match m in urlMatches)
            {
                if (!IsAllowedDomain(m.Value))
                { v.Reasons.Add(Category.PhishingURL); v.SanitizedText = v.SanitizedText.Replace(m.Value, "[link blocked]"); }
            }
            foreach (var p in CredRequestPhrases) if (lower.Contains(p)) { v.Reasons.Add(Category.CredentialRequest); break; }
            foreach (var p in ImpersonationPhrases) if (lower.Contains(p)) { v.Reasons.Add(Category.Impersonation); break; }

            // ── L5: Harassment / self-harm ──
            foreach (var p in HarassmentPhrases) if (lower.Contains(p))
            {
                if (p == "kill yourself" || p == "kys" || p == "go die" || p == "you should die")
                    v.Reasons.Add(Category.SelfHarmDirective);
                else
                    v.Reasons.Add(Category.Harassment);
                break;
            }

            // ── L5b: SELF-HARM IDEATION (user expressing thoughts about themselves) ──
            // CRITICAL: this is a "reach out for help" signal. We DO detect
            // it so the UI can show crisis resources — but we do NOT block
            // the message, count a strike, or auto-flag it as a violation.
            // The original message still sends; the user gets a supportive
            // panel; their friend in chat may be the help they need.
            foreach (var p in SelfHarmIdeationPhrases)
            {
                if (lower.Contains(p) || lowerNorm.Contains(p))
                {
                    if (!v.Reasons.Contains(Category.SelfHarmIdeation))
                        v.Reasons.Add(Category.SelfHarmIdeation);
                    break;
                }
            }

            // ── L5c: THREAT OF VIOLENCE (user threatening someone else) ──
            // Distinct from SelfHarmIdeation: this fires the firm
            // ThreatResponsePanel rather than the supportive
            // CrisisResourcesPanel, blocks the message, and flags
            // ModerationQueue. The SelfHarmIdeation check above runs first
            // on purpose — "going to kill myself" is a self-harm reach-out,
            // not a threat. If that already flagged the message we skip the
            // threat check to avoid double-labeling.
            if (!v.Reasons.Contains(Category.SelfHarmIdeation))
            {
                foreach (var p in ThreatViolencePhrases)
                {
                    if (lower.Contains(p) || lowerNorm.Contains(p))
                    {
                        if (!v.Reasons.Contains(Category.ThreatViolence))
                            v.Reasons.Add(Category.ThreatViolence);
                        break;
                    }
                }
            }

            // ── L6: Context classifier — multi-signal scoring ──
            // Catches subtle predators who avoid keywords but use the
            // grooming pattern combinations (intimacy + secrecy + demands).
            var ctxScore = Sparq.Safety.ContextClassifier.Score(text);
            if (ctxScore.HighRisk)
            {
                v.Reasons.Add(Category.GroomingPattern);
                Debug.LogWarning($"[ContentModerator] Context classifier HIGH-RISK: " +
                                 $"signals=[{string.Join(",", ctxScore.TopSignals)}] " +
                                 $"composite={ctxScore.CompositeRisk:F2}");
            }
            else if (ctxScore.MediumRisk)
            {
                // Don't block on medium alone — but log for review and add
                // a "watch this conversation" reason.
                if (!v.Reasons.Contains(Category.SlangPattern))
                    v.Reasons.Add(Category.SlangPattern);
            }

            // ── Decide severity ──
            v.Severity = DetermineSeverity(v.Reasons);
            v.Allowed = v.Severity != Severity.Block;
            v.UserFacingMessage = BuildUserMessage(v.Reasons);

            // Log every blocked message AND auto-flag to moderator queue
            if (v.Severity == Severity.Block)
            {
                Debug.LogWarning($"[ContentModerator] BLOCKED ({contextHint}): " +
                                 $"reasons=[{string.Join(",", v.Reasons)}] text='{text}'");
                try
                {
                    string sender = "";
                    try { var d = Sparq.Core.SaveService.Data; sender = d?.playerName ?? ""; } catch {}
                    Sparq.Safety.ModerationQueue.AutoFlag(sender, text,
                        string.Join(",", v.Reasons));
                    // Strike against the sender — escalates to mute via rate limiter
                    Sparq.Safety.RateLimiter.RecordViolation();
                }
                catch (System.Exception ex)
                { Debug.LogError($"[ContentModerator] Auto-flag failed: {ex.Message}"); }
            }

            return v;
        }

        /// <summary>Convenience: check if a username is allowed (stricter rules).</summary>
        public static Verdict InspectUsername(string name)
        {
            var v = Inspect(name, "username");
            // Username has stricter rules — also block any URL fragment + any PII
            // even if Inspect() classified it as Warn only.
            if (v.Reasons.Contains(Category.PhishingURL) ||
                v.Reasons.Contains(Category.PII_Email) ||
                v.Reasons.Contains(Category.PII_Phone))
            {
                v.Severity = Severity.Block;
                v.Allowed = false;
            }
            return v;
        }

        // ─────────────────────────────────────────────────────────────────
        // INTERNAL
        // ─────────────────────────────────────────────────────────────────

        private static Severity DetermineSeverity(List<Category> reasons)
        {
            if (reasons.Count == 0) return Severity.Clean;
            // SelfHarmIdeation alone is NOT a violation. It signals the UI
            // to show crisis resources, nothing more. If it's the only flag,
            // the message is Clean.
            bool ideationOnly = true;
            foreach (var r in reasons)
                if (r != Category.SelfHarmIdeation) { ideationOnly = false; break; }
            if (ideationOnly) return Severity.Clean;
            foreach (var r in reasons)
            {
                // Hard block: anything predatory, slurs, self-harm, phishing
                switch (r)
                {
                    case Category.Slur:
                    case Category.SexualContent:
                    case Category.GroomingPattern:
                    case Category.GiftLure:
                    case Category.PhishingURL:
                    case Category.CredentialRequest:
                    case Category.Impersonation:
                    case Category.SelfHarmDirective:
                    case Category.PII_Phone:
                    case Category.PII_Email:
                    case Category.PII_Address:
                    case Category.OffPlatformLure:    // moving to Snap/Discord = predator red flag
                    case Category.SuggestiveEmoji:    // 2+ sexual emoji together
                    case Category.SexualSlang:        // dtf, smash, thicc, rizz, onlyfans
                    case Category.DrugSlang:          // 420, plug, molly, lean
                    case Category.PredatorEndearment: // babygirl, princess, daddy in stranger chat
                    case Category.ThreatViolence:     // weapons / mass-harm / "i'll kill you"
                        return Severity.Block;
                }
            }
            // Bullying is a Warn (single instance) — repeat offender escalates separately
            // via the rate limiter (not yet built).
            foreach (var r in reasons)
                if (r == Category.BullyingSlang) return Severity.Warn;
            return Severity.Warn;
        }

        private static string BuildUserMessage(List<Category> reasons)
        {
            if (reasons.Count == 0) return "";
            // Ideation-only → caller (ChatSender) opens the crisis panel.
            // No toast text — the panel speaks for itself.
            bool ideationOnly = true;
            foreach (var r in reasons)
                if (r != Category.SelfHarmIdeation) { ideationOnly = false; break; }
            if (ideationOnly) return "";
            // Friendly, kid-safe explanations
            foreach (var r in reasons)
            {
                switch (r)
                {
                    case Category.PII_Phone:        return "Don't share phone numbers in chat. Stay safe!";
                    case Category.PII_Email:        return "Don't share emails in chat. Stay safe!";
                    case Category.PII_Address:      return "Don't share where you live. Stay safe!";
                    case Category.PII_Age:          return "Sharing your age in chat isn't safe — message hidden.";
                    case Category.Slur:             return "Slurs aren't allowed in Sparq.";
                    case Category.Profanity:        return "Watch your language — message cleaned up.";
                    case Category.SexualContent:    return "That kind of content isn't allowed here.";
                    case Category.GroomingPattern:  return "This message looks unsafe. If someone is making you uncomfortable, tap REPORT.";
                    case Category.GiftLure:         return "Be careful — strangers offering free things in chat is a known scam.";
                    case Category.PhishingURL:      return "Links to outside sites are blocked for safety.";
                    case Category.CredentialRequest:return "Sparq will never ask for your password. Block + report.";
                    case Category.Impersonation:    return "Real Sparq staff have a verified badge. Block + report.";
                    case Category.Harassment:       return "Be kind. Bullying isn't allowed in Sparq.";
                    case Category.SelfHarmDirective:return "Stop. Reporting this message. If you need help, please reach out to a trusted adult.";
                    case Category.ThreatViolence:   return "";  // ThreatResponsePanel speaks for itself — caller surfaces it.
                    case Category.OffPlatformLure:  return "Conversations stay on Sparq. Moving to other apps isn't safe.";
                    case Category.SuggestiveEmoji:  return "That combination of emoji isn't appropriate here.";
                    case Category.SlangPattern:     return "Watch the shorthand — message reviewed.";
                    case Category.EvasiveSpelling:  return "We caught what that's meant to spell. Cleaned up.";
                    case Category.SexualSlang:      return "Sexual slang isn't allowed in Sparq chat.";
                    case Category.DrugSlang:        return "Drug references aren't allowed in Sparq.";
                    case Category.BullyingSlang:    return "Knock off the put-downs. Be kind in Sparq chat.";
                    case Category.PredatorEndearment: return "Be careful — strangers using pet names is a red flag. Tap REPORT if uncomfortable.";
                    case Category.UnicodeBypass:    return "Nice try with the lookalike characters. Cleaned up.";
                }
            }
            return "Message blocked for safety.";
        }

        private static bool IsAllowedDomain(string url)
        {
            string u = url.ToLower();
            foreach (var d in ALLOWED_DOMAINS) if (u.Contains(d)) return true;
            return false;
        }

        private static string ReplaceWord(string text, string word)
        {
            var rx = new Regex(@"\b" + Regex.Escape(word) + @"\b", RegexOptions.IgnoreCase);
            return rx.Replace(text, new string('*', word.Length));
        }

        // Word-level sexual/drug-slang check.
        private static void CheckSlangSets(string lowerText, Verdict v)
        {
            // Split on word boundaries — keep the multi-word entries handled separately
            string[] words = Regex.Split(lowerText, @"[^a-zA-Z0-9']+");
            foreach (var w in words)
            {
                if (string.IsNullOrEmpty(w)) continue;
                if (SexualSlangSet.Contains(w) && !v.Reasons.Contains(Category.SexualSlang))
                    v.Reasons.Add(Category.SexualSlang);
                if (DrugSlangSet.Contains(w) && !v.Reasons.Contains(Category.DrugSlang))
                    v.Reasons.Add(Category.DrugSlang);
            }
            // Multi-word entries (contains "hook up" not just "hook")
            if (!v.Reasons.Contains(Category.SexualSlang))
            {
                foreach (var p in SexualSlangSet)
                    if (p.Contains(' ') && lowerText.Contains(p))
                    { v.Reasons.Add(Category.SexualSlang); break; }
            }
            if (!v.Reasons.Contains(Category.DrugSlang))
            {
                foreach (var p in DrugSlangSet)
                    if (p.Contains(' ') && lowerText.Contains(p))
                    { v.Reasons.Add(Category.DrugSlang); break; }
            }
        }

        // Word-level profanity/slur check (called for original + normalized).
        private static void CheckWords(string lowerText, Verdict v)
        {
            string[] words = Regex.Split(lowerText, @"[^a-zA-Z']+");
            foreach (var w in words)
            {
                if (string.IsNullOrEmpty(w)) continue;
                if (SlurSet.Contains(w))
                {
                    if (!v.Reasons.Contains(Category.Slur)) v.Reasons.Add(Category.Slur);
                    v.SanitizedText = ReplaceWord(v.SanitizedText, w);
                }
                else if (ProfanitySet.Contains(w))
                {
                    if (!v.Reasons.Contains(Category.Profanity)) v.Reasons.Add(Category.Profanity);
                    v.SanitizedText = ReplaceWord(v.SanitizedText, w);
                }
            }
        }

        // Cyrillic / look-alike → Latin folding. Common bypass: use Cyrillic
        // 'а' (U+0430) instead of Latin 'a' so word lists miss it.
        private static readonly Dictionary<char, char> UnicodeFold = new Dictionary<char, char>
        {
            // Cyrillic
            { 'а', 'a' }, { 'А', 'a' },   // U+0430, U+0410
            { 'е', 'e' }, { 'Е', 'e' },
            { 'о', 'o' }, { 'О', 'o' },
            { 'р', 'p' }, { 'Р', 'p' },
            { 'с', 'c' }, { 'С', 'c' },
            { 'х', 'x' }, { 'Х', 'x' },
            { 'у', 'y' }, { 'У', 'y' },
            { 'і', 'i' }, { 'І', 'i' },   // Ukrainian
            { 'ӏ', 'i' },
            { 'ѕ', 's' }, { 'Ѕ', 's' },
            // Greek
            { 'α', 'a' }, { 'ο', 'o' }, { 'ρ', 'p' }, { 'υ', 'y' },
            // Mathematical alphanumeric (e.g. "𝐤𝐢𝐥𝐥")
            // Note: these are surrogate pairs in C# strings; handled as best-effort
            // by stripping non-ASCII letters after the main fold.
            // Common accented Latin (often pasted from elsewhere)
            { 'à', 'a' }, { 'á', 'a' }, { 'ä', 'a' }, { 'â', 'a' }, { 'ã', 'a' }, { 'å', 'a' },
            { 'è', 'e' }, { 'é', 'e' }, { 'ë', 'e' }, { 'ê', 'e' },
            { 'ì', 'i' }, { 'í', 'i' }, { 'ï', 'i' }, { 'î', 'i' },
            { 'ò', 'o' }, { 'ó', 'o' }, { 'ö', 'o' }, { 'ô', 'o' }, { 'õ', 'o' }, { 'ø', 'o' },
            { 'ù', 'u' }, { 'ú', 'u' }, { 'ü', 'u' }, { 'û', 'u' },
            { 'ñ', 'n' }, { 'ç', 'c' },
        };

        // Zero-width characters used to hide bypasses ("k​ill yourself")
        private static readonly char[] ZeroWidthChars = new[]
        {
            '​',   // zero-width space
            '‌',   // zero-width non-joiner
            '‍',   // zero-width joiner
            '⁠',   // word joiner
            '﻿',   // BOM / zero-width no-break space
        };

        // Normalize text for filter-evasion-resistant matching.
        // Catches:
        //   - Leetspeak: "f*ck" "fck" "ph0n3" "@ge" "1m 13" → "fuck" "fck" "phone" "age" "im 13"
        //   - Slang expansion: "asl?" → "age sex location"
        //   - Repeated chars: "fuuuuck" → "fuck"
        //   - Inserted symbols: "f.u.c.k" "f-u-c-k" → "fuck"
        //   - Unicode lookalikes: cyrillic "kіll" → "kill"
        //   - Zero-width spaces: "k​ill yourself" → "kill yourself"
        //   - Letter spacing: "k i l l   y o u" → "kill you"
        private static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            // 0) Strip zero-width characters
            var sbZ = new System.Text.StringBuilder(text.Length);
            foreach (var ch in text)
            {
                bool isZeroWidth = false;
                foreach (var z in ZeroWidthChars) if (ch == z) { isZeroWidth = true; break; }
                if (!isZeroWidth) sbZ.Append(ch);
            }
            string s = sbZ.ToString();

            // 1) Lowercase
            s = s.ToLower();

            // 2) Fold cyrillic/accented/greek lookalikes to Latin
            var sbU = new System.Text.StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (UnicodeFold.TryGetValue(ch, out char folded)) sbU.Append(folded);
                else sbU.Append(ch);
            }
            s = sbU.ToString();

            // 3) Strip common evasion punctuation BETWEEN letters
            //    "f.u.c.k", "f-u-c-k", "f*u*c*k" → "fuck"
            s = Regex.Replace(s, @"(?<=[a-z])[\.\-\*\+\_]+(?=[a-z])", "");

            // 4) Collapse "letter-space-letter" sequences ("k i l l" → "kill")
            //    Only when 3+ single letters are space-separated (avoids
            //    breaking normal text "a cat" or "I am").
            s = Regex.Replace(s, @"(?:(?<=^|\s)[a-z](?:\s[a-z]){2,}(?=\s|$))",
                m => m.Value.Replace(" ", ""));

            // 5) Collapse 3+ repeated letters (fuuuuuck → fuuck)
            s = Regex.Replace(s, @"(.)\1{2,}", "$1$1");

            // 6) Map leetspeak chars to letters
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (LeetMap.TryGetValue(ch, out char letter)) sb.Append(letter);
                else sb.Append(ch);
            }
            s = sb.ToString();

            // 7) Expand slang abbreviations as standalone words
            foreach (var kv in SlangExpansions)
            {
                s = Regex.Replace(s, @"\b" + Regex.Escape(kv.Key) + @"\b", kv.Value);
            }
            return s;
        }
    }
}
