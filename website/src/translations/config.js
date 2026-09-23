// Supported languages for Axiom Force (Fortnite-style direct language selection)
export const SUPPORTED_LANGUAGES = [
  { id: 'english', code: 'en', name: 'English', englishName: 'English' },
  { id: 'spanish', code: 'es', name: 'Español', englishName: 'Spanish' },
  { id: 'vietnamese', code: 'vi', name: 'Tiếng Việt', englishName: 'Vietnamese' },
  { id: 'japanese', code: 'ja', name: '日本語', englishName: 'Japanese' },
  { id: 'korean', code: 'ko', name: '한국어', englishName: 'Korean' },
  { id: 'mandarin', code: 'zh', name: '简体中文', englishName: 'Simplified Chinese' },
  { id: 'traditional_chinese', code: 'zh_tw', name: '繁體中文', englishName: 'Traditional Chinese' },
  { id: 'french', code: 'fr', name: 'Français', englishName: 'French' },
  { id: 'german', code: 'de', name: 'Deutsch', englishName: 'German' },
  { id: 'portuguese', code: 'pt', name: 'Português', englishName: 'Portuguese' },
  { id: 'italian', code: 'it', name: 'Italiano', englishName: 'Italian' },
  { id: 'polish', code: 'pl', name: 'Polski', englishName: 'Polish' },
  { id: 'turkish', code: 'tr', name: 'Türkçe', englishName: 'Turkish' },
  { id: 'arabic', code: 'ar', name: 'العربية', englishName: 'Arabic' },
  { id: 'hebrew', code: 'he', name: 'עברית', englishName: 'Hebrew' },
  { id: 'indonesian', code: 'id', name: 'Bahasa Indonesia', englishName: 'Indonesian' },
  { id: 'thai', code: 'th', name: 'ไทย', englishName: 'Thai' },
];

export const NATIVE_LANGUAGE_NAMES = {
  english: 'English',
  american_english: 'English',
  british_english: 'English',
  canadian_english: 'English',
  australian_english: 'English',
  international_english: 'English',
  spanish: 'Español',
  castilian_spanish: 'Español',
  latin_american_spanish: 'Español',
  vietnamese: 'Tiếng Việt',
  japanese: '日本語',
  korean: '한국어',
  mandarin: '简体中文',
  simplified_chinese: '简体中文',
  traditional_chinese: '繁體中文',
  traditionalchinese: '繁體中文',
  cantonese: '粵語',
  french: 'Français',
  eurofrench: 'Français',
  canadianfrench: 'Français',
  african_french: 'Français',
  haitian_french: 'Français',
  german: 'Deutsch',
  portuguese: 'Português',
  europortuguese: 'Português',
  brazilianportuguese: 'Português',
  italian: 'Italiano',
  polish: 'Polski',
  turkish: 'Türkçe',
  arabic: 'العربية',
  standard_arabic: 'العربية',
  egyptian_arabic: 'العربية',
  levantine_arabic: 'العربية',
  gulf_arabic: 'العربية',
  maghrebi_arabic: 'العربية',
  hebrew: 'עברית',
  indonesian: 'Bahasa Indonesia',
  thai: 'ไทย',
};

// Backward-compatible config object
export const LANGUAGE_CONFIG = {
  languages: SUPPORTED_LANGUAGES.map(lang => ({
    ...lang,
    countries: [{ name: lang.englishName, code: lang.code, langId: lang.id }]
  }))
};
