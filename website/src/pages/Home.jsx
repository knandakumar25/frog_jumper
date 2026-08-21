import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useLanguage } from '../translations/LanguageContext';

const getOriginalTitleLogoPath = (lang) => {
  const code = (lang || 'english').toLowerCase();
  if (code.includes('ar') || code.includes('arabic')) return '/images/axiom_force_title_logo_arabic.jpg';
  if (code.includes('pt') || code.includes('portuguese') || code.includes('brazilian')) return '/images/axiom_force_title_logo_brazilianportuguese.jpg';
  if (code.includes('fr') || code.includes('french')) return '/images/axiom_force_title_logo_eurofrench.jpg';
  if (code.includes('de') || code.includes('german')) return '/images/axiom_force_title_logo_german.jpg';
  if (code.includes('he') || code.includes('hebrew')) return '/images/axiom_force_title_logo_hebrew.jpg';
  if (code.includes('it') || code.includes('italian')) return '/images/axiom_force_title_logo_italian.jpg';
  if (code.includes('ja') || code.includes('japanese')) return '/images/axiom_force_title_logo_japanese.jpg';
  if (code.includes('ko') || code.includes('korean')) return '/images/axiom_force_title_logo_korean.jpg';
  if (code.includes('zh') || code.includes('mandarin')) return '/images/axiom_force_title_logo_mandarin.jpg';
  if (code.includes('pl') || code.includes('polish')) return '/images/axiom_force_title_logo_polish.jpg';
  if (code.includes('es') || code.includes('spanish')) return '/images/axiom_force_title_logo_spanish.jpg';
  if (code.includes('tr') || code.includes('turkish')) return '/images/axiom_force_title_logo_turkish.jpg';
  return '/images/axiom_force_title_logo.jpg';
};

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
          
          <div className="flex justify-center mb-8">
            <h1 className="sr-only">AXIOM FORCE</h1>
            <img 
              src={getOriginalTitleLogoPath(selectedLanguage)} 
              alt="AXIOM FORCE Logo" 
              className="w-full max-w-[650px] h-auto object-contain drop-shadow-[0_0_30px_rgba(0,243,255,0.9)] rounded-xl border border-primary-container/40 p-1 bg-void-black/60"
            />
          </div>
          
          <p className={`font-body-lg text-[14px] md:text-body-lg text-on-surface-variant max-w-2xl mx-auto mb-10 pl-4 bg-void-black/70 py-4 border-primary-container ${
            isRtl ? 'border-r-4 text-right pr-4 pl-0' : 'border-l-4 text-left pl-4 pr-0'
          }`}>
            &gt; INIT_RESISTANCE_PROTOCOL.exe<br />
            &gt; {t('hero.subtitle', 'A 3D action platformer. Reclaim Earth from alien occupiers.')}<br />
            &gt; {t('footer.status', 'THE COLONIAL MAINFRAME WILL FALL.')}<br />
            &gt; {t('footer.platform_desc', 'Target Platforms: Windows, Xbox (Series X/S & One), PlayStation 5, Nintendo Switch 2, Android, iOS, macOS, visionOS, Linux')}
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
          {/* Operative 1 (Large 3D Model Card) */}
          <div 
            onClick={handleJoinStrike}
            className="md:col-span-2 md:row-span-2 relative group overflow-hidden neon-border bg-surface-container hover:border-secondary-container transition-colors cursor-pointer flex flex-col justify-between"
          >
            <div className="absolute inset-0 z-0">
              <model-viewer
                src="/models/shogun.glb"
                alt="Cyber Shogun 3D"
                auto-rotate
                camera-controls
                shadow-intensity="1.5"
                exposure="1.1"
                interaction-prompt="none"
                style={{ width: '100%', height: '100%', backgroundColor: '#050505' }}
              >
              </model-viewer>
            </div>
            <div className={`relative z-10 p-4 flex justify-between items-start pointer-events-none`}>
              <span className="bg-void-black/90 text-tertiary-fixed font-label-caps text-label-caps px-2 py-1 border border-primary-container/30">
                3D_OPERATIVE // CYBER SHOGUN
              </span>
              <span className="bg-primary-container text-void-black font-label-caps text-[10px] px-2 py-0.5 font-bold">
                INTERACTIVE 360°
              </span>
            </div>
            <div className="relative z-10 p-6 bg-gradient-to-t from-void-black via-void-black/90 to-transparent pointer-events-none">
              <h3 className="font-headline-md text-headline-md text-inverse-surface uppercase mb-1">{t('squad.shogun', 'CYBER SHOGUN')}</h3>
              <p className="font-body-md text-body-md text-on-surface-variant line-clamp-2">
                {t('characters.warriors.shogun.desc', 'Frontline breaker. Specializes in heavy kinetic disruption and energy katana strikes.')}
              </p>
            </div>
          </div>

          {/* Operative 2 (Solaria 3D) */}
          <div 
            onClick={handleJoinStrike}
            className="md:col-span-1 md:row-span-1 relative group overflow-hidden border border-outline-variant hover:border-primary-container bg-surface-container cursor-pointer flex flex-col justify-between"
          >
            <div className="absolute inset-0 z-0">
              <model-viewer
                src="/models/solaria.glb"
                alt="Solaria 3D"
                auto-rotate
                camera-controls
                shadow-intensity="1"
                exposure="1.1"
                interaction-prompt="none"
                style={{ width: '100%', height: '100%', backgroundColor: '#050505' }}
              >
              </model-viewer>
            </div>
            <div className="relative z-10 p-2 text-warning-orange pointer-events-none">
              <span className="material-symbols-outlined text-sm bg-void-black/80 p-1 border border-warning-orange/30">flare</span>
            </div>
            <div className="relative z-10 p-3 bg-void-black/90 pointer-events-none">
              <h3 className="font-headline-md text-[16px] text-primary-container uppercase font-bold">SOLARIA</h3>
              <div className="font-label-caps text-[10px] text-on-surface-variant mt-0.5">
                SOLAR FLARE VANGUARD
              </div>
            </div>
          </div>

          {/* Operative 3 (Surya Prime 3D) */}
          <div 
            onClick={handleJoinStrike}
            className="md:col-span-1 md:row-span-1 relative group overflow-hidden border border-outline-variant hover:border-primary-container bg-surface-container cursor-pointer flex flex-col justify-between"
          >
            <div className="absolute inset-0 z-0">
              <model-viewer
                src="/models/surya.glb"
                alt="Surya Prime 3D"
                auto-rotate
                camera-controls
                shadow-intensity="1"
                exposure="1.1"
                interaction-prompt="none"
                style={{ width: '100%', height: '100%', backgroundColor: '#050505' }}
              >
              </model-viewer>
            </div>
            <div className="relative z-10 p-2 text-primary-container pointer-events-none">
              <span className="material-symbols-outlined text-sm bg-void-black/80 p-1 border border-primary-container/30">sunny</span>
            </div>
            <div className="relative z-10 p-3 bg-void-black/90 pointer-events-none">
              <h3 className="font-headline-md text-[16px] text-primary-container uppercase font-bold">SURYA PRIME</h3>
              <div className="font-label-caps text-[10px] text-on-surface-variant mt-0.5">
                COSMIC GUARDIAN
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
