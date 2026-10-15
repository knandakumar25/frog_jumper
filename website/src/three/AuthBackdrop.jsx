import React, { Suspense, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { Fitted, NeonPulseLights, WorldCanvas, useWorldMode } from './common';

/**
 * Phase 3 of the 3D website: the terminal room behind the auth pages.
 *
 * The forms themselves stay real DOM (Clerk); this is the room they stand in
 * -- datacore server towers behind, a holo projector pedestal and focus rings
 * drifting, over the simulation grid. Composition deliberately leaves the
 * center column clear: that is where the page's card sits.
 */

function Towers() {
  const xs = [-5.4, -2.7, 2.7, 5.4];
  return (
    <>
      {xs.map((x, i) => (
        <Fitted
          key={i}
          url="/models/world/datacore_server_tower.glb"
          target={3.4 + (i % 2) * 0.7}
          position={[x, -2.2, -6.5]}
          rotation={[0, i * 0.6, 0]}
        />
      ))}
    </>
  );
}

function HoloRings() {
  const ref = useRef();
  useFrame((state) => {
    if (!ref.current) return;
    const t = state.clock.elapsedTime;
    ref.current.rotation.y = t * 0.25;
    ref.current.rotation.x = Math.sin(t * 0.4) * 0.12;
    ref.current.position.y = 0.35 + Math.sin(t * 0.7) * 0.08;
  });
  return (
    <group ref={ref} position={[0, 0.35, -1.4]}>
      <Fitted url="/models/world/holo_focus_rings.glb" target={2.6} />
      <pointLight color="#00f3ff" intensity={2.4} distance={9} />
    </group>
  );
}

function Pedestal() {
  const ref = useRef();
  useFrame((state) => {
    if (!ref.current) return;
    ref.current.rotation.y = Math.sin(state.clock.elapsedTime * 0.2) * 0.3;
  });
  return (
    <group ref={ref} position={[-3.1, -2.2, 1.4]}>
      <Fitted url="/models/world/holo_projector_pedestal.glb" target={1.7} />
      <pointLight color="#fe00fe" intensity={1.8} distance={7} />
    </group>
  );
}

function CompanionOrb() {
  // original website asset: AXIOM-7's companion orb, watching the terminal
  const ref = useRef();
  useFrame((state) => {
    const t = state.clock.elapsedTime;
    if (ref.current) {
      ref.current.rotation.y = t * 0.3;
      ref.current.position.y = 1.35 + Math.sin(t * 0.55) * 0.09;
    }
  });
  return (
    <group ref={ref} position={[2.9, 1.35, 0.9]}>
      <Fitted url="/models/web/axiom_orb.glb" target={1.0} />
      <pointLight color="#9fe9ff" intensity={1.6} distance={6} />
    </group>
  );
}

function Drift({ animate }) {
  useFrame((state, dt) => {
    if (!animate) return;
    const cam = state.camera;
    const t = state.clock.elapsedTime;
    cam.position.x += (Math.sin(t * 0.12) * 0.6 - cam.position.x) * Math.min(1, dt * 0.5);
    cam.position.y += (1.42 + Math.sin(t * 0.19) * 0.1 - cam.position.y) * Math.min(1, dt * 0.5);
    cam.lookAt(0, 0.9, -2);
  });
  return null;
}

export default function AuthBackdrop({ quality = 'high' }) {
  const mode = useWorldMode();
  return (
    <WorldCanvas
      mode={mode}
      quality={quality}
      camera={{ position: [0, 1.42, 7.2], fov: 46, near: 0.1, far: 90 }}
    >
      {(animate) => (
        <>
          <NeonPulseLights fogNear={8} fogFar={34} magenta={[-6, 1.8, 2]} cyan={[6, 2.4, 1]} />
          <gridHelper args={[60, 48, '#0d3040', '#092029']} position={[0, -2.2, 0]} />
          <Suspense fallback={null}>
            <Towers />
            <HoloRings />
            <Pedestal />
            <CompanionOrb />
          </Suspense>
          <Drift animate={animate} />
        </>
      )}
    </WorldCanvas>
  );
}