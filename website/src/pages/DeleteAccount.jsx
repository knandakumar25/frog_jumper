import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext';
import { useClerkClient } from '../auth/clerkGate';
import apiClient from '../api/client';
import { Page, useLocalePath } from '../components/Showcase';

/**
 * Permanent account deletion (2026-11-18; Apple and Google require apps with accounts to offer it).
 *
 * Shown at `/:locale/account/delete` behind `AccountAccess`, so a signed-out visitor signs in first
 * and comes back. The game's "Delete account" button opens this page, and it is also the web
 * deletion address for the Google Play listing.
 *
 * Steps: read what is deleted and what is kept, send an emailed code
 * (`POST /auth/account/delete-request/`), enter the code, type DELETE, confirm
 * (`POST /auth/account/delete/`). On success the page clears this browser's API tokens and the
 * Clerk session, then shows a confirmation. The backend also deletes the Clerk identity.
 * Game saves are stored on the account and are deleted with it (2026-11-18).
 */
export default function DeleteAccount() {
  const { t } = useTranslation();
  const { user, logout } = useAuth();
  const clerk = useClerkClient();
  const base = useLocalePath();
  const [sent, setSent] = useState(false);
  const [code, setCode] = useState('');
  const [word, setWord] = useState('');
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState(null);
  const [done, setDone] = useState(false);

  /** Ask the API to email a deletion code to the account's address. */
  async function sendCode() {
    setBusy(true); setMessage(null);
    try { await apiClient.post('/auth/account/delete-request/'); setSent(true); setCode(''); setMessage({ text: t('showcase.codeHelp') }); }
    catch (error) { setMessage({ error: true, text: error?.response?.status === 429 ? t('account.delete_wait') : t('showcase.unavailable') }); }
    finally { setBusy(false); }
  }

  /** Delete the account with the code and the typed word, then sign this browser out. */
  async function confirmDelete(event) {
    event.preventDefault();
    setBusy(true); setMessage(null);
    try {
      await apiClient.post('/auth/account/delete/', { code: code.trim(), confirm: word.trim() });
      // The account no longer exists: drop this page's credentials and the Clerk session.
      logout();
      await clerk.signOut().catch(() => {});
      setDone(true);
    } catch { setMessage({ error: true, text: t('account.delete_failed') }); }
    finally { setBusy(false); }
  }

  if (done) {
    return <Page title={t('account.delete_title')}><div className="account-page"><p role="status" className="notice">{t('account.delete_done')}</p>
      <Link className="button" to={base || '/'}>{t('account.delete_home')}</Link></div></Page>;
  }

  return <Page title={t('account.delete_title')}><div className="account-page">
    <section className="panel">
      <p>{t('account.delete_intro', { email: user?.email || '' })}</p>
      <h2>{t('account.delete_removed_title')}</h2>
      <p>{t('account.delete_removed')}</p>
      <h2>{t('account.delete_kept_title')}</h2>
      <p>{t('account.delete_kept')}</p>
    </section>
    {!sent && <button type="button" className="button" disabled={busy} onClick={sendCode}>{t('account.delete_send_code')}</button>}
    {sent && <form className="panel" onSubmit={confirmDelete}>
      <label className="field" htmlFor="delete-code">{t('auth.verification_code')}
        <input id="delete-code" name="code" inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength={6} value={code} onChange={e => setCode(e.target.value)} required /></label>
      <label className="field" htmlFor="delete-word">{t('account.delete_type_word')}
        <input id="delete-word" name="confirm" autoComplete="off" spellCheck="false" value={word} onChange={e => setWord(e.target.value)} required /></label>
      <div className="actions">
        <button type="submit" className="button danger" disabled={busy || word.trim() !== 'DELETE' || code.trim().length !== 6}>{t('account.delete_confirm')}</button>
        <button type="button" className="button secondary" disabled={busy} onClick={sendCode}>{t('showcase.retry')}</button>
        <Link className="button secondary" to={`${base}/account`}>{t('showcase.cancel')}</Link>
      </div>
    </form>}
    {message && <p role={message.error ? 'alert' : 'status'} className="notice">{message.text}</p>}
  </div></Page>;
}
