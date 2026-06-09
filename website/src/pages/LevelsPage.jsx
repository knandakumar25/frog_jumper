import React from 'react';
import Levels from '../components/Levels';
import cyberCityBg from '@assets/images/cyber_city_dusk_background.png';
import trainingDojoBg from '@assets/images/training_dojo_background.png';
import storyBg from '@assets/images/story-screen-image.png';
import './PageWrapper.css';

export default function LevelsPage() {
  return (
    <div className="page-wrapper">
      <div className="page-hero page-hero--levels">
        <div className="page-hero__overlay" />
        <div className="container page-hero__content">
          <span className="section-label">// campaign levels</span>
          <h1 className="page-hero__title">The <span className="text-gradient-primary">Mission</span> Awaits</h1>
          <p className="page-hero__sub">Five unique levels with increasing difficulty and stakes.</p>
        </div>
      </div>
      <Levels bgCyber={cyberCityBg} bgDojo={trainingDojoBg} bgStory={storyBg} hideSectionHeader />
    </div>
  );
}
