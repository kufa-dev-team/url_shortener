import React from 'react';
import clsx from 'clsx';
import styles from './styles.module.css';

type CalloutType = 'info' | 'tip' | 'warning' | 'danger' | 'success';

interface CalloutBoxProps {
  type?: CalloutType;
  title?: string;
  children: React.ReactNode;
  icon?: string;
}

const typeConfig = {
  info: { icon: '💡', defaultTitle: 'Info' },
  tip: { icon: '✨', defaultTitle: 'Tip' },
  warning: { icon: '⚠️', defaultTitle: 'Warning' },
  danger: { icon: '🚨', defaultTitle: 'Danger' },
  success: { icon: '✅', defaultTitle: 'Success' },
};

export default function CalloutBox({ 
  type = 'info', 
  title, 
  children, 
  icon 
}: CalloutBoxProps): React.ReactElement {
  const config = typeConfig[type];
  const displayIcon = icon || config.icon;
  const displayTitle = title || config.defaultTitle;

  return (
    <div className={clsx(styles.calloutBox, styles[`calloutBox--${type}`])}>
      <div className={styles.calloutHeader}>
        <span className={styles.calloutIcon}>{displayIcon}</span>
        <span className={styles.calloutTitle}>{displayTitle}</span>
      </div>
      <div className={styles.calloutContent}>
        {children}
      </div>
    </div>
  );
}
