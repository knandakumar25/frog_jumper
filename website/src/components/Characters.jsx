import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import './Characters.css';

const CHARACTERS = [
  {
    id: 'ninja-frog',
    name: 'Ninja Frog',
    codename: 'SHADOW-1',
    roleKey: 'characters.warriors.frog.role',
    color: '#39d353',
    glowColor: 'rgba(57, 211, 83, 0.5)',
    descKey: 'characters.warriors.frog.desc',
    abilities: ['Wall Jump', 'Double Jump', 'Sprint', 'Shadow Strike'],
    stats: { speed: 95, power: 60, agility: 99, defense: 45 },
    spriteFile: 'Ninja Frog',
    loreKey: 'characters.warriors.frog.lore',
  },
  {
    id: 'mask-dude',
    name: 'Mask Dude',
    codename: 'PHANTOM-2',
    roleKey: 'characters.warriors.dude.role',
    color: '#00d4ff',
    glowColor: 'rgba(0, 212, 255, 0.5)',
    descKey: 'characters.warriors.dude.desc',
    abilities: ['Balanced Attack', 'Shield Block', 'Ground Slam', 'Recovery Jump'],
    stats: { speed: 75, power: 75, agility: 75, defense: 75 },
    spriteFile: 'Mask Dude',
    loreKey: 'characters.warriors.dude.lore',
  },
  {
    id: 'pink-man',
    name: 'Pink Man',
    codename: 'NOVA-3',
    roleKey: 'characters.warriors.pink.role',
    color: '#ff69b4',
    glowColor: 'rgba(255, 105, 180, 0.5)',
    descKey: 'characters.warriors.pink.desc',
    abilities: ['Power Strike', 'Charge Attack', 'Heavy Stomp', 'Rage Mode'],
    stats: { speed: 55, power: 99, agility: 50, defense: 85 },
    spriteFile: 'Pink Man',
    loreKey: 'characters.warriors.pink.lore',
  },
  {
    id: 'virtual-guy',
    name: 'Virtual Guy',
    codename: 'MATRIX-4',
    roleKey: 'characters.warriors.guy.role',
    color: '#9b4dca',
    glowColor: 'rgba(155, 77, 202, 0.5)',
    descKey: 'characters.warriors.guy.desc',
    abilities: ['EMP Burst', 'Hack Interface', 'Phase Shift', 'Data Siphon'],
    stats: { speed: 80, power: 65, agility: 88, defense: 55 },
    spriteFile: 'Virtual Guy',
    loreKey: 'characters.warriors.guy.lore',
  },
];

function StatBar({ label, value, color }) {
  return (
    <div className="stat-bar">
      <span className="stat-bar__label">{label}</span>
      <div className="stat-bar__track">
        <div
          className="stat-bar__fill"
          style={{
            width: `${value}%`,
            background: color,
            boxShadow: `0 0 8px ${color}`,
          }}
        />
      </div>
      <span className="stat-bar__value">{value}</span>
    </div>
  );
}

export default function Characters({ sprites, hideSectionHeader }) {
  const [activeChar, setActiveChar] = useState(0);
  const char = CHARACTERS[activeChar];
  const { t } = useTranslation();

  return (
    <section className="characters section" id="characters">
      <div className="container">
        {!hideSectionHeader && (
          <div className="characters__header">
            <span className="section-label">// {t('characters.briefing')}</span>
            <h2 className="section-title">
              Choose Your <span className="text-gradient-primary">{t('characters.title')}</span>
            </h2>
            <div className="divider" />
            <p className="section-description">
              {t('characters.desc')}
            </p>
          </div>
        )}

        {/* Selector tabs */}
        <div className="characters__tabs">
          {CHARACTERS.map((c, i) => (
            <button
              key={c.id}
              id={`char-tab-${c.id}`}
              className={`characters__tab ${i === activeChar ? 'characters__tab--active' : ''}`}
              style={i === activeChar ? { borderColor: c.color, color: c.color } : {}}
              onClick={() => setActiveChar(i)}
            >
              <span
                className="characters__tab-dot"
                style={i === activeChar ? { background: c.color, boxShadow: `0 0 8px ${c.color}` } : {}}
              />
              {c.name}
            </button>
          ))}
        </div>

        {/* Character card */}
        <div className="characters__display" key={char.id}>
          <div className="characters__sprite-panel">
            <div
              className="characters__sprite-bg"
              style={{ '--char-color': char.color, '--char-glow': char.glowColor }}
            >
              {/* Sprite animation */}
              <div className="characters__sprite-container">
                {sprites[char.spriteFile] ? (
                  <img
                    src={sprites[char.spriteFile]}
                    alt={`${char.name} idle sprite`}
                    className="characters__sprite"
                  />
                ) : (
                  <div className="characters__sprite-placeholder">{char.name[0]}</div>
                )}
              </div>
              {/* Glow rings */}
              <div className="characters__glow-ring" style={{ '--char-color': char.color }} />
              <div className="characters__glow-ring characters__glow-ring--2" style={{ '--char-color': char.color }} />
            </div>

            {/* Lore quote */}
            <div className="characters__lore" style={{ borderColor: `${char.color}40` }}>
              <span className="characters__lore-text">{t(char.loreKey)}</span>
            </div>
          </div>

          <div className="characters__info">
            <div className="characters__meta">
              <span
                className="characters__codename"
                style={{ color: char.color }}
              >
                ◈ {char.codename}
              </span>
              <span className="characters__role">{t(char.roleKey)}</span>
            </div>

            <h3 className="characters__name" style={{ textShadow: `0 0 30px ${char.glowColor}` }}>
              {char.name}
            </h3>

            <p className="characters__description">{t(char.descKey)}</p>

            <div className="characters__abilities">
              <h4 className="characters__abilities-title">{t('characters.abilities_title')}</h4>
              <div className="characters__ability-grid">
                {char.abilities.map(ab => (
                  <div
                    key={ab}
                    className="characters__ability"
                    style={{ borderColor: `${char.color}40`, color: char.color }}
                  >
                    <span className="characters__ability-icon" style={{ background: `${char.color}20` }}>⚡</span>
                    {ab}
                  </div>
                ))}
              </div>
            </div>

            <div className="characters__stats">
              <h4 className="characters__stats-title">{t('characters.stats_title')}</h4>
              {Object.entries(char.stats).map(([key, val]) => (
                <StatBar
                  key={key}
                  label={t(`characters.stats.${key.toLowerCase()}`)}
                  value={val}
                  color={char.color}
                />
              ))}
            </div>
          </div>
        </div>

        {/* Unlock note */}
        <div className="characters__unlock">
          <span className="characters__unlock-icon">🔐</span>
          <span>
            <strong>Cyber Shogun</strong> — {t('characters.unlock')}
          </span>
        </div>
      </div>
    </section>
  );
}
