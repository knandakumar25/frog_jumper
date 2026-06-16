import React, { useState } from 'react';
import { useLanguage } from '../translations/LanguageContext';

export default function DesignSystem() {
  const { selectedLanguage } = useLanguage();
  const [copiedColor, setCopiedColor] = useState(null);

  const colors = [
    { name: 'Void Black', hex: '#050505', desc: 'Stealth baseline background', type: 'base' },
    { name: 'Deep Grey', hex: '#1A1A1A', desc: 'Secondary elevation container background', type: 'base' },
    { name: 'Neon Cyan (Primary)', hex: '#00f3ff', desc: 'Tactical interfaces and friendly technology', type: 'accent' },
    { name: 'Electric Magenta (Secondary)', hex: '#fe00fe', desc: 'High energy overrides and HLF actions', type: 'accent' },
    { name: 'Toxic Green (Tertiary)', hex: '#36fd0f', desc: 'Successful hacks and active status indicators', type: 'accent' },
    { name: 'Warning Orange', hex: '#FF4D00', desc: 'Critical alerts, threat states, and warnings', type: 'alert' }
  ];

  const handleCopyColor = (hex) => {
    navigator.clipboard.writeText(hex);
    setCopiedColor(hex);
    setTimeout(() => setCopiedColor(null), 1500);
  };

  const isRtl = selectedLanguage === 'arabic' || selectedLanguage === 'hebrew';

  return (
    <div className="pt-32 pb-24 px-4 md:px-margin-desktop max-w-container-max-width mx-auto w-full">
      
      {/* Header */}
      <header className="mb-16 relative">
        <div className={`absolute top-0 text-secondary-container opacity-10 font-display-hero text-display-hero pointer-events-none select-none ${
          isRtl ? '-right-8' : '-left-8'
        }`}>
          SYS.DS //
        </div>
        <h1 className="font-display-hero text-headline-lg-mobile md:text-display-hero text-primary-container mb-4 uppercase tracking-tighter drop-shadow-[0_0_12px_rgba(0,243,255,0.5)] font-extrabold">
          Glitch-Noir Kinetic
        </h1>
        <p className={`font-body-lg text-body-lg text-on-surface-variant max-w-3xl border-secondary-container pl-6 ${
          isRtl ? 'border-r-4 pr-6 pl-0 text-right' : 'border-l-4 pl-6 pr-0 text-left'
        }`}>
          Interactive guide and token showroom for the HLF rebel mainframe design system. High contrast, visceral layouts, and CRT friction overlays.
        </p>
      </header>

      <div className="glitch-divider"></div>

      {/* Brand values grid */}
      <section className="mb-20 grid grid-cols-1 md:grid-cols-4 gap-6">
        <div className="bg-surface-container-low p-5 brutal-border border-outline-variant/30">
          <h3 className="font-label-caps text-label-caps text-primary mb-2">// DIGITAL DEFIANCE</h3>
          <p className="font-body-md text-[13px] text-on-surface-variant leading-relaxed">
            Raw, unrefined edges mixed with glowing cathode effects to breach the corporate mainframe.
          </p>
        </div>
        <div className="bg-surface-container-low p-5 brutal-border border-outline-variant/30">
          <h3 className="font-label-caps text-label-caps text-secondary mb-2">// VISUAL FRICTION</h3>
          <p className="font-body-md text-[13px] text-on-surface-variant leading-relaxed">
            Intentional CRT scanlines and minor color aberrations to represent a localized rebel broadcast.
          </p>
        </div>
        <div className="bg-surface-container-low p-5 brutal-border border-outline-variant/30">
          <h3 className="font-label-caps text-label-caps text-warning-orange mb-2">// RIGID TACTICAL GRID</h3>
          <p className="font-body-md text-[13px] text-on-surface-variant leading-relaxed">
            Asymmetric box models that feel like components plugged directly into a motherboard.
          </p>
        </div>
        <div className="bg-surface-container-low p-5 brutal-border border-outline-variant/30">
          <h3 className="font-label-caps text-label-caps text-tertiary-container mb-2">// LOW-FI SURVIVAL</h3>
          <p className="font-body-md text-[13px] text-on-surface-variant leading-relaxed">
            Monospaced data printouts, sharp 0px border corners, and low-res pixel density motifs.
          </p>
        </div>
      </section>

      {/* Color Tokens Panel */}
      <section className="mb-20">
        <h2 className="font-headline-lg text-headline-lg-mobile md:text-headline-lg text-primary mb-8 uppercase font-bold tracking-tight">
          Color Swatches
        </h2>
        
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {colors.map((color) => (
            <div 
              key={color.hex}
              onClick={() => handleCopyColor(color.hex)}
              className="bg-surface-container-low border border-outline-variant/40 hover:border-primary transition-all p-4 cursor-pointer flex items-center gap-4 relative group"
            >
              {/* Live Preview block */}
              <div 
                className="w-14 h-14 border border-white/10 relative" 
                style={{ backgroundColor: color.hex }}
              >
                <div className="absolute inset-0 bg-scanline-overlay"></div>
              </div>
              
              <div className="flex-grow">
                <h4 className="font-headline-md text-[16px] text-white uppercase font-bold mb-0.5">{color.name}</h4>
                <code className="font-body-md text-[12px] text-primary">{color.hex}</code>
                <p className="font-body-md text-[11px] text-on-surface-variant leading-tight mt-1">{color.desc}</p>
              </div>

              {/* Copy sticker indicator */}
              <span className="absolute bottom-2 right-2 text-[10px] font-label-caps text-outline-variant opacity-0 group-hover:opacity-100 transition-opacity">
                {copiedColor === color.hex ? 'COPIED!' : 'CLICK TO COPY'}
              </span>
            </div>
          ))}
        </div>
      </section>

      {/* Typography Scale */}
      <section className="mb-20">
        <h2 className="font-headline-lg text-headline-lg-mobile md:text-headline-lg text-secondary mb-8 uppercase font-bold tracking-tight">
          Typography Stack
        </h2>
        
        <div className="bg-surface-container-low brutal-border border-outline-variant/30 p-6 md:p-8 space-y-8">
          {/* Display Hero */}
          <div className="border-b border-outline-variant/20 pb-6">
            <span className="font-label-caps text-[11px] text-outline block mb-2">DISPLAY HERO // Space Grotesk / 72px / Bold</span>
            <h1 className="font-display-hero text-[40px] md:text-display-hero uppercase text-primary font-bold leading-tight select-none">
              RECLAIM EARTH
            </h1>
          </div>

          {/* Headline Large */}
          <div className="border-b border-outline-variant/20 pb-6">
            <span className="font-label-caps text-[11px] text-outline block mb-2">HEADLINE LG // Space Grotesk / 40px / Bold</span>
            <h2 className="font-headline-lg text-headline-lg-mobile md:text-headline-lg uppercase text-white font-bold leading-none">
              TACTICAL INTEL REPORT
            </h2>
          </div>

          {/* Body Large */}
          <div className="border-b border-outline-variant/20 pb-6">
            <span className="font-label-caps text-[11px] text-outline block mb-2">BODY LG // JetBrains Mono / 18px / Normal</span>
            <p className="font-body-lg text-body-lg text-on-surface leading-relaxed">
              &gt; Ground forces have breached Sector 4 containment parameters. Launching active sync grid lock.
            </p>
          </div>

          {/* Body Medium */}
          <div className="border-b border-outline-variant/20 pb-6">
            <span className="font-label-caps text-[11px] text-outline block mb-2">BODY MD // JetBrains Mono / 14px / Normal</span>
            <p className="font-body-md text-body-md text-on-surface-variant leading-relaxed">
              The Xelthara arrived not to conquer, but to administer. We operate in the dead zones, striking from the shadows of brutalist monoliths.
            </p>
          </div>

          {/* Label Caps */}
          <div>
            <span className="font-label-caps text-[11px] text-outline block mb-2">LABEL CAPS // Space Mono / 12px / Bold / Monospaced</span>
            <span className="font-label-caps text-label-caps text-warning-orange uppercase block">
              STATUS: RESISTANCE COMM LINK SECURED // LATENCY 4ms
            </span>
          </div>
        </div>
      </section>

      {/* Live Component Showroom */}
      <section className="mb-12">
        <h2 className="font-headline-lg text-headline-lg-mobile md:text-headline-lg text-tertiary-fixed-dim mb-8 uppercase font-bold tracking-tight">
          Component Showroom
        </h2>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          
          {/* Interactive Buttons Card */}
          <div className="bg-surface-container p-6 brutal-border border-outline-variant/40 flex flex-col gap-6">
            <h3 className="font-label-caps text-label-caps text-white">// BUTTON INTERACTIVE STATES</h3>
            
            <div className="flex flex-col gap-4">
              <div>
                <span className="font-body-md text-[11px] text-on-surface-variant block mb-2">Primary Action (flickers and skews to Magenta on hover)</span>
                <button className="brutal-btn font-headline-md text-headline-md px-6 py-3 uppercase font-bold w-full max-w-[280px]">
                  Join the Strike
                </button>
              </div>
              
              <div>
                <span className="font-body-md text-[11px] text-on-surface-variant block mb-2">Secondary Outline</span>
                <button className="border-2 border-primary text-primary hover:bg-primary/10 transition-all font-headline-md text-headline-md px-6 py-3 uppercase font-bold w-full max-w-[280px]">
                  View Intel
                </button>
              </div>

              <div>
                <span className="font-body-md text-[11px] text-on-surface-variant block mb-2">Warning Action (skew animation)</span>
                <button className="border-2 border-warning-orange text-warning-orange hover:bg-warning-orange hover:text-void-black font-label-caps text-label-caps py-2.5 px-6 uppercase font-bold tracking-wider glitch-hover transition-colors w-full max-w-[280px]">
                  RECLAIM EARTH
                </button>
              </div>
            </div>
          </div>

          {/* Cards and Inputs */}
          <div className="bg-surface-container p-6 brutal-border border-outline-variant/40 flex flex-col gap-6">
            <h3 className="font-label-caps text-label-caps text-white">// TACTICAL HOVER CARD & INPUT</h3>
            
            <div className="flex flex-col gap-6">
              {/* Corner Bracket Card */}
              <div className="bg-surface-container-low p-4 relative border border-primary-container/20 group">
                <div className="bracket-tl"></div><div className="bracket-br"></div>
                <h4 className="font-headline-md text-[16px] text-primary-container uppercase mb-1 font-bold">
                  Sector 4 Alpha Card
                </h4>
                <p className="font-body-md text-[12px] text-on-surface-variant leading-relaxed">
                  Interactive frame container equipped with four custom bracket corners and double glow shaders on focus.
                </p>
              </div>

              {/* Pulsing Input */}
              <div>
                <label className="font-label-caps text-[11px] text-on-surface-variant block mb-2">
                  SECURE COMMS TERMINAL (click to see pulsing indicator border)
                </label>
                <div className="relative">
                  <input 
                    type="text" 
                    placeholder="Enter decryption code..."
                    className="w-full bg-void-black border-b-2 border-primary-container/30 focus:border-primary-container focus:ring-0 text-white font-body-md text-[14px] py-3.5 px-4 focus:bg-primary-container/5 transition-all outline-none"
                  />
                </div>
              </div>
            </div>
          </div>

        </div>
      </section>

    </div>
  );
}
