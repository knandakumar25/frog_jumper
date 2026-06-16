import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useLanguage } from '../translations/LanguageContext';


export default function Gameplay() {
  const { t } = useTranslation();
  const { selectedLanguage } = useLanguage();
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

  return (
    <div className="pt-32 pb-24 max-w-container-max-width mx-auto w-full">
      
      {/* Hero Header */}
      <section className="px-4 md:px-margin-desktop mb-16 relative">
        <div className={`absolute top-0 text-tertiary-container opacity-10 font-display-hero text-display-hero pointer-events-none select-none ${
          isRtl ? '-right-8' : '-left-8'
        }`}>
          SYS.REQ //
        </div>
        <h1 className="font-display-hero text-headline-lg-mobile md:text-display-hero text-primary-container mb-4 uppercase tracking-tighter drop-shadow-[0_0_12px_rgba(0,243,255,0.5)] font-extrabold">
          {t('levels.title_prefix', 'The ')}
          <span className="text-secondary-container drop-shadow-[0_0_12px_rgba(254,0,254,0.5)]">
            {t('levels.title_highlight', 'Mission')}
          </span>
          {t('levels.title_suffix', ' Awaits')}
        </h1>
        <p className={`font-body-lg text-body-lg text-on-surface-variant max-w-3xl border-warning-orange pl-6 ${
          isRtl ? 'border-r-4 pr-6 pl-0 text-right' : 'border-l-4 pl-6 pr-0 text-left'
        }`}>
          {t('levels.desc', 'Five unique levels with increasing difficulty. Each mission unlocks progressively as you push back the Xelthara occupation.')}
        </p>
      </section>

      <div className="glitch-divider"></div>

      {/* Los Angeles 2157 Briefing */}
      <section className="px-4 md:px-margin-desktop mb-24 relative">
        <div className="relative h-[400px] md:h-[500px] bg-surface-container-low border border-outline-variant overflow-hidden">
          <div 
            className="absolute inset-0 bg-cover bg-center opacity-50 mix-blend-luminosity" 
            style={{ 
              backgroundImage: "url('https://lh3.googleusercontent.com/aida-public/AB6AXuCko4QlFnMYn_IxApK2GUqJ01AVB0Lr-8x_okm6OI93q6yYfh0KJqPPj7o6JP-t0vT6XL-FypaEKtHO5ussFrWHZ6vhMPJMBvVuT7IGqSKApvibZ7Pd3v6qkuxKEP3Cp1NCBpe0pJQBKxhERZhDUOOvAXdSDYO0-RXEX71hxmNKz23lCOY-qCikG3d5RwpnY9T6te6lASAB9J2w8v1gSBowlMfMWrIXqO965ms9xdYKaR1SjarOJd6rjWUM6xKZ3SwXExb_XflFqBQ')" 
            }}
          ></div>
          <div className="bracket-tl"></div><div className="bracket-tr"></div><div className="bracket-bl"></div><div className="bracket-br"></div>
          
          <div className="absolute inset-0 bg-gradient-to-t from-void-black to-transparent z-10"></div>
          <div className="relative z-20 h-full flex flex-col justify-end p-6 md:p-12">
            <div className="inline-block bg-tertiary-fixed-dim text-on-tertiary px-3 py-1 font-label-caps text-label-caps mb-4 w-fit">
              {t('gameplay.intel_classified', 'INTEL_CLASSIFIED // L.A. QZ')}
            </div>
            <h2 className="font-display-hero text-headline-lg md:text-[54px] text-primary-container mb-4 uppercase drop-shadow-[0_0_12px_rgba(0,243,255,0.4)]">
              {t('gameplay.los_angeles_2157', 'LOS ANGELES 2157')}
            </h2>
            <p className="font-body-lg text-[14px] md:text-body-lg text-on-surface max-w-2xl bg-void-black/80 backdrop-blur-sm p-4 border-l-4 border-warning-orange leading-relaxed">
              {t('story.caption', 'The Xelthara arrived not to conquer, but to harvest. The once-sprawling metropolis is now Sector 4—a heavily militarized quarantine zone where human survivors scavenge beneath alien atmospheric processors.')}
            </p>
          </div>
        </div>
      </section>

      {/* Mechanics Grid */}
      <section className="px-4 md:px-margin-desktop mb-32">
        <h2 className="font-headline-lg text-headline-lg-mobile md:text-headline-lg text-secondary mb-12 flex items-center gap-4 uppercase font-bold tracking-tight">
          <span className="material-symbols-outlined text-secondary" style={{ fontVariationSettings: "'FILL' 1" }}>terminal</span>
          {getUIString('protocols')}
        </h2>
        
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8 md:gap-gutter">
          {/* Protocol 1 */}
          <div className="bg-surface-container-low p-6 relative border border-primary-container/30 hover:border-primary-container transition-colors group">
            <div className="bracket-tl"></div><div className="bracket-br"></div>
            <div className="flex justify-between items-start mb-6">
              <span className="material-symbols-outlined text-primary-container text-4xl group-hover:animate-pulse">speed</span>
              <div className="bg-void-black text-warning-orange font-label-caps text-label-caps px-2.5 py-1 border border-warning-orange font-bold">
                {getUIString('active')}
              </div>
            </div>
            <h3 className="font-headline-md text-headline-md text-on-surface mb-2">{getUIString('t1_title')}</h3>
            <p className="font-body-md text-body-md text-on-surface-variant leading-relaxed">
              {getUIString('t1_desc')}
            </p>
          </div>

          {/* Protocol 2 */}
          <div className="bg-surface-container-low p-6 relative border border-secondary/30 hover:border-secondary transition-colors group">
            <div className="bracket-tl"></div><div className="bracket-br"></div>
            <div className="flex justify-between items-start mb-6">
              <span className="material-symbols-outlined text-secondary text-4xl group-hover:animate-pulse">memory</span>
              <div className="bg-void-black text-secondary font-label-caps text-label-caps px-2.5 py-1 border border-secondary font-bold">
                {getUIString('synced')}
              </div>
            </div>
            <h3 className="font-headline-md text-headline-md text-on-surface mb-2">{getUIString('t2_title')}</h3>
            <p className="font-body-md text-body-md text-on-surface-variant leading-relaxed">
              {getUIString('t2_desc')}
            </p>
          </div>

          {/* Protocol 3 */}
          <div className="bg-surface-container-low p-6 relative border border-tertiary-fixed-dim/30 hover:border-tertiary-fixed-dim transition-colors group">
            <div className="bracket-tl"></div><div className="bracket-br"></div>
            <div className="flex justify-between items-start mb-6">
              <span className="material-symbols-outlined text-tertiary-fixed-dim text-4xl group-hover:animate-pulse">join_full</span>
              <div className="bg-void-black text-tertiary-fixed-dim font-label-caps text-label-caps px-2.5 py-1 border border-tertiary-fixed-dim font-bold">
                {getUIString('online')}
              </div>
            </div>
            <h3 className="font-headline-md text-headline-md text-on-surface mb-2">{getUIString('t3_title')}</h3>
            <p className="font-body-md text-body-md text-on-surface-variant leading-relaxed">
              {getUIString('t3_desc')}
            </p>
          </div>
        </div>
      </section>

      {/* Campaign Database Terminal */}
      <section className="px-4 md:px-margin-desktop mb-12">
        <h2 className="font-headline-lg text-headline-lg-mobile md:text-headline-lg text-primary-container mb-12 flex items-center gap-4 uppercase drop-shadow-[0_0_8px_rgba(0,243,255,0.3)]">
          <span className="material-symbols-outlined text-primary-container">database</span>
          {t('levels.title_highlight')} {getUIString('terminal')}
        </h2>
        
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 lg:gap-gutter">
          {/* Level List */}
          <div className="lg:col-span-5 flex flex-col gap-3">
            {levelsData.map(lvl => (
              <button
                key={lvl.id}
                onClick={() => setSelectedLevelId(lvl.id)}
                className={`w-full text-left p-4 brutal-border flex justify-between items-center transition-all ${
                  selectedLevelId === lvl.id
                    ? 'bg-primary-container text-void-black border-primary-container'
                    : 'bg-surface-container hover:bg-surface-container-high border-outline-variant/30 text-on-surface'
                }`}
              >
                <div className="flex flex-col">
                  <span className="font-label-caps text-[11px] opacity-70 tracking-wider">
                    {lvl.code}
                  </span>
                  <span className="font-headline-md text-[18px] md:text-headline-md font-bold uppercase">
                    {t(`levels.${lvl.key}.name`)}
                  </span>
                </div>
                <span className="font-headline-lg text-lg">
                  {t('levels.select_arrow', '›')}
                </span>
              </button>
            ))}
          </div>

          {/* Level Details Panel */}
          <div className="lg:col-span-7 bg-surface-container-low brutal-border p-6 md:p-8 flex flex-col justify-between min-h-[350px] relative overflow-hidden">
            <div className="absolute top-0 right-0 p-4 opacity-5 pointer-events-none">
              <span className="material-symbols-outlined text-9xl">terminal</span>
            </div>
            
            {(() => {
              const activeLevel = levelsData[selectedLevelId];
              return (
                <div className="flex flex-col gap-6 h-full justify-between">
                  <div>
                    {/* Header */}
                    <div className="flex flex-wrap items-center gap-3 mb-4">
                      <span className="bg-warning-orange text-void-black font-label-caps text-[11px] px-2 py-0.5 font-bold uppercase">
                        {t('levels.threat_title', 'THREAT:')} {t(`levels.threats.${activeLevel.threat}`, activeLevel.threatVal)}
                      </span>
                      <span className="bg-deep-grey border border-outline-variant/30 text-on-surface-variant font-label-caps text-[11px] px-2 py-0.5 uppercase">
                        {t('levels.difficulty_title', 'TYPE:')} {t(`levels.types.${activeLevel.type}`, activeLevel.type.toUpperCase())}
                      </span>
                    </div>

                    <h3 className="font-headline-lg text-[26px] md:text-[32px] text-primary-container uppercase font-extrabold mb-1">
                      {t(`levels.${activeLevel.key}.name`)}
                    </h3>
                    <h4 className="font-label-caps text-label-caps text-secondary-fixed-dim uppercase tracking-widest mb-6">
                      // {t(`levels.${activeLevel.key}.subtitle`)}
                    </h4>

                    <p className="font-body-md text-body-md text-on-surface-variant leading-relaxed border-l-2 border-primary-container/30 pl-4">
                      {t(`levels.${activeLevel.key}.description`)}
                    </p>
                  </div>

                  {/* Objectives */}
                  <div className="mt-8 bg-surface-container-lowest p-4 brutal-border border-outline-variant/20">
                    <h5 className="font-label-caps text-[12px] text-tertiary-fixed-dim uppercase tracking-wider mb-3 flex items-center gap-2">
                      <span className="material-symbols-outlined text-[16px]">checklist</span>
                      {t('levels.objectives_title', 'MISSION OBJECTIVES')}
                    </h5>
                    <ul className="space-y-2 font-body-md text-[13px] text-inverse-surface">
                      <li className="flex items-start gap-2.5">
                        <span className="text-primary-container font-semibold">&gt;</span>
                        <span>{t(`levels.${activeLevel.key}.obj_0`)}</span>
                      </li>
                      <li className="flex items-start gap-2.5">
                        <span className="text-primary-container font-semibold">&gt;</span>
                        <span>{t(`levels.${activeLevel.key}.obj_1`)}</span>
                      </li>
                      <li className="flex items-start gap-2.5">
                        <span className="text-primary-container font-semibold">&gt;</span>
                        <span>{t(`levels.${activeLevel.key}.obj_2`)}</span>
                      </li>
                    </ul>
                  </div>
                </div>
              );
            })()}
          </div>
        </div>
      </section>

    </div>
  );
}
