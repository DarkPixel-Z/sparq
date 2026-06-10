using System.Collections.Generic;
using UnityEngine;

namespace Sparq.Safety
{
    /// <summary>
    /// User-side mute list. Player can block someone — their messages will
    /// be hidden in chat client-side (server-side blocking is a separate
    /// concern when we have a real backend).
    ///
    /// Persisted in PlayerPrefs as a comma-separated list of player IDs/names.
    /// </summary>
    public static class BlockList
    {
        private const string KEY = "sparq.safety.blocked_players";

        private static HashSet<string> _blocked;
        public static event System.Action OnChanged;

        public static void Block(string playerNameOrId)
        {
            if (string.IsNullOrWhiteSpace(playerNameOrId)) return;
            EnsureLoaded();
            if (_blocked.Add(playerNameOrId.Trim().ToLower()))
            {
                Save();
                Debug.Log($"[BlockList] Blocked '{playerNameOrId}'.");
                OnChanged?.Invoke();
            }
        }

        public static void Unblock(string playerNameOrId)
        {
            if (string.IsNullOrWhiteSpace(playerNameOrId)) return;
            EnsureLoaded();
            if (_blocked.Remove(playerNameOrId.Trim().ToLower()))
            {
                Save();
                Debug.Log($"[BlockList] Unblocked '{playerNameOrId}'.");
                OnChanged?.Invoke();
            }
        }

        public static bool IsBlocked(string playerNameOrId)
        {
            if (string.IsNullOrWhiteSpace(playerNameOrId)) return false;
            EnsureLoaded();
            return _blocked.Contains(playerNameOrId.Trim().ToLower());
        }

        public static List<string> All()
        {
            EnsureLoaded();
            return new List<string>(_blocked);
        }

        public static int Count
        {
            get { EnsureLoaded(); return _blocked.Count; }
        }

        public static void ClearAll()
        {
            EnsureLoaded();
            _blocked.Clear();
            Save();
            OnChanged?.Invoke();
        }

        private static void EnsureLoaded()
        {
            if (_blocked != null) return;
            _blocked = new HashSet<string>();
            string csv = PlayerPrefs.GetString(KEY, "");
            if (string.IsNullOrEmpty(csv)) return;
            foreach (var s in csv.Split(','))
                if (!string.IsNullOrWhiteSpace(s)) _blocked.Add(s.Trim().ToLower());
        }
        private static void Save()
        {
            PlayerPrefs.SetString(KEY, string.Join(",", _blocked));
            PlayerPrefs.Save();
        }
    }
}
