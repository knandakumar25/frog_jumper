// Gate, not '@clerk/clerk-react' directly: when VITE_CLERK_PUBLISHABLE_KEY is
// missing from a build (e.g. Vercel, where .env.local never reaches CI), the
// gate renders the app without Clerk instead of white-screening. When the key
// is present the gate injects it itself.
import { ClerkProvider } from './auth/clerkGate.jsx';
import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.jsx'
import './index.css'
import './showcase.css'

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <ClerkProvider afterSignOutUrl="/">
      <App />
    </ClerkProvider>
  </React.StrictMode>,
)
