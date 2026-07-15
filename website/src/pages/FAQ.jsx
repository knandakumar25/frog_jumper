import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';

const FAQ_DATA = [
  {
    id: 1,
    question: { key: 'faq.q1', default: 'What is Axiom Force: Reclaim Earth?' },
    answer: { key: 'faq.a1', default: 'A high-octane 3D platformer where you lead the Human Liberation Front (HLF) in a desperate war to reclaim Earth from an advanced alien occupation.' },
  },
  {
    id: 2,
    question: { key: 'faq.q2', default: 'When is the game being released?' },
    answer: { key: 'faq.a2', default: 'The resistance is still mobilizing. We are currently in active development. Stay tuned to the Tech Hub for deployment dates.' },
  },
  {
    id: 3,
    question: { key: 'faq.q3', default: 'Which platforms are supported?' },
    answer: { key: 'faq.a3', default: 'Our systems are compatible with Windows, macOS, Linux, iOS, Android, and visionOS.' },
  },
  {
    id: 4,
    question: { key: 'faq.q4', default: 'Is it a multiplayer experience?' },
    answer: { key: 'faq.a4', default: 'While the main story is a focused single-player campaign, we are exploring cooperative intel-gathering missions for the squad.' },
  },
  {
    id: 5,
    question: { key: 'faq.q5', default: 'How do I join the HLF?' },
    answer: { key: 'faq.a5', default: 'You can enlist by signing up for an account on the official portal to receive encrypted updates and early access intel.' },
  },
  {
    id: 6,
    question: { key: 'faq.q6', default: 'Where can I find dev logs?' },
    answer: { key: 'faq.a6', default: 'All technical breakthroughs and development milestones are archived in the Tech Hub.' },
  },
  {
    id: 7,
    question: { key: 'faq.q7', default: 'What are the system requirements?' },
    answer: { key: 'faq.a7', default: 'As long as you have a functioning terminal with a basic GPU and a desire for liberation, you can run Axiom Force. Specific specs will be released soon.' },
  },
];

export default function FAQ() {
  const { t } = useTranslation();
  const [openId, setOpenId] = useState(null);

  const toggleItem = (id) => {
    setOpenId(openId === id ? null : id);
  };

  return (
    <div className="pt-32 pb-24 px-4 md:px-margin-desktop max-w-container-max-width mx-auto w-full">
      <header className="mb-12">
        <h1 className="font-display-hero text-headline-lg text-primary-container uppercase tracking-tighter drop-shadow-[0_0_12px_rgba(0,243,255,0.6)] mb-4">
          {t('pages.faq.title', 'Knowledge Base')}
        </h1>
        <div className="border-l-4 border-primary-container pl-4 max-w-2xl">
          <p className="font-body-md text-on-surface-variant opacity-80 leading-relaxed">
            {t('pages.faq.description', 'Access encrypted intel on operation Reclaim Earth. If you have a question not listed here, contact the HLF command center.')}
          </p>
        </div>
      </header>

      <div className="flex flex-col gap-4">
        {FAQ_DATA.map((item) => (
          <div
            key={item.id}
            className={`group transition-all duration-200 border ${
              openId === item.id
                ? 'bg-surface-container-low border-primary-container shadow-[0_0_15px_rgba(0,243,255,0.1)]'
                : 'bg-surface-container-highest border-outline-variant/30 hover:border-primary-container/50'
            }`}
          >
            <button
              onClick={() => toggleItem(item.id)}
              className="w-full text-left px-4 py-4 flex items-center justify-between"
            >
              <span className={`font-headline-md uppercase tracking-wider transition-colors ${
                openId === item.id ? 'text-primary-container' : 'text-on-surface-variant group-hover:text-primary'
              }`}>
                <span className="opacity-50 mr-2">&gt; Q:</span> {t(item.question.key, item.question.default)}
              </span>
              <span className={`material-symbols-outlined transition-transform duration-300 ${
                openId === item.id ? 'rotate-180 text-primary-container' : 'text-on-surface-variant'
              }`}>
                expand_more
              </span>
            </button>

            {openId === item.id && (
              <div className="px-4 pb-4 pt-0 animate-in fade-in slide-in-from-top-2 duration-300">
                <div className="border-l-2 border-primary-container/30 pl-4 pt-2">
                  <p className="font-body-md text-on-surface-variant leading-relaxed">
                    <span className="opacity-50 mr-2">&gt; A:</span> {t(item.answer.key, item.answer.default)}
                  </p>
                </div>
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
