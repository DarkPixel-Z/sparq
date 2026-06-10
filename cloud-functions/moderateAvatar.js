/**
 * moderateAvatar.js — Firebase Cloud Function for Path B avatar moderation.
 *
 * TRIGGER:
 *   Cloud Storage object finalize event on objects matching:
 *     avatars/{userId}/pending/{uuid}.png|jpg
 *
 * FLOW:
 *   1. Pull the just-uploaded image
 *   2. Run Google Cloud Vision SafeSearch (adult, violence, racy, medical, spoof)
 *   3. Score the result against thresholds. Three outcomes:
 *      - CLEAN     → copy to avatars/{userId}/approved/{uuid}.png, return public URL,
 *                    write Firestore doc users/{userId}/avatarModeration { state:"approved", url, decidedAt }
 *      - FLAGGED   → leave in pending/, write Firestore doc { state:"pending_review", flaggedReasons }
 *                    so the human moderator queue picks it up
 *      - REJECTED  → delete the upload, write doc { state:"rejected", reason }
 *   4. Optionally delete original pending/ object after copy (only on CLEAN path)
 *
 * COST:
 *   Cloud Vision: 1,000 free units/month, then $1.50 per 1,000.
 *   At 10k DAU and 5% avatar-change rate, that's ~500 calls/month — free tier.
 *
 * DEPLOY:
 *   1. cd cloud-functions/
 *   2. npm install firebase-functions firebase-admin @google-cloud/vision
 *   3. firebase deploy --only functions:moderateAvatar
 *   4. Enable Vision API in GCP console for the Firebase project
 *
 * SECURITY:
 *   - Storage rules MUST block direct reads of avatars/{userId}/pending/* by other users
 *   - Only the owning user + Cloud Function service account can read pending objects
 *   - avatars/{userId}/approved/* is publicly readable (that's the whole point)
 *
 * Companion files:
 *   - storage.rules            — Firebase Storage security rules (see below)
 *   - firestore.rules          — Firestore security rules
 *   - AvatarModerationListener.cs — Unity-side listener that updates PlayerData
 *                                   when the Firestore doc state changes
 */

const functions = require('firebase-functions');
const admin     = require('firebase-admin');
const vision    = require('@google-cloud/vision');

admin.initializeApp();
const storage = admin.storage();
const db      = admin.firestore();
const visionClient = new vision.ImageAnnotatorClient();

// ── Thresholds — tune after watching the first 100 reviews ─────────────────
// SafeSearch levels: UNKNOWN | VERY_UNLIKELY | UNLIKELY | POSSIBLE | LIKELY | VERY_LIKELY
const REJECT_AT = new Set(['LIKELY', 'VERY_LIKELY']);
const REVIEW_AT = new Set(['POSSIBLE']);

exports.moderateAvatar = functions
  .region('us-central1')
  .storage.object()
  .onFinalize(async (object) => {
    const filePath = object.name;
    if (!filePath || !filePath.startsWith('avatars/') || !filePath.includes('/pending/')) {
      return; // ignore non-avatar uploads
    }

    // Extract userId from path: avatars/{userId}/pending/{uuid}.png
    const parts = filePath.split('/');
    if (parts.length < 4) {
      console.warn('moderateAvatar: unexpected path', filePath);
      return;
    }
    const userId = parts[1];
    const uuid   = parts[3].replace(/\.(png|jpg|jpeg)$/i, '');

    const bucket = storage.bucket(object.bucket);
    const moderationRef = db.collection('users').doc(userId)
                           .collection('avatarModeration').doc('current');

    try {
      // 1) Run SafeSearch
      const gsUri = `gs://${object.bucket}/${filePath}`;
      const [result] = await visionClient.safeSearchDetection(gsUri);
      const ss = result.safeSearchAnnotation || {};

      const flagged = [];
      const reviewWorthy = [];
      ['adult', 'violence', 'racy', 'medical', 'spoof'].forEach(category => {
        const level = ss[category];
        if (REJECT_AT.has(level)) flagged.push(category);
        else if (REVIEW_AT.has(level)) reviewWorthy.push(category);
      });

      // 2) Outcome
      if (flagged.length > 0) {
        // REJECTED — delete upload, write rejection
        await bucket.file(filePath).delete().catch(() => {});
        await moderationRef.set({
          state: 'rejected',
          reason: `Auto-rejected: ${flagged.join(', ')}`,
          decidedAt: admin.firestore.FieldValue.serverTimestamp(),
        }, { merge: true });
        console.log(`moderateAvatar: rejected ${userId} (${flagged.join(',')})`);
        return;
      }

      if (reviewWorthy.length > 0) {
        // FLAGGED — leave in pending/, queue for human review
        await moderationRef.set({
          state: 'pending_review',
          flaggedReasons: reviewWorthy,
          pendingPath: filePath,
          createdAt: admin.firestore.FieldValue.serverTimestamp(),
        }, { merge: true });
        console.log(`moderateAvatar: human review for ${userId} (${reviewWorthy.join(',')})`);
        return;
      }

      // CLEAN — copy to approved/, write Firestore
      const approvedPath = `avatars/${userId}/approved/${uuid}.png`;
      await bucket.file(filePath).copy(bucket.file(approvedPath));
      await bucket.file(approvedPath).makePublic();
      const publicUrl = `https://storage.googleapis.com/${object.bucket}/${approvedPath}`;
      await bucket.file(filePath).delete().catch(() => {});

      await moderationRef.set({
        state: 'approved',
        url: publicUrl,
        approvedPath,
        decidedAt: admin.firestore.FieldValue.serverTimestamp(),
      }, { merge: true });

      // Also update user's denormalized avatarUrl field so other clients
      // listing leaderboards / friends don't need a per-user subcollection read.
      await db.collection('users').doc(userId).set({
        avatarUrl: publicUrl,
      }, { merge: true });

      console.log(`moderateAvatar: approved ${userId}`);
    } catch (e) {
      console.error(`moderateAvatar error for ${userId}:`, e);
      await moderationRef.set({
        state: 'error',
        error: e.message || String(e),
        erroredAt: admin.firestore.FieldValue.serverTimestamp(),
      }, { merge: true });
    }
  });

/**
 * HTTP callable for the human moderator queue.
 * Admin-only (requires custom claim moderator=true on the caller's auth token).
 *
 * Call shape:
 *   { userId: "abc123", decision: "approve" | "reject", reason?: "..." }
 */
exports.resolveAvatarReview = functions
  .region('us-central1')
  .https.onCall(async (data, context) => {
    if (!context.auth || !context.auth.token.moderator) {
      throw new functions.https.HttpsError('permission-denied', 'Moderator role required.');
    }
    const { userId, decision, reason } = data || {};
    if (!userId || !['approve', 'reject'].includes(decision)) {
      throw new functions.https.HttpsError('invalid-argument', 'userId + decision required.');
    }
    const moderationRef = db.collection('users').doc(userId)
                           .collection('avatarModeration').doc('current');
    const snap = await moderationRef.get();
    if (!snap.exists) throw new functions.https.HttpsError('not-found', 'No pending review.');
    const doc = snap.data();
    if (doc.state !== 'pending_review') {
      throw new functions.https.HttpsError('failed-precondition', `Not in pending_review (state=${doc.state}).`);
    }

    const bucket = storage.bucket();   // default bucket
    if (decision === 'approve') {
      const uuid = (doc.pendingPath || '').split('/').pop().replace(/\.(png|jpg|jpeg)$/i, '');
      const approvedPath = `avatars/${userId}/approved/${uuid}.png`;
      await bucket.file(doc.pendingPath).copy(bucket.file(approvedPath));
      await bucket.file(approvedPath).makePublic();
      await bucket.file(doc.pendingPath).delete().catch(() => {});
      const publicUrl = `https://storage.googleapis.com/${bucket.name}/${approvedPath}`;
      await moderationRef.set({
        state: 'approved',
        url: publicUrl,
        approvedPath,
        decidedBy: context.auth.uid,
        decidedAt: admin.firestore.FieldValue.serverTimestamp(),
      }, { merge: true });
      await db.collection('users').doc(userId).set({ avatarUrl: publicUrl }, { merge: true });
      return { ok: true, state: 'approved' };
    } else {
      if (doc.pendingPath) await bucket.file(doc.pendingPath).delete().catch(() => {});
      await moderationRef.set({
        state: 'rejected',
        reason: reason || 'Reviewed and declined.',
        decidedBy: context.auth.uid,
        decidedAt: admin.firestore.FieldValue.serverTimestamp(),
      }, { merge: true });
      return { ok: true, state: 'rejected' };
    }
  });
