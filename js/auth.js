// ─── Firebase Config ──────────────────────────────────────────────────────────
// Replace with your Firebase project config from:
// Firebase Console → Project Settings → Your apps → Config
const firebaseConfig = {
  apiKey:            "YOUR_API_KEY",
  authDomain:        "YOUR_PROJECT.firebaseapp.com",
  projectId:         "YOUR_PROJECT",
  storageBucket:     "YOUR_PROJECT.appspot.com",
  messagingSenderId: "000000000000",
  appId:             "YOUR_APP_ID"
};

firebase.initializeApp(firebaseConfig);
const auth = firebase.auth();

// ─── Auth state listener ─────────────────────────────────────────────────────
auth.onAuthStateChanged(user => {
  const authScreen = document.getElementById('authScreen');
  if (user) {
    authScreen.style.display = 'none';
    document.querySelector('.bottom-nav').style.display = '';
    document.getElementById('fab').style.display = '';
    updateProfileFromAuth(user);
  } else {
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
  document.getElementById('authSubmitBtn').textContent = isLogin ? 'Log In' : 'Sign Up';
  document.getElementById('forgotBtn').style.display = isLogin ? '' : 'none';
  document.getElementById('authError').textContent = '';
}

// ─── Email auth ──────────────────────────────────────────────────────────────
async function handleEmailAuth() {
  const email    = document.getElementById('authEmail').value.trim();
  const password = document.getElementById('authPassword').value;
  const isSignup = document.getElementById('tabSignup').classList.contains('active');
  const errorEl  = document.getElementById('authError');
  errorEl.textContent = '';

  if (!email || !password) {
    errorEl.textContent = 'Please fill in all fields.';
    return;
  }

  if (isSignup) {
    const confirm = document.getElementById('authConfirm').value;
    if (password !== confirm) {
      errorEl.textContent = 'Passwords don\'t match.';
      return;
    }
    if (password.length < 6) {
      errorEl.textContent = 'Password must be at least 6 characters.';
      return;
    }
  }

  try {
    if (isSignup) {
      await auth.createUserWithEmailAndPassword(email, password);
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
    };
    errorEl.textContent = msgs[err.code] || err.message;
  }
}

// ─── Google sign-in ──────────────────────────────────────────────────────────
async function handleGoogleSignIn() {
  const errorEl = document.getElementById('authError');
  errorEl.textContent = '';
  try {
    const provider = new firebase.auth.GoogleAuthProvider();
    await auth.signInWithPopup(provider);
  } catch (err) {
    if (err.code !== 'auth/popup-closed-by-user')
      errorEl.textContent = err.message;
  }
}

// ─── Apple sign-in ───────────────────────────────────────────────────────────
async function handleAppleSignIn() {
  const errorEl = document.getElementById('authError');
  errorEl.textContent = '';
  try {
    const provider = new firebase.auth.OAuthProvider('apple.com');
    provider.addScope('email');
    provider.addScope('name');
    await auth.signInWithPopup(provider);
  } catch (err) {
    if (err.code !== 'auth/popup-closed-by-user')
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
