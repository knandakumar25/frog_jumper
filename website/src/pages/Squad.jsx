import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useLanguage } from '../translations/LanguageContext';


export default function Squad() {
  const { t } = useTranslation();
  const { selectedLanguage } = useLanguage();
  const [searchParams] = useSearchParams();
  const initialChar = searchParams.get('id');

  const characterRoster = {
    shogun: {
      role: 'characters.warriors.shogun.role',
      roleDefault: 'CYBERNETIC SHOGUN WARRIOR',
      descKey: 'characters.warriors.shogun.desc',
      descDefault: 'A master cybernetic warrior from Little Tokyo, armed with an energy katana. Combines traditional bushido with futuristic military tech.',
      loreKey: 'characters.warriors.shogun.lore',
      stats: { firepower: 92, defense: 90, mobility: 88 },
      status: 'ACTIVE',
      civilian: 'Kaito Kurogane (Little Tokyo)',
      specialty: 'Kinetic Disruption / Katana Slice',
      icons: ['swords', 'bolt'],
    },
    oceanus: {
      role: 'characters.warriors.oceanus.role',
      roleDefault: 'HYDROKINETIC PLASMA MANIPULATOR',
      descKey: 'characters.warriors.oceanus.desc',
      descDefault: 'Kiana Vaea manipulates liquid tides and oceanic pressure waves, controlling the battlefield with high-velocity hydro-jets.',
      loreKey: 'characters.warriors.oceanus.lore',
      stats: { firepower: 78, defense: 74, mobility: 91 },
      status: 'STANDBY',
      civilian: 'Kiana Vaea (Pacific Basin)',
      specialty: 'Hydro-Jet Torrents / Fluid Vortex',
      icons: ['water_drop', 'visibility_off'],
    },
    surya: {
      role: 'characters.warriors.surya.role',
      roleDefault: 'COSMIC SOLAR VANGUARD',
      descKey: 'characters.warriors.surya.desc',
      descDefault: 'Arjun Varma channels cosmic fusion energy to manipulate gravimetric fields and release heavy AoE solar pulses.',
      loreKey: 'characters.warriors.surya.lore',
      stats: { firepower: 99, defense: 88, mobility: 75 },
      status: 'STANDBY',
      civilian: 'Arjun Varma (Varanasi / Orbital Station)',
      specialty: 'Solar Aegis / Graviton Fusion Blast',
      icons: ['solar_power', 'shield'],
    },
    mirage: {
      role: 'characters.warriors.mirage.role',
      roleDefault: 'CRYSTALLINE PHANTOM',
      descKey: 'characters.warriors.mirage.desc',
      descDefault: 'Zayd Al-Mansoor is an elite Saudi operative utilizing crystal refractions to create light-bending phantoms and slash through enemies with dual sayf blades.',
      loreKey: 'characters.warriors.mirage.lore',
      stats: { firepower: 82, defense: 58, mobility: 98 },
      status: 'STANDBY',
      civilian: 'Zayd Al-Mansoor (Riyadh)',
      specialty: 'Refraction Camouflage / Dual Sayf',
      icons: ['diamond', 'visibility_off'],
    },
    apex: {
      role: 'characters.warriors.apex.role',
      roleDefault: 'ELECTRO-MAGNETIC STORM VANGUARD',
      descKey: 'characters.warriors.apex.desc',
      descDefault: 'King Tariq Nzinga is a powerhouse who channels electro-magnetic storm clouds, crashing down with a devastating storm-shield leap.',
      loreKey: 'characters.warriors.apex.lore',
      stats: { firepower: 95, defense: 85, mobility: 93 },
      status: 'STANDBY',
      civilian: 'King Tariq Nzinga (Congo Federation)',
      specialty: 'Graviton Hammer / Ground Rupture',
      icons: ['hardware', 'bolt'],
    },
    phoenicia: {
      role: 'characters.warriors.phoenicia.role',
      roleDefault: 'ULTRASONIC PRISM WEAVER',
      descKey: 'characters.warriors.phoenicia.desc',
      descDefault: 'Maya Khoury hails from Byblos, Lebanon. Harnessing ancient Phoenician acoustic physics and resonant prism harmonics, she weaves ultrasonic shockwaves that stagger hostiles and deflect kinetic energy.',
      loreKey: 'characters.warriors.phoenicia.lore',
      stats: { firepower: 86, defense: 78, mobility: 96 },
      status: 'STANDBY',
      civilian: 'Maya Khoury (Byblos, Lebanon)',
      specialty: 'Harmonic Pulse / Prism Deflection',
      icons: ['graphic_eq', 'waves'],
    },
  };

  const [activeCharId, setActiveCharId] = useState(() => {
    return (initialChar && characterRoster[initialChar]) ? initialChar : 'shogun';
  });

  const [deployStatus, setDeployStatus] = useState(null);

  useEffect(() => {
    if (initialChar && characterRoster[initialChar]) {
      setActiveCharId(initialChar);
    }
  }, [initialChar]);

  const activeChar = characterRoster[activeCharId] || characterRoster.shogun;
  const isRtl = selectedLanguage === 'arabic' || selectedLanguage === 'hebrew';


  const getUIString = (key) => {
    return t(`squad.${key}`, key);
  };

  const handleDeploy = () => {
    const activeName = getUIString(activeCharId);
    setDeployStatus(getUIString('deploying'));
    setTimeout(() => {
      setDeployStatus(getUIString('syncComplete').replace('${activeChar.name}', activeName));
    }, 1200);
  };

  const activeName = getUIString(activeCharId);

  return (
    <div className="pt-24 md:pt-28 pb-24 px-4 md:px-margin-desktop max-w-container-max-width mx-auto w-full">

      {/* Header Bar */}
      <header className="mb-8 border-b border-primary-container/20 pb-4 flex flex-col md:flex-row justify-between items-start md:items-end gap-4">
        <div>
          <div className="font-mono text-[11px] text-primary-container tracking-[0.25em] uppercase mb-1 flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-tertiary-container animate-pulse"></span>
            BAY // NEURAL SYNCHRONIZATION RIG
          </div>
          <h1 className="font-display-hero text-2xl md:text-4xl text-primary-container uppercase font-extrabold tracking-tight text-glow-cyan">
            {t('characters.briefing', 'SQUAD DEPLOYMENT')}
          </h1>
          <p className="font-mono text-[12px] md:text-[13px] text-on-surface-variant mt-1 max-w-2xl">
            {t('characters.desc', 'Select operative for mainframe infiltration. Neural sync required for optimal performance.')}
          </p>
        </div>

        <div className="font-mono text-[11px] tracking-wider text-right hidden sm:block">
          <div className="text-tertiary-container font-semibold">ALL UNITS OPERATIONAL [6/6]</div>
          <div className="text-on-surface-variant/70">LINK // QUANTUM ENCRYPTED</div>
        </div>
      </header>

      {/* Main Operations Matrix */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 lg:gap-8 mb-8">

        {/* Left: 2D Holographic Stage */}
        <div className="lg:col-span-7 bg-void-black/95 border border-primary-container/30 relative overflow-hidden flex flex-col justify-between">
          <div className="absolute inset-0 bg-[radial-gradient(110%_80%_at_50%_20%,#0a2230_0%,#06121b_50%,#050505_100%)]"></div>
          <div className="absolute inset-0 hud-grid opacity-30 pointer-events-none"></div>

          {/* Corner HUD Reticles */}
          <div className="absolute top-2 left-2 w-3 h-3 border-t-2 border-l-2 border-primary-container z-20 pointer-events-none"></div>
          <div className="absolute top-2 right-2 w-3 h-3 border-t-2 border-r-2 border-primary-container z-20 pointer-events-none"></div>
          <div className="absolute bottom-2 left-2 w-3 h-3 border-b-2 border-l-2 border-primary-container z-20 pointer-events-none"></div>
          <div className="absolute bottom-2 right-2 w-3 h-3 border-b-2 border-r-2 border-primary-container z-20 pointer-events-none"></div>

          {/* Viewport Top Status */}
          <div className="relative z-20 p-4 flex justify-between items-center pointer-events-none">
            <span className="font-mono text-[10px] text-primary-container bg-void-black/85 border border-primary-container/40 px-2.5 py-1 tracking-widest">
              {t('squad.live_orbit', 'OPERATIVE_DOSSIER')} // {activeCharId.toUpperCase()}
            </span>
            <span className="font-mono text-[10px] text-tertiary-container bg-void-black/85 border border-tertiary-container/30 px-2 py-0.5">
              SYNC STABILITY: 99.4%
            </span>
          </div>

          {/* 2D Canvas Container */}
          <div className="relative w-full h-[400px] md:h-[480px]">
            <div className="h-full flex flex-col items-center justify-center gap-6 text-primary-container"><span className="material-symbols-outlined text-8xl" aria-hidden="true">{activeChar.icons[0]}</span><h2 className="font-display-hero text-3xl">{activeName}</h2><p className="font-mono text-sm">{activeChar.civilian}</p></div>
          </div>

          {/* Viewport Bottom Hint */}
          <div className="relative z-20 p-3 bg-void-black/90 border-t border-primary-container/20 flex justify-between items-center text-[10px] font-mono tracking-wider text-on-surface-variant pointer-events-none">
            <span>SELECT AN OPERATIVE TO VIEW THEIR DOSSIER</span>
            <span className="text-primary-container/70">CHAMBER_04 // ACTIVE</span>
          </div>
        </div>

        {/* Right: Operative Dossier Panel */}
        <div className="lg:col-span-5 bg-surface-container-low/90 border border-primary-container/25 p-6 md:p-8 flex flex-col justify-between relative">
          <div>
            {/* Identity Header */}
            <div className="flex justify-between items-start mb-3">
              <span className={`font-mono text-[10px] px-2.5 py-0.5 uppercase tracking-widest font-bold ${
                activeChar.status === 'ACTIVE'
                  ? 'bg-tertiary-container text-void-black'
                  : 'bg-primary-container/20 text-primary-container border border-primary-container/40'
              }`}>
                {activeChar.status === 'ACTIVE' ? getUIString('active') : getUIString('standby')}
              </span>
              <span className="font-mono text-[10px] text-on-surface-variant/70 tracking-widest">
                ID_REF // {activeCharId.toUpperCase()}_2157
              </span>
            </div>

            <h2 className="font-display-hero text-2xl md:text-3xl text-primary-container uppercase font-extrabold tracking-wide mb-1 text-glow-cyan">
              {activeName}
            </h2>

            <div className="font-mono text-[11px] text-secondary-fixed-dim uppercase tracking-wider mb-3">
              {t(activeChar.role, activeChar.roleDefault)}
            </div>

            {activeChar.civilian && (
              <div className="font-mono text-[11px] text-on-surface-variant/80 border-l border-primary-container/40 pl-3 py-1 mb-4">
                CIVILIAN IDENTITY: <span className="text-on-surface font-semibold">{activeChar.civilian}</span>
              </div>
            )}

            <p className="font-mono text-[12px] md:text-[13px] text-on-surface-variant leading-relaxed mb-6">
              {t(activeChar.descKey, activeChar.descDefault)}
            </p>

            {/* Specialty Badge */}
            <div className="bg-void-black/80 border border-outline-variant/30 p-3 mb-6 font-mono text-[11px]">
              <div className="text-primary-container/80 text-[10px] uppercase tracking-widest mb-0.5">
                // SIGNATURE PROTOCOL & WEAPON
              </div>
              <div className="text-on-surface font-semibold flex items-center gap-2">
                <span className="material-symbols-outlined text-primary-container text-[16px]">
                  {activeChar.icons[0]}
                </span>
                {activeChar.specialty}
              </div>
            </div>

            {/* Ability Stats Grid with Segmented Gauges */}
            <div className="space-y-4 font-mono text-[11px]">
              {/* Firepower */}
              <div>
                <div className="flex justify-between text-on-surface-variant mb-1">
                  <span>{t('characters.stats.power', getUIString('firepower'))}</span>
                  <span className="text-primary-container font-bold">{activeChar.stats.firepower}%</span>
                </div>
                <div className="h-2 w-full bg-surface-bright">
                  <div
                    className="h-full bg-primary-container transition-all duration-300"
                    style={{ width: `${activeChar.stats.firepower}%` }}
                  ></div>
                </div>
              </div>

              {/* Defense */}
              <div>
                <div className="flex justify-between text-on-surface-variant mb-1">
                  <span>{t('characters.stats.defense', getUIString('defense'))}</span>
                  <span className="text-tertiary-container font-bold">{activeChar.stats.defense}%</span>
                </div>
                <div className="h-2 w-full bg-surface-bright">
                  <div
                    className="h-full bg-tertiary-container transition-all duration-300"
                    style={{ width: `${activeChar.stats.defense}%` }}
                  ></div>
                </div>
              </div>

              {/* Mobility */}
              <div>
                <div className="flex justify-between text-on-surface-variant mb-1">
                  <span>{t('characters.stats.speed', getUIString('mobility'))}</span>
                  <span className="text-warning-orange font-bold">{activeChar.stats.mobility}%</span>
                </div>
                <div className="h-2 w-full bg-surface-bright">
                  <div
                    className="h-full bg-warning-orange transition-all duration-300"
                    style={{ width: `${activeChar.stats.mobility}%` }}
                  ></div>
                </div>
              </div>
            </div>
          </div>

          {/* Neural Deploy Trigger */}
          <div className="mt-8 pt-4 border-t border-outline-variant/30 flex flex-col gap-3">
            <button
              onClick={handleDeploy}
              className="cyber-cut w-full bg-primary-container text-void-black font-headline-md text-[15px] uppercase py-3.5 px-6 font-bold tracking-widest hover:bg-secondary-container hover:text-white transition-all shadow-[0_0_20px_rgba(0,243,255,0.35)] flex items-center justify-center gap-2"
            >
              <span className="material-symbols-outlined text-[20px]">power_settings_new</span>
              {getUIString('deploy')}
            </button>

            {deployStatus && (
              <div className={`text-[12px] font-mono p-3 border leading-normal transition-all ${
                deployStatus === getUIString('deploying')
                  ? 'text-warning-orange border-warning-orange/50 bg-warning-orange/10 animate-pulse'
                  : 'text-tertiary-container border-tertiary-container/50 bg-tertiary-container/10'
              }`}>
                &gt; {deployStatus}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Operative Selection Rail (Quick Switcher) */}
      <div className="border border-primary-container/20 bg-void-black/75 p-4">
        <div className="font-mono text-[11px] text-primary-container uppercase tracking-[0.25em] mb-3 flex items-center gap-2">
          <span className="w-1.5 h-1.5 bg-primary-container"></span>
          SELECT OPERATIVE UNIT
        </div>

        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-6 gap-3">
          {Object.entries(characterRoster).map(([id, char]) => {
            const isSelected = activeCharId === id;
            const name = getUIString(id);
            return (
              <button
                key={id}
                onClick={() => {
                  setActiveCharId(id);
                  setDeployStatus(null);
                }}
                className={`p-3 text-left border transition-all duration-150 flex flex-col justify-between h-20 ${
                  isSelected
                    ? 'bg-primary-container/15 border-primary-container text-primary-container shadow-[0_0_12px_rgba(0,243,255,0.2)]'
                    : 'bg-surface-container-low/50 border-outline-variant/30 text-on-surface-variant hover:border-primary-container/50 hover:bg-surface-container-low'
                }`}
              >
                <div className="flex justify-between items-start w-full">
                  <span className="font-mono text-[10px] tracking-widest opacity-60">
                    // 0{hallRoster.findIndex(r => r.id === id) + 1}
                  </span>
                  <span className={`w-2 h-2 rounded-full ${isSelected ? 'bg-primary-container' : 'bg-outline-variant'}`}></span>
                </div>
                <div className="font-display-hero text-[13px] uppercase font-bold tracking-wider truncate w-full">
                  {name}
                </div>
              </button>
            );
          })}
        </div>
      </div>

    </div>
  );
}
