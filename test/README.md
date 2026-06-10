# Sparq Tests

Run with:
```
npm install
npm test
```

Test framework: [Vitest](https://vitest.dev)

## Coverage

- XP and leveling logic (`xpForLevel`, `calculateLevelUp`)
- Streak tracking (`calculateStreakUpdate`)
- Age validation for COPPA compliance (`calculateAge`, `isEligibleAge`)
- Achievement unlock conditions (`checkAchievementsToUnlock`)
- Content filter (`containsBlockedContent`)

## Not covered (needs integration/E2E)

- Firebase Auth flows
- Firestore sync
- UI rendering
- Community feed
