/**
 * Runtime settings for the accounts API, read once from environment variables.
 *
 * Every value the Django `core/settings.py` read is kept under the same variable name, so the
 * existing `.env` and `docker-compose.yml` keep working. Two things changed on purpose
 * (owner decision, 2026-11-15):
 *
 * - The token signing key comes from `JWT_SIGNING_KEY`. Django used a key written into the
 *   source; there is no fallback now, and `loadConfig` throws when it is missing or short.
 * - The third-party sign-in settings are gone with those routes.
 *
 * `loadConfig` never touches the network or the database, so tests can build as many configs as
 * they like (the Node equivalent of Django's `override_settings`).
 */

/**
 * Parse an integer setting, falling back to `fallback` when the value is missing or not a number.
 * @param {string | undefined} raw
 * @param {number} fallback
 * @returns {number}
 */
function intSetting(raw, fallback) {
  const value = Number.parseInt(raw ?? '', 10);
  return Number.isFinite(value) ? value : fallback;
}

/**
 * Parse a seconds value into milliseconds, falling back to `fallbackSeconds` (Django used
 * `float(os.environ.get(..., '10'))` with a 10-second fallback for both Clerk and Resend).
 * @param {string | number | undefined} raw
 * @param {number} fallbackSeconds
 * @returns {number}
 */
export function secondsToMs(raw, fallbackSeconds) {
  const value = Number(raw);
  return Number.isFinite(value) && value > 0 ? value * 1000 : fallbackSeconds * 1000;
}

/**
 * Build the settings object.
 * @param {Record<string, string | undefined>} env usually `process.env`
 * @param {{ requireSigningKey?: boolean }} [options] tests and tooling may skip the key check
 * @returns {Readonly<Config>}
 *
 * @typedef {object} Config
 * @property {{host: string, port: number, database: string, user: string, password: string}} db
 * @property {number} port HTTP port (8000, as Django's dev server used)
 * @property {string} jwtSigningKey HS256 key for access and refresh tokens
 * @property {number} accessTtlSeconds access token lifetime (1 hour)
 * @property {number} refreshTtlSeconds refresh token lifetime (7 days)
 * @property {number} passwordIterations PBKDF2 rounds for new password hashes
 * @property {string} frontendUrl website origin used in emailed and browser sign-in links
 * @property {string} resendApiKey empty means mail is printed to the console instead
 * @property {string} defaultFromEmail sender for every message
 * @property {number} resendTimeoutMs
 * @property {string} clerkSecretKey Clerk Backend API key (never sent to the browser)
 * @property {string} clerkIssuer trusted Clerk issuer, trailing slash removed
 * @property {string} clerkJwksUrl trusted key set URL; never taken from a token
 * @property {string[]} clerkAuthorizedParties origins allowed in a Clerk token's `azp` claim
 * @property {string} clerkApiBase
 * @property {number} clerkTimeoutMs
 * @property {number} onboardingTokenTtlHours
 * @property {number} onboardingResendCooldown seconds
 * @property {number} onboardingMaxSends
 * @property {number} onboardingIpHourlyLimit
 * @property {number} onboardingGlobalHourlyLimit
 * @property {number} browserThrottlePerMinute anonymous requests per IP per minute on browser-* routes
 * @property {number} twoFactorMaxAttempts wrong-code tries before a code is burned
 * @property {number} authPerIpPerMinute requests per IP per minute on login, register,
 *   2fa/verify-login and token/refresh (added 2026-11-15)
 * @property {number} loginPerUserPer15Min login attempts per username in 15 minutes
 * @property {number} codesPerUserPer15Min emailed codes per user and purpose in 15 minutes
 * @property {number} gameWritesPerUserPerMinute game save writes per user per minute (2026-11-18)
 * @property {number} leaderboardPerIpPerMinute public leaderboard reads per IP per minute
 * @property {number} maxConcurrentHashes password hashes allowed to run at once
 * @property {string} trustedProxyHost host name of the reverse proxy (nginx) whose
 *   X-Forwarded-For is trusted; any other caller's header is ignored
 * @property {string[]} trustedProxyAddresses extra trusted proxy addresses (tests use loopback)
 */
export function loadConfig(env = process.env, { requireSigningKey = true } = {}) {
  const signingKey = env.JWT_SIGNING_KEY ?? '';
  if (requireSigningKey && signingKey.length < 32) {
    throw new Error(
      'JWT_SIGNING_KEY must be set to a random value of at least 32 characters (see .env.example). '
      + 'The old Django key was written into the source and is no longer used.',
    );
  }
  const issuer = (env.CLERK_ISSUER ?? '').replace(/\/+$/, '');
  return Object.freeze({
    db: {
      host: env.DB_HOST ?? '127.0.0.1',
      port: intSetting(env.DB_PORT, 5432),
      database: env.DB_NAME ?? 'accounts_db',
      user: env.DB_USER ?? 'postgres',
      password: env.DB_PASSWORD ?? 'postgres',
    },
    port: intSetting(env.PORT, 8000),
    jwtSigningKey: signingKey,
    accessTtlSeconds: 60 * 60,
    refreshTtlSeconds: 7 * 24 * 60 * 60,
    // Django 6's default for pbkdf2_sha256. Tests lower it; verification always reads the
    // count stored in the hash, so old and new hashes both keep working.
    passwordIterations: intSetting(env.PASSWORD_ITERATIONS, 1_200_000),
    frontendUrl: env.FRONTEND_URL ?? 'http://localhost:5173',
    resendApiKey: env.RESEND_API_KEY ?? '',
    defaultFromEmail: env.DEFAULT_FROM_EMAIL ?? 'Axiom Force <onboarding@resend.dev>',
    resendTimeoutMs: secondsToMs(env.RESEND_TIMEOUT, 10),
    clerkSecretKey: env.CLERK_SECRET_KEY ?? '',
    clerkIssuer: issuer,
    clerkJwksUrl: env.CLERK_JWKS_URL ?? `${issuer}/.well-known/jwks.json`,
    clerkAuthorizedParties: (env.CLERK_AUTHORIZED_PARTIES ?? 'http://localhost:5173')
      .split(',').map((origin) => origin.trim()).filter(Boolean),
    clerkApiBase: env.CLERK_API_BASE ?? 'https://api.clerk.com/v1',
    clerkTimeoutMs: secondsToMs(env.CLERK_TIMEOUT, 10),
    onboardingTokenTtlHours: intSetting(env.ONBOARDING_TOKEN_TTL_HOURS, 24),
    onboardingResendCooldown: intSetting(env.ONBOARDING_RESEND_COOLDOWN, 60),
    onboardingMaxSends: intSetting(env.ONBOARDING_MAX_SENDS, 5),
    onboardingIpHourlyLimit: intSetting(env.ONBOARDING_IP_HOURLY_LIMIT, 5),
    onboardingGlobalHourlyLimit: intSetting(env.ONBOARDING_GLOBAL_HOURLY_LIMIT, 100),
    browserThrottlePerMinute: intSetting(env.BROWSER_THROTTLE_PER_MINUTE, 90),
    twoFactorMaxAttempts: 5,
    // Owner-approved hardening (2026-11-15 EGM review, issue 1).
    authPerIpPerMinute: intSetting(env.AUTH_PER_IP_PER_MINUTE, 10),
    loginPerUserPer15Min: intSetting(env.LOGIN_PER_USER_PER_15_MIN, 10),
    codesPerUserPer15Min: intSetting(env.CODES_PER_USER_PER_15_MIN, 5),
    // World autosave runs every 20 s plus action commits; 120 a minute leaves wide headroom.
    gameWritesPerUserPerMinute: intSetting(env.GAME_WRITES_PER_USER_PER_MINUTE, 120),
    leaderboardPerIpPerMinute: intSetting(env.LEADERBOARD_PER_IP_PER_MINUTE, 30),
    maxConcurrentHashes: intSetting(env.MAX_CONCURRENT_HASHES, 4),
    // Issue 2: only the nginx container's X-Forwarded-For counts (Compose service `website`).
    trustedProxyHost: env.TRUSTED_PROXY_HOST ?? 'website',
    trustedProxyAddresses: (env.TRUSTED_PROXY_ADDRESSES ?? '').split(',').map((a) => a.trim()).filter(Boolean),
  });
}

/**
 * Return a copy of `config` with some values replaced (the test-time `override_settings`).
 * @param {Readonly<Config>} config
 * @param {Partial<Config>} overrides
 * @returns {Readonly<Config>}
 */
export function withOverrides(config, overrides) {
  return Object.freeze({ ...config, ...overrides });
}
