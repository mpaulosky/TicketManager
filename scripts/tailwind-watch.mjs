import { spawn } from 'node:child_process';
import { existsSync, readFileSync, watchFile, writeFileSync } from 'node:fs';

const outputFile = './src/Web/wwwroot/tailwind.css';
const importStatement = '@import url("/Style/Theme.css");\n';
const command = process.platform === 'win32' ? 'tailwindcss.cmd' : 'tailwindcss';

function ensureThemeImport() {
  if (!existsSync(outputFile)) {
    return;
  }

  const css = readFileSync(outputFile, 'utf8');

  if (!css.startsWith(importStatement)) {
    writeFileSync(outputFile, `${importStatement}${css}`);
  }
}

const child = spawn(
  command,
  ['-i', './src/Web/wwwroot/app.css', '-o', outputFile, '--watch'],
  { stdio: 'inherit' },
);

watchFile(outputFile, { interval: 250 }, ensureThemeImport);

child.on('spawn', ensureThemeImport);
child.on('exit', code => {
  process.exit(code ?? 0);
});

for (const signal of ['SIGINT', 'SIGTERM']) {
  process.on(signal, () => {
    child.kill(signal);
  });
}
