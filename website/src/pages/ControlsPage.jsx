import React from 'react';
import Controls from '../components/Controls';
import './PageWrapper.css';

export default function ControlsPage() {
  return (
    <div className="page-wrapper">
      <div className="page-hero page-hero--controls">
        <div className="page-hero__overlay" />
        <div className="container page-hero__content">
          <span className="section-label">// command interface</span>
          <h1 className="page-hero__title">Master the <span className="text-gradient-primary">Controls</span></h1>
          <p className="page-hero__sub">Keyboard and gamepad support. Every ability at your fingertips.</p>
        </div>
      </div>
      <Controls hideSectionHeader />
    </div>
  );
}
