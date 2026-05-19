import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';
import PropTypes from 'prop-types';

import styles from './index.module.css';
import benchmarkSalesPitch from '../data/benchmarkSalesPitch';

function HomepageHeader() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <header className={clsx('hero hero--primary', styles.heroBanner)}>
      <div className="container">
        <Heading as="h1" className="hero__title">{siteConfig.title}</Heading>
        <p className="hero__subtitle">{siteConfig.tagline}</p>
        <div className={styles.buttons}>
          <Link className="button button--secondary button--lg" to="/docs/intro">
            Get Started - 5min ⏱️
          </Link>
          <Link
            className="button button--outline button--secondary button--lg"
            to="/docs/getting-started/installation"
            style={{marginLeft: '1rem'}}>
            Installation Guide
          </Link>
        </div>
      </div>
    </header>
  );
}

const BadgeGroups = [
  [
    {
      alt: 'CI/CD Pipeline',
      img: 'https://github.com/schivei/GenDI/actions/workflows/ci-cd.yml/badge.svg',
      href: 'https://github.com/schivei/GenDI/actions/workflows/ci-cd.yml',
    },
    {
      alt: 'Deploy Documentation',
      img: 'https://github.com/schivei/GenDI/actions/workflows/deploy-docs.yml/badge.svg',
      href: 'https://github.com/schivei/GenDI/actions/workflows/deploy-docs.yml',
    },
    {
      alt: 'NuGet GenDI',
      img: 'https://img.shields.io/nuget/v/GenDI.svg?style=flat&label=GenDI&logo=nuget',
      href: 'https://www.nuget.org/packages/GenDI',
    },
    {
      alt: 'NuGet GenDI.SourceGenerator',
      img: 'https://img.shields.io/nuget/v/GenDI.SourceGenerator.svg?style=flat&label=GenDI.SourceGenerator&logo=nuget',
      href: 'https://www.nuget.org/packages/GenDI.SourceGenerator',
    },
    {
      alt: 'NuGet GenDI.Testing',
      img: 'https://img.shields.io/nuget/v/GenDI.Testing.svg?style=flat&label=GenDI.Testing&logo=nuget',
      href: 'https://www.nuget.org/packages/GenDI.Testing',
    },
    {
      alt: 'NuGet GenDI.Analyzers',
      img: 'https://img.shields.io/nuget/v/GenDI.Analyzers.svg?style=flat&label=GenDI.Analyzers&logo=nuget',
      href: 'https://www.nuget.org/packages/GenDI.Analyzers',
    },
    {
      alt: 'NuGet GenDI.Testing',
      img: 'https://img.shields.io/nuget/vpre/GenDI.Testing.svg?style=flat&label=GenDI.Testing%20Pre&logo=nuget',
      href: 'https://www.nuget.org/packages/GenDI.Testing',
    }
  ],
  [
    {
      alt: 'Quality Gate Status',
      img: 'https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=alert_status',
      href: 'https://sonarcloud.io/summary/new_code?id=schivei_GenDI',
    },
    {
      alt: 'Bugs',
      img: 'https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=bugs',
      href: 'https://sonarcloud.io/summary/new_code?id=schivei_GenDI',
    },
    {
      alt: 'Code Smells',
      img: 'https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=code_smells',
      href: 'https://sonarcloud.io/summary/new_code?id=schivei_GenDI',
    },
    {
      alt: 'Coverage',
      img: 'https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=coverage',
      href: 'https://sonarcloud.io/summary/new_code?id=schivei_GenDI',
    },
    {
      alt: 'Duplicated Lines (%)',
      img: 'https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=duplicated_lines_density',
      href: 'https://sonarcloud.io/summary/new_code?id=schivei_GenDI',
    },
    {
      alt: 'Lines of Code',
      img: 'https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=ncloc',
      href: 'https://sonarcloud.io/summary/new_code?id=schivei_GenDI',
    },
    {
      alt: 'Reliability Rating',
      img: 'https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=reliability_rating',
      href: 'https://sonarcloud.io/summary/new_code?id=schivei_GenDI',
    },
    {
      alt: 'Security Rating',
      img: 'https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=security_rating',
      href: 'https://sonarcloud.io/summary/new_code?id=schivei_GenDI',
    },
    {
      alt: 'Technical Debt',
      img: 'https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=sqale_index',
      href: 'https://sonarcloud.io/summary/new_code?id=schivei_GenDI',
    },
    {
      alt: 'Maintainability Rating',
      img: 'https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=sqale_rating',
      href: 'https://sonarcloud.io/summary/new_code?id=schivei_GenDI',
    },
    {
      alt: 'Vulnerabilities',
      img: 'https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=vulnerabilities',
      href: 'https://sonarcloud.io/summary/new_code?id=schivei_GenDI',
    },
  ],
  [
    {
      alt: 'License: MIT',
      img: 'https://img.shields.io/badge/License-MIT-yellow.svg',
      href: 'https://github.com/schivei/GenDI/blob/main/LICENSE',
    },
    {
      alt: 'Documentation',
      img: 'https://img.shields.io/badge/Documentation-Website-blue',
      href: 'https://elton.schivei.nom.br/GenDI',
    },
  ],
];

const BadgeRows = BadgeGroups.map((badges) => ({
  key: badges.map((badge) => badge.alt).join('|'),
  badges,
}));

function HomepageBadges() {
  return (
    <section className={styles.badges}>
      <div className="container">
        {BadgeRows.map((group) => (
          <div key={group.key} className={styles.badgeRow}>
            {group.badges.map((badge) => (
              <a key={badge.alt} href={badge.href}>
                <img src={badge.img} alt={badge.alt} />
              </a>
            ))}
          </div>
        ))}
      </div>
    </section>
  );
}

const FeatureList = [
  {
    title: 'Phase 6 Registration Model',
    emoji: '🚀',
    description: 'Use RM-01..RM-12 features in one generator flow: optional injection, conditional registration, decorators, options, factories and modules.',
  },
  {
    title: 'Factory + Module Paradigm',
    emoji: '🏗️',
    description: 'Prefer [InjectableFactory<TService>] for explicit contracts and compose bounded registrations with [InjectableModule].',
  },
  {
    title: 'Open-Generic Guardrails',
    emoji: '🛡️',
    description: 'Open-generic generation paths are bypassed by design and surfaced as generator warning GENDISG001 for safe NativeAOT-first behavior.',
  },
  {
    title: 'Environment + Decorator Support',
    emoji: '🌍',
    description: 'Activate services by environment with [ConditionalInjectable] and wrap contracts with [DecoratorFor<TService>] in generated registrations.',
  },
  {
    title: 'Options + Microsoft DI',
    emoji: '🧩',
    description: 'Bind configuration to IOptions<T> via [OptionConfig] and keep native integration with Microsoft.Extensions.DependencyInjection.',
  },
  {
    title: 'Documentation + CI Visibility',
    emoji: '✅',
    description: 'Website/docs now cover RM-01..RM-12 and CI publishes coverage summary with SonarScanner for .NET in the pipeline.',
  },
];

function Feature({emoji, title, description}) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
        <div className={styles.featureEmoji}>{emoji}</div>
      </div>
      <div className="text--center padding-horiz--md">
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

Feature.propTypes = {
  emoji: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired,
  description: PropTypes.string.isRequired,
};

function HomepageFeatures() {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((feature) => (
            <Feature key={feature.title} {...feature} />
          ))}
        </div>
      </div>
    </section>
  );
}

function BenchmarkValueProposition() {
  if (!benchmarkSalesPitch) {
    return null;
  }

  return (
    <section className={styles.salesPitch}>
      <div className="container">
        <div className={styles.salesPitchCard}>
          <p className={styles.salesPitchEyebrow}>{benchmarkSalesPitch.eyebrow}</p>
          <Heading as="h2">{benchmarkSalesPitch.title}</Heading>
          <p>{benchmarkSalesPitch.description}</p>
          <ul className={styles.salesPitchList}>
            {benchmarkSalesPitch.points.map((point) => (
              <li key={point}>{point}</li>
            ))}
          </ul>
          <Link className="button button--primary button--lg" to={benchmarkSalesPitch.ctaHref}>
            {benchmarkSalesPitch.ctaLabel}
          </Link>
        </div>
      </div>
    </section>
  );
}

function QuickExample() {
  return (
    <section className={styles.quickExample}>
      <div className="container">
        <Heading as="h2" className="text--center margin-bottom--lg">Quick Example</Heading>
        <pre>
          <code className="language-csharp">{`[ServiceInjection]
public interface IOrderService { }

[Injectable<IOrderService>(ServiceLifetime.Scoped, Module = "sales")]
[ConditionalInjectable("Production")]
public sealed partial class OrderService : IOrderService
{
    [InjectOptional]
    public ILogger<OrderService>? Logger { get; init; }
}

[OptionConfig("Sales:Api")]
public sealed class SalesApiOptions;

[InjectableFactory<IClock>(ServiceLifetime.Singleton)]
public static partial class ClockFactory
{
    public static IClock Create() => SystemClock.Instance;
}

builder.Host.UseGenDI();

builder.Services.AddGenDIServices(modules: "sales");`}</code>
        </pre>
        <p className="text--center margin-top--md">
          <Link to="/docs/advanced/registration-model-rm01-rm12">See full RM-01..RM-12 guide</Link>
        </p>
      </div>
    </section>
  );
}

export default function Home() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout
      title={`${siteConfig.title} - ${siteConfig.tagline}`}
      description="Attribute-first DI source generator with NativeAOT and trimming-oriented behavior.">
      <HomepageHeader />
      <main>
        <HomepageBadges />
        <BenchmarkValueProposition />
        <HomepageFeatures />
        <QuickExample />
      </main>
    </Layout>
  );
}
