import React, { useEffect, useMemo, useState } from 'react';
import { Canvas, useThree } from '@react-three/fiber';
import { useGLTF } from '@react-three/drei';
import * as THREE from 'three';

/**
 * Shared plumbing for the website's 3D scenes (HomeWorld, SquadHall).
 *
 * Degradation contract, decided during the redesign kickoff:
 *   no WebGL         -> scenes render nothing; pages keep their CSS fallback
 *   prefers-reduced-motion -> scene renders as a still (frameloop 'demand')
 *   quality prop     -> 'high' | 'low'; controls dpr (and future light tiers)
 */

// The Draco decoder ships with the site (public/draco, copied from three) so
// models decode offline. drei's string form of this argument is deliberate:
// passing `true` makes drei overwrite any custom loader with its CDN default.
export const DRACO_DECODER_PATH = '/draco/';

export function supportsWebGL() {
  try {
    const c = document.createElement('canvas');
    return !!(window.WebGLRenderingContext && (c.getContext('webgl2') || c.getContext('webgl')));
  } catch {
    return false;
  }
}

/**
 * 'off' | 'still' | 'live' -- resolved once per mount.
 *
 * `?motion=on` / `?motion=off` overrides the system preference (also useful
 * to QA the animated path in headless browsers, which report reduced-motion).
 */
export function useWorldMode() {
  return useState(() => {
    if (typeof window === 'undefined') return 'off';
    if (!supportsWebGL()) return 'off';
    const override = new URLSearchParams(window.location.search).get('motion');
    if (override === 'on') return 'live';
    if (override === 'off') return 'still';
    const reduced = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    return reduced ? 'still' : 'live';
  })[0];
}

/**
 * In 'still' mode the renderer draws on demand -- and the GLBs are still
 * loading at mount, so the first (and only) frame would be empty. This pokes
 * the renderer a few times as assets arrive so a still is actually a still.
 */
function StillFrames() {
  const invalidate = useThree((state) => state.invalidate);
  useEffect(() => {
    const timers = [400, 1200, 2400, 4000].map((ms) => setTimeout(invalidate, ms));
    return () => timers.forEach(clearTimeout);
  }, [invalidate]);
  return null;
}

/**
 * The canvas wrapper every scene uses: handles the mode check, dpr, frameloop
 * and pointer semantics. Returns null in 'off' mode so the page's CSS
 * fallback background shows through.
 */
export function WorldCanvas({ mode, quality = 'high', camera, children, className = '' }) {
  if (mode === 'off') return null;
  const animate = mode === 'live';
  return (
    <div className={`absolute inset-0 ${className}`} aria-hidden="true">
      <Canvas
        dpr={quality === 'high' ? [1, 1.5] : [0.75, 1]}
        frameloop={animate ? 'always' : 'demand'}
        camera={camera}
        gl={{ antialias: true, alpha: true, powerPreference: 'high-performance' }}
      >
        {children(animate)}
        {!animate && <StillFrames />}
      </Canvas>
    </div>
  );
}

/** Load a GLB, center it on its own origin, scale to `target` max dimension. */
export function Fitted({ url, target = 10, anchorBottom = true, ...props }) {
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

/** Ambient/directional/neon point-light kit + fog, shared by both scenes. */
export function NeonPulseLights({ fogNear = 12, fogFar = 46, magenta = [-7, 2.5, 3], cyan = [7, 2.0, 4] }) {
  return (
    <>
      <fog attach="fog" args={['#050505', fogNear, fogFar]} />
      <ambientLight intensity={0.35} />
      <directionalLight position={[6, 9, 4]} intensity={1.1} color="#bfefff" />
      <pointLight position={magenta} intensity={3.2} color="#fe00fe" distance={22} />
      <pointLight position={cyan} intensity={2.6} color="#00f3ff" distance={22} />
    </>
  );
}