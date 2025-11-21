import { test, expect } from '@playwright/test';

const dispatcherNavLabel = 'Dispatch';
const dispatchHeading = /Dispatch Board/i;

async function navigateToDispatchBoard(page: import('@playwright/test').Page) {
  await page.goto('/');
  await page.getByRole('link', { name: dispatcherNavLabel }).click();
  await expect(page).toHaveURL(/\/dispatch$/);
  await expect(page.getByRole('heading', { name: dispatchHeading })).toBeVisible();
}

test.describe('Dispatcher board workflows', () => {
  test.beforeEach(async ({ page }) => {
    await navigateToDispatchBoard(page);
  });

  test('surfaces crew recommendations with telemetry health warnings', async ({ page }) => {
    const recommendationsGrid = page.getByTestId('crew-recommendations-grid');
    await expect(recommendationsGrid).toBeVisible();

    const recommendationRows = recommendationsGrid.getByRole('row');
    const rowCount = await recommendationRows.count();
    expect(rowCount).toBeGreaterThan(1);

    const staleBadge = recommendationsGrid.getByTestId('telemetry-stale-badge').first();
    await expect(staleBadge).toBeVisible();
    await expect(staleBadge).toHaveText(/telemetry stale/i);

    const autoSelectedRow = recommendationsGrid.locator('[data-selected="true"]').first();
    await expect(autoSelectedRow).toBeVisible();
    await expect(autoSelectedRow.getByTestId('score-chip')).toContainText('%');
  });

  test('allows dispatcher to override recommendation before publishing assignment', async ({ page }) => {
    const recommendationsGrid = page.getByTestId('crew-recommendations-grid');
    const rows = recommendationsGrid.getByRole('row');
    await rows.nth(1).click(); // skip header row

    await page.getByRole('button', { name: /Override selection/i }).click();
    const overrideDialog = page.getByRole('dialog', { name: /Override recommendation/i });
    await expect(overrideDialog).toBeVisible();

    await overrideDialog.getByLabel('Override reason').fill('Crew Bravo is already staged near the outage.');
    await overrideDialog.getByRole('button', { name: /Send assignment/i }).click();

    const toast = page.getByTestId('assignment-receipt-toast');
    await expect(toast).toBeVisible();
    await expect(toast).toContainText(/Assignment queued/i);
  });

  test('shows assignment timeline entry after publish acknowledgement', async ({ page }) => {
    const timeline = page.getByTestId('dispatch-timeline');
    await expect(timeline).toBeVisible();

    const latestEvent = timeline.getByRole('listitem').first();
    await expect(latestEvent).toContainText(/Assignment published/i);
    await expect(latestEvent).toContainText(/auto operator/i);
    await expect(latestEvent).toContainText(/score\.composite/i);
  });

  test('allows dispatcher to send crew acknowledgement via status panel', async ({ page }) => {
    const statusPanel = page.getByTestId('crew-status-panel');
    await expect(statusPanel).toBeVisible();

    await page.getByTestId('crew-status-select').selectOption('Acknowledged');
    await page.getByTestId('crew-status-send').click();

    const toast = page.getByTestId('assignment-receipt-toast');
    await expect(toast).toContainText(/acknowledgement sent/i);

    await expect(page.getByTestId('crew-status-alert')).toHaveCount(0);
    const timeline = page.getByTestId('dispatch-timeline');
    await expect(timeline.getByRole('listitem').first()).toContainText(/Crew acknowledged/i);
  });
});
