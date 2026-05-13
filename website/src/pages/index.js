import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';
import PropTypes from 'prop-types';

import styles from './index.module.css';

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
          {FeatureList.map((props) => (
            <Feature key={props.title} {...props} />
          ))}
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

services.AddGenDIServices(modules: "sales");`}</code>
        </pre>
        <p className="text--center margin-top--md">
          <Link to="/docs/advanced/registration-model-rm08-rm12">See full RM-01..RM-12 guide</Link>
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
        <HomepageFeatures />
        <QuickExample />
      </main>
    </Layout>
  );
}
