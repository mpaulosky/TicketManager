import { spawnSync } from 'node:child_process';
import { readFileSync, writeFileSync } from 'node:fs';

const outputFile = './src/Web/wwwroot/tailwind.css';
const importStatement = '@import url("/Style/Theme.css");\n';
const command = process.platform === 'win32' ? 'tailwindcss.cmd' : 'tailwindcss';
const result = spawnSync(
  command,
  ['-i', './src/Web/wwwroot/app.css', '-o', outputFile, '--minify'],
  { stdio: 'inherit' },
);

if (result.status !== 0) {
  process.exit(result.status ?? 1);
}

const css = readFileSync(outputFile, 'utf8');

if (!css.startsWith(importStatement)) {
  writeFileSync(outputFile, `${importStatement}${css}`);
}
