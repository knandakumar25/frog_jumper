import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useLanguage } from '../translations/LanguageContext';

const FAQ_DATA = [
  {
    id: 1,
    category: 'OPERATION',
    question: { key: 'faq.q1', default: 'What is Axiom Force?' },
    answer: { key: 'faq.a1', default: 'A high-octane 3D platformer where you lead the Human Liberation Front (HLF) in a desperate war to reclaim Earth from an advanced alien occupation.' },
  },
  {
    id: 2,
    category: 'TIMELINE',
    question: { key: 'faq.q2', default: 'When is the game being released?' },
    answer: { key: 'faq.a2', default: 'The resistance is still mobilizing. We are currently in active development. Stay tuned to the Tech Hub for deployment dates.' },
  },
  {
    id: 3,
    category: 'PLATFORMS',
    question: { key: 'faq.q3', default: 'Which platforms are supported?' },
    answer: { key: 'faq.a3', default: 'Our systems are compatible with Windows, macOS, Linux, iOS, Android, and visionOS.' },
  },
  {
    id: 4,
    category: 'COMBAT',
    question: { key: 'faq.q4', default: 'Is it a multiplayer experience?' },
    answer: { key: 'faq.a4', default: 'While the main story is a focused single-player campaign, we are exploring cooperative intel-gathering missions for the squad.' },
  },
  {
    id: 5,
    category: 'OPERATION',
    question: { key: 'faq.q5', default: 'How do I join the HLF?' },
    answer: { key: 'faq.a5', default: 'You can enlist by signing up for an account on the official portal to receive encrypted updates and early access intel.' },
  },
  {
    id: 6,
    category: 'TIMELINE',
    question: { key: 'faq.q6', default: 'Where can I find dev logs?' },
    answer: { key: 'faq.a6', default: 'All technical breakthroughs and development milestones are archived in the Tech Hub.' },
  },
  {
    id: 7,
    category: 'PLATFORMS',
    question: { key: 'faq.q7', default: 'What are the system requirements?' },
    answer: { key: 'faq.a7', default: 'As long as you have a functioning terminal with a basic GPU and a desire for liberation, you can run Axiom Force. Specific specs will be released soon.' },
  },
];

export default function FAQ() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);
  const [openId, setOpenId] = useState(1);
  const [activeCategory, setActiveCategory] = useState('ALL');

  const categories = ['ALL', 'OPERATION', 'PLATFORMS', 'TIMELINE', 'COMBAT'];

  const filteredFaq = activeCategory === 'ALL'
    ? FAQ_DATA
    : FAQ_DATA.filter(item => item.category === activeCategory);

  const toggleItem = (id) => {
    setOpenId(openId === id ? null : id);
  };

  return (
    <div className="pt-24 md:pt-28 pb-24 px-4 md:px-margin-desktop max-w-container-max-width mx-auto w-full">
      
      {/* Header Bar */}
      <header className="mb-10 border-b border-primary-container/20 pb-6 flex flex-col md:flex-row justify-between items-start md:items-end gap-4">
        <div>
          <div className="font-mono text-[11px] text-primary-container tracking-[0.25em] uppercase mb-1 flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-primary-container animate-pulse"></span>
            ARCHIVE // ENCRYPTED INTELLIGENCE & INQUIRIES
          </div>
          <h1 className="font-display-hero text-2xl md:text-4xl text-primary-container uppercase font-extrabold tracking-tight text-glow-cyan">
            {t('pages.faq.title', 'Knowledge Base')}
          </h1>
          <p className="font-mono text-[12px] md:text-[13px] text-on-surface-variant mt-2 max-w-2xl leading-relaxed">
            {t('pages.faq.description', 'Access encrypted intel on operation Reclaim Earth. If you have a question not listed here, contact the HLF command center.')}
          </p>
        </div>

        <div className="font-mono text-[11px] tracking-wider text-right hidden sm:block">
          <div className="text-tertiary-container font-semibold">SECURITY: DECLASSIFIED</div>
          <div className="text-on-surface-variant/70">7/7 PACKETS VERIFIED</div>
        </div>
      </header>

      {/* Category Filter Tabs */}
      <div className="flex flex-wrap items-center gap-2 mb-8 font-mono text-[11px]">
        {categories.map(cat => (
          <button
            key={cat}
            onClick={() => setActiveCategory(cat)}
            className={`px-3 py-1.5 border transition-all duration-150 uppercase tracking-wider ${
              activeCategory === cat
                ? 'bg-primary-container/20 border-primary-container text-primary-container shadow-[0_0_10px_rgba(0,243,255,0.2)]'
                : 'bg-surface-container-low/50 border-outline-variant/30 text-on-surface-variant hover:border-primary-container/40 hover:text-on-surface'
            }`}
          >
            [{cat}]
          </button>
        ))}
      </div>

      {/* Accordion List */}
      <div className="flex flex-col gap-3 mb-16">
        {filteredFaq.map((item) => {
          const isOpen = openId === item.id;
          return (
            <div
              key={item.id}
              className={`border transition-all duration-200 overflow-hidden ${
                isOpen
                  ? 'bg-surface-container-low/90 border-primary-container shadow-[0_0_15px_rgba(0,243,255,0.1)]'
                  : 'bg-surface-container-low/40 border-outline-variant/30 hover:border-primary-container/40'
              }`}
            >
              <button
                onClick={() => toggleItem(item.id)}
                className="w-full text-left px-5 py-4 flex items-center justify-between gap-4 cursor-pointer focus:outline-none"
              >
                <div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-4 flex-grow">
                  <span className="font-mono text-[10px] tracking-widest text-primary-container/70 border border-primary-container/30 px-2 py-0.5 bg-void-black w-fit">
                    PACKET // 0x0{item.id}
                  </span>
                  <span className={`font-display-hero text-[14px] md:text-[16px] uppercase tracking-wide transition-colors ${
                    isOpen ? 'text-primary-container font-bold' : 'text-on-surface'
                  }`}>
                    <span className="text-primary-container/50 mr-2 font-mono">&gt;</span>
                    {t(item.question.key, item.question.default)}
                  </span>
                </div>

                <span className={`material-symbols-outlined text-[20px] transition-transform duration-200 ${
                  isOpen ? 'rotate-180 text-primary-container' : 'text-on-surface-variant'
                }`}>
                  expand_more
                </span>
              </button>

              {isOpen && (
                <div className="px-5 pb-5 pt-1 border-t border-primary-container/20 font-mono text-[12px] md:text-[13px] text-on-surface-variant leading-relaxed">
                  <div className="border-l-2 border-primary-container/40 pl-4 py-1 bg-void-black/60 p-3">
                    <div className="text-tertiary-container text-[10px] tracking-widest uppercase mb-1 font-bold">
                      // DECRYPTED TRANSMISSION RESPONSE
                    </div>
                    <p className="text-on-surface">
                      {t(item.answer.key, item.answer.default)}
                    </p>
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </div>

      {/* Terminal Footer CTA */}
      <div className="bg-surface-container-low/80 border border-outline-variant/30 p-6 md:p-8 flex flex-col md:flex-row justify-between items-start md:items-center gap-6">
        <div>
          <div className="font-mono text-[10px] text-warning-orange uppercase tracking-widest mb-1">
            // UNRESOLVED INQUIRIES
          </div>
          <h3 className="font-display-hero text-lg md:text-xl text-inverse-surface uppercase font-bold">
            REQUIRE FURTHER TACTICAL BRIEFING?
          </h3>
          <p className="font-mono text-[12px] text-on-surface-variant mt-1">
            Check the technical archives or connect with the resistance team on the Tech Hub.
          </p>
        </div>

        <button
          onClick={() => navigate(`/${currentLocale}/tech-hub`)}
          className="cyber-cut bg-primary-container text-void-black font-headline-md text-[13px] px-6 py-2.5 uppercase font-bold hover:bg-secondary-container hover:text-white transition-all flex items-center gap-2 whitespace-nowrap"
        >
          <span className="material-symbols-outlined text-[16px]">terminal</span>
          ACCESS TECH HUB
        </button>
      </div>

    </div>
  );
}
