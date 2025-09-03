import React from 'react';
import Link from '@docusaurus/Link';
import clsx from 'clsx';
import styles from './styles.module.css';

interface DocCardProps {
  title: string;
  description: string;
  href: string;
  icon?: string;
  color?: string;
  badge?: string;
}

export default function DocCard({
  title,
  description,
  href,
  icon = '📄',
  color = 'var(--modern-primary)',
  badge
}: DocCardProps): React.ReactElement {
  return (
    <Link href={href} className={styles.cardLink}>
      <div 
        className={clsx(styles.docCard, 'animate-fade-in-up')}
        style={{ '--card-color': color } as React.CSSProperties}
      >
        <div className={styles.cardGlow}></div>
        <div className={styles.cardContent}>
          <div className={styles.cardHeader}>
            <span className={styles.cardIcon}>{icon}</span>
            {badge && <span className={styles.cardBadge}>{badge}</span>}
          </div>
          <h3 className={styles.cardTitle}>{title}</h3>
          <p className={styles.cardDescription}>{description}</p>
          <div className={styles.cardArrow}>→</div>
        </div>
      </div>
    </Link>
  );
}

interface DocCardGridProps {
  children: React.ReactNode;
}

export function DocCardGrid({ children }: DocCardGridProps): React.ReactElement {
  return (
    <div className={styles.docCardGrid}>
      {children}
    </div>
  );
}
