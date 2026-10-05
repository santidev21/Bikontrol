import { expect, Page, test } from '@playwright/test';

/**
 * Onboarding flow (plan 3.5): a brand-new user is guided from the empty
 * dashboard to a created motorcycle + maintenance plan, and lands on the
 * summary. Runs against the real stack with `EmailConfirmation__Required=false`
 * (see `.github/workflows/e2e.yml`).
 */

async function confirmSuccessDialog(page: Page): Promise<void> {
  const confirm = page.locator('.swal2-confirm');
  await expect(confirm).toBeVisible();
  await confirm.click();
}

test('guided onboarding creates motorcycle and plan', async ({ page }) => {
  const email = `onboarding+${Date.now()}@bikontrol.test`;
  const password = 'E2ePassword123!';

  // --- Register and land on the empty dashboard ---------------------------------
  await page.goto('/register');
  await page.getByLabel('Nombre completo').fill('Onboarding Rider');
  await page.getByLabel('Correo').fill(email);
  await page.getByLabel('Contraseña', { exact: true }).fill(password);
  await page.getByLabel('Confirmar contraseña').fill(password);
  await page.getByRole('button', { name: 'Registrarse' }).click();

  await expect(page).toHaveURL(/\/dashboard\/home/);
  await expect(page.getByRole('heading', { name: '¡Te damos la bienvenida!' })).toBeVisible();

  // --- Guided setup: step 1, the motorcycle ------------------------------------
  await page.getByRole('button', { name: 'Comenzar' }).click();
  await expect(page).toHaveURL(/\/dashboard\/onboarding/);
  await expect(page.getByText('Paso 1 de 2')).toBeVisible();

  await page.locator('#moto-name').fill('Onboarding Moto');
  await page.locator('#moto-brand').fill('Honda');
  await page.locator('#moto-year').fill('2023');
  await page.locator('#moto-km').fill('2000');
  await page.locator('#moto-nickname').fill('La nueva');
  await page.locator('#moto-displacement').fill('160');
  await page.locator('#moto-plate').fill('ONB123');
  await page.getByRole('button', { name: 'Continuar' }).click();

  // --- Step 2, the plan (recommended items pre-selected) -----------------------
  await expect(page.getByText('Paso 2 de 2')).toBeVisible();
  await expect(page.getByText(/seleccionados/)).toBeVisible();

  await page.getByRole('button', { name: 'Crear moto y plan' }).click();
  await confirmSuccessDialog(page);

  // --- Landed on the summary with the motorcycle and its plan -------------------
  await expect(page).toHaveURL(/\/dashboard\/motorcycles\/summary/);
  await expect(page.getByRole('heading', { name: 'Onboarding Moto' })).toBeVisible();
  await expect(page.getByText('Cambio de Aceite')).toBeVisible();
});
