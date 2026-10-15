import React, { Suspense, useEffect, useMemo, useRef, useState } from 'react';
import { Canvas, useFrame } from '@react-three/fiber';
import { useGLTF } from '@react-three/drei';
import * as THREE from 'three';

/**
 * Phase 1 of the 3D website: the landing hero world.
 *
 * A full-bleed scene behind the Home page's DOM content (SEO/a11y intact --
 * text stays HTML; this is decoration around it). Reuses the game's own
 * optimized assets: cyber_city_skyline, the patrol drone, and the Cyber
 * Shogun on a glowing podium, over a wireframe simulation grid.
 *
 * Degradation (per the redesign decisions):
 *  - no WebGL      -> renders nothing; the CSS fallback background shows
 *  - reduced motion -> the world renders as a still (frameloop 'demand'),
 *                      no parallax, no drift
 *  - quality prop  -> 'high' | 'low' (dpr + shadows off) for the toggle
 */

// The Draco decoder ships with the site (public/draco, copied from three),
// so the models decode offline instead of from a CDN. drei's second useGLTF
// argument as a string is the decoder path; note that passing `true` would
// overwrite a custom loader with its gstatic CDN default.
const DRACO_DECODER_PATH = '/draco/';

/** Load a GLB, center it on its own origin, scale to `target` max dimension. */
function Fitted({ url, target = 10, anchorBottom = true, ...props }) {
  const { scene } = useGLTF(url, DRACO_DECODER_PATH, false);
  const fit = useMemo(() => {
    const box = new THREE.Box3().setFromObject(scene);
    const size = box.getSize(new THREE.Vector3());
    const center = box.getCenter(new THREE.Vector3());
    const scale = target / Math.max(size.x, size.y, size.z);
    const offset = center.multiplyScalar(-1);
    if (anchorBottom) offset.y -= size.y / 2; // model base sits on the group's y=0
    return { scale, offset };
  }, [scene, target, anchorBottom]);

  return (
    <group {...props} scale={fit.scale}>
      <primitive object={scene} position={fit.offset} />
    </group>
  );
}

function Skyline() {
  return (
    <Fitted url="/models/world/cyber_city_skyline.glb" target={62} position={[0, -2.4, -20]} />
  );
}

function Drone({ animate }) {
  const ref = useRef();
  useFrame((state) => {
    if (!ref.current) return;
    if (animate) {
      const t = state.clock.elapsedTime;
      ref.current.position.y = 1.5 + Math.sin(t * 0.9) * 0.18;
      ref.current.rotation.y = t * 0.35;
    }
  });
  return (
    <group ref={ref} position={[-3.6, 1.5, 1.6]}>
      <Fitted url="/models/world/patrol_surveillance_drone.glb" target={0.85} position={[0, 0, 0]} />
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
    <group position={[3.6, -2.05, 1.0]}>
      {/* podium */}
      <mesh position={[0, 0.11, 0]}>
        <cylinderGeometry args={[1.5, 1.62, 0.22, 40]} />
        <meshStandardMaterial color="#0b1016" metalness={0.7} roughness={0.35} />
      </mesh>
      <mesh position={[0, 0.235, 0]} rotation={[-Math.PI / 2, 0, 0]}>
        <ringGeometry args={[1.32, 1.44, 64]} />
        <meshBasicMaterial color="#00f3ff" toneMapped={false} />
      </mesh>
      <group ref={ref}>
        <Fitted url="/models/shogun.glb" target={3.15} />
      </group>
    </group>
  );
}

function CameraRig({ pointerRef, animate }) {
  useFrame((state, dt) => {
    const cam = state.camera;
    const p = pointerRef.current;
    const tx = animate ? p.x * 1.1 : 0;
    const ty = animate ? 1.72 + p.y * 0.45 : 1.72;
    cam.position.x += (tx - cam.position.x) * Math.min(1, dt * 1.4);
    cam.position.y += (ty - cam.position.y) * Math.min(1, dt * 1.4);
    cam.lookAt(0, 0.55, 0);
  });
  return null;
}

function Scene({ animate, quality }) {
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
      <fog attach="fog" args={['#050505', 12, 46]} />
      <ambientLight intensity={0.35} />
      <directionalLight position={[6, 9, 4]} intensity={1.1} color="#bfefff" />
      <pointLight position={[-7, 2.5, 3]} intensity={3.2} color="#fe00fe" distance={22} />
      <pointLight position={[7, 2.0, 4]} intensity={2.6} color="#00f3ff" distance={22} />

      <gridHelper args={[70, 56, '#0d3040', '#092029']} position={[0, -2.4, 0]} />

      <Suspense fallback={null}>
        <Skyline />
        <Drone animate={animate} />
        <Operative animate={animate} />
      </Suspense>

      <CameraRig pointerRef={pointerRef} animate={animate} />
      {/* quality hook: reserve for cheaper lighting tiers later */}
      <group visible={quality !== 'none'} />
    </>
  );
}

function supportsWebGL() {
  try {
    const c = document.createElement('canvas');
    return !!(window.WebGLRenderingContext && (c.getContext('webgl2') || c.getContext('webgl')));
  } catch {
    return false;
  }
}

export default function HomeWorld({ quality = 'high', className = '' }) {
  const [mode] = useState(() => {
    if (typeof window === 'undefined') return 'off';
    if (!supportsWebGL()) return 'off';
    const reduced = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    return reduced ? 'still' : 'live';
  });

  if (mode === 'off') return null; // CSS fallback background takes over
  const animate = mode === 'live';

  return (
    <div className={`absolute inset-0 ${className}`} aria-hidden="true">
      <Canvas
        dpr={quality === 'high' ? [1, 1.5] : [0.75, 1]}
        frameloop={animate ? 'always' : 'demand'}
        camera={{ position: [0, 1.72, 9], fov: 45, near: 0.1, far: 120 }}
        gl={{ antialias: true, alpha: true, powerPreference: 'high-performance' }}
      >
        <Scene animate={animate} quality={quality} />
      </Canvas>
    </div>
  );
}