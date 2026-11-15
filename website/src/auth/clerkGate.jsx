import React from 'react';
import {
  ClerkProvider as RealClerkProvider,
  SignedIn as RealSignedIn,
  SignedOut as RealSignedOut,
  UserButton as RealUserButton,
  SignIn as RealSignIn,
  SignUp as RealSignUp,
  useUser as useRealUser,
  useAuth as useRealAuth,
  useClerk as useRealClerk,
} from '@clerk/clerk-react';
import { useTranslation } from 'react-i18next';

/**
 * Auth gate for the whole app. Never import from '@clerk/clerk-react' directly.
 *
 * Why: Vite inlines VITE_* env vars at BUILD time, and website/.env.local is
 * gitignored, so any build running where VITE_CLERK_PUBLISHABLE_KEY is unset
 * (the Vercel deployment on 2026-10-09, for one) bakes in `undefined`. The real
 * ClerkProvider then throws "Missing publishableKey" on first render and the
 * entire site white-screens.
 *
 * With no key, these wrappers degrade to a signed-out static site: the navbar
 * shows its signed-out Login / Sign Up links, the account button disappears,
 * /onboarding can't reach its start flow, and Login/Signup show a notice
 * instead of Clerk components. With a key, every one of these is the real
 * Clerk component or hook passed straight through, so behaviour is unchanged.
 */
export const CLERK_PUBLISHABLE_KEY = import.meta.env.VITE_CLERK_PUBLISHABLE_KEY ?? '';

// Only a pk_* value counts. On 2026-10-09 the SECRET key (sk_test_...) was
// pasted into Vercel's VITE_CLERK_PUBLISHABLE_KEY by mistake: Clerk threw
// "The publishableKey passed to Clerk is invalid", the site white-screened
// again, and the secret ended up baked into the public bundle. Anything that
// is not a publishable key now degrades to signed-out, same as an absent one --
// and note the wrong value still ships in the bundle regardless (Vite inlines
// VITE_* unconditionally), so a leaked sk_* must be ROTATED, gate or not.
export const CLERK_ENABLED = /^pk_(test|live)_/.test(CLERK_PUBLISHABLE_KEY);

const AUTH_DISABLED_NOTICE = 'Sign-in is not available on this deployment.';

function AuthDisabledNotice() {
  const { t } = useTranslation();
  return (
    <p className="text-on-surface-variant text-sm font-bold uppercase tracking-widest text-center">
      {t('showcase.notConfigured', AUTH_DISABLED_NOTICE)}
    </p>
  );
}

export function ClerkProvider({ children, ...props }) {
  if (!CLERK_ENABLED) return <>{children}</>;
  return (
    <RealClerkProvider publishableKey={CLERK_PUBLISHABLE_KEY} {...props}>
      {children}
    </RealClerkProvider>
  );
}

export function SignedIn({ children }) {
  if (!CLERK_ENABLED) return null;
  return <RealSignedIn>{children}</RealSignedIn>;
}

export function SignedOut({ children }) {
  if (!CLERK_ENABLED) return <>{children}</>;
  return <RealSignedOut>{children}</RealSignedOut>;
}

export function UserButton(props) {
  if (!CLERK_ENABLED) return null;
  return <RealUserButton {...props} />;
}

export function SignIn(props) {
  if (!CLERK_ENABLED) return <AuthDisabledNotice />;
  return <RealSignIn {...props} />;
}

export function SignUp(props) {
  if (!CLERK_ENABLED) return <AuthDisabledNotice />;
  return <RealSignUp {...props} />;
}

export function useUser() {
  // eslint-disable-next-line react-hooks/rules-of-hooks -- CLERK_ENABLED is a
  // build-time constant; the branch cannot change across renders of one build
  if (!CLERK_ENABLED) return { user: null, isLoaded: true, isSignedIn: false };
  // eslint-disable-next-line react-hooks/rules-of-hooks -- same reasoning
  return useRealUser();
}

/** Safe session access when a deployment intentionally has no Clerk key. */
export function useClerkSession() {
  // eslint-disable-next-line react-hooks/rules-of-hooks -- build-time constant
  if (CLERK_ENABLED) return useRealAuth();
  return { isLoaded: true, isSignedIn: false, getToken: async () => null };
}
/** Account switching keeps the public game request in its return URL. */
export function useClerkClient() {
  // eslint-disable-next-line react-hooks/rules-of-hooks -- build-time constant
  if (CLERK_ENABLED) return useRealClerk();
  return { signOut: async () => {} };
}
