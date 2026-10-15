import React, { Suspense, lazy } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useLanguage } from '../translations/LanguageContext';
import { getTitleLogoPath } from '../translations/titleLogo';

// Phase 1: the 3D hero world is code-split so the rest of the site never
// waits on three.js; below-the-fold pages don't pay for it either.
const HomeWorld = lazy(() => import('../three/HomeWorld'));

const OPERATIVES_PREVIEW = [
  {
    id: 'shogun',
    nameKey: 'squad.shogun',
    defaultName: 'CYBER SHOGUN',
    roleKey: 'characters.warriors.shogun.role',
    defaultRole: 'CYBERNETIC SHOGUN WARRIOR',
    descKey: 'characters.warriors.shogun.desc',
    defaultDesc: 'Frontline breaker. Specializes in heavy kinetic disruption and energy katana strikes.',
    classTag: 'VANGUARD',
    color: '#00f3ff',
    icon: 'swords',
    power: 92,
  },
  {
    id: 'oceanus',
    nameKey: 'squad.oceanus',
    defaultName: 'OCEANUS',
    roleKey: 'characters.warriors.oceanus.role',
    defaultRole: 'HYDROKINETIC OPERATIVE',
    descKey: 'characters.warriors.oceanus.desc',
    defaultDesc: 'Liquid pressure wave manipulator controlling the field with hydro-jets and vortex geysers.',
    classTag: 'TACTICAL',
    color: '#00dce6',
    icon: 'water_drop',
    power: 84,
  },
  {
    id: 'surya',
    nameKey: 'squad.surya',
    defaultName: 'SURYA PRIME',
    roleKey: 'characters.warriors.surya.role',
    defaultRole: 'COSMIC SOLAR VANGUARD',
    descKey: 'characters.warriors.surya.desc',
    defaultDesc: 'Solar fusion guardian unleashing heavy AoE solar pulses and cosmic gravimetric shields.',
    classTag: 'HEAVY',
    color: '#FF4D00',
    icon: 'solar_power',
    power: 96,
  },
  {
    id: 'mirage',
    nameKey: 'squad.mirage',
    defaultName: 'MIRAGE BLADE',
    roleKey: 'characters.warriors.mirage.role',
    defaultRole: 'CRYSTALLINE PHANTOM',
    descKey: 'characters.warriors.mirage.desc',
    defaultDesc: 'Light-bending phantom assassin utilizing crystal refractions and twin precision daggers.',
    classTag: 'INFILTRATOR',
    color: '#fe00fe',
    icon: 'diamond',
    power: 88,
  },
  {
    id: 'apex',
    nameKey: 'squad.apex',
    defaultName: 'APEX TITAN',
    roleKey: 'characters.warriors.apex.role',
    defaultRole: 'SEISMIC TITAN',
    descKey: 'characters.warriors.apex.desc',
    defaultDesc: 'Seismic powerhouse wielding graviton storm hammers and tectonic ground ruptures.',
    classTag: 'JUGGERNAUT',
    color: '#36fd0f',
    icon: 'hardware',
    power: 95,
  },
  {
    id: 'phoenicia',
    nameKey: 'squad.phoenicia',
    defaultName: 'AURA PHOENICIA',
    roleKey: 'characters.warriors.phoenicia.role',
    defaultRole: 'ULTRASONIC PRISM WEAVER',
    descKey: 'characters.warriors.phoenicia.desc',
    defaultDesc: 'Acoustic physicist weaving ultrasonic shockwaves that stagger hostiles and deflect kinetic energy.',
    classTag: 'RESONATOR',
    color: '#ffd7f5',
    icon: 'graphic_eq',
    power: 91,
  },
];

const SECTORS_INTEL = [
  {
    id: 'sec_04',
    code: 'SECTOR // 04',
    title: 'LOS ANGELES QUARANTINE ZONE',
    desc: 'Dense dystopian ruins surrounded by containment shields and continuous alien drone patrols.',
    threat: 'CRITICAL',
    status: 'ACTIVE HOSTILE',
  },
  {
    id: 'proc_09',
    code: 'FACILITY // 09',
    title: 'ATMOSPHERIC PROCESSOR SUB-ARRAY',
    desc: 'Massive terraforming spires harvesting Earth atmospheric nitrogen for colonial integration.',
    threat: 'HIGH',
    status: 'SABOTAGE TARGET',
  },
  {
    id: 'core_00',
    code: 'CITADEL // 00',
    title: 'DISPLACEMENT MAINFRAME VAULT',
    desc: 'Deep sub-level nexus coordinating the occupation network and robotic defense legions.',
    threat: 'OMEGA',
    status: 'ENCRYPTED',
  },
];

export default function Home() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);

  const handleJoinStrike = (charId = null) => {
    navigate(`/${currentLocale}/squad${charId ? `?id=${charId}` : ''}`);
  };

  const handleViewIntel = () => {
    navigate(`/${currentLocale}/gameplay`);
  };

  const isRtl = selectedLanguage === 'arabic' || selectedLanguage === 'hebrew';

  return (
    <div className="pt-[80px] md:pt-[100px] pb-24 px-4 md:px-margin-desktop max-w-container-max-width mx-auto">
      
      {/* =========================================================================
          HERO: SCENE-FIRST HUD COMMAND FRAME
          ========================================================================= */}
      <section className="relative min-h-[78vh] mb-20 overflow-hidden border border-primary-container/25 bg-void-black/90">
        <div className="absolute inset-0 bg-[radial-gradient(120%_90%_at_50%_8%,#0a2230_0%,#06121b_45%,#050505_100%)]"></div>
        <div className="absolute inset-0 opacity-40 bg-[linear-gradient(rgba(0,243,255,0.05)_1px,transparent_1px),linear-gradient(90deg,rgba(0,243,255,0.05)_1px,transparent_1px)] bg-[size:40px_40px]"></div>
        
        {/* Corner HUD Markers */}
        <div className="absolute top-2 left-2 w-3 h-3 border-t-2 border-l-2 border-primary-container z-20"></div>
        <div className="absolute top-2 right-2 w-3 h-3 border-t-2 border-r-2 border-primary-container z-20"></div>
        <div className="absolute bottom-2 left-2 w-3 h-3 border-b-2 border-l-2 border-primary-container z-20"></div>
        <div className="absolute bottom-2 right-2 w-3 h-3 border-b-2 border-r-2 border-primary-container z-20"></div>

        {/* 3D World Canvas */}
        <Suspense fallback={null}>
          <HomeWorld />
        </Suspense>

        {/* Top-Left Identity HUD */}
        <div className="absolute top-6 left-4 md:left-8 z-10 flex flex-col items-start gap-2.5">
          <h1 className="sr-only">AXIOM FORCE</h1>
          <img
            src={getTitleLogoPath(selectedLanguage)}
            alt="AXIOM FORCE Logo"
            className="h-12 md:h-16 w-auto object-contain drop-shadow-[0_0_24px_rgba(0,243,255,0.85)] select-none"
            fetchpriority="high"
          />
          <div className="flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-warning-orange animate-ping"></span>
            <div className="bg-warning-orange text-void-black font-mono text-[10px] px-2.5 py-0.5 uppercase tracking-[0.25em] font-bold">
              {t('home.system_breach', 'System Breach Detected')}
            </div>
            <span className="hidden sm:inline font-mono text-[10px] text-primary-container/70 tracking-widest">
              [GRID_REF: 34.05°N, 118.24°W]
            </span>
          </div>
        </div>

        {/* Bottom-Left Tactical Console Panel */}
        <div className={`absolute bottom-6 z-10 max-w-xl w-[92%] md:w-auto md:max-w-[540px] ${
          isRtl ? 'right-4 md:right-8 border-r-2 text-right' : 'left-4 md:left-8 border-l-2 text-left'
        } border-primary-container bg-void-black/85 backdrop-blur-md px-5 py-4 shadow-[0_0_25px_rgba(0,0,0,0.8)]`}>
          <div className="font-mono text-[11px] md:text-[12px] leading-relaxed text-on-surface-variant">
            <div className="text-primary-container/80 flex items-center gap-2">
              <span className="inline-block w-1.5 h-1.5 bg-primary-container"></span>
              &gt; INIT_RESISTANCE_PROTOCOL.exe
            </div>
            <div className="mt-1 text-on-surface">&gt; {t('hero.subtitle', 'A 3D action platformer. Reclaim Earth from alien occupiers.')}</div>
            <div className="text-tertiary-container font-semibold">&gt; {t('footer.status', 'THE COLONIAL MAINFRAME WILL FALL.')}</div>
            <div className="hidden md:block text-on-surface-variant/70 text-[10px] mt-1">&gt; {t('footer.platform_desc', 'Target Platforms: Windows, Xbox (Series X/S & One), PlayStation 5, Nintendo Switch 2, Android, iOS, macOS, visionOS, Linux')}</div>
          </div>
          
          <div className="mt-4 flex flex-col sm:flex-row gap-3">
            <button
              onClick={() => handleJoinStrike()}
              className="cyber-cut bg-primary-container text-void-black font-headline-md text-[14px] md:text-[16px] px-6 py-2.5 uppercase font-bold hover:bg-secondary-container hover:text-white transition-all shadow-[0_0_15px_rgba(0,243,255,0.4)] flex items-center justify-center gap-2"
            >
              <span className="material-symbols-outlined text-[18px]">bolt</span>
              {t('nav.play_now', 'Join the Strike')}
            </button>
            <button
              onClick={handleViewIntel}
              className="border border-primary-container/50 bg-primary-container/5 text-primary-container font-headline-md text-[14px] md:text-[16px] px-6 py-2.5 uppercase hover:bg-primary-container/20 hover:border-primary-container transition-all flex items-center justify-center gap-2"
            >
              <span className="material-symbols-outlined text-[18px]">radar</span>
              {t('story.briefing', 'View Intel')}
            </button>
          </div>
        </div>

        {/* Bottom-Right Live Telemetry Readout */}
        <div className="hidden lg:flex absolute bottom-6 right-8 z-10 flex-col items-end gap-1.5 font-mono text-[10px] tracking-[0.2em] text-primary-container/70 bg-void-black/80 px-4 py-3 border border-primary-container/20">
          <div className="flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-tertiary-container animate-pulse"></span>
            <span className="text-tertiary-container font-bold">FEED // LA 2157</span>
          </div>
          <span className="text-on-surface-variant">SCENE: LIVE WAR ROOM</span>
          <span className="text-primary-container/50">CAM // DRONE_04 [ORBITING]</span>
        </div>
      </section>

      {/* =========================================================================
          SECTION 2: RESISTANCE TACTICAL DOSSIER & OPERATIONS WAR ROOM
          ========================================================================= */}
      <section className="mb-24">
        <div className="flex items-center gap-3 mb-6">
          <span className="w-2 h-2 bg-primary-container"></span>
          <span className="font-mono text-[11px] text-primary-container uppercase tracking-[0.25em]">
            // TACTICAL INTELLIGENCE ARCHIVE
          </span>
          <div className="h-px bg-primary-container/20 flex-grow"></div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 lg:gap-8">
          {/* Left: Declassified Dossier */}
          <div className="lg:col-span-6 bg-surface-container-low/70 border border-primary-container/25 p-6 md:p-8 relative">
            <div className="absolute top-0 right-0 bg-primary-container/10 border-b border-l border-primary-container/30 px-3 py-1 font-mono text-[10px] text-primary-container tracking-widest">
              DOC_ID: HLF_2157_A1
            </div>

            <div className="text-secondary-container font-mono text-[11px] mb-2 uppercase font-bold tracking-wider flex items-center gap-2">
              <span className="material-symbols-outlined text-[16px]">lock_open</span>
              &gt; {t('home.decrypted_file', 'DECRYPTED FILE: 001_HLF')}
            </div>
            
            <h2 className="font-display-hero text-2xl md:text-3xl text-inverse-surface mb-4 uppercase tracking-wide">
              {t('story.title', 'The Human Liberation Front')}
            </h2>
            
            <p className="font-mono text-[13px] md:text-[14px] text-on-surface-variant mb-6 leading-relaxed border-l-2 border-primary-container/40 pl-4">
              {t('story.desc', 'They mapped our streets, cataloged our resources, and deemed us integrated. But integration is just another word for erasure. We are the glitch in their perfect colonial simulation.')}
            </p>

            <div className="space-y-3 font-mono text-[12px]">
              <div className="bg-void-black/70 border border-outline-variant/30 p-3 flex items-center justify-between hover:border-primary-container/50 transition-colors">
                <div className="flex items-center gap-3">
                  <span className="material-symbols-outlined text-primary-container text-[18px]">terminal</span>
                  <span className="text-on-surface">{t('story.timeline.axiom_title', 'AXIOM-7 Operations Coordination')}</span>
                </div>
                <span className="text-tertiary-container font-bold tracking-widest text-[10px]">[SYNCED]</span>
              </div>

              <div className="bg-void-black/70 border border-outline-variant/30 p-3 flex items-center justify-between hover:border-warning-orange/50 transition-colors">
                <div className="flex items-center gap-3">
                  <span className="material-symbols-outlined text-warning-orange text-[18px]">emergency</span>
                  <span className="text-on-surface">{t('story.timeline.displacement_title', 'Sabotage Displacement Mainframes')}</span>
                </div>
                <span className="text-warning-orange font-bold tracking-widest text-[10px]">[ACTIVE]</span>
              </div>

              <div className="bg-void-black/70 border border-outline-variant/30 p-3 flex items-center justify-between hover:border-secondary-container/50 transition-colors">
                <div className="flex items-center gap-3">
                  <span className="material-symbols-outlined text-secondary-container text-[18px]">vpn_key</span>
                  <span className="text-on-surface">{t('story.timeline.mission_title', 'Execute Core Insurgent Protocols')}</span>
                </div>
                <span className="text-secondary-container font-bold tracking-widest text-[10px]">[PENDING]</span>
              </div>
            </div>
          </div>

          {/* Right: Tactical Radar & Operations Telemetry */}
          <div className="lg:col-span-6 bg-void-black/90 border border-primary-container/25 p-6 md:p-8 relative overflow-hidden flex flex-col justify-between">
            <div className="absolute inset-0 opacity-20 bg-[radial-gradient(ellipse_at_center,rgba(0,243,255,0.25)_0%,transparent_70%)]"></div>
            <div className="absolute inset-0 hud-grid opacity-30"></div>

            <div>
              <div className="flex justify-between items-center mb-6 border-b border-primary-container/20 pb-3">
                <div className="font-mono text-[11px] text-primary-container tracking-widest uppercase flex items-center gap-2">
                  <span className="w-2 h-2 rounded-full bg-primary-container animate-ping"></span>
                  OPERATIONAL RADAR // LOCAL GRID
                </div>
                <span className="font-mono text-[10px] bg-tertiary-container/10 border border-tertiary-container/30 text-tertiary-container px-2 py-0.5">
                  HLF SECURE
                </span>
              </div>

              {/* Radar Map Graphic */}
              <div className="relative h-44 border border-primary-container/20 rounded-full flex items-center justify-center my-4 overflow-hidden">
                <div className="absolute inset-4 border border-primary-container/15 rounded-full"></div>
                <div className="absolute inset-14 border border-primary-container/10 rounded-full"></div>
                <div className="absolute w-full h-[1px] bg-primary-container/20"></div>
                <div className="absolute h-full w-[1px] bg-primary-container/20"></div>
                
                {/* Blinking Targets */}
                <div className="absolute top-10 left-16 flex items-center gap-1.5">
                  <span className="w-2 h-2 bg-warning-orange rounded-full animate-ping"></span>
                  <span className="font-mono text-[9px] text-warning-orange bg-void-black/90 px-1 border border-warning-orange/30">SUBSTATION 4A</span>
                </div>
                <div className="absolute bottom-8 right-14 flex items-center gap-1.5">
                  <span className="w-2 h-2 bg-primary-container rounded-full animate-pulse"></span>
                  <span className="font-mono text-[9px] text-primary-container bg-void-black/90 px-1 border border-primary-container/30">CITADEL SPIRE</span>
                </div>
                <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-3 h-3 bg-tertiary-container rounded-full shadow-[0_0_8px_#36fd0f]"></div>
              </div>
            </div>

            <div className="bg-surface-container-low/90 border-l-2 border-primary-container p-4 font-mono text-[12px] mt-4">
              <div className="text-primary-container font-semibold mb-1">{t('home.status_operational', 'STATUS: OPERATIONAL')}</div>
              <div className="text-on-surface-variant">{t('home.grid_hacked', 'Substation 4Alpha hacked. Local grid under HLF control.')}</div>
            </div>
          </div>
        </div>
      </section>

      {/* =========================================================================
          SECTION 3: STRIKE FORCE TACTICAL ROSTER (ALL 6 OPERATIVES)
          ========================================================================= */}
      <section className="mb-24">
        <div className="flex flex-col md:flex-row justify-between items-start md:items-end gap-4 mb-8 border-b border-primary-container/20 pb-4">
          <div>
            <div className="font-mono text-[11px] text-primary-container uppercase tracking-[0.25em] mb-1">
              // ACTIVE UNIT DEPLOYMENT
            </div>
            <h2 className="font-display-hero text-2xl md:text-3xl text-primary-container uppercase tracking-wide font-extrabold text-glow-cyan">
              {t('characters.title', 'Champions of Resistance')}
            </h2>
            <p className="font-mono text-[12px] text-on-surface-variant mt-1">
              &gt; {t('characters.desc', 'Select your operative to initialize neural synchronization.')}
            </p>
          </div>

          <button
            onClick={() => handleJoinStrike()}
            className="font-mono text-[12px] tracking-wider uppercase text-primary-container border border-primary-container/40 hover:bg-primary-container/10 px-4 py-2 flex items-center gap-2 transition-colors"
          >
            OPEN FULL PODIUM TERMINAL
            <span className="material-symbols-outlined text-[16px]">arrow_forward</span>
          </button>
        </div>

        {/* 6 Operatives Tactical Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {OPERATIVES_PREVIEW.map((op) => (
            <div
              key={op.id}
              onClick={() => handleJoinStrike(op.id)}
              className="group bg-surface-container-low/80 border border-primary-container/25 hover:border-primary-container p-5 transition-all duration-200 cursor-pointer flex flex-col justify-between relative hover:shadow-[0_0_20px_rgba(0,243,255,0.12)]"
            >
              {/* Top Accent Lines */}
              <div className="flex justify-between items-start mb-4">
                <span className="font-mono text-[10px] tracking-widest text-primary-container/80 border border-primary-container/30 px-2 py-0.5 bg-void-black">
                  {op.classTag}
                </span>
                <span className="font-mono text-[10px] text-tertiary-container flex items-center gap-1">
                  <span className="w-1.5 h-1.5 rounded-full bg-tertiary-container animate-pulse"></span>
                  READY
                </span>
              </div>

              <div>
                <h3 className="font-display-hero text-lg text-inverse-surface group-hover:text-primary-container uppercase font-bold tracking-wider transition-colors mb-1">
                  {t(op.nameKey, op.defaultName)}
                </h3>
                <div className="font-mono text-[10px] text-secondary-fixed-dim uppercase tracking-wider mb-3">
                  {t(op.roleKey, op.defaultRole)}
                </div>
                <p className="font-mono text-[12px] text-on-surface-variant line-clamp-2 leading-relaxed mb-4">
                  {t(op.descKey, op.defaultDesc)}
                </p>
              </div>

              <div>
                {/* Power Bar */}
                <div className="mb-4">
                  <div className="flex justify-between font-mono text-[10px] text-on-surface-variant mb-1">
                    <span>COMBAT RATING</span>
                    <span className="text-primary-container font-bold">{op.power}%</span>
                  </div>
                  <div className="h-1.5 bg-surface-bright w-full overflow-hidden">
                    <div
                      className="h-full bg-primary-container transition-all duration-300"
                      style={{ width: `${op.power}%` }}
                    ></div>
                  </div>
                </div>

                <div className="border-t border-outline-variant/30 pt-3 flex justify-between items-center font-mono text-[11px] text-primary-container/80 group-hover:text-primary-container">
                  <span className="tracking-widest uppercase">&gt; SELECT FOR DEPLOYMENT</span>
                  <span className="material-symbols-outlined text-[16px] group-hover:translate-x-1 transition-transform">
                    chevron_right
                  </span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* =========================================================================
          SECTION 4: FRONTLINE COMBAT ZONES & THE WAR EFFORT
          ========================================================================= */}
      <section className="mb-24">
        <div className="flex items-center gap-3 mb-6">
          <span className="w-2 h-2 bg-secondary-container"></span>
          <span className="font-mono text-[11px] text-secondary-container uppercase tracking-[0.25em]">
            // WAR FRONTLINES INTEL
          </span>
          <div className="h-px bg-secondary-container/20 flex-grow"></div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {SECTORS_INTEL.map((sec) => (
            <div
              key={sec.id}
              onClick={handleViewIntel}
              className="bg-void-black/80 border border-outline-variant/40 hover:border-secondary-container/60 p-6 transition-all cursor-pointer relative group"
            >
              <div className="flex justify-between items-center mb-4">
                <span className="font-mono text-[11px] tracking-widest text-on-surface-variant">
                  {sec.code}
                </span>
                <span className={`font-mono text-[10px] px-2 py-0.5 font-bold ${
                  sec.threat === 'CRITICAL' ? 'bg-error-container text-white' :
                  sec.threat === 'HIGH' ? 'bg-warning-orange text-void-black' :
                  'bg-secondary-container text-white'
                }`}>
                  {sec.threat}
                </span>
              </div>

              <h4 className="font-display-hero text-[16px] text-inverse-surface group-hover:text-secondary-container uppercase font-bold tracking-wider mb-2 transition-colors">
                {sec.title}
              </h4>

              <p className="font-mono text-[12px] text-on-surface-variant leading-relaxed mb-6">
                {sec.desc}
              </p>

              <div className="font-mono text-[11px] text-secondary-container flex items-center gap-1.5">
                <span className="opacity-60">&gt;</span> STATUS: {sec.status}
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* =========================================================================
          SECTION 5: COMMAND TRANSMISSION RECRUITMENT CALL-TO-ARMS
          ========================================================================= */}
      <section className="bg-surface-container-low/90 border border-primary-container/30 p-8 md:p-12 relative overflow-hidden">
        <div className="absolute top-0 right-0 w-64 h-64 bg-primary-container/5 rounded-full blur-3xl pointer-events-none"></div>

        <div className="max-w-3xl">
          <div className="font-mono text-[11px] text-warning-orange tracking-[0.25em] uppercase mb-2">
            &gt; SYSTEM_BROADCAST // ENLIST_CALL_2157
          </div>
          <h2 className="font-display-hero text-2xl md:text-4xl text-primary-container uppercase font-extrabold tracking-tight mb-4">
            THE OCCUPATION WILL NOT STAND.
          </h2>
          <p className="font-mono text-[13px] md:text-[14px] text-on-surface-variant leading-relaxed mb-8">
            Join thousands of resistance operatives worldwide. Master high-octane 3D combat, coordinate infiltration strikes, and liberate Earth Sector by Sector.
          </p>

          <div className="flex flex-col sm:flex-row gap-4">
            <button
              onClick={() => handleJoinStrike()}
              className="cyber-cut bg-primary-container text-void-black font-headline-md text-[15px] px-8 py-3 uppercase font-bold hover:bg-secondary-container hover:text-white transition-all shadow-[0_0_15px_rgba(0,243,255,0.4)]"
            >
              ENLIST IN RESISTANCE
            </button>
            <button
              onClick={handleViewIntel}
              className="border border-outline-variant hover:border-primary-container text-on-surface font-headline-md text-[15px] px-8 py-3 uppercase hover:bg-primary-container/10 transition-all"
            >
              ACCESS COMBAT PROTOCOLS
            </button>
          </div>
        </div>
      </section>

    </div>
  );
}
