import React from 'react';
import { useTranslation } from 'react-i18next';
import { useLanguage } from '../translations/LanguageContext';


export default function TechHub() {
  const { t } = useTranslation();
  const { selectedLanguage } = useLanguage();

  const getUIString = (key) => {
    return t(`tech_hub.${key}`, key);
  };

  const isRtl = selectedLanguage === 'arabic' || selectedLanguage === 'hebrew';

  return (
    <div className="pt-32 pb-24 px-4 md:px-margin-desktop max-w-container-max-width mx-auto w-full">
      
      {/* Hero Header */}
      <header className="mb-16 relative">
        <div className={`absolute top-0 text-tertiary-container opacity-20 font-display-hero text-display-hero pointer-events-none select-none ${
          isRtl ? '-right-8' : '-left-8'
        }`}>
          SYS.REQ //
        </div>
        <h1 className="font-display-hero text-headline-lg-mobile md:text-display-hero text-primary-container mb-4 uppercase tracking-tighter drop-shadow-[0_0_12px_rgba(0,243,255,0.5)] font-extrabold">
          {t('features.tech_title', 'Core Architecture')}
        </h1>
        <p className={`font-body-lg text-body-lg text-on-surface-variant max-w-3xl border-warning-orange pl-6 ${
          isRtl ? 'border-r-4 pr-6 pl-0 text-right' : 'border-l-4 pl-6 pr-0 text-left'
        }`}>
          {getUIString('hero_desc')}
        </p>
      </header>

      <div className="glitch-divider"></div>

      {/* Engine Specs Section */}
      <section className="mb-24 grid grid-cols-1 lg:grid-cols-12 gap-8 lg:gap-gutter">
        <div className="lg:col-span-5 flex flex-col justify-center">
          <h2 className="font-headline-lg text-headline-lg text-white uppercase mb-6 flex items-center gap-4">
            <span className="material-symbols-outlined text-primary-container" style={{ fontVariationSettings: "'FILL' 1" }}>terminal</span>
            {getUIString('engine_title')}
          </h2>
          <div className="space-y-6">
            <div className="glass-panel p-6 shadow-[0_0_15px_rgba(0,243,255,0.18)]">
              <div className="flex justify-between items-center mb-2">
                <span className="font-label-caps text-label-caps text-warning-orange">FRAMEWORK</span>
                <span className="px-2 py-0.5 bg-tertiary-fixed-dim text-void-black font-label-caps text-[11px] font-bold">ACTIVE</span>
              </div>
              <h3 className="font-headline-md text-headline-md text-primary-container uppercase font-bold">Godot + C# Core</h3>
              <p className="mt-2 text-on-surface-variant leading-relaxed font-body-md text-[14px]">
                {getUIString('engine_desc')}
              </p>
            </div>
          </div>
        </div>
        
        <div className="lg:col-span-7 relative min-h-[350px] md:min-h-[400px]">
          <div 
            className="absolute inset-0 border border-primary-container/30 bg-[url('https://images.unsplash.com/photo-1555949963-ff9fe0c870eb?ixlib=rb-4.0.3&auto=format&fit=crop&w=1000&q=80')] bg-cover bg-center grayscale mix-blend-luminosity opacity-40"
          ></div>
          <div className="absolute inset-0 bg-gradient-to-r from-background to-transparent"></div>
          <div className={`absolute bottom-8 glass-panel p-4 max-w-xs terminal-text font-body-md text-[13px] leading-relaxed ${
            isRtl ? 'left-8' : 'right-8'
          }`}>
            &gt; {getUIString('status_init')}<br />
            &gt; {getUIString('status_shaders')}<br />
            &gt; {getUIString('status_link')}
          </div>
        </div>
      </section>

      {/* Narrative & Localization */}
      <section className="mb-24">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8 md:gap-12">
          {/* Narrative */}
          <div className="glass-panel p-6 md:p-8 relative overflow-hidden">
            <div className={`absolute top-0 opacity-5 pointer-events-none ${isRtl ? 'left-0' : 'right-0'}`}>
              <span className="material-symbols-outlined text-9xl">forum</span>
            </div>
            <h2 className="font-headline-lg text-headline-lg-mobile md:text-headline-lg text-secondary-container uppercase mb-6 drop-shadow-[0_0_10px_rgba(254,0,254,0.5)] font-bold">
              {t('features.tech.narrative_engine', 'Narrative Engine')}
            </h2>
            <p className="mb-6 text-inverse-surface leading-relaxed">
              {getUIString('narrative_desc')}
            </p>
            <ul className="space-y-4 font-body-md text-[14px] text-on-surface-variant">
              <li className="flex items-start gap-3">
                <span className="material-symbols-outlined text-secondary-container mt-0.5">check_box</span>
                <span>Dynamic variable injection for localized context.</span>
              </li>
              <li className="flex items-start gap-3">
                <span className="material-symbols-outlined text-secondary-container mt-0.5">check_box</span>
                <span>Seamless C# integration for realtime UI updates.</span>
              </li>
            </ul>
          </div>
          
          {/* Localization */}
          <div className="glass-panel p-6 md:p-8 border-warning-orange relative overflow-hidden">
            <div className={`absolute w-32 h-32 bg-warning-orange blur-[80px] opacity-15 pointer-events-none ${
              isRtl ? 'left-0 bottom-0' : 'right-0 bottom-0'
            }`}></div>
            <h2 className="font-headline-lg text-headline-lg-mobile md:text-headline-lg text-warning-orange uppercase mb-6 font-bold">
              {t('features.tags.localisation', 'Global Comms')}
            </h2>
            <p className="mb-6 text-inverse-surface leading-relaxed">
              {getUIString('comms_desc')}
            </p>
            
            <div className="bg-surface-container-high p-4 border border-outline-variant/30 font-body-md text-[13px] flex flex-col gap-2.5">
              <div className="flex justify-between items-center text-on-surface-variant">
                <span>EN</span>
                <span className="font-semibold">{t('tech_hub.offline_status', 'System Offline')}</span>
              </div>
              <div className="h-px bg-outline-variant/50 w-full"></div>
              <div className="flex justify-between items-center terminal-text" dir="rtl">
                <span className="text-on-surface-variant text-sm" dir="ltr">AR</span>
                <span className="font-semibold">{t('tech_hub.offline_status', 'System Offline', { lng: 'ar' })}</span>
              </div>
              <div className="h-px bg-outline-variant/50 w-full"></div>
              <div className="flex justify-between items-center text-secondary">
                <span>JA</span>
                <span className="font-semibold">{t('tech_hub.offline_status', 'System Offline', { lng: 'ja' })}</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Localized System Capabilities */}
      <section className="mb-12">
        <h2 className="font-headline-lg text-headline-lg-mobile md:text-headline-lg text-primary-container mb-12 flex items-center gap-4 uppercase">
          <span className="material-symbols-outlined text-primary-container">settings_suggest</span>
          {t('features.briefing', 'System Capabilities')}
        </h2>
        
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {/* Roster */}
          <div className="bg-surface-container/60 brutal-border p-5 border-outline-variant/20 hover:border-primary-container transition-colors">
            <span className="font-label-caps text-[10px] text-primary-container uppercase tracking-widest block mb-2">
              // {t('features.tags.roster', 'ROSTER')}
            </span>
            <h4 className="font-headline-md text-[18px] text-white uppercase mb-2 font-bold leading-tight">
              {t('features.items.roster.title')}
            </h4>
            <p className="font-body-md text-[13px] text-on-surface-variant leading-relaxed">
              {t('features.items.roster.desc')}
            </p>
          </div>

          {/* Narrative */}
          <div className="bg-surface-container/60 brutal-border p-5 border-outline-variant/20 hover:border-primary-container transition-colors">
            <span className="font-label-caps text-[10px] text-primary-container uppercase tracking-widest block mb-2">
              // {t('features.tags.narrative', 'NARRATIVE')}
            </span>
            <h4 className="font-headline-md text-[18px] text-white uppercase mb-2 font-bold leading-tight">
              {t('features.items.story.title')}
            </h4>
            <p className="font-body-md text-[13px] text-on-surface-variant leading-relaxed">
              {t('features.items.story.desc')}
            </p>
          </div>

          {/* Save System */}
          <div className="bg-surface-container/60 brutal-border p-5 border-outline-variant/20 hover:border-primary-container transition-colors">
            <span className="font-label-caps text-[10px] text-primary-container uppercase tracking-widest block mb-2">
              // {t('features.tags.save_system', 'SAVE SYSTEM')}
            </span>
            <h4 className="font-headline-md text-[18px] text-white uppercase mb-2 font-bold leading-tight">
              {t('features.items.save.title')}
            </h4>
            <p className="font-body-md text-[13px] text-on-surface-variant leading-relaxed">
              {t('features.items.save.desc')}
            </p>
          </div>

          {/* Volume / Audio */}
          <div className="bg-surface-container/60 brutal-border p-5 border-outline-variant/20 hover:border-primary-container transition-colors">
            <span className="font-label-caps text-[10px] text-primary-container uppercase tracking-widest block mb-2">
              // {t('features.tags.audio', 'AUDIO')}
            </span>
            <h4 className="font-headline-md text-[18px] text-white uppercase mb-2 font-bold leading-tight">
              {t('features.items.audio.title')}
            </h4>
            <p className="font-body-md text-[13px] text-on-surface-variant leading-relaxed">
              {t('features.items.audio.desc')}
            </p>
          </div>
        </div>
      </section>

    </div>
  );
}
