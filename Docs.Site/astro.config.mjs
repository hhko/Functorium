import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import starlightImageZoom from 'starlight-image-zoom';
import starlightLinksValidator from 'starlight-links-validator';
import starlightSidebarTopics from 'starlight-sidebar-topics';
import rehypeMermaid from 'rehype-mermaid';

export default defineConfig({
  site: 'https://hhko.github.io',
  base: '/Functorium',
  markdown: {
    rehypePlugins: [
      [
        rehypeMermaid,
        {
          strategy: 'img-svg',
          mermaidConfig: {
            theme: 'base',
            themeVariables: {
              primaryColor: '#e4d9f9',
              primaryTextColor: '#1a1a2e',
              primaryBorderColor: '#512bd4',
              lineColor: '#512bd4',
              secondaryColor: '#f0ebff',
              tertiaryColor: '#faf8ff',
              fontFamily: 'Pretendard Variable, sans-serif',
            },
          },
          dark: {
            theme: 'base',
            themeVariables: {
              primaryColor: '#2a1f4e',
              primaryTextColor: '#e0d8f0',
              primaryBorderColor: '#7c4dff',
              lineColor: '#7c4dff',
              secondaryColor: '#1f1835',
              tertiaryColor: '#16112a',
              background: '#16112a',
              fontFamily: 'Pretendard Variable, sans-serif',
            },
          },
        },
      ],
    ],
  },
  integrations: [
    starlight({
      title: 'Functorium',
      defaultLocale: 'ko',
      locales: {
        root: {
          label: '한국어',
          lang: 'ko',
        },
      },
      customCss: ['./src/styles/custom.css'],
      head: [
        {
          tag: 'script',
          content: `
(function() {
  function syncMermaidTheme() {
    var theme = document.documentElement.dataset.theme;
    document.querySelectorAll('picture > source[id^="mermaid-dark-"]').forEach(function(source) {
      if (theme === 'light') {
        source.media = 'not all';
      } else if (theme === 'dark') {
        source.media = 'all';
      } else {
        source.media = '(prefers-color-scheme: dark)';
      }
    });
  }
  var obs = new MutationObserver(function(mutations) {
    mutations.forEach(function(m) {
      if (m.attributeName === 'data-theme') syncMermaidTheme();
    });
  });
  obs.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
  syncMermaidTheme();
})();
          `,
        },
      ],
      plugins: [
        starlightImageZoom(),
        starlightLinksValidator({
          errorOnRelativeLinks: false,
          errorOnInvalidHashes: false,
        }),
        starlightSidebarTopics([
          {
            label: '소개',
            link: '/',
            icon: 'open-book',
            items: [
              {
                label: '왜 Functorium인가',
                items: [
                  { label: '개요', slug: '' },
                  { label: '설계 동기', slug: 'motivation' },
                  { label: '설계 철학', slug: 'design-philosophy' },
                ],
              },
              {
                label: '무엇을 제공하는가',
                items: [
                  { label: '주요 핵심 기능', slug: 'key-features', badge: { text: '핵심', variant: 'tip' } },
                  { label: '구조 개요', slug: 'architecture' },
                ],
              },
              {
                label: '어떻게 시작하는가',
                items: [
                  { label: '5분 빠른시작', slug: 'quickstart', badge: { text: '5분', variant: 'tip' } },
                  { label: '시작하기', slug: 'getting-started', badge: { text: '시작', variant: 'note' } },
                ],
              },
            ],
          },
          {
            label: '프레임워크 가이드',
            link: '/guides/',
            icon: 'document',
            items: [
              { label: '프레임워크 가이드', slug: 'guides' },
              { label: '프로젝트 기초', autogenerate: { directory: 'guides/architecture' } },
              {
                label: '도메인 레이어',
                items: [
                  { slug: 'guides/domain/04-ddd-tactical-overview' },
                  {
                    label: '값 객체',
                    items: [
                      { slug: 'guides/domain/05a-value-objects' },
                      { slug: 'guides/domain/05b-value-objects-validation' },
                      { slug: 'guides/domain/05c-union-value-objects' },
                    ],
                  },
                  { slug: 'guides/domain/06a-aggregate-design' },
                  {
                    label: '엔티티와 애그리거트 구현',
                    items: [
                      { label: '핵심 패턴', slug: 'guides/domain/06b-entity-aggregate-core' },
                      { label: '고급 패턴', slug: 'guides/domain/06c-entity-aggregate-advanced' },
                    ],
                  },
                  { slug: 'guides/domain/07-domain-events' },
                  {
                    label: '에러 시스템',
                    items: [
                      { slug: 'guides/domain/08a-error-system' },
                      { label: '도메인/애플리케이션 에러', slug: 'guides/domain/08b-error-system-domain-app' },
                      { label: '어댑터 에러와 테스트', slug: 'guides/domain/08c-error-system-adapter-testing' },
                    ],
                  },
                  { slug: 'guides/domain/09-domain-services' },
                  { slug: 'guides/domain/10-specifications' },
                ],
              },
              { label: '애플리케이션 레이어', autogenerate: { directory: 'guides/application' } },
              {
                label: '어댑터 레이어',
                items: [
                  { slug: 'guides/adapter/12-ports' },
                  { slug: 'guides/adapter/13-adapters' },
                  {
                    label: '어댑터 연결',
                    items: [
                      { label: '파이프라인과 의존성 주입', slug: 'guides/adapter/14a-adapter-pipeline-di' },
                      { label: '단위 테스트', slug: 'guides/adapter/14b-adapter-testing' },
                    ],
                  },
                  { slug: 'guides/adapter/14c-repository-query-implementation-guide' },
                ],
              },
              { label: '테스트', autogenerate: { directory: 'guides/testing' } },
              { label: '관측 가능성', autogenerate: { directory: 'guides/observability' } },
              { label: '부록', collapsed: true, autogenerate: { directory: 'guides/appendix' } },
            ],
          },
          {
            label: '튜토리얼',
            link: '/tutorials/',
            icon: 'puzzle',
            items: [
              { label: 'Functorium 튜토리얼', slug: 'tutorials' },
              {
                label: '함수형 값 객체 구현',
                collapsed: true,
                items: [
                  { slug: 'tutorials/functional-valueobject' },
                  { label: '소개', autogenerate: { directory: 'tutorials/functional-valueobject/Part0-Introduction' } },
                  {
                    label: '값 객체 개념',
                    items: [
                      { slug: 'tutorials/functional-valueobject/part1-valueobject-concepts/01-basic-divide' },
                      { slug: 'tutorials/functional-valueobject/part1-valueobject-concepts/02-defensive-programming' },
                      { slug: 'tutorials/functional-valueobject/part1-valueobject-concepts/03-functional-result' },
                      { slug: 'tutorials/functional-valueobject/part1-valueobject-concepts/04-always-valid' },
                      { slug: 'tutorials/functional-valueobject/part1-valueobject-concepts/05-operator-overloading' },
                      { slug: 'tutorials/functional-valueobject/part1-valueobject-concepts/06-linq-expression' },
                      { slug: 'tutorials/functional-valueobject/part1-valueobject-concepts/07-value-equality' },
                      { slug: 'tutorials/functional-valueobject/part1-valueobject-concepts/08-value-comparability' },
                      { slug: 'tutorials/functional-valueobject/part1-valueobject-concepts/09-create-validate-separation' },
                      { slug: 'tutorials/functional-valueobject/part1-valueobject-concepts/10-validated-value-creation' },
                      { slug: 'tutorials/functional-valueobject/part1-valueobject-concepts/11-valueobject-framework' },
                      { slug: 'tutorials/functional-valueobject/part1-valueobject-concepts/12-type-safe-enums' },
                      { slug: 'tutorials/functional-valueobject/part1-valueobject-concepts/13-error-code' },
                      { slug: 'tutorials/functional-valueobject/part1-valueobject-concepts/14-error-code-fluent' },
                      { slug: 'tutorials/functional-valueobject/part1-valueobject-concepts/15-validation-fluent' },
                      { slug: 'tutorials/functional-valueobject/part1-valueobject-concepts/16-architecture-test' },
                    ],
                  },
                  {
                    label: '검증 패턴',
                    items: [
                      { slug: 'tutorials/functional-valueobject/part2-validation-patterns/01-bind-sequential-validation' },
                      { slug: 'tutorials/functional-valueobject/part2-validation-patterns/02-apply-parallel-validation' },
                      { slug: 'tutorials/functional-valueobject/part2-validation-patterns/03-apply-bind-combined-validation' },
                      { slug: 'tutorials/functional-valueobject/part2-validation-patterns/04-apply-internal-bind-validation' },
                      { slug: 'tutorials/functional-valueobject/part2-validation-patterns/05-bind-internal-apply-validation' },
                    ],
                  },
                  {
                    label: '값 객체 패턴',
                    items: [
                      { slug: 'tutorials/functional-valueobject/part3-valueobject-patterns/01-simplevalueobject' },
                      { slug: 'tutorials/functional-valueobject/part3-valueobject-patterns/02-comparablesimplevalueobject' },
                      { slug: 'tutorials/functional-valueobject/part3-valueobject-patterns/03-valueobject-primitive' },
                      { slug: 'tutorials/functional-valueobject/part3-valueobject-patterns/04-comparablevalueobject-primitive' },
                      { slug: 'tutorials/functional-valueobject/part3-valueobject-patterns/05-valueobject-composite' },
                      { slug: 'tutorials/functional-valueobject/part3-valueobject-patterns/06-comparablevalueobject-composite' },
                      { slug: 'tutorials/functional-valueobject/part3-valueobject-patterns/07-typesafeenum' },
                      { slug: 'tutorials/functional-valueobject/part3-valueobject-patterns/08-architecture-test' },
                    ],
                  },
                  {
                    label: '실전 가이드',
                    items: [
                      { slug: 'tutorials/functional-valueobject/part4-practical-guide/01-functorium-framework' },
                      { slug: 'tutorials/functional-valueobject/part4-practical-guide/01-functorium-framework/functoriumframework' },
                      { slug: 'tutorials/functional-valueobject/part4-practical-guide/02-orm-integration' },
                      { slug: 'tutorials/functional-valueobject/part4-practical-guide/02-orm-integration/ormintegration' },
                      { slug: 'tutorials/functional-valueobject/part4-practical-guide/03-cqrs-integration' },
                      { slug: 'tutorials/functional-valueobject/part4-practical-guide/03-cqrs-integration/cqrsintegration' },
                      { slug: 'tutorials/functional-valueobject/part4-practical-guide/04-testing-strategies' },
                      { slug: 'tutorials/functional-valueobject/part4-practical-guide/04-testing-strategies/testingstrategies' },
                    ],
                  },
                  {
                    label: '도메인 예제',
                    items: [
                      { slug: 'tutorials/functional-valueobject/part5-domain-examples/01-ecommerce-domain' },
                      { slug: 'tutorials/functional-valueobject/part5-domain-examples/01-ecommerce-domain/ecommercedomain' },
                      { slug: 'tutorials/functional-valueobject/part5-domain-examples/02-finance-domain' },
                      { slug: 'tutorials/functional-valueobject/part5-domain-examples/02-finance-domain/financedomain' },
                      { slug: 'tutorials/functional-valueobject/part5-domain-examples/03-user-management-domain' },
                      { slug: 'tutorials/functional-valueobject/part5-domain-examples/03-user-management-domain/usermanagementdomain' },
                      { slug: 'tutorials/functional-valueobject/part5-domain-examples/04-scheduling-domain' },
                      { slug: 'tutorials/functional-valueobject/part5-domain-examples/04-scheduling-domain/schedulingdomain' },
                    ],
                  },
                  { label: '부록', collapsed: true, autogenerate: { directory: 'tutorials/functional-valueobject/Appendix' } },
                ],
              },
              {
                label: '소스 생성기 관측 가능성',
                collapsed: true,
                items: [
                  { slug: 'tutorials/sourcegen-observability' },
                  {
                    label: '소개',
                    items: [
                      { slug: 'tutorials/sourcegen-observability/part0-introduction/01-what-is-source-generator' },
                      { slug: 'tutorials/sourcegen-observability/part0-introduction/02-hello-world-generator' },
                      { slug: 'tutorials/sourcegen-observability/part0-introduction/03-why-source-generator' },
                      { slug: 'tutorials/sourcegen-observability/part0-introduction/04-reflection-vs-sourcegen' },
                      { slug: 'tutorials/sourcegen-observability/part0-introduction/05-project-overview' },
                    ],
                  },
                  {
                    label: '기초',
                    items: [
                      { slug: 'tutorials/sourcegen-observability/part1-fundamentals/01-development-environment' },
                      { slug: 'tutorials/sourcegen-observability/part1-fundamentals/02-data-models' },
                      { slug: 'tutorials/sourcegen-observability/part1-fundamentals/03-debugging-setup' },
                      { slug: 'tutorials/sourcegen-observability/part1-fundamentals/04-roslyn-architecture' },
                      { slug: 'tutorials/sourcegen-observability/part1-fundamentals/05-syntax-api' },
                      { slug: 'tutorials/sourcegen-observability/part1-fundamentals/06-semantic-api' },
                      { slug: 'tutorials/sourcegen-observability/part1-fundamentals/07-symbol-types' },
                    ],
                  },
                  {
                    label: '핵심 개념',
                    items: [
                      { slug: 'tutorials/sourcegen-observability/part2-core-concepts/01-iincrementalgenerator' },
                      { slug: 'tutorials/sourcegen-observability/part2-core-concepts/02-provider-pattern' },
                      { slug: 'tutorials/sourcegen-observability/part2-core-concepts/03-forattribute' },
                      { slug: 'tutorials/sourcegen-observability/part2-core-concepts/04-incremental-caching' },
                      { slug: 'tutorials/sourcegen-observability/part2-core-concepts/05-inamedtypesymbol' },
                      { slug: 'tutorials/sourcegen-observability/part2-core-concepts/06-imethodsymbol' },
                      { slug: 'tutorials/sourcegen-observability/part2-core-concepts/07-symboldisplayformat' },
                      { slug: 'tutorials/sourcegen-observability/part2-core-concepts/08-type-extraction' },
                      { slug: 'tutorials/sourcegen-observability/part2-core-concepts/09-stringbuilder-pattern' },
                      { slug: 'tutorials/sourcegen-observability/part2-core-concepts/10-template-design' },
                      { slug: 'tutorials/sourcegen-observability/part2-core-concepts/11-namespace-handling' },
                      { slug: 'tutorials/sourcegen-observability/part2-core-concepts/12-deterministic-output' },
                    ],
                  },
                  {
                    label: '고급',
                    items: [
                      { slug: 'tutorials/sourcegen-observability/part3-advanced/01-constructor-handling' },
                      { slug: 'tutorials/sourcegen-observability/part3-advanced/02-generic-types' },
                      { slug: 'tutorials/sourcegen-observability/part3-advanced/03-collection-types' },
                      { slug: 'tutorials/sourcegen-observability/part3-advanced/04-loggermessage-limits' },
                      { slug: 'tutorials/sourcegen-observability/part3-advanced/05-unit-testing-setup' },
                      { slug: 'tutorials/sourcegen-observability/part3-advanced/06-verify-snapshot-testing' },
                      { slug: 'tutorials/sourcegen-observability/part3-advanced/07-test-scenarios' },
                    ],
                  },
                  {
                    label: '쿡북',
                    items: [
                      { slug: 'tutorials/sourcegen-observability/part4-cookbook/01-development-workflow' },
                      { slug: 'tutorials/sourcegen-observability/part4-cookbook/02-entity-id-generator' },
                      { slug: 'tutorials/sourcegen-observability/part4-cookbook/03-efcore-value-converter' },
                      { slug: 'tutorials/sourcegen-observability/part4-cookbook/04-validation-generator' },
                      { slug: 'tutorials/sourcegen-observability/part4-cookbook/05-custom-generator-template' },
                    ],
                  },
                  { label: '결론', autogenerate: { directory: 'tutorials/sourcegen-observability/Part5-Conclusion' } },
                  { label: '부록', collapsed: true, autogenerate: { directory: 'tutorials/sourcegen-observability/Appendix' } },
                ],
              },
              {
                label: '유스케이스 파이프라인 제약',
                collapsed: true,
                items: [
                  { slug: 'tutorials/usecase-pipeline' },
                  { label: '소개', autogenerate: { directory: 'tutorials/usecase-pipeline/Part0-Introduction' } },
                  {
                    label: '제네릭 변성 기초',
                    items: [
                      { slug: 'tutorials/usecase-pipeline/part1-generic-variance-foundations/01-covariance' },
                      { slug: 'tutorials/usecase-pipeline/part1-generic-variance-foundations/02-contravariance' },
                      { slug: 'tutorials/usecase-pipeline/part1-generic-variance-foundations/03-invariance-and-constraints' },
                      { slug: 'tutorials/usecase-pipeline/part1-generic-variance-foundations/04-interface-segregation-and-variance' },
                    ],
                  },
                  {
                    label: '문제 정의',
                    items: [
                      { slug: 'tutorials/usecase-pipeline/part2-problem-definition/01-mediator-pipeline-structure' },
                      { slug: 'tutorials/usecase-pipeline/part2-problem-definition/02-fin-direct-limitation' },
                      { slug: 'tutorials/usecase-pipeline/part2-problem-definition/03-ifinresponse-wrapper-limitation' },
                      { slug: 'tutorials/usecase-pipeline/part2-problem-definition/04-pipeline-requirements-summary' },
                    ],
                  },
                  {
                    label: '응답 인터페이스 계층',
                    items: [
                      { slug: 'tutorials/usecase-pipeline/part3-ifinresponse-hierarchy/01-ifinresponse-marker' },
                      { slug: 'tutorials/usecase-pipeline/part3-ifinresponse-hierarchy/02-ifinresponse-covariant' },
                      { slug: 'tutorials/usecase-pipeline/part3-ifinresponse-hierarchy/03-ifinresponsefactory-crtp' },
                      { slug: 'tutorials/usecase-pipeline/part3-ifinresponse-hierarchy/04-ifinresponsewitherror' },
                      { slug: 'tutorials/usecase-pipeline/part3-ifinresponse-hierarchy/05-finresponse-discriminated-union' },
                    ],
                  },
                  {
                    label: '파이프라인 제약 패턴',
                    items: [
                      { slug: 'tutorials/usecase-pipeline/part4-pipeline-constraint-patterns/01-create-only-constraint' },
                      { slug: 'tutorials/usecase-pipeline/part4-pipeline-constraint-patterns/02-read-create-constraint' },
                      { slug: 'tutorials/usecase-pipeline/part4-pipeline-constraint-patterns/03-transaction-caching-pipeline' },
                      { slug: 'tutorials/usecase-pipeline/part4-pipeline-constraint-patterns/04-fin-to-finresponse-bridge' },
                    ],
                  },
                  {
                    label: '실전 유스케이스 예제',
                    items: [
                      { slug: 'tutorials/usecase-pipeline/part5-practical-usecase-examples/01-command-usecase-example' },
                      { slug: 'tutorials/usecase-pipeline/part5-practical-usecase-examples/02-query-usecase-example' },
                      { slug: 'tutorials/usecase-pipeline/part5-practical-usecase-examples/03-full-pipeline-integration' },
                    ],
                  },
                  { label: '부록', collapsed: true, autogenerate: { directory: 'tutorials/usecase-pipeline/Appendix' } },
                ],
              },
              {
                label: '아키텍처 규칙 테스트',
                collapsed: true,
                items: [
                  { slug: 'tutorials/architecture-rules' },
                  { label: '소개', autogenerate: { directory: 'tutorials/architecture-rules/Part0-Introduction' } },
                  {
                    label: '클래스 검증기 기초',
                    items: [
                      { slug: 'tutorials/architecture-rules/part1-classvalidator-basics' },
                      { slug: 'tutorials/architecture-rules/part1-classvalidator-basics/01-first-architecture-test' },
                      { slug: 'tutorials/architecture-rules/part1-classvalidator-basics/02-visibility-and-modifiers' },
                      { slug: 'tutorials/architecture-rules/part1-classvalidator-basics/03-naming-rules' },
                      { slug: 'tutorials/architecture-rules/part1-classvalidator-basics/04-inheritance-and-interface' },
                    ],
                  },
                  {
                    label: '메서드/속성 검증',
                    items: [
                      { slug: 'tutorials/architecture-rules/part2-method-and-property-validation' },
                      { slug: 'tutorials/architecture-rules/part2-method-and-property-validation/01-method-validation' },
                      { slug: 'tutorials/architecture-rules/part2-method-and-property-validation/02-return-type-validation' },
                      { slug: 'tutorials/architecture-rules/part2-method-and-property-validation/03-parameter-validation' },
                      { slug: 'tutorials/architecture-rules/part2-method-and-property-validation/04-property-and-field-validation' },
                    ],
                  },
                  {
                    label: '고급 검증',
                    items: [
                      { slug: 'tutorials/architecture-rules/part3-advanced-validation/01-immutability-rule' },
                      { slug: 'tutorials/architecture-rules/part3-advanced-validation/02-nested-class-validation' },
                      { slug: 'tutorials/architecture-rules/part3-advanced-validation/03-interface-validation' },
                      { slug: 'tutorials/architecture-rules/part3-advanced-validation/04-custom-rules' },
                    ],
                  },
                  {
                    label: '실전 패턴',
                    items: [
                      { slug: 'tutorials/architecture-rules/part4-real-world-patterns' },
                      { slug: 'tutorials/architecture-rules/part4-real-world-patterns/01-domain-layer-rules' },
                      { slug: 'tutorials/architecture-rules/part4-real-world-patterns/02-application-layer-rules' },
                      { slug: 'tutorials/architecture-rules/part4-real-world-patterns/03-adapter-layer-rules' },
                      { slug: 'tutorials/architecture-rules/part4-real-world-patterns/04-layer-dependency-rules' },
                    ],
                  },
                  { label: '결론', autogenerate: { directory: 'tutorials/architecture-rules/Part5-Conclusion' } },
                  { label: '부록', collapsed: true, autogenerate: { directory: 'tutorials/architecture-rules/Appendix' } },
                ],
              },
              {
                label: 'CQRS 리포지토리 패턴',
                collapsed: true,
                items: [
                  { slug: 'tutorials/cqrs-repository' },
                  { label: '소개', autogenerate: { directory: 'tutorials/cqrs-repository/Part0-Introduction' } },
                  {
                    label: '도메인 엔티티 기초',
                    items: [
                      { slug: 'tutorials/cqrs-repository/part1-domain-entity-foundations/01-entity-and-identity' },
                      { slug: 'tutorials/cqrs-repository/part1-domain-entity-foundations/02-aggregate-root' },
                      { slug: 'tutorials/cqrs-repository/part1-domain-entity-foundations/03-domain-events' },
                      { slug: 'tutorials/cqrs-repository/part1-domain-entity-foundations/04-entity-interfaces' },
                    ],
                  },
                  {
                    label: '커맨드 리포지토리',
                    items: [
                      { slug: 'tutorials/cqrs-repository/part2-command-repository/01-repository-interface' },
                      { slug: 'tutorials/cqrs-repository/part2-command-repository/02-inmemory-repository' },
                      { slug: 'tutorials/cqrs-repository/part2-command-repository/03-efcore-repository' },
                      { slug: 'tutorials/cqrs-repository/part2-command-repository/04-unit-of-work' },
                    ],
                  },
                  {
                    label: '쿼리 패턴',
                    items: [
                      { slug: 'tutorials/cqrs-repository/part3-query-patterns/01-queryport-interface' },
                      { slug: 'tutorials/cqrs-repository/part3-query-patterns/02-dto-separation' },
                      { slug: 'tutorials/cqrs-repository/part3-query-patterns/03-pagination-and-sorting' },
                      { slug: 'tutorials/cqrs-repository/part3-query-patterns/04-inmemory-query-adapter' },
                      { slug: 'tutorials/cqrs-repository/part3-query-patterns/05-dapper-query-adapter' },
                    ],
                  },
                  {
                    label: 'CQRS 유스케이스 통합',
                    items: [
                      { slug: 'tutorials/cqrs-repository/part4-cqrs-usecase-integration/01-command-usecase' },
                      { slug: 'tutorials/cqrs-repository/part4-cqrs-usecase-integration/02-query-usecase' },
                      { slug: 'tutorials/cqrs-repository/part4-cqrs-usecase-integration/03-fint-to-finresponse' },
                      { slug: 'tutorials/cqrs-repository/part4-cqrs-usecase-integration/04-domain-event-flow' },
                      { slug: 'tutorials/cqrs-repository/part4-cqrs-usecase-integration/05-transaction-pipeline' },
                    ],
                  },
                  {
                    label: '도메인 예제',
                    items: [
                      { slug: 'tutorials/cqrs-repository/part5-domain-examples/01-ecommerce-order-management' },
                      { slug: 'tutorials/cqrs-repository/part5-domain-examples/02-customer-management' },
                      { slug: 'tutorials/cqrs-repository/part5-domain-examples/03-inventory-management' },
                      { slug: 'tutorials/cqrs-repository/part5-domain-examples/04-catalog-search' },
                    ],
                  },
                  { label: '부록', collapsed: true, autogenerate: { directory: 'tutorials/cqrs-repository/Appendix' } },
                ],
              },
              {
                label: '릴리스 노트 자동화',
                collapsed: true,
                items: [
                  { slug: 'tutorials/release-notes-claude' },
                  { label: '소개', autogenerate: { directory: 'tutorials/release-notes-claude/Part0-Introduction' } },
                  { label: '설정', autogenerate: { directory: 'tutorials/release-notes-claude/Part1-Setup' } },
                  { label: 'Claude 명령어', autogenerate: { directory: 'tutorials/release-notes-claude/Part2-Claude-Commands' } },
                  { label: '워크플로우', autogenerate: { directory: 'tutorials/release-notes-claude/Part3-Workflow' } },
                  { label: '구현', autogenerate: { directory: 'tutorials/release-notes-claude/Part4-Implementation' } },
                  { label: '실습', autogenerate: { directory: 'tutorials/release-notes-claude/Part5-Hands-On' } },
                  { label: '부록', collapsed: true, autogenerate: { directory: 'tutorials/release-notes-claude/Appendix' } },
                ],
              },
              {
                label: '명세 패턴',
                collapsed: true,
                items: [
                  { slug: 'tutorials/specification-pattern' },
                  { label: '소개', autogenerate: { directory: 'tutorials/specification-pattern/Part0-Introduction' } },
                  {
                    label: '명세 기초',
                    items: [
                      { slug: 'tutorials/specification-pattern/part1-specification-basics/01-first-specification' },
                      { slug: 'tutorials/specification-pattern/part1-specification-basics/02-composition' },
                      { slug: 'tutorials/specification-pattern/part1-specification-basics/03-operators' },
                      { slug: 'tutorials/specification-pattern/part1-specification-basics/04-all-identity' },
                    ],
                  },
                  {
                    label: '표현식 명세',
                    items: [
                      { slug: 'tutorials/specification-pattern/part2-expression-specification/01-expression-introduction' },
                      { slug: 'tutorials/specification-pattern/part2-expression-specification/02-expressionspecification-class' },
                      { slug: 'tutorials/specification-pattern/part2-expression-specification/03-valueobject-primitive-conversion' },
                      { slug: 'tutorials/specification-pattern/part2-expression-specification/04-expression-resolver' },
                    ],
                  },
                  {
                    label: '리포지토리 통합',
                    items: [
                      { slug: 'tutorials/specification-pattern/part3-repository-integration/01-repository-with-specification' },
                      { slug: 'tutorials/specification-pattern/part3-repository-integration/02-inmemory-implementation' },
                      { slug: 'tutorials/specification-pattern/part3-repository-integration/03-propertymap' },
                      { slug: 'tutorials/specification-pattern/part3-repository-integration/04-efcore-implementation' },
                    ],
                  },
                  {
                    label: '실전 패턴',
                    items: [
                      { slug: 'tutorials/specification-pattern/part4-real-world-patterns/01-usecase-patterns' },
                      { slug: 'tutorials/specification-pattern/part4-real-world-patterns/02-dynamic-filter-builder' },
                      { slug: 'tutorials/specification-pattern/part4-real-world-patterns/03-testing-strategies' },
                      { slug: 'tutorials/specification-pattern/part4-real-world-patterns/04-architecture-rules' },
                    ],
                  },
                  {
                    label: '도메인 예제',
                    items: [
                      { slug: 'tutorials/specification-pattern/part5-domain-examples/01-ecommerce-product-filtering' },
                      { slug: 'tutorials/specification-pattern/part5-domain-examples/02-customer-management' },
                    ],
                  },
                  { label: '부록', collapsed: true, autogenerate: { directory: 'tutorials/specification-pattern/Appendix' } },
                ],
              },
            ],
          },
          {
            label: '샘플',
            link: '/samples/',
            icon: 'rocket',
            items: [
              { label: 'Functorium 샘플', slug: 'samples' },
              {
                label: '타입으로 도메인 설계하기',
                items: [
                  { slug: 'samples/designing-with-types' },
                  {
                    label: '도메인 레이어',
                    items: [
                      { slug: 'samples/designing-with-types/domain/00-business-requirements' },
                      { slug: 'samples/designing-with-types/domain/01-type-design-decisions' },
                      { slug: 'samples/designing-with-types/domain/02-code-design' },
                      { slug: 'samples/designing-with-types/domain/03-implementation-results' },
                    ],
                  },
                ],
              },
              {
                label: '전자상거래 DDD',
                items: [
                  { slug: 'samples/ecommerce-ddd' },
                  {
                    label: '도메인 레이어',
                    items: [
                      { slug: 'samples/ecommerce-ddd/domain/00-business-requirements' },
                      { slug: 'samples/ecommerce-ddd/domain/01-type-design-decisions' },
                      { slug: 'samples/ecommerce-ddd/domain/02-code-design' },
                      { slug: 'samples/ecommerce-ddd/domain/03-implementation-results' },
                    ],
                  },
                  {
                    label: '애플리케이션 레이어',
                    items: [
                      { slug: 'samples/ecommerce-ddd/application/00-business-requirements' },
                      { slug: 'samples/ecommerce-ddd/application/01-type-design-decisions' },
                      { slug: 'samples/ecommerce-ddd/application/02-code-design' },
                      { slug: 'samples/ecommerce-ddd/application/03-implementation-results' },
                    ],
                  },
                ],
              },
            ],
          },
          {
            label: 'API 사양',
            link: '/spec/',
            icon: 'setting',
            items: [
              { label: 'API 사양 레퍼런스', slug: 'spec' },
              {
                label: '도메인 핵심',
                items: [
                  { slug: 'spec/01-entity-aggregate' },
                  { slug: 'spec/02-value-object' },
                  { slug: 'spec/04-error-system' },
                ],
              },
              {
                label: '애플리케이션 계층',
                items: [
                  { slug: 'spec/03-validation' },
                  { slug: 'spec/05-usecase-cqrs' },
                  { slug: 'spec/09-domain-events' },
                ],
              },
              {
                label: '어댑터/인프라',
                items: [
                  { slug: 'spec/06-port-adapter' },
                  { slug: 'spec/07-pipeline' },
                ],
              },
              {
                label: '횡단 관심사',
                items: [
                  { slug: 'spec/08-observability' },
                  { slug: 'spec/10-source-generators' },
                  { slug: 'spec/11-testing' },
                ],
              },
            ],
          },
          {
            label: 'AI 자동화',
            link: '/ai-automation/',
            icon: 'star',
            items: [
              { label: 'AI 자동화', slug: 'ai-automation' },
              { slug: 'ai-automation/installation' },
              {
                label: '스킬',
                items: [
                  { slug: 'ai-automation/skills/domain-develop' },
                  { slug: 'ai-automation/skills/application-develop' },
                  { slug: 'ai-automation/skills/adapter-develop' },
                  { slug: 'ai-automation/skills/test-develop' },
                  { slug: 'ai-automation/skills/domain-review' },
                ],
              },
              { slug: 'ai-automation/agents' },
            ],
          },
        ]),
      ],
      social: [
        {
          icon: 'github',
          label: 'GitHub',
          href: 'https://github.com/hhko/Functorium',
        },
      ],
    }),
  ],
});
