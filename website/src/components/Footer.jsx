import React, { useState } from 'react';
import { NavLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useLanguage } from '../translations/LanguageContext';
import { getTitleLogoPath } from '../translations/titleLogo';
import AccessibilityModal from './AccessibilityModal';

export default function Footer() {
  const year = new Date().getFullYear();
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);
  const { t } = useTranslation();
  const [isAccessibilityOpen, setIsAccessibilityOpen] = useState(false);

  return (
    <>
      <footer className="w-full mt-auto bg-void-black/95 border-t border-primary-container/20 text-on-surface-variant font-mono text-[13px] relative z-40">
        {/* Subtle top cyan line */}
        <div className="absolute top-0 left-0 right-0 h-[1px] bg-gradient-to-r from-transparent via-primary-container/40 to-transparent"></div>

        <div className="max-w-container-max-width mx-auto px-4 md:px-margin-desktop py-12">
          <div className="grid grid-cols-1 md:grid-cols-12 gap-8 md:gap-gutter">
            
            {/* Brand & Status Column */}
            <div className="md:col-span-7 flex flex-col gap-4">
              <div className="flex items-center gap-3">
                <img 
                  src={getTitleLogoPath(selectedLanguage)} 
                  alt="AXIOM FORCE" 
                  className="h-10 md:h-12 w-auto object-contain drop-shadow-[0_0_14px_rgba(0,243,255,0.7)] select-none transition-all duration-300"
                />
                <span className="font-mono text-[10px] tracking-[0.25em] text-primary-container/60 border border-primary-container/30 px-2 py-0.5">// OPS_TERMINAL</span>
              </div>
              
              <p className="text-on-surface-variant/80 max-w-md font-mono text-[12px] leading-relaxed">
                {t('footer.tagline', "A 3D action platformer built with Godot 4.7.2 and C#. Reclaim Earth from Xelthara alien occupiers.")}
              </p>
              
              {/* Telemetry Status Bar */}
              <div className="flex flex-wrap items-center gap-3 mt-1 font-mono text-[11px] tracking-wider text-on-surface-variant/70">
                <div className="flex items-center gap-2 bg-primary-container/10 border border-primary-container/30 px-3 py-1.5">
                  <span className="relative flex h-2 w-2">
                    <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-tertiary-container opacity-75"></span>
                    <span className="relative inline-flex rounded-full h-2 w-2 bg-tertiary-container"></span>
                  </span>
                  <span className="text-tertiary-container font-bold uppercase tracking-[0.15em]">
                    {t('footer.status', "AXIOM-7 ONLINE — RESISTANCE ACTIVE")}
                  </span>
                </div>
                <span className="hidden sm:inline border border-outline-variant/30 px-2 py-1 bg-surface-container-low/50">
                  BUILD: NET10_V4.7.2
                </span>
                <span className="hidden sm:inline border border-outline-variant/30 px-2 py-1 bg-surface-container-low/50">
                  LOC: SECTOR_04
                </span>
              </div>
            </div>

            {/* Navigation Links Column */}
            <div className="md:col-span-5 flex flex-col justify-between">
              <div>
                <h4 className="font-mono text-[11px] text-primary-container uppercase tracking-[0.25em] mb-4 flex items-center gap-2">
                  <span className="w-1.5 h-1.5 bg-primary-container inline-block"></span>
                  {t('footer.nav_title', "HLF DIRECTORY")}
                </h4>
                <div className="grid grid-cols-2 gap-2 font-mono text-[12px] tracking-wider">
                  {[
                    { key: 'home', to: '' },
                    { key: 'gameplay', to: '/gameplay' },
                    { key: 'squad', to: '/squad' },
                    { key: 'tech_hub', to: '/tech-hub' },
                    { key: 'faq', to: '/faq' },
                  ].map(link => (
                    <NavLink
                      key={link.to}
                      to={`/${currentLocale}${link.to}`}
                      end={link.to === ''}
                      className={({ isActive }) =>
                        `py-1 hover:text-primary-container transition-colors flex items-center gap-1.5 ${
                          isActive ? 'text-primary-container font-bold' : 'text-on-surface-variant'
                        }`
                      }
                    >
                      <span className="opacity-40">&gt;</span> {t(`nav.${link.key}`, link.key.toUpperCase().replace('_', ' '))}
                    </NavLink>
                  ))}
                  <div>
                    <button
                      onClick={() => setIsAccessibilityOpen(true)}
                      className="py-1 hover:text-primary-container transition-colors text-left text-on-surface-variant cursor-pointer flex items-center gap-1.5"
                    >
                      <span className="opacity-40">&gt;</span> <span>{t('footer.accessibility_link', 'ACCESSIBILITY')}</span>
                    </button>
                  </div>
                </div>
              </div>
            </div>

          </div>

          {/* Divider with technical ticks */}
          <div className="relative my-8 h-[1px] bg-outline-variant/20 flex items-center justify-between">
            <span className="w-2 h-1 bg-primary-container/40"></span>
            <span className="w-2 h-1 bg-primary-container/40"></span>
          </div>

          {/* Copyright Bar */}
          <div className="flex flex-col md:flex-row justify-between items-center gap-4 text-[11px] font-mono tracking-widest text-on-surface-variant/60">
            <div className="flex items-center gap-4 flex-wrap">
              <span>
                {t('footer.copyright', { year }).replace('{{year}}', year)}
              </span>
              <span className="hidden md:inline text-primary-container/30">|</span>
              <button 
                onClick={() => setIsAccessibilityOpen(true)}
                className="text-primary-container/80 hover:text-primary-container hover:underline cursor-pointer flex items-center gap-1"
              >
                ♿ {t('footer.accessibility_btn', 'Accessibility')}
              </button>
            </div>
            <span className="text-primary-container/70 font-mono">
              {t('footer.broadcast_status', 'SECURE PROTOCOL // QUANTUM CARRIER ACTIVE')}
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

