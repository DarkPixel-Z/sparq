import { describe, it, expect } from 'vitest';
import {
  xpForLevel, calculateLevelUp, calculateStreakUpdate,
  calculateAge, isEligibleAge, checkAchievementsToUnlock,
  containsBlockedContent
} from '../js/game-logic.js';

describe('xpForLevel', () => {
  it('level 1 requires 100 XP', () => expect(xpForLevel(1)).toBe(100));
  it('level 5 requires 500 XP', () => expect(xpForLevel(5)).toBe(500));
  it('level 10 requires 1000 XP', () => expect(xpForLevel(10)).toBe(1000));
});

describe('calculateLevelUp', () => {
  const baseState = { currentXP: 0, totalXP: 0, level: 1, xpToNextLevel: 100, fitchXP: 0 };

  it('no level up if XP insufficient', () => {
    const { newState, levelsGained } = calculateLevelUp(baseState, 50);
    expect(levelsGained).toBe(0);
    expect(newState.level).toBe(1);
    expect(newState.currentXP).toBe(50);
    expect(newState.totalXP).toBe(50);
  });

  it('levels up once at 100 XP', () => {
    const { newState, levelsGained } = calculateLevelUp(baseState, 100);
    expect(levelsGained).toBe(1);
    expect(newState.level).toBe(2);
    expect(newState.currentXP).toBe(0);
    expect(newState.xpToNextLevel).toBe(200);
  });

  it('multi-level jump with massive XP burst', () => {
    const { newState, levelsGained } = calculateLevelUp(baseState, 600);
    // Level 1→2 (100), 2→3 (200), 3→4 (300) consumes 600 total, so gains 3 levels
    expect(levelsGained).toBe(3);
    expect(newState.level).toBe(4);
  });

  it('Fitch gains 40% of player XP', () => {
    const { newState } = calculateLevelUp(baseState, 100);
    expect(newState.fitchXP).toBe(40);
  });

  it('does not mutate original state', () => {
    calculateLevelUp(baseState, 100);
    expect(baseState.level).toBe(1);
    expect(baseState.currentXP).toBe(0);
  });
});

describe('calculateStreakUpdate', () => {
  it('same day: no change', () => {
    const r = calculateStreakUpdate('2026-04-15', '2026-04-15', '2026-04-14', 5);
    expect(r.streak).toBe(5);
  });
  it('consecutive day: +1', () => {
    const r = calculateStreakUpdate('2026-04-14', '2026-04-15', '2026-04-14', 5);
    expect(r.streak).toBe(6);
  });
  it('gap day: reset to 1', () => {
    const r = calculateStreakUpdate('2026-04-10', '2026-04-15', '2026-04-14', 5);
    expect(r.streak).toBe(1);
  });
  it('first ever activity: streak = 1', () => {
    const r = calculateStreakUpdate('', '2026-04-15', '2026-04-14', 0);
    expect(r.streak).toBe(1);
  });
});

describe('calculateAge', () => {
  const today = new Date('2026-04-15');
  it('exact 13th birthday today', () => {
    expect(calculateAge('2013-04-15', today)).toBe(13);
  });
  it('day before 13th birthday = 12', () => {
    expect(calculateAge('2013-04-16', today)).toBe(12);
  });
  it('adult: born 1990', () => {
    expect(calculateAge('1990-06-01', today)).toBe(35);
  });
});

describe('isEligibleAge', () => {
  const today = new Date('2026-04-15');
  it('13+ accepted', () => expect(isEligibleAge('2013-04-15', today)).toBe(true));
  it('12 rejected', () => expect(isEligibleAge('2013-04-16', today)).toBe(false));
  it('adult accepted', () => expect(isEligibleAge('1990-01-01', today)).toBe(true));
});

describe('checkAchievementsToUnlock', () => {
  const achievementDefs = [
    { id: 'first_quest', name: 'First Step' },
    { id: 'streak_3',    name: 'On Fire' },
    { id: 'streak_7',    name: 'Unstoppable' },
    { id: 'beat_fitch',  name: 'Fitch Slayer' },
    { id: 'level_5',     name: 'Rising Star' },
    { id: 'level_10',    name: 'Night Bloom' },
    { id: 'journal_first', name: 'Inner Voice' },
  ];

  it('unlocks first_quest on first task', () => {
    const result = checkAchievementsToUnlock(
      { totalTasksDone: 1, streak: 0, journalCount: 0, totalXP: 0, fitchXP: 50, level: 1, unlockedAchievements: [] },
      achievementDefs
    );
    expect(result.map(a => a.id)).toContain('first_quest');
  });

  it('does not re-unlock already-unlocked achievements', () => {
    const result = checkAchievementsToUnlock(
      { totalTasksDone: 5, streak: 0, journalCount: 0, totalXP: 0, fitchXP: 50, level: 1, unlockedAchievements: ['first_quest'] },
      achievementDefs
    );
    expect(result.map(a => a.id)).not.toContain('first_quest');
  });

  it('unlocks beat_fitch when totalXP exceeds fitchXP', () => {
    const result = checkAchievementsToUnlock(
      { totalTasksDone: 0, streak: 0, journalCount: 0, totalXP: 100, fitchXP: 72, level: 1, unlockedAchievements: [] },
      achievementDefs
    );
    expect(result.map(a => a.id)).toContain('beat_fitch');
  });

  it('multiple achievements unlock at once', () => {
    const result = checkAchievementsToUnlock(
      { totalTasksDone: 1, streak: 7, journalCount: 1, totalXP: 0, fitchXP: 50, level: 10, unlockedAchievements: [] },
      achievementDefs
    );
    const ids = result.map(a => a.id);
    expect(ids).toContain('first_quest');
    expect(ids).toContain('streak_3');
    expect(ids).toContain('streak_7');
    expect(ids).toContain('journal_first');
    expect(ids).toContain('level_5');
    expect(ids).toContain('level_10');
  });
});

describe('containsBlockedContent (content filter)', () => {
  const words = ['address', 'phone number', 'snap', 'dm me', 'meet up', 'how old are you'];
  it('catches exact phrase', () => {
    expect(containsBlockedContent("what's your address?", words)).toBe(true);
  });
  it('case-insensitive', () => {
    expect(containsBlockedContent('DM ME on snap!', words)).toBe(true);
  });
  it('allows safe content', () => {
    expect(containsBlockedContent('I love playing Sparq!', words)).toBe(false);
  });
  it('catches predatory pattern', () => {
    expect(containsBlockedContent('how old are you?', words)).toBe(true);
  });
});
