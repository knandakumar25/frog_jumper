import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';
import { Page, operatives } from '../components/Showcase';
export default function Squad() {
  const { t } = useTranslation();
  const [params, setParams] = useSearchParams();
  const active = operatives.find(([id]) => id === params.get('id')) || operatives[0];
  const [id, image, civilian, mission] = active;
  return <Page title={t('showcase.squad')}><p className="lead">{t('showcase.recruit')}</p>
    <div className="operative-picker" aria-label={t('showcase.squad')}>{operatives.map(([key]) => <button className={id === key ? 'selected' : ''} key={key} aria-pressed={id === key} onClick={() => setParams(previous => { const next = new URLSearchParams(previous); next.set('id', key); return next; })}>{t(`squad.${key}`)}</button>)}</div>
    <article className="dossier" data-operative={id}><figure><img src={`/images/showcase/${image}_portrait.png`} width="768" height="1024" alt={t(`squad.${id}`)} /><figcaption>{t('showcase.art')}</figcaption></figure><div><p className="eyebrow">{civilian}</p><h2>{t(`squad.${id}`)}</h2><p>{t(`showcase.roles.${id}`)}</p><p>{t(`showcase.abilities.${id}`)}</p><h3>{t('showcase.recruitment')}</h3><p>{mission === 1 ? t('showcase.initial') : t('showcase.joins', { mission })}</p></div></article>
  </Page>;
}
