import React, { Suspense, lazy } from 'react';
import { useTranslation } from 'react-i18next';
import { useLanguage } from '../translations/LanguageContext';

// Phase 5: the forge exhibit is code-split like the other scenes.
const LazyKioskRow = lazy(() => import('../three/KioskRow'));

export default function TechHub() {
  const { t } = useTranslation();
  const { selectedLanguage } = useLanguage();

  const getUIString = (key) => {
    return t(`tech_hub.${key}`, key);
  };

  const isRtl = selectedLanguage === 'arabic' || selectedLanguage === 'hebrew';

  return (
    <div className="pt-24 md:pt-28 pb-24 px-4 md:px-margin-desktop max-w-container-max-width mx-auto w-full">
      
      {/* Header Bar */}
      <header className="mb-12 border-b border-primary-container/20 pb-6 flex flex-col md:flex-row justify-between items-start md:items-end gap-4">
        <div>
          <div className="font-mono text-[11px] text-primary-container tracking-[0.25em] uppercase mb-1 flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-primary-container animate-pulse"></span>
            FACILITY // AXIOM-7 R&D LAB & SYSTEMS MATRIX
          </div>
          <h1 className="font-display-hero text-2xl md:text-4xl text-primary-container uppercase font-extrabold tracking-tight text-glow-cyan">
            {t('features.tech_title', 'Resistance Systems')}
          </h1>
          <p className="font-mono text-[12px] md:text-[13px] text-on-surface-variant mt-2 max-w-3xl leading-relaxed">
            {getUIString('hero_desc')}
          </p>
        </div>

        <div className="font-mono text-[11px] tracking-wider text-right hidden sm:block">
          <div className="text-tertiary-container font-semibold">ENGINE: GODOT 4.7.2 .NET</div>
          <div className="text-on-surface-variant/70">C# RUNTIME // 60+ FPS TARGET</div>
        </div>
      </header>

      {/* Engine Specs & Hardware Deployment Pods */}
      <section className="mb-20">
        <div className="flex items-center gap-3 mb-6">
          <span className="w-2 h-2 bg-primary-container"></span>
          <span className="font-mono text-[11px] text-primary-container uppercase tracking-[0.25em]">
            // HARDWARE COMPATIBILITY & CROSS-PLATFORM ARRAY
          </span>
          <div className="h-px bg-primary-container/20 flex-grow"></div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 lg:gap-8">
          {/* Left: Architecture Specifications */}
          <div className="lg:col-span-5 flex flex-col justify-between bg-surface-container-low/80 border border-primary-container/25 p-6 md:p-8">
            <div>
              <div className="flex justify-between items-center mb-4">
                <span className="font-mono text-[10px] text-warning-orange uppercase tracking-widest border border-warning-orange/40 px-2 py-0.5">
                  FRAMEWORK CORE
                </span>
                <span className="font-mono text-[10px] bg-tertiary-container/15 text-tertiary-container border border-tertiary-container/30 px-2 py-0.5 font-bold">
                  ACTIVE BUILD
                </span>
              </div>

              <h2 className="font-display-hero text-xl md:text-2xl text-inverse-surface uppercase font-bold tracking-wide mb-3 flex items-center gap-2">
                <span className="material-symbols-outlined text-primary-container text-[22px]">terminal</span>
                {t('tech_hub.chassis_sync', 'Chassis Neural-Sync')}
              </h2>

              <p className="font-mono text-[12px] md:text-[13px] text-on-surface-variant leading-relaxed mb-6 border-l-2 border-primary-container/40 pl-4">
                {getUIString('engine_desc')}
              </p>

              <div className="space-y-3 font-mono text-[11px]">
                <div className="bg-void-black/70 border border-outline-variant/30 p-3 flex justify-between items-center">
                  <span className="text-on-surface-variant">PHYSICS TICK:</span>
                  <span className="text-primary-container font-semibold">60 HZ DETERMINISTIC</span>
                </div>
                <div className="bg-void-black/70 border border-outline-variant/30 p-3 flex justify-between items-center">
                  <span className="text-on-surface-variant">RENDER PIPELINE:</span>
                  <span className="text-primary-container font-semibold">VULKAN FORWARD+ / MOBILE</span>
                </div>
                <div className="bg-void-black/70 border border-outline-variant/30 p-3 flex justify-between items-center">
                  <span className="text-on-surface-variant">MEMORY ARCH:</span>
                  <span className="text-tertiary-container font-semibold">ZERO-ALLOC OBJECT POOLING</span>
                </div>
              </div>
            </div>

            <div className="mt-6 pt-4 border-t border-outline-variant/30 font-mono text-[10px] text-primary-container/70 tracking-widest">
              SUBSYSTEM // REACTION VELOCITY VERIFIED
            </div>
          </div>
          
          {/* Right: 3D Platform Kiosks Viewport */}
          <div className="lg:col-span-7 bg-void-black/95 border border-primary-container/30 relative min-h-[380px] md:min-h-[440px] flex flex-col justify-between overflow-hidden">
            <div className="absolute inset-0 bg-[radial-gradient(120%_90%_at_50%_20%,#0b2230_0%,#07141d_55%,#050505_100%)]"></div>
            <div className="absolute inset-0 hud-grid opacity-30 pointer-events-none"></div>

            {/* Corner Markers */}
            <div className="absolute top-2 left-2 w-3 h-3 border-t-2 border-l-2 border-primary-container z-20 pointer-events-none"></div>
            <div className="absolute top-2 right-2 w-3 h-3 border-t-2 border-r-2 border-primary-container z-20 pointer-events-none"></div>
            <div className="absolute bottom-2 left-2 w-3 h-3 border-b-2 border-l-2 border-primary-container z-20 pointer-events-none"></div>
            <div className="absolute bottom-2 right-2 w-3 h-3 border-b-2 border-r-2 border-primary-container z-20 pointer-events-none"></div>

            {/* Viewport Top Header */}
            <div className="relative z-20 p-4 flex justify-between items-center pointer-events-none">
              <span className="font-mono text-[10px] text-primary-container bg-void-black/85 border border-primary-container/40 px-2.5 py-1 tracking-widest">
                KIOSK_ARRAY // WIN · LINUX · DROID · IOS
              </span>
              <span className="font-mono text-[10px] text-tertiary-container bg-void-black/85 border border-tertiary-container/30 px-2 py-0.5">
                HOT_DEPLOY: READY
              </span>
            </div>

            {/* 3D Scene */}
            <div className="relative w-full h-[280px] md:h-[320px]">
              <Suspense fallback={null}>
                <LazyKioskRow />
              </Suspense>
            </div>

            {/* Console Readout Bar */}
            <div className={`relative z-20 p-4 bg-void-black/90 border-t border-primary-container/20 font-mono text-[11px] leading-relaxed text-tertiary-container flex flex-col gap-0.5`}>
              <div>&gt; {getUIString('status_init')}</div>
              <div>&gt; {getUIString('status_shaders')}</div>
              <div className="text-primary-container">&gt; {getUIString('status_link')}</div>
            </div>
          </div>
        </div>
      </section>

      {/* Comms Decrypter & Localization Array */}
      <section className="mb-20">
        <div className="flex items-center gap-3 mb-6">
          <span className="w-2 h-2 bg-secondary-container"></span>
          <span className="font-mono text-[11px] text-secondary-container uppercase tracking-[0.25em]">
            // SECURE TRANSMISSION & LOCALIZATION
          </span>
          <div className="h-px bg-secondary-container/20 flex-grow"></div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 md:gap-8">
          {/* Comms Decrypter */}
          <div className="bg-surface-container-low/80 border border-secondary-container/30 p-6 md:p-8 relative flex flex-col justify-between">
            <div>
              <div className="flex justify-between items-center mb-4">
                <span className="font-mono text-[10px] text-secondary-container uppercase tracking-widest border border-secondary-container/40 px-2 py-0.5">
                  DECRYPTION MODULE
                </span>
                <span className="material-symbols-outlined text-secondary-container text-[20px]">vpn_key</span>
              </div>

              <h2 className="font-display-hero text-xl md:text-2xl text-secondary-container uppercase font-bold tracking-wide mb-3 drop-shadow-[0_0_10px_rgba(254,0,254,0.4)]">
                {t('tech_hub.comms_decrypter', 'Comms Decrypter')}
              </h2>

              <p className="font-mono text-[12px] md:text-[13px] text-on-surface-variant leading-relaxed mb-6">
                {getUIString('narrative_desc')}
              </p>

              <div className="space-y-3 font-mono text-[12px] text-inverse-surface">
                <div className="flex items-start gap-3 bg-void-black/60 p-3 border border-outline-variant/30">
                  <span className="material-symbols-outlined text-secondary-container text-[18px] mt-0.5">check_circle</span>
                  <span>Dynamic variable injection for localized resistance context.</span>
                </div>
                <div className="flex items-start gap-3 bg-void-black/60 p-3 border border-outline-variant/30">
                  <span className="material-symbols-outlined text-secondary-container text-[18px] mt-0.5">check_circle</span>
                  <span>Seamless C# signal integration for realtime UI telemetry.</span>
                </div>
              </div>
            </div>
          </div>
          
          {/* Localization Channels */}
          <div className="bg-surface-container-low/80 border border-warning-orange/30 p-6 md:p-8 relative flex flex-col justify-between">
            <div>
              <div className="flex justify-between items-center mb-4">
                <span className="font-mono text-[10px] text-warning-orange uppercase tracking-widest border border-warning-orange/40 px-2 py-0.5">
                  FREQUENCY DEMODULATION
                </span>
                <span className="material-symbols-outlined text-warning-orange text-[20px]">language</span>
              </div>

              <h2 className="font-display-hero text-xl md:text-2xl text-warning-orange uppercase font-bold tracking-wide mb-3">
                {t('features.tags.localisation', 'Global Comms')}
              </h2>

              <p className="font-mono text-[12px] md:text-[13px] text-on-surface-variant leading-relaxed mb-6">
                {getUIString('comms_desc')}
              </p>

              {/* Channel Monitor */}
              <div className="bg-void-black/80 border border-outline-variant/40 p-4 font-mono text-[12px] flex flex-col gap-3">
                <div className="flex justify-between items-center text-on-surface">
                  <span className="text-primary-container font-semibold">[CH_01 // EN]</span>
                  <span className="text-tertiary-container font-semibold">SIGNAL SYNCHRONIZED</span>
                </div>
                <div className="h-px bg-outline-variant/30 w-full"></div>
                <div className="flex justify-between items-center text-on-surface" dir="rtl">
                  <span className="text-tertiary-container font-semibold">{t('tech_hub.offline_status', 'System Offline', { lng: 'ar' })}</span>
                  <span className="text-primary-container font-semibold" dir="ltr">[CH_02 // AR]</span>
                </div>
                <div className="h-px bg-outline-variant/30 w-full"></div>
                <div className="flex justify-between items-center text-on-surface">
                  <span className="text-primary-container font-semibold">[CH_03 // JA]</span>
                  <span className="text-secondary font-semibold">{t('tech_hub.offline_status', 'System Offline', { lng: 'ja' })}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Subsystem Capabilities Grid */}
      <section>
        <div className="flex items-center gap-3 mb-6">
          <span className="w-2 h-2 bg-primary-container"></span>
          <span className="font-mono text-[11px] text-primary-container uppercase tracking-[0.25em]">
            // SUB-SYSTEM ARCHITECTURE SPECIFICATIONS
          </span>
          <div className="h-px bg-primary-container/20 flex-grow"></div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {/* Roster */}
          <div className="bg-surface-container-low/80 border border-primary-container/25 hover:border-primary-container p-5 transition-all flex flex-col justify-between">
            <div>
              <span className="font-mono text-[10px] text-primary-container uppercase tracking-widest block mb-2">
                // {t('features.tags.roster', 'ROSTER')}
              </span>
              <h4 className="font-display-hero text-[16px] text-white uppercase mb-2 font-bold leading-tight">
                {t('features.items.roster.title')}
              </h4>
              <p className="font-mono text-[12px] text-on-surface-variant leading-relaxed">
                {t('features.items.roster.desc')}
              </p>
            </div>
            <div className="mt-4 pt-3 border-t border-outline-variant/20 font-mono text-[10px] text-primary-container/60">
              MODULE // 01_SQUAD
            </div>
          </div>

          {/* Narrative */}
          <div className="bg-surface-container-low/80 border border-primary-container/25 hover:border-primary-container p-5 transition-all flex flex-col justify-between">
            <div>
              <span className="font-mono text-[10px] text-primary-container uppercase tracking-widest block mb-2">
                // {t('features.tags.narrative', 'NARRATIVE')}
              </span>
              <h4 className="font-display-hero text-[16px] text-white uppercase mb-2 font-bold leading-tight">
                {t('features.items.story.title')}
              </h4>
              <p className="font-mono text-[12px] text-on-surface-variant leading-relaxed">
                {t('features.items.story.desc')}
              </p>
            </div>
            <div className="mt-4 pt-3 border-t border-outline-variant/20 font-mono text-[10px] text-primary-container/60">
              MODULE // 02_STORY
            </div>
          </div>

          {/* Save System */}
          <div className="bg-surface-container-low/80 border border-primary-container/25 hover:border-primary-container p-5 transition-all flex flex-col justify-between">
            <div>
              <span className="font-mono text-[10px] text-primary-container uppercase tracking-widest block mb-2">
                // {t('features.tags.save_system', 'SAVE SYSTEM')}
              </span>
              <h4 className="font-display-hero text-[16px] text-white uppercase mb-2 font-bold leading-tight">
                {t('features.items.save.title')}
              </h4>
              <p className="font-mono text-[12px] text-on-surface-variant leading-relaxed">
                {t('features.items.save.desc')}
              </p>
            </div>
            <div className="mt-4 pt-3 border-t border-outline-variant/20 font-mono text-[10px] text-primary-container/60">
              MODULE // 03_STATE
            </div>
          </div>

          {/* Volume / Audio */}
          <div className="bg-surface-container-low/80 border border-primary-container/25 hover:border-primary-container p-5 transition-all flex flex-col justify-between">
            <div>
              <span className="font-mono text-[10px] text-primary-container uppercase tracking-widest block mb-2">
                // {t('features.tags.audio', 'AUDIO')}
              </span>
              <h4 className="font-display-hero text-[16px] text-white uppercase mb-2 font-bold leading-tight">
                {t('features.items.audio.title')}
              </h4>
              <p className="font-mono text-[12px] text-on-surface-variant leading-relaxed">
                {t('features.items.audio.desc')}
              </p>
            </div>
            <div className="mt-4 pt-3 border-t border-outline-variant/20 font-mono text-[10px] text-primary-container/60">
              MODULE // 04_AUDIO
            </div>
          </div>
        </div>
      </section>

    </div>
  );
}
