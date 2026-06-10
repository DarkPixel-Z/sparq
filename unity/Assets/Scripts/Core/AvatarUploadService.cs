using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Sparq.Core
{
    /// <summary>
    /// Handles avatar upload: file picker → client-side validation → local save
    /// (Path A) → optional cloud upload + moderation queue (Path B).
    ///
    /// MODES:
    ///   Path A (current, local-only):
    ///     PickAndUpload → ValidateBytes → SaveLocalAvatar → moderationState="local"
    ///     The image never leaves the device. Safe. No moderation infra needed.
    ///
    ///   Path B (when Firestore + Cloud Functions are wired):
    ///     Same flow, plus UploadToCloud after local save → Firebase Storage at
    ///     avatars/{userId}/pending/{uuid}.png → Cloud Function moderateAvatar.js
    ///     auto-runs SafeSearch → writes Firestore doc at users/{userId}/avatarModeration
    ///     with state="approved" or "rejected" → client listens, updates moderationState.
    ///
    /// FILE PICKER:
    ///   Native file/camera pickers vary by platform. Recommended free options:
    ///   - Unity Native Gallery (https://github.com/yasirkula/UnityNativeGallery) — Android/iOS
    ///   - Unity Native Camera (same author) for live capture
    ///   This file exposes the integration as a single PickFile() call;
    ///   wire your chosen picker in the #if blocks below.
    /// </summary>
    public static class AvatarUploadService
    {
        public const int  MaxBytes          = 5 * 1024 * 1024;  // 5 MB ceiling on disk
        public const int  MaxDimensionPx    = 1024;             // resize larger images to this before saving
        public const long LocalCacheBytes   = 20 * 1024 * 1024; // total quota for cached avatars on disk

        public enum Status { Success, Cancelled, TooLarge, InvalidFormat, AutoRejected, Error }

        public struct Result
        {
            public Status status;
            public string localPath;       // absolute path of saved avatar (when Success)
            public bool   isLocalOnly;     // true in Path A; false in Path B (still pending review)
            public string error;
        }

        /// <summary>
        /// Open the system file picker / camera, validate, save locally, and
        /// (in Path B) queue for moderation. Result delivered via callback.
        /// </summary>
        public static void PickAndUpload(Action<Result> onComplete)
        {
            PickFile(bytes =>
            {
                if (bytes == null || bytes.Length == 0)
                {
                    onComplete?.Invoke(new Result { status = Status.Cancelled });
                    return;
                }

                var v = ValidateBytes(bytes);
                if (v.status != Status.Success)
                {
                    onComplete?.Invoke(v);
                    return;
                }

                string savedPath;
                try
                {
                    savedPath = SaveLocalAvatar(bytes);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AvatarUploadService] SaveLocalAvatar failed: {e}");
                    onComplete?.Invoke(new Result { status = Status.Error, error = e.Message });
                    return;
                }

                // Local-only mode (Path A) — done.
                bool cloudAvailable = false;  // Flip to true when CloudUpload below is wired.
                if (!cloudAvailable)
                {
                    ApplyLocalAvatarToPlayer(savedPath, localOnly: true);
                    onComplete?.Invoke(new Result { status = Status.Success, localPath = savedPath, isLocalOnly = true });
                    return;
                }

                // Cloud mode (Path B) — upload to Firebase Storage and mark pending.
                ApplyLocalAvatarToPlayer(savedPath, localOnly: false);
                CloudUpload(savedPath, cloudResult =>
                {
                    onComplete?.Invoke(cloudResult);
                });
            });
        }

        // ── Validation ────────────────────────────────────────────────────────

        public static Result ValidateBytes(byte[] bytes)
        {
            if (bytes.Length > MaxBytes)
                return new Result { status = Status.TooLarge };

            // Magic-number sniff. PNG: 89 50 4E 47 0D 0A 1A 0A   JPG: FF D8 FF
            bool isPng = bytes.Length >= 8 &&
                bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;
            bool isJpg = bytes.Length >= 3 &&
                bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
            if (!isPng && !isJpg)
                return new Result { status = Status.InvalidFormat };

            // Texture decode sanity check — Unity will reject corrupt/non-image bytes.
            var probe = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!probe.LoadImage(bytes))
            {
                UnityEngine.Object.Destroy(probe);
                return new Result { status = Status.InvalidFormat };
            }
            // Pixels-per-side ceiling — reject absurd dimensions before saving.
            if (probe.width > 8192 || probe.height > 8192)
            {
                UnityEngine.Object.Destroy(probe);
                return new Result { status = Status.TooLarge };
            }
            UnityEngine.Object.Destroy(probe);

            return new Result { status = Status.Success };
        }

        // ── Local save ────────────────────────────────────────────────────────

        public static string SaveLocalAvatar(byte[] bytes)
        {
            // Decode → resize if needed → re-encode as PNG (strips EXIF metadata for privacy)
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);
            if (tex.width > MaxDimensionPx || tex.height > MaxDimensionPx)
            {
                tex = ResizeTexture(tex, MaxDimensionPx);
            }
            byte[] pngBytes = tex.EncodeToPNG();
            UnityEngine.Object.Destroy(tex);

            // Quota check: prune oldest avatars if cache exceeds quota
            EnforceLocalCacheQuota();

            string fileName = $"avatar_{Guid.NewGuid():N}.png";
            string path = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllBytes(path, pngBytes);
            return path;
        }

        public static void ApplyLocalAvatarToPlayer(string localPath, bool localOnly)
        {
            var d = SaveService.Data;
            if (d == null) return;
            // Delete previous custom avatar file (if any) to keep cache tidy
            if (!string.IsNullOrEmpty(d.customAvatarPath))
            {
                string old = Path.IsPathRooted(d.customAvatarPath)
                    ? d.customAvatarPath
                    : Path.Combine(Application.persistentDataPath, d.customAvatarPath);
                try { if (File.Exists(old)) File.Delete(old); } catch { /* best effort */ }
            }
            d.customAvatarPath    = Path.GetFileName(localPath);  // store just the filename; resolve at load
            d.moderationState     = localOnly ? "local" : "pending";
            d.avatarUpdatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            SaveService.Save();
        }

        private static Texture2D ResizeTexture(Texture2D src, int maxSide)
        {
            float scale = Mathf.Min(1f, (float)maxSide / Mathf.Max(src.width, src.height));
            int w = Mathf.Max(1, Mathf.RoundToInt(src.width * scale));
            int h = Mathf.Max(1, Mathf.RoundToInt(src.height * scale));
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.Default);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
            dst.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            dst.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return dst;
        }

        private static void EnforceLocalCacheQuota()
        {
            try
            {
                var dir = new DirectoryInfo(Application.persistentDataPath);
                var files = new List<FileInfo>();
                foreach (var f in dir.GetFiles("avatar_*.png")) files.Add(f);
                long total = 0; foreach (var f in files) total += f.Length;
                if (total <= LocalCacheBytes) return;
                files.Sort((a, b) => a.LastAccessTimeUtc.CompareTo(b.LastAccessTimeUtc));
                foreach (var f in files)
                {
                    if (total <= LocalCacheBytes) break;
                    total -= f.Length;
                    try { f.Delete(); } catch { }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AvatarUploadService] cache prune failed: {e.Message}");
            }
        }

        // ── File picker integration point (platform-specific) ─────────────────

        /// <summary>
        /// Open the platform file picker / camera and deliver bytes via callback.
        /// Stub right now; wire to UnityNativeGallery or your chosen picker.
        /// </summary>
        public static void PickFile(Action<byte[]> onPicked)
        {
#if UNITY_EDITOR
            // Editor: use the built-in file panel for testing
            string path = UnityEditor.EditorUtility.OpenFilePanel("Choose an avatar", "", "png,jpg,jpeg");
            if (string.IsNullOrEmpty(path)) { onPicked?.Invoke(null); return; }
            try { onPicked?.Invoke(File.ReadAllBytes(path)); }
            catch { onPicked?.Invoke(null); }
#elif UNITY_ANDROID || UNITY_IOS
            // TODO: wire UnityNativeGallery — drop the package in then enable below.
            // NativeGallery.GetImageFromGallery(p => {
            //   if (string.IsNullOrEmpty(p)) { onPicked?.Invoke(null); return; }
            //   try { onPicked?.Invoke(File.ReadAllBytes(p)); }
            //   catch { onPicked?.Invoke(null); }
            // }, "Choose an avatar");
            Debug.LogWarning("[AvatarUploadService] No file picker wired on mobile. Install UnityNativeGallery.");
            onPicked?.Invoke(null);
#else
            Debug.LogWarning("[AvatarUploadService] No file picker for this platform.");
            onPicked?.Invoke(null);
#endif
        }

        // ── Cloud upload (Path B — stubbed until Firestore is wired) ─────────

        /// <summary>
        /// PATH B STUB. When Firestore + Firebase Storage are wired, this:
        ///   1. Streams the file at <paramref name="localPath"/> to
        ///      Firebase Storage at avatars/{userId}/pending/{uuid}.png
        ///   2. Returns Success with isLocalOnly=false (so the UI shows "review in progress")
        ///   3. Cloud Function moderateAvatar.js triggers on the upload, runs
        ///      Google Cloud Vision SafeSearch, and writes a moderation result
        ///      to Firestore at users/{userId}/avatarModeration:
        ///        { state: "approved", url, decidedAt } | { state: "rejected", reason }
        ///   4. AvatarModerationListener.cs (a separate file you'll add when
        ///      Firestore lands) subscribes and updates PlayerData.moderationState.
        ///
        /// For now, this is unreachable (the caller has cloudAvailable=false).
        /// </summary>
        private static void CloudUpload(string localPath, Action<Result> onComplete)
        {
            // INTENTIONALLY UNREACHABLE TODAY — see Path B documentation.
            onComplete?.Invoke(new Result {
                status = Status.Success,
                localPath = localPath,
                isLocalOnly = false,
            });
        }
    }
}
