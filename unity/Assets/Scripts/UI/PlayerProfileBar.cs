using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sparq.Core;

namespace Sparq.UI
{
    /// <summary>
    /// Top horizontal banner on the home screen — the "Top Heroes-style" identity
    /// chip showing avatar + name + level + XP-to-next bar. Tap to open
    /// AvatarPickerPanel.
    ///
    /// Layout (left → right):
    ///   [ Avatar (40px) | Name + Level (stacked) | XP bar (flex) | Pending? badge ]
    ///
    /// Render priority for the avatar image:
    ///   1. If customAvatarPath set AND moderation state in {"local","approved"} → load from disk
    ///   2. Otherwise → load preset by avatarPresetId
    ///   3. If preset missing → built-in fallback (knight_01)
    ///
    /// Wire-up in Unity Editor:
    ///   - Drop this component on the empty banner GameObject under the home Canvas
    ///   - Assign the 4 serialized refs (avatarImage, nameLabel, levelLabel, xpFill,
    ///     xpLabel, pendingBadge)
    ///   - The Button on the root opens AvatarPickerPanel via OnPointerClick
    /// </summary>
    public class PlayerProfileBar : MonoBehaviour
    {
        [Header("UI refs (assign in Inspector)")]
        public Image     avatarImage;
        public TMP_Text  nameLabel;
        public TMP_Text  levelLabel;
        public Image     xpFill;            // a Filled Image (Image Type = Filled, Fill Method = Horizontal)
        public TMP_Text  xpLabel;           // optional — "3,240 / 5,000"
        public GameObject pendingBadge;     // small "in review" badge shown when moderationState == "pending"
        public Button    rootButton;        // tap to open AvatarPickerPanel

        [Header("Visuals")]
        public Sprite defaultAvatar;        // emergency fallback if preset resource missing

        [Header("Behavior")]
        public bool openPickerOnTap = true;

        void OnEnable()
        {
            SaveService.OnLoaded += Refresh;
            SaveService.OnSaved  += Refresh;
            if (rootButton != null && openPickerOnTap)
                rootButton.onClick.AddListener(OpenPicker);
            Refresh();
        }

        void OnDisable()
        {
            SaveService.OnLoaded -= Refresh;
            SaveService.OnSaved  -= Refresh;
            if (rootButton != null)
                rootButton.onClick.RemoveListener(OpenPicker);
        }

        public void Refresh()
        {
            var d = SaveService.Data;
            if (d == null) return;

            // Name + level
            if (nameLabel  != null) nameLabel.text  = string.IsNullOrEmpty(d.playerName) ? "Sparq User" : d.playerName;
            if (levelLabel != null) levelLabel.text = $"Lv {d.level}";

            // XP bar
            float fill = (d.xpToNextLevel > 0)
                ? Mathf.Clamp01((float)d.currentXP / d.xpToNextLevel)
                : 0f;
            if (xpFill  != null) xpFill.fillAmount = fill;
            if (xpLabel != null) xpLabel.text = $"{d.currentXP:N0} / {d.xpToNextLevel:N0}";

            // Avatar
            if (avatarImage != null)
            {
                Sprite s = ResolveAvatarSprite(d);
                avatarImage.sprite = s != null ? s : defaultAvatar;
            }

            // Pending badge — only when a custom upload is awaiting moderation
            if (pendingBadge != null)
            {
                bool pending = !string.IsNullOrEmpty(d.customAvatarPath) && d.moderationState == "pending";
                pendingBadge.SetActive(pending);
            }
        }

        public static Sprite ResolveAvatarSprite(PlayerData d)
        {
            // 1. Custom upload — only if approved (or local-only)
            if (!string.IsNullOrEmpty(d.customAvatarPath) &&
                (d.moderationState == "approved" || d.moderationState == "local"))
            {
                Sprite custom = TryLoadCustomSprite(d.customAvatarPath);
                if (custom != null) return custom;
                // If file is missing on disk, fall through to preset
            }
            // 2. Preset
            return PresetAvatars.LoadSprite(d.avatarPresetId);
        }

        /// <summary>
        /// Load a PNG/JPG from persistent storage into a runtime Sprite.
        /// Files are written by AvatarUploadService.SaveLocalAvatar().
        /// Cached per-frame is fine — Unity textures are reference-counted.
        /// </summary>
        public static Sprite TryLoadCustomSprite(string relativeOrAbsolutePath)
        {
            string path = relativeOrAbsolutePath;
            if (!Path.IsPathRooted(path))
                path = Path.Combine(Application.persistentDataPath, path);
            if (!File.Exists(path)) return null;
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(bytes);  // auto-detects PNG/JPG
                // Center-pivot, 100 PPU is fine for UI Image
                return Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PlayerProfileBar] Failed to load custom avatar from {path}: {e.Message}");
                return null;
            }
        }

        void OpenPicker()
        {
            AvatarPickerPanel.Open();
        }
    }
}
