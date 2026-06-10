using UnityEngine;

namespace Sparq.Audio
{
    /// <summary>
    /// Generates chiptune-style AudioClips procedurally at runtime.
    /// No audio files required — everything is math.
    /// Produces short retro sfx for taps, XP gains, level ups, etc.
    /// </summary>
    public static class ProceduralSFX
    {
        private const int SAMPLE_RATE = 44100;

        public enum Wave { Sine, Square, Triangle, Saw, Noise }

        /// <summary>
        /// Build a clip from a sequence of notes.
        /// Each note: (frequencyHz, durationSec, volume 0..1, wave).
        /// Applies a tiny attack + exponential decay to each note so it sounds punchy.
        /// </summary>
        public static AudioClip BuildClip(string name, (float freq, float dur, float vol, Wave wave)[] notes)
        {
            float totalDur = 0f;
            foreach (var n in notes) totalDur += n.dur;
            int totalSamples = Mathf.CeilToInt(totalDur * SAMPLE_RATE);
            var samples = new float[totalSamples];

            int cursor = 0;
            foreach (var n in notes)
            {
                int noteSamples = Mathf.CeilToInt(n.dur * SAMPLE_RATE);
                float phase = 0f;
                float phaseInc = n.freq / SAMPLE_RATE;
                for (int i = 0; i < noteSamples && cursor + i < totalSamples; i++)
                {
                    float t = (float)i / noteSamples;                 // 0..1 through note
                    // 8ms attack, then exponential decay
                    float attack = Mathf.Clamp01(t / 0.05f);
                    float decay  = Mathf.Exp(-3f * t);
                    float env    = attack * decay;

                    float s = 0f;
                    switch (n.wave)
                    {
                        case Wave.Sine:     s = Mathf.Sin(phase * 2f * Mathf.PI); break;
                        case Wave.Square:   s = (phase % 1f) < 0.5f ? 1f : -1f; break;
                        case Wave.Triangle: s = 4f * Mathf.Abs((phase % 1f) - 0.5f) - 1f; break;
                        case Wave.Saw:      s = 2f * (phase % 1f) - 1f; break;
                        case Wave.Noise:    s = Random.Range(-1f, 1f); break;
                    }

                    samples[cursor + i] = s * env * n.vol;
                    phase += phaseInc;
                }
                cursor += noteSamples;
            }

            var clip = AudioClip.Create(name, totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // ---- Note frequencies (equal temperament, A4 = 440) ----
        public static float Note(string n)
        {
            switch (n)
            {
                case "C4":  return 261.63f;
                case "D4":  return 293.66f;
                case "E4":  return 329.63f;
                case "F4":  return 349.23f;
                case "G4":  return 392.00f;
                case "A4":  return 440.00f;
                case "B4":  return 493.88f;
                case "C5":  return 523.25f;
                case "D5":  return 587.33f;
                case "E5":  return 659.25f;
                case "F5":  return 698.46f;
                case "G5":  return 783.99f;
                case "A5":  return 880.00f;
                case "B5":  return 987.77f;
                case "C6":  return 1046.50f;
                case "E6":  return 1318.51f;
                case "G6":  return 1567.98f;
                default: return 440f;
            }
        }

        // ---- Preset builders ----

        public static AudioClip QuestComplete()
        {
            // Happy 3-note rising arpeggio: C5-E5-G5
            return BuildClip("SFX_QuestComplete", new (float, float, float, Wave)[]
            {
                (Note("C5"), 0.08f, 0.5f, Wave.Square),
                (Note("E5"), 0.08f, 0.5f, Wave.Square),
                (Note("G5"), 0.14f, 0.55f, Wave.Square),
            });
        }

        public static AudioClip LevelUp()
        {
            // Fanfare: C5-E5-G5-C6-E6 with a finisher
            return BuildClip("SFX_LevelUp", new (float, float, float, Wave)[]
            {
                (Note("C5"), 0.09f, 0.5f, Wave.Square),
                (Note("E5"), 0.09f, 0.5f, Wave.Square),
                (Note("G5"), 0.09f, 0.55f, Wave.Square),
                (Note("C6"), 0.12f, 0.6f, Wave.Square),
                (Note("E6"), 0.22f, 0.65f, Wave.Triangle),
            });
        }

        public static AudioClip Tap()
        {
            return BuildClip("SFX_Tap", new (float, float, float, Wave)[]
            {
                (Note("A5"), 0.05f, 0.35f, Wave.Triangle),
            });
        }

        public static AudioClip Coin()
        {
            // Two-note "ping" like a coin pickup
            return BuildClip("SFX_Coin", new (float, float, float, Wave)[]
            {
                (Note("E5"), 0.04f, 0.4f, Wave.Square),
                (Note("B5"), 0.09f, 0.45f, Wave.Square),
            });
        }

        public static AudioClip Hit()
        {
            // Deep meaty impact: low thump + high crackle + resonant tail
            return BuildClip("SFX_Hit", new (float, float, float, Wave)[]
            {
                (80f,  0.02f, 0.9f, Wave.Saw),    // low kick
                (300f, 0.04f, 0.8f, Wave.Noise),  // impact crackle
                (150f, 0.08f, 0.5f, Wave.Square), // tone body
                (220f, 0.12f, 0.3f, Wave.Triangle), // ring tail
            });
        }

        public static AudioClip CriticalHit()
        {
            // Bigger, higher punch — for big/critical hits
            return BuildClip("SFX_Crit", new (float, float, float, Wave)[]
            {
                (60f,  0.03f, 1.0f, Wave.Saw),
                (400f, 0.06f, 0.85f, Wave.Noise),
                (Note("A4"), 0.1f, 0.6f, Wave.Square),
                (Note("E5"), 0.18f, 0.5f, Wave.Triangle),
            });
        }

        public static AudioClip Victory()
        {
            // Rising triumphant arpeggio C-E-G-C5-E5-C6
            return BuildClip("SFX_Victory", new (float, float, float, Wave)[]
            {
                (Note("C4"), 0.08f, 0.6f, Wave.Square),
                (Note("E4"), 0.08f, 0.6f, Wave.Square),
                (Note("G4"), 0.08f, 0.7f, Wave.Square),
                (Note("C5"), 0.09f, 0.75f, Wave.Square),
                (Note("E5"), 0.09f, 0.8f, Wave.Triangle),
                (Note("G5"), 0.10f, 0.85f, Wave.Triangle),
                (Note("C6"), 0.35f, 0.9f, Wave.Triangle),
            });
        }

        public static AudioClip Whoosh()
        {
            // Filtered noise sweep — for lunge / slash
            return BuildClip("SFX_Whoosh", new (float, float, float, Wave)[]
            {
                (500f, 0.08f, 0.5f, Wave.Noise),
                (200f, 0.05f, 0.3f, Wave.Noise),
            });
        }

        public static AudioClip Click()
        {
            return BuildClip("SFX_Click", new (float, float, float, Wave)[]
            {
                (Note("E5"), 0.035f, 0.3f, Wave.Triangle),
            });
        }
    }
}
