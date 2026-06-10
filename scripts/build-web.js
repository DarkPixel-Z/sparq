// Simple web build: copy web assets to dist/ for Capacitor to pick up.
// No bundling, no transforms — just mirror the files the browser needs.

import { cpSync, mkdirSync, rmSync, existsSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const dist = resolve(root, 'dist');

// Clean and recreate dist/
if (existsSync(dist)) rmSync(dist, { recursive: true, force: true });
mkdirSync(dist, { recursive: true });

// Files and folders to copy
const items = [
  'index.html',
  'privacy.html',
  'terms.html',
  'css',
  'js',
  'assets',
];

for (const item of items) {
  const src = resolve(root, item);
  const dst = resolve(dist, item);
  if (!existsSync(src)) {
    console.warn(`[build] skip missing: ${item}`);
    continue;
  }
  cpSync(src, dst, { recursive: true });
  console.log(`[build] copied ${item}`);
}

console.log(`[build] dist/ ready at ${dist}`);
