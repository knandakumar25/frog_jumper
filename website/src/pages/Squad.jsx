import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useLanguage } from '../translations/LanguageContext';

export default function Squad() {
  const { t } = useTranslation();
  const { selectedLanguage } = useLanguage();
  const [activeCharId, setActiveCharId] = useState('shogun');
  const [deployStatus, setDeployStatus] = useState(null);

  const characterRoster = {
    shogun: {
      role: 'characters.warriors.frog.role',
      roleDefault: 'ASSAULT COMMANDER',
      descKey: 'characters.unlock',
      descDefault: 'Heavy armor frontline combatant. Specializes in kinetic shielding and close-quarters energy blade strikes. System hack resistance 94%.',
      loreKey: 'characters.warriors.dude.lore',
      stats: { firepower: 88, defense: 95, mobility: 42 },
      img: 'https://lh3.googleusercontent.com/aida-public/AB6AXuCIOJe92QJCceV6o_YKqmiI-GXAnhu5wf7sA6AIbBITzGWrZS7X3NTLOwABvR15y_9COR-uxPlYSdowg3ZaiO-Gvysw-Dw6-7bDVbTgrlfNUq8bNbhqsI1wmlU5HQkRAQm8hb0kAJ_lV6FrBHv_LrC1q1-KUaadpj2wJ4vM5DaNSa-V80ZsaJ7Tb43fj_00TjXs2Nve4o-5ca5CEZs1wYQMLKDHHIJMEszvBg6wH5W-9I4H0DPW2a6ElPi0VvK9nQVnoph0P-zgAUg',
      status: 'ACTIVE',
      icons: ['shield', 'bolt']
    },
    ronin: {
      role: 'characters.warriors.dude.role',
      roleDefault: 'DEFENDER / TACTICIAN',
      descKey: 'characters.warriors.dude.desc',
      descDefault: 'The masked warrior offers the perfect balance of offense and defense. Reliable in all situations, Aegis Ronin is equipped with a Kinetic Hammer and Energy Tower Shield to protect refugees.',
      loreKey: 'characters.warriors.dude.lore',
      stats: { firepower: 68, defense: 85, mobility: 60 },
      img: 'https://lh3.googleusercontent.com/aida-public/AB6AXuCtlOF-7UUy9KGe4IR4XM2WfPXPqdd9oIbGQLR06FnvuLzcch0sVZ9W9WaTo-Re1Kogtm3M5EEiYC-_jBqgghs4nQe3TwlCi1-xtQ61xrIUVDM5H-cqtO-VanT_Sdjnsth0oREpk2cBDGvgma1-9_IkrY_SU0WONyS9Mz7PnoO6XYHqMY0d-CArhCMyC2_H0Go26qHDiJ0rXbg-1NV7z44WcAEf2NY7wXSTZvyyubsRnekF3fvtKc3SuOywhOibP4uSdN1AXO8sNVE',
      status: 'STANDBY',
      icons: ['shield', 'visibility_off']
    },
    matrix: {
      role: 'characters.warriors.pink.role',
      roleDefault: 'INFILTRATOR / HACKER',
      descKey: 'characters.warriors.pink.desc',
      descDefault: 'A specialist in Phase-Shifting and EMP bursts, fighting to reclaim stolen identities from alien databases. Extremely fast and deadly at close quarters.',
      loreKey: 'characters.warriors.pink.lore',
      stats: { firepower: 75, defense: 50, mobility: 95 },
      img: 'https://lh3.googleusercontent.com/aida-public/AB6AXuB3SqMdCra26sXgScQA8KUas5aanVtVlzTzaEz9krzf4Sa7UjkdXmqkUjGP-PnDaRvc7NaeeFMr6HAagKL4j0nfw2i-XeXwMi3ttg6s1b6Vv-1leenbDqtZlThZt7NZhKU-gIU-8pQRS52OnTUuTYPEPMBokqD_bgFPmvq1SPec59XGyM-aECpIT102Z4aDAKML32k_N1RN7wP3gpaCs0W-Z7u4L4Qh1JIUbGtIbqmJT8UfHoMJ9GN5lP1VICpTYlthLx2BLwCYeBo',
      status: 'STANDBY',
      icons: ['terminal', 'visibility_off']
    },
    frog: {
      role: 'characters.warriors.frog.role',
      roleDefault: 'AGILE INFILTRATOR',
      descKey: 'characters.warriors.frog.desc',
      descDefault: 'A master of stealth and agility. The Ninja Frog can wall-jump and double-jump to reach impossible positions. Lightning-fast movement makes this warrior the ideal scout.',
      loreKey: 'characters.warriors.frog.lore',
      stats: { firepower: 55, defense: 40, mobility: 98 },
      img: 'https://lh3.googleusercontent.com/aida-public/AB6AXuC6w3uU4tz9oN0J89xLYYIwCsoK0SMoEkj5QeajfgPozk_pXOYKy3FksE8zlM3rXHxlfGRNehdioNhgBMS1wF-PDEBA7oykkr3LEqOmohyH25bqccBqdM3jhLsUZSJu0l70XNJyRWr3ySu6mDP_KBVWDwem16LsECy--myVTq5xgLC7xfZ5RooX6wvsJemDE6H8TKR6JKD2W6Snj00h6gAQMhsTBcfwGfqw4l902oq8yY5B58nGe2TFaaHx3vUqe-5xh8qWWjhE41Q',
      status: 'STANDBY',
      icons: ['bolt', 'visibility_off']
    },
    guy: {
      role: 'characters.warriors.guy.role',
      roleDefault: 'TECH SPECIALIST',
      descKey: 'characters.warriors.guy.desc',
      descDefault: 'A digital consciousness embedded in a physical chassis. Virtual Guy can interface with enemy systems and exploit technical vulnerabilities that others cannot perceive.',
      loreKey: 'characters.warriors.guy.lore',
      stats: { firepower: 70, defense: 65, mobility: 75 },
      img: 'https://lh3.googleusercontent.com/aida-public/AB6AXuCs4UuTUn4dvlRFUQUQfKMbJ8heyrPYI8GfvVzIaJTC5oVRz5qJ36XPawx237rkgp58A9xyrQhkewzFuexgL8X8jk6pdbEYfzn4_FPRl3HIO6uwryPNBgCCbkYnSEh6FjAOGROJ367WLlrsBmvH1O4hgtJw1_7dl4ny4n29_lKVegCOyF2sFw9f5I9dqlF5If_Hbz718U2NLZD3AlIDlu64KVEWi0KCPvFmnb2bxjyCJunCbkZk7WkouniML7biOf2K8Fbvw04XKaY',
      status: 'STANDBY',
      icons: ['terminal', 'memory']
    }
  };

  const activeChar = characterRoster[activeCharId];
  const isRtl = selectedLanguage === 'arabic' || selectedLanguage === 'hebrew';

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
          
          {/* Operative Render Frame */}
          <div className="w-full md:w-1/2 relative h-[300px] md:h-[400px] bg-void-black border border-outline-variant/30">
            <img 
              alt={activeName} 
              src={activeChar.img}
              className="absolute inset-0 w-full h-full object-cover filter grayscale group-hover:grayscale-0 transition-all duration-75 brightness-[0.85] contrast-125 select-none"
            />
            <div className="absolute inset-0 border border-primary-container/30 m-2 mix-blend-overlay"></div>
            <div className="absolute top-2 left-2 bg-void-black/80 px-2 py-0.5 border border-primary-container/20 text-[10px] font-label-caps">
              SECURE_LINK // {activeCharId.toUpperCase()}_NET
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
              
              <h2 className="font-display-hero text-headline-lg text-primary-container mb-4 uppercase drop-shadow-[0_0_8px_rgba(0,243,255,0.4)]">
                {activeName}
              </h2>
              
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
                <div className="w-16 h-16 relative flex-shrink-0 bg-void-black border border-outline-variant/30 overflow-hidden">
                  <img 
                    alt={name} 
                    src={char.img}
                    className="absolute inset-0 w-full h-full object-cover filter grayscale group-hover:scale-105 transition-all duration-150"
                  />
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
