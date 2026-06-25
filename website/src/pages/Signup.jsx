import React, { useState } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useLanguage } from '../translations/LanguageContext';

export default function Signup() {
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [profilePicture, setProfilePicture] = useState('');
  const [previewUrl, setPreviewUrl] = useState('');
  const [errorMsg, setErrorMsg] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleFileChange = (e) => {
    const file = e.target.files[0];
    if (!file) return;

    if (file.size > 2 * 1024 * 1024) {
      setErrorMsg('Image size must be less than 2MB');
      return;
    }

    const reader = new FileReader();
    reader.onloadend = () => {
      setProfilePicture(reader.result);
      setPreviewUrl(reader.result);
    };
    reader.readAsDataURL(file);
  };

  const handleRemovePhoto = () => {
    setProfilePicture('');
    setPreviewUrl('');
  };

  const { register, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);

  // Extract redirect parameter
  const params = new URLSearchParams(location.search);
  const redirectTarget = params.get('redirect');

  const handleSubmit = async (e) => {
    e.preventDefault();
    setErrorMsg('');
    setIsSubmitting(true);

    try {
      // 1. Create account
      await register(username, email, password, firstName, lastName, profilePicture);
      // 2. Perform auto-login for a seamless experience
      await login(username, password);
      
      if (redirectTarget) {
        navigate(redirectTarget);
      } else {
        navigate(`/${currentLocale}`);
      }
    } catch (err) {
      console.error(err);
      setErrorMsg('Registration failed. Username or email may already be registered.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="pt-[120px] md:pt-[150px] pb-32 px-4 flex justify-center items-center">
      <div className="w-full max-w-md bg-surface-container-lowest border-2 border-primary-container/30 p-8 shadow-[0_0_30px_rgba(0,243,255,0.15)] relative bracket-corners">
        
        {/* Accent corners */}
        <div className="absolute -top-1 -left-1 w-4 h-4 border-t-2 border-l-2 border-primary-container"></div>
        <div className="absolute -top-1 -right-1 w-4 h-4 border-t-2 border-r-2 border-primary-container"></div>
        <div className="absolute -bottom-1 -left-1 w-4 h-4 border-b-2 border-l-2 border-primary-container"></div>
        <div className="absolute -bottom-1 -right-1 w-4 h-4 border-b-2 border-r-2 border-primary-container"></div>

        <h2 className="text-headline-lg font-headline-lg text-primary-container uppercase mb-8 text-center font-extrabold tracking-widest drop-shadow-[0_0_10px_rgba(0,243,255,0.4)]">
          CREATE ACCOUNT
        </h2>

        {errorMsg && (
          <div className="bg-error/15 border border-error/50 p-4 mb-6 text-error text-sm font-semibold flex items-center gap-2">
            <span className="material-symbols-outlined text-[20px]">warning</span>
            <span>{errorMsg}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="flex flex-col gap-5">
          {/* Profile Picture Upload */}
          <div className="flex flex-col items-center gap-3 mb-2">
            <label className="text-xs uppercase font-bold tracking-wider text-on-surface-variant text-center w-full">
              Avatar Port / Profile Picture (Optional)
            </label>
            <div className="relative group">
              <div className="w-24 h-24 rounded-none border-2 border-dashed border-outline-variant/60 hover:border-primary-container/80 transition-all flex items-center justify-center bg-surface-container-highest cursor-pointer relative overflow-hidden group shadow-[0_0_15px_rgba(0,243,255,0.05)] hover:shadow-[0_0_20px_rgba(0,243,255,0.2)]">
                {previewUrl ? (
                  <img
                    src={previewUrl}
                    alt="Avatar Preview"
                    className="w-full h-full object-cover"
                  />
                ) : (
                  <div className="flex flex-col items-center justify-center text-on-surface-variant group-hover:text-primary-container transition-colors">
                    <span className="material-symbols-outlined text-[32px]">add_a_photo</span>
                    <span className="text-[9px] uppercase font-bold mt-1 tracking-tighter">Upload Portrait</span>
                  </div>
                )}
                <input
                  type="file"
                  accept="image/*"
                  onChange={handleFileChange}
                  className="absolute inset-0 opacity-0 cursor-pointer"
                  disabled={isSubmitting}
                />
              </div>
              {previewUrl && (
                <button
                  type="button"
                  onClick={handleRemovePhoto}
                  className="absolute -top-2 -right-2 bg-error text-void-black rounded-none p-1 flex items-center justify-center hover:bg-error-container hover:text-on-error-container transition-colors shadow-lg"
                  title="Remove Photo"
                  disabled={isSubmitting}
                >
                  <span className="material-symbols-outlined text-[16px]">close</span>
                </button>
              )}
            </div>
          </div>

          {/* First Name & Last Name (Optional) */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <label className="text-xs uppercase font-bold tracking-wider text-on-surface-variant flex justify-between">
                <span>First Name</span>
                <span className="text-[10px] lowercase italic opacity-60">optional</span>
              </label>
              <input
                type="text"
                placeholder="First name"
                className="bg-surface-container-highest border-2 border-outline-variant/40 p-3.5 text-on-surface outline-none focus:border-primary-container transition-all text-sm font-body-md"
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                disabled={isSubmitting}
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-xs uppercase font-bold tracking-wider text-on-surface-variant flex justify-between">
                <span>Last Name</span>
                <span className="text-[10px] lowercase italic opacity-60">optional</span>
              </label>
              <input
                type="text"
                placeholder="Last name"
                className="bg-surface-container-highest border-2 border-outline-variant/40 p-3.5 text-on-surface outline-none focus:border-primary-container transition-all text-sm font-body-md"
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                disabled={isSubmitting}
              />
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-xs uppercase font-bold tracking-wider text-on-surface-variant">
              Username
            </label>
            <input
              type="text"
              placeholder="Enter your username"
              className="bg-surface-container-highest border-2 border-outline-variant/40 p-3.5 text-on-surface outline-none focus:border-primary-container transition-all text-sm font-body-md"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
              disabled={isSubmitting}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-xs uppercase font-bold tracking-wider text-on-surface-variant">
              Email Address
            </label>
            <input
              type="email"
              placeholder="Enter your email"
              className="bg-surface-container-highest border-2 border-outline-variant/40 p-3.5 text-on-surface outline-none focus:border-primary-container transition-all text-sm font-body-md"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              disabled={isSubmitting}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-xs uppercase font-bold tracking-wider text-on-surface-variant">
              Password
            </label>
            <input
              type="password"
              placeholder="••••••••"
              className="bg-surface-container-highest border-2 border-outline-variant/40 p-3.5 text-on-surface outline-none focus:border-primary-container transition-all text-sm font-body-md"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              disabled={isSubmitting}
            />
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="brutal-btn py-4 font-headline-md text-headline-md uppercase font-bold text-void-black bg-primary-container hover:bg-secondary-container hover:text-on-secondary-container transition-all duration-150 hover:skew-x-[-8deg] active:scale-95 disabled:opacity-50 mt-3"
          >
            {isSubmitting ? 'Registering...' : 'Initialize Profile'}
          </button>
        </form>

        <div className="mt-8 text-center text-xs font-body-md border-t border-primary-container/10 pt-6">
          <span className="text-on-surface-variant">Already have an account? </span>
          <Link
            to={`/${currentLocale}/login${redirectTarget ? `?redirect=${encodeURIComponent(redirectTarget)}` : ''}`}
            className="text-primary-container hover:text-secondary-container font-bold underline transition-colors"
          >
            Login / Access Terminal
          </Link>
        </div>
      </div>
    </div>
  );
}
