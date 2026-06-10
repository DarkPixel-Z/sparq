using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// "Today's Trial" floating card. Shows one rotating daily challenge
    /// pulled from a small static deck. Tap BEGIN to accept the trial.
    /// </summary>
    public class DailyTrialCard : MonoBehaviour
    {
        public enum TrialKind { Combat, Body, Mind }

        [System.Serializable]
        public struct Trial
        {
            public string title;
            public string subtitle;
            public string glyph;     // single ASCII char
            public TrialKind kind;
            public int xpReward;
        }

        // Curated rotation — 7 trials, one per day-of-week
        public static readonly Trial[] Deck = new[]
        {
            new Trial { title = "Forest Patrol",         subtitle = "Slay 3 forest goblins",        glyph = "S", kind = TrialKind.Combat, xpReward = 30 },
            new Trial { title = "Quiet Mind",            subtitle = "Practice one wisdom card",     glyph = "M", kind = TrialKind.Mind,   xpReward = 15 },
            new Trial { title = "Walk the Path",         subtitle = "Take a 10-minute walk",        glyph = "W", kind = TrialKind.Body,   xpReward = 20 },
            new Trial { title = "Hunt the Shadow Wolf",  subtitle = "Defeat the Lv.3 boss",         glyph = "B", kind = TrialKind.Combat, xpReward = 50 },
            new Trial { title = "Loosen the Armor",      subtitle = "Stretch for 5 minutes",        glyph = "L", kind = TrialKind.Body,   xpReward = 15 },
            new Trial { title = "Read the Tomes",        subtitle = "Read for 20 minutes",          glyph = "R", kind = TrialKind.Mind,   xpReward = 25 },
            new Trial { title = "Breath of Steel",       subtitle = "Box breathing — 3 cycles",     glyph = "B", kind = TrialKind.Mind,   xpReward = 10 },
        };

        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private TMP_Text glyph;
        [SerializeField] private TMP_Text reward;
        [SerializeField] private Image    glyphBg;
        [SerializeField] private Button   beginBtn;

        private Trial _current;

        private void Start()
        {
            _current = Deck[DateTime.UtcNow.DayOfYear % Deck.Length];
            Refresh();
            if (beginBtn != null) beginBtn.onClick.AddListener(OnBegin);
        }

        private void Refresh()
        {
            if (title    != null) title.text    = _current.title;
            if (subtitle != null) subtitle.text = _current.subtitle;
            if (glyph    != null) glyph.text    = _current.glyph;
            if (reward   != null) reward.text   = $"+{_current.xpReward} XP";
            if (glyphBg  != null) glyphBg.color = TintForKind(_current.kind);
        }

        private static Color TintForKind(TrialKind k) => k switch
        {
            TrialKind.Combat => new Color(0.85f, 0.40f, 0.45f),
            TrialKind.Body   => new Color(0.55f, 0.85f, 0.45f),
            TrialKind.Mind   => new Color(0.55f, 0.75f, 1f),
            _                => Color.white,
        };

        private void OnBegin()
        {
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}

            // Combat → Battle scene
            // Mind / Body → toast for now (could route to Forge later)
            if (_current.kind == TrialKind.Combat)
            {
                Sparq.Systems.BattleScene.Start(_current.title);
            }
            else
            {
                try
                {
                    var canvas = GetComponentInParent<Canvas>();
                    if (canvas != null)
                        XPFloater.Spawn(canvas.transform,
                            transform.position + new Vector3(0, 60, 0),
                            $"Trial accepted: {_current.title}",
                            new Color(1f, 0.85f, 0.4f));
                } catch {}
            }
        }
    }
}
