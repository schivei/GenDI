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
    title: 'Attribute-Only Registration',
    emoji: '🏷️',
    description: 'Register services with [Injectable] and [ServiceInjection] without marker interfaces or runtime reflection.',
  },
  {
    title: 'NativeAOT Focus',
    emoji: '⚡',
    description: 'Generated constructors and init-property injection are designed for trimming and NativeAOT publish scenarios.',
  },
  {
    title: 'Deterministic Ordering',
    emoji: '📐',
    description: 'Control precedence with Group and Order, with a stable ordinal service-name fallback for predictable pipelines.',
  },
  {
    title: 'Coverage Control',
    emoji: '🧪',
    description: 'Use [assembly: GenDICoveration(...)] to include or exclude generated extension code in coverage reports.',
  },
  {
    title: 'Generator + Microsoft DI',
    emoji: '🧩',
    description: 'Generated AddGenDIServices() integrates directly with Microsoft.Extensions.DependencyInjection.',
  },
  {
    title: 'Validation Projects Included',
    emoji: '✅',
    description: 'Repository includes source-generator tests, real integration tests, trim publish tests, and NativeAOT publish tests.',
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
          <code className="language-csharp">{`[assembly: GenDI.GenDICoveration(true)]

[ServiceInjection]
public interface IMyService { }

[Injectable<IMyService>(ServiceLifetime.Singleton, Group = 10, Order = 1)]
public sealed class MyService(IDependency dep) : IMyService
{
    [Inject]
    public required IOtherService OtherService { get; init; }
}

services.AddGenDIServices();`}</code>
        </pre>
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
