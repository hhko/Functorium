import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import starlightImageZoom from 'starlight-image-zoom';
import starlightLinksValidator from 'starlight-links-validator';

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
        starlightLinksValidator(),
      ],
      social: [
        {
          icon: 'github',
          label: 'GitHub',
          href: 'https://github.com/hhko/Functorium',
        },
      ],
      sidebar: [
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
      ],
    }),
  ],
});
