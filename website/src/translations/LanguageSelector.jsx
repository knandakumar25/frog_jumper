import { useId } from 'react';
import { useLanguage } from './LanguageContext';
import { SUPPORTED_LANGUAGES } from './config';
export default function LanguageSelector() {
  const { selectedLanguage, chooseLanguage } = useLanguage();
  const id = useId();
  return <label className="language-select" htmlFor={id}><span aria-hidden="true">◎</span><span className="sr-only">Language / اللغة / שפה</span><select id={id} value={selectedLanguage} onChange={event => chooseLanguage(event.target.value)}>{SUPPORTED_LANGUAGES.map(lang => <option key={lang.id} value={lang.id}>{lang.name}</option>)}</select></label>;
}
