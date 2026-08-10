import React, { useState } from 'react';
import { NavLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useLanguage } from '../translations/LanguageContext';
import AccessibilityModal from './AccessibilityModal';

export default function Footer() {
  const year = new Date().getFullYear();
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);
  const { t } = useTranslation();
  const [isAccessibilityOpen, setIsAccessibilityOpen] = useState(false);

  return (
    <>
      <footer className="w-full mt-auto bg-void-black border-t-4 border-double border-secondary-container text-on-surface-variant font-body-md text-[14px]">
        <div className="max-w-container-max-width mx-auto px-4 md:px-margin-desktop py-12">
          <div className="grid grid-cols-1 md:grid-cols-12 gap-8 md:gap-gutter">
            
            {/* Brand & Status Column */}
            <div className="md:col-span-8 flex flex-col gap-4">
              <div className="flex items-center gap-3">
                <img 
                  src="/images/axiom_force_title_logo.jpg" 
                  alt="AXIOM FORCE" 
                  className="h-12 md:h-14 w-auto object-contain drop-shadow-[0_0_10px_rgba(0,243,255,0.5)] rounded-md"
                />
              </div>
              
              <p className="text-on-surface-variant max-w-sm font-body-md leading-relaxed">
                {t('footer.tagline', "A 3D action platformer built with Godot 4.7.1 and C#. Reclaim Earth from Xelthara alien occupiers.")}
              </p>
              
              {/* Status indicator */}
              <div className="flex items-center gap-2.5 mt-2 bg-deep-grey/50 border border-outline-variant/30 px-4 py-2 w-fit">
                <span className="relative flex h-2.5 w-2.5">
                  <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-tertiary-container opacity-75"></span>
                  <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-tertiary-container"></span>
                </span>
                <span className="text-[12px] font-semibold text-tertiary-fixed-dim uppercase tracking-[0.1em] font-label-caps">
                  {t('footer.status', "AXIOM-7 ONLINE — RESISTANCE ACTIVE")}
                </span>
              </div>
            </div>

            {/* Navigation Links Column */}
            <div className="md:col-span-4">
              <h4 className="font-label-caps text-label-caps text-warning-orange uppercase tracking-wider mb-4 border-b border-warning-orange/30 pb-2 w-fit">
                {t('footer.nav_title', "NAVIGATION")}
              </h4>
              <ul className="flex flex-col gap-2">
                {[
                  { key: 'home', to: '' },
                  { key: 'gameplay', to: '/gameplay' },
                  { key: 'squad', to: '/squad' },
                ].map(link => (
                  <li key={link.to}>
                    <NavLink
                      to={`/${currentLocale}${link.to}`}
                      end={link.to === ''}
                      className={({ isActive }) =>
                        `hover:text-primary-container transition-colors ${
                          isActive ? 'text-primary-container font-semibold' : 'text-on-surface-variant'
                        }`
                      }
                    >
                      &gt; {t(`nav.${link.key}`, link.key.toUpperCase().replace('_', ' '))}
                    </NavLink>
                  </li>
                ))}
                <li>
                  <button
                    onClick={() => setIsAccessibilityOpen(true)}
                    className="hover:text-primary-container transition-colors text-left text-on-surface-variant cursor-pointer flex items-center gap-1.5 mt-1"
                  >
                    &gt; <span className="underline decoration-primary-container/40 underline-offset-4">{t('footer.accessibility_link', 'ACCESSIBILITY STATEMENT')}</span>
                  </button>
                </li>
              </ul>
            </div>

          </div>

          {/* Divider */}
          <div className="h-[2px] bg-outline-variant/20 my-8"></div>

          {/* Copyright Bar */}
          <div className="flex flex-col md:flex-row justify-between items-center gap-4 text-[12px] font-label-caps text-on-surface-variant/80">
            <div className="flex items-center gap-4 flex-wrap">
              <span>
                {t('footer.copyright', { year }).replace('{{year}}', year)}
              </span>
              <span className="hidden md:inline">•</span>
              <button 
                onClick={() => setIsAccessibilityOpen(true)}
                className="text-primary-container hover:underline cursor-pointer flex items-center gap-1"
              >
                ♿ {t('footer.accessibility_btn', 'Accessibility')}
              </button>
            </div>
            <span className="text-secondary-fixed-dim font-body-md tracking-wider">
              {t('footer.broadcast_status', 'v1.0.0 // NET-SYNC OPERATIONAL // SECURE BROADCAST')}
            </span>
          </div>
        </div>
      </footer>

      {/* Accessibility Statement Modal */}
      <AccessibilityModal
        isOpen={isAccessibilityOpen}
        onClose={() => setIsAccessibilityOpen(false)}
      />
    </>
  );
}
