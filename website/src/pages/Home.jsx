import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useLanguage } from '../translations/LanguageContext';

export default function Home() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);

  const handleJoinStrike = () => {
    navigate(`/${currentLocale}/squad`);
  };

  const handleViewIntel = () => {
    navigate(`/${currentLocale}/gameplay`);
  };

  const isRtl = selectedLanguage === 'arabic' || selectedLanguage === 'hebrew';

  return (
    <div className="pt-[100px] md:pt-[120px] pb-24 px-4 md:px-margin-desktop max-w-container-max-width mx-auto">
      
      {/* Hero Section */}
      <section className="relative min-h-[600px] md:min-h-[716px] flex items-center justify-center mb-32 bracket-corners neon-border p-6 md:p-8 bg-surface-container-low/50 overflow-hidden">
        {/* Background Image */}
        <div 
          className="absolute inset-0 opacity-40 mix-blend-luminosity bg-cover bg-center" 
          style={{ 
            backgroundImage: "url('https://lh3.googleusercontent.com/aida-public/AB6AXuDZKmyGVNF3TXicRiuSAQhqCtZekvOOL-Q_OuAHtsCvi5GEfKHskSAS5WPFMXW2tnMd6cHfIVYV6n5IaYf8nNA8ylOjRQIyxQHIyGQe0WRXKxEgg6scfB4OTWzvMJ47WtZQ9bzLHt8hIPIG2behbGhAkneMpQCp1VNfUntHVYJjJYZpXaWYxJpwCAsYU37so1iUqNTyqG8Uu4D20Ewl9xBfuwX8KTgqHzddCH3nf5_weYR0d67BVht9ElUlHOgc7_vKOlCpU1-Kn-g')" 
          }}
        ></div>
        
        <div className="relative z-10 text-center max-w-4xl mx-auto px-2">
          <div className="inline-block bg-warning-orange text-void-black font-label-caps text-[11px] md:text-label-caps px-3 py-1 mb-6 uppercase tracking-widest font-bold">
            {t('story.live_feed', 'System Breach Detected')}
          </div>
          
          <h1 className="font-display-hero text-[28px] sm:text-[40px] md:text-[54px] lg:text-[72px] text-primary-container drop-shadow-[0_0_15px_rgba(0,243,255,0.5)] mb-8 uppercase leading-tight font-extrabold select-none">
            {t('hero.title_line2_prefix', 'RECLAIM ')}
            <span className="text-secondary-container drop-shadow-[0_0_15px_rgba(254,0,254,0.5)] block sm:inline">
              {t('hero.title_line2_highlight', 'EARTH')}
            </span>
          </h1>
          
          <p className={`font-body-lg text-[14px] md:text-body-lg text-on-surface-variant max-w-2xl mx-auto mb-10 pl-4 bg-void-black/70 py-4 border-primary-container ${
            isRtl ? 'border-r-4 text-right pr-4 pl-0' : 'border-l-4 text-left pl-4 pr-0'
          }`}>
            &gt; INIT_RESISTANCE_PROTOCOL.exe<br />
            &gt; {t('hero.subtitle', 'A 3D action platformer built with Godot 4.6 and C#.')}<br />
            &gt; {t('footer.status', 'THE COLONIAL MAINFRAME WILL FALL.')}
          </p>
          
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <button 
              onClick={handleJoinStrike}
              className="bg-primary-container text-on-primary-container font-headline-md text-headline-md px-8 py-4 uppercase hover:bg-secondary-container hover:text-on-secondary-container transition-all hover:skew-x-[-12deg]"
            >
              {t('nav.play_now', 'Join the Strike')}
            </button>
            <button 
              onClick={handleViewIntel}
              className="border-2 border-primary-container text-primary-container font-headline-md text-headline-md px-8 py-4 uppercase hover:bg-primary-container/20 transition-all"
            >
              {t('story.briefing', 'View Intel')}
            </button>
          </div>
        </div>
      </section>

      {/* The Struggle Section (Asymmetric Layout) */}
      <section className="mb-32 grid grid-cols-1 lg:grid-cols-12 gap-8 lg:gap-gutter">
        <div className="lg:col-span-5 flex flex-col justify-center">
          <div className="text-secondary-container font-label-caps text-label-caps mb-4 uppercase font-semibold">
            &gt; {t('story.live_feed', 'DECRYPTED FILE: 001_HLF')}
          </div>
          <h2 className="font-headline-lg text-headline-lg-mobile md:text-headline-lg text-inverse-surface mb-6 uppercase">
            {t('story.title', 'The Human Liberation Front')}
          </h2>
          <div className="w-16 h-1 bg-primary-container mb-6"></div>
          <p className="font-body-md text-body-md text-on-surface-variant mb-6 leading-relaxed">
            {t('story.desc', 'They mapped our streets, cataloged our resources, and deemed us integrated. But integration is just another word for erasure. We are the glitch in their perfect colonial simulation.')}
          </p>
          
          <ul className="space-y-4 font-body-md text-body-md text-inverse-surface">
            <li className="flex items-center gap-3">
              <span className="material-symbols-outlined text-primary-container">terminal</span>
              <span>{t('story.timeline.axiom_title', 'AXIOM-7 Operations Coordination')}</span>
            </li>
            <li className="flex items-center gap-3">
              <span className="material-symbols-outlined text-warning-orange">emergency</span>
              <span>{t('story.timeline.displacement_title', 'Sabotage Displacement Mainframes')}</span>
            </li>
            <li className="flex items-center gap-3">
              <span className="material-symbols-outlined text-secondary-container">vpn_key</span>
              <span>{t('story.timeline.mission_title', 'Execute Core Insurgent Protocols')}</span>
            </li>
          </ul>
        </div>
        
        <div className="lg:col-span-7 relative min-h-[350px] md:min-h-[400px] neon-border bg-surface-container mt-8 lg:mt-0">
          <div 
            className="absolute inset-0 opacity-60 mix-blend-screen bg-cover bg-center" 
            style={{ 
              backgroundImage: "url('https://lh3.googleusercontent.com/aida-public/AB6AXuCs4UuTUn4dvlRFUQUQfKMbJ8heyrPYI8GfvVzIaJTC5oVRz5qJ36XPawx237rkgp58A9xyrQhkewzFuexgL8X8jk6pdbEYfzn4_FPRl3HIO6uwryPNBgCCbkYnSEh6FjAOGROJ367WLlrsBmvH1O4hgtJw1_7dl4ny4n29_lKVegCOyF2sFw9f5I9dqlF5If_Hbz718U2NLZD3AlIDlu64KVEWi0KCPvFmnb2bxjyCJunCbkZk7WkouniML7biOf2K8Fbvw04XKaY')" 
            }}
          ></div>
          <div className={`absolute bottom-4 left-4 right-4 bg-surface/90 backdrop-blur p-4 ${
            isRtl ? 'border-r-2 border-primary-container text-right' : 'border-l-2 border-primary-container text-left'
          }`}>
            <div className="font-label-caps text-label-caps text-primary-container mb-1">{t('home.status_operational', 'STATUS: OPERATIONAL')}</div>
            <div className="font-body-md text-body-md text-on-surface">{t('home.grid_hacked', 'Substation 4Alpha hacked. Local grid under HLF control.')}</div>
          </div>
        </div>
      </section>

      {/* Champions Grid (Bento Layout) */}
      <section className="mb-12">
        <div className="text-center mb-12">
          <h2 className="font-headline-lg text-headline-lg-mobile md:text-headline-lg text-primary-container uppercase drop-shadow-[0_0_8px_rgba(0,243,255,0.4)]">
            {t('characters.title', 'Champions of Resistance')}
          </h2>
          <p className="font-body-md text-body-md text-on-surface-variant mt-2">&gt; {t('characters.desc', 'Select your operative')}</p>
        </div>
        
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4 auto-rows-[220px] md:auto-rows-[250px]">
          {/* Operative 1 (Large) */}
          <div 
            onClick={handleJoinStrike}
            className="md:col-span-2 md:row-span-2 relative group overflow-hidden neon-border bg-surface-container hover:border-secondary-container transition-colors cursor-pointer"
          >
            <div 
              className="absolute inset-0 bg-cover bg-center opacity-50 group-hover:opacity-80 transition-opacity duration-300" 
              style={{ 
                backgroundImage: "url('https://lh3.googleusercontent.com/aida-public/AB6AXuC6w3uU4tz9oN0J89xLYYIwCsoK0SMoEkj5QeajfgPozk_pXOYKy3FksE8zlM3rXHxlfGRNehdioNhgBMS1wF-PDEBA7oykkr3LEqOmohyH25bqccBqdM3jhLsUZSJu0l70XNJyRWr3ySu6mDP_KBVWDwem16LsECy--myVTq5xgLC7xfZ5RooX6wvsJemDE6H8TKR6JKD2W6Snj00h6gAQMhsTBcfwGfqw4l902oq8yY5B58nGe2TFaaHx3vUqe-5xh8qWWjhE41Q')" 
              }}
            ></div>
            <div className={`absolute top-4 ${isRtl ? 'right-4' : 'left-4'} bg-void-black text-tertiary-fixed font-label-caps text-label-caps px-2 py-1`}>
              {t('characters.warriors.frog.role', 'ASSAULT / LEADER')}
            </div>
            <div className="absolute bottom-0 left-0 w-full p-6 bg-gradient-to-t from-void-black via-void-black/85 to-transparent">
              <h3 className="font-headline-md text-headline-md text-inverse-surface uppercase mb-2">CYBER SHOGUN</h3>
              <p className="font-body-md text-body-md text-on-surface-variant line-clamp-2">
                {t('characters.warriors.frog.desc', 'Frontline breaker. Specializes in heavy kinetic disruption and shield cracking.')}
              </p>
            </div>
          </div>

          {/* Operative 2 */}
          <div 
            onClick={handleJoinStrike}
            className="md:col-span-1 md:row-span-1 relative group overflow-hidden border border-outline-variant hover:border-primary-container bg-surface-container cursor-pointer"
          >
            <div className={`absolute top-2 ${isRtl ? 'left-2' : 'right-2'} text-warning-orange`}>
              <span className="material-symbols-outlined">wifi_tethering</span>
            </div>
            <div 
              className="absolute inset-0 bg-cover bg-center opacity-40 group-hover:opacity-60 transition-opacity" 
              style={{ 
                backgroundImage: "url('https://lh3.googleusercontent.com/aida-public/AB6AXuA_g2oz6phXI_reFuM6KkjSjfWiTVcZSRkr09bvtAFhSbmcVR7ZBlITskBRIL-3ygjErmMSylBYAwg9wSeQ0v-Mur2KEvy2ueKtzNtjSQPjDSh91MQ9J-h4Y4-cIJ8t8PHm6fjN797Xfq9IydjfExOP0OSG0-PVtFj-eV-6PVJah0hEcWvfMRBdMiyF4o8H6N9tQ1Y1FJrjsHlSWpp4jdvEpnPNrZAiEP-A9kzu4UUc4ZJXqEKGusNK9Jd7daJYhls-gFoxWSNF3eE')" 
              }}
            ></div>
            <div className="absolute bottom-0 left-0 w-full p-4 bg-void-black/90">
              <h3 className="font-headline-md text-[18px] text-primary-container uppercase">AEGIS RONIN</h3>
              <div className="font-label-caps text-label-caps text-on-surface-variant mt-1">
                {t('characters.warriors.dude.role', 'Infiltrator / Hacker')}
              </div>
            </div>
          </div>

          {/* Operative 3 */}
          <div 
            onClick={handleJoinStrike}
            className="md:col-span-1 md:row-span-1 relative group overflow-hidden border border-outline-variant hover:border-primary-container bg-surface-container cursor-pointer"
          >
            <div className={`absolute top-2 ${isRtl ? 'left-2' : 'right-2'} text-primary-container`}>
              <span className="material-symbols-outlined">precision_manufacturing</span>
            </div>
            <div 
              className="absolute inset-0 bg-cover bg-center opacity-40 group-hover:opacity-60 transition-opacity" 
              style={{ 
                backgroundImage: "url('https://lh3.googleusercontent.com/aida-public/AB6AXuAEVnIxaQvNx1Hjsvbxk24gpN0Q_HZlcXwiSRMWUKwfkfX24BWH-wtbBzO-d0ubLuX6rzIX1_6XzM3sFqbGiB_l1ghde_oC1gdpzAGTsKsFah79pMdWv-CnHB0bUMMpcRouHTblGn6vJV35FVQ9cLYZmZ_Hp0TZIXZAZlcJoxs6-T6lkwkSAmWLd6KLqwyaRZeTP9i0EglelRekLzi4khSK8h2COaQYu5seC7m43-8ZIoo6mmnRr4M5qsU8DH4IbXdMdSbJFSCXtcw')" 
              }}
            ></div>
            <div className="absolute bottom-0 left-0 w-full p-4 bg-void-black/90">
              <h3 className="font-headline-md text-[18px] text-primary-container uppercase">ONI MATRIX</h3>
              <div className="font-label-caps text-label-caps text-on-surface-variant mt-1">
                {t('characters.warriors.pink.role', 'Heavy / Support')}
              </div>
            </div>
          </div>

          {/* Lore Block */}
          <div className="md:col-span-2 md:row-span-1 bg-deep-grey p-6 border-l-4 border-warning-orange flex flex-col justify-center">
            <div className="font-body-md text-body-md text-warning-orange mb-2">{t('home.warning_casualty', '> WARNING: HIGH CASUALTY RATE')}</div>
            <p className="font-body-lg text-body-lg text-inverse-surface leading-normal italic">
              "{t('characters.warriors.guy.lore', "We don't recruit heroes. We recruit survivors. The mainframe doesn't sleep, and neither do we.")}"
            </p>
          </div>
        </div>
      </section>

    </div>
  );
}
