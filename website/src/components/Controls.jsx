import { Fragment } from 'react';
import { useTranslation } from 'react-i18next';
import './Controls.css';

const KEYBOARD_CONTROLS = [
  { actionKey: 'move_left', keys: ['A', '←'] },
  { actionKey: 'move_right', keys: ['D', '→'] },
  { actionKey: 'jump', keys: ['W', '↑', 'Space'] },
  { actionKey: 'sword_attack', keys: ['LMB'], noteKey: 'shogun_only' },
  { actionKey: 'karate_attack', keys: ['Q'], noteKey: 'shogun_only' },
  { actionKey: 'pause', keys: ['Esc'] },
];

const GAMEPAD_CONTROLS = [
  { actionKey: 'move_left', keys: ['D-Pad ←', 'L-Stick'] },
  { actionKey: 'move_right', keys: ['D-Pad →', 'L-Stick'] },
  { actionKey: 'jump', keys: ['A / ✕'] },
  { actionKey: 'pause', keys: ['Start'] },
];

const COMBAT_MOVES = [
  { moveKey: 'plasma_blade', icon: '⚔️', color: 'var(--color-primary)' },
  { moveKey: 'energy_shield', icon: '🛡️', color: 'var(--color-secondary)' },
  { moveKey: 'kinetic_overload', icon: '💥', color: 'var(--color-accent)' },
  { moveKey: 'phase_shift', icon: '👻', color: 'var(--color-gold)' },
  { moveKey: 'drone_mark', icon: '🚁', color: 'var(--color-green)' },
  { moveKey: 'emp_burst', icon: '⚡', color: 'var(--color-secondary)' },
  { moveKey: 'shield_overload', icon: '🌟', color: 'var(--color-gold)' },
  { moveKey: 'orbital_strike', icon: '☄️', color: 'var(--color-accent)' },
];

function Key({ label }) {
  return <kbd className="key">{label}</kbd>;
}

export default function Controls({ hideSectionHeader }) {
  const { t } = useTranslation();

  return (
    <section className="controls section grid-bg" id="controls">
      <div className="container">
        {!hideSectionHeader && (
          <div className="controls__header">
            <span className="section-label">// {t('controls.briefing')}</span>
            <h2 className="section-title">
              {t('controls.title_prefix')}<span className="text-gradient-primary">{t('controls.title_highlight')}</span>
            </h2>
            <div className="divider" />
            <p className="section-description">
              {t('controls.desc')}
            </p>
          </div>
        )}

        <div className="controls__layout">
          {/* Keyboard */}
          <div className="controls__group">
            <h3 className="controls__group-title">
              <span className="controls__group-icon">⌨️</span>
              {t('controls.keyboard')}
            </h3>
            <div className="controls__table">
              {KEYBOARD_CONTROLS.map(row => (
                <div key={row.actionKey} className="controls__row">
                  <span className="controls__action">
                    {t(`controls.actions.${row.actionKey}`)}
                    {row.noteKey && <span className="controls__note">{t(`controls.notes.${row.noteKey}`)}</span>}
                  </span>
                  <div className="controls__keys">
                    {row.keys.map((k, i) => (
                      <Fragment key={k}>
                        <Key label={k} />
                        {i < row.keys.length - 1 && <span className="controls__or">{t('controls.or')}</span>}
                      </Fragment>
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
              {t('controls.gamepad')}
            </h3>
            <div className="controls__table">
              {GAMEPAD_CONTROLS.map(row => (
                <div key={row.actionKey} className="controls__row">
                  <span className="controls__action">{t(`controls.actions.${row.actionKey}`)}</span>
                  <div className="controls__keys">
                    {row.keys.map((k, i) => (
                      <Fragment key={k}>
                        <Key label={k} />
                        {i < row.keys.length - 1 && <span className="controls__or">{t('controls.or')}</span>}
                      </Fragment>
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
            <span className="section-label" style={{ display: 'inline-block', marginBottom: '4px' }}>// {t('controls.special_briefing')}</span>
            {t('controls.special_title')}
          </h3>
          <div className="controls__moves-grid">
            {COMBAT_MOVES.map(move => (
              <div
                key={move.moveKey}
                className="controls__move"
                style={{ '--move-color': move.color }}
              >
                <span className="controls__move-icon">{move.icon}</span>
                <div className="controls__move-info">
                  <span className="controls__move-name">{t(`controls.moves.${move.moveKey}.name`)}</span>
                  <span className="controls__move-input">{t(`controls.moves.${move.moveKey}.input`)}</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Quick tip */}
        <div className="controls__tip">
          <span className="controls__tip-icon">💡</span>
          <div>
            <strong>{t('controls.tip.title')}</strong> {t('controls.tip.text')}
          </div>
        </div>
      </div>
    </section>
  );
}

