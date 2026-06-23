import { createContext, useContext, useState, useEffect, useRef } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { LANGUAGE_CONFIG } from './config';
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
  armenian: 'hy',
  azerbaijani: 'az',
  bengali: 'bn',
  czech: 'cs',
  danish: 'da',
  dutch: 'nl',
  estonian: 'et',
  filipino: 'fil',
  finnish: 'fi',
  georgian: 'ka',
  german: 'de',
  greek: 'el',
  gujarati: 'gu',
  hebrew: 'he',
  hindi: 'hi',
  hungarian: 'hu',
  icelandic: 'is',
  indonesian: 'id',
  italian: 'it',
  japanese: 'ja',
  kannada: 'kn',
  korean: 'ko',
  latvian: 'lv',
  lithuanian: 'lt',
  malay: 'ms',
  malayalam: 'ml',
  mandarin: 'zh',
  simplified_chinese: 'zh',
  traditional_chinese: 'zh',
  cantonese: 'yue',
  marathi: 'mr',
  norwegian: 'no',
  polish: 'pl',
  punjabi: 'pa',
  romanian: 'ro',
  sinhala: 'si',
  slovak: 'sk',
  slovenian: 'sl',
  swedish: 'sv',
  tamil: 'ta',
  telugu: 'te',
  thai: 'th',
  turkish: 'tr',
  ukrainian: 'uk',
  vietnamese: 'vi',
  albanian: 'sq',
  bulgarian: 'bg',
  croatian: 'hr',
  dzongkha: 'dz',
  kazakh: 'kk',
  khmer: 'km',
  lao: 'lo',
};

export function getLocaleCode(langId, countryName, languageConfig) {
  const langCode = LANGUAGE_ID_TO_CODE[langId] || 'en';
  let countryCode = 'us';
  if (languageConfig && languageConfig.languages) {
    for (const lang of languageConfig.languages) {
      const country = lang.countries.find(c => c.name === countryName);
      if (country) {
        countryCode = country.code;
        break;
      }
    }
  }
  return `${langCode}_${countryCode.toLowerCase()}`;
}

export function parseLocaleCode(locale, languageConfig) {
  if (!locale) return null;
  const parts = locale.split('_');
  if (parts.length !== 2) return null;
  const langCode = parts[0].toLowerCase();
  const countryCode = parts[1].toLowerCase();

  for (const lang of languageConfig.languages) {
    const isLangMatch = LANGUAGE_ID_TO_CODE[lang.id] === langCode;
    if (isLangMatch) {
      const country = lang.countries.find(c => c.code.toLowerCase() === countryCode);
      if (country) {
        return {
          langId: country.langId || lang.id,
          countryName: country.name,
        };
      }
    }
  }

  // Fallback
  let matchedLang = languageConfig.languages.find(l => LANGUAGE_ID_TO_CODE[l.id] === langCode);
  let matchedCountry = null;
  for (const l of languageConfig.languages) {
    const c = l.countries.find(c => c.code.toLowerCase() === countryCode);
    if (c) {
      matchedCountry = c;
      break;
    }
  }
  if (matchedLang && matchedCountry) {
    return {
      langId: matchedCountry.langId || matchedLang.id,
      countryName: matchedCountry.name,
    };
  }

  return null;
}



function detectBrowserDefault() {
  const navLang = (navigator.languages && navigator.languages[0]) || navigator.language;
  if (!navLang) return null;

  const [langCode, countryCode] = navLang.toLowerCase().split('-');

  for (const lang of LANGUAGE_CONFIG.languages) {
    if (LANGUAGE_ID_TO_CODE[lang.id] === langCode) {
      if (countryCode) {
        const country = lang.countries.find(c => c.code.toLowerCase() === countryCode);
        if (country) {
          return { langId: country.langId || lang.id, countryName: country.name };
        }
      }
      // Fallback to first country of this language
      return { langId: lang.countries[0]?.langId || lang.id, countryName: lang.countries[0]?.name };
    }
  }
  return null;
}

export function LanguageProvider({ children }) {
  const [selectedCountry, setSelectedCountry] = useState(() => {
    const stored = localStorage.getItem('selectedCountry');
    if (stored) return stored;

    const browser = detectBrowserDefault();
    if (browser) {
      return browser.countryName;
    }
    return 'United States';
  });
  const [selectedLanguage, setSelectedLanguage] = useState(() => {
    const stored = localStorage.getItem('selectedLanguage');
    if (stored) return stored;

    const browser = detectBrowserDefault();
    if (browser) {
      return browser.langId;
    }
    return 'english';
  });
  const [isModalOpen, setIsModalOpen] = useState(false);

  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    const userConfirmed = localStorage.getItem('userSelectedLocale') === '1';
    if (!userConfirmed) {
      setIsModalOpen(true);
    }
  }, []);

  useEffect(() => {
    const userConfirmed = localStorage.getItem('userSelectedLocale') === '1';
    if (!isModalOpen && !userConfirmed) {
      const finalMatch = detectBrowserDefault() || { langId: 'english', countryName: 'United States' };
      
      const lang = LANGUAGE_CONFIG.languages.find(l => l.id === finalMatch.langId);
      const country =
        lang?.countries.find(c => c.name === finalMatch.countryName) ||
        lang?.countries[0];

      if (lang && country) {
        localStorage.setItem('userSelectedLocale', '1');
        setSelectedLanguage(finalMatch.langId);
        setSelectedCountry(country.name);
        const newLocale = getLocaleCode(finalMatch.langId, country.name, LANGUAGE_CONFIG);
        const pathParts = location.pathname.split('/');
        const restOfPath = pathParts.slice(2).join('/');
        const targetPath = restOfPath ? `/${newLocale}/${restOfPath}` : `/${newLocale}`;
        navigate(targetPath);
      }
    }
  }, [isModalOpen]);

  useEffect(() => {
    localStorage.setItem('selectedCountry', selectedCountry || '');
    localStorage.setItem('selectedLanguage', selectedLanguage);
  }, [selectedCountry, selectedLanguage]);

  useEffect(() => {
    const langCode = LANGUAGE_ID_TO_CODE[selectedLanguage] || 'en';
    i18n.changeLanguage(langCode);

    // Set text direction and language dynamically
    const isRtl = langCode === 'ar' || langCode === 'he';
    document.documentElement.dir = isRtl ? 'rtl' : 'ltr';
    document.documentElement.lang = langCode;

    // Dynamically set document title based on localized game name
    const titleObj = GAME_NAME_TRANSLATIONS[langCode] || GAME_NAME_TRANSLATIONS['en'];
    document.title = `${titleObj.brand}: ${titleObj.prefix}${titleObj.highlight}`;
  }, [selectedLanguage]);


  const chooseLanguageAndCountry = (langId, country) => {
    const actualLangId = country.langId || langId;
    localStorage.setItem('userSelectedLocale', '1');
    setSelectedLanguage(actualLangId);
    setSelectedCountry(country.name);
    setIsModalOpen(false);

    const newLocale = getLocaleCode(actualLangId, country.name, LANGUAGE_CONFIG);
    const pathParts = location.pathname.split('/');
    const restOfPath = pathParts.slice(2).join('/');
    navigate(`/${newLocale}/${restOfPath}`);
  };

  return (
    <LanguageContext.Provider value={{
      selectedLanguage,
      setSelectedLanguage,
      selectedCountry,
      setSelectedCountry,
      isModalOpen,
      setIsModalOpen,
      chooseLanguageAndCountry,
      LANGUAGE_CONFIG,
      getLocaleCode,
    }}>
      {children}
    </LanguageContext.Provider>
  );
}

export function useLanguage() {
  return useContext(LanguageContext);
}
