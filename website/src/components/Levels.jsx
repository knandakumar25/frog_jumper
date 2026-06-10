import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import './Levels.css';

const LEVELS = [
  {
    id: 0,
    number: '00',
    key: 'levels.level_0',
    typeKey: 'levels.types.training',
    threatKey: 'levels.threats.minimal',
    typeColor: 'var(--color-green)',
    difficulty: 1,
    bgClass: 'level-bg--dojo',
  },
  {
    id: 1,
    number: '01',
    key: 'levels.level_1',
    typeKey: 'levels.types.infiltration',
    threatKey: 'levels.threats.high',
    typeColor: 'var(--color-primary)',
    difficulty: 2,
    bgClass: 'level-bg--cyber',
  },
  {
    id: 2,
    number: '02',
    key: 'levels.level_2',
    typeKey: 'levels.types.defense',
    threatKey: 'levels.threats.omega',
    typeColor: 'var(--color-accent)',
    difficulty: 3,
    bgClass: 'level-bg--story',
  },
  {
    id: 3,
    number: '03',
    key: 'levels.level_3',
    typeKey: 'levels.types.stealth',
    threatKey: 'levels.threats.high',
    typeColor: 'var(--color-secondary)',
    difficulty: 3,
    bgClass: 'level-bg--cyber',
  },
  {
    id: 4,
    number: '04',
    key: 'levels.level_4',
    typeKey: 'levels.types.assault',
    threatKey: 'levels.threats.critical',
    typeColor: 'var(--color-gold)',
    difficulty: 4,
    bgClass: 'level-bg--story',
  },
];

function DifficultyPips({ level }) {
  return (
    <div className="difficulty-pips">
      {[1, 2, 3, 4].map(i => (
        <span
          key={i}
          className={`difficulty-pip ${i <= level ? 'difficulty-pip--active' : ''}`}
        />
      ))}
    </div>
  );
}

export default function Levels({ bgCyber, bgDojo, bgStory, hideSectionHeader }) {
  const { t } = useTranslation();
  const [activeLevel, setActiveLevel] = useState(0);
  const level = LEVELS[activeLevel];

  const bgMap = { 'level-bg--cyber': bgCyber, 'level-bg--dojo': bgDojo, 'level-bg--story': bgStory };

  return (
    <section className="levels section noise-bg" id="levels">
      <div className="container">
        {!hideSectionHeader && (
          <div className="levels__header">
            <span className="section-label">// {t('levels.briefing')}</span>
            <h2 className="section-title">
              {t('levels.title_prefix')}<span className="text-gradient-primary">{t('levels.title_highlight')}</span>{t('levels.title_suffix')}
            </h2>
            <div className="divider" />
            <p className="section-description">
              {t('levels.desc')}
            </p>
          </div>
        )}

        <div className="levels__layout">
          {/* Level selector */}
          <div className="levels__selector">
            {LEVELS.map((lv, i) => (
              <button
                key={lv.id}
                id={`level-select-${lv.id}`}
                className={`levels__select-item ${i === activeLevel ? 'levels__select-item--active' : ''}`}
                onClick={() => setActiveLevel(i)}
                style={i === activeLevel ? { '--lv-color': lv.typeColor } : {}}
              >
                <span className="levels__select-num">{lv.number}</span>
                <div className="levels__select-info">
                  <span className="levels__select-name">{t(`${lv.key}.name`)}</span>
                  <span
                    className="levels__select-type"
                    style={i === activeLevel ? { color: lv.typeColor } : {}}
                  >
                    {t(lv.typeKey)}
                  </span>
                </div>
                <div className="levels__select-arrow">{t('levels.select_arrow')}</div>
              </button>
            ))}
          </div>

          {/* Level detail */}
          <div className="levels__detail" key={level.id}>
            {/* Level background image */}
            <div className="levels__detail-bg">
              <img
                src={bgMap[level.bgClass]}
                alt={t(`${level.key}.name`)}
                className="levels__detail-img"
              />
              <div className="levels__detail-overlay" />
              <div className="levels__detail-header">
                <div className="levels__detail-badges">
                  <span className="levels__detail-num">{t('nav.levels').toUpperCase().slice(0, -1)} {level.number}</span>
                  <span
                    className="levels__detail-type"
                    style={{ color: level.typeColor, borderColor: `${level.typeColor}50` }}
                  >
                    {t(level.typeKey)}
                  </span>
                </div>
                <div className="levels__detail-threat">
                  <span className="levels__detail-threat-label">{t('levels.threat_title')}</span>
                  <span
                    className="levels__detail-threat-value"
                    style={{
                      color: level.threatKey === 'levels.threats.minimal' ? 'var(--color-green)'
                        : level.threatKey === 'levels.threats.high' ? 'var(--color-gold)'
                        : 'var(--color-accent)'
                    }}
                  >
                    {t(level.threatKey)}
                  </span>
                </div>
              </div>
            </div>

            {/* Level info */}
            <div className="levels__detail-body">
              <div className="levels__detail-title-row">
                <div>
                  <h3 className="levels__detail-title">{t(`${level.key}.name`)}</h3>
                  <p className="levels__detail-subtitle">{t(`${level.key}.subtitle`)}</p>
                </div>
                <div className="levels__detail-difficulty">
                  <span className="levels__difficulty-label">{t('levels.difficulty_title')}</span>
                  <DifficultyPips level={level.difficulty} />
                </div>
              </div>

              <p className="levels__detail-desc">{t(`${level.key}.description`)}</p>

              <div className="levels__objectives">
                <h4 className="levels__objectives-title">{t('levels.objectives_title')}</h4>
                {Array.from({ length: 3 }).map((_, i) => (
                  <div key={i} className="levels__objective">
                    <span
                      className="levels__objective-num"
                      style={{ color: level.typeColor, background: `${level.typeColor}15` }}
                    >
                      {String(i + 1).padStart(2, '0')}
                    </span>
                    <span className="levels__objective-text">{t(`${level.key}.obj_${i}`)}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

