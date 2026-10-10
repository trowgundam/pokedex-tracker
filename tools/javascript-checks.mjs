import { readdir } from 'node:fs/promises';
import { spawnSync } from 'node:child_process';

const scripts = [
    ...(await readdir('pokedex_tracker/wwwroot')).filter(file => file.endsWith('.js')).map(file => 'pokedex_tracker/wwwroot/' + file),
    ...(await readdir('tools')).filter(file => file.endsWith('.mjs') || /^browser-.*\.js$/.test(file)).map(file => 'tools/' + file)
];
for (const script of scripts) {
    const result = spawnSync(process.execPath, ['--check', script], { stdio: 'inherit' });
    if (result.status !== 0) throw new Error('Syntax check failed: ' + script);
}
const result = spawnSync(process.execPath, ['--test', 'tools/preferences-checks.mjs', 'tools/ci-checks.mjs'], { stdio: 'inherit' });
if (result.status !== 0) process.exitCode = 1;
