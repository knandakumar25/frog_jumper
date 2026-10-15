import React, { Suspense, lazy, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useLanguage } from '../translations/LanguageContext';

// Phase 2: the podium hall is code-split like the home world, so the Squad
// page's text/content renders immediately and three.js loads after.
const LazySquadHall = lazy(() => import('../three/SquadHall'));

export default function Squad() {
  const { t } = useTranslation();
  const { selectedLanguage } = useLanguage();
  const [activeCharId, setActiveCharId] = useState('shogun');
  const [deployStatus, setDeployStatus] = useState(null);

  const characterRoster = {
    shogun: {
      role: 'characters.warriors.shogun.role',
      roleDefault: 'CYBERNETIC SHOGUN WARRIOR',
      descKey: 'characters.warriors.shogun.desc',
      descDefault: 'A master cybernetic warrior from Neo-Tokyo, armed with an energy katana. Combines traditional bushido with futuristic military tech.',
      loreKey: 'characters.warriors.shogun.lore',
      stats: { firepower: 92, defense: 90, mobility: 88 },
      model: '/models/shogun.glb',
      status: 'ACTIVE',
      civilian: 'Kaito Kurogane',
      icons: ['sword', 'bolt']
    },
    oceanus: {
      role: 'characters.warriors.oceanus.role',
      roleDefault: 'HYDROKINETIC PLASMA MANIPULATOR',
      descKey: 'characters.warriors.oceanus.desc',
      descDefault: 'Kiana Vaea manipulates liquid tides and oceanic pressure waves, controlling the battlefield with high-velocity hydro-jets.',
      loreKey: 'characters.warriors.oceanus.lore',
      stats: { firepower: 78, defense: 74, mobility: 91 },
      model: '/models/oceanus.glb',
      status: 'STANDBY',
      civilian: 'Kiana Vaea',
      icons: ['water', 'visibility_off']
    },
    surya: {
      role: 'characters.warriors.surya.role',
      roleDefault: 'COSMIC SOLAR VANGUARD',
      descKey: 'characters.warriors.surya.desc',
      descDefault: 'Arjun Varma channels cosmic fusion energy to manipulate gravimetric fields and release heavy AoE solar pulses.',
      loreKey: 'characters.warriors.surya.lore',
      stats: { firepower: 99, defense: 88, mobility: 75 },
      model: '/models/surya.glb',
      status: 'STANDBY',
      civilian: 'Arjun Varma',
      icons: ['sunny', 'bolt']
    },
    mirage: {
      role: 'characters.warriors.mirage.role',
      roleDefault: 'CRYSTALLINE PHANTOM',
      descKey: 'characters.warriors.mirage.desc',
      descDefault: 'Zayd Al-Mansoor is an elite Saudi operative utilizing crystal refractions to create light-bending phantoms and slash through enemies with dual sayf blades.',
      loreKey: 'characters.warriors.mirage.lore',
      stats: { firepower: 82, defense: 58, mobility: 98 },
      model: '/models/mirage.glb',
      status: 'STANDBY',
      civilian: 'Zayd Al-Mansoor (Saudi Arabia)',
      icons: ['diamond', 'visibility_off']
    },
    apex: {
      role: 'characters.warriors.apex.role',
      roleDefault: 'ELECTRO-MAGNETIC STORM VANGUARD',
      descKey: 'characters.warriors.apex.desc',
      descDefault: 'King Tariq Nzinga is a powerhouse who channels electro-magnetic storm clouds, crashing down with a devastating storm-shield leap.',
      loreKey: 'characters.warriors.apex.lore',
      stats: { firepower: 95, defense: 85, mobility: 93 },
      model: '/models/apex.glb',
      status: 'STANDBY',
      civilian: 'King Tariq Nzinga',
      icons: ['electric_bolt', 'shield']
    },
    phoenicia: {
      role: 'characters.warriors.phoenicia.role',
      roleDefault: 'ULTRASONIC PRISM WEAVER',
      descKey: 'characters.warriors.phoenicia.desc',
      descDefault: 'Maya Khoury hails from Byblos, Lebanon. Harnessing ancient Phoenician acoustic physics and resonant prism harmonics, she weaves ultrasonic shockwaves that stagger hostiles and deflect kinetic energy.',
      loreKey: 'characters.warriors.phoenicia.lore',
      stats: { firepower: 86, defense: 78, mobility: 96 },
      model: '/models/phoenicia.glb',
      status: 'STANDBY',
      civilian: 'Maya Khoury (Lebanon)',
      icons: ['graphic_eq', 'waves']
    }
  };

  const activeChar = characterRoster[activeCharId];
  const isRtl = selectedLanguage === 'arabic' || selectedLanguage === 'hebrew';
  // the podium hall wants the roster as a stable, ordered list
  const hallRoster = React.useMemo(
    () => Object.entries(characterRoster).map(([id, c]) => ({ id, model: c.model })),
    []
  );

  const getUIString = (key) => {
    return t(`squad.${key}`, key);
  };

  const handleDeploy = () => {
    const activeName = getUIString(activeCharId);
    setDeployStatus(getUIString('deploying'));
    setTimeout(() => {
      setDeployStatus(getUIString('syncComplete').replace('${activeChar.name}', activeName));
    }, 1500);
  };

  const activeName = getUIString(activeCharId);

  return (
    <div className="pt-32 pb-24 px-4 md:px-margin-desktop max-w-container-max-width mx-auto w-full">
      
      {/* Header */}
      <header className={`mb-16 border-primary-container pl-6 ${
        isRtl ? 'border-r-4 pr-6 pl-0 text-right' : 'border-l-4 pl-6 pr-0 text-left'
      }`}>
        <h1 className="font-display-hero text-headline-lg-mobile md:text-display-hero text-primary-container uppercase tracking-tight drop-shadow-[0_0_12px_rgba(0,243,255,0.4)] font-extrabold">
          {t('characters.briefing', 'SQUAD DEPLOYMENT')}
        </h1>
        <p className="font-body-lg text-body-lg text-on-surface-variant mt-4 max-w-2xl">
          {t('characters.desc', 'Select operative for mainframe infiltration. Neural sync required for optimal performance.')}
        </p>
      </header>

      {/* Main Layout Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 lg:gap-gutter">
        
        {/* Main Display Port */}
        <div className="lg:col-span-8 bg-surface-container-low brutal-border p-4 md:p-6 flex flex-col md:flex-row gap-8 relative overflow-hidden group">
          <div className="absolute inset-0 bg-scanline-overlay pointer-events-none z-10"></div>
          
          {/* Operative 3D Podium Hall (Phase 2): every operative on a glowing
              platform; the active one swings to front-center. The thumbnail
              grid below stays the accessible selector and no-WebGL fallback. */}
          <div className="w-full md:w-1/2 relative h-[350px] md:h-[420px] border border-outline-variant/30 overflow-hidden bg-void-black">
            {/* CSS fallback behind the canvas (no-WebGL / reduced-motion-off) */}
            <div className="absolute inset-0 bg-[radial-gradient(110%_80%_at_50%_20%,#0a2230_0%,#06121b_50%,#050505_100%)]"></div>
            <div className="absolute inset-0 opacity-40 bg-[linear-gradient(rgba(0,243,255,0.05)_1px,transparent_1px),linear-gradient(90deg,rgba(0,243,255,0.05)_1px,transparent_1px)] bg-[size:38px_38px]"></div>
            <Suspense fallback={null}>
              <LazySquadHall
                roster={hallRoster}
                activeId={activeCharId}
                onSelect={setActiveCharId}
              />
            </Suspense>
            <div className="absolute top-2 left-2 bg-void-black/90 px-2 py-0.5 border border-primary-container/30 text-[10px] font-label-caps text-primary-container pointer-events-none z-20">
              {t('squad.live_orbit', '3D_LIVE_ORBIT')} // {activeCharId.toUpperCase()}
            </div>
            <div className="absolute bottom-2 right-2 bg-void-black/80 px-2 py-0.5 border border-outline-variant/20 text-[9px] font-label-caps text-on-surface-variant pointer-events-none z-20">
              {t('squad.select_platform', '🖱️ CLICK A PLATFORM TO DEPLOY')}
            </div>
          </div>
          
          {/* Details & Actions Frame */}
          <div className="w-full md:w-1/2 flex flex-col justify-between relative z-20">
            <div>
              <div className="flex flex-wrap items-center gap-3 mb-3">
                <span className={`font-label-caps text-[11px] px-2 py-0.5 font-bold uppercase ${
                  activeCharId === 'shogun' 
                    ? 'bg-tertiary-fixed-dim text-void-black' 
                    : 'bg-deep-grey text-on-surface-variant border border-outline-variant/30'
                }`}>
                  {activeCharId === 'shogun' ? getUIString('active') : getUIString('standby')}
                </span>
                <span className="font-body-md text-on-surface-variant text-[12px] tracking-wider uppercase">
                  {getUIString('role')}: {t(activeChar.role, activeChar.roleDefault)}
                </span>
              </div>
              
              <h2 className="font-display-hero text-headline-lg text-primary-container mb-1 uppercase drop-shadow-[0_0_8px_rgba(0,243,255,0.4)]">
                {activeName}
              </h2>
              {activeChar.civilian && (
                <p className="font-label-caps text-[10px] text-secondary-fixed-dim uppercase tracking-widest mb-4 opacity-60">
                  ID: {activeChar.civilian}
                </p>
              )}
              
              <p className="font-body-md text-[14px] text-on-surface-variant mb-6 leading-relaxed">
                {t(activeChar.descKey, activeChar.descDefault)}
              </p>
              
              {/* Ability Stats Grid */}
              <div className="space-y-4">
                {/* Firepower */}
                <div>
                  <div className="flex justify-between font-label-caps text-[11px] mb-1 text-primary">
                    <span>{t('characters.stats.power', getUIString('firepower'))}</span>
                    <span>{activeChar.stats.firepower}%</span>
                  </div>
                  <div className="h-2 w-full bg-surface-bright">
                    <div className="h-full stat-bar-fill transition-all duration-300" style={{ width: `${activeChar.stats.firepower}%` }}></div>
                  </div>
                </div>

                {/* Defense */}
                <div>
                  <div className="flex justify-between font-label-caps text-[11px] mb-1 text-primary">
                    <span>{t('characters.stats.defense', getUIString('defense'))}</span>
                    <span>{activeChar.stats.defense}%</span>
                  </div>
                  <div className="h-2 w-full bg-surface-bright">
                    <div className="h-full stat-bar-fill transition-all duration-300" style={{ width: `${activeChar.stats.defense}%` }}></div>
                  </div>
                </div>

                {/* Mobility */}
                <div>
                  <div className="flex justify-between font-label-caps text-[11px] mb-1 text-primary">
                    <span>{t('characters.stats.speed', getUIString('mobility'))}</span>
                    <span>{activeChar.stats.mobility}%</span>
                  </div>
                  <div className="h-2 w-full bg-surface-bright">
                    <div className="h-full bg-warning-orange transition-all duration-300" style={{ width: `${activeChar.stats.mobility}%` }}></div>
                  </div>
                </div>
              </div>
            </div>
            
            {/* Deploy Trigger */}
            <div className="mt-8 flex flex-col gap-3">
              <button 
                onClick={handleDeploy}
                className="w-full bg-primary-container text-void-black font-headline-md text-headline-md uppercase py-3.5 px-6 hover:bg-secondary-container transition-colors brutal-border font-bold tracking-wider"
              >
                {getUIString('deploy')}
              </button>
              
              {deployStatus && (
                <div className={`text-[12px] font-label-caps p-2 border leading-normal ${
                  deployStatus === getUIString('deploying')
                    ? 'text-warning-orange border-warning-orange/30 bg-warning-orange/5 animate-pulse' 
                    : 'text-tertiary-container border-tertiary-container/30 bg-tertiary-container/5'
                }`}>
                  &gt; {deployStatus}
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Secondary Selection Grid */}
        <div className="lg:col-span-4 flex flex-col gap-4">
          {Object.entries(characterRoster).map(([id, char]) => {
            const name = getUIString(id);
            return (
              <div
                key={id}
                onClick={() => {
                  setActiveCharId(id);
                  setDeployStatus(null);
                }}
                className={`brutal-border p-4 flex gap-4 transition-colors cursor-pointer group items-center ${
                  activeCharId === id
                    ? 'bg-surface-container border-primary-container'
                    : 'bg-surface-container/40 border-outline-variant/20 opacity-70 hover:opacity-100 hover:bg-surface-container/80'
                }`}
              >
                {/* Thumbnail frame */}
                <div className="w-14 h-14 relative flex-shrink-0 bg-void-black border border-primary-container/40 flex items-center justify-center text-primary-container">
                  <span className="material-symbols-outlined text-2xl group-hover:scale-110 transition-transform">view_in_ar</span>
                </div>
                
                {/* Title & Type */}
                <div className="flex-grow">
                  <h3 className="font-headline-md text-[18px] uppercase text-inverse-surface font-bold group-hover:text-primary-container transition-colors">
                    {name}
                  </h3>
                  <p className="font-label-caps text-[10px] text-secondary-fixed-dim uppercase tracking-wider mt-0.5">
                    {t(char.role, char.roleDefault)}
                  </p>
                </div>

                {/* Status Icons */}
                <div className="flex flex-col gap-1 text-right">
                  <span className="material-symbols-outlined text-primary-container text-sm">
                    {char.icons[0]}
                  </span>
                  <span className="material-symbols-outlined text-outline-variant text-sm">
                    {char.icons[1]}
                  </span>
                </div>
              </div>
            );
          })}
        </div>

      </div>

    </div>
  );
}
