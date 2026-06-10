using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace Sparq.UI
{
    /// <summary>
    /// Renders the home-screen quest list. Builds rows procedurally (no prefab needed)
    /// and rebuilds on any change. Tapping a quest marks it done + awards XP.
    /// </summary>
    public class QuestListUI : MonoBehaviour
    {
        [SerializeField] private Transform rowsParent;  // usually 'this' or a child VerticalLayoutGroup

        private readonly List<GameObject> _rowObjects = new();
        private Sparq.Systems.QuestManager _manager;

        private void OnEnable()
        {
            _manager = Sparq.Systems.QuestManager.Instance;
            if (_manager != null)
            {
                _manager.OnQuestCompleted += HandleQuestDone;
                _manager.OnDailyReset     += Rebuild;
                _manager.OnFoodDropped    += HandleFoodDrop;
            }
            Rebuild();
        }
        private void OnDisable()
        {
            if (_manager != null)
            {
                _manager.OnQuestCompleted -= HandleQuestDone;
                _manager.OnDailyReset     -= Rebuild;
                _manager.OnFoodDropped    -= HandleFoodDrop;
            }
        }

        private void HandleFoodDrop(string foodId)
        {
            var f = Sparq.Systems.PetService.FindFood(foodId);
            if (f == null) return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                XPFloater.Spawn(canvas.transform, transform.position + new Vector3(0, 80, 0),
                    $"🍓 +1 {f.name} for pet!", new Color(1f, 0.55f, 0.30f));
            }
        }

        private void Start()
        {
            // Defer 1 frame so QuestManager's Start has run and seeded defaults
            StartCoroutine(DeferredRebuild());
        }
        private System.Collections.IEnumerator DeferredRebuild()
        {
            // Poll for QuestManager to come online (it may instantiate in Awake of another root)
            for (int i = 0; i < 30; i++)   // up to ~30 frames (~0.5s)
            {
                _manager = Sparq.Systems.QuestManager.Instance;
                if (_manager != null) break;
                yield return null;
            }

            if (_manager != null)
            {
                _manager.OnQuestCompleted += HandleQuestDone;
                _manager.OnDailyReset     += Rebuild;
                // Make sure quests are seeded before we render
                _manager.CheckDailyReset();
            }
            Rebuild();
        }

        private void Update()
        {
            // Safety: if data has tasks but UI is empty (race on first launch), rebuild
            if (_rowObjects.Count == 0)
            {
                var data = Sparq.Core.SaveService.Data;
                if (data != null && data.customTasks != null && data.customTasks.Count > 0)
                {
                    Rebuild();
                }
            }
        }

        private void HandleQuestDone(Sparq.Core.CustomTask q)
        {
            Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.QuestComplete);
            // Spawn a floating XP number where the completed row was
            var row = FindRowForQuest(q);
            if (row != null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    var worldPos = row.transform.position;
                    XPFloater.Spawn(canvas.transform, worldPos, $"+{q.xp} XP");
                }
                // Punch the row briefly
                StartCoroutine(RowPunch(row.transform));
            }
            Rebuild();
        }

        private GameObject FindRowForQuest(Sparq.Core.CustomTask quest)
        {
            foreach (var r in _rowObjects)
            {
                if (r != null && r.name.Contains(quest.name)) return r;
            }
            return null;
        }

        private System.Collections.IEnumerator RowPunch(Transform t)
        {
            Vector3 baseScale = t.localScale;
            float duration = 0.25f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = elapsed / duration;
                float s = 1f + Mathf.Sin(k * Mathf.PI) * 0.1f; // 1 → 1.1 → 1
                if (t != null) t.localScale = baseScale * s;
                yield return null;
            }
            if (t != null) t.localScale = baseScale;
        }

        public void Rebuild()
        {
            var parent = rowsParent != null ? rowsParent : transform;
            // Destroy old rows
            foreach (var r in _rowObjects)
            {
                if (r != null) Destroy(r);
            }
            _rowObjects.Clear();

            var manager = _manager ?? Sparq.Systems.QuestManager.Instance;
            if (manager == null) return;

            var quests = manager.GetActiveQuests();
            foreach (var quest in quests)
            {
                _rowObjects.Add(BuildRow(parent, quest));
            }

            // "+ Add Custom Quest" row at the bottom
            _rowObjects.Add(BuildAddRow(parent));
        }

        private GameObject BuildAddRow(Transform parent)
        {
            var row = new GameObject("AddQuestRow");
            row.transform.SetParent(parent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 48);

            var bg = row.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.65f, 0.40f, 0.85f);

            // Layout
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(14, 14, 6, 6);
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(row.transform, false);
            lblGO.AddComponent<RectTransform>();
            var tm = lblGO.AddComponent<TextMeshProUGUI>();
            tm.text = "+ Add your own quest";
            tm.fontSize = 18;
            tm.fontStyle = FontStyles.Bold;
            tm.color = Color.white;
            tm.alignment = TextAlignmentOptions.Center;

            var btn = row.AddComponent<Button>();
            btn.onClick.AddListener(() => CustomQuestCreator.Show());

            return row;
        }

        private GameObject BuildRow(Transform parent, Sparq.Core.CustomTask quest)
        {
            var row = new GameObject("QuestRow_" + quest.name);
            row.transform.SetParent(parent, false);
            var rowRT = row.AddComponent<RectTransform>();
            rowRT.sizeDelta = new Vector2(0, 58);

            // Background
            var bg = row.AddComponent<Image>();
            bg.color = quest.done
                ? new Color(0.15f, 0.32f, 0.22f, 0.85f)   // done = greenish
                : new Color(0.22f, 0.18f, 0.4f,  0.85f);  // todo = dark purple

            // Layout — horizontal: checkbox | title | xp chip
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 8, 8);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            // Checkbox (left)
            var check = new GameObject("Check");
            check.transform.SetParent(row.transform, false);
            var checkRT = check.AddComponent<RectTransform>();
            checkRT.sizeDelta = new Vector2(36, 36);
            var checkImg = check.AddComponent<Image>();
            checkImg.color = quest.done ? new Color(0.1f, 0.9f, 0.4f) : new Color(1f, 1f, 1f, 0.15f);
            var checkLE = check.AddComponent<LayoutElement>();
            checkLE.preferredWidth = 36; checkLE.preferredHeight = 36;
            if (quest.done)
            {
                var tick = new GameObject("Tick");
                tick.transform.SetParent(check.transform, false);
                var tickRT = tick.AddComponent<RectTransform>();
                tickRT.anchorMin = Vector2.zero; tickRT.anchorMax = Vector2.one;
                tickRT.offsetMin = Vector2.zero; tickRT.offsetMax = Vector2.zero;
                var tickTxt = tick.AddComponent<TextMeshProUGUI>();
                tickTxt.text = "DONE"; // ASCII — universal font support
                tickTxt.alignment = TextAlignmentOptions.Center;
                tickTxt.fontSize = 12;
                tickTxt.color = Color.white;
                tickTxt.fontStyle = FontStyles.Bold;
            }

            // Title (middle, flex)
            var title = new GameObject("Title");
            title.transform.SetParent(row.transform, false);
            var titleRT = title.AddComponent<RectTransform>();
            var titleTxt = title.AddComponent<TextMeshProUGUI>();
            titleTxt.text = quest.name;
            titleTxt.fontSize = 20;
            titleTxt.alignment = TextAlignmentOptions.MidlineLeft;
            titleTxt.color = quest.done ? new Color(0.7f, 1f, 0.75f) : Color.white;
            if (quest.done)
            {
                titleTxt.fontStyle = FontStyles.Strikethrough;
            }
            var titleLE = title.AddComponent<LayoutElement>();
            titleLE.flexibleWidth = 1f;

            // XP chip (right)
            var chip = new GameObject("XPChip");
            chip.transform.SetParent(row.transform, false);
            var chipRT = chip.AddComponent<RectTransform>();
            chipRT.sizeDelta = new Vector2(72, 36);
            var chipImg = chip.AddComponent<Image>();
            chipImg.color = new Color(1f, 0.86f, 0.1f, quest.done ? 0.35f : 0.9f);
            var chipTxt = new GameObject("XPText");
            chipTxt.transform.SetParent(chip.transform, false);
            var chipTxtRT = chipTxt.AddComponent<RectTransform>();
            chipTxtRT.anchorMin = Vector2.zero; chipTxtRT.anchorMax = Vector2.one;
            chipTxtRT.offsetMin = Vector2.zero; chipTxtRT.offsetMax = Vector2.zero;
            var chipTm = chipTxt.AddComponent<TextMeshProUGUI>();
            chipTm.text = $"+{quest.xp} XP";
            chipTm.fontSize = 14;
            chipTm.alignment = TextAlignmentOptions.Center;
            chipTm.color = new Color(0.12f, 0.05f, 0.2f);
            chipTm.fontStyle = FontStyles.Bold;
            var chipLE = chip.AddComponent<LayoutElement>();
            chipLE.preferredWidth = 72; chipLE.preferredHeight = 36;

            // Click handler — only for incomplete quests
            if (!quest.done)
            {
                var trigger = row.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                entry.callback.AddListener((e) =>
                {
                    // Spoon Check is interactive — tapping opens the
                    // 1/2/3-spoon picker; the panel completes the quest
                    // through CompleteQuest after a pick is made.
                    if (quest != null && quest.questId == "spoon_check")
                    {
                        Sparq.UI.SpoonCheckPanel.Show();
                        return;
                    }
                    var mgr = Sparq.Systems.QuestManager.Instance;
                    mgr?.CompleteQuest(quest);
                });
                trigger.triggers.Add(entry);
            }

            return row;
        }
    }
}
