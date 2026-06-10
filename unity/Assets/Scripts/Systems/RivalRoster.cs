namespace Sparq.Systems
{
    /// <summary>
    /// 20+ rival monsters across 5 tiers.
    /// Tier 1 (mini)  : animated slime / wisp (lvl 1-5)
    /// Tier 2 (fodder): static monsters from the 60-pack (lvl 6-15)
    /// Tier 3 (elite) : animated Fantasy Monster Pack #2-4 (lvl 16-25)
    /// Tier 4 (boss)  : animated Fantasy Monster Pack #5 + Dark Knight (lvl 26+)
    /// Cycles forever after the last entry.
    /// </summary>
    public static class RivalRoster
    {
        public struct Rival
        {
            public string folderName;   // animated pack folder ("monster1", "monster2", etc.) OR ""
            public string animSubfolder; // "idel" | "attack" etc.
            public string staticSpritePath; // Assets/... .png (used when folderName is empty)
            public string name;
            public string title;
            public int    minLevel;
            public int    baseHpXP;
            public string tier;   // "mini" | "fodder" | "elite" | "boss"
        }

        private const string STATIC_ROOT = "Assets/2D Fantasy Monster Sprite Pack/Monsters/";

        public static readonly Rival[] ROSTER = new Rival[]
        {
            // Tier 1 — starter minis (animated)
            new Rival { folderName="monster1", animSubfolder="idel", staticSpritePath="",
                        name="Slym",     title="Goo Trickster",     minLevel=1,  baseHpXP=60,  tier="mini" },
            new Rival { folderName="",         animSubfolder="",      staticSpritePath=STATIC_ROOT+"Droplet/Droplet.png",
                        name="Plip",     title="Water Sprite",      minLevel=2,  baseHpXP=80,  tier="mini" },
            new Rival { folderName="",         animSubfolder="",      staticSpritePath=STATIC_ROOT+"Wisp/Wisp.png",
                        name="Will",     title="Wandering Wisp",    minLevel=3,  baseHpXP=100, tier="mini" },

            // Tier 2 — fodder (static)
            new Rival { folderName="",         animSubfolder="",      staticSpritePath=STATIC_ROOT+"Chick/Chick.png",
                        name="Pecky",    title="Tiny Terror",       minLevel=4,  baseHpXP=130, tier="fodder" },
            new Rival { folderName="",         animSubfolder="",      staticSpritePath=STATIC_ROOT+"Mouse/Mouse.png",
                        name="Nibbz",    title="Dungeon Rat",       minLevel=5,  baseHpXP=160, tier="fodder" },
            new Rival { folderName="",         animSubfolder="",      staticSpritePath=STATIC_ROOT+"Bat/Bat.png",
                        name="Batrix",   title="Shadow Bat",        minLevel=6,  baseHpXP=200, tier="fodder" },
            new Rival { folderName="",         animSubfolder="",      staticSpritePath=STATIC_ROOT+"Spider/Spider.png",
                        name="Webble",   title="Cave Spider",       minLevel=7,  baseHpXP=250, tier="fodder" },
            new Rival { folderName="",         animSubfolder="",      staticSpritePath=STATIC_ROOT+"Pumpkin/Pumpkin.png",
                        name="Gordo",    title="Jack o' Gorger",    minLevel=8,  baseHpXP=300, tier="fodder" },
            new Rival { folderName="",         animSubfolder="",      staticSpritePath=STATIC_ROOT+"Hellhound/Darkness-Hellhound.png",
                        name="Volt",     title="Shadow Hellhound",  minLevel=9,  baseHpXP=360, tier="fodder" },

            // Tier 3 — elite (animated)
            new Rival { folderName="monster2", animSubfolder="idel", staticSpritePath="",
                        name="Vex",      title="Whisper Wraith",    minLevel=10, baseHpXP=450, tier="elite" },
            new Rival { folderName="",         animSubfolder="",      staticSpritePath=STATIC_ROOT+"Skeleton/Skeleton.png",
                        name="Rattle",   title="Bone Knight",       minLevel=12, baseHpXP=550, tier="elite" },
            new Rival { folderName="",         animSubfolder="",      staticSpritePath=STATIC_ROOT+"Ghoul/Ghoul.png",
                        name="Ashen",    title="Crypt Ghoul",       minLevel=14, baseHpXP=650, tier="elite" },
            new Rival { folderName="monster3", animSubfolder="idel", staticSpritePath="",
                        name="Ember",    title="Magma Beast",       minLevel=15, baseHpXP=780, tier="elite" },
            new Rival { folderName="",         animSubfolder="",      staticSpritePath=STATIC_ROOT+"Reaper/Reaper.png",
                        name="Morta",    title="Soul Reaper",       minLevel=17, baseHpXP=920, tier="elite" },

            // Tier 4 — bosses (animated + Dark Knight)
            new Rival { folderName="monster4", animSubfolder="idel", staticSpritePath="",
                        name="Thorne",   title="Forest Tyrant",     minLevel=18, baseHpXP=1100, tier="boss" },
            new Rival { folderName="",         animSubfolder="",      staticSpritePath=STATIC_ROOT+"Cyclops/Cyclops.png",
                        name="Gorr",     title="One-Eyed Warlord",  minLevel=20, baseHpXP=1300, tier="boss" },
            new Rival { folderName="",         animSubfolder="",      staticSpritePath=STATIC_ROOT+"Dragon/Dragon.png",
                        name="Pyros",    title="Ember Dragon",      minLevel=22, baseHpXP=1600, tier="boss" },
            new Rival { folderName="monster5", animSubfolder="idel", staticSpritePath="",
                        name="Skarn",    title="Abyss Warlord",     minLevel=25, baseHpXP=2000, tier="boss" },
            new Rival { folderName="",         animSubfolder="",      staticSpritePath=STATIC_ROOT+"The-Overseer/The-Overseer.png",
                        name="Oraxis",   title="The Overseer",      minLevel=28, baseHpXP=2500, tier="boss" },

            // Tier 5 — final challenge
            new Rival { folderName="",         animSubfolder="",      staticSpritePath="Assets/Dark Knight/Sprites/DarkKnight_Idle_0.png",
                        name="Morthal",  title="The Dark Knight",   minLevel=32, baseHpXP=3500, tier="boss" },
        };

        public static Rival GetRivalForLevel(int level)
        {
            Rival pick = ROSTER[0];
            foreach (var r in ROSTER)
            {
                if (level >= r.minLevel) pick = r;
            }
            return pick;
        }

        public static int IndexOf(Rival r)
        {
            for (int i = 0; i < ROSTER.Length; i++)
            {
                if (ROSTER[i].name == r.name) return i;
            }
            return 0;
        }
    }
}
