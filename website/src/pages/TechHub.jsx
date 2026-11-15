import { useTranslation } from 'react-i18next';
import { Page } from '../components/Showcase';
export default function TechHub() {
  const { t } = useTranslation();
  return <Page title={t('showcase.development')}><p className="lead">{t('showcase.inDevelopment')}</p><dl className="tech-list"><div><dt>Godot 4.7.2 .NET</dt><dd>C# · 2D</dd></div><div><dt>React + Vite</dt><dd>Express.js 5 · Node 26</dd></div><div><dt>Clerk</dt><dd>PostgreSQL</dd></div></dl><section className="panel"><h2>{t('showcase.campaign')}</h2><p>{t('showcase.world')}</p><p>{t('showcase.platformsA')}</p><p>{t('showcase.requirementsA')}</p></section></Page>;
}
