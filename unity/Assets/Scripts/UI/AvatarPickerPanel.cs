using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sparq.Core;

namespace Sparq.UI
{
    /// <summary>
    /// Modal panel that opens when the player taps their PlayerProfileBar.
    /// Two sections:
    ///   1. Preset grid — class-tinted defaults, pets, vibes. Locked presets show
    ///      a grey overlay + "Lv N" badge.
    ///   2. Upload button — opens the system file picker / camera. Wired via
    ///      AvatarUploadService; only shown when allowCustomUpload is true.
    ///
    /// Lifecycle:
    ///   - Singleton via static instance; Open() shows the panel
    ///   - On preset tap: SaveService.Data.avatarPresetId = id; clear customAvatarPath
    ///   - On upload: see AvatarUploadService.PickAndUpload()
    /// </summary>
    public class AvatarPickerPanel : MonoBehaviour
    {
        // ── Singleton plumbing ────────────────────────────────────────────────
        private static AvatarPickerPanel _instance;
        public static AvatarPickerPanel Instance => _instance;

        public static void Open()
        {
            if (_instance == null)
            {
                // Lazy-instantiate from Resources (drop a prefab at Resources/UI/AvatarPickerPanel.prefab)
                var prefab = Resources.Load<GameObject>("UI/AvatarPickerPanel");
                if (prefab == null)
                {
                    Debug.LogWarning("[AvatarPickerPanel] Missing prefab at Resources/UI/AvatarPickerPanel — picker disabled.");
                    return;
                }
                var go = Instantiate(prefab);
                _instance = go.GetComponent<AvatarPickerPanel>();
            }
            _instance.gameObject.SetActive(true);
            _instance.PopulateGrid();
        }

        public static void Close()
        {
            if (_instance != null) _instance.gameObject.SetActive(false);
        }

        // ── UI refs (assign in Inspector) ─────────────────────────────────────
        [Header("Refs")]
        public Transform   gridContainer;        // ScrollView/Viewport/Content
        public GameObject  presetCellPrefab;     // a Button + Image + lock overlay
        public Button      closeButton;
        public Button      uploadButton;         // hidden if !allowCustomUpload
        public TMP_Text    statusLabel;          // shown on errors / pending state

        [Header("Behavior")]
        [Tooltip("Path A safety switch: enable to allow custom uploads. Children (under-13) " +
                 "must NEVER be allowed to upload — gate this on age tier in parent code.")]
        public bool allowCustomUpload = true;

        [Header("Visuals")]
        public Color lockedTint  = new Color(0.4f, 0.4f, 0.4f, 1f);
        public Color selectedTint = new Color(1f, 0.85f, 0.2f, 1f);

        private readonly List<GameObject> _cells = new List<GameObject>();

        void Awake()
        {
            if (_instance == null) _instance = this;
            if (closeButton  != null) closeButton.onClick.AddListener(Close);
            if (uploadButton != null)
            {
                uploadButton.gameObject.SetActive(allowCustomUpload);
                uploadButton.onClick.AddListener(OnUploadTapped);
            }
        }

        void PopulateGrid()
        {
            foreach (var c in _cells) Destroy(c);
            _cells.Clear();

            var d = SaveService.Data;
            if (d == null || gridContainer == null || presetCellPrefab == null) return;

            foreach (var preset in PresetAvatars.ALL)
            {
                bool unlocked = PresetAvatars.IsUnlocked(preset, d.level);
                bool selected = preset.id == d.avatarPresetId && string.IsNullOrEmpty(d.customAvatarPath);

                var cell = Instantiate(presetCellPrefab, gridContainer);
                _cells.Add(cell);

                // Convention: cell prefab has an Image (the avatar), a Text (label), a GameObject ("Lock") for locked overlay
                var img = cell.GetComponentInChildren<Image>(includeInactive: true);
                var txt = cell.GetComponentInChildren<TMP_Text>(includeInactive: true);
                var lockOverlay = cell.transform.Find("Lock");
                var btn = cell.GetComponent<Button>();

                if (img != null)
                {
                    var sprite = PresetAvatars.LoadSprite(preset.id);
                    if (sprite != null) img.sprite = sprite;
                    img.color = unlocked ? Color.white : lockedTint;
                }
                if (txt != null) txt.text = unlocked ? preset.label : $"Lv {preset.minLevel}";
                if (lockOverlay != null) lockOverlay.gameObject.SetActive(!unlocked);

                if (btn != null)
                {
                    string presetId = preset.id;
                    bool   canPick  = unlocked;
                    btn.interactable = canPick;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => SelectPreset(presetId));
                    if (selected)
                    {
                        var colors = btn.colors;
                        colors.normalColor = selectedTint;
                        btn.colors = colors;
                    }
                }
            }
        }

        public void SelectPreset(string presetId)
        {
            var d = SaveService.Data;
            if (d == null) return;
            d.avatarPresetId      = presetId;
            d.customAvatarPath    = "";          // clear any custom override
            d.moderationState     = "local";
            d.avatarUpdatedAtUnix = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            SaveService.Save();
            Close();
        }

        void OnUploadTapped()
        {
            if (!allowCustomUpload)
            {
                if (statusLabel != null) statusLabel.text = "Photo upload is disabled.";
                return;
            }
            // COPPA precaution: child age tier never reaches here (UI hides uploadButton).
            // Belt-and-suspenders check would live in PlayerData if Sparq gains age_tier later.
            if (statusLabel != null) statusLabel.text = "Choose a photo...";
            AvatarUploadService.PickAndUpload(OnUploadResult);
        }

        void OnUploadResult(AvatarUploadService.Result result)
        {
            if (statusLabel == null) return;
            switch (result.status)
            {
                case AvatarUploadService.Status.Success:
                    statusLabel.text = result.isLocalOnly
                        ? "Photo set ✓"
                        : "Uploaded — review in progress. Your photo will appear publicly once approved.";
                    PopulateGrid();
                    if (result.isLocalOnly) Close();   // local mode is instant
                    break;
                case AvatarUploadService.Status.Cancelled:
                    statusLabel.text = "";
                    break;
                case AvatarUploadService.Status.TooLarge:
                    statusLabel.text = $"Photo too large (max {AvatarUploadService.MaxBytes / 1024 / 1024} MB).";
                    break;
                case AvatarUploadService.Status.InvalidFormat:
                    statusLabel.text = "Use a JPG or PNG.";
                    break;
                case AvatarUploadService.Status.AutoRejected:
                    statusLabel.text = "That photo can't be used (safety filter). Try another.";
                    break;
                case AvatarUploadService.Status.Error:
                default:
                    statusLabel.text = "Upload failed. Try again.";
                    break;
            }
        }
    }
}
