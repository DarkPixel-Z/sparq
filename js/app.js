// ─── Achievements ─────────────────────────────────────────────────────────────
const ACHIEVEMENTS = [
  { id: 'first_quest',   icon: '🌟', name: 'First Step',    desc: 'Complete your first quest',      xp: 20  },
  { id: 'streak_3',      icon: '🔥', name: 'On Fire',       desc: 'Reach a 3-day streak',           xp: 30  },
  { id: 'streak_7',      icon: '💥', name: 'Unstoppable',   desc: 'Reach a 7-day streak',           xp: 75  },
  { id: 'journal_first', icon: '📓', name: 'Inner Voice',   desc: 'Write your first journal entry', xp: 25  },
  { id: 'beat_fitch',    icon: '⚔️', name: 'Fitch Slayer',  desc: 'Surpass Fitch in total XP',      xp: 50  },
  { id: 'level_5',       icon: '⭐', name: 'Rising Star',   desc: 'Reach Level 5',                  xp: 40  },
  { id: 'level_10',      icon: '🌙', name: 'Night Bloom',   desc: 'Reach Level 10',                 xp: 100 },
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
};

const state = { ...DEFAULT_STATE };

// ─── Persistence ──────────────────────────────────────────────────────────────
function saveState() {
  try { localStorage.setItem('sparq_v2', JSON.stringify(state)); } catch (_) {}
}

function loadState() {
  try {
    const saved = localStorage.getItem('sparq_v2');
    if (saved) Object.assign(state, JSON.parse(saved));
  } catch (_) {}
}

// ─── Streak (date-aware) ──────────────────────────────────────────────────────
function updateStreak() {
  const today     = new Date().toDateString();
  const yesterday = new Date(Date.now() - 86_400_000).toDateString();

  if (state.lastActiveDate === today) return; // already counted

  if (state.lastActiveDate === yesterday) {
    state.streak++;
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

  // Fitch slowly gains XP too (adds tension)
  state.fitchXP += Math.floor(amount * 0.4);

  while (state.currentXP >= state.xpToNextLevel) {
    state.currentXP    -= state.xpToNextLevel;
    state.level        += 1;
    state.xpToNextLevel = xpForLevel(state.level);
    showLevelUpModal(state.level);
  }

  checkAchievements();
  updateUI();
  saveState();
}

// ─── Level-up modal ───────────────────────────────────────────────────────────
function showLevelUpModal(lvl) {
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
  };

  if (state.totalTasksDone >= 1)  unlock('first_quest');
  if (state.streak >= 3)          unlock('streak_3');
  if (state.streak >= 7)          unlock('streak_7');
  if (state.journalCount >= 1)    unlock('journal_first');
  if (state.totalXP > state.fitchXP) unlock('beat_fitch');
  if (state.level >= 5)           unlock('level_5');
  if (state.level >= 10)          unlock('level_10');
}

function showAchievementToast(ach) {
  const toast = document.getElementById('achievementToast');
  document.getElementById('achIcon').textContent  = ach.icon;
  document.getElementById('achName').textContent  = ach.name;
  document.getElementById('achDesc').textContent  = ach.desc;
  document.getElementById('achXP').textContent    = `+${ach.xp} XP`;
  toast.style.right = '16px';
  toast.classList.add('show');
  setTimeout(() => { toast.style.right = '-320px'; toast.classList.remove('show'); }, 3500);
}

// ─── Stars ────────────────────────────────────────────────────────────────────
function initStars() {
  const container = document.getElementById('stars');
  for (let i = 0; i < 55; i++) {
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
  setInterval(spawnBlossom, 3500);
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
  document.getElementById('nav-' + page).classList.add('active');
  document.getElementById('fab').className = (page === 'home') ? 'fab' : 'fab fab-hidden';
  if (page === 'community') showCommunityPage();
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
  const gap = state.fitchXP - state.totalXP;
  document.getElementById('fitchXP').textContent        = state.fitchXP;
  document.getElementById('fitchXPProfile').textContent = state.fitchXP;

  const rivalGap   = document.getElementById('rivalGap');
  const rivalDelta = document.getElementById('rivalDelta');

  if (gap > 0) {
    rivalGap.textContent   = `You're ${gap} XP behind — catch up! 😤`;
    rivalGap.style.color   = 'var(--cherry)';
    rivalDelta.textContent = `-${gap} XP`;
    rivalDelta.style.color = 'var(--maple-red)';
  } else if (gap < 0) {
    rivalGap.textContent   = `You're ${Math.abs(gap)} XP AHEAD of Fitch! 🔥`;
    rivalGap.style.color   = 'var(--teal)';
    rivalDelta.textContent = `+${Math.abs(gap)} XP`;
    rivalDelta.style.color = 'var(--teal)';
  } else {
    rivalGap.textContent   = 'Dead even with Fitch! Push harder! ⚡';
    rivalGap.style.color   = 'var(--yellow)';
    rivalDelta.textContent = 'TIED';
    rivalDelta.style.color = 'var(--yellow)';
  }
}

function challengeFitch() {
  const gap = state.fitchXP - state.totalXP;
  showPopup(gap > 0 ? `🗡️ ${gap} XP to go!` : '🏆 You\'re BEATING Fitch!', 1400);
  launchConfetti();
}

// ─── Complete task ────────────────────────────────────────────────────────────
function completeTask(el, xpAmount) {
  if (el.classList.contains('done')) return;
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
  state.petTaps++;
  const messages = [
    'Karu loves you! 💕','So happy! 🌸','*happy red panda noises* 🐾',
    'More quests plz! ⚡','Best human ever! 🥰','Karu wants a nap... 😴',
    'You smell like XP! 🌟',
  ];
  showPopup(messages[state.petTaps % messages.length], 1000);
  spawnBlossom(); spawnBlossom();
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
function showAddTask() {
  const name = prompt('Name your quest 📜');
  if (!name?.trim()) return;
  const xp   = Math.floor(Math.random() * 3 + 1) * 10;
  const list = document.getElementById('taskList');
  const item = document.createElement('div');
  item.className = 'task-item';
  item.innerHTML = `
    <div class="task-check"></div>
    <div class="task-content">
      <div class="task-title">${escapeHtml(name.trim())}</div>
      <div class="task-meta">✦ Custom quest</div>
    </div>
    <div class="task-xp">+${xp} XP</div>`;
  item.onclick = () => completeTask(item, xp);
  list.appendChild(item);
  updateUI();
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

// ─── Init ─────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
  loadState();
  initStars();
  initDate();
  initBlossoms();
  initJournalDate();
  updateStreak();
  updateUI();
});
