// ─── Achievements ─────────────────────────────────────────────────────────────
const ACHIEVEMENTS = [
  { id: 'first_quest',   icon: '🌟', name: 'First Step',    desc: 'Complete your first quest',      xp: 20  },
  { id: 'streak_3',      icon: '🔥', name: 'On Fire',       desc: 'Reach a 3-day streak',           xp: 30  },
  { id: 'streak_7',      icon: '💥', name: 'Unstoppable',   desc: 'Reach a 7-day streak',           xp: 75  },
  { id: 'journal_first', icon: '📓', name: 'Inner Voice',   desc: 'Write your first journal entry', xp: 25  },
  { id: 'beat_fitch',    icon: '⚔️', name: 'Volt Slayer',  desc: 'Surpass Volt in total XP',      xp: 50  },
  { id: 'level_5',       icon: '⭐', name: 'Rising Star',   desc: 'Reach Level 5',                  xp: 40  },
  { id: 'level_10',      icon: '🌙', name: 'Night Bloom',   desc: 'Reach Level 10',                 xp: 100 },

  // Task milestones
  { id: 'tasks_10',   icon: '✅', name: 'Task Machine',    desc: '10 total tasks done',    xp: 50  },
  { id: 'tasks_50',   icon: '🎯', name: 'Quest Crusher',   desc: '50 total tasks done',    xp: 150 },
  { id: 'tasks_100',  icon: '💪', name: 'Century Club',    desc: '100 total tasks done',   xp: 300 },
  { id: 'tasks_500',  icon: '🏛️', name: 'Legendary Worker', desc: '500 total tasks done',  xp: 1000 },

  // Streak milestones
  { id: 'streak_14',  icon: '🌋', name: 'Volcano',         desc: 'Reach a 14-day streak',   xp: 150 },
  { id: 'streak_30',  icon: '👑', name: 'Streak Royalty',   desc: 'Reach a 30-day streak',   xp: 400 },
  { id: 'streak_100', icon: '🌌', name: 'Galactic',        desc: 'Reach a 100-day streak',  xp: 1500 },

  // Level milestones
  { id: 'level_15',   icon: '🌺', name: 'Legendary Karu',  desc: 'Reach Level 15 (Karu goes LEGENDARY)', xp: 200 },
  { id: 'level_20',   icon: '🦄', name: 'Ascendant',       desc: 'Reach Level 20',  xp: 300 },
  { id: 'level_30',   icon: '👼', name: 'Transcendent',    desc: 'Reach Level 30',  xp: 500 },
  { id: 'level_50',   icon: '🧙', name: 'Sparq Sage',      desc: 'Reach Level 50',  xp: 1000 },

  // Journal
  { id: 'journal_5',  icon: '📖', name: 'Storyteller',     desc: 'Write 5 journal entries',  xp: 40  },
  { id: 'journal_30', icon: '📚', name: 'Library',         desc: 'Write 30 journal entries', xp: 200 },
  { id: 'journal_100',icon: '🖋️', name: 'Voluminous',      desc: 'Write 100 journal entries', xp: 500 },

  // Rival
  { id: 'first_tap_battle', icon: '🥊', name: 'Throwdown', desc: 'Win your first tap battle', xp: 150 },
  { id: 'beat_volt_3',  icon: '⚔️', name: 'Rivalry Rising', desc: 'Beat Volt 3 times', xp: 100 },
  { id: 'beat_volt_10', icon: '🗡️', name: 'Volt\'s Nightmare', desc: 'Beat Volt 10 times', xp: 500 },

  // Enemy-specific
  { id: 'beat_shade',  icon: '🐈‍⬛', name: 'Shadow Chaser',   desc: 'Defeat Shade',   xp: 200 },
  { id: 'beat_pyra',   icon: '🦊', name: 'Fox Hunter',       desc: 'Defeat Pyra',    xp: 400 },
  { id: 'beat_barry',  icon: '💨', name: 'Unclogger',         desc: 'Defeat Barry the Fart Gobbler (sweet relief)', xp: 500 },
  { id: 'beat_frost',  icon: '🐦‍⬛', name: 'Ice Breaker',      desc: 'Defeat Frost',   xp: 600 },
  { id: 'beat_keira',  icon: '☀️', name: 'Eclipse',           desc: 'Defeat Keira the Solar Wraith', xp: 750 },
  { id: 'beat_drift',  icon: '🐍', name: 'Wind Chaser',       desc: 'Defeat Drift',   xp: 900 },
  { id: 'beat_valya',  icon: '🩰', name: 'Shadow Step',       desc: 'Defeat Valya the Void Dancer', xp: 1200 },
  { id: 'beat_rook',   icon: '🗿', name: 'Stone Crusher',     desc: 'Defeat Rook',    xp: 1500 },
  { id: 'beat_ember',  icon: '🔥', name: 'Phoenix Down',      desc: 'Defeat Ember',   xp: 2500 },
  { id: 'beat_ally',   icon: '🧝', name: 'Grove Keeper',      desc: 'Defeat Ally the Archwood Elven Mage', xp: 3000 },
  { id: 'beat_nyx',    icon: '🐉', name: 'Dragon Slayer',     desc: 'Defeat Nyx — TRUE LEGEND', xp: 5000 },
  { id: 'all_enemies', icon: '👑', name: 'Undisputed',        desc: 'Defeat ALL 12 enemies', xp: 10000 },

  // Pet
  { id: 'pet_taps_50',  icon: '🐾', name: 'Karu\'s BFF',   desc: 'Tap Karu 50 times',  xp: 30  },
  { id: 'pet_taps_500', icon: '🌸', name: 'Karu\'s Soulmate', desc: 'Tap Karu 500 times', xp: 200 },

  // Store
  { id: 'first_purchase', icon: '🛍️', name: 'Shopper',      desc: 'Buy your first store item', xp: 50 },
  { id: 'fully_dressed',  icon: '👗', name: 'Fashionista',   desc: 'Equip a hat AND accessory', xp: 75 },

  // Reminders
  { id: 'first_reminder', icon: '🔔', name: 'Scheduled',    desc: 'Create your first custom reminder', xp: 20 },

  // Adventures
  { id: 'first_adventure', icon: '🗺️', name: 'Explorer',    desc: 'Complete your first adventure', xp: 100 },
  { id: 'all_adventures',  icon: '🏅', name: 'Adventurer Elite', desc: 'Complete all 5 adventures', xp: 500 },

  // Focus Dungeon
  { id: 'fd_first',  icon: '🏰', name: 'Dungeon Diver',  desc: 'Complete your first Focus Dungeon', xp: 100 },
  { id: 'fd_10',     icon: '⚔️', name: 'Focus Master',   desc: 'Complete 10 Focus Dungeons',        xp: 500 },
  { id: 'fd_50',     icon: '👑', name: 'Dungeon King',   desc: 'Complete 50 Focus Dungeons',        xp: 2000 },

  // Easter egg achievements (hidden — desc says "???")
  { id: 'egg_konami',    icon: '🎮', name: 'Retro Gamer',   desc: '???', xp: 150, hidden: true },
  { id: 'egg_date_tap',  icon: '📅', name: 'Time Bandit',   desc: '???', xp: 75, hidden: true },
  { id: 'egg_karu_100',  icon: '💖', name: 'Karu\'s Chosen One', desc: '???', xp: 200, hidden: true },
  { id: 'egg_sparq_word',icon: '⚡', name: 'The Magic Word', desc: '???', xp: 100, hidden: true },
  { id: 'egg_night_owl', icon: '🦉', name: 'Night Owl',    desc: '???', xp: 50, hidden: true },
];

// ─── Enemies / Rivals ────────────────────────────────────────────────────────
// Defeat enemies in order. Each one is stronger. Volt is always first (tutorial rival).
const ENEMIES = [
  {
    id:    'volt',
    name:  'Volt',
    title: 'Electric Wolf',
    sprite: 'assets/fitch.svg',   // existing electric-blue wolf
    fallbackEmoji: '⚡',
    levelReq:  1,
    startXP:   72,
    catchRate: 0.4,               // gains 40% of player XP on each quest
    color:     '#4A9EFF',         // electric blue
    flavor:    'Your starter rival. Fast, cocky, underestimates you.',
    rewardCoins: 100,
  },
  {
    id:    'shade',
    name:  'Shade',
    title: 'Shadow Cat',
    sprite: null,
    fallbackEmoji: '🐈‍⬛',
    levelReq:  5,
    startXP:   500,
    catchRate: 0.5,
    color:     '#7F55E6',
    flavor:    "Sneaky. Strikes when you're not looking. Weak to routine.",
    rewardCoins: 250,
  },
  {
    id:    'pyra',
    name:  'Pyra',
    title: 'Flame Fox',
    sprite: null,
    fallbackEmoji: '🦊',
    levelReq:  10,
    startXP:   1200,
    catchRate: 0.55,
    color:     '#FF6A00',
    flavor:    'Aggressive and fiery. Burns bright but burns out.',
    rewardCoins: 500,
  },
  {
    id:    'barry',
    name:  'Barry',
    title: 'The Fart Gobbler',
    sprite: null,
    fallbackEmoji: '💨',
    levelReq:  12,
    startXP:   1800,
    catchRate: 0.55,
    color:     '#9ACD32',  // sickly yellow-green
    flavor:    "Smells terrible, fights worse. But don't let your guard down — he burps XP.",
    rewardCoins: 666,
  },
  {
    id:    'frost',
    name:  'Frost',
    title: 'Ice Raven',
    sprite: null,
    fallbackEmoji: '🐦‍⬛',
    levelReq:  15,
    startXP:   2500,
    catchRate: 0.6,
    color:     '#7FE8FF',
    flavor:    'Cold and calculating. Breaks your focus with whispers.',
    rewardCoins: 800,
  },
  {
    id:    'keira',
    name:  'Keira',
    title: 'Solar Wraith',
    sprite: null,
    fallbackEmoji: '☀️',
    levelReq:  18,
    startXP:   3500,
    catchRate: 0.62,
    color:     '#FFB800',
    flavor:    "Born from the heart of a dying star. Blinds you with her own glory.",
    rewardCoins: 1000,
  },
  {
    id:    'drift',
    name:  'Drift',
    title: 'Wind Serpent',
    sprite: null,
    fallbackEmoji: '🐍',
    levelReq:  20,
    startXP:   4500,
    catchRate: 0.65,
    color:     '#00D4AA',
    flavor:    "Slippery and fast. Never in the same place twice.",
    rewardCoins: 1200,
  },
  {
    id:    'valya',
    name:  'Valya',
    title: 'Void Dancer',
    sprite: null,
    fallbackEmoji: '🩰',
    levelReq:  23,
    startXP:   6000,
    catchRate: 0.68,
    color:     '#7F3FDA',
    flavor:    "Moves between worlds. You feel her before you see her.",
    rewardCoins: 1500,
  },
  {
    id:    'rook',
    name:  'Rook',
    title: 'Stone Golem',
    sprite: null,
    fallbackEmoji: '🗿',
    levelReq:  25,
    startXP:   8000,
    catchRate: 0.7,
    color:     '#8B8B8B',
    flavor:    'Slow, enormous, grinds you down with sheer persistence.',
    rewardCoins: 1800,
  },
  {
    id:    'ember',
    name:  'Ember',
    title: 'Lava Phoenix',
    sprite: null,
    fallbackEmoji: '🔥',
    levelReq:  30,
    startXP:   14000,
    catchRate: 0.8,
    color:     '#FF4444',
    flavor:    "Rises from defeat. Beat them once, they come back harder.",
    rewardCoins: 2500,
  },
  {
    id:    'ally',
    name:  'Ally',
    title: 'Archwood Elven Mage',
    sprite: null,
    fallbackEmoji: '🧝',
    levelReq:  35,
    startXP:   20000,
    catchRate: 0.9,
    color:     '#3D8F5E',
    flavor:    "Thousand-year elf of the Archwood. Weaves nature magic older than your bloodline.",
    rewardCoins: 3500,
  },
  {
    id:    'nyx',
    name:  'Nyx',
    title: 'Night Dragon',
    sprite: null,
    fallbackEmoji: '🐉',
    levelReq:  45,
    startXP:   40000,
    catchRate: 1.0,
    color:     '#A87EFF',
    flavor:    'The final boss. Godlike. Legend says nobody has beaten her.',
    rewardCoins: 5000,
  },
];

function getCurrentEnemy() {
  const idx = state.currentEnemyIndex || 0;
  return ENEMIES[Math.min(idx, ENEMIES.length - 1)];
}

function getEnemyById(id) {
  return ENEMIES.find(e => e.id === id);
}

// ─── Pets ────────────────────────────────────────────────────────────────────
const PETS = {
  karu:  { id: 'karu',  defaultName: 'Karu',  sprite: 'assets/red-panda.svg', species: 'Red Panda',  desc: 'Fluffy, energetic, loves bamboo' },
  mochi: { id: 'mochi', defaultName: 'Mochi', sprite: 'assets/mochi.svg',     species: 'Axolotl',    desc: 'Mellow, curious, always smiling' },
};

function getActivePet() {
  return PETS[state.activePet] || PETS.karu;
}

// ─── Store items ─────────────────────────────────────────────────────────────
const STORE_ITEMS = [
  // ── HATS (13) ───────────────────────────────────────────────────
  { id: 'hat_bamboo_crown', slot: 'hat', name: 'Bamboo Crown', icon: '🎋', cost: 100,  levelReq: 1,  desc: 'A natural leafy crown' },
  { id: 'hat_party',        slot: 'hat', name: 'Party Hat',    icon: '🎉', cost: 150,  levelReq: 2,  desc: "Someone's having a good day" },
  { id: 'hat_wizard',       slot: 'hat', name: 'Wizard Hat',   icon: '🧙', cost: 250,  levelReq: 3,  desc: 'For when Karu studies magic' },
  { id: 'hat_beanie',       slot: 'hat', name: 'Cozy Beanie',  icon: '🧢', cost: 180,  levelReq: 2,  desc: 'Snug on chilly mornings' },
  { id: 'hat_tophat',       slot: 'hat', name: 'Top Hat',      icon: '🎩', cost: 300,  levelReq: 4,  desc: 'Dapper Karu!' },
  { id: 'hat_straw',        slot: 'hat', name: 'Straw Hat',    icon: '👒', cost: 220,  levelReq: 3,  desc: 'Summer beach vibes' },
  { id: 'hat_graduation',   slot: 'hat', name: 'Grad Cap',     icon: '🎓', cost: 400,  levelReq: 5,  desc: 'A scholar, a warrior' },
  { id: 'hat_antlers',      slot: 'hat', name: 'Reindeer Antlers', icon: '🦌', cost: 350, levelReq: 5, desc: 'Ready for the holidays' },
  { id: 'hat_halo',         slot: 'hat', name: 'Angel Halo',   icon: '😇', cost: 600,  levelReq: 7,  desc: 'Pure & radiant' },
  { id: 'hat_devil',        slot: 'hat', name: 'Devil Horns',  icon: '😈', cost: 600,  levelReq: 7,  desc: 'Mischief mode on' },
  { id: 'hat_headphones',   slot: 'hat', name: 'Headphones',   icon: '🎧', cost: 450,  levelReq: 6,  desc: 'Lo-fi Karu beats' },
  { id: 'hat_ninja',        slot: 'hat', name: 'Ninja Hood',   icon: '🥷', cost: 700,  levelReq: 8,  desc: 'Silent but deadly cute' },
  { id: 'hat_crown',        slot: 'hat', name: 'Golden Crown', icon: '👑', cost: 1000, levelReq: 10, desc: 'Royalty status: unlocked' },

  // ── ACCESSORIES (11) ─────────────────────────────────────────────
  { id: 'acc_scarf',    slot: 'accessory', name: 'Cozy Scarf',    icon: '🧣', cost: 80,  levelReq: 1, desc: 'Warm & stylish' },
  { id: 'acc_glasses',  slot: 'accessory', name: 'Nerd Glasses',  icon: '🤓', cost: 120, levelReq: 2, desc: 'Galaxy-brain energy' },
  { id: 'acc_sunglasses',slot:'accessory', name: 'Sunglasses',    icon: '😎', cost: 150, levelReq: 2, desc: 'Too cool for school' },
  { id: 'acc_flower',   slot: 'accessory', name: 'Flower Crown',  icon: '🌸', cost: 200, levelReq: 3, desc: 'Soft & whimsical' },
  { id: 'acc_bowtie',   slot: 'accessory', name: 'Bow Tie',       icon: '🎀', cost: 180, levelReq: 3, desc: 'Fancy occasion ready' },
  { id: 'acc_necklace', slot: 'accessory', name: 'Gem Necklace',  icon: '💎', cost: 380, levelReq: 5, desc: 'Sparkle + shimmer' },
  { id: 'acc_cape',     slot: 'accessory', name: 'Hero Cape',     icon: '🦸', cost: 400, levelReq: 5, desc: 'Look like the hero you are' },
  { id: 'acc_backpack', slot: 'accessory', name: 'Adventure Pack',icon: '🎒', cost: 500, levelReq: 6, desc: 'Ready for anything' },
  { id: 'acc_wings',    slot: 'accessory', name: 'Fairy Wings',   icon: '🧚', cost: 750, levelReq: 8, desc: 'Tiny but magical' },
  { id: 'acc_medal',    slot: 'accessory', name: 'Gold Medal',    icon: '🥇', cost: 650, levelReq: 8, desc: 'You earned it' },
  { id: 'acc_crystal',  slot: 'accessory', name: 'Energy Crystal',icon: '🔮', cost: 900, levelReq: 10, desc: 'Pulsing with spark' },

  // ── BACKGROUNDS (10) ────────────────────────────────────────────
  { id: 'bg_sakura',    slot: 'background', name: 'Sakura Grove',   icon: '🌸', cost: 300,  levelReq: 4,  desc: 'Cherry blossom paradise' },
  { id: 'bg_beach',     slot: 'background', name: 'Sunset Beach',   icon: '🏖️', cost: 500,  levelReq: 6,  desc: 'Chill & golden' },
  { id: 'bg_forest',    slot: 'background', name: 'Enchanted Forest',icon: '🌲', cost: 400, levelReq: 5, desc: 'Mossy & mysterious' },
  { id: 'bg_galaxy',    slot: 'background', name: 'Galaxy',         icon: '🌌', cost: 800,  levelReq: 8,  desc: 'Cosmic vibes' },
  { id: 'bg_underwater',slot: 'background', name: 'Coral Reef',     icon: '🐠', cost: 650,  levelReq: 7,  desc: 'Deep sea serenity' },
  { id: 'bg_mountain',  slot: 'background', name: 'Snowy Peaks',    icon: '🏔️', cost: 550,  levelReq: 6,  desc: 'Crisp alpine air' },
  { id: 'bg_neon',      slot: 'background', name: 'Neon City',      icon: '🌃', cost: 1200, levelReq: 12, desc: 'Cyberpunk Karu' },
  { id: 'bg_candy',     slot: 'background', name: 'Candy Land',     icon: '🍭', cost: 700,  levelReq: 8,  desc: 'Sweet & colorful' },
  { id: 'bg_volcano',   slot: 'background', name: 'Volcano Lair',   icon: '🌋', cost: 1500, levelReq: 14, desc: "Karu's hot side" },
  { id: 'bg_rainbow',   slot: 'background', name: 'Rainbow Realm',  icon: '🌈', cost: 2000, levelReq: 18, desc: 'Ultimate chromatic flex' },
];

// ─── Food items (restore hunger, small XP bonuses) ──────────────────────────
const FOOD_ITEMS = [
  { id: 'food_apple',     name: 'Red Apple',     icon: '🍎', cost: 15, hunger: 10, xp: 2,  desc: 'A crunchy snack' },
  { id: 'food_berry',     name: 'Berry Bowl',    icon: '🍓', cost: 25, hunger: 15, xp: 3,  desc: 'Sweet & juicy' },
  { id: 'food_bamboo',    name: 'Bamboo Shoot',  icon: '🎋', cost: 35, hunger: 25, xp: 5,  desc: "Karu's favorite! +5 XP bonus" },
  { id: 'food_honey',     name: 'Honey Jar',     icon: '🍯', cost: 45, hunger: 30, xp: 6,  desc: 'Liquid gold energy' },
  { id: 'food_ricecake',  name: 'Rice Cake',     icon: '🍙', cost: 60, hunger: 40, xp: 8,  desc: 'Filling & simple' },
  { id: 'food_sushi',     name: 'Sushi Platter', icon: '🍣', cost: 90, hunger: 50, xp: 10, desc: 'Restaurant-quality lunch' },
  { id: 'food_watermelon',name: 'Watermelon',    icon: '🍉', cost: 70, hunger: 55, xp: 9,  desc: 'Max refreshment' },
  { id: 'food_cake',      name: 'Birthday Cake', icon: '🎂', cost: 150, hunger: 100, xp: 30, desc: 'Full belly + celebration!' },
];

// ─── Potions (temporary boosts + shields) ───────────────────────────────────
const POTION_ITEMS = [
  { id: 'pot_spark',    name: 'Spark Potion',  icon: '⚡', cost: 50,  effect: 'xp',       value: 30,  desc: '+30 XP instantly' },
  { id: 'pot_mega',     name: 'Mega Spark',    icon: '💥', cost: 150, effect: 'xp',       value: 100, desc: '+100 XP instantly' },
  { id: 'pot_super',    name: 'Super Spark',   icon: '🌟', cost: 400, effect: 'xp',       value: 300, desc: '+300 XP instantly' },
  { id: 'pot_coins',    name: 'Coin Pouch',    icon: '💰', cost: 100, effect: 'coins',    value: 120, desc: 'Unlock a small pile: +120 🪙' },
  { id: 'pot_shield',   name: 'Streak Shield', icon: '🛡️', cost: 200, effect: 'shield',   value: 1,   desc: 'Protects streak if you miss a day' },
  { id: 'pot_freeze',   name: 'Volt Freeze',   icon: '❄️', cost: 250, effect: 'freeze',   value: 24,  desc: 'Volt gains no XP for 24h' },
  { id: 'pot_mystery',  name: 'Mystery Box',   icon: '🎁', cost: 80,  effect: 'mystery',  value: 0,   desc: 'Random surprise!' },
];

// ─── Pet Treats (special interaction boosters) ──────────────────────────────
const TREAT_ITEMS = [
  { id: 'treat_fish',     name: 'Dried Fish',     icon: '🐟', cost: 30,  effect: 'pet_xp',    value: 5,  desc: 'A savory snack. +5 XP per pet tap for 10 taps' },
  { id: 'treat_milk',     name: 'Warm Milk',      icon: '🥛', cost: 40,  effect: 'hunger',    value: 20, desc: 'Restores 20 hunger + makes Karu cuddly' },
  { id: 'treat_cookie',   name: 'Paw Cookie',     icon: '🍪', cost: 55,  effect: 'pet_xp',    value: 10, desc: 'Karu\'s favorite. +10 XP per tap for 5 taps' },
  { id: 'treat_strawberry',name:'Strawberry',     icon: '🍓', cost: 35,  effect: 'hunger',    value: 15, desc: 'Sweet and juicy' },
  { id: 'treat_bone',     name: 'Chew Bone',      icon: '🦴', cost: 80,  effect: 'coin_boost',value: 30, desc: 'Next 5 quests give +30% coins' },
  { id: 'treat_catnip',   name: 'Catnip Toy',     icon: '🌿', cost: 120, effect: 'mood',      value: 1,  desc: 'Karu goes into zoomies mode — double XP for 30 min' },
];

// ─── Keys / Special Items (gated / rare) ────────────────────────────────────
const KEY_ITEMS = [
  { id: 'key_bronze',  name: 'Bronze Key',   icon: '🗝️', cost: 100,  rarity: 'common',    desc: 'Opens a bronze mystery crate (small reward)' },
  { id: 'key_silver',  name: 'Silver Key',   icon: '🔑', cost: 300,  rarity: 'uncommon',  desc: 'Opens a silver mystery crate (medium reward)' },
  { id: 'key_gold',    name: 'Gold Key',     icon: '🔐', cost: 800,  rarity: 'rare',      desc: 'Opens a gold mystery crate (big reward)' },
  { id: 'key_rainbow', name: 'Rainbow Key',  icon: '🌈', cost: 2000, rarity: 'legendary', desc: 'Opens a legendary crate (random pet outfit)' },
];

// ─── Hunger system ──────────────────────────────────────────────────────────
const HUNGER_DECAY_PER_HOUR = 5;

function tickHunger() {
  const now = Date.now();
  if (!state.lastHungerTick) state.lastHungerTick = now;
  const hoursElapsed = (now - state.lastHungerTick) / (1000 * 60 * 60);
  const decay = Math.floor(hoursElapsed * HUNGER_DECAY_PER_HOUR);
  if (decay > 0) {
    state.hunger = Math.max(0, (state.hunger || 100) - decay);
    state.lastHungerTick = now;
    saveState();
  }
}

function getHungerLabel() {
  const h = state.hunger || 0;
  if (h >= 80) return { label: 'Full', color: '#00D4AA', emoji: '😋' };
  if (h >= 50) return { label: 'OK',   color: '#FFE000', emoji: '🙂' };
  if (h >= 25) return { label: 'Hungry', color: '#FF9500', emoji: '😕' };
  return                 { label: 'Starving', color: '#FF4444', emoji: '😩' };
}

// ─── Adventures ──────────────────────────────────────────────────────────────
const ADVENTURES = [
  {
    id: 'morning_routine',
    title: 'Build Your Morning Routine',
    icon: '☀️',
    description: 'Master your mornings in 5 steps',
    reward: 300,
    steps: [
      'Wake up before 9 AM (tap to mark)',
      'Drink a glass of water',
      'Stretch for 2 minutes',
      'Eat something healthy',
      'Start one focused task',
    ]
  },
  {
    id: 'beat_procrastination',
    title: 'Beat Procrastination Week',
    icon: '⚡',
    description: 'Tackle 7 tough tasks',
    reward: 500,
    steps: [
      'Complete 1 task before noon',
      'Finish a task you\'ve been avoiding',
      'Do something for just 2 minutes',
      'Break a big task into 3 smaller ones',
      'Start a task within 10 seconds of thinking of it',
      'Complete a task without checking your phone',
      'Celebrate a win — big or small',
    ]
  },
  {
    id: 'journal_journey',
    title: 'The Journal Journey',
    icon: '📔',
    description: 'Unlock your inner writer',
    reward: 200,
    steps: [
      'Write your first entry',
      'Write for 3 days in a row',
      'Try 3 different moods',
      'Write a 100+ character entry',
      'Complete 5 total entries',
    ]
  },
  {
    id: 'volt_training',
    title: 'Volt Training Arc',
    icon: '⚔️',
    description: 'Crush the electric wolf',
    reward: 400,
    steps: [
      'Surpass Volt for the first time',
      'Stay ahead of Volt for 24 hours',
      'Widen the XP gap to 100+',
      'Beat Volt a second time',
      'Beat Volt a third time — Volt crumbles',
    ]
  },
  {
    id: 'streak_master',
    title: 'Streak Master',
    icon: '🔥',
    description: 'Build an unbreakable habit',
    reward: 600,
    steps: [
      'Reach a 3-day streak',
      'Reach a 7-day streak',
      'Reach a 14-day streak',
      'Reach a 21-day streak',
      'Hit the legendary 30-day mark',
    ]
  },
];

// ─── State ────────────────────────────────────────────────────────────────────
const DEFAULT_STATE = {
  currentXP:       0,
  totalXP:         0,
  level:           1,
  xpToNextLevel:   100,
  streak:          0,
  longestStreak:   0,
  lastActiveDate:  '',
  completedToday:  0,
  fitchXP:         72,
  petTaps:         0,
  journalMood:     '😄',
  unlockedAchievements: [],
  totalTasksDone:  0,
  journalCount:    0,
  customTasks:     [],
  customReminders: [],
  ndProfile:       'ADHD',
  sparqCoins:          0,      // New currency for store
  purchases:           [],     // Array of purchased item IDs
  equippedItems:       {},     // { hat: 'bamboo_crown', accessory: 'scarf_red', background: 'sakura' }
  voltBeats:           0,      // Count of times beaten Volt (when beat_fitch first unlocks, also increment this)
  currentEnemyIndex:   0,      // Index into ENEMIES array
  defeatedEnemies:     [],     // Array of enemy IDs defeated
  completedAdventures: [],     // Array of adventure IDs completed
  adventureProgress:   {},     // { adventureId: stepIndex }
  eggs:                [],     // Array of easter-egg IDs found
  onboardingComplete:  false,
  hunger:          100,          // 0-100, decreases over time
  lastHungerTick:  0,            // timestamp (ms) of last decay
  foodInventory:   { food_apple: 2, food_bamboo: 1 },
  potionInventory: { pot_spark: 3 },
  treatInventory:  { treat_fish: 2 },
  keyInventory:    { key_bronze: 1 },
  streakShields:   0,            // count of unused streak shields
  voltFreezeUntil: 0,            // timestamp — Volt can't gain XP until this
  tapBattleCooldownUntil: 0,     // timestamp — tap battle unavailable until this
  soundEnabled:    true,         // master sound toggle
  reducedMotion:   false,        // global reduce-animations toggle
  activePet:       'karu',       // 'karu' | 'mochi'
  petName:         'Karu',       // user can rename
  pronouns:        'they/them',  // 'he/him' | 'she/her' | 'they/them' | etc.
  focusSessions:      0,         // total completed Focus Dungeon sessions
  focusMinutesTotal:  0,         // lifetime focus minutes
  focusStreakBest:    0,         // best session completion streak
  lastFocusFail:      0,         // timestamp of last fail (for daily guard)
};

const state = { ...DEFAULT_STATE };

// ─── Persistence ──────────────────────────────────────────────────────────────
function saveState() {
  try { localStorage.setItem('sparq_v2', JSON.stringify(state)); } catch (_) {}
  if (typeof scheduleSync === 'function') scheduleSync();
}

function loadState() {
  try {
    const saved = localStorage.getItem('sparq_v2');
    if (saved) Object.assign(state, JSON.parse(saved));
  } catch (_) {}

  // Migrate existing saves
  if (state.currentEnemyIndex === undefined) state.currentEnemyIndex = 0;
  if (!Array.isArray(state.defeatedEnemies)) state.defeatedEnemies = [];
  // If user has already surpassed Volt but defeatedEnemies is empty, backfill
  if (state.totalXP > (state.fitchXP || 0) && state.defeatedEnemies.length === 0 && state.currentEnemyIndex === 0) {
    state.defeatedEnemies.push('volt');
    state.voltBeats = Math.max(1, state.voltBeats || 0);
    state.currentEnemyIndex = 1;
    state.fitchXP = ENEMIES[1].startXP;
  }
}

// ─── Streak (date-aware) ──────────────────────────────────────────────────────
function updateStreak() {
  const today     = new Date().toDateString();
  const yesterday = new Date(Date.now() - 86_400_000).toDateString();

  if (state.lastActiveDate === today) return; // already counted

  if (state.lastActiveDate === yesterday) {
    state.streak++;
  } else if (state.lastActiveDate && (state.streakShields || 0) > 0) {
    // Use a shield to preserve streak
    state.streakShields--;
    showPopup(`🛡️ Streak shield used! ${state.streakShields} left.`);
    state.streak++;  // treat as continuous
  } else {
    state.streak = 1; // reset or first day
  }

  state.lastActiveDate = today;
  if (state.streak > state.longestStreak) state.longestStreak = state.streak;
  saveState();
}

// ─── XP thresholds ───────────────────────────────────────────────────────────
function xpForLevel(lvl) { return lvl * 100; }

// ─── Award XP + handle level-up ───────────────────────────────────────────────
function awardXP(amount) {
  state.currentXP += amount;
  state.totalXP   += amount;

  // Earn Sparq Coins (1 per 10 XP)
  state.sparqCoins = (state.sparqCoins || 0) + Math.floor(amount / 10);

  // Volt slowly gains XP too (adds tension) — unless frozen
  if (Date.now() > (state.voltFreezeUntil || 0)) {
    const enemy = getCurrentEnemy();
    state.fitchXP += Math.floor(amount * (enemy?.catchRate || 0.4));
  }

  // Show "damage" number on rival card (D mechanic)
  if (typeof spawnEnemyDamage === 'function') {
    const damage = amount - Math.floor(amount * (getCurrentEnemy()?.catchRate || 0.4));
    // "damage" is how much the gap closed (your gain minus their gain)
    spawnEnemyDamage(damage);
  }

  while (state.currentXP >= state.xpToNextLevel) {
    state.currentXP    -= state.xpToNextLevel;
    state.level        += 1;
    state.xpToNextLevel = xpForLevel(state.level);
    showLevelUpModal(state.level);
  }

  checkEnemyDefeat();
  checkAchievements();
  updateUI();
  saveState();
}

// ─── Enemy defeat detection ───────────────────────────────────────────────────
function checkEnemyDefeat() {
  const enemy = getCurrentEnemy();
  if (!enemy) return;
  if (!state.defeatedEnemies) state.defeatedEnemies = [];
  // Already defeated this one — no-op
  if (state.defeatedEnemies.includes(enemy.id)) return;
  if (state.totalXP > state.fitchXP) {
    // Victory!
    state.defeatedEnemies.push(enemy.id);
    state.sparqCoins = (state.sparqCoins || 0) + (enemy.rewardCoins || 0);
    if (enemy.id === 'volt') state.voltBeats = (state.voltBeats || 0) + 1;
    // Advance to next enemy (if exists)
    const nextIdx = (state.currentEnemyIndex || 0) + 1;
    if (nextIdx < ENEMIES.length) {
      state.currentEnemyIndex = nextIdx;
      state.fitchXP = ENEMIES[nextIdx].startXP;
    } else {
      // All defeated — max index stays, fitchXP can grow further
      state.currentEnemyIndex = ENEMIES.length - 1;
    }
    showEnemyVictoryModal(enemy, ENEMIES[Math.min(nextIdx, ENEMIES.length - 1)]);
    saveState();
  }
}

function showEnemyVictoryModal(defeatedEnemy, nextEnemy) {
  if (typeof playSound === 'function') playSound('victory');
  document.getElementById('victoryEnemyIcon').textContent = defeatedEnemy.fallbackEmoji;
  document.getElementById('victoryEnemyName').textContent = defeatedEnemy.name;
  document.getElementById('victoryReward').textContent = `+${defeatedEnemy.rewardCoins} 🪙`;
  const nextText = document.getElementById('victoryNextText');
  if (nextEnemy && nextEnemy.id !== defeatedEnemy.id) {
    nextText.innerHTML = `A new challenger approaches:<br><strong>${nextEnemy.fallbackEmoji} ${nextEnemy.name}</strong> the ${nextEnemy.title}`;
  } else {
    nextText.textContent = "🏆 You've defeated ALL enemies! Legendary status.";
  }
  const modal = document.getElementById('enemyVictoryModal');
  modal.style.opacity = '1';
  modal.style.pointerEvents = 'all';
  modal.classList.add('show');
  launchConfetti();
  launchConfetti();
}

function closeVictoryModal() {
  if (typeof playSound === 'function') playSound('close');
  const modal = document.getElementById('enemyVictoryModal');
  modal.style.opacity = '0';
  modal.style.pointerEvents = 'none';
  modal.classList.remove('show');
  updateRivalUI();
  updateUI();
}

// ─── Level-up modal ───────────────────────────────────────────────────────────
function showLevelUpModal(lvl) {
  if (typeof playSound === 'function') playSound('levelup');
  const modal  = document.getElementById('levelUpModal');
  const lvlEl  = document.getElementById('levelUpNum');
  const titleEl = document.getElementById('levelUpTitle');
  const evoEl  = document.getElementById('evolutionMsg');

  lvlEl.textContent = `Level ${lvl}`;

  const titles = ['Novice','Apprentice','Adept','Achiever','Champion',
                  'Hero','Legend','Mythic','Radiant','Ascendant'];
  titleEl.textContent = titles[Math.min(lvl - 1, titles.length - 1)] + ' ✦';

  // Evolution milestones
  if (lvl === 5)       evoEl.textContent = '✨ Karu evolved! Teen form unlocked!';
  else if (lvl === 10) evoEl.textContent = '🌟 Karu fully evolved! Adult form!';
  else if (lvl === 15) evoEl.textContent = '🌙 Karu is LEGENDARY now!';
  else                 evoEl.textContent = '';

  modal.style.opacity = '1';
  modal.style.pointerEvents = 'all';
  modal.classList.add('show');
  launchConfetti();
  updatePetEvolution();
}

function closeLevelUp() {
  if (typeof playSound === 'function') playSound('close');
  const m = document.getElementById('levelUpModal');
  m.style.opacity = '0';
  m.style.pointerEvents = 'none';
  m.classList.remove('show');
}

// ─── Pet evolution visual ─────────────────────────────────────────────────────
function updatePetEvolution() {
  const avatar = document.querySelector('.pet-avatar');
  avatar.classList.remove('evo-teen', 'evo-adult', 'evo-legendary');
  if      (state.level >= 15) avatar.classList.add('evo-legendary');
  else if (state.level >= 10) avatar.classList.add('evo-adult');
  else if (state.level >= 5)  avatar.classList.add('evo-teen');
}

// ─── Achievements ─────────────────────────────────────────────────────────────
function checkAchievements() {
  const unlock = (id) => {
    if (state.unlockedAchievements.includes(id)) return;
    const ach = ACHIEVEMENTS.find(a => a.id === id);
    if (!ach) return;
    state.unlockedAchievements.push(id);
    showAchievementToast(ach);
    state.currentXP += ach.xp;
    state.totalXP   += ach.xp;
    // First time beating Volt — bump voltBeats counter
    if (id === 'beat_fitch') {
      state.voltBeats = (state.voltBeats || 0) + 1;
    }
  };

  if (state.totalTasksDone >= 1)  unlock('first_quest');
  if (state.streak >= 3)          unlock('streak_3');
  if (state.streak >= 7)          unlock('streak_7');
  if (state.journalCount >= 1)    unlock('journal_first');
  if (state.totalXP > state.fitchXP) unlock('beat_fitch');
  if (state.level >= 5)           unlock('level_5');
  if (state.level >= 10)          unlock('level_10');

  // Task milestones
  if (state.totalTasksDone >= 10)  unlock('tasks_10');
  if (state.totalTasksDone >= 50)  unlock('tasks_50');
  if (state.totalTasksDone >= 100) unlock('tasks_100');
  if (state.totalTasksDone >= 500) unlock('tasks_500');
  // Streak milestones
  if (state.streak >= 14)  unlock('streak_14');
  if (state.streak >= 30)  unlock('streak_30');
  if (state.streak >= 100) unlock('streak_100');
  // Level milestones
  if (state.level >= 15)   unlock('level_15');
  if (state.level >= 20)   unlock('level_20');
  if (state.level >= 30)   unlock('level_30');
  if (state.level >= 50)   unlock('level_50');
  // Journal milestones
  if (state.journalCount >= 5)   unlock('journal_5');
  if (state.journalCount >= 30)  unlock('journal_30');
  if (state.journalCount >= 100) unlock('journal_100');
  // Rival
  if ((state.voltBeats || 0) >= 3)  unlock('beat_volt_3');
  if ((state.voltBeats || 0) >= 10) unlock('beat_volt_10');
  // Pet
  if (state.petTaps >= 50)  unlock('pet_taps_50');
  if (state.petTaps >= 500) unlock('pet_taps_500');
  // Store
  if ((state.purchases || []).length >= 1) unlock('first_purchase');
  const equipped = state.equippedItems || {};
  if (equipped.hat && equipped.accessory) unlock('fully_dressed');
  // Reminders
  if ((state.customReminders || []).length >= 1) unlock('first_reminder');
  // Adventures
  if ((state.completedAdventures || []).length >= 1) unlock('first_adventure');
  if ((state.completedAdventures || []).length >= 5) unlock('all_adventures');
  // Focus Dungeon
  if ((state.focusSessions || 0) >= 1)  unlock('fd_first');
  if ((state.focusSessions || 0) >= 10) unlock('fd_10');
  if ((state.focusSessions || 0) >= 50) unlock('fd_50');

  // Enemy defeats
  const defeated = state.defeatedEnemies || [];
  if (defeated.includes('shade'))  unlock('beat_shade');
  if (defeated.includes('pyra'))   unlock('beat_pyra');
  if (defeated.includes('barry'))  unlock('beat_barry');
  if (defeated.includes('frost'))  unlock('beat_frost');
  if (defeated.includes('keira'))  unlock('beat_keira');
  if (defeated.includes('drift'))  unlock('beat_drift');
  if (defeated.includes('valya'))  unlock('beat_valya');
  if (defeated.includes('rook'))   unlock('beat_rook');
  if (defeated.includes('ember'))  unlock('beat_ember');
  if (defeated.includes('ally'))   unlock('beat_ally');
  if (defeated.includes('nyx'))    unlock('beat_nyx');
  if (defeated.length >= ENEMIES.length) unlock('all_enemies');
}

function showAchievementToast(ach) {
  if (typeof playSound === 'function') playSound('achievement');
  const toast = document.getElementById('achievementToast');
  document.getElementById('achIcon').textContent  = ach.icon;
  document.getElementById('achName').textContent  = ach.name;
  document.getElementById('achDesc').textContent  = ach.desc;
  document.getElementById('achXP').textContent    = `+${ach.xp} XP`;
  toast.style.right = '16px';
  toast.classList.add('show');
  setTimeout(() => { toast.style.right = '-320px'; toast.classList.remove('show'); }, 3500);
}

// ─── Performance detection ───────────────────────────────────────────────────
// Reduce particle counts on native (Capacitor) builds — WebView + emulator is slow.
const IS_NATIVE = !!(window.Capacitor && window.Capacitor.isNativePlatform && window.Capacitor.isNativePlatform());
const STAR_COUNT    = 55;
const BLOSSOM_EVERY = 3500; // ms

// ─── Stars ────────────────────────────────────────────────────────────────────
function initStars() {
  const container = document.getElementById('stars');
  for (let i = 0; i < STAR_COUNT; i++) {
    const s    = document.createElement('div');
    s.className = 'star';
    const size = Math.random() * 2.5 + 1;
    s.style.cssText = `width:${size}px;height:${size}px;top:${Math.random()*100}%;left:${Math.random()*100}%;--dur:${2+Math.random()*3}s;--delay:${-Math.random()*4}s`;
    container.appendChild(s);
  }
}

// ─── Cherry blossoms ──────────────────────────────────────────────────────────
function spawnBlossom() {
  const petals = ['🌸','🌺','✿','❀','🌼'];
  const b      = document.createElement('div');
  b.className  = 'blossom';
  const drift  = (Math.random() * 120 - 60) + 'px';
  b.style.cssText = `left:${Math.random()*100}%;top:-30px;--dur:${6+Math.random()*6}s;--delay:0s;--drift:${drift};`;
  b.textContent = petals[Math.floor(Math.random() * petals.length)];
  document.body.appendChild(b);
  setTimeout(() => b.remove(), 13_000);
}

function initBlossoms() {
  spawnBlossom();
  setInterval(spawnBlossom, BLOSSOM_EVERY);
}

// ─── Date pill ────────────────────────────────────────────────────────────────
function initDate() {
  const days   = ['SUNDAY','MONDAY','TUESDAY','WEDNESDAY','THURSDAY','FRIDAY','SATURDAY'];
  const months = ['JANUARY','FEBRUARY','MARCH','APRIL','MAY','JUNE','JULY','AUGUST','SEPTEMBER','OCTOBER','NOVEMBER','DECEMBER'];
  const now    = new Date();
  const text   = `● ${days[now.getDay()]} · ${months[now.getMonth()]} ${now.getDate()}`;
  document.getElementById('datePill').textContent    = text;
  document.getElementById('journalDatePill').textContent = text;
}

// ─── Navigation ───────────────────────────────────────────────────────────────
function switchPage(page) {
  document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
  document.querySelectorAll('.nav-btn').forEach(b => b.classList.remove('active'));
  document.getElementById('page-' + page).classList.add('active');
  const navBtn = document.getElementById('nav-' + page);
  if (navBtn) navBtn.classList.add('active');
  document.getElementById('fab').className = (page === 'home') ? 'fab' : 'fab fab-hidden';
  if (page === 'community') showCommunityPage();
  if (page === 'store') renderStore();
  if (page === 'adventures') renderAdventures();
  if (page === 'rogues') renderRogues();
}

function renderRogues() {
  const list = document.getElementById('roguesList');
  if (!list) return;
  const defeated = new Set(state.defeatedEnemies || []);
  const currentId = getCurrentEnemy()?.id;
  list.innerHTML = ENEMIES.map((e, idx) => {
    const isDefeated = defeated.has(e.id);
    const isCurrent = e.id === currentId && !isDefeated;
    const isLocked = state.level < e.levelReq && !isDefeated && !isCurrent;
    const statusClass = isDefeated ? 'rogue-defeated' : isCurrent ? 'rogue-current' : isLocked ? 'rogue-locked' : 'rogue-pending';
    const statusLabel = isDefeated ? '✓ Defeated' : isCurrent ? '🎯 Current Rival' : isLocked ? `🔒 Lv.${e.levelReq}` : 'Pending';
    const displayIcon = isLocked ? '❓' : e.fallbackEmoji;
    const displayName = isLocked ? '???' : e.name;
    const displayTitle = isLocked ? 'Unknown threat' : e.title;
    const displayFlavor = isLocked ? 'Level up to reveal.' : e.flavor;
    return `
      <div class="rogue-card ${statusClass}" style="--enemy-color:${e.color}">
        <div class="rogue-icon">${displayIcon}</div>
        <div class="rogue-body">
          <div class="rogue-name">${escapeHtml(displayName)} <span class="rogue-title">${escapeHtml(displayTitle)}</span></div>
          <div class="rogue-flavor">${escapeHtml(displayFlavor)}</div>
          <div class="rogue-meta">
            <span class="rogue-status">${statusLabel}</span>
            ${!isLocked ? `<span class="rogue-reward">💰 ${e.rewardCoins} coins</span>` : ''}
          </div>
        </div>
      </div>
    `;
  }).join('');
}

// ─── XP popup ─────────────────────────────────────────────────────────────────
function showPopup(message, duration = 1200) {
  const popup = document.getElementById('xpPopup');
  popup.textContent = message;
  popup.classList.add('show');
  setTimeout(() => popup.classList.remove('show'), duration);
}

// ─── Floating damage numbers (MapleStory style) ───────────────────────────────
function spawnDamageNum(amount, color = '#FFE000') {
  const el = document.createElement('div');
  el.className = 'damage-num';
  el.textContent = `+${amount} XP`;
  el.style.cssText = `
    left: ${30 + Math.random()*40}%;
    top: ${35 + Math.random()*20}%;
    color: ${color};
  `;
  document.body.appendChild(el);
  setTimeout(() => el.remove(), 1300);
}

// ─── Confetti ─────────────────────────────────────────────────────────────────
function launchConfetti(count = 14) {
  const colors = ['#FF2D2D','#FF7849','#FFD700','#00D4AA','#FF9BC5','#A87EFF'];
  for (let i = 0; i < count; i++) {
    const c = document.createElement('div');
    c.className = 'confetti-piece';
    c.style.cssText = `left:${35+Math.random()*30}%;top:38%;background:${colors[Math.floor(Math.random()*colors.length)]};transform:rotate(${Math.random()*360}deg);animation-delay:${Math.random()*0.3}s;`;
    document.body.appendChild(c);
    setTimeout(() => c.remove(), 1600);
  }
}

// ─── Update all UI ────────────────────────────────────────────────────────────
function updateUI() {
  tickHunger();
  const hb = document.getElementById('hungerBar');
  const hl = document.getElementById('hungerLabel');
  if (hb && hl) {
    const h = state.hunger || 0;
    const info = getHungerLabel();
    hb.style.width = h + '%';
    hb.style.background = `linear-gradient(90deg, ${info.color}, ${info.color}CC)`;
    hl.textContent = `${info.emoji} ${info.label}`;
  }
  const pct = (state.currentXP / state.xpToNextLevel) * 100;
  document.getElementById('xpBar').style.width = pct + '%';
  document.getElementById('currentXP').textContent   = state.currentXP;
  document.getElementById('xpNextLevel').textContent = state.xpToNextLevel;
  document.getElementById('totalXP').textContent     = state.totalXP;
  document.getElementById('profileXP').textContent   = state.totalXP;
  document.getElementById('streakCount').textContent = state.streak;
  document.getElementById('streakDays').textContent  = state.streak;
  document.getElementById('petLevelBadge').textContent = `✦ Lv.${state.level}`;
  document.getElementById('profileLevel').textContent = state.level;
  document.getElementById('profileStreak').textContent = state.streak;
  document.getElementById('profileDone').textContent  = state.totalTasksDone;

  const remaining = document.querySelectorAll('.task-item:not(.done)').length;
  document.getElementById('taskCount').textContent = remaining;

  updateRivalUI();
  updatePetEvolution();
}

// ─── Rival / Fitch ────────────────────────────────────────────────────────────
function updateRivalUI() {
  const enemy = getCurrentEnemy();
  if (!enemy) return;
  const gap = state.fitchXP - state.totalXP;

  // Update XP displays
  const fitchXPEl = document.getElementById('fitchXP');
  const fitchXPProfileEl = document.getElementById('fitchXPProfile');
  if (fitchXPEl) fitchXPEl.textContent = state.fitchXP;
  if (fitchXPProfileEl) fitchXPProfileEl.textContent = state.fitchXP;

  // Update name/title/sprite on home rival card
  const rivalCard = document.getElementById('rivalCard');
  if (rivalCard) {
    const nameEl = rivalCard.querySelector('.rival-name');
    if (nameEl) nameEl.innerHTML = `${escapeHtml(enemy.name)} <span class="rival-title">${escapeHtml(enemy.title)}</span>`;
    const avatarEl = rivalCard.querySelector('.rival-avatar');
    if (avatarEl) {
      if (enemy.sprite) {
        avatarEl.innerHTML = `<img src="${enemy.sprite}" alt="${escapeHtml(enemy.name)}" width="52" height="52">`;
      } else {
        avatarEl.innerHTML = `<span class="rival-emoji" style="font-size:42px;filter:drop-shadow(0 0 10px ${enemy.color})">${enemy.fallbackEmoji}</span>`;
      }
    }
    const btn = rivalCard.querySelector('.rival-btn');
    if (btn) btn.innerHTML = `⚔️ Challenge ${escapeHtml(enemy.name)}`;
  }

  // Same for profile mini rival
  const miniCard = document.querySelector('.rival-mini-card');
  if (miniCard) {
    const nameEl = miniCard.querySelector('.rival-mini-name');
    if (nameEl) nameEl.innerHTML = `${escapeHtml(enemy.name)} <span>${escapeHtml(enemy.title)}</span>`;
    const imgEl = miniCard.querySelector('.rival-mini-img');
    if (imgEl && !enemy.sprite) {
      // Replace img with emoji span
      const span = document.createElement('span');
      span.className = 'rival-mini-img';
      span.style.cssText = 'font-size:36px;display:inline-block;width:44px;text-align:center;';
      span.textContent = enemy.fallbackEmoji;
      imgEl.replaceWith(span);
    } else if (imgEl && enemy.sprite) {
      if (imgEl.tagName === 'IMG') {
        imgEl.src = enemy.sprite;
        imgEl.alt = enemy.name;
      } else {
        // Was a span, replace with img
        const img = document.createElement('img');
        img.className = 'rival-mini-img';
        img.src = enemy.sprite;
        img.alt = enemy.name;
        img.width = 44;
        img.height = 44;
        imgEl.replaceWith(img);
      }
    }
  }

  const rivalGap   = document.getElementById('rivalGap');
  const rivalDelta = document.getElementById('rivalDelta');
  if (!rivalGap || !rivalDelta) return;

  if (gap > 0) {
    rivalGap.textContent   = `You're ${gap} XP behind ${enemy.name} — catch up! 😤`;
    rivalGap.style.color   = 'var(--cherry)';
    rivalDelta.textContent = `-${gap} XP`;
    rivalDelta.style.color = 'var(--maple-red)';
  } else if (gap < 0) {
    rivalGap.textContent   = `You're ${Math.abs(gap)} XP AHEAD of ${enemy.name}! 🔥`;
    rivalGap.style.color   = 'var(--teal)';
    rivalDelta.textContent = `+${Math.abs(gap)} XP`;
    rivalDelta.style.color = 'var(--teal)';
  } else {
    rivalGap.textContent   = `Dead even with ${enemy.name}! Push harder! ⚡`;
    rivalGap.style.color   = 'var(--yellow)';
    rivalDelta.textContent = 'TIED';
    rivalDelta.style.color = 'var(--yellow)';
  }

  const fill = document.getElementById('enemyHpFill');
  const txt  = document.getElementById('enemyHpText');
  if (fill && txt) {
    const startXP = enemy.startXP || 100;
    const gap = Math.max(0, state.fitchXP - state.totalXP);
    // HP percent: full when you first meet the enemy, 0 when you surpass
    const maxGap = Math.max(gap, startXP);
    const pct = Math.max(0, Math.min(100, (gap / maxGap) * 100));
    fill.style.width = pct + '%';
    txt.textContent = Math.round(pct) + '%';
    // Color shift — green → yellow → red as HP drops
    if (pct > 66)      fill.style.background = `linear-gradient(90deg, ${enemy.color}, #3EB89A)`;
    else if (pct > 33) fill.style.background = `linear-gradient(90deg, #FFB800, #FF6A00)`;
    else               fill.style.background = `linear-gradient(90deg, #FF4444, #C92D2D)`;
  }
}

function spawnEnemyDamage(amount) {
  if (amount <= 0) return;
  const card = document.getElementById('rivalCard');
  if (!card) return;
  const el = document.createElement('div');
  el.className = 'enemy-damage-float';
  el.textContent = `-${amount} HP`;
  const rect = card.getBoundingClientRect();
  el.style.left = (rect.left + rect.width / 2) + 'px';
  el.style.top  = (rect.top + 20) + 'px';
  document.body.appendChild(el);
  setTimeout(() => el.remove(), 1400);
}

// ─── Tap Battle ──────────────────────────────────────────────────────────────
let _tapBattle = null;

const TAP_BATTLE_COOLDOWN_MS = 60 * 60 * 1000; // 1 hour

function isTapBattleOnCooldown() {
  return (state.tapBattleCooldownUntil || 0) > Date.now();
}

function startTapBattle() {
  if (isTapBattleOnCooldown()) {
    const remainingMin = Math.ceil((state.tapBattleCooldownUntil - Date.now()) / 60000);
    showPopup(`⏱️ Battle on cooldown! ${remainingMin} min left.`);
    return;
  }
  const enemy = getCurrentEnemy();
  if (!enemy) return;
  const defeated = (state.defeatedEnemies || []).includes(enemy.id);
  if (defeated) {
    showPopup('This foe is already defeated!');
    return;
  }
  if (typeof playSound === 'function') playSound('challenge');

  // Scale enemy HP to player level — player should be able to win with ~60-80 fast taps
  const playerHP = 100;
  const enemyHP = 100;

  _tapBattle = {
    playerHP,  playerMaxHP: playerHP,
    enemyHP,   enemyMaxHP: enemyHP,
    timeLeft:  30,
    tapDamage: 3,  // per tap
    dodgeActive: false,
    dodgeFailTimeout: null,
    ended: false,
    enemyRef: enemy,
  };

  // Setup UI
  document.getElementById('tapBattleEnemyLabel').innerHTML = `${enemy.fallbackEmoji} ${escapeHtml(enemy.name)}`;
  document.getElementById('tapBattleTargetEmoji').textContent = enemy.fallbackEmoji;
  document.getElementById('tapBattlePlayerHp').style.width = '100%';
  document.getElementById('tapBattleEnemyHp').style.width = '100%';
  document.getElementById('tapBattleTimer').textContent = '30';
  document.getElementById('tapBattleDodge').style.display = 'none';

  // Show modal
  const modal = document.getElementById('tapBattleModal');
  modal.style.opacity = '1';
  modal.style.pointerEvents = 'all';
  modal.classList.add('show');

  // Start countdown
  _tapBattle.timerId = setInterval(() => {
    if (!_tapBattle || _tapBattle.ended) return;
    _tapBattle.timeLeft--;
    document.getElementById('tapBattleTimer').textContent = _tapBattle.timeLeft;
    if (_tapBattle.timeLeft <= 0) endTapBattle(false, 'Time\'s up!');
  }, 1000);

  // Schedule enemy attack windows
  scheduleEnemyAttack();
}

function scheduleEnemyAttack() {
  if (!_tapBattle || _tapBattle.ended) return;
  // Attack every 4-7 seconds
  const delay = 4000 + Math.random() * 3000;
  setTimeout(() => {
    if (!_tapBattle || _tapBattle.ended) return;
    triggerEnemyAttack();
  }, delay);
}

function triggerEnemyAttack() {
  if (!_tapBattle || _tapBattle.ended) return;
  _tapBattle.dodgeActive = true;
  document.getElementById('tapBattleDodge').style.display = 'block';
  if (typeof playSound === 'function') playSound('error');
  // Player has 1.5s to tap dodge
  _tapBattle.dodgeFailTimeout = setTimeout(() => {
    if (!_tapBattle || _tapBattle.ended || !_tapBattle.dodgeActive) return;
    // Missed dodge — take damage
    _tapBattle.dodgeActive = false;
    document.getElementById('tapBattleDodge').style.display = 'none';
    const dmg = 15 + Math.floor(Math.random() * 10);
    _tapBattle.playerHP = Math.max(0, _tapBattle.playerHP - dmg);
    const hpEl = document.getElementById('tapBattlePlayerHp');
    if (hpEl) hpEl.style.width = (_tapBattle.playerHP / _tapBattle.playerMaxHP * 100) + '%';
    if (typeof showPopup === 'function') showPopup(`💥 Hit! -${dmg} HP`);
    if (_tapBattle.playerHP <= 0) return endTapBattle(false, 'You were defeated!');
    scheduleEnemyAttack();
  }, 1500);
}

function tapBattleHit() {
  if (!_tapBattle || _tapBattle.ended) return;
  const dmg = _tapBattle.tapDamage;
  _tapBattle.enemyHP = Math.max(0, _tapBattle.enemyHP - dmg);
  const hpEl = document.getElementById('tapBattleEnemyHp');
  if (hpEl) hpEl.style.width = (_tapBattle.enemyHP / _tapBattle.enemyMaxHP * 100) + '%';
  // Flash the target
  const target = document.getElementById('tapBattleTarget');
  if (target) {
    target.classList.remove('tap-hit');
    void target.offsetWidth;
    target.classList.add('tap-hit');
  }
  if (typeof playSound === 'function') playSound('xp');
  if (_tapBattle.enemyHP <= 0) endTapBattle(true);
}

function tapBattleDodge() {
  if (!_tapBattle || !_tapBattle.dodgeActive) return;
  _tapBattle.dodgeActive = false;
  clearTimeout(_tapBattle.dodgeFailTimeout);
  document.getElementById('tapBattleDodge').style.display = 'none';
  if (typeof playSound === 'function') playSound('complete');
  showPopup('🛡️ Dodged!');
  // Counter-attack — free damage
  _tapBattle.enemyHP = Math.max(0, _tapBattle.enemyHP - 8);
  const hpEl = document.getElementById('tapBattleEnemyHp');
  if (hpEl) hpEl.style.width = (_tapBattle.enemyHP / _tapBattle.enemyMaxHP * 100) + '%';
  if (_tapBattle.enemyHP <= 0) return endTapBattle(true);
  scheduleEnemyAttack();
}

function tapBattleFlee() {
  if (!_tapBattle) return;
  endTapBattle(false, 'Fled from battle');
}

function endTapBattle(won, msg) {
  if (!_tapBattle || _tapBattle.ended) return;
  _tapBattle.ended = true;
  clearInterval(_tapBattle.timerId);
  clearTimeout(_tapBattle.dodgeFailTimeout);
  const modal = document.getElementById('tapBattleModal');
  modal.style.opacity = '0';
  modal.style.pointerEvents = 'none';
  modal.classList.remove('show');
  const enemy = _tapBattle.enemyRef;
  if (won) {
    // Victory — force defeat
    if (typeof playSound === 'function') playSound('victory');
    showPopup('🏆 VICTORY!');
    launchConfetti();
    // Advance total XP past enemy XP to trigger normal victory flow
    const overflow = state.fitchXP - state.totalXP + 1;
    if (overflow > 0) {
      state.totalXP += overflow;
      state.currentXP += overflow;
    }
    if (!state.unlockedAchievements.includes('first_tap_battle')) {
      state.unlockedAchievements.push('first_tap_battle');
      state.currentXP += 150;
      state.totalXP += 150;
      showAchievementToast(ACHIEVEMENTS.find(a => a.id === 'first_tap_battle'));
    }
    checkEnemyDefeat();
    updateUI();
  } else {
    // Loss — set cooldown
    if (typeof playSound === 'function') playSound('error');
    state.tapBattleCooldownUntil = Date.now() + TAP_BATTLE_COOLDOWN_MS;
    state.sparqCoins = (state.sparqCoins || 0) + 20; // consolation
    showPopup(`💀 ${msg || 'Defeated!'} +20 🪙 consolation. 1hr cooldown.`);
    saveState();
  }
  _tapBattle = null;
}

function challengeFitch() {
  startTapBattle();
}

// ─── Complete task ────────────────────────────────────────────────────────────
function completeTask(el, xpAmount) {
  if (el.classList.contains('done')) return;
  if (typeof playSound === 'function') playSound('complete');
  el.classList.add('done');
  el.querySelector('.task-check').textContent = '✓';
  el.querySelector('.task-xp').textContent    = '✓ Done';

  state.totalTasksDone++;
  state.completedToday++;
  if (state.completedToday === 1) updateStreak();

  awardXP(xpAmount);
  showPopup(`⚡ +${xpAmount} XP!`);
  spawnDamageNum(xpAmount);
  launchConfetti();

  const pet = document.querySelector('.pet-avatar');
  pet.style.transform = 'scale(1.25) rotate(8deg)';
  setTimeout(() => (pet.style.transform = ''), 400);
}

// ─── Pet tap ──────────────────────────────────────────────────────────────────
function petTap() {
  if (typeof playSound === 'function') playSound('pet');
  state.petTaps++;
  const messages = [
    'Karu loves you! 💕','So happy! 🌸','*happy red panda noises* 🐾',
    'More quests plz! ⚡','Best human ever! 🥰','Karu wants a nap... 😴',
    'You smell like XP! 🌟',
  ];
  showPopup(messages[state.petTaps % messages.length], 1000);
  spawnBlossom(); spawnBlossom();
  if (state.petTaps === 100) {
    triggerEgg('egg_karu_100', '💖 KARU\'S CHOSEN ONE — ultimate bond');
  }
  checkAchievements();
  saveState();
}

// ─── Streak tap ───────────────────────────────────────────────────────────────
function streakTap() { showPopup('🔥 Keep the flame alive!', 1000); }

// ─── Mood ─────────────────────────────────────────────────────────────────────
function selectMood(btn) {
  document.querySelectorAll('.mood-btn').forEach(b => b.classList.remove('selected'));
  btn.classList.add('selected');
  const moods = { awful:'Hang in there 💙', meh:"That's valid 🤍", ok:'You got this 🌿', good:'Love that! 🌸', great:"You're on FIRE! 🔥" };
  showPopup(moods[btn.dataset.mood]);
}

// ─── Toggle reminder ──────────────────────────────────────────────────────────
function toggleReminder(el) { el.classList.toggle('off'); }

// ─── Custom reminders ────────────────────────────────────────────────────────
const REMINDER_COLORS = ['#7C5CFC','#00D4AA','#FF7849','#FF5FA0','#FFD93D','#4D96FF','#FFADAD','#A0E7E5'];

function onReminderFreqChange() {
  const freqEl = document.querySelector('input[name="reminderFreq"]:checked');
  const freq = freqEl ? freqEl.value : 'daily';
  const domRow = document.getElementById('reminderDayOfMonthRow');
  const onceRow = document.getElementById('reminderOnceDateRow');
  const rangeRow = document.getElementById('reminderRangeRow');
  if (domRow)   domRow.style.display   = freq === 'monthly' ? 'block' : 'none';
  if (onceRow)  onceRow.style.display  = freq === 'once' ? 'block' : 'none';
  if (rangeRow) rangeRow.style.display = (freq === 'daily' || freq === 'weekdays' || freq === 'monthly') ? 'block' : 'none';
}

function showAddReminderDialog() {
  if (typeof playSound === 'function') playSound('open');
  document.getElementById('newReminderTitle').value = '';
  document.getElementById('newReminderTime').value = '';
  const dom = document.getElementById('reminderDayOfMonth'); if (dom) dom.value = '';
  const once = document.getElementById('reminderOnceDate');  if (once) once.value = '';
  const sd = document.getElementById('reminderStartDate');   if (sd) sd.value = '';
  const ed = document.getElementById('reminderEndDate');     if (ed) ed.value = '';
  const dailyRadio = document.querySelector('input[name="reminderFreq"][value="daily"]');
  if (dailyRadio) dailyRadio.checked = true;
  onReminderFreqChange();
  const modal = document.getElementById('addReminderModal');
  modal.style.opacity = '1';
  modal.style.pointerEvents = 'all';
  modal.classList.add('show');
}

function closeAddReminderDialog() {
  if (typeof playSound === 'function') playSound('close');
  const modal = document.getElementById('addReminderModal');
  modal.style.opacity = '0';
  modal.style.pointerEvents = 'none';
  modal.classList.remove('show');
}

function saveCustomReminder() {
  const title = document.getElementById('newReminderTitle').value.trim();
  const time  = document.getElementById('newReminderTime').value;
  const freqEl = document.querySelector('input[name="reminderFreq"]:checked');
  const freq  = freqEl ? freqEl.value : 'daily';
  if (!title) { showPopup('Give it a name ✍️'); return; }
  if (!time)  { showPopup('Pick a time ⏰');    return; }
  const reminder = {
    id: 'r' + Date.now(),
    title, time, freq,
    color: REMINDER_COLORS[(state.customReminders || []).length % REMINDER_COLORS.length],
    enabled: true,
  };
  if (freq === 'monthly') {
    const dom = parseInt(document.getElementById('reminderDayOfMonth').value, 10);
    if (!dom || dom < 1 || dom > 31) { showPopup('Pick day 1-31 for monthly'); return; }
    reminder.dayOfMonth = dom;
  }
  if (freq === 'once') {
    const d = document.getElementById('reminderOnceDate').value;
    if (!d) { showPopup('Pick a date for one-time reminder'); return; }
    reminder.date = d;
  }
  if (freq !== 'once') {
    const start = document.getElementById('reminderStartDate').value;
    const end   = document.getElementById('reminderEndDate').value;
    if (start) reminder.startDate = start;
    if (end)   reminder.endDate = end;
  }
  if (!state.customReminders) state.customReminders = [];
  state.customReminders.push(reminder);
  saveState();
  renderCustomReminders();
  closeAddReminderDialog();
  showPopup('🔔 Reminder saved!');
}

function deleteCustomReminder(id) {
  if (!confirm('Delete this reminder?')) return;
  state.customReminders = (state.customReminders || []).filter(r => r.id !== id);
  saveState();
  renderCustomReminders();
}

function toggleCustomReminder(id) {
  const r = (state.customReminders || []).find(x => x.id === id);
  if (!r) return;
  r.enabled = !r.enabled;
  saveState();
  renderCustomReminders();
}

function renderCustomReminders() {
  const list = document.getElementById('customReminderList');
  if (!list) return;
  const reminders = state.customReminders || [];
  if (!reminders.length) {
    list.innerHTML = '<div class="empty-hint">No custom reminders yet. Tap ➕ below to add one!</div>';
    return;
  }
  const freqLabels = { daily: 'Every day', weekdays: 'Weekdays', monthly: 'Monthly', once: 'One time' };
  list.innerHTML = reminders.map(r => {
    let subText = `⏰ ${r.time} · ${freqLabels[r.freq] || r.freq}`;
    if (r.freq === 'monthly' && r.dayOfMonth) subText += ` · day ${r.dayOfMonth}`;
    if (r.freq === 'once' && r.date) subText += ` · ${r.date}`;
    if (r.endDate) subText += ` · until ${r.endDate}`;
    return `
      <div class="reminder-item">
        <div class="reminder-color" style="background:${r.color}"></div>
        <div class="reminder-content">
          <div class="reminder-title">${escapeHtml(r.title)}</div>
          <div class="reminder-time">${subText}</div>
        </div>
        <button class="reminder-delete-btn" onclick="deleteCustomReminder('${r.id}')" aria-label="Delete">🗑️</button>
        <div class="reminder-toggle ${r.enabled ? '' : 'off'}" onclick="toggleCustomReminder('${r.id}')"><div class="toggle-knob"></div></div>
      </div>
    `;
  }).join('');
}

// ─── Profile: Customize Pet ──────────────────────────────────────────────────
function applyActivePet() {
  const pet = getActivePet();
  // Home page avatar
  const homeImg = document.querySelector('.pet-avatar .pet-img');
  if (homeImg) homeImg.src = pet.sprite;
  // Profile avatar (skip if user has a remote photoURL from auth)
  const profileImg = document.querySelector('.profile-avatar-big img');
  if (profileImg && !(profileImg.src || '').includes('http')) {
    profileImg.src = pet.sprite;
  }
  // Level-up modal
  const levelupImg = document.querySelector('.levelup-karu img');
  if (levelupImg) levelupImg.src = pet.sprite;
  // Pet name (preserve the level-badge inside .pet-name)
  const nameEl = document.querySelector('.pet-name');
  if (nameEl) {
    const badge = nameEl.querySelector('.level-badge');
    nameEl.textContent = (state.petName || pet.defaultName) + ' ';
    if (badge) nameEl.appendChild(badge);
  }
  // Auth screen logo
  const authLogoImg = document.querySelector('.auth-logo img');
  if (authLogoImg) authLogoImg.src = pet.sprite;
}

function showCustomizePet() {
  if (typeof playSound === 'function') playSound('open');
  renderPetPicker();
  const modal = document.getElementById('petSelectorModal');
  if (!modal) return;
  modal.style.opacity = '1';
  modal.style.pointerEvents = 'all';
  modal.classList.add('show');
}

function closePetSelector() {
  if (typeof playSound === 'function') playSound('close');
  const modal = document.getElementById('petSelectorModal');
  if (!modal) return;
  modal.style.opacity = '0';
  modal.style.pointerEvents = 'none';
  modal.classList.remove('show');
}

function renderPetPicker() {
  const grid = document.getElementById('petPickerGrid');
  if (!grid) return;
  grid.innerHTML = Object.values(PETS).map(pet => `
    <div class="pet-pick-card ${state.activePet === pet.id ? 'pet-pick-active' : ''}" onclick="selectPet('${pet.id}')">
      <div class="pet-pick-avatar"><img src="${pet.sprite}" alt="${escapeHtml(pet.defaultName)}" width="90" height="90"></div>
      <div class="pet-pick-name">${escapeHtml(pet.defaultName)}</div>
      <div class="pet-pick-species">${escapeHtml(pet.species)}</div>
      <div class="pet-pick-desc">${escapeHtml(pet.desc)}</div>
      ${state.activePet === pet.id ? '<div class="pet-pick-badge">✓ Active</div>' : '<button class="pet-pick-btn">Choose</button>'}
    </div>
  `).join('');
}

function selectPet(petId) {
  if (!PETS[petId]) return;
  if (typeof playSound === 'function') playSound('complete');
  state.activePet = petId;
  state.petName = PETS[petId].defaultName;
  saveState();
  applyActivePet();
  renderPetPicker();
  showPopup(`✨ ${PETS[petId].defaultName} is now your companion!`);
  launchConfetti();
}

// ─── Sound toggle label sync ─────────────────────────────────────────────────
function updateSoundToggleLabel() {
  const lbl = document.getElementById('soundToggleLabel');
  if (lbl) lbl.textContent = state.soundEnabled === false ? 'Sound: OFF' : 'Sound: ON';
}

// ─── Reduced motion toggle ───────────────────────────────────────────────────
function applyReducedMotion() {
  document.documentElement.classList.toggle('reduce-motion', state.reducedMotion === true);
}

function toggleReducedMotion() {
  state.reducedMotion = !state.reducedMotion;
  saveState();
  applyReducedMotion();
  updateMotionToggleLabel();
  if (typeof playSound === 'function') playSound('tap');
  if (typeof showPopup === 'function') {
    showPopup(state.reducedMotion ? '🪷 Reduced motion ON' : '✨ Full animations ON');
  }
}

function updateMotionToggleLabel() {
  const lbl = document.getElementById('motionToggleLabel');
  if (lbl) lbl.textContent = state.reducedMotion ? 'Motion: Reduced' : 'Motion: Full';
}

// ─── Pronouns ────────────────────────────────────────────────────────────────
function showPronouns() {
  if (typeof playSound === 'function') playSound('open');
  const current = state.pronouns || 'they/them';
  const radio = document.querySelector(`input[name="pronounChoice"][value="${current}"]`);
  if (radio) radio.checked = true;
  const modal = document.getElementById('pronounModal');
  if (!modal) return;
  modal.style.opacity = '1';
  modal.style.pointerEvents = 'all';
  modal.classList.add('show');
}

function closePronouns() {
  if (typeof playSound === 'function') playSound('close');
  const modal = document.getElementById('pronounModal');
  if (!modal) return;
  modal.style.opacity = '0';
  modal.style.pointerEvents = 'none';
  modal.classList.remove('show');
}

function savePronouns() {
  const sel = document.querySelector('input[name="pronounChoice"]:checked');
  if (!sel) return;
  state.pronouns = sel.value;
  saveState();
  updatePronounLabel();
  closePronouns();
  showPopup(`💬 Got it — ${sel.value}!`);
}

function updatePronounLabel() {
  const lbl = document.getElementById('pronounLabel');
  if (lbl) lbl.textContent = `Pronouns: ${state.pronouns || 'they/them'}`;
}

// ─── Profile: ND Profile ─────────────────────────────────────────────────────
function showNDProfile() {
  const current = state.ndProfile || 'ADHD';
  const options = ['ADHD', 'Autism', 'Dyslexia', 'Anxiety', 'Multiple', 'Prefer not to say'];
  const idx = options.indexOf(current);
  const choice = prompt(
    `Your ND profile: ${current}\n\nChange to one of:\n${options.map((o,i) => `${i+1}. ${o}`).join('\n')}\n\nEnter a number (1-${options.length}):`,
    String(idx + 1)
  );
  if (!choice) return;
  const n = parseInt(choice, 10);
  if (n >= 1 && n <= options.length) {
    state.ndProfile = options[n - 1];
    saveState();
    document.querySelector('.profile-tag').textContent = `Neurodivergent & Proud ✦ ${state.ndProfile}`;
    showPopup(`🧠 Profile updated: ${state.ndProfile}`);
  }
}

// ─── Profile: Share Sparq ────────────────────────────────────────────────────
async function shareSparq() {
  const shareData = {
    title: 'Sparq — ADHD Companion',
    text: 'Check out Sparq! A gamified ADHD companion app with a virtual pet. Come beat me 🔥',
    url: 'https://sparqapp.com',
  };
  try {
    if (navigator.share) {
      await navigator.share(shareData);
    } else {
      await navigator.clipboard.writeText(`${shareData.text} ${shareData.url}`);
      showPopup('💝 Share link copied to clipboard!');
    }
  } catch (err) {
    if (err.name !== 'AbortError') {
      showPopup('Share unavailable — copy link manually');
    }
  }
}

// ─── Safety: content filter wordlist ─────────────────────────────────────────
const BLOCKED_WORDS = [
  'address','phone number','snap','snapchat','discord server','whatsapp','meet up',
  'meet irl','private chat','dm me','kik','telegram','signal me','send pics',
  'how old are you','what school','what grade','you alone','don\'t tell',
  'keep this between us','our secret'
];

function containsBlockedContent(text) {
  const lower = text.toLowerCase();
  return BLOCKED_WORDS.some(w => lower.includes(w));
}

// ─── Safety: modal + community gate ──────────────────────────────────────────
function showOverlay(id) {
  const el = document.getElementById(id);
  el.style.opacity = '1';
  el.style.pointerEvents = 'all';
  el.classList.add('show');
}
function hideOverlay(id) {
  const el = document.getElementById(id);
  el.style.opacity = '0';
  el.style.pointerEvents = 'none';
  el.classList.remove('show');
}

function showCommunityPage() {
  const agreed = localStorage.getItem('sparq_safety_agreed');
  if (!agreed) showOverlay('safetyModal');
}

function agreeSafety() {
  localStorage.setItem('sparq_safety_agreed', '1');
  hideOverlay('safetyModal');
}

function showSafetyInfo() {
  showOverlay('safetyModal');
}

// ─── Safety: report ──────────────────────────────────────────────────────────
let _reportingPostId = null;

function reportPostById(postId) {
  _reportingPostId = postId;
  closeAllPostMenus();
  showOverlay('reportModal');
}

function submitReport(btn) {
  const reason = btn.textContent;
  if (_reportingPostId) hidePost(_reportingPostId);
  hideOverlay('reportModal');
  showPopup('🛡️ Reported — thank you for keeping Sparq safe.');
  _reportingPostId = null;
}

function closeReport() {
  hideOverlay('reportModal');
  _reportingPostId = null;
}

// ─── Safety: block / hide ────────────────────────────────────────────────────
function blockUser(postId, username) {
  closeAllPostMenus();
  const blocked = JSON.parse(localStorage.getItem('sparq_blocked') || '[]');
  if (!blocked.includes(username)) {
    blocked.push(username);
    localStorage.setItem('sparq_blocked', JSON.stringify(blocked));
  }
  hidePost(postId);
  showPopup(`🚫 ${escapeHtml(username)} blocked.`);
}

function hidePost(postId) {
  const el = document.getElementById(postId);
  if (el) { el.style.transition = 'opacity .3s'; el.style.opacity = '0'; setTimeout(() => el.remove(), 300); }
  hideOverlay('reportModal');
}

function showBlockedList() {
  const blocked = JSON.parse(localStorage.getItem('sparq_blocked') || '[]');
  if (!blocked.length) { showPopup('No blocked users yet ✦'); return; }
  alert('Blocked users:\n' + blocked.join('\n'));
}

// ─── Safety: post menu toggle ─────────────────────────────────────────────────
function togglePostMenu(postId) {
  closeAllPostMenus();
  const menu = document.getElementById('menu-' + postId);
  if (menu) menu.classList.toggle('show');
}

function closeAllPostMenus() {
  document.querySelectorAll('.post-menu.show').forEach(m => m.classList.remove('show'));
}

document.addEventListener('click', (e) => {
  if (!e.target.closest('.post-menu-btn') && !e.target.closest('.post-menu'))
    closeAllPostMenus();
});

// ─── Safety: settings toggles ────────────────────────────────────────────────
function saveSafetySetting(key, value) {
  const settings = JSON.parse(localStorage.getItem('sparq_safety_settings') || '{}');
  settings[key] = value;
  localStorage.setItem('sparq_safety_settings', JSON.stringify(settings));
  const msgs = {
    privateMode: value ? '👁️ Profile hidden from search.' : '👁️ Profile is now public.',
    safeDMs:     value ? '🔒 Safe DMs on.' : '🔒 Anyone can message you now.',
    under13:     value ? '👶 Under 13 mode enabled.' : '👶 Under 13 mode off.',
    contentFilter: value ? '🤬 Content filter on.' : '🤬 Content filter disabled.',
  };
  showPopup(msgs[key] || 'Setting saved.');
}

// ─── Like post ────────────────────────────────────────────────────────────────
function likePost(btn) {
  btn.classList.toggle('liked');
  const el = btn.querySelector('span');
  el.textContent = btn.classList.contains('liked') ? +el.textContent + 1 : +el.textContent - 1;
}

// ─── Add task ─────────────────────────────────────────────────────────────────
// ─── Pre-generated ADHD-friendly task presets ────────────────────────────────
const QUEST_PRESETS = {
  morning: [
    { t: 'Take morning meds',      xp: 20 },
    { t: 'Drink a glass of water', xp: 10 },
    { t: 'Brush teeth',            xp: 10 },
    { t: 'Eat something',          xp: 20 },
    { t: 'Shower',                 xp: 20 },
    { t: 'Get dressed',            xp: 10 },
    { t: 'Make bed',               xp: 10 },
    { t: 'Step outside for 2 min', xp: 20 },
  ],
  work: [
    { t: 'Answer urgent emails',        xp: 20 },
    { t: '25-min focus block',          xp: 40 },
    { t: 'Write tomorrow\'s to-do list',xp: 20 },
    { t: 'Take a screen break',         xp: 10 },
    { t: 'Finish one small task',       xp: 20 },
    { t: 'Clear desk',                  xp: 10 },
    { t: 'Plan one thing for today',    xp: 20 },
  ],
  selfcare: [
    { t: 'Stretch for 2 minutes',       xp: 10 },
    { t: 'Call or text a friend',       xp: 20 },
    { t: 'Go for a walk',               xp: 40 },
    { t: 'Journal for 5 minutes',       xp: 20 },
    { t: 'Deep breathing (3 min)',      xp: 10 },
    { t: 'Do one nice thing for you',   xp: 20 },
    { t: 'Say 3 things you\'re grateful for', xp: 10 },
  ],
  evening: [
    { t: 'Pack bag for tomorrow',   xp: 20 },
    { t: 'Set out clothes',         xp: 10 },
    { t: 'Take evening meds',       xp: 20 },
    { t: 'Phone away at bedtime',   xp: 20 },
    { t: 'Tidy one small area',     xp: 20 },
    { t: 'Read for 10 minutes',     xp: 20 },
    { t: 'Lights out by 11pm',      xp: 40 },
  ],
  quickwin: [
    { t: 'Drink water',         xp: 10 },
    { t: 'Make the bed',        xp: 10 },
    { t: 'Do one dish',         xp: 10 },
    { t: 'Put phone down (2 min)', xp: 10 },
    { t: 'Take 5 deep breaths', xp: 10 },
    { t: 'Stand up + stretch',  xp: 10 },
    { t: 'Reply to one text',   xp: 10 },
    { t: 'Throw out 3 things',  xp: 10 },
  ],
};

let _questPresetCat = 'morning';

function filterQuestPresets(cat, btn) {
  _questPresetCat = cat;
  document.querySelectorAll('.qp-tab').forEach(t => t.classList.remove('active'));
  if (btn) btn.classList.add('active');
  renderQuestPresets();
}

function renderQuestPresets() {
  const grid = document.getElementById('questPresetGrid');
  if (!grid) return;
  const list = QUEST_PRESETS[_questPresetCat] || [];
  grid.innerHTML = list.map(p => `
    <button class="quest-preset-chip" onclick="fillQuestFromPreset('${escapeHtml(p.t).replace(/'/g,'&apos;')}', ${p.xp})">
      <span class="qp-text">${escapeHtml(p.t)}</span>
      <span class="qp-xp">+${p.xp} XP</span>
    </button>
  `).join('');
}

function fillQuestFromPreset(text, xp) {
  if (typeof playSound === 'function') playSound('tap');
  document.getElementById('newQuestTitle').value = text.replace(/&apos;/g, "'");
  const radio = document.querySelector(`input[name="questXp"][value="${xp}"]`);
  if (radio) radio.checked = true;
  // Flash the input to show it's been filled
  const input = document.getElementById('newQuestTitle');
  if (input) {
    input.style.transition = 'background-color 0.3s';
    input.style.backgroundColor = 'rgba(0,255,212,0.15)';
    setTimeout(() => { input.style.backgroundColor = ''; }, 500);
  }
}

function showAddTask() {
  if (typeof playSound === 'function') playSound('open');
  document.getElementById('newQuestTitle').value = '';
  const defaultRadio = document.querySelector('input[name="questXp"][value="20"]');
  if (defaultRadio) defaultRadio.checked = true;
  // Reset to morning category and render presets
  _questPresetCat = 'morning';
  document.querySelectorAll('.qp-tab').forEach(t => t.classList.toggle('active', t.dataset.cat === 'morning'));
  renderQuestPresets();
  const modal = document.getElementById('newQuestModal');
  modal.style.opacity = '1';
  modal.style.pointerEvents = 'all';
  modal.classList.add('show');
}

function closeNewQuestDialog() {
  if (typeof playSound === 'function') playSound('close');
  const modal = document.getElementById('newQuestModal');
  modal.style.opacity = '0';
  modal.style.pointerEvents = 'none';
  modal.classList.remove('show');
}

function saveNewQuest() {
  const name = document.getElementById('newQuestTitle').value.trim();
  const xpEl = document.querySelector('input[name="questXp"]:checked');
  if (!name) { showPopup('Give your quest a name ✍️'); return; }
  const xp = parseInt(xpEl ? xpEl.value : 20, 10);
  if (!state.customTasks) state.customTasks = [];
  const task = { name, xp, done: false };
  state.customTasks.push(task);
  saveState();
  const list = document.getElementById('taskList');
  const item = document.createElement('div');
  item.className = 'task-item';
  item.innerHTML = `
    <div class="task-check"></div>
    <div class="task-content">
      <div class="task-title">${escapeHtml(name)}</div>
      <div class="task-meta">✦ Custom quest</div>
    </div>
    <div class="task-xp">+${xp} XP</div>`;
  item.onclick = () => { completeTask(item, xp); task.done = true; saveState(); };
  list.appendChild(item);
  updateUI();
  closeNewQuestDialog();
  showPopup(`⚔️ Quest added: ${name}`);
}

// ─── Journal ──────────────────────────────────────────────────────────────────
const JOURNAL_PROMPTS = [
  "What made you smile today, even just a little?",
  "What's one thing you're grateful for right now?",
  "What challenged you today? How did you handle it?",
  "If today were a movie scene, what would the title be?",
  "What's something you did today that future-you will thank you for?",
  "Describe your energy level today in one sentence.",
  "What's one kind thing you can say to yourself right now?",
  "What distracted you the most today? No judgment.",
  "Write about a small win — even getting out of bed counts.",
  "If your brain had a weather forecast today, what would it be?",
  "What's one thing you wish people understood about you?",
  "Name three things you can see, hear, and feel right now.",
  "What would make tomorrow a good day?",
  "What song matches your mood right now?",
  "Write a letter to yesterday's you. What would you say?",
];

let _promptIdx = Math.floor(Math.random() * JOURNAL_PROMPTS.length);

function initJournalDate() {
  const now = new Date();
  const days = ['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'];
  const months = ['January','February','March','April','May','June','July','August','September','October','November','December'];
  const dayName = document.getElementById('journalDayName');
  const fullDate = document.getElementById('journalFullDate');
  if (dayName) dayName.textContent = days[now.getDay()];
  if (fullDate) fullDate.textContent = `${months[now.getMonth()]} ${now.getDate()}, ${now.getFullYear()}`;

  const prompt = document.getElementById('journalPrompt');
  if (prompt) prompt.textContent = JOURNAL_PROMPTS[_promptIdx];

  const badge = document.getElementById('journalStreakBadge');
  if (badge) badge.textContent = `${state.journalCount || 0} entries`;
}

function shufflePrompt() {
  _promptIdx = (_promptIdx + 1) % JOURNAL_PROMPTS.length;
  const el = document.getElementById('journalPrompt');
  if (el) el.textContent = JOURNAL_PROMPTS[_promptIdx];
}

function setJournalMood(btn) {
  document.querySelectorAll('.journal-mood-btn').forEach(b => b.classList.remove('selected'));
  btn.classList.add('selected');
  state.journalMood = btn.dataset.emoji;
}

document.addEventListener('input', (e) => {
  if (e.target.id === 'journalEntry')
    document.getElementById('wordCount').textContent = `${e.target.value.length} / 2000`;
});

function saveJournalEntry() {
  const textarea = document.getElementById('journalEntry');
  const text     = textarea.value.trim();
  if (!text) { showPopup('Write something first ✍️', 1000); return; }

  state.journalCount++;
  awardXP(10);
  showPopup('📓 +10 XP! Entry saved!');
  spawnDamageNum(10, '#00FFD4');
  launchConfetti();

  renderJournalEntry({ mood: state.journalMood, text, date: 'Just now', xp: 10 }, true);
  checkJournalEgg(text);
  textarea.value = '';
  document.getElementById('wordCount').textContent = '0 / 1000';
}

function renderJournalEntry(entry, prepend = false) {
  const list = document.getElementById('journalList');
  const card = document.createElement('div');
  card.className = 'journal-entry-card';
  card.innerHTML = `
    <div class="journal-entry-header">
      <span class="journal-entry-mood">${entry.mood}</span>
      <span class="journal-entry-date">${escapeHtml(entry.date)}</span>
    </div>
    <p class="journal-entry-text">${escapeHtml(entry.text)}</p>`;
  prepend && list.firstChild ? list.insertBefore(card, list.firstChild) : list.appendChild(card);
}

// ─── XSS protection ───────────────────────────────────────────────────────────
function escapeHtml(str) {
  return str.replace(/[&<>"']/g, m => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#039;'}[m]));
}

// ─── Render saved custom tasks ───────────────────────────────────────────────
function renderSavedTasks() {
  const list = document.getElementById('taskList');
  if (!state.customTasks) state.customTasks = [];
  state.customTasks.forEach((task, i) => {
    if (task.done) return;
    const item = document.createElement('div');
    item.className = 'task-item';
    item.innerHTML = `
      <div class="task-check"></div>
      <div class="task-content">
        <div class="task-title">${escapeHtml(task.name)}</div>
        <div class="task-meta">✦ Custom quest</div>
      </div>
      <div class="task-xp">+${task.xp} XP</div>`;
    item.onclick = () => { completeTask(item, task.xp); task.done = true; saveState(); };
    list.appendChild(item);
  });
}

// ─── Store ───────────────────────────────────────────────────────────────────
let _storeFilter = 'hat';

function filterStore(slot, btnEl) {
  _storeFilter = slot;
  if (btnEl) {
    document.querySelectorAll('.store-tab').forEach(b => b.classList.remove('active'));
    btnEl.classList.add('active');
  }
  renderStore();
}

function renderStore() {
  const grid = document.getElementById('storeGrid');
  if (!grid) return;
  const coinEl = document.getElementById('coinCount');
  if (coinEl) coinEl.textContent = state.sparqCoins || 0;

  // Food and potions are stockpilable — different rendering
  if (['food','potion','treat','key'].includes(_storeFilter)) {
    const poolMap = { food: FOOD_ITEMS, potion: POTION_ITEMS, treat: TREAT_ITEMS, key: KEY_ITEMS };
    const keyMap  = { food: 'foodInventory', potion: 'potionInventory', treat: 'treatInventory', key: 'keyInventory' };
    const items = poolMap[_storeFilter];
    const invKey = keyMap[_storeFilter];
    const inv = state[invKey] || {};
    grid.innerHTML = items.map(item => {
      const owned = inv[item.id] || 0;
      const canAfford = (state.sparqCoins || 0) >= item.cost;
      const btnLabel = canAfford ? `Buy · ${item.cost} 🪙` : `Need ${item.cost} 🪙`;
      const btnClass = canAfford ? '' : 'disabled';
      let subInfo;
      if (_storeFilter === 'food')      subInfo = `+${item.hunger} 🍴 · +${item.xp} ⚡`;
      else if (_storeFilter === 'key')  subInfo = `${item.rarity} · ${item.desc}`;
      else                              subInfo = item.desc || '';
      return `
        <div class="store-card ${owned ? 'owned-card' : ''}">
          <div class="store-icon">${item.icon}</div>
          <div class="store-name">${escapeHtml(item.name)}</div>
          <div class="store-desc">${escapeHtml(subInfo)}</div>
          ${owned ? `<div class="store-inv-count">You have: x${owned}</div>` : ''}
          <button class="store-btn ${btnClass}" ${canAfford ? '' : 'disabled'} onclick="buyConsumable('${item.id}','${_storeFilter}')">${btnLabel}</button>
        </div>
      `;
    }).join('');
    return;
  }

  const items = STORE_ITEMS.filter(i => i.slot === _storeFilter);
  const owned = new Set(state.purchases || []);
  const equipped = state.equippedItems || {};
  grid.innerHTML = items.map(item => {
    const isOwned = owned.has(item.id);
    const isEquipped = equipped[item.slot] === item.id;
    const canAfford = (state.sparqCoins || 0) >= item.cost;
    const meetsLevel = state.level >= item.levelReq;
    let btn;
    if (isEquipped) btn = '<button class="store-btn equipped" disabled>✓ Equipped</button>';
    else if (isOwned) btn = `<button class="store-btn owned" onclick="equipItem('${item.id}')">Equip</button>`;
    else if (!meetsLevel) btn = `<button class="store-btn locked" disabled>🔒 Lv.${item.levelReq}</button>`;
    else if (!canAfford) btn = `<button class="store-btn disabled" disabled>Need ${item.cost} 🪙</button>`;
    else btn = `<button class="store-btn" onclick="buyItem('${item.id}')">Buy · ${item.cost} 🪙</button>`;
    return `
      <div class="store-card ${isOwned ? 'owned-card' : ''}">
        <div class="store-icon">${item.icon}</div>
        <div class="store-name">${escapeHtml(item.name)}</div>
        <div class="store-desc">${escapeHtml(item.desc)}</div>
        ${btn}
      </div>
    `;
  }).join('');
}

function buyItem(itemId) {
  const item = STORE_ITEMS.find(i => i.id === itemId);
  if (!item) return;
  if ((state.sparqCoins || 0) < item.cost) { showPopup('Not enough coins 🪙'); return; }
  if (state.level < item.levelReq) { showPopup(`Need level ${item.levelReq}!`); return; }
  state.sparqCoins -= item.cost;
  if (!state.purchases) state.purchases = [];
  state.purchases.push(itemId);
  if (!state.equippedItems) state.equippedItems = {};
  state.equippedItems[item.slot] = itemId; // auto-equip
  saveState();
  if (typeof playSound === 'function') playSound('coin');
  showPopup(`🎉 Bought ${item.name}!`);
  launchConfetti();
  checkAchievements();
  renderStore();
  applyEquippedItems();
}

function equipItem(itemId) {
  const item = STORE_ITEMS.find(i => i.id === itemId);
  if (!item) return;
  if (!state.equippedItems) state.equippedItems = {};
  if (state.equippedItems[item.slot] === itemId) {
    // Unequip if already equipped
    delete state.equippedItems[item.slot];
    showPopup(`Unequipped ${item.name}`);
  } else {
    state.equippedItems[item.slot] = itemId;
    showPopup(`✨ Equipped ${item.name}!`);
  }
  saveState();
  renderStore();
  applyEquippedItems();
}

function applyEquippedItems() {
  // Show equipped hat/accessory icons over Karu on home page
  const petAvatars = document.querySelectorAll('.pet-avatar');
  petAvatars.forEach(avatar => {
    // Remove existing badges
    avatar.querySelectorAll('.equipped-badge').forEach(e => e.remove());
    const equipped = state.equippedItems || {};
    const hatItem = STORE_ITEMS.find(i => i.id === equipped.hat);
    const accItem = STORE_ITEMS.find(i => i.id === equipped.accessory);
    if (hatItem) {
      const el = document.createElement('span');
      el.className = 'equipped-badge equipped-hat';
      el.textContent = hatItem.icon;
      avatar.appendChild(el);
    }
    if (accItem) {
      const el = document.createElement('span');
      el.className = 'equipped-badge equipped-accessory';
      el.textContent = accItem.icon;
      avatar.appendChild(el);
    }
  });
  // Apply background if equipped
  const bgItem = STORE_ITEMS.find(i => i.id === (state.equippedItems || {}).background);
  const phoneEl = document.querySelector('.phone');
  if (phoneEl) phoneEl.setAttribute('data-bg', bgItem ? bgItem.id : '');
}

// ─── Buy consumable (food / potion) ──────────────────────────────────────────
function buyConsumable(itemId, type) {
  const poolMap = { food: FOOD_ITEMS, potion: POTION_ITEMS, treat: TREAT_ITEMS, key: KEY_ITEMS };
  const keyMap  = { food: 'foodInventory', potion: 'potionInventory', treat: 'treatInventory', key: 'keyInventory' };
  const pool = poolMap[type];
  const invKey = keyMap[type];
  if (!pool || !invKey) return;
  const item = pool.find(i => i.id === itemId);
  if (!item) return;
  if ((state.sparqCoins || 0) < item.cost) { showPopup('Not enough coins 🪙'); return; }
  state.sparqCoins -= item.cost;
  if (!state[invKey]) state[invKey] = {};
  state[invKey][itemId] = (state[invKey][itemId] || 0) + 1;
  if (typeof playSound === 'function') playSound('coin');
  saveState();
  showPopup(`🎁 Bought ${item.name}! (x${state[invKey][itemId]})`);
  renderStore();
  checkAchievements();
}

// ─── Feed menu ──────────────────────────────────────────────────────────────
function openFeedMenu() {
  if (typeof playSound === 'function') playSound('open');
  renderFeedGrid();
  const modal = document.getElementById('feedModal');
  modal.style.opacity = '1';
  modal.style.pointerEvents = 'all';
  modal.classList.add('show');
}

function closeFeedMenu() {
  if (typeof playSound === 'function') playSound('close');
  const modal = document.getElementById('feedModal');
  modal.style.opacity = '0';
  modal.style.pointerEvents = 'none';
  modal.classList.remove('show');
}

function renderFeedGrid() {
  const grid = document.getElementById('feedGrid');
  if (!grid) return;
  const inv = state.foodInventory || {};
  const owned = FOOD_ITEMS.filter(f => (inv[f.id] || 0) > 0);
  if (!owned.length) {
    grid.innerHTML = `
      <div class="empty-hint">
        No food in your bag yet.
        <br><button class="inline-link-btn" onclick="closeFeedMenu(); switchPage('store'); setTimeout(() => filterStore('food', document.querySelector('[data-slot=food]')), 100);">Visit the shop →</button>
      </div>`;
    return;
  }
  grid.innerHTML = owned.map(f => `
    <div class="feed-card" onclick="feedKaru('${f.id}')">
      <div class="feed-icon">${f.icon}</div>
      <div class="feed-name">${escapeHtml(f.name)}</div>
      <div class="feed-qty">x${inv[f.id]}</div>
      <div class="feed-boost">+${f.hunger} 🍴 · +${f.xp} ⚡</div>
    </div>
  `).join('');
}

function feedKaru(foodId) {
  if (typeof playSound === 'function') playSound('feed');
  const food = FOOD_ITEMS.find(f => f.id === foodId);
  if (!food) return;
  if (!state.foodInventory) state.foodInventory = {};
  if ((state.foodInventory[foodId] || 0) < 1) { showPopup('Out of stock'); return; }
  state.foodInventory[foodId]--;
  state.hunger = Math.min(100, (state.hunger || 0) + food.hunger);
  awardXP(food.xp);
  showPopup(`${food.icon} Karu loved the ${food.name}!`);
  spawnBlossom(); spawnBlossom();
  saveState();
  updateUI();
  renderFeedGrid();
  // Karu happy animation
  const pet = document.querySelector('.pet-avatar');
  if (pet) {
    pet.style.transform = 'scale(1.15) rotate(-5deg)';
    setTimeout(() => { pet.style.transform = ''; }, 300);
  }
}

// ─── Bag / Inventory ────────────────────────────────────────────────────────
let _bagFilter = 'potion';

function openInventory() {
  if (typeof playSound === 'function') playSound('open');
  _bagFilter = 'potion';
  document.querySelectorAll('.bag-tab').forEach(t => t.classList.toggle('active', t.dataset.type === 'potion'));
  renderBag();
  const modal = document.getElementById('bagModal');
  modal.style.opacity = '1';
  modal.style.pointerEvents = 'all';
  modal.classList.add('show');
}

function closeInventory() {
  if (typeof playSound === 'function') playSound('close');
  const modal = document.getElementById('bagModal');
  modal.style.opacity = '0';
  modal.style.pointerEvents = 'none';
  modal.classList.remove('show');
}

function filterBag(type, btn) {
  _bagFilter = type;
  document.querySelectorAll('.bag-tab').forEach(t => t.classList.remove('active'));
  if (btn) btn.classList.add('active');
  renderBag();
}

function renderBag() {
  const grid = document.getElementById('bagGrid');
  if (!grid) return;

  // Gear tab — show equipped items
  if (_bagFilter === 'gear') {
    const equipped = state.equippedItems || {};
    const hatItem = STORE_ITEMS.find(i => i.id === equipped.hat);
    const accItem = STORE_ITEMS.find(i => i.id === equipped.accessory);
    const bgItem  = STORE_ITEMS.find(i => i.id === equipped.background);
    const owned = state.purchases || [];
    const ownedCount = owned.length;
    if (!hatItem && !accItem && !bgItem && ownedCount === 0) {
      grid.innerHTML = `<div class="empty-hint">No gear yet! Visit Shop → Hats / Gear / Backgrounds to buy outfits.</div>`;
      return;
    }
    let html = '';
    html += `<div class="bag-section-title">Equipped</div><div class="bag-equipped-row">`;
    html += hatItem ? `<div class="bag-equipped-slot"><div class="bag-equipped-icon">${hatItem.icon}</div><div class="bag-equipped-name">${escapeHtml(hatItem.name)}</div><div class="bag-equipped-slot-label">Hat</div></div>` : `<div class="bag-equipped-slot bag-slot-empty"><div class="bag-equipped-icon">🎩</div><div class="bag-equipped-slot-label">No hat</div></div>`;
    html += accItem ? `<div class="bag-equipped-slot"><div class="bag-equipped-icon">${accItem.icon}</div><div class="bag-equipped-name">${escapeHtml(accItem.name)}</div><div class="bag-equipped-slot-label">Accessory</div></div>` : `<div class="bag-equipped-slot bag-slot-empty"><div class="bag-equipped-icon">✨</div><div class="bag-equipped-slot-label">No accessory</div></div>`;
    html += bgItem ? `<div class="bag-equipped-slot"><div class="bag-equipped-icon">${bgItem.icon}</div><div class="bag-equipped-name">${escapeHtml(bgItem.name)}</div><div class="bag-equipped-slot-label">Background</div></div>` : `<div class="bag-equipped-slot bag-slot-empty"><div class="bag-equipped-icon">🏞️</div><div class="bag-equipped-slot-label">No background</div></div>`;
    html += `</div>`;
    html += `<div class="bag-section-title">Owned (${ownedCount})</div>`;
    if (ownedCount === 0) {
      html += `<div class="empty-hint" style="grid-column:1/-1">Nothing owned yet</div>`;
    } else {
      html += `<div class="bag-grid-sub">`;
      owned.forEach(id => {
        const item = STORE_ITEMS.find(i => i.id === id);
        if (!item) return;
        const isEquipped = (state.equippedItems || {})[item.slot] === item.id;
        html += `<div class="bag-card bag-card-owned" onclick="equipItem('${item.id}')">
          <div class="bag-icon">${item.icon}</div>
          <div class="bag-name">${escapeHtml(item.name)}</div>
          <div class="bag-desc">${isEquipped ? '<span style="color:var(--teal)">✓ Equipped</span>' : 'Tap to equip'}</div>
        </div>`;
      });
      html += `</div>`;
    }
    grid.innerHTML = html;
    return;
  }

  // Trophy tab — show defeated enemies
  if (_bagFilter === 'trophy') {
    const defeated = state.defeatedEnemies || [];
    if (!defeated.length) {
      grid.innerHTML = `<div class="empty-hint">No trophies yet. Defeat an enemy to earn your first!</div>`;
      return;
    }
    grid.innerHTML = defeated.map(id => {
      const enemy = ENEMIES.find(e => e.id === id);
      if (!enemy) return '';
      return `<div class="bag-card bag-trophy-card" style="border-color:${enemy.color};">
        <div class="bag-icon">${enemy.fallbackEmoji}</div>
        <div class="bag-name">${escapeHtml(enemy.name)}</div>
        <div class="bag-desc">⚔️ Defeated<br><small>${escapeHtml(enemy.title)}</small></div>
      </div>`;
    }).join('');
    return;
  }

  // Consumables (potion, food, treat, key)
  const poolMap = { potion: POTION_ITEMS, food: FOOD_ITEMS, treat: TREAT_ITEMS, key: KEY_ITEMS };
  const keyMap  = { potion: 'potionInventory', food: 'foodInventory', treat: 'treatInventory', key: 'keyInventory' };
  const pool = poolMap[_bagFilter] || POTION_ITEMS;
  const invKey = keyMap[_bagFilter] || 'potionInventory';
  const inv = state[invKey] || {};
  const owned = pool.filter(i => (inv[i.id] || 0) > 0);
  if (!owned.length) {
    grid.innerHTML = `<div class="empty-hint">Nothing here. Buy some from the Shop!</div>`;
    return;
  }
  grid.innerHTML = owned.map(item => `
    <div class="bag-card">
      <div class="bag-icon">${item.icon}</div>
      <div class="bag-name">${escapeHtml(item.name)}</div>
      <div class="bag-desc">${escapeHtml(item.desc || '')}</div>
      <div class="bag-qty">x${inv[item.id]}</div>
      <button class="bag-use-btn" onclick="useBagItem('${item.id}','${_bagFilter}')">Use</button>
    </div>
  `).join('');
}

function useBagItem(itemId, type) {
  if (type === 'food') { feedKaru(itemId); renderBag(); return; }

  if (type === 'treat') {
    const treat = TREAT_ITEMS.find(t => t.id === itemId);
    if (!treat) return;
    if (!state.treatInventory) state.treatInventory = {};
    if ((state.treatInventory[itemId] || 0) < 1) { showPopup('Out of stock'); return; }
    state.treatInventory[itemId]--;
    if (typeof playSound === 'function') playSound('feed');
    switch (treat.effect) {
      case 'pet_xp':
        state.petTapBoost = { remaining: (treat.id === 'treat_fish' ? 10 : 5), amount: treat.value };
        showPopup(`💖 Karu's next ${state.petTapBoost.remaining} taps give +${treat.value} XP each!`);
        break;
      case 'hunger':
        state.hunger = Math.min(100, (state.hunger || 0) + treat.value);
        showPopup(`${treat.icon} +${treat.value} hunger restored`);
        break;
      case 'coin_boost':
        state.coinBoostQuests = 5;
        state.coinBoostPct = treat.value;
        showPopup(`💰 Next 5 quests pay +${treat.value}% coins!`);
        break;
      case 'mood':
        state.doubleXpUntil = Date.now() + 30 * 60 * 1000;
        showPopup(`🌿 ZOOMIES MODE! Double XP for 30 min!`);
        launchConfetti();
        break;
    }
    saveState();
    renderBag();
    return;
  }

  if (type === 'key') {
    const key = KEY_ITEMS.find(k => k.id === itemId);
    if (!key) return;
    if (!state.keyInventory) state.keyInventory = {};
    if ((state.keyInventory[itemId] || 0) < 1) { showPopup('Out of stock'); return; }
    state.keyInventory[itemId]--;
    if (typeof playSound === 'function') playSound('coin');
    const rolls = {
      common:    { xp: [30, 80],   coins: [40, 100],  chance_item: 0.15 },
      uncommon:  { xp: [80, 200],  coins: [100, 250], chance_item: 0.3  },
      rare:      { xp: [200, 500], coins: [250, 600], chance_item: 0.5  },
      legendary: { xp: [500, 1500],coins: [600, 2000],chance_item: 1.0  },
    };
    const r = rolls[key.rarity] || rolls.common;
    const xpWon = Math.floor(Math.random() * (r.xp[1] - r.xp[0])) + r.xp[0];
    const coinsWon = Math.floor(Math.random() * (r.coins[1] - r.coins[0])) + r.coins[0];
    awardXP(xpWon);
    state.sparqCoins = (state.sparqCoins || 0) + coinsWon;
    let msg = `🎁 +${xpWon} XP, +${coinsWon} 🪙!`;
    if (Math.random() < r.chance_item) {
      const ownedSet = new Set(state.purchases || []);
      const unowned = STORE_ITEMS.filter(i => !ownedSet.has(i.id));
      if (unowned.length > 0) {
        const prize = unowned[Math.floor(Math.random() * unowned.length)];
        if (!state.purchases) state.purchases = [];
        state.purchases.push(prize.id);
        msg += `\n✨ ${prize.icon} ${prize.name}!`;
      }
    }
    showPopup(msg, 3000);
    launchConfetti();
    saveState();
    renderBag();
    return;
  }

  // Potion
  const pot = POTION_ITEMS.find(p => p.id === itemId);
  if (!pot) return;
  if (!state.potionInventory) state.potionInventory = {};
  if ((state.potionInventory[itemId] || 0) < 1) { showPopup('Out of stock'); return; }
  state.potionInventory[itemId]--;

  switch (pot.effect) {
    case 'xp':
      awardXP(pot.value);
      showPopup(`⚡ +${pot.value} XP!`);
      launchConfetti();
      break;
    case 'coins':
      state.sparqCoins = (state.sparqCoins || 0) + pot.value;
      showPopup(`🪙 +${pot.value} coins!`);
      break;
    case 'shield':
      state.streakShields = (state.streakShields || 0) + 1;
      showPopup(`🛡️ Shield stored! You have ${state.streakShields} now.`);
      break;
    case 'freeze':
      state.voltFreezeUntil = Date.now() + (pot.value * 60 * 60 * 1000);
      showPopup(`❄️ Volt frozen for ${pot.value} hours!`);
      break;
    case 'mystery': {
      const roll = Math.random();
      if (roll < 0.4) { awardXP(50); showPopup('🎁 +50 XP!'); }
      else if (roll < 0.7) { state.sparqCoins = (state.sparqCoins||0) + 80; showPopup('🎁 +80 coins!'); }
      else if (roll < 0.9) { awardXP(150); showPopup('🎁 JACKPOT! +150 XP!'); launchConfetti(); }
      else {
        const randFood = FOOD_ITEMS[Math.floor(Math.random() * FOOD_ITEMS.length)];
        if (!state.foodInventory) state.foodInventory = {};
        state.foodInventory[randFood.id] = (state.foodInventory[randFood.id] || 0) + 1;
        showPopup(`🎁 Got ${randFood.icon} ${randFood.name}!`);
      }
      break;
    }
  }
  saveState();
  updateUI();
  renderBag();
}

// ─── Adventures ──────────────────────────────────────────────────────────────
function renderAdventures() {
  const row = document.getElementById('adventuresRow');
  if (!row) return;
  const completed = new Set(state.completedAdventures || []);
  row.innerHTML = ADVENTURES.map(adv => {
    const progress = (state.adventureProgress || {})[adv.id] || 0;
    const isComplete = completed.has(adv.id);
    const pct = Math.round((progress / adv.steps.length) * 100);
    return `
      <div class="adventure-card ${isComplete ? 'adventure-done' : ''}" onclick="openAdventure('${adv.id}')">
        <div class="adventure-icon">${adv.icon}</div>
        <div class="adventure-title">${escapeHtml(adv.title)}</div>
        <div class="adventure-progress-bar">
          <div class="adventure-progress-fill" style="width:${pct}%"></div>
        </div>
        <div class="adventure-progress-text">${isComplete ? '✓ Complete' : `${progress}/${adv.steps.length} steps`}</div>
      </div>
    `;
  }).join('');
}

function openAdventure(id) {
  const adv = ADVENTURES.find(a => a.id === id);
  if (!adv) return;
  const progress = (state.adventureProgress || {})[id] || 0;
  const completed = (state.completedAdventures || []).includes(id);
  const lines = adv.steps.map((s, i) => {
    const done = i < progress;
    const current = i === progress && !completed;
    return `${done ? '✅' : current ? '👉' : '⬜'} ${s}`;
  }).join('\n');
  const msg = `${adv.icon} ${adv.title}\n${adv.description}\nReward: ${adv.reward} XP\n\n${lines}\n\n${completed ? '🏆 COMPLETE!' : (progress < adv.steps.length ? 'Tap OK to mark current step done' : '')}`;
  if (completed) { alert(msg); return; }
  if (confirm(msg)) {
    advanceAdventure(id);
  }
}

function advanceAdventure(id) {
  const adv = ADVENTURES.find(a => a.id === id);
  if (!adv) return;
  if (!state.adventureProgress) state.adventureProgress = {};
  const cur = state.adventureProgress[id] || 0;
  const next = cur + 1;
  state.adventureProgress[id] = next;
  if (next >= adv.steps.length) {
    // Complete!
    if (!state.completedAdventures) state.completedAdventures = [];
    if (!state.completedAdventures.includes(id)) {
      state.completedAdventures.push(id);
      awardXP(adv.reward);
      showPopup(`🏆 Adventure complete! +${adv.reward} XP`);
      launchConfetti();
    }
  } else {
    showPopup(`📜 Step ${next}/${adv.steps.length} done!`);
    awardXP(20);
  }
  saveState();
  checkAchievements();
  renderAdventures();
}

// ─── Easter eggs ────────────────────────────────────────────────────────────
// Egg #1: Konami code
const KONAMI = ['ArrowUp','ArrowUp','ArrowDown','ArrowDown','ArrowLeft','ArrowRight','ArrowLeft','ArrowRight','b','a'];
let _konamiIdx = 0;
document.addEventListener('keydown', (e) => {
  if (e.key && e.key.toLowerCase() === KONAMI[_konamiIdx].toLowerCase()) {
    _konamiIdx++;
    if (_konamiIdx >= KONAMI.length) {
      _konamiIdx = 0;
      triggerEgg('egg_konami', '🎮 RETRO MODE ACTIVATED', () => {
        document.body.style.animation = 'konamiRainbow 1s infinite';
        setTimeout(() => { document.body.style.animation = ''; }, 8000);
      });
    }
  } else {
    _konamiIdx = 0;
  }
});

// Egg #2: Tap date pill 10 times
let _dateTaps = 0;
document.addEventListener('click', (e) => {
  if (e.target.closest && e.target.closest('#datePill')) {
    _dateTaps++;
    if (_dateTaps === 10) {
      _dateTaps = 0;
      triggerEgg('egg_date_tap', '📅 TIME BANDIT — you cracked the calendar!', () => {
        for (let i = 0; i < 20; i++) spawnBlossom();
      });
    }
  } else {
    _dateTaps = 0;
  }
});

// Egg #4: Type "sparq" in journal entry
function checkJournalEgg(text) {
  const lower = (text || '').toLowerCase();
  if (lower.includes('sparq')) {
    triggerEgg('egg_sparq_word', '⚡ THE MAGIC WORD — you said the name', () => {
      for (let i = 0; i < 10; i++) spawnDamageNum(10, '#FFD700');
    });
  }
}

function triggerEgg(id, message, effect) {
  if (!state.eggs) state.eggs = [];
  if (state.eggs.includes(id)) return;  // already found
  state.eggs.push(id);
  showPopup(message, 2500);
  if (effect) effect();
  // Unlock the achievement
  const ach = ACHIEVEMENTS.find(a => a.id === id);
  if (ach && !state.unlockedAchievements.includes(id)) {
    state.unlockedAchievements.push(id);
    state.currentXP += ach.xp;
    state.totalXP += ach.xp;
    setTimeout(() => showAchievementToast(ach), 1500);
  }
  saveState();
  updateUI();
}

// Inject rainbow animation CSS
(function injectEggStyle() {
  const eggStyle = document.createElement('style');
  eggStyle.textContent = `
@keyframes konamiRainbow {
  0%   { filter: hue-rotate(0deg); }
  100% { filter: hue-rotate(360deg); }
}
`;
  document.head.appendChild(eggStyle);
})();

// ─── Focus Dungeon ───────────────────────────────────────────────────────────
const ENEMY_EMOJI = ['👾','🦇','🕷️','🐀','👻','💀','🦂','🐍','🦟','🪲'];
const BOSS_EMOJI  = ['👹','😈','🧛','🐲'];

let _fd = null;

function openFocusDungeonPicker() {
  if (typeof playSound === 'function') playSound('open');
  const modal = document.getElementById('focusPickerModal');
  modal.style.opacity = '1';
  modal.style.pointerEvents = 'all';
  modal.classList.add('show');
}

function closeFocusPicker() {
  if (typeof playSound === 'function') playSound('close');
  const modal = document.getElementById('focusPickerModal');
  modal.style.opacity = '0';
  modal.style.pointerEvents = 'none';
  modal.classList.remove('show');
}

function startFocusDungeon(minutes) {
  closeFocusPicker();
  if (typeof playSound === 'function') playSound('challenge');

  const durationMs = minutes * 60 * 1000;
  _fd = {
    durationMs,
    totalMinutes: minutes,
    startTime: Date.now(),
    endTime: Date.now() + durationMs,
    kills: 0,
    xp: 0,
    enemies: [],
    bossSpawned: false,
    ended: false,
    distractionPenalty: false,
    warningCount: 0,
  };

  // Show dungeon screen
  const screen = document.getElementById('focusDungeonScreen');
  screen.style.opacity = '1';
  screen.style.pointerEvents = 'all';
  screen.classList.add('active');
  document.getElementById('fdKills').textContent = '0';
  document.getElementById('fdXp').textContent = '0';

  // Apply selected pet to hero
  const pet = (typeof getActivePet === 'function') ? getActivePet() : null;
  if (pet) {
    const heroImg = document.querySelector('.fd-hero-img');
    if (heroImg) heroImg.src = pet.sprite;
  }

  // Start ticker
  _fd.tickerId = setInterval(fdTick, 1000);

  // Start enemy spawner
  fdScheduleNextSpawn();

  // Visibility change listener for distraction detection
  _fd.visListener = () => {
    if (document.visibilityState === 'hidden' && _fd && !_fd.ended) {
      _fd.distractionPenalty = true;
      _fd.warningCount++;
    }
    if (document.visibilityState === 'visible' && _fd && !_fd.ended) {
      const warn = document.getElementById('fdWarning');
      if (warn && _fd.warningCount > 0) {
        warn.style.display = 'block';
        setTimeout(() => { warn.style.display = 'none'; }, 3000);
      }
    }
  };
  document.addEventListener('visibilitychange', _fd.visListener);

  showPopup(`🏰 Dungeon started! Focus for ${minutes} min.`, 1800);
}

function fdTick() {
  if (!_fd || _fd.ended) return;
  const remaining = Math.max(0, _fd.endTime - Date.now());
  const elapsed = _fd.durationMs - remaining;
  const pct = (elapsed / _fd.durationMs) * 100;

  const mm = Math.floor(remaining / 60000);
  const ss = Math.floor((remaining % 60000) / 1000);
  document.getElementById('fdTime').textContent = `${String(mm).padStart(2,'0')}:${String(ss).padStart(2,'0')}`;
  document.getElementById('fdProgressFill').style.width = pct + '%';

  if (!_fd.bossSpawned && remaining <= 60000 && remaining > 0) {
    _fd.bossSpawned = true;
    fdSpawnBoss();
  }

  if (remaining <= 0) {
    endFocusDungeon(true);
  }
}

function fdScheduleNextSpawn() {
  if (!_fd || _fd.ended) return;
  const delay = 6000 + Math.random() * 8000;
  _fd.spawnTimeoutId = setTimeout(() => {
    if (!_fd || _fd.ended) return;
    fdSpawnEnemy();
    fdScheduleNextSpawn();
  }, delay);
}

function fdSpawnEnemy() {
  const arena = document.getElementById('fdArena');
  if (!arena) return;
  const enemy = document.createElement('div');
  enemy.className = 'fd-enemy';
  enemy.textContent = ENEMY_EMOJI[Math.floor(Math.random() * ENEMY_EMOJI.length)];
  const side = Math.floor(Math.random() * 4);
  const rect = arena.getBoundingClientRect();
  let x, y;
  const pad = 30;
  if (side === 0) { x = Math.random() * (rect.width - pad * 2) + pad; y = pad; }
  else if (side === 1) { x = rect.width - pad; y = Math.random() * (rect.height - pad * 2) + pad; }
  else if (side === 2) { x = Math.random() * (rect.width - pad * 2) + pad; y = rect.height - pad; }
  else { x = pad; y = Math.random() * (rect.height - pad * 2) + pad; }
  enemy.style.left = x + 'px';
  enemy.style.top  = y + 'px';
  const xp = 1 + Math.floor(Math.random() * 3);
  enemy.dataset.xp = xp;
  enemy.onclick = (e) => { e.stopPropagation(); fdKillEnemy(enemy, xp); };
  arena.appendChild(enemy);
  const despawnId = setTimeout(() => {
    if (enemy.parentNode) {
      enemy.classList.add('fd-enemy-flee');
      setTimeout(() => enemy.remove(), 400);
    }
  }, 8000);
  enemy.dataset.despawnId = despawnId;
}

function fdSpawnBoss() {
  const arena = document.getElementById('fdArena');
  if (!arena) return;
  const boss = document.createElement('div');
  boss.className = 'fd-enemy fd-boss';
  boss.textContent = BOSS_EMOJI[Math.floor(Math.random() * BOSS_EMOJI.length)];
  const rect = arena.getBoundingClientRect();
  boss.style.left = (rect.width / 2) + 'px';
  boss.style.top  = '60px';
  boss.dataset.hp = '5';
  boss.dataset.xp = '50';
  boss.onclick = (e) => {
    e.stopPropagation();
    boss.dataset.hp = String(parseInt(boss.dataset.hp, 10) - 1);
    boss.classList.remove('fd-boss-hit');
    void boss.offsetWidth;
    boss.classList.add('fd-boss-hit');
    if (typeof playSound === 'function') playSound('xp');
    if (parseInt(boss.dataset.hp, 10) <= 0) {
      fdKillEnemy(boss, 50);
    }
  };
  arena.appendChild(boss);
  showPopup('👹 BOSS APPROACHES!', 2000);
  if (typeof playSound === 'function') playSound('challenge');
}

function fdKillEnemy(enemy, xp) {
  if (!_fd || _fd.ended) return;
  _fd.kills++;
  _fd.xp += xp;
  document.getElementById('fdKills').textContent = _fd.kills;
  document.getElementById('fdXp').textContent = _fd.xp;
  const rect = enemy.getBoundingClientRect();
  const poof = document.createElement('div');
  poof.className = 'fd-poof';
  poof.textContent = `+${xp}`;
  poof.style.left = rect.left + rect.width / 2 + 'px';
  poof.style.top  = rect.top + 'px';
  document.body.appendChild(poof);
  setTimeout(() => poof.remove(), 900);
  if (typeof playSound === 'function') playSound('xp');
  enemy.classList.add('fd-enemy-killed');
  clearTimeout(parseInt(enemy.dataset.despawnId, 10));
  setTimeout(() => enemy.remove(), 300);
}

function quitFocusDungeon() {
  if (!_fd) return;
  if (confirm('End this focus run early? You\'ll get partial XP.')) {
    endFocusDungeon(false);
  }
}

function endFocusDungeon(completed) {
  if (!_fd || _fd.ended) return;
  _fd.ended = true;
  clearInterval(_fd.tickerId);
  clearTimeout(_fd.spawnTimeoutId);
  if (_fd.visListener) document.removeEventListener('visibilitychange', _fd.visListener);

  const minutes = _fd.totalMinutes;
  const elapsed = (Date.now() - _fd.startTime) / 60000;
  let baseXp = 0;
  if (completed && !_fd.distractionPenalty) {
    baseXp = minutes * 20;
  } else if (completed && _fd.distractionPenalty) {
    baseXp = Math.floor(minutes * 10);
  } else {
    baseXp = Math.floor(elapsed * 8);
  }
  const killXp = _fd.xp;
  const totalXp = baseXp + killXp;
  const coinBonus = completed && !_fd.distractionPenalty ? minutes * 5 : 0;

  const screen = document.getElementById('focusDungeonScreen');
  screen.style.opacity = '0';
  screen.style.pointerEvents = 'none';
  screen.classList.remove('active');
  const arena = document.getElementById('fdArena');
  if (arena) arena.querySelectorAll('.fd-enemy').forEach(e => e.remove());

  if (totalXp > 0) {
    awardXP(totalXp);
    state.sparqCoins = (state.sparqCoins || 0) + coinBonus;
  }

  if (completed && !_fd.distractionPenalty) {
    state.focusSessions = (state.focusSessions || 0) + 1;
    state.focusMinutesTotal = (state.focusMinutesTotal || 0) + minutes;
  } else if (!completed || _fd.distractionPenalty) {
    state.lastFocusFail = Date.now();
  }
  if (typeof checkAchievements === 'function') checkAchievements();
  saveState();

  let title, msg;
  if (completed && !_fd.distractionPenalty) {
    title = '🏆 DUNGEON CLEARED!';
    msg = `+${totalXp} XP · +${coinBonus} 🪙 · ${_fd.kills} kills`;
    if (typeof playSound === 'function') playSound('victory');
    launchConfetti();
  } else if (completed && _fd.distractionPenalty) {
    title = '💀 Distracted...';
    msg = `+${totalXp} XP (half reward — you left the app ${_fd.warningCount}x)`;
    if (typeof playSound === 'function') playSound('error');
  } else {
    title = '🚪 Left the dungeon';
    msg = `+${totalXp} XP (partial, ${Math.round(elapsed)} min)`;
    if (typeof playSound === 'function') playSound('close');
  }
  showPopup(`${title}\n${msg}`, 4000);

  const mini = document.getElementById('fdStreakMini');
  if (mini) mini.textContent = state.focusSessions || 0;

  _fd = null;
}

function updateFocusMini() {
  const mini = document.getElementById('fdStreakMini');
  if (mini) mini.textContent = state.focusSessions || 0;
}

// ─── Init ─────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
  loadState();
  initStars();
  initDate();
  initBlossoms();
  initJournalDate();
  updateStreak();
  updateUI();
  renderSavedTasks();
  renderCustomReminders();
  renderAdventures();
  applyEquippedItems();
  applyActivePet();
  updateSoundToggleLabel();
  updatePronounLabel();
  applyReducedMotion();
  updateMotionToggleLabel();
  updateFocusMini();
  // Apply saved ND profile
  if (state.ndProfile) {
    const tag = document.querySelector('.profile-tag');
    if (tag) tag.textContent = `Neurodivergent & Proud ✦ ${state.ndProfile}`;
  }
  // Egg #5: Night Owl — open app between 3am and 4am
  const h = new Date().getHours();
  if (h === 3) triggerEgg('egg_night_owl', '🦉 NIGHT OWL — what are you doing up?');

  // Show Una onboarding on first launch (only if auth screen isn't blocking)
  setTimeout(() => {
    const authScreen = document.getElementById('authScreen');
    const authVisible = authScreen && authScreen.offsetHeight > 0 && authScreen.style.display !== 'none';
    if (!state.onboardingComplete && !authVisible) {
      startOnboarding();
    }
  }, 800);
});

// ─── Una onboarding ─────────────────────────────────────────────────────────
const ONBOARDING_STEPS = [
  {
    text: "Hi! I'm Una, your Sparq guide 👋 I'll show you around in 60 seconds — skip anytime!",
    highlight: null,
  },
  {
    text: "Meet Karu! 🐼 Your red panda companion. Tap them for love. Karu levels up as YOU level up.",
    highlight: '.pet-avatar',
  },
  {
    text: "Complete daily quests to earn XP ⚡ Your XP bar fills up here. Every 100 XP = 1 level.",
    highlight: '.xp-bar-wrap',
  },
  {
    text: "Meet your rival, Volt ⚔️ The electric wolf gains XP over time. Stay ahead to unlock achievements!",
    highlight: '#rivalCard',
  },
  {
    text: "Adventures 🗺️ are multi-step quest chains with BIG XP rewards. Scroll to see them all.",
    highlight: '.adventures-row',
  },
  {
    text: "Tap the '⚔️ New Quest' button to add your own tasks. Walking the dog? Calling mom? Your call.",
    highlight: '#fab',
  },
  {
    text: "Use the bottom nav to jump between: Home, Journal 📓, Reminders 🔔, Community 🌈, and Profile 👤",
    highlight: '.bottom-nav',
  },
  {
    text: "Pro tip: Visit Profile → Sparq Shop 🛒 to spend Sparq Coins 🪙 on outfits for Karu!",
    highlight: null,
  },
  {
    text: "And hey... there might be some hidden Easter eggs around 🥚 Keep exploring! See you around ⚡",
    highlight: null,
  },
];

let _unaStep = 0;

function startOnboarding() {
  _unaStep = 0;
  const overlay = document.getElementById('unaOverlay');
  if (!overlay) return;
  overlay.style.opacity = '1';
  overlay.style.pointerEvents = 'all';
  overlay.classList.add('show');
  // Make sure home page is shown
  if (typeof switchPage === 'function') switchPage('home');
  renderUnaStep();
}

function renderUnaStep() {
  const step = ONBOARDING_STEPS[_unaStep];
  if (!step) return;
  document.getElementById('unaText').textContent = step.text;
  const dotsEl = document.getElementById('unaDots');
  dotsEl.innerHTML = ONBOARDING_STEPS.map((_, i) =>
    `<span class="una-dot ${i === _unaStep ? 'active' : ''}${i < _unaStep ? ' passed' : ''}"></span>`
  ).join('');
  document.getElementById('unaNextBtn').textContent =
    _unaStep === ONBOARDING_STEPS.length - 1 ? "Let's go! ⚡" : 'Next →';
  const backBtn = document.getElementById('unaBackBtn');
  if (backBtn) backBtn.style.visibility = _unaStep > 0 ? 'visible' : 'hidden';
  const spotlight = document.getElementById('unaSpotlight');
  const backdrop = document.querySelector('.una-backdrop');
  if (step.highlight) {
    const el = document.querySelector(step.highlight);
    if (el) {
      // Punch-through mode: spotlight stays visible, just slides to new position
      if (backdrop) backdrop.classList.remove('no-highlight');
      spotlight.style.display = 'block';
      spotlight.style.opacity = '1';
      el.scrollIntoView({ block: 'center', behavior: 'smooth' });
      // Position once after scroll settles. The CSS transition handles smooth movement.
      setTimeout(() => {
        const r = el.getBoundingClientRect();
        spotlight.style.top = (r.top - 8) + 'px';
        spotlight.style.left = (r.left - 8) + 'px';
        spotlight.style.width = (r.width + 16) + 'px';
        spotlight.style.height = (r.height + 16) + 'px';
      }, 280);
    } else {
      // Target not found — use backdrop dim, fade spotlight out
      spotlight.style.opacity = '0';
      if (backdrop) backdrop.classList.add('no-highlight');
    }
  } else {
    // No highlight — shrink spotlight out of view, use backdrop dim
    spotlight.style.opacity = '0';
    if (backdrop) backdrop.classList.add('no-highlight');
  }
}

function advanceOnboarding() {
  if (typeof playSound === 'function') playSound('tap');
  _unaStep++;
  if (_unaStep >= ONBOARDING_STEPS.length) {
    completeOnboarding();
  } else {
    renderUnaStep();
  }
}

function rewindOnboarding() {
  if (typeof playSound === 'function') playSound('tap');
  if (_unaStep > 0) {
    _unaStep--;
    renderUnaStep();
  }
}

function skipOnboarding() {
  const ok = confirm(
    "Skip Una's tutorial?\n\n" +
    "You can always come back — just tap Profile → 🐉 Meet Una Again and she'll pick up right here!"
  );
  if (!ok) return;
  completeOnboarding(true);
}

function completeOnboarding(skipped = false) {
  state.onboardingComplete = true;
  saveState();
  const overlay = document.getElementById('unaOverlay');
  overlay.style.opacity = '0';
  overlay.style.pointerEvents = 'none';
  overlay.classList.remove('show');
  if (skipped) {
    showPopup("Una's waiting for you in Profile! 🐉");
  } else {
    showPopup("🎉 You're all set! Go conquer today.");
    if (typeof launchConfetti === 'function') launchConfetti();
  }
}

// Replay onboarding from profile
function replayOnboarding() { startOnboarding(); }
