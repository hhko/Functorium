# Functorium 릴리스 노트 생성 문서

이 디렉터리는 전문적인 Functorium 릴리스 노트를 생성하기 위한 모듈화된 문서를 포함합니다.

## 문서 구조

- **[data-collection.md](data-collection.md)** - 데이터 수집 단계 및 출력 검증
- **[commit-analysis.md](commit-analysis.md)** - 커밋 분석 및 기능 추출 가이드
- **[api-documentation.md](api-documentation.md)** - API 검증, 코드 샘플, 정확성 가이드라인
- **[writing-guidelines.md](writing-guidelines.md)** - 문서 스타일, 구조, 템플릿 요구사항
- **[validation-checklist.md](validation-checklist.md)** - 성공 기준 및 검증 프로세스

## 빠른 시작

1. [data-collection.md](data-collection.md)에서 데이터 수집 프로세스 확인
2. [commit-analysis.md](commit-analysis.md)에서 변경사항 분석 방법 확인
3. [api-documentation.md](api-documentation.md)에서 정확한 코드 샘플 작성법 확인
4. [writing-guidelines.md](writing-guidelines.md)에서 적절한 포맷팅 적용
5. [validation-checklist.md](validation-checklist.md)로 검증

## 핵심 원칙

> **정확성 우선**: 모든 문서화된 API는 Uber 파일에 존재해야 합니다.
> - API를 임의로 만들어내지 않습니다
> - 모든 기능은 커밋/PR로 추적 가능해야 합니다
