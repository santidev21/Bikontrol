import { spawn } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const mode = (process.argv[2] ?? 'all').toLowerCase();
const extraArgs = process.argv.slice(3);
const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const rootDirectory = path.resolve(scriptDirectory, '..');
const frontendDirectory = path.join(rootDirectory, 'bikontrol-web');
const backendProjectPath = path.join(rootDirectory, 'Bikontrol', 'Bikontrol.API', 'Bikontrol.API.csproj');
const npmCommand = process.platform === 'win32' ? 'npm.cmd' : 'npm';
const frontendPort = '4201';
const backendEnvironment = {
  ASPNETCORE_ENVIRONMENT: 'Development',
  ASPNETCORE_URLS: 'https://localhost:7179'
};

function createCommand(command, args, options = {}) {
  return {
    command,
    args,
    cwd: options.cwd ?? rootDirectory,
    env: options.env ?? {},
    label: options.label ?? command
  };
}

function buildCommands(selectedMode) {
  if (selectedMode === 'ui') {
    return [
      createCommand(
        npmCommand,
        ['--prefix', frontendDirectory, 'run', 'start', '--', '--port', frontendPort, '--no-open', ...extraArgs],
        { label: 'ui' }
      )
    ];
  }

  if (selectedMode === 'api') {
    return [
      createCommand('dotnet', ['run', '--no-launch-profile', '--project', backendProjectPath, ...extraArgs], {
        label: 'api',
        env: backendEnvironment
      })
    ];
  }

  if (selectedMode === 'all') {
    return [
      createCommand(
        npmCommand,
        ['--prefix', frontendDirectory, 'run', 'start', '--', '--port', frontendPort, '--no-open', ...extraArgs],
        { label: 'ui' }
      ),
      createCommand('dotnet', ['run', '--no-launch-profile', '--project', backendProjectPath, ...extraArgs], {
        label: 'api',
        env: backendEnvironment
      })
    ];
  }

  return null;
}

function writeOutput(label, chunk, stream = process.stdout) {
  const text = chunk.toString();
  const prefix = `[${label}] `;
  const formatted = text
    .split(/\r?\n/)
    .map((line, index, lines) => {
      if (!line && index === lines.length - 1) {
        return '';
      }

      return `${prefix}${line}`;
    })
    .join('\n');

  stream.write(formatted);
}

function runSingle(commandConfig) {
  return new Promise((resolve, reject) => {
    const child = spawn(commandConfig.command, commandConfig.args, {
      cwd: commandConfig.cwd,
      env: { ...process.env, ...commandConfig.env },
      stdio: ['inherit', 'pipe', 'pipe'],
      shell: false
    });

    child.stdout.on('data', chunk => writeOutput(commandConfig.label, chunk));
    child.stderr.on('data', chunk => writeOutput(commandConfig.label, chunk, process.stderr));

    child.on('error', reject);
    child.on('exit', (code, signal) => {
      if (signal) {
        reject(new Error(`${commandConfig.label} exited with signal ${signal}`));
        return;
      }

      if (code !== 0) {
        reject(new Error(`${commandConfig.label} exited with code ${code}`));
        return;
      }

      resolve();
    });
  });
}

function runCombined(commands) {
  const children = [];
  let resolved = false;

  const shutdown = signal => {
    for (const child of children) {
      if (!child.killed) {
        child.kill(signal);
      }
    }
  };

  const onSignal = signal => {
    shutdown(signal);
    process.exit(0);
  };

  process.on('SIGINT', onSignal);
  process.on('SIGTERM', onSignal);

  return new Promise((resolve, reject) => {
    let finished = 0;

    const finalize = error => {
      if (resolved) {
        return;
      }

      resolved = true;
      process.off('SIGINT', onSignal);
      process.off('SIGTERM', onSignal);
      shutdown();

      if (error) {
        reject(error);
        return;
      }

      resolve();
    };

    for (const commandConfig of commands) {
      const child = spawn(commandConfig.command, commandConfig.args, {
        cwd: commandConfig.cwd,
        env: { ...process.env, ...commandConfig.env },
        stdio: ['inherit', 'pipe', 'pipe'],
        shell: false
      });

      children.push(child);

      child.stdout.on('data', chunk => writeOutput(commandConfig.label, chunk));
      child.stderr.on('data', chunk => writeOutput(commandConfig.label, chunk, process.stderr));

      child.on('error', error => finalize(error));
      child.on('exit', (code, signal) => {
        if (signal) {
          finalize(new Error(`${commandConfig.label} exited with signal ${signal}`));
          return;
        }

        if (code !== 0) {
          finalize(new Error(`${commandConfig.label} exited with code ${code}`));
          return;
        }

        finished += 1;
        if (finished === commands.length) {
          finalize();
        }
      });
    }
  });
}

const commands = buildCommands(mode);

if (!commands) {
  console.error('Usage: node scripts/run-bikontrol.mjs <all|ui|api> [extra args]');
  process.exit(1);
}

if (extraArgs.includes('--dry-run')) {
  for (const commandConfig of commands) {
    console.log(`[${commandConfig.label}] ${commandConfig.command} ${commandConfig.args.join(' ')}`);
  }

  process.exit(0);
}

if (commands.length === 1) {
  await runSingle(commands[0]);
} else {
  await runCombined(commands);
}
