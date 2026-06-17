using UnityEngine;

namespace Sparq.UI
{
    /// <summary>
    /// Editor-time helper that loads a hero class's idle frame and auto-crops
    /// it to the actual figure using alpha-channel bounds.
    ///
    /// Why this exists: the FantasyKnight pack ships 1800×980 frames — the
    /// figure floats in a sea of transparent padding with a long spear
    /// extending right — while the other chibi packs are clean ~900×900.
    /// Dropping a raw Knight sprite into a UI Image with preserveAspect renders
    /// him at a fraction of the size of the other classes and offsets him from
    /// where the layout expects.
    ///
    /// LoadIdle() scans the texture's alpha channel for the figure's exact
    /// bounding box, and (for the Knight) detects the body vs. the thin spear
    /// so it can crop to a clean body shot. The result is a tight sprite the
    /// caller can drop into any Image — preserveAspect then "just works".
    ///
    /// Single source of truth: HomeLobbyPanel, HeroSelectPanel and
    /// HomeChibiUpgrade should all route through here.
    /// </summary>
    public static class HeroPortrait
    {
        public struct Result
        {
            public Sprite sprite;   // figure-tight sprite (or raw fallback)
            public bool   ok;
            public bool   cropped;  // true if alpha-cropped, false if raw fallback
        }

        /// <summary>
        /// Load + auto-crop the idle-000 frame for a resolved hero.
        /// excludeWeapon: for the Knight, drop the long spear so the portrait
        /// is a clean body shot. Has no effect on the other classes.
        /// </summary>
        public static Result LoadIdle(Sparq.Systems.HeroClassResolver.HeroSprite hero,
                                      bool excludeWeapon)
        {
            var r = new Result { ok = false, sprite = null, cropped = false };
            if (hero == null || string.IsNullOrEmpty(hero.idleBase)) return r;
            string path = hero.idleBase + "000.png";

#if UNITY_EDITOR
            // Editor-only: make the texture CPU-readable so we can scan its
            // alpha channel for a tight figure crop. Persisted on the asset
            // import settings so it stays readable for repeat opens.
            var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            if (imp != null && !imp.isReadable)
            {
                imp.isReadable = true;
                imp.SaveAndReimport();
            }
#endif

            // Runs in BOTH Editor + Player builds. Previously the whole body
            // was #if UNITY_EDITOR which made the EquipmentPanel hero portrait
            // (the LV.x box) render blank purple in APK builds even though
            // the source PNG was reachable via Resources.
            var sp = Sparq.Core.SpriteLoader.Load(path);
            if (sp == null)
            {
                Debug.LogWarning($"[HeroPortrait] Sprite not found: {path}");
                return r;
            }
            if (sp.texture == null || !sp.texture.isReadable)
            {
                // Non-CPU-readable texture (the default in Player builds).
                // Return the raw sprite — uncropped but visible. preserveAspect
                // on the Image will still scale it sensibly inside the LV box.
                r.sprite = sp; r.ok = true; r.cropped = false;
                return r;
            }

#if UNITY_EDITOR
            // Alpha-crop path is editor-only (needs GetPixels32). Player
            // builds hit the !isReadable early-return above.
            var tex = sp.texture;
            var px = tex.GetPixels32();
            RectInt fig = AlphaBounds(px, tex.width, tex.height);
            if (fig.width <= 4 || fig.height <= 4)
            {
                r.sprite = sp; r.ok = true; r.cropped = false;
                return r;
            }

            int cropX = fig.x, cropW = fig.width;
            if (excludeWeapon && hero.classId == "knight")
            {
                int bodyMinX, bodyMaxX;
                BodyXRange(px, tex.width, fig, out bodyMinX, out bodyMaxX);
                cropX = bodyMinX;
                cropW = Mathf.Max(1, bodyMaxX - bodyMinX + 1);
            }

            var crop = new Rect(cropX, fig.y, cropW, fig.height);
            r.sprite = Sprite.Create(tex, crop, new Vector2(0.5f, 0.5f), 100f);
            r.ok = true;
            r.cropped = true;
            Debug.Log($"[HeroPortrait] {hero.classId}: figure={fig} " +
                      $"crop=({cropX},{fig.y},{cropW},{fig.height})");
#else
            // Player build fallback when a texture somehow comes through as
            // CPU-readable (rare): raw sprite already covered above.
            r.sprite = sp; r.ok = true; r.cropped = false;
#endif
            return r;
        }

        /// <summary>
        /// Load ANY sprite by path and crop it tight to its alpha bounds.
        /// For non-hero art (pets, companions) that ships with transparent
        /// padding — gives it the same polished, frame-filling look the
        /// cropped heroes get. Returns the raw sprite if it can't be made
        /// readable, or null if the path is bad.
        /// </summary>
        public static Sprite LoadCropped(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;

#if UNITY_EDITOR
            // Editor-only: ensure the texture is CPU-readable so we can scan
            // alpha bounds. Auto-fixes the import setting once.
            var imp = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
            if (imp != null && !imp.isReadable)
            {
                imp.isReadable = true;
                imp.SaveAndReimport();
            }
#endif

            // Runs in BOTH Editor + Player builds. Previously the entire body
            // was #if UNITY_EDITOR which made portrait images (Karu's pet care
            // portrait, pet sprites, companion art) render as placeholder
            // boxes in APKs. Now: SpriteLoader works at runtime + we attempt
            // alpha-crop only if the texture is CPU-readable. If not (which
            // is the default for textures in built APKs), we return the raw
            // sprite — uncropped but visible.
            var sp = Sparq.Core.SpriteLoader.Load(assetPath);
            if (sp == null) return null;
            if (sp.texture == null || !sp.texture.isReadable) return sp;   // raw fallback

#if UNITY_EDITOR
            // AlphaBounds() is editor-only (uses GetPixels32 which needs
            // CPU-readable textures — only set in editor by our import fixup).
            // In Player builds the !isReadable early-return above catches
            // this; we keep the call gated so the compiler is happy.
            var tex = sp.texture;
            var px = tex.GetPixels32();
            RectInt fig = AlphaBounds(px, tex.width, tex.height);
            if (fig.width <= 4 || fig.height <= 4) return sp;

            return Sprite.Create(tex, new Rect(fig.x, fig.y, fig.width, fig.height),
                                 new Vector2(0.5f, 0.5f), 100f);
#else
            return sp;
#endif
        }

        /// <summary>
        /// Load an animated frame sequence (base + "000.png", "001.png", …) and
        /// crop EVERY frame to one shared bounding box — the union of all frames'
        /// figures. This grounds the squad: the figure fills its box so the feet
        /// reach the bottom (a shadow at the box base then lands under them), and
        /// using a SHARED box (not per-frame) keeps the feet from jittering as the
        /// animation plays.
        ///
        /// clipThinSides: drop thin horizontal protrusions (e.g. the FantasyKnight's
        /// long spear) from the WIDTH so the body isn't squeezed tiny by
        /// preserveAspect. Height always uses the full alpha bounds.
        /// </summary>
        // Cache for cropped sprite sequences — keyed by (path, count, clipThinSides).
        // Without this, opening the explore map re-imported every fighter sprite
        // and re-ran the alpha-bounds scan (~17 fighters × 8 frames × millions of
        // pixels) on every entry — visible hitch + GC churn on cold opens.
        private static readonly System.Collections.Generic.Dictionary<string, Sprite[]> _croppedCache
            = new System.Collections.Generic.Dictionary<string, Sprite[]>();

        public static Sprite[] LoadCroppedFrames(string base3DigitPath, int count, bool clipThinSides = false)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(base3DigitPath) || count <= 0) return new Sprite[0];

            // Cache lookup: textures are immutable assets, so a same-key request
            // is guaranteed identical work — return the prior result.
            string cacheKey = $"{base3DigitPath}|{count}|{clipThinSides}";
            if (_croppedCache.TryGetValue(cacheKey, out var cached) && cached != null && cached.Length > 0)
                return cached;

            var sprites = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; i < count; i++)
            {
                string path = base3DigitPath + i.ToString().PadLeft(3, '0') + ".png";
                var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
                if (imp != null)
                {
                    bool changed = false;
                    if (imp.textureType != UnityEditor.TextureImporterType.Sprite) { imp.textureType = UnityEditor.TextureImporterType.Sprite; changed = true; }
                    if (!imp.isReadable) { imp.isReadable = true; changed = true; }
                    if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; changed = true; }
                    if (changed) imp.SaveAndReimport();
                }
                var sp = Sparq.Core.SpriteLoader.Load(path);
                if (sp != null && sp.texture != null) sprites.Add(sp);
            }
            if (sprites.Count == 0) return new Sprite[0];

            // Union bounds across every frame.
            int uMinX = int.MaxValue, uMinY = int.MaxValue, uMaxX = int.MinValue, uMaxY = int.MinValue;
            foreach (var sp in sprites)
            {
                var tex = sp.texture;
                if (tex == null || !tex.isReadable) continue;
                var px = tex.GetPixels32();
                RectInt fig = AlphaBounds(px, tex.width, tex.height);
                if (fig.width <= 0 || fig.height <= 0) continue;

                int minX = fig.xMin, maxX = fig.xMax;   // xMax = xMin + width (exclusive)
                if (clipThinSides)
                {
                    BodyXRange(px, tex.width, fig, out int bMinX, out int bMaxX);
                    minX = bMinX; maxX = bMaxX + 1;
                }
                if (minX < uMinX) uMinX = minX;
                if (fig.yMin < uMinY) uMinY = fig.yMin;
                if (maxX > uMaxX) uMaxX = maxX;
                if (fig.yMax > uMaxY) uMaxY = fig.yMax;
            }
            if (uMaxX <= uMinX || uMaxY <= uMinY) { var raw = sprites.ToArray(); _croppedCache[cacheKey] = raw; return raw; }   // couldn't read — raw + cache

            var rect = new Rect(uMinX, uMinY, uMaxX - uMinX, uMaxY - uMinY);
            var cropped = new Sprite[sprites.Count];
            for (int i = 0; i < sprites.Count; i++)
            {
                var tex = sprites[i].texture;
                if (tex == null || rect.xMax > tex.width || rect.yMax > tex.height || !tex.isReadable)
                { cropped[i] = sprites[i]; continue; }   // raw fallback for odd frames
                cropped[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0f), 100f);
            }
            _croppedCache[cacheKey] = cropped;
            return cropped;
#else
            return new Sprite[0];
#endif
        }

#if UNITY_EDITOR
        // Tight bounding box of every non-transparent pixel (texture space,
        // origin bottom-left). Returns a zero rect for a fully empty texture.
        private static RectInt AlphaBounds(Color32[] px, int w, int h)
        {
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    if (px[row + x].a > 12)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }
            if (maxX < minX || maxY < minY) return new RectInt(0, 0, 0, 0);
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        // Within the figure bounds, find the horizontal span of the BODY.
        // The body / shield / cape are TALL columns; the spear is a thin
        // sliver. Columns whose vertical alpha span exceeds ~42% of the figure
        // height count as "body" — which cleanly excludes the spear.
        private static void BodyXRange(Color32[] px, int w, RectInt fig,
                                       out int bodyMinX, out int bodyMaxX)
        {
            float threshold = fig.height * 0.42f;
            bodyMinX = fig.x;
            bodyMaxX = fig.x + fig.width - 1;
            int firstTall = -1, lastTall = -1;
            for (int x = fig.x; x < fig.x + fig.width; x++)
            {
                int top = -1, bot = -1;
                for (int y = fig.y; y < fig.y + fig.height; y++)
                {
                    if (px[y * w + x].a > 12)
                    {
                        if (top < 0) top = y;
                        bot = y;
                    }
                }
                if (top >= 0 && (bot - top) >= threshold)
                {
                    if (firstTall < 0) firstTall = x;
                    lastTall = x;
                }
            }
            if (firstTall >= 0)
            {
                bodyMinX = firstTall;
                bodyMaxX = lastTall;
            }
        }
#endif
    }
}
