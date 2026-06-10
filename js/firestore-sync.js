// ─── Firestore sync ──────────────────────────────────────────────────────────
const db = firebase.firestore();

// Debounce timer for sync-on-change
let _syncTimer = null;
const SYNC_DEBOUNCE_MS = 2000;

/** Load state from Firestore for the current user. Falls back to local state if none. */
async function loadStateFromCloud(uid) {
  try {
    const doc = await db.collection('users').doc(uid).get();
    if (doc.exists) {
      const cloud = doc.data();
      // Cloud state takes priority if it's newer (higher totalXP as proxy)
      if ((cloud.totalXP || 0) >= (state.totalXP || 0)) {
        Object.assign(state, cloud);
        saveState(); // Mirror to local
        updateUI();
        console.log('[sync] Loaded state from cloud');
      } else {
        console.log('[sync] Local state is newer, pushing to cloud');
        await syncStateToCloud(uid);
      }
    } else {
      // First time — push local state to cloud
      await syncStateToCloud(uid);
      console.log('[sync] Created cloud document from local state');
    }
  } catch (err) {
    console.warn('[sync] Cloud load failed:', err.message);
  }
}

/** Push current state to Firestore. */
async function syncStateToCloud(uid) {
  if (!uid) return;
  try {
    const payload = { ...state, _updatedAt: firebase.firestore.FieldValue.serverTimestamp() };
    await db.collection('users').doc(uid).set(payload, { merge: true });
  } catch (err) {
    console.warn('[sync] Push failed:', err.message);
  }
}

/** Call this whenever state changes. Debounces to avoid flooding Firestore. */
function scheduleSync() {
  const user = firebase.auth().currentUser;
  if (!user) return;
  clearTimeout(_syncTimer);
  _syncTimer = setTimeout(() => syncStateToCloud(user.uid), SYNC_DEBOUNCE_MS);
}

/** GDPR: delete user data from Firestore and delete the auth account. */
async function deleteAccount() {
  const user = firebase.auth().currentUser;
  if (!user) return;

  const confirmed = confirm(
    'Delete your account?\n\n' +
    'This will permanently erase:\n' +
    '• Your XP, level, and streak\n' +
    '• All journal entries\n' +
    '• All custom quests\n' +
    '• Your Sparq account\n\n' +
    'This cannot be undone.'
  );
  if (!confirmed) return;

  try {
    // Delete Firestore document first
    await db.collection('users').doc(user.uid).delete();
    // Then delete auth account
    await user.delete();
    // Clear local state
    localStorage.removeItem('sparq_v2');
    localStorage.removeItem('sparq_safety_agreed');
    localStorage.removeItem('sparq_blocked');
    localStorage.removeItem('sparq_safety_settings');
    alert('Your account has been deleted. Goodbye! 👋');
    location.reload();
  } catch (err) {
    if (err.code === 'auth/requires-recent-login') {
      alert('For security, please log out and log back in, then try deleting again.');
    } else {
      alert('Delete failed: ' + err.message);
    }
  }
}

/** GDPR: download all user data as JSON. */
function exportUserData() {
  const user = firebase.auth().currentUser;
  if (!user) return;
  const blob = new Blob([JSON.stringify(state, null, 2)], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `sparq-data-${user.uid}-${Date.now()}.json`;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}
