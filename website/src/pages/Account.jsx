import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext';
import apiClient from '../api/client';
import { Page, useLocalePath } from '../components/Showcase';

export default function Account() {
  const { t } = useTranslation();
  const { user, setUser } = useAuth();
  const base = useLocalePath();
  const [form, setForm] = useState(() => Object.fromEntries(['first_name','last_name','email','phone_number','country'].map(key => [key, user?.[key] || ''])));
  const [countries, setCountries] = useState([]);
  const [countryError, setCountryError] = useState(false);
  const [busy, setBusy] = useState(false);
  const [awaiting, setAwaiting] = useState(false);
  const [code, setCode] = useState('');
  const [message, setMessage] = useState(null);
  const loadCountries = () => apiClient.get('/auth/countries/').then(({data}) => { setCountries(data.sort((a,b) => a.name.common.localeCompare(b.name.common))); setCountryError(false); }).catch(() => setCountryError(true));
  useEffect(() => { loadCountries(); }, []);
  async function requestCode() {
    setBusy(true); setMessage(null);
    try { await apiClient.post('/auth/profile/email-code/'); setAwaiting(true); setCode(''); setMessage({ text: t('showcase.codeHelp') }); }
    catch { setMessage({ error: true, text: t('showcase.unavailable') }); }
    finally { setBusy(false); }
  }
  async function save(event) {
    event.preventDefault();
    if (form.email.trim() !== user.email && !awaiting) { await requestCode(); return; }
    setBusy(true); setMessage(null);
    try {
      const { data } = await apiClient.patch('/auth/profile/', { ...form, ...(form.email.trim() !== user.email ? {email_code: code.trim()} : {}) });
      const changedEmail = form.email.trim() !== user.email;
      setUser(data); setAwaiting(false); setCode(''); setMessage({text: t('showcase.saved') + (changedEmail ? ' ' + t('showcase.sessionRenewal') : '')});
    } catch { setMessage({error: true, text: t('showcase.saveFailed')}); }
    finally { setBusy(false); }
  }
  return <Page title={t('showcase.account')}><div className="account-page"><h2>{t('showcase.profile')}</h2>
    <form className="panel" onSubmit={save}><div className="form-grid">{['first_name','last_name','email','phone_number'].map(key => <label className="field" key={key} htmlFor={`profile-${key}`}>{t(key === 'phone_number' ? 'account.phone_number' : `auth.${key}`)}<input id={`profile-${key}`} name={key} type={key === 'email' ? 'email' : key === 'phone_number' ? 'tel' : 'text'} autoComplete={({first_name:'given-name',last_name:'family-name',email:'email',phone_number:'tel'})[key]} value={form[key]} required={key === 'email'} onChange={e => { setForm({...form,[key]:e.target.value}); if (key === 'email') { setAwaiting(false); setCode(''); } }} /></label>)}</div>
      <label className="field" htmlFor="profile-country">{t('account.country')}<select id="profile-country" name="country" value={form.country} onChange={e => setForm({...form,country:e.target.value})}><option value="">{t('auth.select_country')}</option>{form.country && !countries.some(c=>c.name.common === form.country) && <option>{form.country}</option>}{countries.map(c=><option key={c.cca3} value={c.name.common}>{c.name.common}</option>)}</select></label>
      {countryError && <div role="alert">{t('showcase.unavailable')} <button type="button" onClick={loadCountries}>{t('showcase.requestAgain')}</button></div>}
      {awaiting && <div className="notice"><p id="email-code-help">{t('showcase.codeHelp')}</p><label className="field" htmlFor="email-code">{t('auth.verification_code')}<input id="email-code" name="email_code" inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength={6} aria-describedby="email-code-help" value={code} onChange={e=>setCode(e.target.value)} required /></label><div className="actions"><button type="button" className="button secondary" disabled={busy} onClick={requestCode}>{t('showcase.retry')}</button><button type="button" className="button secondary" disabled={busy} onClick={()=>{setAwaiting(false);setCode('');setForm({...form,email:user.email});setMessage(null);}}>{t('showcase.cancel')}</button></div></div>}
      {message && <p role={message.error ? 'alert' : 'status'} className="notice">{message.text}</p>}
      <button className="button" disabled={busy} type="submit">{busy ? t('account.saving') : t('account.save_changes')}</button>
    </form><section className="world-section"><h2>{t('showcase.security')}</h2><Link className="button secondary" to={`${base}/account/2fa`}>{t('auth.two_factor_title')}</Link></section>
    <section className="world-section"><h2>{t('account.delete_title')}</h2><p>{t('account.delete_summary')}</p><Link className="button danger" to={`${base}/account/delete`}>{t('account.delete_title')}</Link></section></div></Page>;
}
