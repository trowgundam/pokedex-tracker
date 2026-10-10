import { spawn, execFileSync } from 'node:child_process';
import { appendFile, mkdir, writeFile } from 'node:fs/promises';
import { createWriteStream } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { join } from 'node:path';

// Unknown paths run all checks. Only documentation outside the shipped app is exempt.
export function applicationChanges(paths) {
    return paths.length === 0 || paths.some(path => !/^(?:[^/]+\.md|docs\/.*\.md)$/.test(path));
}

export function gateResults(needs, jobs) {
    if (needs.changes?.result !== 'success') throw new Error('Change detection did not succeed.');
    const application = needs.changes.outputs?.application;
    if (!['true', 'false'].includes(application)) throw new Error('Change detection did not report a valid scope.');
    const expected = application === 'true' ? 'success' : 'skipped';
    for (const job of jobs) {
        const result = needs[job]?.result;
        if (result !== expected) throw new Error(`${job}: expected ${expected}, received ${result ?? 'missing'}.`);
    }
    if (application === 'false') return 'Documentation-only change; application checks were deliberately skipped.';
    return jobs.length ? 'All required categories passed.' : 'Application checks are required.';
}

async function summary(text) {
    if (process.env.GITHUB_STEP_SUMMARY) await appendFile(process.env.GITHUB_STEP_SUMMARY, text + '\n');
}

export async function runCheck(name, command, args, output = 'artifacts/check-results') {
    if (!/^[a-z][a-z0-9-]*$/.test(name) || !command) throw new Error('Usage: ci.mjs run <check-name> <command> [args...]');
    await mkdir(output, { recursive: true });
    const log = createWriteStream(join(output, name + '.log'));
    let tail = '', launchError;
    const child = spawn(command, args, { stdio: ['inherit', 'pipe', 'pipe'] });
    for (const [source, target] of [[child.stdout, process.stdout], [child.stderr, process.stderr]]) {
        source.on('data', chunk => {
            target.write(chunk);
            log.write(chunk);
            tail = (tail + chunk).slice(-12000);
        });
    }
    child.on('error', error => { launchError = error.message; });
    const [code, signal] = await new Promise(resolve => child.on('close', (code, signal) => resolve([code, signal])));
    await new Promise((resolve, reject) => { log.on('error', reject); log.end(resolve); });
    const result = { name, status: code === 0 ? 'passed' : 'failed', exitCode: code, signal, output: launchError ?? tail.trim() };
    await writeFile(join(output, name + '.json'), JSON.stringify(result, null, 2) + '\n');
    await summary(`### ${name}\n\n${result.status === 'passed' ? 'Passed.' : 'Failed.'}\n\n${result.output.split('\n').map(line => '    ' + line).join('\n')}\n`);
    if (result.status === 'failed' && process.env.GITHUB_ACTIONS === 'true') {
        const message = result.output.split('\n').slice(-15).join('\n').replaceAll('%', '%25').replaceAll('\r', '%0D').replaceAll('\n', '%0A');
        console.error(`::error title=${name}::${message}`);
    }
    return result;
}

async function main() {
    const [action, name, ...args] = process.argv.slice(2);
    switch (action) {
        case 'changes': {
            const base = process.env.CI_BASE_SHA, head = process.env.CI_HEAD_SHA;
            let application = true;
            if (base && head && !/^0+$/.test(base)) {
                const comparison = process.env.CI_EVENT === 'pull_request' ? [`${base}...${head}`] : [base, head];
                execFileSync('git', ['diff', '--check', ...comparison], { stdio: 'inherit' });
                const paths = execFileSync('git', ['diff', '--no-renames', '--name-only', '-z', ...comparison], { encoding: 'utf8' }).split('\0').filter(Boolean);
                application = applicationChanges(paths);
            }
            if (process.env.GITHUB_OUTPUT) await appendFile(process.env.GITHUB_OUTPUT, `application=${application}\n`);
            await summary(application ? 'Application or CI changes: run all checks.' : 'Documentation-only changes: skip application builds, tests, and deployment.');
            console.log(`application=${application}`);
            break;
        }
        case 'gate': {
            const message = gateResults(JSON.parse(process.env.NEEDS ?? '{}'), args);
            await summary(`### ${name}\n\n${message}`);
            console.log(message);
            break;
        }
        case 'run': {
            const [command, ...commandArgs] = args;
            const result = await runCheck(name, command, commandArgs);
            if (result.status === 'failed') process.exitCode = result.exitCode > 0 ? result.exitCode : 1;
            break;
        }
        default: throw new Error('Usage: ci.mjs changes | gate <name> [job...] | run <name> <command> [args...]');
    }
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
    main().catch(error => { console.error(error.message); process.exitCode = 1; });
}
