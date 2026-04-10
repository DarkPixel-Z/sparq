using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Sparq.Systems;

namespace Sparq.UI
{
    /// <summary>
    /// Drives the Journal screen. Handles entry input, mood selection, save, and history list.
    /// </summary>
    public class JournalUI : MonoBehaviour
    {
        [Header("Write Panel")]
        [SerializeField] private TMP_InputField entryInput;
        [SerializeField] private TMP_Text       charCountText;
        [SerializeField] private Button         saveButton;

        [Header("Mood Buttons")]
        [SerializeField] private Toggle[] moodToggles;    // 😄 😌 😤 😔 🤩
        [SerializeField] private string[] moodEmojis = { "😄", "😌", "😤", "😔", "🤩" };

        [Header("Entry List")]
        [SerializeField] private Transform      entryContainer;
        [SerializeField] private GameObject     entryCardPrefab;
        [SerializeField] private ScrollRect     scrollRect;

        private string _selectedMood = "😄";

        private void OnEnable()
        {
            JournalSystem.Instance.OnEntrySaved.AddListener(OnEntrySaved);
            entryInput.onValueChanged.AddListener(OnInputChanged);
            saveButton.onClick.AddListener(TrySave);
            SetupMoodToggles();
            RefreshEntryList();
        }

        private void OnDisable()
        {
            JournalSystem.Instance.OnEntrySaved.RemoveListener(OnEntrySaved);
            entryInput.onValueChanged.RemoveListener(OnInputChanged);
            saveButton.onClick.RemoveListener(TrySave);
        }

        private void SetupMoodToggles()
        {
            for (int i = 0; i < moodToggles.Length; i++)
            {
                int idx = i;
                moodToggles[i].onValueChanged.AddListener(on => {
                    if (on) _selectedMood = moodEmojis[idx];
                });
            }
            if (moodToggles.Length > 0) moodToggles[0].isOn = true;
        }

        private void OnInputChanged(string text)
        {
            charCountText.text  = $"{text.Length} / 1000";
            saveButton.interactable = text.Trim().Length > 0;
        }

        private void TrySave()
        {
            string text = entryInput.text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            JournalSystem.Instance.SaveEntry(text, _selectedMood);
            entryInput.text = "";
        }

        private void OnEntrySaved(JournalEntry entry)
        {
            PrependEntryCard(entry);
            // Scroll to top
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private void RefreshEntryList()
        {
            foreach (Transform child in entryContainer)
                Destroy(child.gameObject);

            foreach (var entry in JournalSystem.Instance.Entries)
                PrependEntryCard(entry);
        }

        private void PrependEntryCard(JournalEntry entry)
        {
            var go   = Instantiate(entryCardPrefab, entryContainer);
            go.transform.SetAsFirstSibling();

            var texts = go.GetComponentsInChildren<TMP_Text>();
            // Expected order in prefab: [0]=mood, [1]=date, [2]=xp, [3]=body
            if (texts.Length >= 4)
            {
                texts[0].text = entry.moodEmoji;
                texts[1].text = FormatDate(entry.dateISO);
                texts[2].text = entry.xpAwarded > 0 ? $"+{entry.xpAwarded} XP" : "✓";
                texts[3].text = entry.text;
            }
        }

        private static string FormatDate(string iso)
        {
            if (DateTime.TryParse(iso, out var dt))
            {
                double hours = (DateTime.Now - dt).TotalHours;
                if (hours < 1)   return "Just now";
                if (hours < 24)  return $"{(int)hours}h ago";
                if (hours < 48)  return "Yesterday";
                return dt.ToString("MMM d");
            }
            return iso;
        }
    }
}
