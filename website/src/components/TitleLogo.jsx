import { useState } from 'react';
import { getTitleLogoPath } from '../translations/titleLogo';

/**
 * Localized AXIOM FORCE title image with a text fallback.
 *
 * Restored SpriteCook title art lives in public/images; retain a text fallback if
 * a deployment is missing a localized file. See docs/ART_REGENERATION.md.
 *
 * @param {object} props
 * @param {string} props.lang - Language or locale code passed to getTitleLogoPath.
 * @param {string} [props.alt] - Alternative text for the image.
 * @param {string} [props.className] - Classes for the image.
 * @param {string} [props.textClassName] - Size classes for the text fallback.
 * @param {string} [props.fetchPriority] - Optional fetchpriority hint for the image.
 * @returns {JSX.Element} The image, or the text title when the image is missing.
 */
export default function TitleLogo({ lang, alt = 'AXIOM FORCE', className = '', textClassName = 'text-xl', fetchPriority }) {
  const src = getTitleLogoPath(lang);
  // Remember which file failed, not just that one did: a language change points at a different
  // file, which may exist, so the image is tried again without resetting state in an effect.
  const [failedSrc, setFailedSrc] = useState(null);

  if (failedSrc === src) {
    return (
      <span
        className={`font-mono font-bold uppercase tracking-[0.2em] text-primary-container drop-shadow-[0_0_12px_rgba(0,243,255,0.7)] select-none whitespace-nowrap ${textClassName}`}
      >
        AXIOM FORCE
      </span>
    );
  }
  return (
    <img
      src={src}
      alt={alt}
      width="1920"
      height="588"
      className={className}
      fetchpriority={fetchPriority}
      onError={() => setFailedSrc(src)}
    />
  );
}
