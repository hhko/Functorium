# Functorium 가이드 문서

이 폴더는 Functorium 프레임워크 사용을 위한 Claude Code 가이드 문서를 포함합니다.

## 문서 목록

| 문서 | 설명 |
|------|------|
| [domain-modeling-overview.md](./domain-modeling-overview.md) | 도메인 모델링 개요 |
| [valueobject-guide.md](./valueobject-guide.md) | 값 객체 구현 및 검증 패턴 |
| [entity-guide.md](./entity-guide.md) | Entity 및 Aggregate Root 구현 |
| [usecase-implementation-guide.md](./usecase-implementation-guide.md) | 유스케이스 구현 (CQRS Command/Query) |
| [error-guide.md](./error-guide.md) | 레이어별 에러 시스템 (정의, 네이밍) |
| [adapter-guide.md](./adapter-guide.md) | Adapter 구현 (Repository, 외부 API) |
| [adapter-implementation-activity-guide.md](./adapter-implementation-activity-guide.md) | Adapter 구현 활동 가이드 (단계별 실행) |
| [unit-testing-guide.md](./unit-testing-guide.md) | 단위 테스트 규칙 |
| [error-testing-guide.md](./error-testing-guide.md) | 에러 테스트 패턴 |
| [observability-spec.md](./observability-spec.md) | Observability 사양 (Field/Tag, Meter, 메시지 템플릿) |

## 문서 구조

```
domain-modeling-overview.md (개요)
│
├── Domain Layer
│   ├── valueobject-guide.md (값 객체)
│   └── entity-guide.md (Entity/Aggregate)
│
├── Application Layer
│   ├── usecase-implementation-guide.md (Command/Query)
│   └── error-guide.md (에러 정의)
│
├── Adapter Layer
│   ├── adapter-guide.md (Repository/외부 API)
│   └── adapter-implementation-activity-guide.md (구현 활동 가이드)
│
├── Observability
│   ├── observability-spec.md (사양)
│   └── observability-field-naming-guide.md (필드 이름 규칙)
│
└── Testing
    ├── unit-testing-guide.md (단위 테스트)
    └── error-testing-guide.md (에러 테스트)
```

## 빠른 참조

- **값 객체 만들기**: [valueobject-guide.md](./valueobject-guide.md)
- **Entity 만들기**: [entity-guide.md](./entity-guide.md)
- **Usecase 만들기**: [usecase-implementation-guide.md](./usecase-implementation-guide.md)
- **Event Handler 만들기**: [usecase-implementation-guide.md#event-handler-구현](./usecase-implementation-guide.md#event-handler-구현)
- **Adapter 만들기**: [adapter-guide.md](./adapter-guide.md)
- **Adapter 구현 활동**: [adapter-implementation-activity-guide.md](./adapter-implementation-activity-guide.md)
- **검증 메서드**: [valueobject-guide.md#validationrulest-시작점](./valueobject-guide.md#validationrulest-시작점)
- **에러 타입**: [error-guide.md](./error-guide.md)
- **에러 테스트**: [error-testing-guide.md](./error-testing-guide.md)
- **Observability 사양**: [observability-spec.md](./observability-spec.md)
