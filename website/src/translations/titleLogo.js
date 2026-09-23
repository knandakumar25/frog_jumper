/**
 * Maps any language identifier, locale code (e.g. 'vi_vn', 'es_es'), or dialect
 * to the corresponding new cyberpunk 2D title design image.
 * Supports English and all 17 localized game versions with seamless fallback.
 *
 * @param {string} lang - Language identifier, locale code, or language name
 * @returns {string} Absolute URL path to the transparent 2D title logo image
 */
export function getTitleLogoPath(lang) {
  if (!lang) return '/images/axiom_force_title_2d.png';
  const code = lang.toLowerCase().trim();

  // Arabic
  if (code.startsWith('ar') || code.includes('arabic')) {
    return '/images/axiom_force_title_2d_arabic.png';
  }
  // Vietnamese
  if (code.startsWith('vi') || code.includes('vietnam')) {
    return '/images/axiom_force_title_2d_vietnamese.png';
  }
  // Traditional Chinese (Taiwan, Hong Kong, Macau)
  if (
    code.includes('traditional') ||
    code.includes('zh_tw') ||
    code.includes('zh_hk') ||
    code.includes('zh_mo') ||
    code.includes('hant')
  ) {
    return '/images/axiom_force_title_2d_traditionalchinese.png';
  }
  // Mandarin / Simplified Chinese
  if (
    code.startsWith('zh') ||
    code.includes('mandarin') ||
    code.includes('chinese') ||
    code.includes('hans') ||
    code.includes('cantonese')
  ) {
    return '/images/axiom_force_title_2d_mandarin.png';
  }
  // Japanese
  if (code.startsWith('ja') || code.includes('japan')) {
    return '/images/axiom_force_title_2d_japanese.png';
  }
  // Korean
  if (code.startsWith('ko') || code.includes('korea')) {
    return '/images/axiom_force_title_2d_korean.png';
  }
  // Spanish
  if (code.startsWith('es') || code.includes('spanish') || code.includes('castilian')) {
    return '/images/axiom_force_title_2d_spanish.png';
  }
  // French
  if (code.startsWith('fr') || code.includes('french')) {
    return '/images/axiom_force_title_2d_eurofrench.png';
  }
  // Portuguese (Brazil / Portugal)
  if (code.startsWith('pt') || code.includes('portuguese') || code.includes('brazil')) {
    return '/images/axiom_force_title_2d_brazilianportuguese.png';
  }
  // German
  if (code.startsWith('de') || code.includes('german') || code.includes('deutsch')) {
    return '/images/axiom_force_title_2d_german.png';
  }
  // Italian
  if (code.startsWith('it') || code.includes('italian')) {
    return '/images/axiom_force_title_2d_italian.png';
  }
  // Polish
  if (code.startsWith('pl') || code.includes('polish') || code.includes('polski')) {
    return '/images/axiom_force_title_2d_polish.png';
  }
  // Turkish
  if (code.startsWith('tr') || code.includes('turkish') || code.includes('türk')) {
    return '/images/axiom_force_title_2d_turkish.png';
  }
  // Hebrew
  if (code.startsWith('he') || code.includes('hebrew') || code.startsWith('iw')) {
    return '/images/axiom_force_title_2d_hebrew.png';
  }
  // Indonesian
  if (code.startsWith('id') || code.includes('indonesian') || code.includes('bahasa')) {
    return '/images/axiom_force_title_2d_indonesian.png';
  }
  // Thai
  if (code.startsWith('th') || code.includes('thai')) {
    return '/images/axiom_force_title_2d_thai.png';
  }

  // Default: English new 2D Title
  return '/images/axiom_force_title_2d.png';
}

// Alias for backwards compatibility
export const getOriginalTitleLogoPath = getTitleLogoPath;
export default getTitleLogoPath;
