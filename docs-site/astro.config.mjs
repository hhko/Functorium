import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import starlightImageZoom from 'starlight-image-zoom';
import starlightLinksValidator from 'starlight-links-validator';
import starlightSidebarTopics from 'starlight-sidebar-topics';

export default defineConfig({
  site: 'https://hhko.github.io',
  base: '/Functorium',
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
                label: '소개',
                items: [
                  { label: '개요', slug: '' },
                  { label: '주요 핵심 기능', slug: 'key-features', badge: { text: '핵심', variant: 'tip' } },
                  { label: '설계 동기', slug: 'motivation' },
                ],
              },
              {
                label: '가이드',
                items: [
                  { label: '설계 철학', slug: 'design-philosophy' },
                  { label: '구조 개요', slug: 'architecture' },
                  { label: '품질 전략과 기대 효과', slug: 'quality-and-benefits' },
                  { label: '시작하기', slug: 'getting-started', badge: { text: '시작', variant: 'note' } },
                ],
              },
              {
                label: '아키텍처',
                autogenerate: { directory: 'architecture-is' },
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
            link: '/tutorials/tutorial-writing-guide/',
            icon: 'puzzle',
            items: [
              { label: '튜토리얼 작성 가이드', slug: 'tutorials/tutorial-writing-guide' },
              {
                label: '함수형 값 객체 구현',
                collapsed: true,
                items: [
                  { slug: 'tutorials/functional-valueobject' },
                  { label: '소개', autogenerate: { directory: 'tutorials/functional-valueobject/Part0-Introduction' } },
                  { label: '값 객체 개념', autogenerate: { directory: 'tutorials/functional-valueobject/Part1-ValueObject-Concepts' } },
                  { label: '검증 패턴', autogenerate: { directory: 'tutorials/functional-valueobject/Part2-Validation-Patterns' } },
                  { label: '값 객체 패턴', autogenerate: { directory: 'tutorials/functional-valueobject/Part3-ValueObject-Patterns' } },
                  { label: '실전 가이드', autogenerate: { directory: 'tutorials/functional-valueobject/Part4-Practical-Guide' } },
                  { label: '도메인 예제', autogenerate: { directory: 'tutorials/functional-valueobject/Part5-Domain-Examples' } },
                  { label: '부록', collapsed: true, autogenerate: { directory: 'tutorials/functional-valueobject/Appendix' } },
                ],
              },
              {
                label: '소스 생성기 관측 가능성',
                collapsed: true,
                items: [
                  { slug: 'tutorials/sourcegen-observability' },
                  { label: '소개', autogenerate: { directory: 'tutorials/sourcegen-observability/Part0-Introduction' } },
                  { label: '기초', autogenerate: { directory: 'tutorials/sourcegen-observability/Part1-Fundamentals' } },
                  { label: '핵심 개념', autogenerate: { directory: 'tutorials/sourcegen-observability/Part2-Core-Concepts' } },
                  { label: '고급', autogenerate: { directory: 'tutorials/sourcegen-observability/Part3-Advanced' } },
                  { label: '쿡북', autogenerate: { directory: 'tutorials/sourcegen-observability/Part4-Cookbook' } },
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
                  { label: '제네릭 변성 기초', autogenerate: { directory: 'tutorials/usecase-pipeline/Part1-Generic-Variance-Foundations' } },
                  { label: '문제 정의', autogenerate: { directory: 'tutorials/usecase-pipeline/Part2-Problem-Definition' } },
                  { label: '응답 인터페이스 계층', autogenerate: { directory: 'tutorials/usecase-pipeline/Part3-IFinResponse-Hierarchy' } },
                  { label: '파이프라인 제약 패턴', autogenerate: { directory: 'tutorials/usecase-pipeline/Part4-Pipeline-Constraint-Patterns' } },
                  { label: '실전 유스케이스 예제', autogenerate: { directory: 'tutorials/usecase-pipeline/Part5-Practical-Usecase-Examples' } },
                  { label: '부록', collapsed: true, autogenerate: { directory: 'tutorials/usecase-pipeline/Appendix' } },
                ],
              },
              {
                label: '아키텍처 규칙 테스트',
                collapsed: true,
                items: [
                  { slug: 'tutorials/architecture-rules' },
                  { label: '소개', autogenerate: { directory: 'tutorials/architecture-rules/Part0-Introduction' } },
                  { label: '클래스 검증기 기초', autogenerate: { directory: 'tutorials/architecture-rules/Part1-ClassValidator-Basics' } },
                  { label: '메서드/속성 검증', autogenerate: { directory: 'tutorials/architecture-rules/Part2-Method-And-Property-Validation' } },
                  { label: '고급 검증', autogenerate: { directory: 'tutorials/architecture-rules/Part3-Advanced-Validation' } },
                  { label: '실전 패턴', autogenerate: { directory: 'tutorials/architecture-rules/Part4-Real-World-Patterns' } },
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
                  { label: '도메인 엔티티 기초', autogenerate: { directory: 'tutorials/cqrs-repository/Part1-Domain-Entity-Foundations' } },
                  { label: '커맨드 리포지토리', autogenerate: { directory: 'tutorials/cqrs-repository/Part2-Command-Repository' } },
                  { label: '쿼리 패턴', autogenerate: { directory: 'tutorials/cqrs-repository/Part3-Query-Patterns' } },
                  { label: 'CQRS 유스케이스 통합', autogenerate: { directory: 'tutorials/cqrs-repository/Part4-CQRS-Usecase-Integration' } },
                  { label: '도메인 예제', autogenerate: { directory: 'tutorials/cqrs-repository/Part5-Domain-Examples' } },
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
                  { label: '명세 기초', autogenerate: { directory: 'tutorials/specification-pattern/Part1-Specification-Basics' } },
                  { label: '표현식 명세', autogenerate: { directory: 'tutorials/specification-pattern/Part2-Expression-Specification' } },
                  { label: '리포지토리 통합', autogenerate: { directory: 'tutorials/specification-pattern/Part3-Repository-Integration' } },
                  { label: '실전 패턴', autogenerate: { directory: 'tutorials/specification-pattern/Part4-Real-World-Patterns' } },
                  { label: '도메인 예제', autogenerate: { directory: 'tutorials/specification-pattern/Part5-Domain-Examples' } },
                  { label: '부록', collapsed: true, autogenerate: { directory: 'tutorials/specification-pattern/Appendix' } },
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
  ],
});
