import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import './Characters.css';

const CHARACTERS = [
  {
    id: 'cyber-shogun',
    name: 'Cyber Shogun',
    civilianName: 'Kaito Kurogane',
    codename: 'SHOGUN-1',
    roleKey: 'characters.warriors.shogun.role',
    color: '#00e5ff',
    glowColor: 'rgba(0, 229, 255, 0.5)',
    descKey: 'characters.warriors.shogun.desc',
    abilities: ['Energy Katana', 'Bushido Strike', 'Phase Dash', 'Cyber Armor'],
    stats: { speed: 88, power: 92, agility: 85, defense: 90 },
    spriteFile: 'Cyber Shogun',
    loreKey: 'characters.warriors.shogun.lore',
  },
  {
    id: 'oceanus',
    name: 'Oceanus',
    civilianName: 'Kiana Vaea',
    codename: 'OCEANUS-3',
    roleKey: 'characters.warriors.oceanus.role',
    color: '#00b4d8',
    glowColor: 'rgba(0, 180, 216, 0.5)',
    descKey: 'characters.warriors.oceanus.desc',
    abilities: ['Hydro Jet', 'Tidal Wave', 'Pressure Lock', 'Liquid Form'],
    stats: { speed: 87, power: 78, agility: 91, defense: 74 },
    spriteFile: 'Oceanus',
    loreKey: 'characters.warriors.oceanus.lore',
  },
  {
    id: 'surya-prime',
    name: 'Surya Prime',
    civilianName: 'Arjun Varma',
    codename: 'SURYA-4',
    roleKey: 'characters.warriors.surya.role',
    color: '#ffd60a',
    glowColor: 'rgba(255, 214, 10, 0.5)',
    descKey: 'characters.warriors.surya.desc',
    abilities: ['Solar Pulse', 'Cosmic Slam', 'Gravity Field', 'Star Forge'],
    stats: { speed: 75, power: 99, agility: 70, defense: 88 },
    spriteFile: 'Surya Prime',
    loreKey: 'characters.warriors.surya.lore',
  },
  {
    id: 'mirage-blade',
    name: 'Mirage Blade',
    civilianName: 'Zayd Al-Mansoor (Saudi Arabia)',
    codename: 'MIRAGE-5',
    roleKey: 'characters.warriors.mirage.role',
    color: '#00f5d4',
    glowColor: 'rgba(0, 245, 212, 0.5)',
    descKey: 'characters.warriors.mirage.desc',
    abilities: ['Dual Sayf Slash', 'Crystal Phantom', 'Light Bend', 'Mirror Strike'],
    stats: { speed: 96, power: 82, agility: 98, defense: 58 },
    spriteFile: 'Mirage Blade',
    loreKey: 'characters.warriors.mirage.lore',
  },
  {
    id: 'apex-titan',
    name: 'Apex Titan',
    civilianName: 'King Tariq Nzinga',
    codename: 'APEX-6',
    roleKey: 'characters.warriors.apex.role',
    color: '#ff4d4d',
    glowColor: 'rgba(255, 77, 77, 0.5)',
    descKey: 'characters.warriors.apex.desc',
    abilities: ['Storm Leap', 'EM Shield', 'Thunder Crash', 'Lightning Gauntlet'],
    stats: { speed: 93, power: 95, agility: 94, defense: 85 },
    spriteFile: 'Apex Titan',
    loreKey: 'characters.warriors.apex.lore',
  },
  {
    id: 'aura-phoenicia',
    name: 'Aura Phoenicia',
    civilianName: 'Maya Khoury (Lebanon)',
    codename: 'PHOENICIA-7',
    roleKey: 'characters.warriors.phoenicia.role',
    color: '#9d4edd',
    glowColor: 'rgba(157, 78, 221, 0.5)',
    descKey: 'characters.warriors.phoenicia.desc',
    abilities: ['Harmonic Pulse', 'Prism Reverberation', 'Wave Glide', "Cedars' Echo"],
    stats: { speed: 96, power: 84, agility: 98, defense: 78 },
    spriteFile: 'Aura Phoenicia',
    loreKey: 'characters.warriors.phoenicia.lore',
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
              <span className="text-gradient-primary">{t('characters.title', 'Choose Your Fighter')}</span>
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
            {char.civilianName && (
              <div className="characters__civilian-name" style={{ color: `${char.color}99`, fontSize: '0.72rem', letterSpacing: '0.12em', marginTop: '-0.25rem', marginBottom: '0.25rem', fontFamily: 'monospace' }}>
                ID: {char.civilianName}
              </div>
            )}

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

        {/* Operative count note */}
        <div className="characters__unlock">
          <span className="characters__unlock-icon">🛡️</span>
          <span>
            <strong>6 Operatives</strong> — {t('characters.desc')}
          </span>
        </div>
      </div>
    </section>
  );
}
