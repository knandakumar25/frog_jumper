import React from 'react';
import './Controls.css';

const KEYBOARD_CONTROLS = [
  { action: 'Move Left', keys: ['A', '←'] },
  { action: 'Move Right', keys: ['D', '→'] },
  { action: 'Jump', keys: ['W', '↑', 'Space'] },
  { action: 'Sword Attack', keys: ['LMB'], note: 'Cyber Shogun only' },
  { action: 'Karate Attack', keys: ['Q'], note: 'Cyber Shogun only' },
  { action: 'Pause', keys: ['Esc'] },
];

const GAMEPAD_CONTROLS = [
  { action: 'Move Left', keys: ['D-Pad ←', 'L-Stick'] },
  { action: 'Move Right', keys: ['D-Pad →', 'L-Stick'] },
  { action: 'Jump', keys: ['A / ✕'] },
  { action: 'Pause', keys: ['Start'] },
];

const COMBAT_MOVES = [
  { move: 'Plasma Blade Strike', input: 'Light Attack', icon: '⚔️', color: 'var(--color-primary)' },
  { move: 'Energy Tower Shield', input: 'LB / L1', icon: '🛡️', color: 'var(--color-secondary)' },
  { move: 'Kinetic Overload', input: 'Hold RB/R1 + Heavy', icon: '💥', color: 'var(--color-accent)' },
  { move: 'Phase Shift', input: 'L2 / LT', icon: '👻', color: 'var(--color-gold)' },
  { move: 'Drone Mark & Fire', input: 'Hold LT + Release', icon: '🚁', color: 'var(--color-green)' },
  { move: 'EMP Burst', input: '○+△ / B+Y', icon: '⚡', color: 'var(--color-secondary)' },
  { move: 'Shield Overload', input: 'Hold X+Y / □+△', icon: '🌟', color: 'var(--color-gold)' },
  { move: 'Orbital Strike', input: '↑↑↓↓←→', icon: '☄️', color: 'var(--color-accent)' },
];

function Key({ label }) {
  return <kbd className="key">{label}</kbd>;
}

export default function Controls({ hideSectionHeader }) {
  return (
    <section className="controls section grid-bg" id="controls">
      <div className="container">
        {!hideSectionHeader && (
          <div className="controls__header">
            <span className="section-label">// command interface</span>
            <h2 className="section-title">
              Master the <span className="text-gradient-primary">Controls</span>
            </h2>
            <div className="divider" />
            <p className="section-description">
              Full keyboard and gamepad support. Each warrior's unique abilities map to
              dedicated input commands — master them all to survive.
            </p>
          </div>
        )}

        <div className="controls__layout">
          {/* Keyboard */}
          <div className="controls__group">
            <h3 className="controls__group-title">
              <span className="controls__group-icon">⌨️</span>
              Keyboard
            </h3>
            <div className="controls__table">
              {KEYBOARD_CONTROLS.map(row => (
                <div key={row.action} className="controls__row">
                  <span className="controls__action">
                    {row.action}
                    {row.note && <span className="controls__note">{row.note}</span>}
                  </span>
                  <div className="controls__keys">
                    {row.keys.map((k, i) => (
                      <React.Fragment key={k}>
                        <Key label={k} />
                        {i < row.keys.length - 1 && <span className="controls__or">or</span>}
                      </React.Fragment>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Gamepad */}
          <div className="controls__group">
            <h3 className="controls__group-title">
              <span className="controls__group-icon">🎮</span>
              Gamepad
            </h3>
            <div className="controls__table">
              {GAMEPAD_CONTROLS.map(row => (
                <div key={row.action} className="controls__row">
                  <span className="controls__action">{row.action}</span>
                  <div className="controls__keys">
                    {row.keys.map((k, i) => (
                      <React.Fragment key={k}>
                        <Key label={k} />
                        {i < row.keys.length - 1 && <span className="controls__or">or</span>}
                      </React.Fragment>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Special moves */}
        <div className="controls__special">
          <h3 className="controls__special-title">
            <span className="section-label" style={{ display: 'inline-block', marginBottom: '4px' }}>// special abilities</span>
            Combat Techniques
          </h3>
          <div className="controls__moves-grid">
            {COMBAT_MOVES.map(move => (
              <div
                key={move.move}
                className="controls__move"
                style={{ '--move-color': move.color }}
              >
                <span className="controls__move-icon">{move.icon}</span>
                <div className="controls__move-info">
                  <span className="controls__move-name">{move.move}</span>
                  <span className="controls__move-input">{move.input}</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Quick tip */}
        <div className="controls__tip">
          <span className="controls__tip-icon">💡</span>
          <div>
            <strong>Training Protocol:</strong> Start with Level 00 — Protocol Zero to learn
            all controls in the Neural-Sync Virtual Reality before entering live combat.
          </div>
        </div>
      </div>
    </section>
  );
}
