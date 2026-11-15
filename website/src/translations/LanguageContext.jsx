import { createContext, useContext, useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { SUPPORTED_LANGUAGES, LANGUAGE_CONFIG } from './config';
import i18n from './i18n';
import { GAME_NAME_TRANSLATIONS } from './gameNames';

const LanguageContext = createContext();

// Map language ID to 2-letter ISO language code
export const LANGUAGE_ID_TO_CODE = {
  arabic: 'ar',
  standard_arabic: 'ar',
  egyptian_arabic: 'ar',
  levantine_arabic: 'ar',
  gulf_arabic: 'ar',
  maghrebi_arabic: 'ar',
  english: 'en',
  american_english: 'en',
  british_english: 'en',
  canadian_english: 'en',
  australian_english: 'en',
  international_english: 'en',
  spanish: 'es',
  castilian_spanish: 'es',
  latin_american_spanish: 'es',
  french: 'fr',
  eurofrench: 'fr',
  canadianfrench: 'fr',
  african_french: 'fr',
  haitian_french: 'fr',
  portuguese: 'pt',
  europortuguese: 'pt',
  brazilianportuguese: 'pt',
  german: 'de',
  hebrew: 'he',
  italian: 'it',
  japanese: 'ja',
  korean: 'ko',
  mandarin: 'zh',
  simplified_chinese: 'zh',
  traditional_chinese: 'zh_tw',
  traditionalchinese: 'zh_tw',
  cantonese: 'zh_tw',
  polish: 'pl',
  turkish: 'tr',
  vietnamese: 'vi',
  indonesian: 'id',
  thai: 'th',
};

// Map code back to primary language ID
export const CODE_TO_LANGUAGE_ID = {
  en: 'english',
  es: 'spanish',
  vi: 'vietnamese',
  ja: 'japanese',
  ko: 'korean',
  zh: 'mandarin',
  zh_tw: 'traditional_chinese',
  fr: 'french',
  de: 'german',
  pt: 'portuguese',
  it: 'italian',
  pl: 'polish',
  tr: 'turkish',
  ar: 'arabic',
  he: 'hebrew',
  id: 'indonesian',
  th: 'thai',
};

/**
 * Returns the short locale code for a given language ID (e.g. 'en', 'vi', 'es')
 */
export function getLocaleCode(langId) {
  if (!langId) return 'en';
  return LANGUAGE_ID_TO_CODE[langId] || langId;
}

/**
 * Parses any incoming URL locale token (e.g. 'en', 'vi', 'en_us', 'vi_vn', 'es_es')
 * into the resolved language ID and code.
 */
export function parseLocaleCode(locale) {
  if (!locale) return null;
  const clean = locale.toLowerCase().trim();

  // 1. Direct code match (e.g. 'en', 'vi', 'es')
  if (CODE_TO_LANGUAGE_ID[clean]) {
    return { langId: CODE_TO_LANGUAGE_ID[clean], code: clean };
  }

  // 2. Direct language ID match (e.g. 'english', 'vietnamese')
  if (LANGUAGE_ID_TO_CODE[clean]) {
    return { langId: clean, code: LANGUAGE_ID_TO_CODE[clean] };
  }

  // 3. Handle traditional Chinese dialect code
  if (clean === 'zh_tw' || clean === 'zh-tw') {
    return { langId: 'traditional_chinese', code: 'zh_tw' };
  }

  // 4. Handle compound country codes (e.g. 'en_us', 'vi_vn', 'es_mx')
  const primary = clean.split(/[_-]/)[0];
  if (CODE_TO_LANGUAGE_ID[primary]) {
    return { langId: CODE_TO_LANGUAGE_ID[primary], code: primary };
  }

  return null;
}

function detectBrowserDefault() {
  const navLang = (navigator.languages && navigator.languages[0]) || navigator.language;
  if (!navLang) return 'english';
  const primary = navLang.toLowerCase().split('-')[0];
  return CODE_TO_LANGUAGE_ID[primary] || 'english';
}

/** Keeps locale in the current route and runtime state, with no browser persistence. */
export function LanguageProvider({ children }) {
  const [selectedLanguage, setSelectedLanguage] = useState(() => {
    // The locale is carried by the URL; no browser preference file is consulted.
    return detectBrowserDefault();
  });

  const [isModalOpen, setIsModalOpen] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();

  // Sync state with URL route changes
  useEffect(() => {
    const pathParts = location.pathname.split('/').filter(Boolean);
    const potentialLocale = ['auth', 'onboarding'].includes(pathParts[0])
      ? new URLSearchParams(location.search).get('locale') : pathParts[0];
    if (potentialLocale) {
      const match = parseLocaleCode(potentialLocale);
      if (match) {
        if (match.langId !== selectedLanguage) {
          setSelectedLanguage(match.langId);
        }

      }
    }
  }, [location.pathname, location.search, selectedLanguage]);

  // Update i18n, RTL direction, and HTML title dynamically
  useEffect(() => {
    const langCode = LANGUAGE_ID_TO_CODE[selectedLanguage] || 'en';
    i18n.changeLanguage(langCode);

    // Set text direction and language attribute
    const isRtl = langCode === 'ar' || langCode === 'he';
    document.documentElement.dir = isRtl ? 'rtl' : 'ltr';
    document.documentElement.lang = langCode;

  }, [selectedLanguage]);

  // Direct 1-click language switcher (Fortnite-style)
  const chooseLanguage = (langId) => {
    const actualLangId = CODE_TO_LANGUAGE_ID[langId] || langId;

    setSelectedLanguage(actualLangId);
    setIsModalOpen(false);

    const newLocale = getLocaleCode(actualLangId);
    const pathParts = location.pathname.split('/').filter(Boolean);
    const firstPart = pathParts[0];

    let restOfPath = '';
    if (firstPart && parseLocaleCode(firstPart)) {
      restOfPath = pathParts.slice(1).join('/');
    } else {
      restOfPath = pathParts.join('/');
    }

    const query = new URLSearchParams(location.search);
    const bare = firstPart === 'auth' || firstPart === 'onboarding';
    if (bare) query.set('locale', newLocale);
    const targetUrl = bare ? location.pathname : restOfPath ? `/${newLocale}/${restOfPath}` : `/${newLocale}`;
    const search = query.toString();
    navigate(`${targetUrl}${search ? `?${search}` : ''}${location.hash}`);
  };

  // Backward compatibility alias for any component still calling chooseLanguageAndCountry
  const chooseLanguageAndCountry = (langId, country) => {
    const targetLang = country?.langId || langId;
    chooseLanguage(targetLang);
  };

  return (
    <LanguageContext.Provider value={{
      selectedLanguage,
      setSelectedLanguage,
      selectedCountry: '', // Deprecated, provided for backward compatibility
      setSelectedCountry: () => {},
      isModalOpen,
      setIsModalOpen,
      chooseLanguage,
      chooseLanguageAndCountry,
      SUPPORTED_LANGUAGES,
      LANGUAGE_CONFIG,
      getLocaleCode,
      parseLocaleCode,
    }}>
      {children}
    </LanguageContext.Provider>
  );
}

export function useLanguage() {
  return useContext(LanguageContext);
}
