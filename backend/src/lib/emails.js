/**
 * The three emails the API sends, with text and HTML copied from the Django backend:
 * the welcome mail (Django's `accounts/signals.py`), the 2FA code (`accounts/utils.py`) and the
 * onboarding link (`accounts/onboarding_views.py`).
 *
 * Every user-supplied value placed in HTML is escaped; Django's f-strings did not escape.
 */

/**
 * Escape text for HTML.
 * @param {string} value
 * @returns {string}
 */
function esc(value) {
  return String(value).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
}

// Colour is declared on .terminal as well as on body, and inline on paragraphs: mail clients often
// move the message into their own container, where the `body` rule stops matching.
const TERMINAL_STYLE = `
            body { background-color: #05070a; color: #e2e8f0; font-family: 'Courier New', Courier, monospace; margin: 0; padding: 20px; }
            .terminal { max-width: 600px; margin: 0 auto; background-color: #0b0e14; color: #e2e8f0; border: 2px solid #00f3ff; box-shadow: 0 0 20px rgba(0, 243, 255, 0.2); padding: 30px; border-radius: 4px; }
            .header { border-bottom: 2px solid #ff007f; padding-bottom: 15px; margin-bottom: 25px; text-align: center; }
            .header h1 { color: #00f3ff; font-size: 24px; margin: 0; text-transform: uppercase; letter-spacing: 3px; text-shadow: 0 0 10px rgba(0, 243, 255, 0.5); }
            .greeting { font-size: 18px; color: #ff007f; margin-bottom: 20px; font-weight: bold; text-shadow: 0 0 5px rgba(255, 0, 127, 0.3); }
            .details { background-color: #030508; color: #e2e8f0; border-left: 3px solid #ff007f; padding: 15px; margin: 20px 0; font-size: 14px; line-height: 1.6; }
            .status-active { color: #39ff14; font-weight: bold; text-shadow: 0 0 5px rgba(57, 255, 20, 0.5); }
            .cta { display: inline-block; margin: 10px 0 20px; padding: 14px 28px; background-color: #00f3ff; color: #05070a; font-weight: bold; text-transform: uppercase; letter-spacing: 2px; text-decoration: none; border-radius: 3px; }
            .footer { margin-top: 30px; font-size: 11px; color: #94a3b8; text-align: center; border-top: 1px solid #1a202c; padding-top: 15px; }`;

/**
 * Wrap a terminal-styled body in the shared HTML page.
 * @param {string} inner
 * @returns {string}
 */
function page(inner) {
  return `<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="color-scheme" content="dark light">
    <meta name="supported-color-schemes" content="dark light">
    <style>${TERMINAL_STYLE}
    </style>
</head>
<body>
    <div class="terminal">
        <div class="header">
            <h1>[ AXIOM_FORCE_MAIN_NET ]</h1>
        </div>
${inner}
        <div class="footer">
            AXIOM FORCE TERMINAL DIRECTIVE // CONFIDENTIAL TRANSMISSION // DO NOT REPLICATE
        </div>
    </div>
</body>
</html>`;
}

/**
 * The welcome mail sent once when an account is created.
 * @param {{username: string, email: string}} user
 * @param {string} from
 * @returns {object | null} null when the user has no address (nothing is sent)
 */
export function welcomeEmail(user, from) {
  if (!user.email) return null;
  const text = '=== AXIOM FORCE TERMINAL SIGN-IN SUCCESSFUL ===\n\n'
    + `Welcome to the mainframe, Operator ${user.username}.\n\n`
    + 'Your user profile has been successfully initialized. You have been granted access '
    + 'to the Frog Jumper simulation network.\n\n'
    + 'STATUS: ACTIVE\n'
    + 'IP PROTOCOL: DETECTED\n'
    + 'LEVEL ACCESS: LEVEL 1 OPERATOR\n\n'
    + 'Prepare for deployment. See you on the leaderboard.\n\n'
    + '----------------------------------------\n'
    + 'Axiom Force Terminal Admin\n';
  const html = page(`        <div class="greeting">
            &gt; CONNECTION ESTABLISHED: OPERATOR_${esc(user.username.toUpperCase())}
        </div>
        <p style="color: #e2e8f0;">Welcome to the simulation grid. Your system profile has been successfully integrated into the Frog Jumper network.</p>
        <div class="details">
            <strong>SYSTEM STATUS REPORT:</strong><br>
            ------------------------------<br>
            ACCESS LEVEL : OPERATOR (LVL_1)<br>
            SESSION CODE : SECURE<br>
            GRID STATUS  : <span class="status-active">ONLINE</span>
        </div>
        <p style="color: #e2e8f0;">Prepare your reflexes. Jump, survive, and dominate the ranking boards.</p>`);
  return { subject: `SYS_INIT: Welcome to Frog Jumper, ${user.username}`, text, html, from, to: [user.email] };
}

/**
 * Send the welcome mail, logging (never throwing) on failure: a mail outage must not fail
 * registration, as in Django.
 * @param {import('./mail.js').Mailer} mailer
 * @param {import('../config.js').Config} config
 * @param {{username: string, email: string}} user
 */
export async function sendWelcomeEmail(mailer, config, user) {
  const message = welcomeEmail(user, config.defaultFromEmail);
  if (!message) return;
  try {
    await mailer.send(message);
  } catch (error) {
    console.error(`Error sending welcome email to ${user.email}:`, error);
  }
}

/**
 * A plain notice sent to the account's (old) address after a security-relevant change, so the
 * owner learns about it even if someone else made it. Failures are logged, never thrown.
 * @param {import('./mail.js').Mailer} mailer
 * @param {import('../config.js').Config} config
 * @param {string} to
 * @param {'2fa_off' | 'email_changed' | 'account_deleted'} kind
 * @param {string} [newEmail] for `email_changed`
 */
export async function sendSecurityNotice(mailer, config, to, kind, newEmail = '') {
  if (!to) return;
  const message = kind === '2fa_off'
    ? { subject: 'Frog Jumper 2FA Was Turned Off',
      text: 'Two-Factor Authentication was turned off on your account, and other signed-in sessions were signed out.\n\nIf this wasn\'t you, sign in, turn it back on and change your password.' }
    : kind === 'account_deleted'
    ? { subject: 'Your Frog Jumper Account Was Deleted',
      text: 'Your account, profile, sign-in identity, security settings and game saves have been deleted, and every signed-in session was signed out. This cannot be undone.\n\nYou can create a new account at any time.\n\nIf this wasn\'t you, contact support at once.' }
    : { subject: 'Frog Jumper Email Address Changed',
      text: `The email address on your account was changed to ${newEmail}, and other signed-in sessions were signed out.\n\nIf this wasn't you, contact support at once.` };
  try {
    await mailer.send({ ...message, from: config.defaultFromEmail, to: [to] });
  } catch (error) {
    console.error(`Error sending security notice to ${to}:`, error);
  }
}

/**
 * The 2FA code mail.
 * @param {{email: string}} user
 * @param {string} code
 * @param {'opt_in' | 'login' | 'opt_out' | 'email_change' | 'delete_account'} purpose
 * @param {string} from
 * @returns {object}
 */
export function twoFactorEmail(user, code, purpose, from) {
  let subject;
  let text;
  if (purpose === 'opt_in') {
    subject = 'Verify Your Frog Jumper 2FA Setup';
    text = `Welcome to a more secure Frog Jumper experience!\n\nYour verification code to enable Two-Factor Authentication is: ${code}\n\nThis code will expire in 10 minutes.`;
  } else if (purpose === 'login') {
    subject = 'Your Frog Jumper Login Code';
    text = `A login attempt was made on your account. Your verification code is: ${code}\n\nThis code will expire in 10 minutes. If this wasn't you, please secure your account.`;
  } else if (purpose === 'opt_out') {
    subject = 'Confirm Turning Off Frog Jumper 2FA';
    text = `Someone asked to turn off Two-Factor Authentication on your account. Your verification code is: ${code}\n\nThis code will expire in 10 minutes. If this wasn't you, do not share it, and change your password.`;
  } else if (purpose === 'delete_account') {
    subject = 'Confirm Deleting Your Frog Jumper Account';
    text = `Someone asked to permanently delete your account. Your verification code is: ${code}\n\nThis code will expire in 10 minutes. Deleting removes your account, profile, sign-in identity, security settings and game saves; it cannot be undone.\n\nIf this wasn't you, do not share the code, and change your password.`;
  } else if (purpose === 'email_change') {
    subject = 'Confirm Your Frog Jumper Email Change';
    text = `Someone asked to change the email address on your account. Your verification code is: ${code}\n\nThis code will expire in 10 minutes. If this wasn't you, do not share it, and change your password.`;
  } else {
    subject = 'Frog Jumper Security Code';
    text = `Your security code is: ${code}`;
  }
  return { subject, text, from, to: [user.email] };
}

/**
 * The onboarding link mail. The link is deliberately not locale-prefixed: the website's catch-all
 * locale route drops the query string, which would lose `?token=`.
 * @param {string} to
 * @param {string} rawToken
 * @param {string} firstName
 * @param {import('../config.js').Config} config
 * @returns {object}
 */
export function onboardingEmail(to, rawToken, firstName, config) {
  const link = `${config.frontendUrl.replace(/\/+$/, '')}/onboarding?token=${rawToken}`;
  const ttl = config.onboardingTokenTtlHours;
  const greeting = (firstName ?? '').trim() || 'Operator';
  const text = '=== AXIOM FORCE // REGISTRATION INCOMPLETE ===\n\n'
    + `Welcome, ${greeting}.\n\n`
    + 'Your identity has been verified, but your operator profile is not yet '
    + 'on the grid. Two fields remain before your account is created:\n\n'
    + '  [1] COUNTRY OF OPERATION\n'
    + '  [2] DATE OF BIRTH\n\n'
    + 'Complete them here:\n\n'
    + `${link}\n\n`
    + `This link expires in ${ttl} hours and can only be used once.\n`
    + 'If you did not register for Frog Jumper, ignore this message -- no '
    + 'account will be created.\n\n'
    + '----------------------------------------\n'
    + 'Axiom Force Terminal Admin\n';
  const html = page(`        <div class="greeting">
            &gt; IDENTITY VERIFIED: ${esc(greeting.toUpperCase())}
        </div>
        <p style="color: #e2e8f0;">Your credentials cleared the gate, but your operator profile is not on the grid yet. Two required fields remain before your account is created.</p>
        <div class="details">
            <strong>OUTSTANDING FIELDS:</strong><br>
            ------------------------------<br>
            [1] COUNTRY OF OPERATION<br>
            [2] DATE OF BIRTH<br>
            ------------------------------<br>
            LINK EXPIRY : ${ttl} HOURS<br>
            USES        : 1
        </div>
        <p style="color: #e2e8f0;">Resume registration:</p>
        <a class="cta" href="${esc(link)}">Finish Setup</a>
        <p style="font-size: 11px; color: #67e8f9; word-break: break-all;">${esc(link)}</p>
        <p style="font-size: 12px; color: #94a3b8;">If you did not register for Frog Jumper, ignore this message &mdash; no account will be created.</p>`);
  return { subject: 'SYS_INIT: Complete your Axiom Force registration', text, html, from: config.defaultFromEmail, to: [to] };
}
