# Sparq — Closed Test Playbook

This is the playbook for the 6-person friends-and-family test. It covers
what to send the testers, what to ask them, and how to read the results.

If you're a TESTER reading this: jump to [For Testers](#for-testers).
If you're the developer running the test: start at [Test setup](#test-setup).

---

## Test goals — pick one

Be ruthless about scope. A 6-person test can answer ONE question well, or
five questions badly. Decide which one before you ship the build:

1. **"Does it install and not crash?"** — pure stability check. 2–3 day
   window. You watch crash reports / DMs. Cheap and high-signal.
2. **"Is the core loop fun?"** — engagement check. 5–7 day window. You
   want testers playing day 2 and day 3 to gauge retention pull.
3. **"Does the onboarding land?"** — FTUE check. 1–2 day window. Focus
   on the first 10 minutes. Testers don't need to come back.
4. **"Where does the economy break?"** — balance check. 5–7 days. Pay
   attention to which testers feel stuck or rich.

The setup below assumes goal #2 (the most common first-test choice). For
others, trim accordingly.

---

## Test setup

### 1. Lock the save shape

Cloud save is off (`SPARQ_CLOUDSAVE` gated). Saves live in `PlayerPrefs` on
the tester's phone. **Don't ship a `PlayerData.cs` change mid-test** — it'll
wipe their progress. If you must, push a migration and warn them.

### 2. Build the APK (Android)

See [SETUP.md](SETUP.md) for the one-time Unity install. Then:

```
File → Build Settings → Android → Build
```

Output: a `.apk` file. Sign with your debug or release keystore.

### 3. Distribute

Pick one based on tester comfort with Android:

| Tester profile             | Method                              | Setup cost |
| -------------------------- | ----------------------------------- | ---------- |
| All Android, tech-comfortable | Direct APK + sideload instructions  | None       |
| Mixed / less technical     | Google Play Internal Testing track  | ~1 day     |
| Any iOS testers            | TestFlight                          | ~2 days    |

For 6 friends-and-family, direct APK is usually fastest. See the
[For Testers](#for-testers) section below for the install instructions
to copy-paste into your DM.

### 4. Spin up a feedback channel

A 5-question Google Form, linked from inside the debug menu. Use the
ready-to-paste form template at the bottom of this file — it's calibrated
for ~2-minute completion time so reply rates stay high.

That's enough to triangulate where to spend the next sprint. More than 5
questions and reply rates collapse.

### 5. Set expectations with testers

Use the ready-to-paste tester DM at the bottom of this file. One short
message you customize per friend. Send once you have the APK + form link
in hand.

---

## For testers

You've been sent an Android APK of **Sparq**. Here's how to install and what
to do.

### Install (Android)

1. **Allow installs from unknown sources** — on first install attempt,
   Android will ask. Allow it for whichever app delivered the APK (Drive,
   email, Discord, etc.).
2. **Tap the APK file** to install.
3. **Open Sparq.** First launch shows a welcome screen (FTUE).

### What we'd love you to do

- Play through the welcome / first-time onboarding.
- Do a quest or two. Tap a mood crystal.
- Try a battle on the explore map.
- **Come back tomorrow** — even for 2 minutes. That's the single most
  useful thing you can give us.
- Whenever something feels off, jot it in the feedback form (link below).

### Reset / get unstuck

If something breaks, or you want to see the FTUE again:

1. Open **Settings** (gear icon, top-right of the lobby).
2. Tap the row labeled **"Version"** seven times in a row.
3. A red **TESTER TOOLS** panel appears with:
   - **Reset Save** — wipes everything, fresh start
   - **+1000 Coins** — for testing the shop
   - **Replay FTUE** — see the welcome flow again
   - **Force Daily Reset** — pretend a day has passed
4. Each action requires a second tap to confirm (so a butter-finger can't
   nuke your save).

If a panel really jams (won't close), force-stop the app from your phone's
app settings and reopen.

### Feedback form

[Link goes here] ← developer fills this in before sending.

Five questions, takes under 2 minutes. Even one-word answers help.

### What we're NOT testing

So you can stop worrying about:

- Crashes (we'll see them) — just keep playing or restart
- Graphics polish (we'll iterate)
- The shop economy (numbers are placeholder)
- iOS support (Android-only this round)
- Multiplayer or social features (none yet)

Thank you for playing.

---

## During the test — what to watch

### Day 1
- Did everyone install successfully? Chase the silent ones.
- Any crash reports surfacing? Read them same-day; they decay in interest fast.
- First-form-fills usually arrive within 24h.

### Day 2 (the important one)
- Who came back? That's your retention signal — more important than any
  individual piece of feedback.
- The ones who DIDN'T come back: was it a bug, or did they bounce? A
  short DM ("hey, did the app crash or did it just not pull you back?")
  usually resolves this.

### Day 3+
- Feedback themes start to cluster. Triage into:
  - **Must-fix-before-store**: crashes, save loss, payment-impacting balance
  - **Should-fix-next-sprint**: confusing UI, balance issues, missing onboarding clarity
  - **Nice-to-have**: cosmetic, edge-case, "I wish it had X" feature requests

---

## After the test — closing out

Send a thank-you. Tell each tester the ONE thing their feedback changed
(even if it's small) — they'll be more responsive for round 2.

Archive the build APK with a version label so you can repro any bug they
report against a specific build.

If retention was weak: that's the priority signal for the next sprint, not
"add more features." If retention was strong: shore up the bugs and prep
for a Google Play Internal Testing track (wider, less hands-on).

---

## Appendix A — Google Form template (copy-paste)

Go to **forms.google.com** → blank form. Set:

- **Form title**: `Sparq closed test — feedback`
- **Form description**: `Thanks for playing! 5 questions, ~2 minutes. Even one-word answers help.`

Then add the questions below in order. The `[Question type]` tag in each
header tells you which Google Forms type to pick from the dropdown.

---

**1. How long did you play in total?**   *[Multiple choice]*

- Less than 5 minutes
- 5 to 20 minutes
- 20 to 60 minutes
- More than an hour
- Multiple sessions across different days

Required: yes

---

**2. Did you come back to play on a different day?**   *[Multiple choice]*

- Yes, I came back
- No, just the one session
- Haven't had a chance yet — it's been less than 24 hours

Required: yes

---

**3. What did you like? Anything that grabbed you, made you smile, or kept you playing one more minute?**   *[Paragraph]*

Required: no   *(free text — even one word is fine)*

---

**4. What broke or felt wrong? Bugs, confusing screens, anything that pulled you out of it.**   *[Paragraph]*

Required: no   *(free text)*

---

**5. Would you keep playing if I left the build on your phone?**   *[Linear scale, 1–5]*

- 1 = nope, would uninstall
- 5 = yes, I'd actually play this

Required: yes

---

**Optional final field** *(if you want it)*:

**6. Anything else? Feature wishes, questions, weird stuff.**   *[Paragraph]*

Required: no

---

After creating the form:

1. Click **Send** → **link icon** → copy the short URL (or click the
   "Shorten URL" checkbox).
2. Paste it into TESTING.md "For Testers → Feedback form" line above.
3. Also drop it into the in-app debug menu — currently Reset Save toasts
   a relaunch message; if you want, add a "Feedback form" row that
   `Application.OpenURL`s the form link. (Optional; the form link in the
   DM is usually enough.)

---

## Appendix B — Tester invite DM (copy-paste, customize per friend)

Send via DM (Discord / Messenger / WhatsApp / iMessage). Personalize the
opening line per person — generic mass-sends get ignored.

---

```
Hey [NAME] — sharing an early Android build of Sparq, the wellness/RPG
thing I've been building. Looking for 5 honest testers, you're one of
them if you're up for it.

Closed test, 3–5 day window. Total time ask is ~10 minutes across two
days. Not asking for a review — just an honest "does this feel like
something I'd play."

APK + install instructions: [LINK_TO_APK_OR_DRIVE_FOLDER]
Feedback form (5 questions, ~2 min): [LINK_TO_GOOGLE_FORM]

The one ask: try to open the app on day 1 AND again on day 2. That's
the data point I actually care about — does it pull you back, or did
it die in the first session?

Bugs are expected. If something breaks: open Settings (gear icon),
tap the "Version" row seven times, you'll get a TESTER TOOLS menu with
a Reset Save button. Force-close the app from your phone if a panel
totally jams.

Thanks for doing this. Happy to grab a coffee / drink to say thanks
once the test wraps.
```

---

### How to send

1. Have the APK + form link both ready before sending the first DM. Don't
   send to anyone before both exist — half of testers will reply within
   an hour and you don't want them blocked.
2. Send one at a time over ~24 hours. If you send all 6 at once and one
   has a bug-blocking install issue, you can't fix it before the others
   hit the same wall.
3. After 48 hours, DM the silent ones once. Not pushy — "hey did the
   install work?" That alone usually unblocks them. Don't chase a second
   time; some people just don't reply.

### Stuff to NOT do

- Don't post the APK link publicly (Twitter, Discord servers, etc.) — you
  want feedback from people you can follow up with, not strangers.
- Don't promise updates. You'll feel pressure to keep iterating during the
  test and that's the wrong work for the test window.
- Don't read every form reply the moment it lands. Batch them at 24h /
  48h / 72h checkpoints. Reading them in real time makes you change the
  build mid-test which contaminates the data.
