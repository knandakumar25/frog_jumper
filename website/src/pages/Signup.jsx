import React, { useState } from 'react';
import { useParams } from 'react-router-dom';
import { gameRequest, confirmationPath } from '../auth/gameHandoff';
// Via the gate so a keyless build shows a notice instead of throwing.
import { SignUp } from '../auth/clerkGate.jsx';
import { useLanguage } from '../translations/LanguageContext';

// Phase 3: the terminal room the Clerk card stands in (code-split; the form
// never waits on three.js, and a CSS fallback shows if WebGL is unavailable).

export default function Signup() {
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  // Keep the public handoff association while Clerk advances its nested form.
  const [handoff] = useState(gameRequest);
  const suffix = handoff ? `/game/${handoff}` : "";
  const { locale: routeLocale } = useParams();
  const currentLocale = routeLocale || getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);
  const target = handoff ? confirmationPath(handoff, currentLocale) : null;

  return (
    <div data-handoff-target={target || ''} className="relative overflow-hidden pt-[120px] md:pt-[150px] pb-32 px-4 flex justify-center items-center">
      <div className="absolute inset-0 bg-[radial-gradient(110%_80%_at_50%_15%,#0a2230_0%,#06121b_50%,#050505_100%)]"></div>
      <div className="absolute inset-0 opacity-40 bg-[linear-gradient(rgba(0,243,255,0.05)_1px,transparent_1px),linear-gradient(90deg,rgba(0,243,255,0.05)_1px,transparent_1px)] bg-[size:44px_44px]"></div>

      <div className="relative z-10 w-full max-w-md bg-surface-container-lowest/95 border-2 border-primary-container/30 p-8 shadow-[0_0_30px_rgba(0,243,255,0.15)] bracket-corners">

        {/* Accent corners */}
        <div className="absolute -top-1 -left-1 w-4 h-4 border-t-2 border-l-2 border-primary-container"></div>
        <div className="absolute -top-1 -right-1 w-4 h-4 border-t-2 border-r-2 border-primary-container"></div>
        <div className="absolute -bottom-1 -left-1 w-4 h-4 border-b-2 border-l-2 border-primary-container"></div>
        <div className="absolute -bottom-1 -right-1 w-4 h-4 border-b-2 border-r-2 border-primary-container"></div>

        {/* Tactical Security Header */}
        <div className="border-b border-primary-container/20 pb-3 mb-6 flex justify-between items-center font-mono text-[10px] tracking-widest text-primary-container">
          <span className="flex items-center gap-1.5">
            <span className="w-2 h-2 rounded-full bg-secondary-container animate-pulse"></span>
            RECRUITMENT // NEW_OPERATIVE_ENLIST
          </span>
          <span className="text-on-surface-variant/70">HLF_REGISTRY</span>
        </div>

        <div className="flex justify-center">
          <SignUp
            routing="path"
            path={`/${currentLocale}/signup${suffix}`}
            signInUrl={`/${currentLocale}/login${suffix}`}
            // Where the user finishes onboarding. BARE, with no locale segment:
            // the "*" catch-all in App.jsx discards the query string, so a
            // locale-prefixed path would break the ?token= link. Also set in
            // the Clerk dashboard for the hosted Account Portal -- this prop
            // only covers the embedded component.
            forceRedirectUrl={target || "/onboarding"}
            fallbackRedirectUrl="/onboarding"
            appearance={{
              elements: {
                formButtonPrimary: "bg-primary-container text-void-black hover:bg-secondary-container transition-colors font-bold uppercase",
                card: "bg-transparent shadow-none border-none",
                headerTitle: "text-primary-container font-headline-md uppercase tracking-widest",
                headerSubtitle: "text-on-surface-variant opacity-80",
                formFieldLabel: "text-on-surface-variant font-bold uppercase tracking-wider",
                formFieldInput: "text-on-surface bg-surface-container-highest border-primary-container/30",
                footerActionLink: "text-primary-container hover:text-secondary-container underline",
                socialsBlockButton: "bg-surface-container-highest text-on-surface border-primary-container/20 hover:bg-surface-container-high"
              }
            }}
          />
        </div>
      </div>
    </div>
  );
}
