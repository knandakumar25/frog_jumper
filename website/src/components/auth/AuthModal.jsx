import React, { useState } from 'react';
import { useAuth } from '../../context/AuthContext';

const AuthModal = ({ isOpen, onClose }) => {
  const [isLogin, setIsLogin] = useState(true);
  const [formData, setFormData] = useState({ username: '', email: '', password: '' });
  const { login, register } = useAuth();

  if (!isOpen) return null;

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      if (isLogin) {
        await login(formData.username, formData.password);
      } else {
        await register(formData.username, formData.email, formData.password);
        setIsLogin(true); // Switch to login after registration
      }
      onClose();
    } catch (error) {
      alert('Authentication failed. Please check your credentials.');
    }
  };

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-black/60 backdrop-blur-sm">
      <div className="bg-surface border-2 border-primary-container p-8 w-full max-w-md relative shadow-[0_0_20px_rgba(0,243,255,0.3)]">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 text-on-surface-variant hover:text-primary"
        >
          ✕
        </button>

        <h2 className="text-headline-md text-primary-container uppercase mb-6 text-center font-bold">
          {isLogin ? 'User Login' : 'Create Account'}
        </h2>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <input
            type="text"
            placeholder="Username"
            className="bg-surface-container-highest border border-outline-variant p-3 text-on-surface outline-none focus:border-primary-container transition-colors"
            value={formData.username}
            onChange={(e) => setFormData({ ...formData, username: e.target.value })}
            required
          />
          {!isLogin && (
            <input
              type="email"
              placeholder="Email"
              className="bg-surface-container-highest border border-outline-variant p-3 text-on-surface outline-none focus:border-primary-container transition-colors"
              value={formData.email}
              onChange={(e) => setFormData({ ...formData, email: e.target.value })}
              required
            />
          )}
          <input
            type="password"
            placeholder="Password"
            className="bg-surface-container-highest border border-outline-variant p-3 text-on-surface outline-none focus:border-primary-container transition-colors"
            value={formData.password}
            onChange={(e) => setFormData({ ...formData, password: e.target.value })}
            required
          />
          <button
            type="submit"
            className="brutal-btn py-3 font-bold uppercase text-primary-container bg-primary-container/10 hover:bg-primary-container/20 transition-all"
          >
            {isLogin ? 'Login' : 'Register'}
          </button>
        </form>

        <div className="mt-6 text-center">
          <button
            onClick={() => setIsLogin(!isLogin)}
            className="text-sm text-on-surface-variant hover:text-primary transition-colors underline"
          >
            {isLogin ? 'Need an account? Register' : 'Already have an account? Login'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default AuthModal;
