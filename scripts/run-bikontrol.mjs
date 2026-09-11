import { spawn, spawnSync } from 'node:child_process';
import { copyFileSync, existsSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const mode = (process.argv[2] ?? 'all').toLowerCase();
const extraArgs = process.argv.slice(3);
const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const rootDirectory = path.resolve(scriptDirectory, '..');
const devConfigPath = path.join(rootDirectory, 'Bikontrol', 'Bikontrol.API', 'appsettings.Development.json');
const devConfigExamplePath = `${devConfigPath}.example`;
const envFilePath = path.join(rootDirectory, '.env');

function parseDotEnv(filePath) {
  const values = {};
  for (const rawLine of readFileSync(filePath, 'utf8').split(/\r?\n/)) {
    const line = rawLine.trim();
    if (!line || line.startsWith('#')) continue;
    const eq = line.indexOf('=');
    if (eq < 0) continue;
    const key = line.slice(0, eq).trim();
    let value = line.slice(eq + 1).trim();
    if ((value.startsWith('"') && value.endsWith('"')) || (value.startsWith("'") && value.endsWith("'"))) {
      value = value.slice(1, -1);
    }
    values[key] = value;
  }
  return values;
}

// Single source of truth: .env (same credentials the Docker DB uses).
// Falls back to appsettings.Development.json when .env is absent.
function backendEnvironment() {
  const env = {
    ASPNETCORE_ENVIRONMENT: 'Development',
    ASPNETCORE_URLS: 'https://localhost:7179'
  };

  if (!existsSync(envFilePath)) {
    return env;
  }

  const dotEnv = parseDotEnv(envFilePath);
  const db = dotEnv.POSTGRES_DB;
  const user = dotEnv.POSTGRES_USER;
  const password = dotEnv.POSTGRES_PASSWORD;
  if (db && user && password && !password.startsWith('CHANGE_ME')) {
    env.ConnectionStrings__DefaultConnection = `Host=127.0.0.1;Port=5434;Database=${db};Username=${user};Password=${password}`;
  }
  const jwtKey = dotEnv.Jwt__Key;
  if (jwtKey && !jwtKey.startsWith('CHANGE_ME')) {
    env.Jwt__Key = jwtKey;
  }

  return env;
}

const frontendDirectory = path.join(rootDirectory, 'bikontrol-web');
const backendProjectPath = path.join(rootDirectory, 'Bikontrol', 'Bikontrol.API', 'Bikontrol.API.csproj');
const npmCommand = process.platform === 'win32' ? 'npm.cmd' : 'npm';
const frontendPort = '4201';

function createCommand(command, args, options = {}) {
  return {
    command,
    args,
    cwd: options.cwd ?? rootDirectory,
    env: options.env ?? {},
    label: options.label ?? command
  };
}

function buildCommands(selectedMode, apiEnv) {
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
        env: apiEnv
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
        env: apiEnv
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

function ensureBackendPrereqs() {
  if (!existsSync(devConfigPath) && existsSync(devConfigExamplePath)) {
    copyFileSync(devConfigExamplePath, devConfigPath);
    console.log('[setup] created Bikontrol/Bikontrol.API/appsettings.Development.json from the .example template.');
    console.log('[setup] edit ConnectionStrings:DefaultConnection (127.0.0.1:5434) and Jwt:Key before first run.');
  }

  const dockerCommand = process.platform === 'win32' ? 'docker.exe' : 'docker';
  const result = spawnSync(
    dockerCommand,
    ['compose', '-f', 'docker-compose.yml', '-f', 'docker-compose.local.yml', 'up', '-d', 'db'],
    { cwd: rootDirectory, stdio: 'inherit', shell: process.platform === 'win32' }
  );

  if (result.error ?? result.status !== 0) {
    console.error('[db] could not start the Postgres container. Run `npm run db:up` manually.');
    process.exit(1);
  }
}

const apiEnv = mode === 'ui' ? {} : backendEnvironment();

if (mode === 'migrate') {
  ensureBackendPrereqs();
  const result = spawnSync(
    'dotnet',
    ['ef', 'database', 'update', '--project', 'Bikontrol/Bikontrol.Persistence/Bikontrol.Persistence.csproj', '--startup-project', 'Bikontrol/Bikontrol.API/Bikontrol.API.csproj'],
    { cwd: rootDirectory, env: { ...process.env, ...apiEnv }, stdio: 'inherit', shell: process.platform === 'win32' }
  );

  if (result.error ?? result.status !== 0) {
    console.error('[db] `dotnet ef database update` failed. Is the dotnet-ef tool installed? (`dotnet tool install -g dotnet-ef`)');
    process.exit(1);
  }

  process.exit(0);
}

const commands = buildCommands(mode, apiEnv);

if (!commands) {
  console.error('Usage: node scripts/run-bikontrol.mjs <all|ui|api|migrate> [extra args]');
  process.exit(1);
}

if (mode !== 'ui') {
  ensureBackendPrereqs();
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
