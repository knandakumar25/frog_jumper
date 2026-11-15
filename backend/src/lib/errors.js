/**
 * Error types and response shapes kept compatible with Django REST Framework.
 *
 * The website and game read these bodies: `{detail}` for auth and generic errors, `{error}` for
 * the codes the views return, and `{field: [messages]}` for validation. Keeping the shapes means
 * no client change was needed when the backend moved to Express.
 */

/** An error that becomes an HTTP response with a fixed JSON body. */
export class HttpError extends Error {
  /**
   * @param {number} status HTTP status code
   * @param {object} body JSON body sent as is
   * @param {Record<string, string>} [headers] extra response headers
   */
  constructor(status, body, headers = {}) {
    super(typeof body?.detail === 'string' ? body.detail : `HTTP ${status}`);
    this.status = status;
    this.body = body;
    this.headers = headers;
  }
}

/**
 * DRF's `AuthenticationFailed` / `NotAuthenticated`.
 *
 * DRF sends 401 with a `WWW-Authenticate` header on routes that authenticate, and 403 on routes
 * with `authentication_classes = []` (there is no scheme to name). `routeHasAuth` picks which.
 * @param {object} body e.g. `{detail: 'Invalid Clerk session'}`
 * @param {boolean} routeHasAuth
 * @returns {HttpError}
 */
export function authFailed(body, routeHasAuth = true) {
  return routeHasAuth
    ? new HttpError(401, body, { 'WWW-Authenticate': 'Bearer realm="api"' })
    : new HttpError(403, body);
}

/**
 * A field-error map, DRF's `ValidationError` (400).
 * @param {Record<string, string[]>} fields
 * @returns {HttpError}
 */
export function validationError(fields) {
  return new HttpError(400, fields);
}

/** Collects field errors in DRF's `{field: [message, ...]}` shape. */
export class FieldErrors {
  constructor() {
    /** @type {Record<string, string[]>} */
    this.errors = {};
  }

  /**
   * Record one message for `field`.
   * @param {string} field
   * @param {string} message
   */
  add(field, message) {
    (this.errors[field] ??= []).push(message);
  }

  /** @returns {boolean} true when at least one error was recorded */
  get any() {
    return Object.keys(this.errors).length > 0;
  }

  /** Throw a 400 when any error was recorded. */
  throwIfAny() {
    if (this.any) throw validationError(this.errors);
  }
}

/** DRF's stock validation messages, word for word. */
export const MESSAGES = Object.freeze({
  required: 'This field is required.',
  blank: 'This field may not be blank.',
  null: 'This field may not be null.',
  notString: 'Not a valid string.',
  email: 'Enter a valid email address.',
  maxLength: (n) => `Ensure this field has no more than ${n} characters.`,
  usernameInvalid: 'Enter a valid username. This value may contain only letters, numbers, and @/./+/-/_ characters.',
  usernameTaken: 'A user with that username already exists.',
  emailTaken: 'user with this email already exists.',
  dateInvalid: 'Date has wrong format. Use one of these formats instead: YYYY-MM-DD.',
  booleanInvalid: 'Must be a valid boolean.',
});

/**
 * Express error handler: turns `HttpError` into its response and anything else into a 500.
 *
 * A malformed JSON body (from `express.json`) becomes DRF's 400 parse error. Other errors are
 * logged without request bodies, which can hold passwords and tokens.
 * @type {import('express').ErrorRequestHandler}
 */
export function errorHandler(error, req, res, next) {
  if (res.headersSent) return next(error);
  if (error instanceof HttpError) {
    res.set(error.headers).status(error.status).json(error.body);
    return undefined;
  }
  if (error?.type === 'entity.parse.failed') {
    res.status(400).json({ detail: `JSON parse error - ${error.message}` });
    return undefined;
  }
  if (error?.type === 'entity.too.large') {
    res.status(413).json({ detail: 'Request body is too large.' });
    return undefined;
  }
  console.error(`Unhandled error on ${req.method} ${req.path}:`, error);
  res.status(500).json({ detail: 'A server error occurred.' });
  return undefined;
}
