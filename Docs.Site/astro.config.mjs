import { defineConfig } from 'astro/config';
import mdx from '@astrojs/mdx';
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
      defaultLocale: 'root',
      locales: {
        root: {
          label: 'English',
          lang: 'en',
        },
        ko: {
          label: '한국어',
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
            label: 'Introduction',
            translations: { ko: '소개' },
            link: '/',
            icon: 'open-book',
            items: [
              {
                label: 'Why Functorium',
                translations: { ko: '왜 Functorium인가' },
                items: [
                  { label: 'Overview', translations: { ko: '개요' }, slug: '' },
                  { label: 'Design Motivation', translations: { ko: '설계 동기' }, slug: 'motivation' },
                  { label: 'Design Philosophy', translations: { ko: '설계 철학' }, slug: 'design-philosophy' },
                ],
              },
              {
                label: 'What It Offers',
                translations: { ko: '무엇을 제공하는가' },
                items: [
                  { label: 'Key Features', translations: { ko: '주요 핵심 기능' }, slug: 'key-features', badge: { text: 'Core', variant: 'tip' } },
                  { label: 'Architecture Overview', translations: { ko: '구조 개요' }, slug: 'architecture' },
                ],
              },
              {
                label: 'How to Get Started',
                translations: { ko: '어떻게 시작하는가' },
                items: [
                  { label: 'Quick Start (5 min)', translations: { ko: '5분 빠른시작' }, slug: 'quickstart', badge: { text: '5 min', variant: 'tip' } },
                  { label: 'Getting Started', translations: { ko: '시작하기' }, slug: 'getting-started', badge: { text: 'Start', variant: 'note' } },
                ],
              },
            ],
          },
          {
            label: 'Architecture Decision Records',
            translations: { ko: '아키텍처 의사결정 기록' },
            link: '/adr/',
            icon: 'list-format',
            items: [
              { label: 'List', translations: { ko: '목록' }, slug: 'adr' },
              {
                label: 'Foundation',
                collapsed: true,
                items: [
                  { label: 'ADR-0001 Fin Type over Exceptions', translations: { ko: 'ADR-0001 예외 대신 Fin 타입' }, slug: 'adr/0001-foundation-use-fin-over-exceptions' },
                  { label: 'ADR-0002 Adopt LanguageExt', translations: { ko: 'ADR-0002 LanguageExt 채택' }, slug: 'adr/0002-foundation-languageext' },
                ],
              },
              {
                label: 'Domain',
                collapsed: true,
                items: [
                  { label: 'ADR-0003 CQRS Read/Write Separation', translations: { ko: 'ADR-0003 CQRS 읽기/쓰기 분리' }, slug: 'adr/0003-domain-cqrs-read-write-separation' },
                  { label: 'ADR-0004 Ulid-Based Entity ID', translations: { ko: 'ADR-0004 Ulid 기반 Entity ID' }, slug: 'adr/0004-domain-ulid-based-entity-id' },
                  { label: 'ADR-0005 UnionValueObject State Machine', translations: { ko: 'ADR-0005 UnionValueObject 상태 머신' }, slug: 'adr/0005-domain-union-valueobject-state-machine' },
                  { label: 'ADR-0006 Specification + Expression Tree', slug: 'adr/0006-domain-specification-expression-tree' },
                  { label: 'ADR-0007 Aggregate ID-Only Cross References', translations: { ko: 'ADR-0007 Aggregate 간 ID 전용 참조' }, slug: 'adr/0007-domain-aggregate-id-only-cross-references' },
                  { label: 'ADR-0008 Domain Service Pure vs Repository', translations: { ko: 'ADR-0008 Domain Service 순수 vs Repository' }, slug: 'adr/0008-domain-service-pure-vs-repository' },
                  { label: 'ADR-0009 Value Object Class/Record Duality', translations: { ko: 'ADR-0009 Value Object Class/Record 이중 계층' }, slug: 'adr/0009-domain-value-object-class-record-duality' },
                  { label: 'ADR-0010 Error Code Sealed Record Hierarchy', translations: { ko: 'ADR-0010 에러 코드 sealed record 계층' }, slug: 'adr/0010-domain-error-code-sealed-record-hierarchy' },
                ],
              },
              {
                label: 'Application',
                collapsed: true,
                items: [
                  { label: 'ADR-0011 Pipeline Execution Order', translations: { ko: 'ADR-0011 Pipeline 실행 순서' }, slug: 'adr/0011-application-pipeline-execution-order' },
                  { label: 'ADR-0012 FinResponse Type Constraints', translations: { ko: 'ADR-0012 FinResponse 타입 제약' }, slug: 'adr/0012-application-finresponse-pipeline-constraints' },
                  { label: 'ADR-0013 Validation Order Normalize then MaxLength', translations: { ko: 'ADR-0013 검증 순서 Normalize → MaxLength' }, slug: 'adr/0013-application-validation-normalize-maxlength' },
                  { label: 'ADR-0014 Explicit Transaction Support', translations: { ko: 'ADR-0014 명시적 트랜잭션 지원' }, slug: 'adr/0014-application-explicit-transaction' },
                ],
              },
              {
                label: 'Adapter',
                collapsed: true,
                items: [
                  { label: 'ADR-0015 Observable Port Source Generator', slug: 'adr/0015-adapter-observable-port-source-generator' },
                  { label: 'ADR-0016 Persistence Suffix Naming', translations: { ko: 'ADR-0016 영속성 접미사 네이밍' }, slug: 'adr/0016-adapter-suffix-naming-persistence' },
                ],
              },
              {
                label: 'Observability',
                collapsed: true,
                items: [
                  { label: 'ADR-0017 ctx.* Pillar Targeting', slug: 'adr/0017-observability-ctx-enricher-pillar-targeting' },
                  { label: 'ADR-0018 Auto Error Classification (3 Types)', translations: { ko: 'ADR-0018 에러 3종 자동 분류' }, slug: 'adr/0018-observability-error-classification' },
                  { label: 'ADR-0019 Field Naming snake_case + dot', translations: { ko: 'ADR-0019 필드 네이밍 snake_case + dot' }, slug: 'adr/0019-observability-field-naming-snake-case-dot' },
                ],
              },
              {
                label: 'Event',
                collapsed: true,
                items: [
                  { label: 'ADR-0020 DomainEvent Publisher Simplification', translations: { ko: 'ADR-0020 DomainEvent Publisher 단순화' }, slug: 'adr/0020-event-publisher-simplification' },
                  { label: 'ADR-0021 DomainEvent Correlation ID', translations: { ko: 'ADR-0021 DomainEvent 상관관계 ID' }, slug: 'adr/0021-event-tracing-correlation' },
                ],
              },
              {
                label: 'Testing',
                collapsed: true,
                items: [{ label: 'ADR-0022 Architecture Test Suite', translations: { ko: 'ADR-0022 아키텍처 테스트 Suite' }, slug: 'adr/0022-testing-architecture-test-suite' }],
              },
            ],
          },
          {
            label: 'Specifications',
            translations: { ko: '스펙' },
            link: '/spec/',
            icon: 'setting',
            items: [
              { label: 'Functorium Specifications', translations: { ko: 'Functorium 스펙' }, slug: 'spec' },
              {
                label: 'Domain Core',
                translations: { ko: '도메인 핵심' },
                items: [
                  { slug: 'spec/01-entity-aggregate' },
                  { slug: 'spec/02-value-object' },
                  { slug: 'spec/04-error-system' },
                ],
              },
              {
                label: 'Application Layer',
                translations: { ko: '애플리케이션 계층' },
                items: [
                  { slug: 'spec/03-validation' },
                  { slug: 'spec/05-usecase-cqrs' },
                  { slug: 'spec/09-domain-events' },
                ],
              },
              {
                label: 'Adapter / Infrastructure',
                translations: { ko: '어댑터/인프라' },
                items: [
                  { slug: 'spec/06-port-adapter' },
                  { slug: 'spec/07-pipeline' },
                ],
              },
              {
                label: 'Cross-Cutting Concerns',
                translations: { ko: '횡단 관심사' },
                items: [
                  { slug: 'spec/08-observability' },
                  { slug: 'spec/10-source-generators' },
                  { slug: 'spec/11-testing' },
                ],
              },
            ],
          },
                    {
            label: 'AX',
            link: '/ax/',
            icon: 'star',
            items: [
              { label: 'AX (AI Transformation)', slug: 'ax' },
              {
                label: 'functorium-develop',
                items: [
                  { slug: 'ax/functorium-develop' },
                  { slug: 'ax/functorium-develop/workflow' },
                  {
                    label: 'Skills',
                    translations: { ko: '스킬' },
                    items: [
                      { slug: 'ax/functorium-develop/skills/project-spec' },
                      { slug: 'ax/functorium-develop/skills/architecture-design' },
                      { slug: 'ax/functorium-develop/skills/domain-develop' },
                      { slug: 'ax/functorium-develop/skills/application-develop' },
                      { slug: 'ax/functorium-develop/skills/adapter-develop' },
                      { slug: 'ax/functorium-develop/skills/observability-develop' },
                      { slug: 'ax/functorium-develop/skills/test-develop' },
                      { slug: 'ax/functorium-develop/skills/domain-review' },
                    ],
                  },
                  { slug: 'ax/functorium-develop/agents' },
                ],
              },
              {
                label: 'release-note',
                items: [
                  { slug: 'ax/release-note' },
                  { slug: 'ax/release-note/agents' },
                ],
              },
            ],
          },
          {
            label: 'Samples',
            translations: { ko: '예제' },
            link: '/samples/',
            icon: 'rocket',
            items: [
              { label: 'Functorium Samples', translations: { ko: 'Functorium 예제' }, slug: 'samples' },
              {
                label: 'Designing with Types',
                translations: { ko: '타입으로 도메인 설계하기' },
                items: [
                  { slug: 'samples/designing-with-types' },
                  {
                    label: 'Domain Layer',
                    translations: { ko: '도메인 레이어' },
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
                label: 'E-Commerce DDD',
                translations: { ko: '전자상거래 DDD' },
                items: [
                  { slug: 'samples/ecommerce-ddd' },
                  {
                    label: 'Domain Layer',
                    translations: { ko: '도메인 레이어' },
                    items: [
                      { slug: 'samples/ecommerce-ddd/domain/00-business-requirements' },
                      { slug: 'samples/ecommerce-ddd/domain/01-type-design-decisions' },
                      { slug: 'samples/ecommerce-ddd/domain/02-code-design' },
                      { slug: 'samples/ecommerce-ddd/domain/03-implementation-results' },
                    ],
                  },
                  {
                    label: 'Application Layer',
                    translations: { ko: '애플리케이션 레이어' },
                    items: [
                      { slug: 'samples/ecommerce-ddd/application/00-business-requirements' },
                      { slug: 'samples/ecommerce-ddd/application/01-type-design-decisions' },
                      { slug: 'samples/ecommerce-ddd/application/02-code-design' },
                      { slug: 'samples/ecommerce-ddd/application/03-implementation-results' },
                    ],
                  },
                ],
              },
              {
                label: 'AI Model Governance',
                translations: { ko: 'AI 모델 거버넌스' },
                items: [
                  { slug: 'samples/ai-model-governance' },
                  { slug: 'samples/ai-model-governance/00-project-spec' },
                  { slug: 'samples/ai-model-governance/01-architecture-design' },
                  {
                    label: 'Domain Layer',
                    translations: { ko: '도메인 레이어' },
                    items: [
                      { slug: 'samples/ai-model-governance/domain/00-business-requirements' },
                      { slug: 'samples/ai-model-governance/domain/01-type-design-decisions' },
                      { slug: 'samples/ai-model-governance/domain/02-code-design' },
                      { slug: 'samples/ai-model-governance/domain/03-implementation-results' },
                    ],
                  },
                  {
                    label: 'Application Layer',
                    translations: { ko: '애플리케이션 레이어' },
                    items: [
                      { slug: 'samples/ai-model-governance/application/00-business-requirements' },
                      { slug: 'samples/ai-model-governance/application/01-type-design-decisions' },
                      { slug: 'samples/ai-model-governance/application/02-code-design' },
                      { slug: 'samples/ai-model-governance/application/03-implementation-results' },
                    ],
                  },
                  {
                    label: 'Adapter Layer',
                    translations: { ko: '어댑터 레이어' },
                    items: [
                      { slug: 'samples/ai-model-governance/adapter/00-business-requirements' },
                      { slug: 'samples/ai-model-governance/adapter/01-type-design-decisions' },
                      { slug: 'samples/ai-model-governance/adapter/02-code-design' },
                      { slug: 'samples/ai-model-governance/adapter/03-implementation-results' },
                    ],
                  },
                  {
                    label: 'Observability',
                    translations: { ko: '관측성' },
                    items: [
                      { slug: 'samples/ai-model-governance/observability/00-business-requirements' },
                      { slug: 'samples/ai-model-governance/observability/01-type-design-decisions' },
                      { slug: 'samples/ai-model-governance/observability/02-code-design' },
                      { slug: 'samples/ai-model-governance/observability/03-implementation-results' },
                    ],
                  },
                ],
              },
            ],
          },
          {
            label: 'Guides',
            translations: { ko: '가이드' },
            link: '/guides/',
            icon: 'document',
            items: [
              { label: 'Functorium Guides', translations: { ko: 'Functorium 가이드' }, slug: 'guides' },
              { label: 'Project Foundations', translations: { ko: '프로젝트 기초' }, autogenerate: { directory: 'guides/architecture' } },
              {
                label: 'Domain Layer',
                translations: { ko: '도메인 레이어' },
                items: [
                  { slug: 'guides/domain/04-ddd-tactical-overview' },
                  {
                    label: 'Value Objects',
                    translations: { ko: '값 객체' },
                    items: [
                      { slug: 'guides/domain/05a-value-objects' },
                      { slug: 'guides/domain/05b-value-objects-validation' },
                      { slug: 'guides/domain/05c-union-value-objects' },
                    ],
                  },
                  { slug: 'guides/domain/06a-aggregate-design' },
                  {
                    label: 'Entity & Aggregate Implementation',
                    translations: { ko: '엔티티와 애그리거트 구현' },
                    items: [
                      { label: 'Core Patterns', translations: { ko: '핵심 패턴' }, slug: 'guides/domain/06b-entity-aggregate-core' },
                      { label: 'Advanced Patterns', translations: { ko: '고급 패턴' }, slug: 'guides/domain/06c-entity-aggregate-advanced' },
                    ],
                  },
                  { slug: 'guides/domain/07-domain-events' },
                  {
                    label: 'Error System',
                    translations: { ko: '에러 시스템' },
                    items: [
                      { slug: 'guides/domain/08a-error-system' },
                      { label: 'Domain/Application Errors', translations: { ko: '도메인/애플리케이션 에러' }, slug: 'guides/domain/08b-error-system-domain-app' },
                      { label: 'Adapter Errors & Testing', translations: { ko: '어댑터 에러와 테스트' }, slug: 'guides/domain/08c-error-system-adapter-testing' },
                    ],
                  },
                  { slug: 'guides/domain/09-domain-services' },
                  { slug: 'guides/domain/10-specifications' },
                ],
              },
              { label: 'Application Layer', translations: { ko: '애플리케이션 레이어' }, autogenerate: { directory: 'guides/application' } },
              {
                label: 'Adapter Layer',
                translations: { ko: '어댑터 레이어' },
                items: [
                  { slug: 'guides/adapter/12-ports' },
                  { slug: 'guides/adapter/13-adapters' },
                  {
                    label: 'Adapter Integration',
                    translations: { ko: '어댑터 연결' },
                    items: [
                      { label: 'Pipeline & Dependency Injection', translations: { ko: '파이프라인과 의존성 주입' }, slug: 'guides/adapter/14a-adapter-pipeline-di' },
                      { label: 'Unit Testing', translations: { ko: '단위 테스트' }, slug: 'guides/adapter/14b-adapter-testing' },
                    ],
                  },
                  { slug: 'guides/adapter/14c-repository-query-implementation-guide' },
                ],
              },
              { label: 'Testing', translations: { ko: '테스트' }, autogenerate: { directory: 'guides/testing' } },
              { label: 'Observability', translations: { ko: '관측 가능성' }, autogenerate: { directory: 'guides/observability' } },
              { label: 'Appendix', translations: { ko: '부록' }, collapsed: true, autogenerate: { directory: 'guides/appendix' } },
            ],
          },
          {
            label: 'Tutorials',
            translations: { ko: '튜토리얼' },
            link: '/tutorials/',
            icon: 'puzzle',
            items: [
              { label: 'Functorium Tutorials', translations: { ko: 'Functorium 튜토리얼' }, slug: 'tutorials' },
              {
                label: 'Functional Value Object Implementation',
                translations: { ko: '함수형 값 객체 구현' },
                collapsed: true,
                items: [
                  { slug: 'tutorials/functional-valueobject' },
                  { label: 'Introduction', translations: { ko: '소개' }, autogenerate: { directory: 'tutorials/functional-valueobject/part0-introduction' } },
                  {
                    label: 'Value Object Concepts',
                    translations: { ko: '값 객체 개념' },
                    autogenerate: { directory: 'tutorials/functional-valueobject/part1-valueobject-concepts' },
                  },
                  {
                    label: 'Validation Patterns',
                    translations: { ko: '검증 패턴' },
                    autogenerate: { directory: 'tutorials/functional-valueobject/part2-validation-patterns' },
                  },
                  {
                    label: 'Value Object Patterns',
                    translations: { ko: '값 객체 패턴' },
                    autogenerate: { directory: 'tutorials/functional-valueobject/part3-valueobject-patterns' },
                  },
                  {
                    label: 'Practical Guide',
                    translations: { ko: '실전 가이드' },
                    autogenerate: { directory: 'tutorials/functional-valueobject/part4-practical-guide' },
                  },
                  {
                    label: 'Domain Examples',
                    translations: { ko: '도메인 예제' },
                    autogenerate: { directory: 'tutorials/functional-valueobject/part5-domain-examples' },
                  },
                  { label: 'Appendix', translations: { ko: '부록' }, collapsed: true, autogenerate: { directory: 'tutorials/functional-valueobject/appendix' } },
                ],
              },
              {
                label: 'Source Generator Observability',
                translations: { ko: '소스 생성기 관측 가능성' },
                collapsed: true,
                items: [
                  { slug: 'tutorials/sourcegen-observability' },
                  {
                    label: 'Introduction',
                    translations: { ko: '소개' },
                    autogenerate: { directory: 'tutorials/sourcegen-observability/part0-introduction' },
                  },
                  {
                    label: 'Fundamentals',
                    translations: { ko: '기초' },
                    autogenerate: { directory: 'tutorials/sourcegen-observability/part1-fundamentals' },
                  },
                  {
                    label: 'Core Concepts',
                    translations: { ko: '핵심 개념' },
                    autogenerate: { directory: 'tutorials/sourcegen-observability/part2-core-concepts' },
                  },
                  {
                    label: 'Advanced',
                    translations: { ko: '고급' },
                    autogenerate: { directory: 'tutorials/sourcegen-observability/part3-advanced' },
                  },
                  {
                    label: 'Cookbook',
                    translations: { ko: '쿡북' },
                    autogenerate: { directory: 'tutorials/sourcegen-observability/part4-cookbook' },
                  },
                  { label: 'Conclusion', translations: { ko: '결론' }, autogenerate: { directory: 'tutorials/sourcegen-observability/part5-conclusion' } },
                  { label: 'Appendix', translations: { ko: '부록' }, collapsed: true, autogenerate: { directory: 'tutorials/sourcegen-observability/appendix' } },
                ],
              },
              {
                label: 'Use Case Pipeline Constraints',
                translations: { ko: '유스케이스 파이프라인 제약' },
                collapsed: true,
                items: [
                  { slug: 'tutorials/usecase-pipeline' },
                  { label: 'Introduction', translations: { ko: '소개' }, autogenerate: { directory: 'tutorials/usecase-pipeline/part0-introduction' } },
                  {
                    label: 'Generic Variance Foundations',
                    translations: { ko: '제네릭 변성 기초' },
                    autogenerate: { directory: 'tutorials/usecase-pipeline/part1-generic-variance-foundations' },
                  },
                  {
                    label: 'Problem Definition',
                    translations: { ko: '문제 정의' },
                    autogenerate: { directory: 'tutorials/usecase-pipeline/part2-problem-definition' },
                  },
                  {
                    label: 'Response Interface Hierarchy',
                    translations: { ko: '응답 인터페이스 계층' },
                    autogenerate: { directory: 'tutorials/usecase-pipeline/part3-ifinresponse-hierarchy' },
                  },
                  {
                    label: 'Pipeline Constraint Patterns',
                    translations: { ko: '파이프라인 제약 패턴' },
                    autogenerate: { directory: 'tutorials/usecase-pipeline/part4-pipeline-constraint-patterns' },
                  },
                  {
                    label: 'Practical Use Case Examples',
                    translations: { ko: '실전 유스케이스 예제' },
                    autogenerate: { directory: 'tutorials/usecase-pipeline/part5-practical-usecase-examples' },
                  },
                  { label: 'Appendix', translations: { ko: '부록' }, collapsed: true, autogenerate: { directory: 'tutorials/usecase-pipeline/appendix' } },
                ],
              },
              {
                label: 'Architecture Rules Testing',
                translations: { ko: '아키텍처 규칙 테스트' },
                collapsed: true,
                items: [
                  { slug: 'tutorials/architecture-rules' },
                  { label: 'Introduction', translations: { ko: '소개' }, autogenerate: { directory: 'tutorials/architecture-rules/part0-introduction' } },
                  {
                    label: 'Class Validator Basics',
                    translations: { ko: '클래스 검증기 기초' },
                    autogenerate: { directory: 'tutorials/architecture-rules/part1-classvalidator-basics' },
                  },
                  {
                    label: 'Method & Property Validation',
                    translations: { ko: '메서드/속성 검증' },
                    autogenerate: { directory: 'tutorials/architecture-rules/part2-method-and-property-validation' },
                  },
                  {
                    label: 'Advanced Validation',
                    translations: { ko: '고급 검증' },
                    autogenerate: { directory: 'tutorials/architecture-rules/part3-advanced-validation' },
                  },
                  {
                    label: 'Real-World Patterns',
                    translations: { ko: '실전 패턴' },
                    autogenerate: { directory: 'tutorials/architecture-rules/part4-real-world-patterns' },
                  },
                  { label: 'Conclusion', translations: { ko: '결론' }, autogenerate: { directory: 'tutorials/architecture-rules/part5-conclusion' } },
                  { label: 'Appendix', translations: { ko: '부록' }, collapsed: true, autogenerate: { directory: 'tutorials/architecture-rules/appendix' } },
                ],
              },
              {
                label: 'CQRS Repository Pattern',
                translations: { ko: 'CQRS 리포지토리 패턴' },
                collapsed: true,
                items: [
                  { slug: 'tutorials/cqrs-repository' },
                  { label: 'Introduction', translations: { ko: '소개' }, autogenerate: { directory: 'tutorials/cqrs-repository/part0-introduction' } },
                  {
                    label: 'Domain Entity Foundations',
                    translations: { ko: '도메인 엔티티 기초' },
                    autogenerate: { directory: 'tutorials/cqrs-repository/part1-domain-entity-foundations' },
                  },
                  {
                    label: 'Command Repository',
                    translations: { ko: '커맨드 리포지토리' },
                    autogenerate: { directory: 'tutorials/cqrs-repository/part2-command-repository' },
                  },
                  {
                    label: 'Query Patterns',
                    translations: { ko: '쿼리 패턴' },
                    autogenerate: { directory: 'tutorials/cqrs-repository/part3-query-patterns' },
                  },
                  {
                    label: 'CQRS Use Case Integration',
                    translations: { ko: 'CQRS 유스케이스 통합' },
                    autogenerate: { directory: 'tutorials/cqrs-repository/part4-cqrs-usecase-integration' },
                  },
                  {
                    label: 'Domain Examples',
                    translations: { ko: '도메인 예제' },
                    autogenerate: { directory: 'tutorials/cqrs-repository/part5-domain-examples' },
                  },
                  { label: 'Appendix', translations: { ko: '부록' }, collapsed: true, autogenerate: { directory: 'tutorials/cqrs-repository/appendix' } },
                ],
              },
              {
                label: 'Release Notes Automation',
                translations: { ko: '릴리스 노트 자동화' },
                collapsed: true,
                items: [
                  { slug: 'tutorials/release-notes-claude' },
                  { label: 'Introduction', translations: { ko: '소개' }, autogenerate: { directory: 'tutorials/release-notes-claude/part0-introduction' } },
                  { label: 'Setup', translations: { ko: '설정' }, autogenerate: { directory: 'tutorials/release-notes-claude/part1-setup' } },
                  { label: 'Claude Commands', translations: { ko: 'Claude 명령어' }, autogenerate: { directory: 'tutorials/release-notes-claude/part2-claude-commands' } },
                  { label: 'Workflow', translations: { ko: '워크플로우' }, autogenerate: { directory: 'tutorials/release-notes-claude/part3-workflow' } },
                  { label: 'Implementation', translations: { ko: '구현' }, autogenerate: { directory: 'tutorials/release-notes-claude/part4-implementation' } },
                  { label: 'Hands-On', translations: { ko: '실습' }, autogenerate: { directory: 'tutorials/release-notes-claude/part5-hands-on' } },
                  { label: 'Appendix', translations: { ko: '부록' }, collapsed: true, autogenerate: { directory: 'tutorials/release-notes-claude/appendix' } },
                ],
              },
              {
                label: 'Specification Pattern',
                translations: { ko: '명세 패턴' },
                collapsed: true,
                items: [
                  { slug: 'tutorials/specification-pattern' },
                  { label: 'Introduction', translations: { ko: '소개' }, autogenerate: { directory: 'tutorials/specification-pattern/part0-introduction' } },
                  {
                    label: 'Specification Basics',
                    translations: { ko: '명세 기초' },
                    autogenerate: { directory: 'tutorials/specification-pattern/part1-specification-basics' },
                  },
                  {
                    label: 'Expression Specification',
                    translations: { ko: '표현식 명세' },
                    autogenerate: { directory: 'tutorials/specification-pattern/part2-expression-specification' },
                  },
                  {
                    label: 'Repository Integration',
                    translations: { ko: '리포지토리 통합' },
                    autogenerate: { directory: 'tutorials/specification-pattern/part3-repository-integration' },
                  },
                  {
                    label: 'Real-World Patterns',
                    translations: { ko: '실전 패턴' },
                    autogenerate: { directory: 'tutorials/specification-pattern/part4-real-world-patterns' },
                  },
                  {
                    label: 'Domain Examples',
                    translations: { ko: '도메인 예제' },
                    autogenerate: { directory: 'tutorials/specification-pattern/part5-domain-examples' },
                  },
                  { label: 'Appendix', translations: { ko: '부록' }, collapsed: true, autogenerate: { directory: 'tutorials/specification-pattern/appendix' } },
                ],
              },
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
    mdx(),
  ],
});
