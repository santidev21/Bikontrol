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

function writeEnv(file, apiUrl, production) {
  const content = `export const environment = {
  production: ${production},
  apiUrl: '${apiUrl}',
  googleClientId: '${googleClientId}',
  // Etiqueta legible de la versión que se muestra en Perfil.
  // Súbela solo en releases con cambios visibles para el usuario (no en cada commit).
  appVersion: '${appVersion}'
};
`;
  fs.writeFileSync(file, content);
}

const envDir = path.resolve(__dirname, '..', 'src', 'environments');
// Dev usa HTTP plano (:5202) para no pelear con el cert autofirmado de Kestrel
// en el navegador (ERR_CERT_AUTHORITY_INVALID). Prod mantiene su HTTPS real.
writeEnv(path.join(envDir, 'environment.ts'), 'http://localhost:5202/api', false);
writeEnv(path.join(envDir, 'environment.prod.ts'), 'https://bikontrol.santidev21.tech/api', true);
console.log('environment.ts and environment.prod.ts generated');
