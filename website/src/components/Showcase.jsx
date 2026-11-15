import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useLanguage } from '../translations/LanguageContext';

export const operatives = [
  ['shogun', 'cyber_shogun', 'Kaito Kurogane', 1],
  ['oceanus', 'oceanus', 'Kiana Vaea', 2],
  ['apex', 'apex_titan', 'King Tariq Nzinga', 2],
  ['phoenicia', 'phoenicia', 'Maya Khoury', 3],
  ['mirage', 'mirage_blade', 'Zayd Al-Mansoor', 3],
  ['surya', 'surya_prime', 'Arjun Varma', 4],
];
export const missionTitles = ['Ghost in the Gridlock', 'The Concrete Wildfire', 'Neon Rust and Data Dust', 'Shattered Aegis', 'Names Without Numbers', 'Cyber-Grave Labs', 'The Foundry Forge', 'Stratos Docks', 'Spire Outer Hull', 'The Cyber-Nexus', 'The Apex Bastion', 'Judgment Day'];
export function useLocalePath() {
  const { selectedLanguage, getLocaleCode } = useLanguage();
  return `/${getLocaleCode(selectedLanguage)}`;
}
export function Page({ title, children }) {
  return <div className="showcase-page"><header className="page-heading"><h1>{title}</h1></header>{children}</div>;
}
export function SquadPreview() {
  const { t } = useTranslation();
  const base = useLocalePath();
  return <div className="squad-grid">{operatives.map(([id, image]) => <Link className="operative-card" key={id} to={`${base}/squad?id=${id}`}>
    <img src={`/images/showcase/${image}_portrait.png`} width="768" height="1024" loading="lazy" alt="" /><span>{t(`squad.${id}`)}</span>
  </Link>)}</div>;
}
export function Questions({ short = false }) {
  const { t } = useTranslation();
  return <div className="questions">{(short ? ['release', 'signin', 'requirements'] : ['release', 'platforms', 'account', 'signin', 'requirements']).map(key =>
    <details key={key}><summary>{t(`showcase.${key}Q`)}</summary><p>{t(`showcase.${key}A`)}</p></details>
  )}</div>;
}
