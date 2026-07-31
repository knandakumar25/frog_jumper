import React, { useEffect } from 'react';
import { useTranslation } from 'react-i18next';

export default function AccessibilityModal({ isOpen, onClose }) {
  const { t } = useTranslation();

  useEffect(() => {
    const handleKeyDown = (e) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return (
    <div 
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md animate-fade-in"
      role="dialog"
      aria-modal="true"
      aria-labelledby="accessibility-title"
      onClick={onClose}
    >
      <div 
        className="relative w-full max-w-2xl bg-deep-grey/95 border-2 border-primary-container rounded-lg p-6 md:p-8 shadow-[0_0_30px_rgba(0,243,255,0.25)] text-on-surface max-h-[85vh] overflow-y-auto"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex justify-between items-center border-b border-primary-container/30 pb-4 mb-6">
          <div className="flex items-center gap-3">
            <span className="text-2xl text-primary-container font-bold">♿</span>
            <h2 id="accessibility-title" className="text-xl md:text-2xl font-orbitron font-bold text-primary-container uppercase tracking-wider">
              {t('accessibility.title', 'Accessibility Statement')}
            </h2>
          </div>
          <button
            onClick={onClose}
            className="text-on-surface-variant hover:text-tertiary-container transition-colors text-2xl font-bold px-2 py-1"
            aria-label="Close Accessibility Statement"
          >
            ✕
          </button>
        </div>

        {/* Statement Content */}
        <div className="space-y-5 text-sm md:text-base leading-relaxed text-on-surface-variant">
          <section>
            <h3 className="text-base font-orbitron font-semibold text-secondary-container mb-2 uppercase tracking-wide">
              {t('accessibility.commitment_title', '// Our Commitment')}
            </h3>
            <p>
              {t('accessibility.commitment_text', 'Axiom Force: Reclaim Earth is committed to ensuring digital accessibility for people with disabilities. We are continually improving the user experience for everyone and applying relevant accessibility standards (WCAG 2.1 Level AA).')}
            </p>
          </section>

          <section className="bg-void-black/60 p-4 rounded border border-outline-variant/30">
            <h3 className="text-base font-orbitron font-semibold text-tertiary-container mb-2 uppercase tracking-wide">
              {t('accessibility.features_title', '// Accessibility Features')}
            </h3>
            <ul className="list-disc list-inside space-y-2 text-on-surface">
              <li>
                <strong className="text-primary-container">Keyboard Navigation:</strong> Full keyboard accessibility across the website and in-game UI with clear focus indicators and customizable keybindings.
              </li>
              <li>
                <strong className="text-primary-container">High Contrast Design:</strong> Designed with high contrast cyberpunk colors, distinct focus states, and scalable vector icons for optimal visibility.
              </li>
              <li>
                <strong className="text-primary-container">Subtitles & Audio Controls:</strong> Full text captions for narrative briefings (such as AXIOM-7 transmissions) with independent volume controls for SFX and Music.
              </li>
              <li>
                <strong className="text-primary-container">Screen Reader Ready:</strong> Structured with semantic HTML5 elements, descriptive alt text for game graphics, and ARIA labels.
              </li>
            </ul>
          </section>

          <section>
            <h3 className="text-base font-orbitron font-semibold text-secondary-container mb-2 uppercase tracking-wide">
              {t('accessibility.conformance_title', '// Conformance Status')}
            </h3>
            <p>
              {t('accessibility.conformance_text', 'The Web Content Accessibility Guidelines (WCAG) defines requirements for designers and developers to improve accessibility for people with disabilities. Axiom Force is partially conformant with WCAG 2.1 Level AA standards.')}
            </p>
          </section>

          <section className="border-t border-outline-variant/30 pt-4 mt-6">
            <h3 className="text-base font-orbitron font-semibold text-tertiary-container mb-2 uppercase tracking-wide">
              {t('accessibility.feedback_title', '// Feedback & Support')}
            </h3>
            <p>
              {t('accessibility.feedback_text', 'We welcome your feedback on the accessibility of Axiom Force. If you encounter accessibility barriers or need assistance, please contact our support team:')}
            </p>
            <div className="mt-3 p-3 bg-primary-container/10 border border-primary-container/30 rounded text-sm font-mono text-primary-container">
              Email: accessibility@axiomforce-game.com
            </div>
          </section>
        </div>

        {/* Footer Action */}
        <div className="mt-8 pt-4 border-t border-outline-variant/30 flex justify-end">
          <button
            onClick={onClose}
            className="px-6 py-2 bg-primary-container/20 border border-primary-container text-primary-container hover:bg-primary-container hover:text-black transition-all rounded font-orbitron text-sm uppercase tracking-wider font-semibold"
          >
            {t('accessibility.close', 'Close Statement')}
          </button>
        </div>
      </div>
    </div>
  );
}
