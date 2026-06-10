using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Sparq.Systems
{
    /// <summary>
    /// Real journal entry system. Stores text + optional voice recording per
    /// entry, with optional lock (requires codex password to view).
    /// Persists entries to PlayerPrefs (text/meta) and audio WAVs to disk.
    /// </summary>
    public static class JournalService
    {
        [Serializable]
        public class Entry
        {
            public string id;
            public long   unix;
            public string text;
            public string mood;       // optional — same enum as MoodService
            public bool   locked;     // requires codex password to read
            public string voicePath;  // file path of voice clip (or "")
        }

        private const string KEY = "sparq.journal.entries.v1";
        public static event Action OnChanged;

        private static List<Entry> _cache;

        public static List<Entry> All()
        {
            if (_cache != null) return _cache;
            _cache = new List<Entry>();
            string raw = PlayerPrefs.GetString(KEY, "");
            if (string.IsNullOrEmpty(raw)) return _cache;
            foreach (var seg in raw.Split('|'))
            {
                if (string.IsNullOrEmpty(seg)) continue;
                var p = seg.Split(new[] { '⟨' }, 6);
                if (p.Length < 5) continue;
                _cache.Add(new Entry
                {
                    id     = p[0],
                    unix   = long.TryParse(p[1], out long u) ? u : 0,
                    text   = p[2].Replace("⟪p⟫", "|"),
                    mood   = p[3],
                    locked = p[4] == "1",
                    voicePath = p.Length > 5 ? p[5] : "",
                });
            }
            // newest first
            _cache.Sort((a, b) => b.unix.CompareTo(a.unix));
            return _cache;
        }

        public static Entry Add(string text, string mood = "", bool locked = false, string voicePath = "", long unixOverride = 0)
        {
            All();
            var entry = new Entry
            {
                id = Guid.NewGuid().ToString("N").Substring(0, 8),
                unix = unixOverride > 0 ? unixOverride : DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                text = text ?? "",
                mood = mood ?? "",
                locked = locked,
                voicePath = voicePath ?? "",
            };
            // Insert in correct chronological slot (newest first)
            int idx = 0;
            while (idx < _cache.Count && _cache[idx].unix > entry.unix) idx++;
            _cache.Insert(idx, entry);
            Save();
            try { Sparq.Core.SaveService.Data.journalCount = _cache.Count; Sparq.Core.SaveService.Save(); } catch {}
            OnChanged?.Invoke();
            return entry;
        }

        public static void Delete(string id)
        {
            All();
            for (int i = 0; i < _cache.Count; i++)
                if (_cache[i].id == id)
                {
                    if (!string.IsNullOrEmpty(_cache[i].voicePath))
                    { try { File.Delete(_cache[i].voicePath); } catch {} }
                    _cache.RemoveAt(i);
                    Save();
                    OnChanged?.Invoke();
                    return;
                }
        }

        public static int LockedCount()
        {
            int n = 0;
            foreach (var e in All()) if (e.locked) n++;
            return n;
        }

        private static void Save()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _cache.Count; i++)
            {
                if (i > 0) sb.Append('|');
                var e = _cache[i];
                sb.Append(e.id).Append('⟨')
                  .Append(e.unix).Append('⟨')
                  .Append((e.text ?? "").Replace("|", "⟪p⟫")).Append('⟨')
                  .Append(e.mood ?? "").Append('⟨')
                  .Append(e.locked ? "1" : "0").Append('⟨')
                  .Append(e.voicePath ?? "");
            }
            PlayerPrefs.SetString(KEY, sb.ToString());
            PlayerPrefs.Save();
        }

        // ───────── Voice recording ─────────
        private static AudioClip _activeRecord;
        private static string _activeDevice;
        public static int  RecordSampleRate = 22050;
        public static int  RecordMaxSeconds = 60;

        public static bool IsRecording => _activeRecord != null && Microphone.IsRecording(_activeDevice);

        public static bool StartRecording()
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[Journal] No microphone device available.");
                return false;
            }
            _activeDevice = Microphone.devices[0];
            _activeRecord = Microphone.Start(_activeDevice, false, RecordMaxSeconds, RecordSampleRate);
            return _activeRecord != null;
        }

        public static string StopRecordingAndSave()
        {
            if (_activeRecord == null) return "";
            int pos = Microphone.GetPosition(_activeDevice);
            Microphone.End(_activeDevice);
            // Trim to actual recorded length
            var trimmed = AudioClip.Create("voice", Mathf.Max(1, pos), _activeRecord.channels, _activeRecord.frequency, false);
            float[] samples = new float[Mathf.Max(1, pos) * _activeRecord.channels];
            _activeRecord.GetData(samples, 0);
            trimmed.SetData(samples, 0);
            _activeRecord = null;

            string dir = Path.Combine(Application.persistentDataPath, "journal_voice");
            try { Directory.CreateDirectory(dir); } catch {}
            string filename = $"voice_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.wav";
            string fullPath = Path.Combine(dir, filename);
            try { WriteWav(fullPath, trimmed); } catch (Exception ex) { Debug.LogError($"[Journal] Voice save failed: {ex.Message}"); return ""; }
            return fullPath;
        }

        public static AudioClip LoadVoice(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try { return ReadWav(path); } catch { return null; }
        }

        // Minimal WAV writer — 16-bit PCM, mono/stereo
        private static void WriteWav(string path, AudioClip clip)
        {
            int sampleCount = clip.samples * clip.channels;
            float[] data = new float[sampleCount];
            clip.GetData(data, 0);
            using (var fs = new FileStream(path, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                int byteRate = clip.frequency * clip.channels * 2;
                int dataSize = sampleCount * 2;
                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + dataSize);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16); bw.Write((short)1); bw.Write((short)clip.channels);
                bw.Write(clip.frequency); bw.Write(byteRate); bw.Write((short)(clip.channels * 2)); bw.Write((short)16);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write(dataSize);
                for (int i = 0; i < sampleCount; i++)
                {
                    short s = (short)(Mathf.Clamp(data[i], -1f, 1f) * 32767);
                    bw.Write(s);
                }
            }
        }

        private static AudioClip ReadWav(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            int channels = BitConverter.ToInt16(bytes, 22);
            int frequency = BitConverter.ToInt32(bytes, 24);
            int dataStart = 44;
            int sampleCount = (bytes.Length - dataStart) / 2;
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                short s = BitConverter.ToInt16(bytes, dataStart + i * 2);
                samples[i] = s / 32768f;
            }
            var clip = AudioClip.Create("voice", sampleCount / Mathf.Max(1, channels), Mathf.Max(1, channels), frequency, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }

    /// <summary>
    /// Codex-style password for locking sensitive journal entries.
    /// Player picks an ordered sequence of 4 arcane symbols on first setup,
    /// re-enters them to unlock. Persists in PlayerPrefs as a hash.
    /// </summary>
    public static class JournalCodex
    {
        // 12 ancient codex glyphs — pick 4 to form a password
        public static readonly string[] GLYPHS = { "✦", "☾", "❄", "♛", "✧", "⚡", "☘", "❀", "⚜", "✠", "☀", "❖" };
        private const string KEY = "sparq.journal.codex.v1";

        public static bool IsSet => !string.IsNullOrEmpty(PlayerPrefs.GetString(KEY, ""));

        public static void Set(string sequence)
        {
            if (string.IsNullOrEmpty(sequence) || sequence.Length < 3)
            { PlayerPrefs.DeleteKey(KEY); PlayerPrefs.Save(); return; }
            PlayerPrefs.SetString(KEY, Hash(sequence));
            PlayerPrefs.Save();
        }

        public static bool Verify(string sequence) =>
            !string.IsNullOrEmpty(sequence) && Hash(sequence) == PlayerPrefs.GetString(KEY, "");

        public static void Clear()
        { PlayerPrefs.DeleteKey(KEY); PlayerPrefs.Save(); }

        // Trivial salted hash — deters casual peeking, not real security
        private static string Hash(string s)
        {
            unchecked
            {
                ulong h = 14695981039346656037UL;
                string salted = "sparq-codex-v1:" + s;
                foreach (char c in salted) { h ^= c; h *= 1099511628211UL; }
                return h.ToString("x16");
            }
        }
    }
}
