// ─── State ────────────────────────────────────────────────────────────────────
const state = {
  currentXP: 55,
  totalXP: 55,
  streak: 0,
  completedToday: 0,
  currentPage: 'home',
  petTaps: 0,
  fitchXP: 72,
  journalMood: '😄',
  journalEntries: [],
};

// ─── Stars ────────────────────────────────────────────────────────────────────
function initStars() {
  const container = document.getElementById('stars');
  for (let i = 0; i < 55; i++) {
    const s = document.createElement('div');
    s.className = 'star';
    const size = Math.random() * 2.5 + 1;
    s.style.cssText = `width:${size}px;height:${size}px;top:${Math.random()*100}%;left:${Math.random()*100}%;--dur:${2+Math.random()*3}s;--delay:${-Math.random()*4}s`;
    container.appendChild(s);
  }
}

// ─── Cherry blossoms (MapleStory/anime feel) ──────────────────────────────────
function spawnBlossom() {
  const petals = ['🌸', '🌺', '✿', '❀'];
  const b = document.createElement('div');
  b.className = 'blossom';
  const drift = (Math.random() * 120 - 60) + 'px';
  b.style.cssText = `
    left: ${Math.random() * 100}%;
    top: -30px;
    --dur: ${6 + Math.random() * 6}s;
    --delay: 0s;
    --drift: ${drift};
  `;
  b.textContent = petals[Math.floor(Math.random() * petals.length)];
  document.body.appendChild(b);
  setTimeout(() => b.remove(), 13000);
}

function initBlossoms() {
  // spawn a blossom every few seconds
  spawnBlossom();
  setInterval(spawnBlossom, 3500);
}

// ─── Date pill ────────────────────────────────────────────────────────────────
function initDate() {
  const days   = ['SUNDAY','MONDAY','TUESDAY','WEDNESDAY','THURSDAY','FRIDAY','SATURDAY'];
  const months = ['JANUARY','FEBRUARY','MARCH','APRIL','MAY','JUNE','JULY','AUGUST','SEPTEMBER','OCTOBER','NOVEMBER','DECEMBER'];
  const now    = new Date();
  const text   = `● ${days[now.getDay()]} · ${months[now.getMonth()]} ${now.getDate()}`;
  document.getElementById('datePill').textContent = text;
  document.getElementById('journalDatePill').textContent = text;
}

// ─── Navigation ───────────────────────────────────────────────────────────────
function switchPage(page) {
  document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
  document.querySelectorAll('.nav-btn').forEach(b => b.classList.remove('active'));
  document.getElementById('page-' + page).classList.add('active');
  document.getElementById('nav-' + page).classList.add('active');
  document.getElementById('fab').className = (page === 'home') ? 'fab' : 'fab fab-hidden';
  state.currentPage = page;
}

// ─── XP popup ─────────────────────────────────────────────────────────────────
function showPopup(message, duration = 1200) {
  const popup = document.getElementById('xpPopup');
  popup.textContent = message;
  popup.classList.add('show');
  setTimeout(() => popup.classList.remove('show'), duration);
}

// ─── Confetti ─────────────────────────────────────────────────────────────────
function launchConfetti() {
  const colors = ['#FF2D2D','#FF7849','#FFD700','#00D4AA','#FF9BC5','#A87EFF'];
  for (let i = 0; i < 14; i++) {
    const c = document.createElement('div');
    c.className = 'confetti-piece';
    c.style.cssText = `
      left: ${38 + Math.random()*24}%;
      top: 38%;
      background: ${colors[Math.floor(Math.random()*colors.length)]};
      transform: rotate(${Math.random()*360}deg);
      animation-delay: ${Math.random()*0.3}s;
    `;
    document.body.appendChild(c);
    setTimeout(() => c.remove(), 1600);
  }
}

// ─── Update UI ────────────────────────────────────────────────────────────────
function updateUI() {
  document.getElementById('xpBar').style.width = state.currentXP + '%';
  document.getElementById('currentXP').textContent = state.currentXP;
  document.getElementById('totalXP').textContent = state.totalXP;
  document.getElementById('profileXP').textContent = state.totalXP;
  document.getElementById('streakCount').textContent = state.streak;
  document.getElementById('streakDays').textContent = state.streak;

  const remaining = document.querySelectorAll('.task-item:not(.done)').length;
  document.getElementById('taskCount').textContent = remaining;

  updateRivalUI();
}

// ─── Rival / Fitch system ─────────────────────────────────────────────────────
function updateRivalUI() {
  const gap = state.fitchXP - state.totalXP;
  const rivalGap = document.getElementById('rivalGap');
  const rivalDelta = document.getElementById('rivalDelta');
  const fitchXPEl = document.getElementById('fitchXP');
  const fitchXPProfile = document.getElementById('fitchXPProfile');

  fitchXPEl.textContent = state.fitchXP;
  fitchXPProfile.textContent = state.fitchXP;

  if (gap > 0) {
    rivalGap.textContent = `You're ${gap} XP behind — catch up! 😤`;
    rivalGap.style.color = 'var(--cherry)';
    rivalDelta.textContent = `-${gap} XP`;
    rivalDelta.style.color = 'var(--maple-red)';
  } else if (gap < 0) {
    rivalGap.textContent = `You're ${Math.abs(gap)} XP AHEAD of Fitch! 🔥`;
    rivalGap.style.color = 'var(--teal)';
    rivalDelta.textContent = `+${Math.abs(gap)} XP`;
    rivalDelta.style.color = 'var(--teal)';
  } else {
    rivalGap.textContent = `Dead even with Fitch! Push harder! ⚡`;
    rivalGap.style.color = 'var(--yellow)';
    rivalDelta.textContent = `TIED`;
    rivalDelta.style.color = 'var(--yellow)';
  }
}

function challengeFitch() {
  const gap = state.fitchXP - state.totalXP;
  if (gap > 0) {
    showPopup(`🗡️ Challenge accepted! ${gap} XP to go!`, 1400);
  } else {
    showPopup('🏆 You\'re BEATING Fitch!', 1400);
  }
  launchConfetti();
}

// ─── Complete task ────────────────────────────────────────────────────────────
function completeTask(el, xpAmount) {
  if (el.classList.contains('done')) return;

  el.classList.add('done');
  el.querySelector('.task-check').textContent = '✓';
  el.querySelector('.task-xp').textContent = '✓ Done';

  state.currentXP = Math.min(100, state.currentXP + xpAmount);
  state.totalXP += xpAmount;
  state.completedToday++;

  if (state.completedToday === 1) state.streak = 1;

  updateUI();
  showPopup(`⚡ +${xpAmount} XP!`);
  launchConfetti();

  // Pet bounce
  const pet = document.querySelector('.pet-avatar');
  pet.style.transform = 'scale(1.25) rotate(8deg)';
  setTimeout(() => (pet.style.transform = ''), 400);
}

// ─── Pet tap ──────────────────────────────────────────────────────────────────
function petTap() {
  state.petTaps++;
  const messages = [
    'Karu loves you! 💕','So happy! 🌸','*happy red panda noises* 🐾',
    'Give me more quests! ⚡','Best human ever! 🥰','Karu wants a nap... 😴',
  ];
  showPopup(messages[state.petTaps % messages.length], 1000);
  spawnBlossom();
  spawnBlossom();
}

// ─── Streak tap ───────────────────────────────────────────────────────────────
function streakTap() {
  showPopup('🔥 Keep the flame alive!', 1000);
}

// ─── Mood select ──────────────────────────────────────────────────────────────
function selectMood(btn) {
  document.querySelectorAll('.mood-btn').forEach(b => b.classList.remove('selected'));
  btn.classList.add('selected');
  const moods = {
    awful: 'Hang in there 💙',
    meh:   "That's valid 🤍",
    ok:    'You got this 🌿',
    good:  'Love that! 🌸',
    great: "You're on FIRE! 🔥",
  };
  showPopup(moods[btn.dataset.mood]);
}

// ─── Toggle reminder ──────────────────────────────────────────────────────────
function toggleReminder(el) {
  el.classList.toggle('off');
}

// ─── Like post ────────────────────────────────────────────────────────────────
function likePost(btn) {
  btn.classList.toggle('liked');
  const countEl = btn.querySelector('span');
  let count = parseInt(countEl.textContent);
  countEl.textContent = btn.classList.contains('liked') ? count + 1 : count - 1;
}

// ─── Add task ─────────────────────────────────────────────────────────────────
function showAddTask() {
  const name = prompt('Name your quest 📜');
  if (!name || !name.trim()) return;

  const xp = Math.floor(Math.random() * 3 + 1) * 10;
  const list = document.getElementById('taskList');
  const item = document.createElement('div');
  item.className = 'task-item';
  item.innerHTML = `
    <div class="task-check"></div>
    <div class="task-content">
      <div class="task-title">${escapeHtml(name.trim())}</div>
      <div class="task-meta">✦ Custom quest</div>
    </div>
    <div class="task-xp">+${xp} XP</div>
  `;
  item.onclick = function () { completeTask(item, xp); };
  list.appendChild(item);
  updateUI();
}

// ─── Journal ──────────────────────────────────────────────────────────────────
function setJournalMood(btn) {
  document.querySelectorAll('.journal-mood-btn').forEach(b => b.classList.remove('selected'));
  btn.classList.add('selected');
  state.journalMood = btn.dataset.emoji;
}

document.addEventListener('input', (e) => {
  if (e.target.id === 'journalEntry') {
    const len = e.target.value.length;
    document.getElementById('wordCount').textContent = `${len} / 1000`;
  }
});

function saveJournalEntry() {
  const textarea = document.getElementById('journalEntry');
  const text = textarea.value.trim();
  if (!text) {
    showPopup('Write something first ✍️', 1000);
    return;
  }

  // Award XP
  state.currentXP = Math.min(100, state.currentXP + 10);
  state.totalXP += 10;
  updateUI();
  showPopup('📓 +10 XP! Entry saved!');
  launchConfetti();

  // Prepend to journal list
  const entry = {
    mood: state.journalMood,
    text: text,
    date: 'Just now',
    xp: 10,
  };
  state.journalEntries.unshift(entry);
  renderJournalEntry(entry, true);

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
      <span class="journal-entry-xp">+${entry.xp} XP</span>
    </div>
    <p class="journal-entry-text">${escapeHtml(entry.text)}</p>
  `;
  if (prepend && list.firstChild) {
    list.insertBefore(card, list.firstChild);
  } else {
    list.appendChild(card);
  }
}

// ─── XSS protection ───────────────────────────────────────────────────────────
function escapeHtml(str) {
  const map = { '&':'&amp;', '<':'&lt;', '>':'&gt;', '"':'&quot;', "'":'&#039;' };
  return str.replace(/[&<>"']/g, m => map[m]);
}

// ─── Init ─────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
  initStars();
  initDate();
  initBlossoms();
  updateRivalUI();
});
