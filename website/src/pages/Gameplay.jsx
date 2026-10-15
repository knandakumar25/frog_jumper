import React, { Suspense, lazy, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useLanguage } from '../translations/LanguageContext';

// Phase 4: the briefing vista (code-split; the page's copy never waits on it).
const IntelBackdrop = lazy(() => import('../three/IntelBackdrop'));

export default function Gameplay() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);
  const [selectedLevelId, setSelectedLevelId] = useState(0);

  const levelsData = [
    { id: 0, key: 'level_0', threat: 'minimal', type: 'training', threatVal: 'MINIMAL', code: 'PROT_000' },
    { id: 1, key: 'level_1', threat: 'high', type: 'infiltration', threatVal: 'HIGH', code: 'SECT_001' },
    { id: 2, key: 'level_2', threat: 'omega', type: 'defense', threatVal: 'OMEGA', code: 'BRIDGE_002' },
    { id: 3, key: 'level_3', threat: 'stealth', type: 'stealth', threatVal: 'CRITICAL', code: 'VOID_003' },
    { id: 4, key: 'level_4', threat: 'critical', type: 'assault', threatVal: 'CRITICAL', code: 'ORB_004' },
  ];

  const getUIString = (key) => {
    return t(`gameplay.${key}`, key);
  };

  const isRtl = selectedLanguage === 'arabic' || selectedLanguage === 'hebrew';
  const activeLevel = levelsData[selectedLevelId] || levelsData[0];

  return (
    <div className="pt-24 md:pt-28 pb-24 px-4 md:px-margin-desktop max-w-container-max-width mx-auto w-full">
      
      {/* Header Bar */}
      <header className="mb-12 border-b border-primary-container/20 pb-6 flex flex-col md:flex-row justify-between items-start md:items-end gap-4">
        <div>
          <div className="font-mono text-[11px] text-primary-container tracking-[0.25em] uppercase mb-1 flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-secondary-container animate-pulse"></span>
            SECTOR // COMBAT MISSIONS & CAMPAIGN DATABASE
          </div>
          <h1 className="font-display-hero text-2xl md:text-4xl text-primary-container uppercase font-extrabold tracking-tight text-glow-cyan">
            {t('levels.title_prefix', 'The ')}
            <span className="text-secondary-container drop-shadow-[0_0_12px_rgba(254,0,254,0.6)]">
              {t('levels.title_highlight', 'Mission')}
            </span>
            {t('levels.title_suffix', ' Awaits')}
          </h1>
          <p className="font-mono text-[12px] md:text-[13px] text-on-surface-variant mt-2 max-w-3xl leading-relaxed">
            {t('levels.desc', 'Five unique levels with increasing difficulty. Each mission unlocks progressively as you push back the Xelthara occupation.')}
          </p>
        </div>

        <div className="font-mono text-[11px] tracking-wider text-right hidden sm:block">
          <div className="text-warning-orange font-semibold">CLEARANCE: LEVEL 4</div>
          <div className="text-on-surface-variant/70">5/5 THEATERS CATALOGED</div>
        </div>
      </header>

      {/* Los Angeles 2157 Surveillance Vista */}
      <section className="mb-20">
        <div className="flex items-center gap-3 mb-4">
          <span className="w-2 h-2 bg-warning-orange"></span>
          <span className="font-mono text-[11px] text-warning-orange uppercase tracking-[0.25em]">
            // LIVE RECONNAISSANCE SURVEILLANCE
          </span>
          <div className="h-px bg-warning-orange/20 flex-grow"></div>
        </div>

        <div className="relative min-h-[420px] md:min-h-[500px] bg-void-black border border-primary-container/30 overflow-hidden flex flex-col justify-between">
          <div className="absolute inset-0 bg-[radial-gradient(120%_90%_at_50%_85%,#0c2634_0%,#07141d_55%,#050505_100%)]"></div>
          
          <Suspense fallback={null}>
            <IntelBackdrop />
          </Suspense>

          {/* Corner Markers */}
          <div className="absolute top-2 left-2 w-3 h-3 border-t-2 border-l-2 border-primary-container z-20"></div>
          <div className="absolute top-2 right-2 w-3 h-3 border-t-2 border-r-2 border-primary-container z-20"></div>
          <div className="absolute bottom-2 left-2 w-3 h-3 border-b-2 border-l-2 border-primary-container z-20"></div>
          <div className="absolute bottom-2 right-2 w-3 h-3 border-b-2 border-r-2 border-primary-container z-20"></div>

          {/* Top HUD Camera Readout */}
          <div className="relative z-20 p-4 md:p-6 flex justify-between items-center bg-gradient-to-b from-void-black/80 to-transparent font-mono text-[10px] md:text-[11px] tracking-widest text-primary-container">
            <div className="flex items-center gap-2">
              <span className="w-2.5 h-2.5 rounded-full bg-error-container animate-ping"></span>
              <span className="text-white font-bold">REC ● 00:21:57:04</span>
            </div>
            <div className="hidden sm:flex items-center gap-4 text-on-surface-variant">
              <span>LAT: 34.0522° N</span>
              <span>LON: 118.2437° W</span>
              <span className="text-primary-container">ALT: 480M [UAV_04]</span>
            </div>
          </div>

          {/* Center Tactical Crosshair */}
          <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 pointer-events-none opacity-40 z-10">
            <div className="w-16 h-16 border border-primary-container/50 rounded-full flex items-center justify-center">
              <div className="w-2 h-2 bg-primary-container rounded-full"></div>
            </div>
          </div>

          {/* Bottom Briefing Overlay */}
          <div className="relative z-20 p-6 md:p-10 bg-gradient-to-t from-void-black via-void-black/90 to-transparent">
            <div className="inline-flex items-center gap-2 bg-tertiary-container/15 text-tertiary-container border border-tertiary-container/30 px-3 py-1 font-mono text-[10px] mb-3 uppercase tracking-widest font-bold">
              <span className="w-1.5 h-1.5 bg-tertiary-container"></span>
              {t('gameplay.intel_classified', 'INTEL_CLASSIFIED // L.A. QZ')}
            </div>
            
            <h2 className="font-display-hero text-2xl md:text-4xl text-primary-container mb-3 uppercase font-extrabold tracking-wide text-glow-cyan">
              {t('gameplay.los_angeles_2157', 'LOS ANGELES 2157')}
            </h2>
            
            <p className="font-mono text-[12px] md:text-[13px] text-on-surface max-w-3xl bg-void-black/85 backdrop-blur-md p-4 border-l-2 border-warning-orange leading-relaxed shadow-lg">
              {t('story.caption', 'The Xelthara arrived not to conquer, but to harvest. The once-sprawling metropolis is now Sector 4—a heavily militarized quarantine zone where human survivors scavenge beneath alien atmospheric processors.')}
            </p>
          </div>
        </div>
      </section>

      {/* Combat Protocols Grid */}
      <section className="mb-20">
        <div className="flex items-center gap-3 mb-6">
          <span className="w-2 h-2 bg-secondary-container"></span>
          <span className="font-mono text-[11px] text-secondary-container uppercase tracking-[0.25em]">
            // TACTICAL COMBAT DOCTRINES
          </span>
          <div className="h-px bg-secondary-container/20 flex-grow"></div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {/* Protocol 1 */}
          <div className="bg-surface-container-low/80 border border-primary-container/30 hover:border-primary-container p-6 transition-all relative flex flex-col justify-between group">
            <div>
              <div className="flex justify-between items-start mb-4">
                <span className="material-symbols-outlined text-primary-container text-3xl group-hover:scale-110 transition-transform">
                  speed
                </span>
                <span className="bg-void-black text-warning-orange font-mono text-[10px] px-2 py-0.5 border border-warning-orange/40 font-bold uppercase tracking-wider">
                  {getUIString('active')}
                </span>
              </div>
              <h3 className="font-display-hero text-lg text-inverse-surface mb-2 font-bold uppercase tracking-wide group-hover:text-primary-container transition-colors">
                {getUIString('t1_title')}
              </h3>
              <p className="font-mono text-[12px] text-on-surface-variant leading-relaxed">
                {getUIString('t1_desc')}
              </p>
            </div>
            <div className="mt-6 pt-3 border-t border-outline-variant/30 font-mono text-[10px] text-primary-container/70 tracking-widest">
              DOCTRINE // KINETIC BREAK
            </div>
          </div>

          {/* Protocol 2 */}
          <div className="bg-surface-container-low/80 border border-secondary/30 hover:border-secondary p-6 transition-all relative flex flex-col justify-between group">
            <div>
              <div className="flex justify-between items-start mb-4">
                <span className="material-symbols-outlined text-secondary text-3xl group-hover:scale-110 transition-transform">
                  memory
                </span>
                <span className="bg-void-black text-secondary font-mono text-[10px] px-2 py-0.5 border border-secondary/40 font-bold uppercase tracking-wider">
                  {getUIString('synced')}
                </span>
              </div>
              <h3 className="font-display-hero text-lg text-inverse-surface mb-2 font-bold uppercase tracking-wide group-hover:text-secondary transition-colors">
                {getUIString('t2_title')}
              </h3>
              <p className="font-mono text-[12px] text-on-surface-variant leading-relaxed">
                {getUIString('t2_desc')}
              </p>
            </div>
            <div className="mt-6 pt-3 border-t border-outline-variant/30 font-mono text-[10px] text-secondary/70 tracking-widest">
              DOCTRINE // REACTION SHIFT
            </div>
          </div>

          {/* Protocol 3 */}
          <div className="bg-surface-container-low/80 border border-tertiary-container/30 hover:border-tertiary-container p-6 transition-all relative flex flex-col justify-between group">
            <div>
              <div className="flex justify-between items-start mb-4">
                <span className="material-symbols-outlined text-tertiary-container text-3xl group-hover:scale-110 transition-transform">
                  join_full
                </span>
                <span className="bg-void-black text-tertiary-container font-mono text-[10px] px-2 py-0.5 border border-tertiary-container/40 font-bold uppercase tracking-wider">
                  {getUIString('online')}
                </span>
              </div>
              <h3 className="font-display-hero text-lg text-inverse-surface mb-2 font-bold uppercase tracking-wide group-hover:text-tertiary-container transition-colors">
                {getUIString('t3_title')}
              </h3>
              <p className="font-mono text-[12px] text-on-surface-variant leading-relaxed">
                {getUIString('t3_desc')}
              </p>
            </div>
            <div className="mt-6 pt-3 border-t border-outline-variant/30 font-mono text-[10px] text-tertiary-container/70 tracking-widest">
              DOCTRINE // SQUAD RESONANCE
            </div>
          </div>
        </div>
      </section>

      {/* Campaign Database Terminal */}
      <section>
        <div className="flex items-center gap-3 mb-6">
          <span className="w-2 h-2 bg-primary-container"></span>
          <span className="font-mono text-[11px] text-primary-container uppercase tracking-[0.25em]">
            // CAMPAIGN OPERATIONS THEATER
          </span>
          <div className="h-px bg-primary-container/20 flex-grow"></div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 lg:gap-8">
          {/* Level List */}
          <div className="lg:col-span-5 flex flex-col gap-2.5">
            {levelsData.map(lvl => {
              const isSelected = selectedLevelId === lvl.id;
              return (
                <button
                  key={lvl.id}
                  onClick={() => setSelectedLevelId(lvl.id)}
                  className={`w-full text-left p-4 border transition-all duration-150 flex justify-between items-center ${
                    isSelected
                      ? 'bg-primary-container/15 border-primary-container text-primary-container shadow-[0_0_15px_rgba(0,243,255,0.15)]'
                      : 'bg-surface-container-low/60 border-outline-variant/30 hover:border-primary-container/40 text-on-surface'
                  }`}
                >
                  <div className="flex flex-col">
                    <span className="font-mono text-[10px] tracking-widest opacity-60">
                      {lvl.code}
                    </span>
                    <span className="font-display-hero text-[15px] md:text-[16px] font-bold uppercase tracking-wider">
                      {t(`levels.${lvl.key}.name`)}
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className={`font-mono text-[9px] px-1.5 py-0.5 font-bold ${
                      lvl.threat === 'minimal' ? 'bg-tertiary-container/20 text-tertiary-container' :
                      lvl.threat === 'high' ? 'bg-warning-orange/20 text-warning-orange' :
                      lvl.threat === 'omega' ? 'bg-error-container text-white' :
                      'bg-secondary-container/20 text-secondary-container'
                    }`}>
                      {lvl.threatVal}
                    </span>
                    <span className="font-mono text-lg opacity-60">›</span>
                  </div>
                </button>
              );
            })}
          </div>

          {/* Level Details Panel */}
          <div className="lg:col-span-7 bg-surface-container-low/90 border border-primary-container/30 p-6 md:p-8 flex flex-col justify-between min-h-[380px] relative">
            <div>
              {/* Header Badges */}
              <div className="flex flex-wrap items-center gap-2 mb-4">
                <span className="bg-warning-orange text-void-black font-mono text-[10px] px-2.5 py-0.5 font-bold uppercase tracking-wider">
                  {t('levels.threat_title', 'THREAT:')} {t(`levels.threats.${activeLevel.threat}`, activeLevel.threatVal)}
                </span>
                <span className="bg-void-black border border-outline-variant/40 text-on-surface-variant font-mono text-[10px] px-2 py-0.5 uppercase tracking-wider">
                  {t('levels.difficulty_title', 'TYPE:')} {t(`levels.types.${activeLevel.type}`, activeLevel.type.toUpperCase())}
                </span>
                <span className="font-mono text-[10px] text-primary-container/70 ml-auto">
                  {activeLevel.code}
                </span>
              </div>

              <h3 className="font-display-hero text-2xl md:text-3xl text-primary-container uppercase font-extrabold tracking-wide mb-1 text-glow-cyan">
                {t(`levels.${activeLevel.key}.name`)}
              </h3>
              <h4 className="font-mono text-[11px] text-secondary-fixed-dim uppercase tracking-widest mb-4">
                // {t(`levels.${activeLevel.key}.subtitle`)}
              </h4>

              <p className="font-mono text-[12px] md:text-[13px] text-on-surface-variant leading-relaxed border-l-2 border-primary-container/40 pl-4 mb-6">
                {t(`levels.${activeLevel.key}.description`)}
              </p>
            </div>

            {/* Objectives */}
            <div className="bg-void-black/85 border border-primary-container/20 p-5">
              <h5 className="font-mono text-[11px] text-tertiary-container uppercase tracking-wider mb-3 flex items-center gap-2 font-bold">
                <span className="material-symbols-outlined text-[16px]">task_alt</span>
                {t('levels.objectives_title', 'MISSION OBJECTIVES')}
              </h5>
              <ul className="space-y-2.5 font-mono text-[12px] text-inverse-surface">
                <li className="flex items-start gap-2.5">
                  <span className="text-primary-container font-bold">&gt;</span>
                  <span>{t(`levels.${activeLevel.key}.obj_0`)}</span>
                </li>
                <li className="flex items-start gap-2.5">
                  <span className="text-primary-container font-bold">&gt;</span>
                  <span>{t(`levels.${activeLevel.key}.obj_1`)}</span>
                </li>
                <li className="flex items-start gap-2.5">
                  <span className="text-primary-container font-bold">&gt;</span>
                  <span>{t(`levels.${activeLevel.key}.obj_2`)}</span>
                </li>
              </ul>

              <div className="mt-5 pt-4 border-t border-outline-variant/30 flex justify-end">
                <button
                  onClick={() => navigate(`/${currentLocale}/squad`)}
                  className="cyber-cut bg-primary-container text-void-black font-headline-md text-[13px] px-6 py-2.5 uppercase font-bold hover:bg-secondary-container hover:text-white transition-all flex items-center gap-2"
                >
                  <span className="material-symbols-outlined text-[16px]">rocket_launch</span>
                  DEPLOY SQUAD TO THEATER
                </button>
              </div>
            </div>
          </div>
        </div>
      </section>

    </div>
  );
}
