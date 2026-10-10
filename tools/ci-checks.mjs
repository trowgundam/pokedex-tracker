import assert from 'node:assert/strict';
import { test } from 'node:test';
import { mkdtemp, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { applicationChanges, gateResults } from './ci.mjs';
import { browserSuite, browserGroups } from './browser-suite.mjs';

test('only documentation outside the shipped app skips application checks', () => {
    assert.equal(applicationChanges(['README.md', 'CONTRIBUTING.md', 'docs/verification.md', 'docs/adr/0001.md']), false);
    for (const path of ['pokedex_tracker/wwwroot/help.md', '.github/workflows/pages.yml', 'tools/ci.mjs', 'global.json', 'docs/example.js', 'docs/image.png', 'unknown']) {
        assert.equal(applicationChanges(['README.md', path]), true, path);
    }
    assert.equal(applicationChanges([]), true);
});

test('moving executable code into documentation still runs application checks', async () => {
    const directory = await mkdtemp(join(tmpdir(), 'pokedex-ci-scope-'));
    try {
        const git = (args, input) => {
            const result = spawnSync('git', ['-c', 'diff.renames=true', ...args], { cwd: directory, input, encoding: 'utf8' });
            assert.equal(result.status, 0, result.stderr);
            return result.stdout.trim();
        };
        git(['init', '--quiet']);
        const blob = git(['hash-object', '-w', '--stdin'], 'console.log("same executable content");\n');
        const tools = git(['mktree'], `100644 blob ${blob}\trunner.mjs\n`);
        const docs = git(['mktree'], `100644 blob ${blob}\trunner.md\n`);
        const before = git(['mktree'], `040000 tree ${tools}\ttools\n`);
        const after = git(['mktree'], `040000 tree ${docs}\tdocs\n`);
        assert.equal(git(['diff', '--name-only', before, after]), 'docs/runner.md');
        const output = join(directory, 'scope-output');
        const result = spawnSync(process.execPath, [fileURLToPath(new URL('./ci.mjs', import.meta.url)), 'changes'], {
            cwd: directory, encoding: 'utf8', env: { ...process.env, CI_BASE_SHA: before, CI_HEAD_SHA: after, CI_EVENT: 'push', GITHUB_OUTPUT: output, GITHUB_STEP_SUMMARY: '', GIT_CONFIG_COUNT: '1', GIT_CONFIG_KEY_0: 'diff.renames', GIT_CONFIG_VALUE_0: 'true' }
        });
        assert.equal(result.status, 0, result.stderr);
        assert.equal(await readFile(output, 'utf8'), 'application=true\n');
    } finally { await rm(directory, { recursive: true, force: true }); }
});

test('required gates reject failures, cancellations, missing jobs, and unexpected skips', () => {
    const changes = { result: 'success', outputs: { application: 'true' } };
    assert.equal(gateResults({ changes }, []), 'Application checks are required.');
    assert.equal(gateResults({ changes, build: { result: 'success' }, tests: { result: 'success' } }, ['build', 'tests']), 'All required categories passed.');
    for (const result of ['failure', 'cancelled', 'skipped', undefined]) {
        assert.throws(() => gateResults({ changes, build: { result: 'success' }, tests: { result } }, ['build', 'tests']), /tests: expected success/);
    }
    for (const result of ['failure', 'cancelled', 'skipped', undefined]) {
        assert.throws(() => gateResults({ changes: { result }, tests: { result: 'success' } }, ['tests']), /Change detection did not succeed/);
    }
    assert.throws(() => gateResults({ changes: { result: 'success', outputs: { application: 'unknown' } } }, []), /valid scope/);
});

test('documentation-only gates require deliberate skips and successful change detection', () => {
    const changes = { result: 'success', outputs: { application: 'false' } };
    assert.equal(gateResults({ changes, build: { result: 'skipped' }, tests: { result: 'skipped' } }, ['build', 'tests']), 'Documentation-only change; application checks were deliberately skipped.');
    for (const result of ['failure', 'cancelled', 'success', undefined]) {
        assert.throws(() => gateResults({ changes, tests: { result } }, ['tests']), /tests: expected skipped/);
    }
});

test('browser groups preserve every script and desktop/phone variant exactly once', async () => {
    const expected = {
        interface: ['browser-ui-checks.js at 1440px', 'browser-ui-checks.js at 390px'],
        appearance: ['browser-theme-checks.js at 1440px'],
        sources: ['browser-catalog-checks.js at 1440px', 'browser-source-checks.js at 1440px', 'browser-evolution-checks.js at 1440px', 'browser-evolution-checks.js at 390px'],
        storage: ['browser-sync-checks.js at 1440px', 'browser-storage-checks.js at 1440px', 'browser-checks.js at 1440px']
    };
    const plans = await Promise.all(browserGroups.map(group => browserSuite(group)));
    for (let index = 0; index < 4; index++) {
        const actions = plans[index];
        assert.deepEqual(actions.filter(action => action.kind === 'evaluate').map(action => action.name), expected[browserGroups[index]]);
        for (let action = 0; action < actions.length; action += 3) {
            assert.equal(actions[action].kind, 'reset');
            assert.equal(actions[action + 1].kind, 'resize');
            assert.equal(actions[action + 2].kind, 'evaluate');
            assert.match(actions[action + 2].expression, /runPokedex/);
        }
    }
    const full = await browserSuite();
    const serialize = actions => actions.map(action => JSON.stringify(action)).sort();
    assert.deepEqual(serialize(plans.flat()), serialize(full));
    const navigation = plans[4];
    assert.equal(navigation[0].kind, 'reset');
    assert.equal(navigation[1].width, 1440);
    assert.deepEqual(navigation.filter(action => action.kind === 'offline').map(action => action.offline), [true, false]);
    assert.equal(navigation.some(action => action.name === 'Verify Back'), true);
    assert.equal(navigation.some(action => action.name === 'Verify Forward'), true);
    assert.equal(navigation.some(action => action.name === 'Verify offline progress'), true);
    await assert.rejects(browserSuite('typo'), /Unknown browser group: typo/);
});

test('check runner preserves exit status, failure details, logs, and job summaries', async () => {
    const directory = await mkdtemp(join(tmpdir(), 'pokedex-ci-report-'));
    try {
        for (const [name, code] of [['passes', 0], ['fails', 7]]) {
            const summary = join(directory, name + '-summary.md');
            const result = spawnSync(process.execPath, [fileURLToPath(new URL('./ci.mjs', import.meta.url)), 'run', name, process.execPath, '-e', `console.log('Literal check output'); console.error('Literal diagnostic'); process.exit(${code});`], {
                cwd: directory, encoding: 'utf8', env: { ...process.env, GITHUB_ACTIONS: '', GITHUB_STEP_SUMMARY: summary }
            });
            assert.equal(result.status, code);
            const report = JSON.parse(await readFile(join(directory, 'artifacts/check-results', name + '.json'), 'utf8'));
            assert.equal(report.status, code === 0 ? 'passed' : 'failed');
            assert.equal(report.exitCode, code);
            assert.match(report.output, /Literal diagnostic/);
            assert.match(await readFile(join(directory, 'artifacts/check-results', name + '.log'), 'utf8'), /Literal check output/);
            assert.match(await readFile(summary, 'utf8'), code === 0 ? /Passed\./ : /Failed\./);
        }
        const missing = spawnSync(process.execPath, [fileURLToPath(new URL('./ci.mjs', import.meta.url)), 'run', 'missing', join(directory, 'absent-command')], { cwd: directory, encoding: 'utf8', env: { ...process.env, GITHUB_ACTIONS: '', GITHUB_STEP_SUMMARY: '' } });
        assert.equal(missing.status, 1);
        const report = JSON.parse(await readFile(join(directory, 'artifacts/check-results/missing.json'), 'utf8'));
        assert.equal(report.status, 'failed');
        assert.match(report.output, /ENOENT/);
    } finally { await rm(directory, { recursive: true, force: true }); }
});

test('gate CLI returns a failing exit status when an upstream check fails', () => {
    const result = spawnSync(process.execPath, [fileURLToPath(new URL('./ci.mjs', import.meta.url)), 'gate', 'release', 'browser'], {
        encoding: 'utf8', env: { ...process.env, GITHUB_STEP_SUMMARY: '', NEEDS: JSON.stringify({ changes: { result: 'success', outputs: { application: 'true' } }, browser: { result: 'failure' } }) }
    });
    assert.equal(result.status, 1);
    assert.match(result.stderr, /browser: expected success, received failure/);
});
