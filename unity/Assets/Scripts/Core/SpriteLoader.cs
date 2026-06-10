using UnityEngine;

namespace Sparq.Core
{
    /// <summary>
    /// Single source of truth for sprite loading that works in BOTH the
    /// editor AND built APKs.
    ///
    /// Why this exists:
    /// Before this, every panel had its own private LoadSprite() that called
    /// UnityEditor.AssetDatabase.LoadAssetAtPath — which only exists in the
    /// Editor namespace. In a built Player, those calls returned null and the
    /// UI rendered as colored placeholder boxes (we shipped a closed-test build
    /// like that and saw the damage).
    ///
    /// How it works:
    ///   1. Strip "Assets/" prefix and ".png" extension from the caller's asset
    ///      path, giving a Resources-style relative path.
    ///   2. Resources.Load it as a Sprite — fastest path, works in Editor + APK.
    ///   3. If that's null (image imported as Texture2D, not Sprite), Resources.Load
    ///      it as a Texture2D and wrap in a runtime Sprite.
    ///   4. Editor-only fallback: AssetDatabase against the original asset path,
    ///      so the screen still works while a Resources copy is being staged.
    ///
    /// To make this work, the referenced assets must live under
    /// Assets/Resources/... mirroring their original path. A migration script
    /// (scripts/migrate-to-resources.ps1) copies all referenced sprites into
    /// Assets/Resources/&lt;original-path-minus-Assets&gt;.
    /// </summary>
    public static class SpriteLoader
    {
        /// <summary>
        /// Load a sprite by its original asset path (e.g.
        /// "Assets/Layer Lab/Foo/Bar.png"). Returns null if the asset can't
        /// be found by any path — caller should provide a fallback color.
        /// </summary>
        public static Sprite Load(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;

            // Convert Assets/Foo/Bar.png → Foo/Bar (Resources.Load path style).
            string r = assetPath;
            if (r.StartsWith("Assets/")) r = r.Substring(7);
            if (r.EndsWith(".png"))     r = r.Substring(0, r.Length - 4);
            if (r.EndsWith(".jpg"))     r = r.Substring(0, r.Length - 4);

            // Runtime-safe path: Resources.Load. Works in Editor + Player.
            var sp = Resources.Load<Sprite>(r);
            if (sp != null) return sp;

            var tex = Resources.Load<Texture2D>(r);
            if (tex != null)
                return Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), 100f);

#if UNITY_EDITOR
            // Editor-only fallback if the Resources copy hasn't been staged yet.
            // Useful during migration — Editor sessions stay visually intact
            // while we move assets into Resources/.
            try
            {
                var dbSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (dbSp != null) return dbSp;
                var dbTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (dbTex != null)
                    return Sprite.Create(dbTex,
                        new Rect(0, 0, dbTex.width, dbTex.height),
                        new Vector2(0.5f, 0.5f), 100f);
            }
            catch (System.Exception ex)
            { Debug.LogWarning($"[SpriteLoader] AssetDatabase fallback failed for '{assetPath}': {ex.Message}"); }
#endif
            return null;
        }
    }
}
