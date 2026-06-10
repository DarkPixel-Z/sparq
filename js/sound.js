// ─── Sparq Sound System ──────────────────────────────────────────────────────
// Web Audio-generated sound effects. No audio files — all synthesized live.
// Sounds respect the state.soundEnabled flag (default: true).

let _audioCtx = null;

function getAudioCtx() {
  if (_audioCtx) return _audioCtx;
  try {
    _audioCtx = new (window.AudioContext || window.webkitAudioContext)();
  } catch (e) {
    _audioCtx = null;
  }
  return _audioCtx;
}

function isSoundEnabled() {
  // Default to true if not yet set
  if (typeof state === 'undefined') return true;
  return state.soundEnabled !== false;
}

/** Internal: play a single oscillator tone */
function _tone(freq, duration, type = 'sine', gain = 0.15, delay = 0) {
  const ctx = getAudioCtx();
  if (!ctx) return;
  const now = ctx.currentTime + delay;
  const osc = ctx.createOscillator();
  const g   = ctx.createGain();
  osc.type = type;
  osc.frequency.setValueAtTime(freq, now);
  g.gain.setValueAtTime(0, now);
  g.gain.linearRampToValueAtTime(gain, now + 0.01);
  g.gain.exponentialRampToValueAtTime(0.001, now + duration);
  osc.connect(g);
  g.connect(ctx.destination);
  osc.start(now);
  osc.stop(now + duration + 0.05);
}

/** Internal: play a frequency sweep (woosh-like) */
function _sweep(startFreq, endFreq, duration, type = 'sine', gain = 0.12) {
  const ctx = getAudioCtx();
  if (!ctx) return;
  const now = ctx.currentTime;
  const osc = ctx.createOscillator();
  const g   = ctx.createGain();
  osc.type = type;
  osc.frequency.setValueAtTime(startFreq, now);
  osc.frequency.exponentialRampToValueAtTime(endFreq, now + duration);
  g.gain.setValueAtTime(0, now);
  g.gain.linearRampToValueAtTime(gain, now + 0.01);
  g.gain.exponentialRampToValueAtTime(0.001, now + duration);
  osc.connect(g);
  g.connect(ctx.destination);
  osc.start(now);
  osc.stop(now + duration + 0.05);
}

/** Internal: play noise burst (for confetti / poof effects) */
function _noise(duration, gain = 0.08, filterFreq = 2000) {
  const ctx = getAudioCtx();
  if (!ctx) return;
  const now = ctx.currentTime;
  const bufferSize = ctx.sampleRate * duration;
  const buffer = ctx.createBuffer(1, bufferSize, ctx.sampleRate);
  const data = buffer.getChannelData(0);
  for (let i = 0; i < bufferSize; i++) data[i] = (Math.random() * 2 - 1) * 0.5;
  const src = ctx.createBufferSource();
  src.buffer = buffer;
  const filter = ctx.createBiquadFilter();
  filter.type = 'lowpass';
  filter.frequency.setValueAtTime(filterFreq, now);
  const g = ctx.createGain();
  g.gain.setValueAtTime(gain, now);
  g.gain.exponentialRampToValueAtTime(0.001, now + duration);
  src.connect(filter);
  filter.connect(g);
  g.connect(ctx.destination);
  src.start(now);
  src.stop(now + duration);
}

/**
 * Public: playSound(name) — play a named sound effect.
 * Supported names:
 *   tap        — soft button click
 *   complete   — task complete chime
 *   xp         — XP popup bloop
 *   levelup    — ascending fanfare
 *   achievement— triumphant ding-dong-ding
 *   coin       — coin jingle
 *   victory    — big win fanfare
 *   pet        — cute pet squeak
 *   error      — error buzz
 *   open       — modal opens (soft woosh)
 *   close      — modal closes (softer reverse woosh)
 *   feed       — pet feeding chomp
 */
function playSound(name) {
  if (!isSoundEnabled()) return;
  const ctx = getAudioCtx();
  if (!ctx) return;
  // Resume context on first user interaction (browser autoplay policy)
  if (ctx.state === 'suspended') ctx.resume();

  switch (name) {
    case 'tap':
      _tone(800, 0.06, 'sine', 0.08);
      break;
    case 'complete':
      _tone(523.25, 0.1, 'sine', 0.15);       // C5
      _tone(659.25, 0.15, 'sine', 0.15, 0.08); // E5
      _tone(783.99, 0.2, 'sine', 0.15, 0.15);  // G5
      break;
    case 'xp':
      _tone(1200, 0.08, 'triangle', 0.12);
      _tone(1600, 0.1, 'triangle', 0.1, 0.05);
      break;
    case 'levelup':
      // Triumphant ascending fanfare
      _tone(523.25, 0.12, 'triangle', 0.18);      // C5
      _tone(659.25, 0.12, 'triangle', 0.18, 0.1); // E5
      _tone(783.99, 0.12, 'triangle', 0.18, 0.2); // G5
      _tone(1046.5, 0.3, 'triangle', 0.2, 0.3);   // C6
      _tone(1318.5, 0.3, 'triangle', 0.15, 0.3);  // E6 (harmony)
      break;
    case 'achievement':
      _tone(987.77, 0.15, 'sine', 0.18);       // B5
      _tone(1318.5, 0.15, 'sine', 0.18, 0.12); // E6
      _tone(987.77, 0.2, 'sine', 0.18, 0.24);  // B5
      break;
    case 'coin':
      _tone(987.77, 0.08, 'square', 0.1);        // B5
      _tone(1318.5, 0.12, 'square', 0.08, 0.06); // E6
      break;
    case 'victory':
      // Big dramatic fanfare
      _tone(440, 0.15, 'sawtooth', 0.18);          // A4
      _tone(659.25, 0.15, 'sawtooth', 0.18, 0.12); // E5
      _tone(880, 0.15, 'sawtooth', 0.18, 0.24);    // A5
      _tone(1318.5, 0.5, 'triangle', 0.22, 0.4);   // E6 sustain
      _tone(880, 0.5, 'triangle', 0.15, 0.4);      // A5 harmony
      break;
    case 'pet':
      // Cute squeak — quick high frequency wiggle
      _sweep(900, 1400, 0.12, 'sine', 0.1);
      _sweep(1200, 800, 0.1, 'sine', 0.08, 0.06);
      break;
    case 'error':
      _tone(200, 0.2, 'sawtooth', 0.1);
      break;
    case 'open':
      _sweep(400, 1000, 0.15, 'sine', 0.08);
      break;
    case 'close':
      _sweep(1000, 400, 0.15, 'sine', 0.08);
      break;
    case 'feed':
      _tone(300, 0.08, 'triangle', 0.12);
      _noise(0.1, 0.06, 800);
      _tone(400, 0.1, 'triangle', 0.1, 0.08);
      break;
    case 'challenge':
      // Ominous downward pulse for enemy challenge
      _tone(220, 0.2, 'sawtooth', 0.15);
      _tone(165, 0.3, 'sawtooth', 0.15, 0.15);
      break;
  }
}

function toggleSound() {
  if (typeof state === 'undefined') return;
  state.soundEnabled = !isSoundEnabled();
  if (typeof saveState === 'function') saveState();
  if (state.soundEnabled) playSound('complete');
  if (typeof showPopup === 'function') {
    showPopup(state.soundEnabled ? '🔊 Sound ON' : '🔇 Sound OFF');
  }
}
