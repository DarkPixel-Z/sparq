using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Sparq.Systems
{
    [Serializable]
    public class QuestTask
    {
        public string  id;
        public string  title;
        public string  category;   // Movement | Wellness | Focus | Custom
        public int     xpReward;
        public bool    completed;
        public string  scheduledTime;
    }

    /// <summary>
    /// Manages daily quests. Resets at midnight. Syncs with backend (future).
    /// </summary>
    public class TaskManager : MonoBehaviour
    {
        public static TaskManager Instance { get; private set; }

        public UnityEvent<QuestTask> OnTaskCompleted;
        public UnityEvent<int>       OnAllTasksDone;   // arg: bonus XP awarded

        [SerializeField] private List<QuestTask> _tasks = new();

        private const int ALL_DONE_BONUS = 25;
        private const string SAVE_KEY    = "sparq_tasks";

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            LoadOrSeedTasks();
        }

        public IReadOnlyList<QuestTask> Tasks => _tasks.AsReadOnly();

        public void CompleteTask(string taskId)
        {
            var task = _tasks.Find(t => t.id == taskId);
            if (task == null || task.completed) return;

            task.completed = true;
            Sparq.Core.XPSystem.Instance.Award(task.xpReward);
            OnTaskCompleted?.Invoke(task);
            Save();

            if (_tasks.TrueForAll(t => t.completed))
            {
                Sparq.Core.XPSystem.Instance.Award(ALL_DONE_BONUS);
                OnAllTasksDone?.Invoke(ALL_DONE_BONUS);
            }
        }

        public void AddCustomTask(string title)
        {
            int xp = UnityEngine.Random.Range(1, 4) * 10;
            _tasks.Add(new QuestTask {
                id           = Guid.NewGuid().ToString(),
                title        = title,
                category     = "Custom",
                xpReward     = xp,
                completed    = false,
                scheduledTime = "",
            });
            Save();
        }

        private void LoadOrSeedTasks()
        {
            string json = PlayerPrefs.GetString(SAVE_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                // TODO: deserialize list from JSON wrapper
                return;
            }

            // Seed defaults
            _tasks = new List<QuestTask> {
                new() { id="1", title="Morning walk 🌅",              category="Movement", xpReward=15, completed=false, scheduledTime="8:00 AM" },
                new() { id="2", title="Drink 8 glasses of water 💧",  category="Wellness", xpReward=20, completed=false, scheduledTime="All day" },
                new() { id="3", title="Focus session (25 min) ⏱️",    category="Focus",    xpReward=25, completed=false, scheduledTime="2:00 PM" },
            };
        }

        private void Save()
        {
            // TODO: serialize to JSON and store in PlayerPrefs
            PlayerPrefs.Save();
        }
    }
}
