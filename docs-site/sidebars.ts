import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

/**
 * Creating a sidebar enables you to:
 - create an ordered group of docs
 - render a sidebar for each doc of that group
 - provide next/previous navigation

 The sidebars can be generated from the filesystem, or explicitly defined here.

 Create as many sidebars as you want.
 */
const sidebars: SidebarsConfig = {
  mainSidebar: [
    'intro',
    'overview',
    'getting-started',
    {
      type: 'category',
      label: 'Architecture',
      items: [
        'architecture/clean-architecture',
        'architecture/modules',
        'architecture/di-and-configuration',
      ],
    },
    {
      type: 'category',
      label: 'API',
      items: [
        'api/endpoints',
        'api/dto',
      ],
    },
    {
      type: 'category',
      label: 'Data & Caching',
      items: [
        'data-model/entities',
        'data-model/repository-pattern',
        'caching/patterns',
      ],
    },
    {
      type: 'category',
      label: 'Monitoring',
      items: [
        'monitoring/observability',
      ],
    },
    {
      type: 'category',
      label: 'Development',
      items: [
        'development/local-setup',
        'development/docker-compose',
        'development/migrations',
        'development/error-handling',
      ],
    },
    'roadmap',
    'contributing',
  ],
};

export default sidebars;
