/** Read only a public UUID from the URL; credentials never enter browser storage. */
export function gameRequest() {
  const query = new URLSearchParams(window.location.search);
  // A public ID in the Clerk form's base path survives full navigations between
  // factor/verification steps, including browsers that strip the initial query.
  const formPath = window.location.pathname.match(/\/(?:login|signup)\/game\/([^/]+)/);
  let value = query.get('session_id') || formPath?.[1] || '';
  // A full reload of Clerk's intermediate route can retain its trusted local
  // return URL rather than the original query. Only extract a public request ID.
  if (!value) {
    for (const key of ['redirect_url', 'sign_in_force_redirect_url', 'sign_up_force_redirect_url']) {
      try {
        const target = new URL(query.get(key), window.location.origin);
        if (target.origin === window.location.origin && target.pathname === '/auth/browser-confirm')
          value = target.searchParams.get('session_id') || '';
      } catch { /* A malformed optional redirect never authorizes anything. */ }
    }
  }
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value) ? value : '';
}

/** Build the single explicit-approval route shared by Clerk and email completion. */
export function confirmationPath(id, locale = 'en') {
  return `/auth/browser-confirm?session_id=${encodeURIComponent(id)}&locale=${encodeURIComponent(locale)}`;
}
