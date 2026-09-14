import { chromium } from 'playwright';
import fs from 'node:fs/promises';

const baseUrl = process.env.WEBGL_URL;
if (!baseUrl) {
  throw new Error('WEBGL_URL is not set.');
}

const separator = baseUrl.includes('?') ? '&' : '?';
const url = `${baseUrl}${separator}ci=${encodeURIComponent(process.env.GITHUB_SHA ?? Date.now())}`;

await fs.mkdir('playwright-artifacts', { recursive: true });

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({
  viewport: { width: 1280, height: 900 },
});

const pageErrors = [];
const consoleErrors = [];

page.on('pageerror', error => pageErrors.push(error.message));
page.on('console', message => {
  if (message.type() === 'error') {
    consoleErrors.push(message.text());
  }
});

try {
  console.log(`Opening: ${url}`);

  const response = await page.goto(url, {
    waitUntil: 'domcontentloaded',
    timeout: 120_000,
  });

  if (!response || !response.ok()) {
    throw new Error(`GitHub Pages returned HTTP ${response?.status() ?? 'no response'}.`);
  }

  const canvas = page.locator('#unity-canvas');
  await canvas.waitFor({ state: 'visible', timeout: 120_000 });

  // waitForFunction's timeout belongs in the THIRD argument.
  // Passing it as the second argument makes Playwright use its default 30s timeout.
  await page.waitForFunction(
    () => {
      const loadingBar = document.querySelector('#unity-loading-bar');
      return !loadingBar || getComputedStyle(loadingBar).display === 'none';
    },
    null,
    { timeout: 120_000 }
  );

  const warningText = await page.locator('#unity-warning').textContent().catch(() => '');
  if ((warningText ?? '').trim()) {
    throw new Error(`Unity warning/error banner is not empty: ${(warningText ?? '').trim()}`);
  }

  const canvasState = await canvas.evaluate(element => ({
    width: element.width,
    height: element.height,
    clientWidth: element.clientWidth,
    clientHeight: element.clientHeight,
  }));

  if (
    canvasState.width <= 0 ||
    canvasState.height <= 0 ||
    canvasState.clientWidth <= 0 ||
    canvasState.clientHeight <= 0
  ) {
    throw new Error(`Unity canvas has invalid dimensions: ${JSON.stringify(canvasState)}`);
  }

  if (pageErrors.length > 0) {
    throw new Error(`Browser page errors: ${pageErrors.join(' | ')}`);
  }

  await page.screenshot({
    path: 'playwright-artifacts/webgl-smoke.png',
    fullPage: true,
  });

  console.log('PASS: Unity WebGL loaded successfully.');
  console.log(`Canvas: ${JSON.stringify(canvasState)}`);

  if (consoleErrors.length > 0) {
    console.log(`Browser console errors seen after load: ${consoleErrors.join(' | ')}`);
  }
} catch (error) {
  console.error('Playwright smoke test failed.');
  if (pageErrors.length > 0) {
    console.error(`Page errors: ${pageErrors.join(' | ')}`);
  }
  if (consoleErrors.length > 0) {
    console.error(`Console errors: ${consoleErrors.join(' | ')}`);
  }

  await page.screenshot({
    path: 'playwright-artifacts/webgl-smoke-failure.png',
    fullPage: true,
  }).catch(() => {});

  throw error;
} finally {
  await browser.close();
}
