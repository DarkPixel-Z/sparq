using UnityEngine;
using System.Collections.Generic;

namespace Sparq.Audio
{
    /// <summary>
    /// Global sound manager. Auto-creates itself on first access.
    /// Holds procedural chiptune clips + a pool of AudioSources for overlapping sfx.
    ///
    /// Usage anywhere:
    ///   SoundManager.Play(SoundManager.Sfx.QuestComplete);
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public enum Sfx { Tap, Click, Coin, Hit, QuestComplete, LevelUp, Crit, Victory, Whoosh }

        private static SoundManager _instance;
        public static SoundManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[SoundManager]");
                    _instance = go.AddComponent<SoundManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private readonly Dictionary<Sfx, AudioClip> _clips = new();
        private AudioSource[] _pool;
        private int _poolCursor = 0;
        private const int POOL_SIZE = 6;

        [Range(0f, 1f)] public float masterVolume = 0.7f;
        public bool muted = false;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;

            // Build procedural clips once
            _clips[Sfx.Tap]            = ProceduralSFX.Tap();
            _clips[Sfx.Click]          = ProceduralSFX.Click();
            _clips[Sfx.Coin]           = ProceduralSFX.Coin();
            _clips[Sfx.Hit]            = ProceduralSFX.Hit();
            _clips[Sfx.QuestComplete]  = ProceduralSFX.QuestComplete();
            _clips[Sfx.LevelUp]        = ProceduralSFX.LevelUp();
            _clips[Sfx.Crit]           = ProceduralSFX.CriticalHit();
            _clips[Sfx.Victory]        = ProceduralSFX.Victory();
            _clips[Sfx.Whoosh]         = ProceduralSFX.Whoosh();

            // Pool of AudioSources so overlapping sfx don't cut each other off
            _pool = new AudioSource[POOL_SIZE];
            for (int i = 0; i < POOL_SIZE; i++)
            {
                _pool[i] = gameObject.AddComponent<AudioSource>();
                _pool[i].playOnAwake = false;
                _pool[i].spatialBlend = 0f; // 2D
            }

            // Persist master volume / mute across sessions
            masterVolume = PlayerPrefs.GetFloat("sparq_sfx_volume", 0.7f);
            muted        = PlayerPrefs.GetInt("sparq_sfx_muted", 0) == 1;
        }

        public static void Play(Sfx sfx, float pitch = 1f, float volumeScale = 1f)
        {
            Instance.PlayInternal(sfx, pitch, volumeScale);
        }

        private void PlayInternal(Sfx sfx, float pitch, float volumeScale)
        {
            if (muted) return;
            if (!_clips.TryGetValue(sfx, out var clip) || clip == null) return;

            var src = _pool[_poolCursor];
            _poolCursor = (_poolCursor + 1) % POOL_SIZE;

            src.clip = clip;
            src.pitch = pitch;
            src.volume = masterVolume * Mathf.Clamp01(volumeScale);
            src.Play();
        }

        public void SetVolume(float v)
        {
            masterVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat("sparq_sfx_volume", masterVolume);
        }

        public void SetMuted(bool m)
        {
            muted = m;
            PlayerPrefs.SetInt("sparq_sfx_muted", m ? 1 : 0);
        }
    }
}
