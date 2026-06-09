import React from 'react'
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import Navbar from './components/Navbar'
import Footer from './components/Footer'
import HomePage from './pages/HomePage'
import StoryPage from './pages/StoryPage'
import CharactersPage from './pages/CharactersPage'
import LevelsPage from './pages/LevelsPage'
import FeaturesPage from './pages/FeaturesPage'
import ControlsPage from './pages/ControlsPage'
import './App.css'

function Layout() {
  return (
    <>
      <Navbar />
      <main>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/story" element={<StoryPage />} />
          <Route path="/characters" element={<CharactersPage />} />
          <Route path="/levels" element={<LevelsPage />} />
          <Route path="/features" element={<FeaturesPage />} />
          <Route path="/controls" element={<ControlsPage />} />
          {/* 404 fallback */}
          <Route path="*" element={<HomePage />} />
        </Routes>
      </main>
      <Footer />
    </>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <Layout />
    </BrowserRouter>
  )
}
