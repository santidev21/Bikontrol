// Genera environment.ts / environment.prod.ts desde el .env raíz (igual que SplitIt).
// Única fuente de verdad: <repo>/.env → Google__ClientId (o GOOGLE_CLIENT_ID).
// Se ejecuta vía prestart/prebuild, así el Client ID no se hardcodea en git.
const fs = require('fs');
const path = require('path');

function parseEnvFile(file) {
  if (!fs.existsSync(file)) return {};
  const lines = fs.readFileSync(file, 'utf-8').split('\n');
  const env = {};
  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith('#')) continue;
    const idx = trimmed.indexOf('=');
    if (idx === -1) continue;
    const key = trimmed.substring(0, idx).trim();
    let value = trimmed.substring(idx + 1).trim();
    if (
      (value.startsWith('"') && value.endsWith('"')) ||
      (value.startsWith("'") && value.endsWith("'"))
    ) {
      value = value.slice(1, -1);
    }
    env[key] = value;
  }
  return env;
}

const envPath = path.resolve(__dirname, '..', '..', '.env');
const fileEnv = parseEnvFile(envPath);
const googleClientId =
  process.env.GOOGLE_CLIENT_ID || fileEnv.GOOGLE_CLIENT_ID || fileEnv.Google__ClientId || '';

// Etiqueta visible en Perfil. Súbela solo en releases con cambios visibles (no en cada commit).
const appVersion = '0.1.0';

// Public demo tenant. Must match the API's Demo:Enabled flag (Demo__Enabled in
// .env). Defaults to false so a production build never shows the demo button
// unless it is turned on deliberately.
function readDemoEnabled() {
  const raw = process.env.DEMO_ENABLED ?? fileEnv.Demo__Enabled ?? fileEnv.DEMO_ENABLED ?? 'false';
  return String(raw).trim().toLowerCase() === 'true';
}

function writeEnv(file, apiUrl, production, demoEnabled) {
  const content = `export const environment = {
  production: ${production},
  apiUrl: '${apiUrl}',
  googleClientId: '${googleClientId}',
  // Public demo tenant. Must match the API's \`Demo:Enabled\` flag; when false
  // the login screen hides the demo button (POST /api/auth/demo returns 404).
  demoEnabled: ${demoEnabled},
  // Etiqueta legible de la versión que se muestra en Perfil.
  // Súbela solo en releases con cambios visibles para el usuario (no en cada commit).
  appVersion: '${appVersion}',
};
`;
  fs.writeFileSync(file, content);
}

// Dev mirrors Development (demo on by default); prod defaults the demo off.
const prodDemoEnabled = readDemoEnabled();
const envDir = path.resolve(__dirname, '..', 'src', 'environments');
// Producer URL: overridable via API_URL so the container can point the bundle at
// a same-origin `/api` (E2E) or another host without editing source. Defaults to
// the production URL.
const prodApiUrl = process.env.API_URL || 'https://bikontrol.santidev21.tech/api';
// Dev usa HTTP plano (:5202) para no pelear con el cert autofirmado de Kestrel
// en el navegador (ERR_CERT_AUTHORITY_INVALID). Prod mantiene su HTTPS real.
writeEnv(path.join(envDir, 'environment.ts'), 'http://localhost:5202/api', false, true);
writeEnv(path.join(envDir, 'environment.prod.ts'), prodApiUrl, true, prodDemoEnabled);
console.log(
  `environment.ts and environment.prod.ts generated (prod apiUrl=${prodApiUrl}, prod demoEnabled=${prodDemoEnabled})`,
);
