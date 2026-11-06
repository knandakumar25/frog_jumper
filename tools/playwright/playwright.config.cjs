/** Docker browser tests use the real localhost website origin so Clerk secure development cookies survive reloads. */
const { defineConfig } = require('@playwright/test');
const { execFileSync } = require('node:child_process');
const address = execFileSync('getent', ['ahostsv4', 'host.docker.internal'], { encoding: 'utf8' }).split(/\s+/)[0];
module.exports = defineConfig({
  use: {
    launchOptions: { args: [`--host-resolver-rules=MAP localhost ${address}`] },
  },
  // Credential-bearing browser state and traces must never be persisted as artifacts.
  reporter: 'line',
});
