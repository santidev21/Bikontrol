import { expect, test } from '@playwright/test';

/**
 * Landing (plan 5.1): guests see the marketing page at `/`, while authenticated
 * users are sent straight to the dashboard.
 */

test('guests see the landing and authenticated users are redirected to the dashboard', async ({
  page,
}) => {
  // --- Guest: the landing renders -------------------------------------------
  await page.goto('/');
  await expect(page.getByRole('heading', { name: /El mantenimiento de tu moto/ })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Empezar gratis' })).toBeVisible();

  // --- Authenticated: root goes to the dashboard -----------------------------
  const email = `landing+${Date.now()}@bikontrol.test`;
  const password = 'E2ePassword123!';
  await page.goto('/register');
  await page.getByLabel('Nombre completo').fill('Landing Rider');
  await page.getByLabel('Correo').fill(email);
  await page.getByLabel('Contraseña', { exact: true }).fill(password);
  await page.getByLabel('Confirmar contraseña').fill(password);
  await page.getByRole('button', { name: 'Registrarse' }).click();
  await expect(page).toHaveURL(/\/dashboard\/home/);

  await page.goto('/');
  await expect(page).toHaveURL(/\/dashboard\/home/);
});
