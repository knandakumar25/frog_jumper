import { useLanguage } from './LanguageContext';
import { NATIVE_LANGUAGE_NAMES } from './config';
import './LanguageSelector.css';

export default function LanguageSelector() {
  const { selectedCountry, selectedLanguage, setIsModalOpen, LANGUAGE_CONFIG } = useLanguage();

  if (!selectedCountry) return null;

  // Find the flag code for the selected country
  let flagCode = '';
  LANGUAGE_CONFIG.languages.forEach(lang => {
    const country = lang.countries.find(c => c.name === selectedCountry);
    if (country) flagCode = country.code;
  });

  const displayName = NATIVE_LANGUAGE_NAMES[selectedLanguage] || (selectedLanguage.charAt(0).toUpperCase() + selectedLanguage.slice(1));

  return (
    <div className="lang-selector" onClick={() => setIsModalOpen(true)}>
      <img
        src={`https://flagcdn.com/w40/${flagCode}.png`}
        alt={selectedCountry}
        className="lang-selector-flag-img"
      />
      <span className="lang-selector-text">{displayName}</span>
    </div>
  );
}

