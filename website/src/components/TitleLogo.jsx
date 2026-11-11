import { useState } from 'react';
import { getTitleLogoPath } from '../translations/titleLogo';

/**
 * Localized AXIOM FORCE title image with a text fallback.
 *
 * The title images in public/images were made with Blender and deleted on 2026-11-11; they are
 * waiting to be remade with SpriteCook (docs/ART_REGENERATION.md). Until a file comes back, the
 * image fails to load and this component shows the words "AXIOM FORCE" instead of a broken-image
 * icon. A regenerated file at the same path is shown again with no code change.
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
      className={className}
      fetchpriority={fetchPriority}
      onError={() => setFailedSrc(src)}
    />
  );
}
