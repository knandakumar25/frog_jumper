import React, { Suspense, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { Fitted, NeonPulseLights, WorldCanvas, useWorldMode } from './common';

/**
 * Phase 4 of the 3D website: the mission-briefing vista on the Gameplay page.
 *
 * Replaces the externally hosted placeholder image in the "LOS ANGELES 2157"
 * briefing with a live feed: the game's skyline under heavy fog with a slow
 * surveillance pan, so the briefing window reads like a drone downlink.
 * The page's gradient, labels and copy sit on top unchanged.
 */

function Skyline() {
  return (
    <>
      <Fitted url="/models/world/cyber_city_skyline.glb" target={78} position={[0, -1.2, -26]} />
      {/* the orange sodium glow of the occupied city, over the neon kit */}
      <pointLight position={[0, 5.5, -18]} intensity={1.6} color="#ff8844" distance={30} />
    </>
  );
}

function SurveillancePan({ animate }) {
  useFrame((state) => {
    if (!animate) return;
    const cam = state.camera;
    const t = state.clock.elapsedTime;
    cam.position.x = Math.sin(t * 0.05) * 2.4; // slow drone pan
    cam.position.y = 1.15 + Math.sin(t * 0.11) * 0.12;
    cam.lookAt(0, 3.0, -16);
  });
  return null;
}

export default function IntelBackdrop({ quality = 'high' }) {
  const mode = useWorldMode();
  return (
    <WorldCanvas
      mode={mode}
      quality={quality}
      camera={{ position: [0, 1.15, -2.0], fov: 44, near: 0.1, far: 140 }}
    >
      {(animate) => (
        <>
          <NeonPulseLights fogNear={6} fogFar={44} magenta={[-7, 3.5, -10]} cyan={[7, 3.0, -12]} />
          <Suspense fallback={null}>
            <Skyline />
          </Suspense>
          <SurveillancePan animate={animate} />
        </>
      )}
    </WorldCanvas>
  );
}