# Unity Asset Store — Sparq Asset Inventory

> **Status: post-shopping.** The original shopping list below is preserved as
> a buyer's guide. The project's `Assets/` folder now has ~120 packs in it.
> The "currently in use" section just below lists what's actually wired up.

## Currently in use (June 2026)

The build references these packs — keep them in the build, the others can be
trimmed later if the APK gets bloated:

- **Layer Lab** — Popup_Box frames, circle buttons, the bulk of the panel chrome
- **2D Casual UI** — generic buttons, sliders, frame variants
- **FantasyMaps** — explore-map backdrops (set "01" and "02")
- **FantasyKnight** — knight chibi (1800×980 frames, cropped at runtime via `HeroPortrait`)
- **AmazonChibi / BanditChibi / ElfArcherChibi / ElementalChibi** — squad / ally art
- **2D Fantasy Monster Sprite Pack** — enemy roster
- **2D Animal Character Pack** — Karu, Mochi, pet art
- **2D Potion Icon Pack** — consumables in the bag
- **CoinCrystalShop** — economy / shop icons
- **CurrencyLootIcons** — drop visuals
- **Fantasy Battle Music Free Pack** / **Action RPG Music** — BGM
- **CartoonSmokeFX** + various VFX sub-packs — hit / level-up / signature-drop bursts
- **FantasyIconPack + FantasyIconsPackFree + Casual Fantasy Book Icon Pack** — skill / item icons
- **AnimatedTextGame** — text-juice helpers for combat numbers and victory banners

About 60+ packs in `Assets/` are unused (demo scenes, surplus icon variants,
backup options). They're kept on-disk but not referenced — Unity won't include
unreferenced assets in the build automatically. If APK size becomes a problem
later, the trim list will be obvious.

---

## Original shopping list (preserved as a buyer's guide)

Detailed list of what to buy, when, and why. Total budget: **~$150-300** for pro-looking game.

**Rule**: always prefer assets with a **URP 2D** tag (Universal Render Pipeline) and **high 4-5★ rating** with **recent updates** (within last year).

---

## TIER 1 — Must-buy before Week 1 ($100-150)

These unlock 80% of the visual polish.

### 1. UI Kit — "Fantasy RPG GUI" style
**Purpose**: buttons, panels, bars, icons for the whole app
**Search**: "RPG GUI", "Fantasy UI", "Pixel UI Kit"
**Recommendations**:
- **Modern Procedural Pixel UI** (~$25) — clean, works great
- **Fantasy Medieval RPG UI** (~$40) — if you want more decorative
- **Clean UI Pack 2D** (~$15) — minimalist option
**What to look for**: 100+ icons, bar/panel sprites, frame borders, button states (normal/hover/pressed)

### 2. Character pack — player's pet replacement
**Purpose**: Karu redesign as proper animated sprite
**Search**: "pixel hero animated", "2D character sprite sheet"
**Recommendations**:
- **Tiny Pixel Heroes** (~$20) — multiple heroes, each with idle/walk/attack/hurt/die animations
- **2D Animated Characters Mega Pack** (~$50) — larger variety
**What to look for**: idle animation loop, multiple states, has .controller or sprite sheets

### 3. Enemy pack — 12 unique monsters
**Purpose**: Volt, Shade, Pyra, Barry, etc.
**Search**: "pixel monster pack", "2D enemy sprites animated"
**Recommendations**:
- **Monster Pack Vol. 1-3** (~$25 each, or bundled) — 30+ monsters with animations
- **Pixel Fantasy Monsters** (~$30)
**What to look for**: animated idle + attack, variety of body shapes (not all humanoid)

### 4. VFX / Particles
**Purpose**: hits, level-ups, explosions, sparkles, confetti
**Search**: "pixel VFX", "2D effects animated", "hit effect sprite"
**Recommendations**:
- **Pixel VFX Pack** (~$20) — 50+ effects
- **Hand-painted 2D VFX** (~$30)
**What to look for**: spritesheet-based effects (not particle system based — those are harder to use)

### 5. Sound FX library
**Purpose**: taps, hits, level-up chime, coin pickup, etc.
**Search**: "retro game sfx", "RPG sound pack", "UI sound effects"
**Recommendations**:
- **Universal Sound FX** (~$25) — 7000+ sounds
- **8-bit Sound Effects** (~$10) — smaller but pure retro
**What to look for**: UI sounds (click, confirm, back), combat sounds (hit, crit, death), rewards (coin, XP)

### 6. Music pack
**Purpose**: background loops + signature Sparq theme
**Search**: "8-bit music pack", "chiptune music", "retro game BGM"
**Recommendations**:
- **Chiptune Bundle** (~$20) — 20+ tracks
- **RPG Music Pack** (~$30)
**What to look for**: 5-10 loopable tracks with different moods (menu, combat, victory, calm)

### 7. DOTween Pro (animation library)
**Purpose**: smooth tween animations everywhere (UI slides, pet bounces, HP bar drains)
**Link**: https://assetstore.unity.com/packages/tools/visual-scripting/dotween-pro-32416
**Price**: $15
**Note**: The free **DOTween** works too (I'll adapt) but Pro adds visual sequencing

---

## TIER 2 — Buy in Week 3-4 (~$50-80)

Once Tier 1 is in and we know the gaps.

### 8. Background/environment
**Purpose**: Karu's home, dungeon backdrop, shop scene
**Search**: "2D pixel environment", "tileset fantasy"
**Recommendations**: **Tiny RPG Forest** ($15), **Pixel Backgrounds Pack** ($20)

### 9. Pet outfits/accessories
**Purpose**: the 34 wearables in the shop
**Search**: "pixel character accessories", "hat collection sprite"
**Note**: might need to commission ~$50-100 custom for perfect fit — save for Week 5

### 10. Item icons pack
**Purpose**: potions, food, keys in the bag
**Search**: "item icons pack 2D", "consumable icons"
**Recommendations**: **RPG Item Icons** ($15), **Item Icon Pack** ($20)

---

## TIER 3 — Optional nice-to-haves (~$30-50)

### 11. Text juice / font effects
- **Text Animator for Unity** ($30) — typewriter effects, shakes on critical hits

### 12. Animation helpers
- **SpriteShape** (free) — for making curved UI shapes
- **2D Animation** (free Unity package) — for bone-based character rigs

### 13. Shader packs
- **All-In-1 Sprite Shader** ($25) — outline, glow, dissolve effects

---

## Asset quality checklist

Before buying, verify:
- ⭐ **4+ stars** with 20+ reviews
- 🗓️ **Updated in the last 12 months**
- 🎨 **Preview video or gallery** shows actual in-game usage
- 📦 **URP 2D** or **Built-in RP** tag (avoid HDRP — that's 3D only)
- 📏 **Resolution**: match your game's style. Pixel art = 16x16, 32x32, or 64x64 source sprites. Not 512x512 ultra-HD.
- 💰 **Check sale prices** — Unity runs 50-80% off sales monthly. Wait for one if not urgent.

---

## Free alternatives (if budget is tight)

Everything above has free equivalents if you want to DIY:

- **UI Kit**: free GUI packs on itch.io
- **Characters**: **Kenney.nl** (free pixel assets, CC0 license)
- **VFX**: **OpenGameArt.org**
- **Sound**: **Freesound.org**, **itch.io soundpacks**
- **Music**: **FreeMusicArchive**, **YouTube Audio Library**

Quality is lower but $0 is $0. Buying >>>> DIY for speed.

---

## What NOT to buy yet

- ❌ Weather systems (we're not that complex)
- ❌ Dialogue system plugins ("Dialogue System for Unity" is overkill)
- ❌ Inventory system plugins (we have our own)
- ❌ Quest system plugins (we have our own)
- ❌ Ads/monetization SDKs (not launching monetized v1)
- ❌ Multiplayer (no real-time multiplayer in Sparq)

---

## Budget reality check

| Tier | Cost | Impact |
|------|------|--------|
| Just Tier 1 | $100-150 | Pro-looking MVP |
| Tier 1 + 2 | $150-230 | Full Top Heroes-feel |
| All 3 Tiers | $180-300 | Indie studio polish |
| DIY (all free) | $0 | Slower, rougher |

My pick for Sparq: **Tier 1 ($100-150)** to start. Add Tier 2 in Week 3-4 based on actual gaps.
