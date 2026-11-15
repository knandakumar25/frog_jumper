import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useLocalePath } from './Showcase';
export default function Footer() {
  const { t } = useTranslation();
  const base = useLocalePath();
  return <footer className="site-footer"><div><strong>AXIOM FORCE</strong><p>{t('showcase.inDevelopment')} · {new Date().getFullYear()}</p></div><div><Link to={`${base}/faq`}>{t('showcase.faq')}</Link><Link to={`${base}/tech-hub`}>{t('showcase.development')}</Link></div></footer>;
}
