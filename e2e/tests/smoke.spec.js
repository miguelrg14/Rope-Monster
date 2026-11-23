const fs = require('fs');
const path = require('path');
const { test, expect } = require('@playwright/test');
const { PNG } = require('pngjs');

const SCREENSHOTS = path.join(__dirname, '..', 'screenshots');

/** Number of distinct (coarsely quantised) colours: a blank canvas has one or two. */
function distinctColours(pngBuffer) {
  const png = PNG.sync.read(pngBuffer);
  const colours = new Set();
  for (let i = 0; i < png.data.length; i += 4 * 7) {
    colours.add(`${png.data[i] >> 4},${png.data[i + 1] >> 4},${png.data[i + 2] >> 4}`);
  }
  return colours.size;
}

/** Resolves when the game logs "[RopeMonster] Scene ready: <scene>". */
function sceneReady(page, scene) {
  return page.waitForEvent('console', {
    predicate: (message) => message.text().includes(`[RopeMonster] Scene ready: ${scene}`),
    timeout: 4 * 60 * 1000,
  });
}

test('the WebGL build boots, shows the main menu and loads a level', async ({ page }) => {
  fs.mkdirSync(SCREENSHOTS, { recursive: true });

  // Keep a handle on the Unity instance created by the page template: wrap
  // createUnityInstance as soon as the loader script has run, before the
  // template's own onload handler calls it.
  await page.addInitScript(() => {
    document.addEventListener('load', (event) => {
      const src = event.target && event.target.src;
      if (!src || !src.includes('.loader.js') || typeof window.createUnityInstance !== 'function') return;
      const original = window.createUnityInstance;
      window.createUnityInstance = (...args) =>
        original(...args).then((instance) => (window.unityInstance = instance));
    }, true);
  });

  const errors = [];
  page.on('pageerror', (error) => errors.push(`pageerror: ${error.message}`));
  page.on('console', (message) => {
    if (process.env.E2E_VERBOSE) console.log(`[browser ${message.type()}] ${message.text()}`);
    if (message.type() === 'error' && !/favicon/i.test(message.text())) {
      errors.push(`console: ${message.text()}`);
    }
  });

  const menuReady = sceneReady(page, 'MainMenu');
  await page.goto('/index.html');
  await menuReady;

  const canvas = page.locator('#unity-canvas');
  await expect(canvas).toBeVisible();
  await page.waitForTimeout(5000);

  const menu = await canvas.screenshot({ path: path.join(SCREENSHOTS, 'main-menu.png') });
  expect(distinctColours(menu), 'main menu should not be a blank canvas').toBeGreaterThan(40);

  // Same call the "1" button of the level-select menu makes.
  await page.waitForFunction(() => !!window.unityInstance, null, { timeout: 30 * 1000 });
  const levelReady = sceneReady(page, 'Lvl_1');
  await page.evaluate(() => window.unityInstance.SendMessage('MainMenu_CANVAS', 'LoadLevel', 0));
  await levelReady;
  await page.waitForTimeout(5000);

  const level = await canvas.screenshot({ path: path.join(SCREENSHOTS, 'level-1.png') });
  expect(distinctColours(level), 'level should render').toBeGreaterThan(40);

  expect(errors, errors.join('\n')).toEqual([]);
});
