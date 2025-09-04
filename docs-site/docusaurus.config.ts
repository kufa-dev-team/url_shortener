import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

// Optional/extra plugins with guarded requires so dev doesn't break if not installed yet
const plugins: any[] = [];

// Local search (fast, no Algolia account required)
if (process.env.DOCS_ENABLE_LOCAL_SEARCH === 'true') {
  try {
    plugins.push([
      require.resolve('@easyops-cn/docusaurus-search-local'),
      {
        hashed: true,
        indexDocs: true,
        language: ['en'],
        highlightSearchTermsOnTargetPage: true,
        searchContextByPaths: ['docs'],
      },
    ]);
  } catch (e) {
    // eslint-disable-next-line no-console
    console.warn('[docs] Local search plugin not installed; set DOCS_ENABLE_LOCAL_SEARCH=true after `yarn`.');
  }
}

// Responsive images with blur-up placeholders
try {
  plugins.push(require.resolve('@docusaurus/plugin-ideal-image'));
} catch (e) {
  // eslint-disable-next-line no-console
  console.warn('[docs] Ideal Image plugin not installed; responsive image optimization disabled.');
}

// Progressive Web App (offline caching, installable)
try {
  plugins.push([
    require.resolve('@docusaurus/plugin-pwa'),
    {
      debug: false,
      offlineModeActivationStrategies: ['appInstalled', 'standalone', 'queryString'],
      swRegister: false,
      pwaHead: [
        {tagName: 'link', rel: 'icon', href: '/img/favicon.ico'},
        {tagName: 'link', rel: 'manifest', href: '/manifest.json'},
        {tagName: 'meta', name: 'theme-color', content: '#0b3a6a'},
        {tagName: 'meta', name: 'apple-mobile-web-app-capable', content: 'yes'},
        {tagName: 'meta', name: 'apple-mobile-web-app-status-bar-style', content: 'black-translucent'},
      ],
    },
  ]);
} catch (e) {
  // eslint-disable-next-line no-console
  console.warn('[docs] PWA plugin not installed; offline support disabled.');
}

const config: Config = {
  title: 'URL Shortener',
  tagline: 'High-performance ASP.NET Core URL shortener with advanced caching',
  favicon: 'img/logo.webp',

  // Future flags, see https://docusaurus.io/docs/api/docusaurus-config#future
  future: {
    v4: true, // Improve compatibility with the upcoming Docusaurus v4
  },

  // Set the production url of your site here
  url: 'https://short.ly',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'kufa-dev-team', // Your GitHub org/user name.
  projectName: 'url_shortener', // Your repo name.

  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          editUrl:
            'https://github.com/kufa-dev-team/url_shortener/edit/develop/docs-site/',
        },
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],
  // Enable Markdown features
  markdown: { mermaid: true },
  // Add Mermaid theme
  themes: ['@docusaurus/theme-mermaid'],
  // Plugins
  plugins,

  themeConfig: {
    // Replace with your project's social card
    image: 'img/docusaurus-social-card.jpg',
    navbar: {
      hideOnScroll: true,
      title: 'URL Shortener',
      logo: {
        alt: 'URL Shortener Logo',
        src: 'img/logo.webp',
        width: 40,
        height: 40,
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'mainSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          href: 'https://github.com/kufa-dev-team/url_shortener',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    announcementBar: {
      id: 'project_status',
      content:
        'This documentation is under active development. Have feedback? Open an issue on <a href="https://github.com/kufa-dev-team/url_shortener" target="_blank" rel="noopener noreferrer">GitHub</a>.',
      backgroundColor: '#0b3a6a',
      textColor: '#ffffff',
      isCloseable: true,
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            {
              label: 'Overview',
              to: '/docs/overview',
            },
            {
              label: 'Getting Started',
              to: '/docs/getting-started',
            },
            {
              label: 'API',
              to: '/docs/api/endpoints',
            },
          ],
        },
        {
          title: 'Community',
          items: [
            {
              label: 'GitHub Discussions',
              href: 'https://github.com/kufa-dev-team/url_shortener/discussions',
            },
            {
              label: 'Stack Overflow',
              href: 'https://stackoverflow.com/questions/tagged/asp.net-core',
            },
          ],
        },
        {
          title: 'More',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/kufa-dev-team/url_shortener',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} kufa-dev-team. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp', 'docker', 'json'],
    },
    colorMode: {
      defaultMode: 'dark',
      disableSwitch: false,
      respectPrefersColorScheme: true,
    },
  tableOfContents: { minHeadingLevel: 2, maxHeadingLevel: 4 },
    mermaid: {
      theme: {light: 'neutral', dark: 'dark'},
    },
    docs: {
      sidebar: {
        hideable: true,
        autoCollapseCategories: true,
      },
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
