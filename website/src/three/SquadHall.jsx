import React, { Suspense, useEffect, useMemo, useRef, useState } from 'react';
import { useFrame } from '@react-three/fiber';
import * as THREE from 'three';
import { WorldCanvas, Fitted, NeonPulseLights, useWorldMode } from './common';

/**
 * Phase 2 of the 3D website: the operative podium hall.
 *
 * Replaces the Squad page's single model-viewer display port with a hall of
 * all six operatives on glowing platforms. The active operative is swung to
 * front-center and lit brighter; clicking a platform selects it (the page's
 * existing thumbnail grid stays as the accessible selector and fallback).
 */

const ARC_SPACING = (25 * Math.PI) / 180; // radians between platforms
const ARC_RADIUS = 5.0;
const PLATFORM_Y = -1.55;

function Platform({ entry, index, isActive, onSelect }) {
  const angle = (index - 2.5) * ARC_SPACING;
  const x = Math.sin(angle) * ARC_RADIUS;
  const z = Math.cos(angle) * ARC_RADIUS - 1.2;
  const turnRef = useRef();
  const ringRef = useRef();
  const [hovered, setHovered] = useState(false);

  useFrame((state, dt) => {
    if (turnRef.current) turnRef.current.rotation.y = state.clock.elapsedTime * 0.22 + index;
    if (ringRef.current) {
      const target = isActive ? 1 : 0.12;
      const mat = ringRef.current.material;
      mat.opacity += (target - mat.opacity) * Math.min(1, dt * 4);
    }
  });

  return (
    <group position={[x, PLATFORM_Y, z]}>
      {/* podium */}
      <mesh
        position={[0, 0.12, 0]}
        onClick={(e) => { e.stopPropagation(); onSelect(entry.id); }}
        onPointerOver={(e) => { e.stopPropagation(); setHovered(true); document.body.style.cursor = 'pointer'; }}
        onPointerOut={() => { setHovered(false); document.body.style.cursor = 'auto'; }}
      >
        <cylinderGeometry args={[isActive ? 1.06 : 0.92, isActive ? 1.16 : 1.0, 0.24, 36]} />
        <meshStandardMaterial color={hovered ? '#16222e' : '#0b1016'} metalness={0.7} roughness={0.35} />
      </mesh>
      {/* glowing rim: bright for the active operative */}
      <mesh ref={ringRef} position={[0, 0.245, 0]} rotation={[-Math.PI / 2, 0, 0]}>
        <ringGeometry args={[0.82, 0.96, 48]} />
        <meshBasicMaterial color={isActive ? '#00f3ff' : '#1c6a80'} transparent opacity={0.12} toneMapped={false} />
      </mesh>
      {isActive && (
        <pointLight position={[0, 2.4, 0.4]} intensity={3.4} color="#9fe9ff" distance={9} />
      )}
      <group ref={turnRef}>
        <Fitted url={entry.model} target={isActive ? 3.05 : 2.75} />
      </group>
    </group>
  );
}

function Hall({ roster, activeId, onSelect }) {
  const arcRef = useRef();
  const activeIndex = Math.max(0, roster.findIndex((r) => r.id === activeId));
  // the active platform's angular offset; rotating the arc by its negative
  // swings that operative to front-center
  const goal = -(activeIndex - 2.5) * ARC_SPACING;

  useFrame((state, dt) => {
    if (!arcRef.current) return;
    arcRef.current.rotation.y += (goal - arcRef.current.rotation.y) * Math.min(1, dt * 2.2);
  });

  return (
    <group ref={arcRef} rotation={[0, goal, 0]}>
      {roster.map((entry, i) => (
        <Platform key={entry.id} entry={entry} index={i} isActive={entry.id === activeId} onSelect={onSelect} />
      ))}
    </group>
  );
}

export default function SquadHall({ roster, activeId, onSelect, quality = 'high' }) {
  const mode = useWorldMode();
  return (
    <WorldCanvas mode={mode} quality={quality} camera={{ position: [0, 1.35, 9.6], fov: 42, near: 0.1, far: 100 }}>
      {(animate) => (
        <>
          <NeonPulseLights fogNear={10} fogFar={40} magenta={[-8, 2.2, 2]} cyan={[8, 2.0, 3]} />
          <gridHelper args={[70, 56, '#0d3040', '#092029']} position={[0, PLATFORM_Y, 0]} />
          <Suspense fallback={null}>
            <Hall roster={roster} activeId={activeId} onSelect={onSelect} animate={animate} />
          </Suspense>
        </>
      )}
    </WorldCanvas>
  );
}