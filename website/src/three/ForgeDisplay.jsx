import React, { Suspense, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { Fitted, NeonPulseLights, WorldCanvas, useWorldMode } from './common';

/**
 * Phase 5 of the 3D website: the forge display case on the TechHub page.
 *
 * Replaces the externally hosted placeholder image in the engine-specs
 * column with the game's own virtual forge arena as a slowly turning
 * exhibit, ringed by the holo focus rings. The page's status overlay sits
 * on top unchanged.
 */

function Forge({ animate }) {
  const ref = useRef();
  useFrame((state) => {
    if (ref.current && animate) ref.current.rotation.y = state.clock.elapsedTime * 0.12;
  });
  return (
    <group ref={ref}>
      <Fitted url="/models/world/virtual_forge.glb" target={9.5} />
    </group>
  );
}

function Rings() {
  const ref = useRef();
  useFrame((state) => {
    if (!ref.current) return;
    const t = state.clock.elapsedTime;
    ref.current.rotation.y = -t * 0.22;
    ref.current.rotation.z = Math.sin(t * 0.3) * 0.1;
  });
  return (
    <group ref={ref} position={[0, 3.4, 0]}>
      <Fitted url="/models/world/holo_focus_rings.glb" target={4.4} />
      <pointLight color="#00f3ff" intensity={3.0} distance={12} />
    </group>
  );
}

export default function ForgeDisplay({ quality = 'high' }) {
  const mode = useWorldMode();
  return (
    <WorldCanvas
      mode={mode}
      quality={quality}
      camera={{ position: [0, 2.6, 10.5], fov: 40, near: 0.1, far: 80 }}
    >
      {(animate) => (
        <>
          <NeonPulseLights fogNear={8} fogFar={32} magenta={[-6, 3.0, 3]} cyan={[6, 2.6, 3]} />
          <gridHelper args={[50, 40, '#0d3040', '#092029']} position={[0, -1.4, 0]} />
          <Suspense fallback={null}>
            <Forge animate={animate} />
            <Rings />
          </Suspense>
        </>
      )}
    </WorldCanvas>
  );
}