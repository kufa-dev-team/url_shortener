import React from 'react';
import clsx from 'clsx';
import styles from './styles.module.css';

// Modern SVG Icons as React Components
const ArchitectureIcon = () => (
  <svg width="40" height="40" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
    <path d="M12 2L2 7L12 12L22 7L12 2Z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
    <path d="M2 17L12 22L22 17" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
    <path d="M2 12L12 17L22 12" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
  </svg>
);

const SpeedIcon = () => (
  <svg width="40" height="40" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
    <path d="M13 2L3 14H12L11 22L21 10H12L13 2Z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
  </svg>
);

const DeveloperIcon = () => (
  <svg width="40" height="40" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
    <path d="M16 18L22 12L16 6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
    <path d="M8 6L2 12L8 18" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
  </svg>
);

const SecurityIcon = () => (
  <svg width="40" height="40" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
    <path d="M12 22S8 18 8 13V6L12 4L16 6V13C16 18 12 22 12 22Z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
  </svg>
);

const ScalabilityIcon = () => (
  <svg width="40" height="40" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
    <path d="M21 16V8A2 2 0 0 0 19 6H5A2 2 0 0 0 3 8V16A2 2 0 0 0 5 18H19A2 2 0 0 0 21 16Z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
    <path d="M7 10H17" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
    <path d="M7 14H13" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
  </svg>
);

const MonitoringIcon = () => (
  <svg width="40" height="40" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
    <path d="M3 3V21H21" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
    <path d="M9 9L12 6L16 10L20 6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
  </svg>
);

const features = [
  {
    title: 'Clean Architecture',
    description: 'Learn how to structure .NET applications using layered architecture principles.',
    icon: <ArchitectureIcon />,
    color: 'var(--modern-primary)'
  },
  {
    title: 'Modern .NET',
    description: 'Built with ASP.NET Core, Entity Framework, and modern C# features.',
    icon: <SpeedIcon />,
    color: 'var(--modern-accent)'
  },
  {
    title: 'Best Practices',
    description: 'Demonstrates industry best practices for web API development.',
    icon: <DeveloperIcon />,
    color: 'var(--modern-secondary)'
  }
];

export default function Features(): React.ReactElement {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className={styles.featuresHeader}>
          <h2 className={styles.featuresTitle}>What You'll Learn</h2>
          <p className={styles.featuresSubtitle}>
            An educational project demonstrating modern .NET development practices
          </p>
        </div>
        
        <div className={styles.featuresGrid}>
          {features.map((feature, index) => (
            <div 
              key={feature.title} 
              className={clsx(styles.featureCard, 'animate-fade-in-up')}
              style={{
                animationDelay: `${index * 0.15}s`,
                '--feature-color': feature.color
              } as React.CSSProperties & { '--feature-color': string }}
            >
              <div className={styles.featureIcon}>
                {feature.icon}
              </div>
              <div className={styles.featureContent}>
                <h3 className={styles.featureTitle}>{feature.title}</h3>
                <p className={styles.featureDescription}>{feature.description}</p>
              </div>
              <div className={styles.featureGlow}></div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
