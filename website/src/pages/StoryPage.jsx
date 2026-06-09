import React from 'react';
import Story from '../components/Story';
import storyBg from '@assets/images/story-screen-image.png';
import './PageWrapper.css';

export default function StoryPage() {
  return (
    <div className="page-wrapper">
      <div className="page-hero page-hero--story">
        <div className="page-hero__overlay" />
        <div className="container page-hero__content">
          <span className="section-label">// narrative briefing</span>
          <h1 className="page-hero__title">The <span className="text-gradient-primary">Story</span></h1>
          <p className="page-hero__sub">Earth has fallen. Four warriors rise.</p>
        </div>
      </div>
      <Story bgImage={storyBg} hideSectionHeader />
    </div>
  );
}
