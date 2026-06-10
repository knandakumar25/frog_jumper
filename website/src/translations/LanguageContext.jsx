import { createContext, useContext, useState, useEffect, useRef } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { LANGUAGE_CONFIG } from './config';
import i18n from './i18n';
import { GAME_NAME_TRANSLATIONS } from './gameNames';


const LanguageContext = createContext();

// Map language ID to 2-letter ISO language code
export const LANGUAGE_ID_TO_CODE = {
  arabic: 'ar',
  english: 'en',
  spanish: 'es',
  french: 'fr',
  eurofrench: 'fr',
  canadianfrench: 'fr',
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

// Find the best language+country match for a given 2-letter country code
function findGeoMatch(countryCode) {
  const code = countryCode.toLowerCase();
  for (const lang of LANGUAGE_CONFIG.languages) {
    const country = lang.countries.find(c => c.code.toLowerCase() === code);
    if (country) {
      return { langId: country.langId || lang.id, countryName: country.name };
    }
  }
  return null; // country not in our config
}

async function detectGeoDefault() {
  try {
    const res = await fetch('https://ip-api.com/json/?fields=countryCode', {
      signal: AbortSignal.timeout(4000),
    });
    if (!res.ok) return null;
    const data = await res.json();
    if (!data.countryCode) return null;
    return findGeoMatch(data.countryCode);
  } catch {
    return null;
  }
}

export function LanguageProvider({ children }) {
  const [selectedCountry, setSelectedCountry] = useState(() => {
    return localStorage.getItem('selectedCountry') || null;
  });
  const [selectedLanguage, setSelectedLanguage] = useState(() => {
    return localStorage.getItem('selectedLanguage') || 'english';
  });
  const [isModalOpen, setIsModalOpen] = useState(false);

  const navigate = useNavigate();
  const location = useLocation();

  // Track whether the modal was opened for a first-time visitor (no stored country)
  const needsGeoFallback = useRef(false);
  // Skip the geo-detection effect on the very first render (isModalOpen starts false)
  const hasMounted = useRef(false);

  useEffect(() => {
    const storedCountry = localStorage.getItem('selectedCountry');
    if (!storedCountry) {
      needsGeoFallback.current = true;
      setIsModalOpen(true);
    }
  }, []);

  // When modal closes without the user picking a country, geo-detect and apply a default
  useEffect(() => {
    if (!hasMounted.current) {
      hasMounted.current = true;
      return;
    }
    if (!isModalOpen && !selectedCountry && needsGeoFallback.current) {
      needsGeoFallback.current = false;
      detectGeoDefault().then(match => {
        const fallback = match || { langId: 'english', countryName: 'United States' };
        const lang = LANGUAGE_CONFIG.languages.find(l => l.id === fallback.langId);
        const country =
          lang?.countries.find(c => c.name === fallback.countryName) ||
          lang?.countries[0];
        if (lang && country) {
          setSelectedLanguage(fallback.langId);
          setSelectedCountry(country.name);
          const newLocale = getLocaleCode(fallback.langId, country.name, LANGUAGE_CONFIG);
          const pathParts = location.pathname.split('/');
          const restOfPath = pathParts.slice(2).join('/');
          navigate(`/${newLocale}/${restOfPath}`);
        }
      });
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
    needsGeoFallback.current = false; // user made an explicit choice
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
