import React, { useState, useEffect } from 'react';
import { useParams, Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import apiClient from '../api/client';

const getCallingCode = (c) => {
  if (!c.idd || !c.idd.root) return '';
  const suffixes = c.idd.suffixes || [];
  if (suffixes.length === 1) {
    return `${c.idd.root}${suffixes[0]}`;
  }
  return c.idd.root;
};

const Account = () => {
  const { locale } = useParams();
  const { user, setUser } = useAuth();
  const [formData, setFormData] = useState({
    first_name: '',
    last_name: '',
    email: '',
    phone_number: '',
    country: '',
    country_code: '',
  });
  const [countries, setCountries] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [message, setMessage] = useState(null);

  useEffect(() => {
    const fetchCountriesAndInitialize = async () => {
      let loadedCountries = [];
      try {
        const res = await apiClient.get('/auth/countries/');
        const data = res.data;
        loadedCountries = data.sort((a, b) => a.name.common.localeCompare(b.name.common));
        setCountries(loadedCountries);
      } catch (e) {
        console.error('Failed to fetch countries', e);
      }

      if (user) {
        let phoneNum = user.phone_number || '';
        let code = '';

        if (phoneNum.startsWith('+') && loadedCountries.length > 0) {
          let bestMatch = null;
          let longestMatchLength = 0;

          loadedCountries.forEach(c => {
            const callingCode = getCallingCode(c);
            if (callingCode && phoneNum.startsWith(callingCode)) {
              if (callingCode.length > longestMatchLength) {
                longestMatchLength = callingCode.length;
                bestMatch = callingCode;
              }
            }
          });

          if (bestMatch) {
            code = bestMatch;
            phoneNum = phoneNum.slice(bestMatch.length).trim();
          }
        }

        setFormData({
          first_name: user.first_name || '',
          last_name: user.last_name || '',
          email: user.email || '',
          phone_number: phoneNum,
          country: user.country || '',
          country_code: code,
        });
      }
    };

    fetchCountriesAndInitialize();
  }, [user]);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setIsLoading(true);
    setMessage(null);
    try {
      const combinedPhoneNumber = (formData.country_code + formData.phone_number.trim()).trim();
      const payload = {
        first_name: formData.first_name,
        last_name: formData.last_name,
        email: formData.email,
        phone_number: combinedPhoneNumber,
        country: formData.country,
      };
      const response = await apiClient.patch('/auth/profile/', payload);
      setUser(response.data);
      setMessage({ type: 'success', text: 'Profile updated successfully!' });
    } catch (error) {
      console.error('Failed to update profile', error);
      setMessage({ type: 'error', text: 'Failed to update profile.' });
    } finally {
      setIsLoading(false);
    }
  };


  if (!user) return <Navigate to={`/${locale}/login`} />;

  return (
    <div className="min-h-screen py-20 px-4 md:px-margin-desktop">
      <div className="max-w-2xl mx-auto">
        <h1 className="text-headline-md text-primary-container font-bold uppercase tracking-tighter mb-8 border-l-4 border-primary-container pl-4">
          My Account
        </h1>

        <form onSubmit={handleSubmit} className="bg-surface-container-highest border-2 border-primary-container p-6 shadow-[8px_8px_0px_rgba(0,243,255,0.3)]">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="flex flex-col gap-2">
              <label className="text-xs font-bold uppercase text-on-surface-variant">First Name</label>
              <input
                name="first_name"
                value={formData.first_name}
                onChange={handleInputChange}
                className="bg-surface-container-lowest border-2 border-outline-variant p-2 text-on-surface focus:border-primary-container outline-none transition-colors font-mono"
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-xs font-bold uppercase text-on-surface-variant">Last Name</label>
              <input
                name="last_name"
                value={formData.last_name}
                onChange={handleInputChange}
                className="bg-surface-container-lowest border-2 border-outline-variant p-2 text-on-surface focus:border-primary-container outline-none transition-colors font-mono"
              />
            </div>
            <div className="flex flex-col gap-2 md:col-span-2">
              <label className="text-xs font-bold uppercase text-on-surface-variant">Email</label>
              <input
                name="email"
                value={formData.email}
                onChange={handleInputChange}
                className="bg-surface-container-lowest border-2 border-outline-variant p-2 text-on-surface focus:border-primary-container outline-none transition-colors font-mono"
              />
            </div>

            <div className="flex flex-col gap-2 md:col-span-2">
              <label className="text-xs font-bold uppercase text-on-surface-variant">Phone Number</label>
              <div className="flex gap-2">
                <div className="relative w-1/3">
                  <select
                    className="w-full bg-surface-container-lowest border-2 border-outline-variant p-2 text-on-surface focus:border-primary-container outline-none transition-colors font-mono text-xs"
                    value={formData.country_code}
                    onChange={(e) => setFormData(prev => ({...prev, country_code: e.target.value}))}
                  >
                    <option value="">Code</option>
                    {countries.map(c => {
                      const callingCode = getCallingCode(c);
                      if (!callingCode) return null;
                      return (
                        <option key={c.cca3} value={callingCode}>
                          {c.name.common} ({callingCode})
                        </option>
                      );
                    })}
                  </select>
                </div>
                <input
                  name="phone_number"
                  placeholder="123 456 7890"
                  value={formData.phone_number}
                  onChange={handleInputChange}
                  className="flex-grow bg-surface-container-lowest border-2 border-outline-variant p-2 text-on-surface focus:border-primary-container outline-none transition-colors font-mono"
                />
              </div>
            </div>

            <div className="flex flex-col gap-2 md:col-span-2">
              <label className="text-xs font-bold uppercase text-on-surface-variant">Country</label>
              <div className="flex items-center gap-2">
                <select
                  name="country"
                  value={formData.country}
                  onChange={handleInputChange}
                  className="flex-grow bg-surface-container-lowest border-2 border-outline-variant p-2 text-on-surface focus:border-primary-container outline-none transition-colors font-mono"
                >
                  <option value="">Select Country</option>
                  {countries.map(c => (
                    <option key={c.cca3} value={c.name.common}>
                      {c.name.common}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          </div>

          {message && (
            <div className={`mt-6 p-3 text-xs font-bold uppercase border-2 ${message.type === 'success' ? 'border-primary-container text-primary-container' : 'border-error text-error'}`}>
              {message.text}
            </div>
          )}

          <button
            type="submit"
            disabled={isLoading}
            className="mt-8 brutal-btn w-full py-3 font-bold uppercase transition-all duration-150 active:scale-95"
          >
            {isLoading ? 'Saving...' : 'Save Changes'}
          </button>
        </form>
      </div>
    </div>
  );
};

export default Account;
