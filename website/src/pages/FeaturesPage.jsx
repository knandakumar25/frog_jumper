import React from 'react';
import Features from '../components/Features';
import './PageWrapper.css';

export default function FeaturesPage() {
  return (
    <div className="page-wrapper">
      <div className="page-hero page-hero--features">
        <div className="page-hero__overlay" />
        <div className="container page-hero__content">
          <span className="section-label">// system capabilities</span>
          <h1 className="page-hero__title">Game <span className="text-gradient-primary">Features</span></h1>
          <p className="page-hero__sub">Deep systems, rich audio, and 50+ language support.</p>
        </div>
      </div>
      <Features hideSectionHeader />
    </div>
  );
}
