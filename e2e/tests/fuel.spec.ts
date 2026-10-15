import { expect, Page, test } from '@playwright/test';

/**
 * Fuel flow: register → motorcycle → log two full tanks → the log derives
 * consumption (km/l), fuel cost per km and total cost of ownership.
 *
 * Complements the critical maintenance flow (critical-flow.spec.ts); the stack
 * runs with EmailConfirmation__Required=false (see .github/workflows/e2e.yml).
 * Registration uses a unique email per run so repeated CI runs never collide.
 */

// SweetAlert2 dialogs gate navigation after writes; confirm them when they appear.
async function confirmSuccessDialog(page: Page): Promise<void> {
  const confirm = page.locator('.swal2-confirm');
  await expect(confirm).toBeVisible();
  await confirm.click();
}

test('fuel-up log → consumption and cost metrics', async ({ page }) => {
  const email = `e2e-fuel+${Date.now()}@bikontrol.test`;
  const password = 'E2ePassword123!';

  // --- Register -----------------------------------------------------------------
  await page.goto('/register');
  await page.getByLabel('Nombre completo').fill('E2E Fuel');
  await page.getByLabel('Correo').fill(email);
  await page.getByLabel('Contraseña', { exact: true }).fill(password);
  await page.getByLabel('Confirmar contraseña').fill(password);
  await page.getByRole('button', { name: 'Registrarse' }).click();

  await expect(page).toHaveURL(/\/dashboard\/home/);

  // --- Add a motorcycle ---------------------------------------------------------
  await page.getByRole('button', { name: '+ Agregar motocicleta' }).click();
  await expect(page).toHaveURL(/\/dashboard\/motorcycles\/add/);

  await page.locator('#moto-name').fill('XTZ 150 Fuel E2E');
  await page.locator('#moto-brand').fill('Yamaha');
  await page.locator('#moto-year').fill('2024');
  await page.locator('#moto-km').fill('1000');
  await page.locator('#moto-nickname').fill('La fuel');
  await page.locator('#moto-displacement').fill('150');
  await page.locator('#moto-plate').fill('FUEL12');
  await page.getByRole('button', { name: 'Guardar Motocicleta' }).click();
  await confirmSuccessDialog(page);

  // --- Motorcycle summary → fuel log --------------------------------------------
  await expect(page).toHaveURL(/\/dashboard\/home/);
  const card = page.getByRole('heading', { name: 'XTZ 150 Fuel E2E' });
  await expect(card).toBeVisible();
  await card.click();

  await expect(page).toHaveURL(/\/dashboard\/motorcycles\/summary/);
  await page.getByRole('button', { name: 'Ver combustible y consumo' }).click();
  await expect(page).toHaveURL(/\/dashboard\/motorcycles\/.+\/fuel$/);
  await expect(page.getByRole('heading', { name: 'Combustible y consumo' })).toBeVisible();

  // --- Log two full tanks (the first opens the baseline, the second closes it) ---
  await logFuelUp(page, { odometerKm: '1200', litres: '4', cost: '8' });
  await logFuelUp(page, { odometerKm: '1400', litres: '4', cost: '8' });

  // Two rows in the log; the odometer also advanced the motorcycle km.
  await expect(page.getByText('Registros de combustible')).toBeVisible();
  await expect(page.getByText('1400 km')).toBeVisible();

  // 200 km over 4 L → 50 km/l, and 200 km of logged distance.
  await expect(page.getByText('50.00')).toBeVisible();
  await expect(page.getByText('200 km')).toBeVisible();
});

async function logFuelUp(
  page: Page,
  values: { odometerKm: string; litres: string; cost: string },
): Promise<void> {
  await page.locator('#fuel-odometer').fill(values.odometerKm);
  await page.locator('#fuel-litres').fill(values.litres);
  await page.locator('#fuel-cost').fill(values.cost);
  await page.getByRole('button', { name: 'Guardar carga' }).click();
  await confirmSuccessDialog(page);
}
