import { useState, useRef, useEffect } from 'react';
import { useLanguage } from './LanguageContext';
import { SUPPORTED_LANGUAGES, NATIVE_LANGUAGE_NAMES } from './config';
import './LanguageSelector.css';

export default function LanguageSelector() {
  const { selectedLanguage, chooseLanguage } = useLanguage();
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef(null);

  // Close dropdown when clicking outside
  useEffect(() => {
    function handleClickOutside(event) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    }

    function handleKeyDown(event) {
      if (event.key === 'Escape') {
        setIsOpen(false);
      }
    }

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
      document.addEventListener('keydown', handleKeyDown);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [isOpen]);

  // Find active language display name
  const currentLang = SUPPORTED_LANGUAGES.find(
    l => l.id === selectedLanguage || l.code === selectedLanguage
  );

  const displayName = currentLang 
    ? currentLang.name 
    : (NATIVE_LANGUAGE_NAMES[selectedLanguage] || 'English');

  const handleSelectLanguage = (langId) => {
    chooseLanguage(langId);
    setIsOpen(false);
  };

  return (
    <div className="lang-dropdown-container" ref={dropdownRef}>
      {/* Dropdown Toggle Button */}
      <button 
        className={`lang-dropdown-btn ${isOpen ? 'active' : ''}`}
        onClick={() => setIsOpen(!isOpen)}
        aria-haspopup="listbox"
        aria-expanded={isOpen}
        type="button"
      >
        <span className="material-symbols-outlined lang-globe-icon">language</span>
        <span className="lang-current-label">{displayName}</span>
        <span className={`material-symbols-outlined lang-chevron ${isOpen ? 'open' : ''}`}>
          expand_more
        </span>
      </button>

      {/* Dropdown Menu */}
      {isOpen && (
        <ul className="lang-dropdown-menu" role="listbox">
          {SUPPORTED_LANGUAGES.map(lang => {
            const isSelected = selectedLanguage === lang.id || selectedLanguage === lang.code;
            return (
              <li
                key={lang.id}
                role="option"
                aria-selected={isSelected}
                className={`lang-dropdown-item ${isSelected ? 'selected' : ''}`}
                onClick={() => handleSelectLanguage(lang.id)}
              >
                <span className="lang-item-name">{lang.name}</span>
                {isSelected && (
                  <span className="material-symbols-outlined lang-item-check">check</span>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
