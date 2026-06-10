using UnityEngine;

namespace Sparq.UI
{
    /// <summary>
    /// Looping home-page background music. Plays a town/hero track from the
    /// Action RPG Music pack at moderate volume. Auto-fades when battles start.
    ///
    /// Wired from BottomNavBar.Start() via HomeBgm.Ensure().
    /// </summary>
    public static class HomeBgm
    {
        // User-supplied loop (Freesound #853586 by bassimat — "elastic metallic
        // drum groove loop at 120bpm"). Replaces the town-theme rotation, which
        // read as too chipper for the lobby vibe.
        private static readonly string[] HOME_TRACKS = {
            "Assets/Audio/HomeBgm_ElasticMetallic.wav",
        };

        private static GameObject _host;
        private static AudioSource _source;
        private static bool _started;

        public static void Ensure()
        {
            if (_started) return;
            _started = true;

            _host = new GameObject("Sparq_HomeBgm");
            Object.DontDestroyOnLoad(_host);
            _source = _host.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.loop = true;
            _source.volume = 0.18f;

            #if UNITY_EDITOR
            string track = HOME_TRACKS[Random.Range(0, HOME_TRACKS.Length)];
            // Auto-fix import settings to prevent choppy playback (was Decompress-on-Load
            // for big WAV files, causing hitches). Switch to Streaming + Vorbis.
            ConfigureForStreaming(track);

            var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(track);
            if (clip != null)
            {
                _source.clip = clip;
                _source.Play();
                Debug.Log($"[HomeBgm] Playing {System.IO.Path.GetFileName(track)} (streaming, vorbis, vol 0.18)");
            }
            else
            {
                Debug.LogWarning($"[HomeBgm] Couldn't load track: {track}");
            }
            #endif
        }

        #if UNITY_EDITOR
        // Set music files to stream from disk (instead of decompress-on-load into RAM),
        // and use Vorbis compression. Eliminates choppy/stuttering playback for big WAVs.
        // PUBLIC so SquadBattle / TurnBasedBattle can reuse for their music tracks.
        public static void ConfigureForStreaming(string assetPath)
        {
            var imp = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.AudioImporter;
            if (imp == null) return;
            var s = imp.defaultSampleSettings;
            bool changed = false;
            if (s.loadType != AudioClipLoadType.Streaming)
            { s.loadType = AudioClipLoadType.Streaming; changed = true; }
            // AudioCompressionFormat lives in the UnityEngine namespace in Unity 6
            if (s.compressionFormat != AudioCompressionFormat.Vorbis)
            { s.compressionFormat = AudioCompressionFormat.Vorbis; changed = true; }
            if (Mathf.Abs(s.quality - 0.7f) > 0.01f)
            { s.quality = 0.7f; changed = true; }
            // Preload setting moved into the SampleSettings struct (the old AudioImporter.preloadAudioData is obsolete)
            if (s.preloadAudioData)
            { s.preloadAudioData = false; changed = true; }
            if (changed)
            {
                imp.defaultSampleSettings = s;
                imp.SaveAndReimport();
                Debug.Log($"[HomeBgm] Reimported {System.IO.Path.GetFileName(assetPath)} as Streaming/Vorbis");
            }
        }
        #endif

        // Optional public controls — for battle scenes to mute home BGM
        public static void Pause()  { if (_source != null && _source.isPlaying) _source.Pause(); }
        public static void Resume() { if (_source != null) _source.UnPause(); }
        public static void Stop()
        {
            if (_source != null) _source.Stop();
            if (_host != null)   { Object.Destroy(_host); _host = null; }
            _started = false;
        }
    }
}
