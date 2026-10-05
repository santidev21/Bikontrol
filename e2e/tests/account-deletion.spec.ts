import { expect, Page, test } from '@playwright/test';

/**
 * Account deletion (plan 5.2): a user can delete their account from the profile
 * and is logged out afterwards. Runs against the real stack with
 * `EmailConfirmation__Required=false` (see `.github/workflows/e2e.yml`).
 */

async function confirmSuccessDialog(page: Page): Promise<void> {
  const confirm = page.locator('.swal2-confirm');
  await expect(confirm).toBeVisible();
  await confirm.click();
}

test('delete account from profile', async ({ page }) => {
  const email = `delete+${Date.now()}@bikontrol.test`;
  const password = 'E2ePassword123!';

  await page.goto('/register');
  await page.getByLabel('Nombre completo').fill('Delete Rider');
  await page.getByLabel('Correo').fill(email);
  await page.getByLabel('Contraseña', { exact: true }).fill(password);
  await page.getByLabel('Confirmar contraseña').fill(password);
  await page.getByRole('button', { name: 'Registrarse' }).click();
  await expect(page).toHaveURL(/\/dashboard\/home/);

  await page.goto('/dashboard/profile');
  await page.getByRole('button', { name: 'Eliminar mi cuenta' }).click();

  await page.getByLabel('Confirmación', { exact: true }).fill('ELIMINAR');
  await page.getByLabel('Contraseña', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Eliminar cuenta', exact: true }).click();
  await confirmSuccessDialog(page);

  // Deleting logs the user out and returns them to login.
  await expect(page).toHaveURL(/\/login/);
});
