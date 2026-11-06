import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import { GAME_NAME_TRANSLATIONS } from './gameNames.js';
import uiTranslations from './ui_translations.json';
import browserAuth from './browser_auth.json';

const resources = {};
Object.keys(uiTranslations).forEach(lang => {
  resources[lang] = { translation: uiTranslations[lang] };
});
// Keep the few handoff-specific strings beside the existing localized auth copy.
Object.entries(browserAuth).forEach(([lang, [switchAccount, missing, confirm, expired]]) => {
  resources[lang] ||= { translation: {} };
  resources[lang].translation.browserAuth = { switchAccount, missing, confirm, expired };
});

// Register game title translations for all languages dynamically
Object.keys(GAME_NAME_TRANSLATIONS).forEach(lang => {
  if (!resources[lang]) {
    resources[lang] = { translation: {} };
  }
  if (!resources[lang].translation) {
    resources[lang].translation = {};
  }
  if (!resources[lang].translation.hero) {
    resources[lang].translation.hero = {};
  }
  resources[lang].translation.hero.title_line1 = GAME_NAME_TRANSLATIONS[lang].brand;
  resources[lang].translation.hero.title_line2_prefix = GAME_NAME_TRANSLATIONS[lang].prefix;
  resources[lang].translation.hero.title_line2_highlight = GAME_NAME_TRANSLATIONS[lang].highlight;
});

i18n
  .use(initReactI18next)
  .init({
    resources,
    lng: 'en',
    fallbackLng: 'en',
    interpolation: {
      escapeValue: false // React already escapes values
    }
  });

export default i18n;
