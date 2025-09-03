import type {ReactNode} from 'react';
import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import Features from '@site/src/components/Features';
import Heading from '@theme/Heading';

import styles from './index.module.css';

function HomepageHeader() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <header className={clsx('hero', styles.heroBanner)}>
      {/* Floating background elements */}
      <div className={styles.heroFloatingElement}></div>
      <div className={styles.heroFloatingElement}></div>
      <div className={styles.heroFloatingElement}></div>
      
      <div className="container">
        <Heading as="h1" className={clsx("hero__title", "animate-fade-in-up")}>
          {siteConfig.title}
        </Heading>
        <p className={clsx("hero__subtitle", "animate-fade-in-up")} style={{animationDelay: '0.2s'}}>
          {siteConfig.tagline}
        </p>
        <div className={clsx(styles.buttons, "animate-fade-in-up")} style={{animationDelay: '0.4s'}}>
          <Link
            className="button button--secondary button--lg"
            to="/docs/overview">
            📖 Read the Overview
          </Link>
          <Link
            className="button button--outline button--lg"
            to="/docs/getting-started">
            🚀 Get Started
          </Link>
        </div>
        
        {/* Stats badges */}
        <div className={clsx("margin-top--xl", "animate-fade-in")} style={{animationDelay: '0.6s'}}>
          <div style={{
            display: 'flex',
            justifyContent: 'center',
            gap: '2rem',
            flexWrap: 'wrap',
            marginTop: '3rem'
          }}>
            <div style={{
              background: 'rgba(255, 255, 255, 0.1)',
              backdropFilter: 'blur(20px)',
              padding: '1rem 1.5rem',
              borderRadius: 'var(--modern-radius-lg)',
              border: '1px solid rgba(255, 255, 255, 0.2)',
              textAlign: 'center'
            }}>
              <div style={{fontSize: '1.5rem', fontWeight: '700', marginBottom: '0.25rem'}}>⚡</div>
              <div style={{fontSize: '0.9rem', opacity: 0.8}}>Lightning Fast</div>
            </div>
            <div style={{
              background: 'rgba(255, 255, 255, 0.1)',
              backdropFilter: 'blur(20px)',
              padding: '1rem 1.5rem',
              borderRadius: 'var(--modern-radius-lg)',
              border: '1px solid rgba(255, 255, 255, 0.2)',
              textAlign: 'center'
            }}>
              <div style={{fontSize: '1.5rem', fontWeight: '700', marginBottom: '0.25rem'}}>🔧</div>
              <div style={{fontSize: '0.9rem', opacity: 0.8}}>Clean Architecture</div>
            </div>
            <div style={{
              background: 'rgba(255, 255, 255, 0.1)',
              backdropFilter: 'blur(20px)',
              padding: '1rem 1.5rem',
              borderRadius: 'var(--modern-radius-lg)',
              border: '1px solid rgba(255, 255, 255, 0.2)',
              textAlign: 'center'
            }}>
              <div style={{fontSize: '1.5rem', fontWeight: '700', marginBottom: '0.25rem'}}>🚀</div>
              <div style={{fontSize: '0.9rem', opacity: 0.8}}>Production Ready</div>
            </div>
          </div>
        </div>
      </div>
    </header>
  );
}

export default function Home(): ReactNode {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout
      title={`${siteConfig.title} Documentation`}
      description="Production-ready ASP.NET Core URL shortener with clean architecture, caching patterns, and developer-friendly tooling.">
      <HomepageHeader />
      <main>
        <Features />
      </main>
    </Layout>
  );
}
