// @ts-check

/** @type {import('@docusaurus/plugin-content-docs').SidebarsConfig} */
const sidebars = {
  tutorialSidebar: [
    {
      type: 'doc',
      id: 'intro',
      label: 'Introduction',
    },
    {
      type: 'category',
      label: 'Getting Started',
      items: [
        'getting-started/installation',
        'getting-started/quick-start',
      ],
    },
    {
      type: 'category',
      label: 'Core Concepts',
      items: [
        'core-concepts/attributes',
        'core-concepts/service-registration',
      ],
    },
    {
      type: 'category',
      label: 'Advanced',
      items: [
        'advanced/phase6-delivery-status',
        'advanced/nativeaot-and-trimming',
        'advanced/platform-and-framework-support',
        'advanced/benchmarks',
        'advanced/testing-and-validation',
        'advanced/analyzer-diagnostics',
        'advanced/registration-model-rm01-rm12',
      ],
    },
    {
      type: 'category',
      label: 'Community',
      items: [
        'community/contributing',
        'community/roadmap',
      ],
    },
  ],
};

export default sidebars;
