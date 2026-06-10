using UnityEngine;

namespace Sparq.UI
{
    /// <summary>
    /// Map-screen background music. Plays an adventure/exploration track while
    /// the Forest of Trials map is open. Stops cleanly when map closes or a
    /// stage launches into battle.
    /// </summary>
    public static class MapBgm
    {
        // Locked to BGM00 Hero — swapped from home per user request.
        private static readonly string[] MAP_TRACKS = {
            "Assets/Action RPG Music 1.6/BGM00hero.wav",
        };

        private static GameObject _host;
        private static AudioSource _source;

        public static void Ensure()
        {
            // Already running? Don't double-spawn.
            if (_source != null && _source.isPlaying) return;

            if (_host == null)
            {
                _host = new GameObject("Sparq_MapBgm");
                Object.DontDestroyOnLoad(_host);
                _source = _host.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.spatialBlend = 0f;
                _source.loop = true;
                _source.volume = 0.65f;
            }

            #if UNITY_EDITOR
            string track = MAP_TRACKS[Random.Range(0, MAP_TRACKS.Length)];
            // Streaming + Vorbis prevents choppy playback for big WAVs
            HomeBgm.ConfigureForStreaming(track);
            var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(track);
            if (clip != null)
            {
                _source.clip = clip;
                _source.Play();
                Debug.Log($"[MapBgm] Playing {System.IO.Path.GetFileName(track)} (looped, vol 0.65)");
            }
            else
            {
                Debug.LogWarning($"[MapBgm] Couldn't load track: {track}");
            }
            #endif
        }

        public static void Stop()
        {
            if (_source != null) _source.Stop();
        }

        public static void Pause()  { if (_source != null && _source.isPlaying) _source.Pause(); }
        public static void Resume() { if (_source != null) _source.UnPause(); }
    }
}
