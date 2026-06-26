import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useLanguage } from '../translations/LanguageContext';
import apiClient from '../api/client';

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

  const { register, login, setUser } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);

  // Extract redirect parameter
  const params = new URLSearchParams(location.search);
  const redirectTarget = params.get('redirect');

  const handleGoogleLoginCallback = async (response) => {
    setErrorMsg('');
    setIsSubmitting(true);
    try {
      const res = await apiClient.post('/auth/google/', { token: response.credential });
      const { access, refresh, user: userData } = res.data;
      
      localStorage.setItem('accessToken', access);
      localStorage.setItem('refreshToken', refresh);
      setUser(userData);

      if (redirectTarget) {
        navigate(redirectTarget);
      } else {
        navigate(`/${currentLocale}`);
      }
    } catch (err) {
      console.error(err);
      setErrorMsg('Google login failed. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleFacebookLoginCallback = async (authResponse) => {
    setErrorMsg('');
    setIsSubmitting(true);
    try {
      const res = await apiClient.post('/auth/facebook/', { token: authResponse.accessToken });
      const { access, refresh, user: userData } = res.data;
      
      localStorage.setItem('accessToken', access);
      localStorage.setItem('refreshToken', refresh);
      setUser(userData);

      if (redirectTarget) {
        navigate(redirectTarget);
      } else {
        navigate(`/${currentLocale}`);
      }
    } catch (err) {
      console.error(err);
      setErrorMsg('Facebook login failed. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleFacebookLogin = () => {
    if (!window.FB) {
      setErrorMsg('Facebook SDK is loading, please try again in a moment.');
      return;
    }
    window.FB.login((response) => {
      if (response.authResponse) {
        handleFacebookLoginCallback(response.authResponse);
      } else {
        setErrorMsg('Facebook login was cancelled.');
      }
    }, { scope: 'email' });
  };

  const handleEpicLogin = () => {
    setIsSubmitting(true);
    const epicLoginUrl = `http://localhost:8000/api/auth/epic/login/${redirectTarget ? `?redirect=${encodeURIComponent(redirectTarget)}` : ''}`;
    window.location.href = epicLoginUrl;
  };

  useEffect(() => {
    const scriptId = 'google-gsi-client-script';
    let script = document.getElementById(scriptId);
    
    const initializeGoogleSignIn = () => {
      if (window.google) {
        window.google.accounts.id.initialize({
          client_id: '108181092442-n5ncn13mt2c5dst38dk8hh1rceargkpi.apps.googleusercontent.com',
          callback: handleGoogleLoginCallback,
        });
        window.google.accounts.id.renderButton(
          document.getElementById('google-signin-btn'),
          { theme: 'dark', size: 'large', type: 'standard', text: 'signup_with', shape: 'rectangular', logo_alignment: 'left' }
        );
      }
    };

    if (!script) {
      script = document.createElement('script');
      script.id = scriptId;
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      script.onload = initializeGoogleSignIn;
      document.body.appendChild(script);
    } else {
      initializeGoogleSignIn();
    }
  }, []);

  useEffect(() => {
    const fbScriptId = 'facebook-jssdk';
    let fbScript = document.getElementById(fbScriptId);

    const initializeFacebookSignIn = () => {
      if (window.FB) {
        window.FB.init({
          appId: '1331327648423029',
          cookie: true,
          xfbml: true,
          version: 'v19.0'
        });
      }
    };

    if (!fbScript) {
      fbScript = document.createElement('script');
      fbScript.id = fbScriptId;
      fbScript.src = 'https://connect.facebook.net/en_US/sdk.js';
      fbScript.async = true;
      fbScript.defer = true;
      fbScript.onload = initializeFacebookSignIn;
      document.body.appendChild(fbScript);
    } else {
      initializeFacebookSignIn();
    }
  }, []);

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

        <div className="flex items-center gap-3 my-5">
          <div className="h-[1px] bg-outline-variant/30 flex-grow"></div>
          <span className="text-[10px] uppercase font-bold tracking-wider text-on-surface-variant/60">OR</span>
          <div className="h-[1px] bg-outline-variant/30 flex-grow"></div>
        </div>

        <div className="flex flex-col gap-3 w-full">
          <div id="google-signin-btn" className="w-full flex justify-center shadow-[0_0_15px_rgba(0,243,255,0.05)] border-2 border-primary-container/20 p-0.5 bg-surface-container-highest"></div>
          
          <button
            type="button"
            onClick={handleFacebookLogin}
            disabled={isSubmitting}
            className="flex items-center justify-center gap-3 w-full py-2.5 px-4 bg-[#1877F2]/10 hover:bg-[#1877F2]/20 border-2 border-[#1877F2]/30 hover:border-[#1877F2]/60 text-on-surface font-bold text-sm tracking-wider uppercase transition-all duration-150 hover:skew-x-[-4deg] active:scale-95 disabled:opacity-50"
          >
            <svg className="w-4 h-4 fill-[#1877F2]" viewBox="0 0 24 24">
              <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/>
            </svg>
            <span>Facebook Login</span>
          </button>

          <button
            type="button"
            onClick={handleEpicLogin}
            disabled={isSubmitting}
            className="flex items-center justify-center gap-3 w-full py-2.5 px-4 bg-[#313131]/20 hover:bg-[#313131]/45 border-2 border-[#313131]/40 hover:border-[#313131]/70 text-on-surface font-bold text-sm tracking-wider uppercase transition-all duration-150 hover:skew-x-[-4deg] active:scale-95 disabled:opacity-50"
          >
            <svg className="w-4 h-4 fill-on-surface" viewBox="0 0 24 24">
              <path d="M12 .002L1.832 3.18C1.458 3.296 1.2 3.64 1.2 4.032V12.18c0 5.485 3.929 10.395 10.4 11.82a.82.82 0 0 0 .4 0c6.471-1.425 10.4-6.335 10.4-11.82V4.032c0-.392-.258-.736-.632-.852L12 .002zm5.798 12.178c0 3.493-2.316 6.643-5.798 7.643c-3.482-1-5.798-4.15-5.798-7.643V5.986l5.798-1.812l5.798 1.812v6.194z" />
            </svg>
            <span>Epic Games Login</span>
          </button>
        </div>

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
