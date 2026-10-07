import { expect, Page, test } from '@playwright/test';

/**
 * Critical flow (plan 4.3): a new user registers, adds a motorcycle, creates a
 * maintenance with a small Km interval, records it, advances the odometer and
 * sees the maintenance come due.
 *
 * The stack runs with `EmailConfirmation__Required=false` (see
 * `.github/workflows/e2e.yml`) so registration returns a session directly and
 * the flow does not depend on reading a confirmation email.
 *
 * Registration uses a unique email per run so repeated CI runs never collide.
 */

// SweetAlert2 dialogs gate navigation after writes; confirm them when they appear.
async function confirmSuccessDialog(page: Page): Promise<void> {
  const confirm = page.locator('.swal2-confirm');
  await expect(confirm).toBeVisible();
  await confirm.click();
}

test('register → motorcycle → maintenance → record → odometer → due', async ({ page }) => {
  const email = `e2e+${Date.now()}@bikontrol.test`;
  const password = 'E2ePassword123!';

  // --- Register -----------------------------------------------------------------
  await page.goto('/register');
  await page.getByLabel('Nombre completo').fill('E2E Rider');
  await page.getByLabel('Correo').fill(email);
  await page.getByLabel('Contraseña', { exact: true }).fill(password);
  await page.getByLabel('Confirmar contraseña').fill(password);
  await page.getByRole('button', { name: 'Registrarse' }).click();

  // A confirmed registration lands on the dashboard (the app redirects there).
  await expect(page).toHaveURL(/\/dashboard\/home/);
  await expect(page.getByRole('heading', { name: 'Bienvenido de nuevo' })).toBeVisible();

  // --- Add a motorcycle ---------------------------------------------------------
  await page.getByRole('button', { name: '+ Agregar motocicleta' }).click();
  await expect(page).toHaveURL(/\/dashboard\/motorcycles\/add/);

  await page.locator('#moto-name').fill('XTZ 150 E2E');
  await page.locator('#moto-brand').fill('Yamaha');
  await page.locator('#moto-year').fill('2024');
  await page.locator('#moto-km').fill('1000');
  await page.locator('#moto-nickname').fill('La azul');
  await page.locator('#moto-displacement').fill('150');
  await page.locator('#moto-plate').fill('E2E123');
  await page.getByRole('button', { name: 'Guardar Motocicleta' }).click();
  await confirmSuccessDialog(page);

  // Back on the home page the new motorcycle is listed.
  await expect(page).toHaveURL(/\/dashboard\/home/);
  const card = page.getByRole('heading', { name: 'XTZ 150 E2E' });
  await expect(card).toBeVisible();
  await card.click();

  // --- Motorcycle summary: create a maintenance with a small Km interval --------
  await expect(page).toHaveURL(/\/dashboard\/motorcycles\/summary/);
  await expect(page.getByText('1000 km')).toBeVisible();

  await page.getByRole('button', { name: '+ Agregar mantenimientos' }).click();
  await expect(page).toHaveURL(/\/dashboard\/motorcycles\/.+\/maintenance$/);

  await page.getByRole('button', { name: '+ Agregar mantenimiento personalizado' }).click();
  await expect(page).toHaveURL(/\/dashboard\/motorcycles\/.+\/maintenance\/add$/);

  await page.locator('#maint-name').fill('Aceite E2E');
  await page.locator('#maint-description').fill('Cambio de aceite recurrente');
  // Monitoring type defaults to Km; set a 100 km interval.
  await page.getByLabel('Intervalo en kilómetros').fill('100');
  await page.getByRole('button', { name: 'Guardar Mantenimiento' }).click();
  await confirmSuccessDialog(page);

  // Back on the maintenance catalog, the new maintenance is listed.
  await expect(page).toHaveURL(/\/dashboard\/motorcycles\/.+\/maintenance$/);

  // --- Record the maintenance ---------------------------------------------------
  // The catalog does not expose the record action, so return to the summary with
  // the top-nav back button (a direct URL load has no motorcycle state and would
  // bounce to home).
  await page.getByRole('button', { name: 'Volver' }).click();
  await expect(page).toHaveURL(/\/dashboard\/motorcycles\/summary\?motorcycleId=/);

  await page.getByRole('button', { name: '+ Registrar mantenimiento' }).click();
  await expect(page).toHaveURL(/\/dashboard\/motorcycles\/.+\/register-maintenance$/);

  await page.getByRole('checkbox', { name: /Aceite E2E/ }).check();
  await page.locator('#record-km').fill('1000');
  await page.getByRole('button', { name: 'Registrar mantenimiento' }).click();
  await confirmSuccessDialog(page);

  // Back on the summary, the record is listed and the countdown restarted.
  await expect(page).toHaveURL(/\/dashboard\/motorcycles\/summary/);
  await expect(page.getByText('Aceite E2E')).toBeVisible();

  // --- Advance the odometer beyond the interval → the item comes due ------------
  await page.getByLabel('Editar kilometraje').click();
  await page.locator('.km-input').fill('1200');
  await page.getByRole('button', { name: 'Aceptar' }).click();
  await confirmSuccessDialog(page);

  await expect(page.getByText('1200 km')).toBeVisible();
  // Interval was 100 km and the record was at 1000 km, so at 1200 km it is overdue.
  await expect(page.getByText(/Vencido por \d+ km/)).toBeVisible();
});
