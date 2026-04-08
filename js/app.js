// ─── State ────────────────────────────────────────────────────────────────────
const state = {
  currentXP: 55,
  totalXP: 55,
  streak: 0,
  completedToday: 0,
  currentPage: 'home',
  petTaps: 0,
};

// ─── Stars ────────────────────────────────────────────────────────────────────
function initStars() {
  const container = document.getElementById('stars');
  for (let i = 0; i < 60; i++) {
    const s = document.createElement('div');
    s.className = 'star';
    const size = Math.random() * 2.5 + 1;
    s.style.cssText = `width:${size}px;height:${size}px;top:${Math.random()*100}%;left:${Math.random()*100}%;--dur:${2+Math.random()*3}s;--delay:${-Math.random()*4}s`;
    container.appendChild(s);
  }
}

// ─── Date pill ────────────────────────────────────────────────────────────────
function initDate() {
  const days = ['SUNDAY','MONDAY','TUESDAY','WEDNESDAY','THURSDAY','FRIDAY','SATURDAY'];
  const months = ['JANUARY','FEBRUARY','MARCH','APRIL','MAY','JUNE','JULY','AUGUST','SEPTEMBER','OCTOBER','NOVEMBER','DECEMBER'];
  const now = new Date();
  document.getElementById('datePill').textContent =
    `● ${days[now.getDay()]} · ${months[now.getMonth()]} ${now.getDate()}`;
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
  const colors = ['#7C5CFC','#FF5FA0','#FFD93D','#00D4AA','#FF7849'];
  for (let i = 0; i < 12; i++) {
    const c = document.createElement('div');
    c.className = 'confetti-piece';
    c.style.cssText = `
      left: ${40 + Math.random()*20}%;
      top: 40%;
      background: ${colors[Math.floor(Math.random()*colors.length)]};
      transform: rotate(${Math.random()*360}deg);
      animation-delay: ${Math.random()*0.3}s;
    `;
    document.body.appendChild(c);
    setTimeout(() => c.remove(), 1500);
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

  if (state.completedToday === 1) {
    state.streak = 1;
  }

  updateUI();
  showPopup(`⚡ +${xpAmount} XP!`);
  launchConfetti();

  // Pet bounce reaction
  const pet = document.querySelector('.pet-avatar');
  pet.style.transform = 'scale(1.2) rotate(5deg)';
  setTimeout(() => (pet.style.transform = ''), 400);
}

// ─── Pet tap ──────────────────────────────────────────────────────────────────
function petTap() {
  state.petTaps++;
  const messages = ['Buddy loves you! 💕','So happy! 🌟','Woof! 🦊','Give me XP! ⚡','Best human ever! 🥰'];
  showPopup(messages[state.petTaps % messages.length], 1000);
}

// ─── Streak tap ───────────────────────────────────────────────────────────────
function streakTap() {
  showPopup('🔥 Keep it up!', 1000);
}

// ─── Mood select ──────────────────────────────────────────────────────────────
function selectMood(btn) {
  document.querySelectorAll('.mood-btn').forEach(b => b.classList.remove('selected'));
  btn.classList.add('selected');
  const moods = {
    awful: 'Hang in there 💙',
    meh:   "That's valid 🤍",
    ok:    'You got this 🌿',
    good:  'Love that! ✨',
    great: "You're on fire! 🔥",
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
  const name = prompt('What\'s your task? 📝');
  if (!name || !name.trim()) return;

  const xp = Math.floor(Math.random() * 3 + 1) * 10;
  const list = document.getElementById('taskList');
  const item = document.createElement('div');
  item.className = 'task-item';
  item.innerHTML = `
    <div class="task-check"></div>
    <div class="task-content">
      <div class="task-title">${escapeHtml(name.trim())}</div>
      <div class="task-meta">✨ Custom task</div>
    </div>
    <div class="task-xp">+${xp} XP</div>
  `;
  item.onclick = function () { completeTask(item, xp); };
  list.appendChild(item);
  updateUI();
}

// ─── Sanitize user input ──────────────────────────────────────────────────────
function escapeHtml(str) {
  const map = { '&':'&amp;', '<':'&lt;', '>':'&gt;', '"':'&quot;', "'":'&#039;' };
  return str.replace(/[&<>"']/g, m => map[m]);
}

// ─── Init ─────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
  initStars();
  initDate();
});
