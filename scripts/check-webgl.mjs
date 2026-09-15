import { chromium } from 'playwright';
import fs from 'node:fs/promises';

const baseUrl = process.env.WEBGL_URL;
if (!baseUrl) {
  throw new Error('WEBGL_URL is not set.');
}

const separator = baseUrl.includes('?') ? '&' : '?';
const url = `${baseUrl}${separator}ci=${encodeURIComponent(process.env.GITHUB_SHA ?? Date.now())}`;

await fs.mkdir('playwright-artifacts', { recursive: true });

const browser = await chromium.launch({
  headless: true,
  args: [
    '--enable-webgl',
    '--ignore-gpu-blocklist',
    '--use-angle=swiftshader',
  ],
});

const page = await browser.newPage({
  viewport: { width: 1280, height: 900 },
});

const pageErrors = [];
const consoleErrors = [];
const failedRequests = [];
const badResponses = [];

page.on('pageerror', error => pageErrors.push(error.message));

page.on('console', message => {
  if (message.type() === 'error') {
    consoleErrors.push(message.text());
  }
});

page.on('requestfailed', request => {
  failedRequests.push(`${request.method()} ${request.url()} :: ${request.failure()?.errorText ?? 'failed'}`);
});

page.on('response', response => {
  if (response.status() >= 400 && response.url().includes('/Build/')) {
    badResponses.push(`${response.status()} ${response.url()}`);
  }
});

try {
  console.log(`Opening: ${url}`);

  const response = await page.goto(url, {
    waitUntil: 'domcontentloaded',
    timeout: 120_000,
  });

  if (!response || !response.ok()) {
    throw new Error(`Page returned HTTP ${response?.status() ?? 'no response'}.`);
  }

  const canvas = page.locator('#unity-canvas');
  await canvas.waitFor({ state: 'visible', timeout: 120_000 });

  // Do not rely on Unity template's loading-bar CSS state. It can remain
  // unchanged in headless Chromium even when the WebGL resources are healthy.
  // Instead allow startup time, then verify the canvas and resource health.
  await page.waitForTimeout(15_000);

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

  if (badResponses.length > 0) {
    throw new Error(`Unity Build resources returned HTTP errors: ${badResponses.join(' | ')}`);
  }

  if (failedRequests.some(item => item.includes('/Build/'))) {
    throw new Error(`Unity Build resource requests failed: ${failedRequests.join(' | ')}`);
  }

  if (pageErrors.length > 0) {
    throw new Error(`Browser page errors: ${pageErrors.join(' | ')}`);
  }

  await page.screenshot({
    path: 'playwright-artifacts/webgl-smoke.png',
    fullPage: true,
  });

  // Compact JPEG dedicated to ChatGPT visual review.
  // Keeping this much smaller than the full PNG makes it practical for the
  // GitHub connector to return the complete image as base64 in one fetch.
  await page.screenshot({
    path: 'playwright-artifacts/webgl-ai-review.jpg',
    type: 'jpeg',
    quality: 45,
    fullPage: true,
  });

  await fs.writeFile(
    'playwright-artifacts/webgl-report.json',
    JSON.stringify({
      status: 'PASS',
      url,
      sourceSha: process.env.GITHUB_SHA ?? null,
      canvas: canvasState,
      pageErrors,
      consoleErrors,
      failedRequests,
      badResponses,
      screenshot: 'webgl-smoke.png',
      aiReviewImage: 'webgl-ai-review.jpg'
    }, null, 2)
  );

  console.log('PASS: Unity WebGL smoke test passed.');
  console.log(`Canvas: ${JSON.stringify(canvasState)}`);

  if (consoleErrors.length > 0) {
    console.log(`Non-fatal browser console errors: ${consoleErrors.join(' | ')}`);
  }
} catch (error) {
  console.error('Playwright smoke test failed.');

  if (pageErrors.length > 0) {
    console.error(`Page errors: ${pageErrors.join(' | ')}`);
  }
  if (consoleErrors.length > 0) {
    console.error(`Console errors: ${consoleErrors.join(' | ')}`);
  }
  if (failedRequests.length > 0) {
    console.error(`Failed requests: ${failedRequests.join(' | ')}`);
  }
  if (badResponses.length > 0) {
    console.error(`Bad responses: ${badResponses.join(' | ')}`);
  }

  await page.screenshot({
    path: 'playwright-artifacts/webgl-smoke-failure.png',
    fullPage: true,
  }).catch(() => {});

  throw error;
} finally {
  await browser.close();
}
