import { useTranslation } from 'react-i18next';
import './Features.css';

const FEATURES = [
  {
    icon: '👥',
    key: 'roster',
    color: 'var(--color-primary)',
  },
  {
    icon: '📜',
    key: 'story',
    color: 'var(--color-gold)',
  },
  {
    icon: '🍒',
    key: 'progress',
    color: 'var(--color-accent)',
  },
  {
    icon: '💾',
    key: 'save',
    color: 'var(--color-green)',
  },
  {
    icon: '🌍',
    key: 'local',
    color: 'var(--color-secondary)',
  },
  {
    icon: '🎮',
    key: 'controls',
    color: 'var(--color-primary)',
  },
  {
    icon: '⏸️',
    key: 'ux',
    color: 'var(--color-gold)',
  },
  {
    icon: '🎵',
    key: 'audio',
    color: 'var(--color-green)',
  },
];

const TECH = [
  { name: 'Godot 4.6', descKey: 'engine' },
  { name: 'C# / .NET 8', descKey: 'logic' },
  { name: 'GDScript', descKey: 'scripting' },
  { name: 'SQLite', descKey: 'persistence' },
  { name: 'Ink Runtime', descKey: 'narrative_engine' },
  { name: 'MSBuild', descKey: 'build' },
];

export default function Features({ hideSectionHeader }) {
  const { t } = useTranslation();

  return (
    <section className="features section" id="features">
      <div className="container">
        {!hideSectionHeader && (
          <div className="features__header">
            <span className="section-label">// {t('features.briefing')}</span>
            <h2 className="section-title">
              {t('features.title_prefix')}<span className="text-gradient-primary">{t('features.title_highlight')}</span>
            </h2>
            <div className="divider" />
          </div>
        )}

        <div className="features__grid">
          {FEATURES.map((feat) => (
            <div
              key={feat.key}
              className="features__card card"
              style={{ '--feat-color': feat.color }}
            >
              <div className="features__card-top">
                <span className="features__icon">{feat.icon}</span>
                <span
                  className="features__tag"
                  style={{ color: feat.color, borderColor: `${feat.color}40` }}
                >
                  {t(`features.tags.${feat.key}`)}
                </span>
              </div>
              <h3 className="features__title">{t(`features.items.${feat.key}.title`)}</h3>
              <p className="features__desc">{t(`features.items.${feat.key}.desc`)}</p>
            </div>
          ))}
        </div>

        {/* Tech Stack */}
        <div className="features__tech">
          <h3 className="features__tech-title">
            <span className="section-label" style={{ fontSize: '0.7rem' }}>// {t('features.built_with')}</span>
            {t('features.tech_title')}
          </h3>
          <div className="features__tech-grid">
            {TECH.map((tItem) => (
              <div key={tItem.name} className="features__tech-item">
                <div className="features__tech-dot" />
                <div>
                  <div className="features__tech-name">{tItem.name}</div>
                  <div className="features__tech-desc">{t(`features.tech.${tItem.descKey}`)}</div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}

