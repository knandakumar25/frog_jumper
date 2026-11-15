import { useTranslation } from 'react-i18next';
import { Page, missionTitles } from '../components/Showcase';
export default function Gameplay() {
  const { t } = useTranslation();
  return <Page title={t('showcase.gameplay')}><p className="lead">{t('showcase.campaign')}</p><div className="feature-split"><p>{t('showcase.combat')}</p><p>{t('showcase.recruit')}</p></div>
    <div className="act-grid">{[[1, 0, 4], [2, 4, 7], [3, 7, 12]].map(([act, from, to]) => <section className="panel" key={act}><h2>{t('showcase.act')} {act}</h2><ol start={from + 1}>{missionTitles.slice(from, to).map((title, index) => <li key={title} data-mission={from + index + 1}><span className="eyebrow">{t('showcase.mission')} {from + index + 1}</span><h3>{t(`showcase.missions.${from + index + 1}`, title)}</h3></li>)}</ol></section>)}</div>
    <section className="world-section"><h2>Los Angeles / 2157</h2><p>{t('showcase.world')}</p></section>
  </Page>;
}
