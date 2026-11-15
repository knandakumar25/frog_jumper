import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useLanguage } from '../translations/LanguageContext';
import TitleLogo from '../components/TitleLogo';
import { SquadPreview, Questions, useLocalePath } from '../components/Showcase';
export default function Home() {
  const { t } = useTranslation();
  const { selectedLanguage } = useLanguage();
  const base = useLocalePath();
  return <>
    <section className="showcase-hero">
      <figure className="hero-art"><img src="/images/showcase/city_base.png" width="1920" height="1080" alt="" fetchPriority="high" /><figcaption>{t('showcase.art')}</figcaption></figure>
      <div className="hero-copy"><p className="eyebrow">{t('showcase.inDevelopment')} · 2157</p><h1><TitleLogo lang={selectedLanguage} className="hero-logo" fetchPriority="high" /></h1><p className="hero-intro">{t('showcase.intro')}</p>
        <div className="actions"><Link className="button" to={`${base}/gameplay`}>{t('showcase.explore')} <span aria-hidden="true">↗</span></Link><Link className="button secondary" to={`${base}/squad`}>{t('showcase.meet')}</Link></div>
      </div>
    </section>
    <div className="showcase-page home-content">
      <section className="feature-split"><div><p className="eyebrow">01 / {t('showcase.gameplay')}</p><h2>{t('showcase.campaign')}</h2></div><div><p>{t('showcase.combat')}</p><p>{t('showcase.recruit')}</p><Link className="text-link" to={`${base}/gameplay`}>{t('showcase.explore')} →</Link></div></section>
      <section><div className="section-heading"><h2>{t('showcase.meet')}</h2><span className="eyebrow">02 / 06</span></div><SquadPreview /></section>
      <section className="feature-split world-section"><h2>{t('showcase.gameplay')} / Los Angeles</h2><p>{t('showcase.world')}</p></section>
      <section><h2>{t('showcase.faq')}</h2><Questions short /><Link className="text-link" to={`${base}/faq`}>{t('showcase.faq')} →</Link></section>
    </div>
  </>;
}
