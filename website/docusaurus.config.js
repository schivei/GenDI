import {themes as prismThemes} from 'prism-react-renderer';

const config = {
  title: 'GenDI',
  tagline: 'Attribute-first Dependency Injection source generation for NativeAOT-ready .NET applications',
  favicon: 'img/favicon.ico',
  url: 'https://schivei.github.io',
  baseUrl: '/GenDI/',
  organizationName: 'schivei',
  projectName: 'GenDI',
  onBrokenLinks: 'warn',
  markdown: {
    hooks: {
      onBrokenMarkdownLinks: 'warn',
    },
  },
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },
  presets: [
    [
      'classic',
      ({
        docs: {
          sidebarPath: './sidebars.js',
          editUrl: 'https://github.com/schivei/GenDI/tree/main/website/',
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      }),
    ],
  ],
  themeConfig: ({
    navbar: {
      title: 'GenDI',
      logo: {
        alt: 'GenDI Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'tutorialSidebar',
          position: 'left',
          label: 'Documentation',
        },
        {
          href: 'https://github.com/schivei/GenDI',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            { label: 'Introduction', to: '/docs/intro' },
            { label: 'Getting Started', to: '/docs/getting-started/installation' },
            { label: 'Attribute Reference', to: '/docs/core-concepts/attributes' },
          ],
        },
        {
          title: 'Community',
          items: [
            { label: 'GitHub', href: 'https://github.com/schivei/GenDI' },
            { label: 'Issues', href: 'https://github.com/schivei/GenDI/issues' },
            { label: 'Discussions', href: 'https://github.com/schivei/GenDI/discussions' },
          ],
        },
        {
          title: 'More',
          items: [
            { label: 'Contributing', href: 'https://github.com/schivei/GenDI/blob/main/CONTRIBUTING.md' },
            { label: 'Roadmap', href: 'https://github.com/schivei/GenDI/blob/main/ROADMAP.md' },
            { label: 'License', href: 'https://github.com/schivei/GenDI/blob/main/LICENSE' },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} GenDI. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp', 'bash', 'powershell', 'json'],
    },
    colorMode: {
      defaultMode: 'light',
      disableSwitch: false,
      respectPrefersColorScheme: true,
    },
    announcementBar: {
      id: 'announcement',
      content:
        '⭐️ If you use GenDI in production, share your feedback in <a target="_blank" rel="noopener noreferrer" href="https://github.com/schivei/GenDI/discussions">GitHub Discussions</a>! ⭐️',
      backgroundColor: '#20232a',
      textColor: '#fff',
      isCloseable: true,
    },
  }),
};

export default config;
