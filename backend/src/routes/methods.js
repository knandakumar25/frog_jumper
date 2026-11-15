/**
 * Route registration that answers unsupported methods with DRF's 405 instead of Express's 404.
 *
 * Paths are registered with their trailing slash, as the Django URLs had; Express's default
 * non-strict routing also accepts them without it.
 */

/**
 * Register `handlers` for `path` on `router`, plus a 405 for every other method.
 * @param {import('express').Router} router
 * @param {string} path
 * @param {Partial<Record<'get' | 'post' | 'put' | 'patch' | 'delete', import('express').RequestHandler[]>>} handlers
 *   each value is the middleware chain for that method
 */
export function route(router, path, handlers) {
  const allowed = Object.keys(handlers).map((m) => m.toUpperCase());
  if (allowed.includes('GET')) allowed.push('HEAD');
  allowed.push('OPTIONS');
  for (const [method, chain] of Object.entries(handlers)) router[method](path, ...chain);
  router.all(path, (req, res) => {
    res.set('Allow', allowed.join(', ')).status(405).json({ detail: `Method "${req.method}" not allowed.` });
  });
}
