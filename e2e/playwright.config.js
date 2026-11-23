// Serves the WebGL build from ./site and plays it in headless Chromium.
const { defineConfig } = require('@playwright/test');

module.exports = defineConfig({
  testDir: './tests',
  timeout: 6 * 60 * 1000,
  retries: 1,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: 'http://127.0.0.1:8080',
    viewport: { width: 720, height: 1100 },
    trace: 'retain-on-failure',
    launchOptions: {
      // Software WebGL so the game renders on a runner without a GPU.
      args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist'],
    },
  },
  webServer: {
    command: 'python3 -m http.server 8080 --bind 127.0.0.1 --directory site',
    url: 'http://127.0.0.1:8080/index.html',
    reuseExistingServer: !process.env.CI,
    timeout: 60 * 1000,
  },
});
