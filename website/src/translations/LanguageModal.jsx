import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useLanguage } from './LanguageContext';
import './LanguageModal.css';

export default function LanguageModal() {
  const { isModalOpen, setIsModalOpen, chooseLanguageAndCountry, LANGUAGE_CONFIG } = useLanguage();
  const [selectedLangId, setSelectedLangId] = useState('');
  const [selectedCountryName, setSelectedCountryName] = useState('');
  const { t } = useTranslation();

  if (!isModalOpen) return null;

  const getAvailableCountries = () => {
    const countriesMap = new Map();
    LANGUAGE_CONFIG.languages.forEach(lang => {
      lang.countries.forEach(c => {
        if (!countriesMap.has(c.code)) {
          countriesMap.set(c.code, { name: c.name, flag: c.flag, code: c.code });
        }
      });
    });
    return Array.from(countriesMap.values())
      .sort((a, b) => {
        if (a.name === 'United States') return -1;
        if (b.name === 'United States') return 1;
        return a.name.localeCompare(b.name);
      });
  };

  const getAvailableLanguages = () => {
    if (!selectedCountryName) return [];
    return LANGUAGE_CONFIG.languages.filter(lang =>
      lang.countries.some(c => c.name === selectedCountryName)
    );
  };

  const handleLanguageClick = (langId) => {
    setSelectedLangId(langId);
    if (selectedCountryName) {
      const supports = LANGUAGE_CONFIG.languages
        .find(l => l.id === langId)
        ?.countries.some(c => c.name === selectedCountryName);
      if (!supports) setSelectedCountryName('');
    }
  };

  const handleCountryClick = (country) => {
    setSelectedCountryName(country.name);
    setSelectedLangId('');
  };

  const handleChoose = () => {
    if (selectedLangId && selectedCountryName) {
      const lang = LANGUAGE_CONFIG.languages.find(l => l.id === selectedLangId);
      const country = lang.countries.find(c => c.name === selectedCountryName);
      chooseLanguageAndCountry(selectedLangId, country);
    }
  };

  const availableLanguages = getAvailableLanguages();
  const availableCountries = getAvailableCountries();

  return (
    <div className="lang-modal-overlay" onClick={(e) => { if (e.target === e.currentTarget) setIsModalOpen(false); }}>
      <div className="lang-modal">
        <div className="lang-modal-header">
          <button
            className="lang-modal-close"
            onClick={() => setIsModalOpen(false)}
            aria-label="Close"
            id="lang-modal-close-btn"
          >
            ✕
          </button>
        </div>
        <div className="lang-modal-content">
          <div className="country-selection-column">
            <h3 className="column-header">{t('modal.country')}</h3>
            <div className="country-list">
              {availableCountries.length > 0 ? (
                availableCountries.map((country, index) => (
                  <div
                    key={`${country.name}-${index}`}
                    className={`country-item ${selectedCountryName === country.name ? 'active' : ''}`}
                    onClick={() => handleCountryClick(country)}
                  >
                    <img
                      src={`https://flagcdn.com/w40/${country.code}.png`}
                      alt={country.name}
                      className="country-flag-img"
                    />
                    <span className="country-name">{country.name}</span>
                  </div>
                ))
              ) : (
                <div className="country-placeholder">{t('modal.no_countries')}</div>
              )}
            </div>
          </div>
          <div className="lang-selection-column">
            <h3 className="column-header">{t('modal.language')}</h3>
            <div className="lang-list">
              {availableLanguages.length > 0 ? (
                availableLanguages.map(lang => (
                  <div
                    key={lang.id}
                    className={`lang-item ${selectedLangId === lang.id ? 'active' : ''}`}
                    onClick={() => handleLanguageClick(lang.id)}
                  >
                    {lang.name}
                  </div>
                ))
              ) : (
                <div className="country-placeholder">{t('modal.select_country_first')}</div>
              )}
            </div>
          </div>
        </div>
        <div className="lang-modal-footer">
          <button
            className="btn btn-primary"
            disabled={!selectedLangId || !selectedCountryName}
            onClick={handleChoose}
          >
            {t('modal.choose')}
          </button>
        </div>
      </div>
    </div>
  );
}

