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
        'advanced/nativeaot-and-trimming',
        'advanced/testing-and-validation',
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
