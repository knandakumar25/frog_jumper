import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import apiClient from '../api/client';

const BrowserAuth = () => {
  const { user, logout } = useAuth();
  const [sessionId, setSessionId] = useState(null);
  const [status, setStatus] = useState('idle'); // idle, confirming, authorized, error

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const sid = params.get('session_id');
    if (sid) {
      setSessionId(sid);
      setStatus('confirming');
    }
  }, []);

  const handleConfirm = async () => {
    try {
      await apiClient.post('/auth/browser-confirm/', { session_id: sessionId });
      setStatus('authorized');
    } catch (error) {
      setStatus('error');
    }
  };

  if (!user) {
    return (
      <div style={{ padding: '20px', textAlign: 'center' }}>
        <h1>Authentication Required</h1>
        <p>Please log in to your account to authorize the game client.</p>
        <a href="/login" style={{ color: 'blue', textDecoration: 'underline' }}>Go to Login Page</a>
      </div>
    );
  }

  return (
    <div style={{ padding: '20px', textAlign: 'center' }}>
      <h1>Authorize Game Client</h1>
      {status === 'confirming' && (
        <div>
          <p>The game client is requesting access to your account.</p>
          <button
            onClick={handleConfirm}
            style={{ padding: '10px 20px', fontSize: '16px', cursor: 'pointer', backgroundColor: '#4CAF50', color: 'white', border: 'none', borderRadius: '4px' }}
          >
            Authorize Now
          </button>
        </div>
      )}
      {status === 'authorized' && (
        <div style={{ color: 'green' }}>
          <p>✓ Your account has been authorized. You can return to the game.</p>
        </div>
      )}
      {status === 'error' && (
        <div style={{ color: 'red' }}>
          <p>An error occurred during authorization. Please try again.</p>
          <button onClick={() => setStatus('confirming')}>Try Again</button>
        </div>
      )}
      {status === 'idle' && (
        <p>No active session found for authorization.</p>
      )}
    </div>
  );
};

export default BrowserAuth;
