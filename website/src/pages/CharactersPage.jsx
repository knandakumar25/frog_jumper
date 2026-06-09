import React from 'react';
import Characters from '../components/Characters';
import ninjaFrogSprite from '@assets/images/Ninja Frog/Idle (32x32).png';
import maskDudeSprite from '@assets/images/Mask Dude/Idle (32x32).png';
import pinkManSprite from '@assets/images/Pink Man/Idle (32x32).png';
import virtualGuySprite from '@assets/images/Virtual Guy/Idle (32x32).png';
import './PageWrapper.css';

const sprites = {
  'Ninja Frog': ninjaFrogSprite,
  'Mask Dude': maskDudeSprite,
  'Pink Man': pinkManSprite,
  'Virtual Guy': virtualGuySprite,
};

export default function CharactersPage() {
  return (
    <div className="page-wrapper">
      <div className="page-hero page-hero--characters">
        <div className="page-hero__overlay" />
        <div className="container page-hero__content">
          <span className="section-label">// warrior selection</span>
          <h1 className="page-hero__title">Choose Your <span className="text-gradient-primary">Fighter</span></h1>
          <p className="page-hero__sub">Five elite warriors stand against the Xelthara occupation.</p>
        </div>
      </div>
      <Characters sprites={sprites} hideSectionHeader />
    </div>
  );
}
