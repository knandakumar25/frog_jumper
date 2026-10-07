import React from 'react';
import { SignIn } from '@clerk/clerk-react';
import { useLanguage } from '../translations/LanguageContext';

export default function Login() {
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);

  return (
    <div className="pt-[120px] md:pt-[150px] pb-32 px-4 flex justify-center items-center">
      <div className="w-full max-w-md bg-surface-container-lowest border-2 border-primary-container/30 p-8 shadow-[0_0_30px_rgba(0,243,255,0.15)] relative bracket-corners">

        {/* Accent corners */}
        <div className="absolute -top-1 -left-1 w-4 h-4 border-t-2 border-l-2 border-primary-container"></div>
        <div className="absolute -top-1 -right-1 w-4 h-4 border-t-2 border-r-2 border-primary-container"></div>
        <div className="absolute -bottom-1 -left-1 w-4 h-4 border-b-2 border-l-2 border-primary-container"></div>
        <div className="absolute -bottom-1 -right-1 w-4 h-4 border-b-2 border-r-2 border-primary-container"></div>

        <div className="flex justify-center">
          <SignIn
            afterSignInUrl={`/${currentLocale}`}
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
