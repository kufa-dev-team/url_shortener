import React from 'react';
import styles from './styles.module.css';

const items = [
  {
    title: 'Clean Architecture',
    description: 'Layered design with Domain, Application, Infrastructure, and API.',
  },
  {
    title: 'Fast & Reliable',
    description: 'Optimized EF Core access and Redis caching patterns.',
  },
  {
    title: 'Developer Friendly',
    description: 'Dockerized services, rich docs, and typed DTOs.',
  },
];

export default function Features(): React.ReactElement {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {items.map((i) => (
            <div key={i.title} className="col col--4">
              <div className={`card ${styles.card}`}>
                <div className="card__header"><h3>{i.title}</h3></div>
                <div className="card__body"><p>{i.description}</p></div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
