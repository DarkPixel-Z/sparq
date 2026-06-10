// ─── Firebase Config ──────────────────────────────────────────────────────────
// Config loaded from js/firebase-config.js (gitignored)
// See js/firebase-config.example.js for the template
firebase.initializeApp(firebaseConfig);
const auth = firebase.auth();

// ─── DOB auto-advance between Month/Day/Year inputs ─────────────────────────
function setupDobAutoAdvance() {
  const month = document.getElementById('dobMonth');
  const day   = document.getElementById('dobDay');
  const year  = document.getElementById('dobYear');
  if (!month || !day || !year) return;
  month.addEventListener('input', () => {
    if (month.value.length >= 2 || parseInt(month.value, 10) > 1) day.focus();
  });
  day.addEventListener('input', () => {
    if (day.value.length >= 2 || parseInt(day.value, 10) > 3) year.focus();
  });
}
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', setupDobAutoAdvance);
} else {
  setupDobAutoAdvance();
}

// ─── Auth state listener ─────────────────────────────────────────────────────
auth.onAuthStateChanged(user => {
  const authScreen = document.getElementById('authScreen');
  if (user) {
    // Smooth fade-out, then remove from flow after transition
    authScreen.classList.add('auth-fading');
    setTimeout(() => { authScreen.style.display = 'none'; }, 260);

    document.querySelector('.bottom-nav').style.display = '';
    document.getElementById('fab').style.display = '';
    updateProfileFromAuth(user);

    // Defer heavy work to idle time — let the transition finish first
    const kickoff = () => {
      if (typeof applyActivePet === 'function') applyActivePet();
      if (typeof loadStateFromCloud === 'function') loadStateFromCloud(user.uid);
    };
    (window.requestIdleCallback || (cb => setTimeout(cb, 300)))(kickoff);

    // First-time onboarding — wait till after transition + heavy work settles
    setTimeout(() => {
      if (typeof state !== 'undefined' && !state.onboardingComplete && typeof startOnboarding === 'function') {
        startOnboarding();
      }
    }, 1800);
  } else {
    authScreen.classList.remove('auth-fading');
    authScreen.style.display = '';
    document.querySelector('.bottom-nav').style.display = 'none';
    document.getElementById('fab').style.display = 'none';
  }
});

// ─── Update profile with auth info ──────────────────────────────────────────
function updateProfileFromAuth(user) {
  const nameEl = document.querySelector('.profile-name');
  if (nameEl && user.displayName) nameEl.textContent = user.displayName;
  else if (nameEl && user.email) nameEl.textContent = user.email.split('@')[0];

  const avatarEl = document.querySelector('.profile-avatar-big img');
  if (avatarEl && user.photoURL) {
    avatarEl.src = user.photoURL;
    avatarEl.style.borderRadius = '50%';
    avatarEl.style.width = '54px';
    avatarEl.style.height = '54px';
    avatarEl.style.objectFit = 'cover';
  }
}

// ─── Tab switching ───────────────────────────────────────────────────────────
function switchAuthTab(tab) {
  const isLogin = tab === 'login';
  document.getElementById('tabLogin').classList.toggle('active', isLogin);
  document.getElementById('tabSignup').classList.toggle('active', !isLogin);
  document.getElementById('confirmRow').style.display = isLogin ? 'none' : '';
  document.getElementById('dobRow').style.display = isLogin ? 'none' : '';
  document.getElementById('authSubmitBtn').textContent = isLogin ? 'Log In' : 'Sign Up';
  document.getElementById('forgotBtn').style.display = isLogin ? '' : 'none';
  document.getElementById('authError').textContent = '';
}

// ─── Age retry lockout (COPPA) ──────────────────────────────────────────────
// If someone enters an under-13 DOB, lock them out for 24h to prevent easy retry.
const AGE_LOCKOUT_KEY = 'sparq_age_lockout_until';
const AGE_LOCKOUT_MS  = 24 * 60 * 60 * 1000; // 24 hours

function isAgeLockedOut() {
  const until = parseInt(localStorage.getItem(AGE_LOCKOUT_KEY) || '0', 10);
  return until > Date.now();
}

function setAgeLockout() {
  localStorage.setItem(AGE_LOCKOUT_KEY, String(Date.now() + AGE_LOCKOUT_MS));
}

function computeAge(dobString, now = new Date()) {
  const dob = new Date(dobString);
  let age = now.getFullYear() - dob.getFullYear();
  const monthDiff = now.getMonth() - dob.getMonth();
  if (monthDiff < 0 || (monthDiff === 0 && now.getDate() < dob.getDate())) age--;
  return age;
}

// ─── Consent logging ────────────────────────────────────────────────────────
// Records the signup consent artifact (DOB, timestamp, IP, userAgent).
// Stored at users/{uid}/consent/signup — meets COPPA "reasonable effort" standard.
async function logConsentArtifact(uid, dob) {
  try {
    // Fetch IP with 3s timeout so it never hangs the UI thread
    const ipPromise = fetch('https://api.ipify.org?format=json', { signal: AbortSignal.timeout?.(3000) })
      .then(r => r.json()).then(d => d.ip);
    const ip = await Promise.race([
      ipPromise.catch(() => 'unknown'),
      new Promise(res => setTimeout(() => res('unknown'), 3000))
    ]);
    const artifact = {
      type:      'age_gate_signup',
      dob:       dob,
      ageAtSignup: computeAge(dob),
      timestamp: firebase.firestore.FieldValue.serverTimestamp(),
      clientTimestamp: new Date().toISOString(),
      userAgent: navigator.userAgent,
      ip:        ip,
      platform:  (window.Capacitor?.isNativePlatform?.() ? 'native' : 'web'),
    };
    await firebase.firestore()
      .collection('users').doc(uid)
      .collection('consent').doc('signup')
      .set(artifact);
  } catch (err) {
    console.warn('[consent] Failed to log artifact:', err.message);
    // Non-fatal — signup still succeeds
  }
}

// ─── Password visibility toggle ─────────────────────────────────────────────
function togglePasswordVisibility(inputId, btn) {
  const input = document.getElementById(inputId);
  if (!input) return;
  const isHidden = input.type === 'password';
  input.type = isHidden ? 'text' : 'password';
  btn.textContent = isHidden ? '🙈' : '👁️';
  btn.setAttribute('aria-label', isHidden ? 'Hide password' : 'Show password');
}

// ─── Submit button loading state ────────────────────────────────────────────
function setAuthSubmitting(isSubmitting, label = null) {
  const btn = document.getElementById('authSubmitBtn');
  if (!btn) return;
  btn.disabled = isSubmitting;
  btn.style.opacity = isSubmitting ? '0.65' : '';
  btn.style.cursor = isSubmitting ? 'wait' : '';
  if (label !== null) btn.textContent = label;
}

// ─── Email auth ──────────────────────────────────────────────────────────────
async function handleEmailAuth() {
  const email    = document.getElementById('authEmail').value.trim();
  const password = document.getElementById('authPassword').value;
  const isSignup = document.getElementById('tabSignup').classList.contains('active');
  const errorEl  = document.getElementById('authError');
  errorEl.style.color = '';
  errorEl.textContent = '';

  if (!email || !password) {
    errorEl.textContent = 'Please fill in all fields.';
    return;
  }

  let dobVal = null;
  if (isSignup) {
    // Retry lockout check
    if (isAgeLockedOut()) {
      errorEl.textContent = 'Sign-up is temporarily unavailable on this device. Please try again later.';
      return;
    }

    const confirm = document.getElementById('authConfirm').value;
    if (password !== confirm) {
      errorEl.textContent = 'Passwords don\'t match.';
      return;
    }
    if (password.length < 6) {
      errorEl.textContent = 'Password must be at least 6 characters.';
      return;
    }
    const m = document.getElementById('dobMonth').value;
    const d = document.getElementById('dobDay').value;
    const y = document.getElementById('dobYear').value;
    if (!m || !d || !y) {
      errorEl.textContent = 'Please enter your full date of birth.';
      return;
    }
    // Pad to YYYY-MM-DD
    dobVal = `${y}-${String(m).padStart(2,'0')}-${String(d).padStart(2,'0')}`;
    const age = computeAge(dobVal);
    if (age < 13) {
      setAgeLockout();
      errorEl.textContent = 'You must be 13 or older to use Sparq. You won\'t be able to sign up again for 24 hours.';
      return;
    }
  }

  const originalLabel = isSignup ? 'Sign Up' : 'Log In';
  setAuthSubmitting(true, isSignup ? 'Creating account…' : 'Signing in…');

  try {
    let cred;
    if (isSignup) {
      cred = await auth.createUserWithEmailAndPassword(email, password);
      // Log consent artifact (fire-and-forget, no await blocking UI)
      if (cred?.user?.uid && dobVal) logConsentArtifact(cred.user.uid, dobVal);
    } else {
      await auth.signInWithEmailAndPassword(email, password);
    }
  } catch (err) {
    const msgs = {
      'auth/email-already-in-use':  'Email already registered. Try logging in.',
      'auth/invalid-email':         'Invalid email address.',
      'auth/weak-password':         'Password too weak (min 6 chars).',
      'auth/user-not-found':        'No account with this email.',
      'auth/wrong-password':        'Incorrect password.',
      'auth/invalid-credential':    'Incorrect email or password.',
      'auth/too-many-requests':     'Too many attempts. Try again later.',
      'auth/network-request-failed':'Network error. Check your connection.',
    };
    errorEl.textContent = msgs[err.code] || err.message;
    setAuthSubmitting(false, originalLabel);
  }
}

// ─── Platform detection ─────────────────────────────────────────────────────
// Running inside a Capacitor native shell? If so, use native auth plugin.
// Otherwise (plain browser), fall back to Firebase Web SDK popup flow.
function isNativePlatform() {
  return !!(window.Capacitor && window.Capacitor.isNativePlatform && window.Capacitor.isNativePlatform());
}

// ─── Google sign-in ──────────────────────────────────────────────────────────
async function handleGoogleSignIn() {
  const errorEl = document.getElementById('authError');
  errorEl.textContent = '';
  try {
    if (isNativePlatform()) {
      // Native Google Sign-In — opens Android's account picker, no WebView popup
      const { FirebaseAuthentication } = window.Capacitor.Plugins;
      const result = await FirebaseAuthentication.signInWithGoogle();
      // Bridge the native credential back into the Firebase JS SDK so onAuthStateChanged fires
      if (result?.credential?.idToken) {
        const credential = firebase.auth.GoogleAuthProvider.credential(
          result.credential.idToken,
          result.credential.accessToken
        );
        await auth.signInWithCredential(credential);
      }
    } else {
      const provider = new firebase.auth.GoogleAuthProvider();
      await auth.signInWithPopup(provider);
    }
  } catch (err) {
    if (err.code !== 'auth/popup-closed-by-user' && err.message !== 'Sign in canceled.')
      errorEl.textContent = err.message;
  }
}

// ─── Apple sign-in ───────────────────────────────────────────────────────────
async function handleAppleSignIn() {
  const errorEl = document.getElementById('authError');
  errorEl.textContent = '';
  try {
    if (isNativePlatform()) {
      const { FirebaseAuthentication } = window.Capacitor.Plugins;
      const result = await FirebaseAuthentication.signInWithApple();
      if (result?.credential?.idToken) {
        const provider = new firebase.auth.OAuthProvider('apple.com');
        const credential = provider.credential({
          idToken: result.credential.idToken,
          rawNonce: result.credential.nonce,
        });
        await auth.signInWithCredential(credential);
      }
    } else {
      const provider = new firebase.auth.OAuthProvider('apple.com');
      provider.addScope('email');
      provider.addScope('name');
      await auth.signInWithPopup(provider);
    }
  } catch (err) {
    if (err.code !== 'auth/popup-closed-by-user' && err.message !== 'Sign in canceled.')
      errorEl.textContent = err.message;
  }
}

// ─── Forgot password ─────────────────────────────────────────────────────────
async function handleForgotPassword() {
  const email   = document.getElementById('authEmail').value.trim();
  const errorEl = document.getElementById('authError');

  if (!email) {
    errorEl.textContent = 'Enter your email first.';
    return;
  }

  try {
    await auth.sendPasswordResetEmail(email);
    errorEl.style.color = '#00FFD4';
    errorEl.textContent = 'Reset link sent! Check your inbox.';
    setTimeout(() => { errorEl.style.color = ''; }, 4000);
  } catch (err) {
    errorEl.textContent = err.code === 'auth/user-not-found'
      ? 'No account with this email.'
      : err.message;
  }
}

// ─── Logout ──────────────────────────────────────────────────────────────────
async function handleLogout() {
  await auth.signOut();
  switchPage('home');
}
