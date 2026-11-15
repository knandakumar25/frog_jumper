import { useTranslation } from 'react-i18next';
import { Page, Questions } from '../components/Showcase';
export default function FAQ() {
  const { t } = useTranslation();
  return <Page title={t('showcase.faq')}><Questions /></Page>;
}
