import React, { Suspense, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { Fitted, NeonPulseLights, WorldCanvas, useWorldMode } from './common';

/**
 * Original website assets, scene 3: the deploy kiosk row on TechHub.
 *
 * Three kiosk pods on a slow display turn, each carrying its platform's
 * emissive glyph plate (WINDOWS / LINUX / ANDROID -- iOS joins when the
 * roster needs it). These are assets authored for the site itself
 * (tools/assets/build_web_assets.py), not game reuses.
 */

const KIOSKS = [
  { glyph: '/models/web/platform_glyph_windows.glb', x: -2.7, accent: '#00f3ff' },
  { glyph: '/models/web/platform_glyph_linux.glb', x: 0, accent: '#fe00fe' },
  { glyph: '/models/web/platform_glyph_android.glb', x: 2.7, accent: '#00f3ff' },
];

function Kiosk({ glyph, x, accent, index }) {
  const turn = useRef();
  useFrame((state) => {
    if (turn.current) turn.current.rotation.y = Math.sin(state.clock.elapsedTime * 0.35 + index * 2.1) * 0.16;
  });
  return (
    <group position={[x, -1.35, 0]}>
      <group ref={turn}>
        <Fitted url="/models/web/platform_kiosk.glb" target={1.75} />
        {/* the platform plate, slotted into the kiosk's rail */}
        <group position={[0, 0.87, 0.12]}>
          <Fitted url={glyph} target={0.5} />
        </group>
      </group>
      <pointLight position={[0, 1.5, 0.6]} intensity={1.5} color={accent} distance={5} />
    </group>
  );
}

export default function KioskRow({ quality = 'high' }) {
  const mode = useWorldMode();
  return (
    <WorldCanvas
      mode={mode}
      quality={quality}
      camera={{ position: [0, 1.5, 7.6], fov: 42, near: 0.1, far: 80 }}
    >
      {() => (
        <>
          <NeonPulseLights fogNear={9} fogFar={30} magenta={[-7, 2.4, 2]} cyan={[7, 2.4, 2]} />
          <gridHelper args={[50, 40, '#0d3040', '#092029']} position={[0, -1.4, 0]} />
          <Suspense fallback={null}>
            {KIOSKS.map((k, i) => (
              <Kiosk key={k.glyph} {...k} index={i} />
            ))}
          </Suspense>
        </>
      )}
    </WorldCanvas>
  );
}