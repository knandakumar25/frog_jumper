import React, { Suspense, useEffect, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { Fitted, NeonPulseLights, WorldCanvas, useWorldMode } from './common';

/**
 * Phase 1 of the 3D website: the landing hero world.
 *
 * A full-bleed scene behind the Home page's DOM content (SEO/a11y intact --
 * text stays HTML; this is decoration around it). Reuses the game's own
 * optimized assets: cyber_city_skyline, the patrol drone, and the Cyber
 * Shogun on a glowing podium, over a wireframe simulation grid.
 *
 * Degradation lives in common.jsx (no WebGL -> nothing; reduced motion ->
 * a still; quality -> dpr).
 */

function Beacon() {
  // the brand monument (original website asset); left third, clear of the
  // centred logo and terminal copy
  return (
    <group position={[-7.5, 0.1, -8.0]}>
      <Fitted url="/models/web/axiom_beacon.glb" target={7.4} />
      <pointLight position={[0, 6.0, 1.8]} intensity={3.0} color="#00f3ff" distance={16} />
    </group>
  );
}

function Skyline() {
  return (
    <Fitted url="/models/world/cyber_city_skyline.glb" target={70} position={[0, -3.4, -26]} />
  );
}

function Drone({ animate }) {
  const ref = useRef();
  useFrame((state) => {
    if (!ref.current || !animate) return;
    const t = state.clock.elapsedTime;
    ref.current.position.y = 1.5 + Math.sin(t * 0.9) * 0.18;
    ref.current.rotation.y = t * 0.35;
  });
  return (
    <group ref={ref} position={[-4.2, 3.4, -4.0]}>
      <Fitted url="/models/world/patrol_surveillance_drone.glb" target={0.95} />
      <pointLight color="#00f3ff" intensity={2.2} distance={6} />
    </group>
  );
}

function Operative({ animate }) {
  const ref = useRef();
  useFrame((state) => {
    if (ref.current && animate) ref.current.rotation.y = state.clock.elapsedTime * 0.25;
  });
  return (
    <group position={[4.9, -0.45, 1.2]}>
      <mesh position={[0, 0.11, 0]}>
        <cylinderGeometry args={[1.5, 1.62, 0.22, 40]} />
        <meshStandardMaterial color="#0b1016" metalness={0.7} roughness={0.35} />
      </mesh>
      <mesh position={[0, 0.235, 0]} rotation={[-Math.PI / 2, 0, 0]}>
        <ringGeometry args={[1.32, 1.44, 64]} />
        <meshBasicMaterial color="#00f3ff" toneMapped={false} />
      </mesh>
      <group ref={ref}>
        <Fitted url="/models/shogun.glb" target={3.3} />
      </group>
    </group>
  );
}

function CameraRig({ pointerRef, animate }) {
  useFrame((state, dt) => {
    const cam = state.camera;
    const p = pointerRef.current;
    const tx = animate ? p.x * 1.1 : 0;
    const ty = animate ? 2.4 + p.y * 0.45 : 2.4;
    cam.position.x += (tx - cam.position.x) * Math.min(1, dt * 1.4);
    cam.position.y += (ty - cam.position.y) * Math.min(1, dt * 1.4);
    cam.lookAt(0, 1.2, 0);
  });
  return null;
}

function Scene({ animate }) {
  const pointerRef = useRef({ x: 0, y: 0 });

  useEffect(() => {
    if (!animate) return undefined;
    const onMove = (e) => {
      pointerRef.current.x = (e.clientX / window.innerWidth) * 2 - 1;
      pointerRef.current.y = -((e.clientY / window.innerHeight) * 2 - 1);
    };
    window.addEventListener('pointermove', onMove, { passive: true });
    return () => window.removeEventListener('pointermove', onMove);
  }, [animate]);

  return (
    <>
      <NeonPulseLights fogNear={16} fogFar={60} magenta={[-8, 3.0, 2]} cyan={[8, 3.0, 2]} />
      <gridHelper args={[70, 56, '#0d3040', '#092029']} position={[0, -2.4, 0]} />
      <Suspense fallback={null}>
        <Skyline />
        <Beacon />
        <Drone animate={animate} />
        <Operative animate={animate} />
      </Suspense>
      <CameraRig pointerRef={pointerRef} animate={animate} />
    </>
  );
}

export default function HomeWorld({ quality = 'high', className = '' }) {
  const mode = useWorldMode();
  return (
    <WorldCanvas
      mode={mode}
      quality={quality}
      className={className}
      camera={{ position: [0, 2.4, 10.5], fov: 42, near: 0.1, far: 160 }}
    >
      {(animate) => <Scene animate={animate} />}
    </WorldCanvas>
  );
}