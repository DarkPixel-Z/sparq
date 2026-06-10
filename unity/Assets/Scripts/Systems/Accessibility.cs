// Accessibility.cs — central accessibility flags.
//
// Sparq's audience skews neurodivergent, so motion/flash sensitivity matters.
// "Reduce Motion" (persisted in PlayerData.reducedMotion, toggled in Settings)
// suppresses screen shakes, full-screen flashes, banner swooshes, ambient
// particles, and scale "punch" effects across the game. Effects check this
// flag and either skip or fall back to a calm, static presentation.

namespace Sparq.Systems
{
    public static class Accessibility
    {
        /// <summary>True when the player has opted into calmer, low-motion visuals.</summary>
        public static bool ReducedMotion
        {
            get
            {
                try { return Sparq.Core.SaveService.Data != null && Sparq.Core.SaveService.Data.reducedMotion; }
                catch { return false; }
            }
            set
            {
                try
                {
                    var d = Sparq.Core.SaveService.Data;
                    if (d != null && d.reducedMotion != value)
                    {
                        d.reducedMotion = value;
                        Sparq.Core.SaveService.ScheduleSave();
                    }
                }
                catch {}
            }
        }
    }
}
