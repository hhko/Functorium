# Functorium 가이드 문서

이 폴더는 Functorium 프레임워크 사용을 위한 Claude Code 가이드 문서를 포함합니다.

## 문서 목록

| 문서 | 설명 |
|------|------|
| [domain-modeling-overview.md](./domain-modeling-overview.md) | 도메인 모델링 개요 |
| [valueobject-guide.md](./valueobject-guide.md) | 값 객체 구현 및 검증 패턴 |
| [entity-guide.md](./entity-guide.md) | Entity 및 Aggregate Root 구현 |
| [error-guide.md](./error-guide.md) | 레이어별 에러 시스템 (정의, 네이밍) |
| [error-testing-guide.md](./error-testing-guide.md) | 에러 테스트 패턴 |
| [unit-testing-guide.md](./unit-testing-guide.md) | 단위 테스트 규칙 |

## 문서 구조

```
domain-modeling-overview.md (개요)
│
├── valueobject-guide.md (값 객체 구현)
│       └── error-guide.md 참조 (에러 타입)
│
├── entity-guide.md (Entity 구현)
│       └── valueobject-guide.md 참조 (검증 예시)
│
├── error-guide.md (에러 정의/네이밍)
│       └── valueobject-guide.md 참조 (검증 예시)
│
└── error-testing-guide.md (테스트)
        ├── error-guide.md 참조 (에러 타입)
        └── valueobject-guide.md 참조 (값 객체 예시)
```

## 빠른 참조

- **값 객체 만들기**: [valueobject-guide.md](./valueobject-guide.md)
- **Entity 만들기**: [entity-guide.md](./entity-guide.md)
- **검증 메서드**: [valueobject-guide.md#validationrulest-시작점](./valueobject-guide.md#validationrulest-시작점)
- **에러 타입**: [error-guide.md](./error-guide.md)
- **에러 테스트**: [error-testing-guide.md](./error-testing-guide.md)
