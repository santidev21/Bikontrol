import { spawn, spawnSync } from 'node:child_process';
import { copyFileSync, existsSync, readFileSync, mkdirSync, readdirSync, statSync, unlinkSync, createWriteStream } from 'node:fs';
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
    env.ConnectionStrings__DefaultConnection = `Host=127.0.0.1;Port=5434;Database=${db};Username=${user};Password=${password};SslMode=Require;Trust Server Certificate=true`;
  }
  const jwtKey = dotEnv.Jwt__Key;
  if (jwtKey && !jwtKey.startsWith('CHANGE_ME')) {
    env.Jwt__Key = jwtKey;
  }
  const expireMinutes = dotEnv.Jwt__ExpireMinutes;
  if (expireMinutes) {
    env.Jwt__ExpireMinutes = expireMinutes;
  }
  const refreshExpireDays = dotEnv.Jwt__RefreshExpireDays;
  if (refreshExpireDays) {
    env.Jwt__RefreshExpireDays = refreshExpireDays;
  }
  const googleClientId = dotEnv.Google__ClientId;
  if (googleClientId && !googleClientId.startsWith('CHANGE_ME')) {
    env.Google__ClientId = googleClientId;
  }
  const frontendBaseUrl = dotEnv.Frontend__BaseUrl;
  if (frontendBaseUrl) {
    env.Frontend__BaseUrl = frontendBaseUrl;
  }
  for (const key of [
    'Smtp__Host',
    'Smtp__Port',
    'Smtp__Username',
    'Smtp__Password',
    'Smtp__FromEmail',
    'Smtp__FromName',
    'Smtp__EnableSsl'
  ]) {
    const value = dotEnv[key];
    if (value && !value.startsWith('CHANGE_ME')) {
      env[key] = value;
    }
  }

  return env;
}

const frontendDirectory = path.join(rootDirectory, 'bikontrol-web');
const backendProjectPath = path.join(rootDirectory, 'Bikontrol', 'Bikontrol.API', 'Bikontrol.API.csproj');
const frontendPort = '4201';

// Node 24 on Windows throws EINVAL when spawning the npm.cmd shim, so run
// npm's CLI through node directly (same behavior, no shell needed).
function resolveNpm() {
  const npmCli = path.join(path.dirname(process.execPath), 'node_modules', 'npm', 'bin', 'npm-cli.js');
  if (existsSync(npmCli)) {
    return { command: process.execPath, argsPrefix: [npmCli], shell: false };
  }
  return {
    command: process.platform === 'win32' ? 'npm.cmd' : 'npm',
    argsPrefix: [],
    shell: process.platform === 'win32'
  };
}
const npm = resolveNpm();

function createCommand(command, args, options = {}) {
  return {
    command,
    args,
    cwd: options.cwd ?? rootDirectory,
    env: options.env ?? {},
    shell: options.shell ?? false,
    label: options.label ?? command
  };
}

function uiCommand() {
  return createCommand(
    npm.command,
    [...npm.argsPrefix, '--prefix', frontendDirectory, 'run', 'start', '--', '--port', frontendPort, '--no-open', ...extraArgs],
    { label: 'ui', shell: npm.shell }
  );
}

function buildCommands(selectedMode, apiEnv) {
  if (selectedMode === 'ui') {
    return [uiCommand()];
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
      uiCommand(),
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
      shell: commandConfig.shell ?? false
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
        shell: commandConfig.shell ?? false
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

if (mode === 'backup') {
  ensureBackendPrereqs();
  const dotEnv = existsSync(envFilePath) ? parseDotEnv(envFilePath) : {};
  const dbName = dotEnv.POSTGRES_DB ?? 'bikontrol_db';
  const dbUser = dotEnv.POSTGRES_USER ?? 'bikontrol';
  const backupDir = path.join(rootDirectory, 'backups');
  mkdirSync(backupDir, { recursive: true });
  const ts = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
  const fileName = `bikontrol-db-${ts}.sql.gz`;
  const filePath = path.join(backupDir, fileName);
  console.log(`[db:backup] Dumping "${dbName}" to backups/${fileName} ...`);
  const dockerCmd = process.platform === 'win32' ? 'docker.exe' : 'docker';
  // pg_dump via exec, pipe through gzip on host
  const dump = spawnSync(dockerCmd, ['compose', '-f', 'docker-compose.yml', '-f', 'docker-compose.local.yml', 'exec', '-T', 'db', 'pg_dump', '-U', dbUser, '-d', dbName], { cwd: rootDirectory, encoding: 'buffer', maxBuffer: 200 * 1024 * 1024, shell: process.platform === 'win32' });
  if (dump.error ?? dump.status !== 0) {
    console.error('[db:backup] pg_dump failed. Is the DB running? (`npm run db:up`)');
    if (dump.stderr) console.error(dump.stderr.toString());
    process.exit(1);
  }
  // gzip on host (Node)
  const { gzipSync } = await import('node:zlib');
  const gz = gzipSync(dump.stdout);
  const { writeFileSync } = await import('node:fs');
  writeFileSync(filePath, gz);
  console.log(`[db:backup] Saved ${fileName} (${(gz.length / 1024).toFixed(1)} KB)`);
  // retention: keep last 7
  const files = readdirSync(backupDir).filter(f => f.startsWith('bikontrol-db-') && f.endsWith('.sql.gz')).sort().reverse();
  for (const old of files.slice(7)) {
    try { unlinkSync(path.join(backupDir, old)); console.log(`[db:backup] Pruned ${old}`); } catch {}
  }
  process.exit(0);
}

if (mode === 'restore') {
  ensureBackendPrereqs();
  const backupFile = extraArgs[0];
  if (!backupFile) {
    console.error('Usage: node scripts/run-bikontrol.mjs restore <backups/bikontrol-db-XXXX.sql.gz | .sql>');
    process.exit(1);
  }
  const resolved = path.isAbsolute(backupFile) ? backupFile : path.join(rootDirectory, backupFile);
  if (!existsSync(resolved)) {
    console.error(`[db:restore] file not found: ${resolved}`);
    process.exit(1);
  }
  const dotEnv = existsSync(envFilePath) ? parseDotEnv(envFilePath) : {};
  const dbName = dotEnv.POSTGRES_DB ?? 'bikontrol_db';
  const dbUser = dotEnv.POSTGRES_USER ?? 'bikontrol';
  // wait healthy
  console.log(`[db:restore] Restoring ${resolved} into ${dbName} ...`);
  const dockerCmd2 = process.platform === 'win32' ? 'docker.exe' : 'docker';
  // decompress if gz
  let sqlBuffer = readFileSync(resolved);
  if (resolved.endsWith('.gz')) {
    const { gunzipSync } = await import('node:zlib');
    sqlBuffer = gunzipSync(sqlBuffer);
  }
  // feed to psql via stdin
  const psql = spawn(dockerCmd2, ['compose', '-f', 'docker-compose.yml', '-f', 'docker-compose.local.yml', 'exec', '-T', 'db', 'psql', '-U', dbUser, '-d', dbName, '-v', 'ON_ERROR_STOP=1'], { cwd: rootDirectory, stdio: ['pipe', 'inherit', 'inherit'], shell: process.platform === 'win32' });
  psql.stdin.write(sqlBuffer);
  psql.stdin.end();
  const code = await new Promise(res => psql.on('close', c => res(c)));
  if (code !== 0) {
    console.error('[db:restore] psql restore failed');
    process.exit(1);
  }
  console.log('[db:restore] Restore complete.');
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
