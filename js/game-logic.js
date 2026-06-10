// js/game-logic.js
// Pure game logic functions - no DOM, no side effects - testable

export function xpForLevel(lvl) { return lvl * 100; }

export function calculateLevelUp(state, xpGained) {
  // Returns { newState, levelsGained }
  const newState = { ...state };
  newState.currentXP += xpGained;
  newState.totalXP += xpGained;
  newState.fitchXP += Math.floor(xpGained * 0.4);
  let levels = 0;
  while (newState.currentXP >= newState.xpToNextLevel) {
    newState.currentXP -= newState.xpToNextLevel;
    newState.level += 1;
    newState.xpToNextLevel = xpForLevel(newState.level);
    levels++;
  }
  return { newState, levelsGained: levels };
}

export function calculateStreakUpdate(lastActiveDate, todayStr, yesterdayStr, currentStreak) {
  // Returns new streak value and new lastActiveDate
  if (lastActiveDate === todayStr) return { streak: currentStreak, lastActiveDate: todayStr };
  if (lastActiveDate === yesterdayStr) return { streak: currentStreak + 1, lastActiveDate: todayStr };
  return { streak: 1, lastActiveDate: todayStr };
}

export function calculateAge(dobString, todayDate = new Date()) {
  const dob = new Date(dobString);
  let age = todayDate.getFullYear() - dob.getFullYear();
  const monthDiff = todayDate.getMonth() - dob.getMonth();
  if (monthDiff < 0 || (monthDiff === 0 && todayDate.getDate() < dob.getDate())) {
    age--;
  }
  return age;
}

export function isEligibleAge(dobString, todayDate = new Date()) {
  return calculateAge(dobString, todayDate) >= 13;
}

export function checkAchievementsToUnlock(state, achievementDefs) {
  const toUnlock = [];
  const unlocked = new Set(state.unlockedAchievements || []);
  const conditions = {
    'first_quest':    state.totalTasksDone >= 1,
    'streak_3':       state.streak >= 3,
    'streak_7':       state.streak >= 7,
    'journal_first':  state.journalCount >= 1,
    'beat_fitch':     state.totalXP > state.fitchXP,
    'level_5':        state.level >= 5,
    'level_10':       state.level >= 10,
  };
  for (const ach of achievementDefs) {
    if (conditions[ach.id] && !unlocked.has(ach.id)) toUnlock.push(ach);
  }
  return toUnlock;
}

export function containsBlockedContent(text, blockedWords) {
  const lower = text.toLowerCase();
  return blockedWords.some(w => lower.includes(w));
}
