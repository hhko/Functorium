---
title: Functorium {VERSION} 새로운 기능
description: Functorium {VERSION}의 새로운 기능을 알아봅니다.
date: {DATE}
---

# Functorium Release {VERSION}

## 개요

{버전 소개 - 이 릴리스의 주요 목표와 테마}

**주요 기능**:

- **{기능1 카테고리}**: {한 줄 설명}
- **{기능2 카테고리}**: {한 줄 설명}
- **{기능3 카테고리}**: {한 줄 설명}

## Breaking Changes

{Breaking Changes가 없는 경우}
이번 릴리스에는 Breaking Changes가 없습니다.

{Breaking Changes가 있는 경우 - 아래 구조 사용}
<!--
### {변경된 API/기능명}

{변경 내용 설명}

**이전 ({이전 버전})**:
```csharp
{이전 코드}
```

**이후 ({현재 버전})**:
```csharp
{새 코드}
```

**마이그레이션 가이드**:
1. {단계 1}
2. {단계 2}
3. {단계 3}
-->

## 새로운 기능

### {컴포넌트명} 라이브러리

#### 1. {기능명}

{기능 설명 - What: 무엇을 하는가?}

```csharp
{코드 샘플 - How: 어떻게 사용하는가?}
```

**Why this matters (왜 중요한가):**
- {해결하는 문제 - 이 기능이 없으면 개발자가 직면하는 문제}
- {개발자 생산성 - 시간 절약, 보일러플레이트 감소}
- {코드 품질 향상 - 타입 안전성, 유지보수성, 가독성}
- {정량적 이점 - 가능한 경우: 50줄 → 5줄, 10분 → 1분}

<!-- 관련 커밋: {SHA} {커밋 메시지} -->

---

#### 2. {기능명}

{기능 설명}

```csharp
{코드 샘플}
```

**Why this matters (왜 중요한가):**
- {이점 1}
- {이점 2}
- {이점 3}

<!-- 관련 커밋: {SHA} {커밋 메시지} -->

---

### {다른 컴포넌트명} 라이브러리

#### 1. {기능명}

{기능 설명}

```csharp
{코드 샘플}
```

**Why this matters (왜 중요한가):**
- {이점 1}
- {이점 2}

<!-- 관련 커밋: {SHA} {커밋 메시지} -->

## 버그 수정

{버그 수정이 없는 경우 이 섹션 삭제}

- {버그 설명} ({SHA})
- {버그 설명} ({SHA})

## API 변경사항

### {컴포넌트명} 네임스페이스 구조

```
{Namespace.Root}
├── {SubNamespace1}/
│   ├── {Class1}
│   └── {Class2}
└── {SubNamespace2}/
    └── {Class3}
```

## 설치

### NuGet 패키지 설치

```bash
# {패키지명} 핵심 라이브러리
dotnet add package {PackageName} --version {VERSION}

# {패키지명} 테스트 라이브러리 (선택적)
dotnet add package {PackageName}.Testing --version {VERSION}
```

### 필수 의존성

- .NET {버전} 이상
- {의존성 1}
- {의존성 2}

<!--
============================================================
템플릿 사용 가이드
============================================================

1. {VERSION}을 실제 버전으로 교체 (예: v1.0.0)
2. {DATE}를 오늘 날짜로 교체 (예: 2025-12-19)
3. 각 섹션의 {placeholder}를 실제 내용으로 교체
4. 주석 (`<!-- -->` 형식)은 최종 문서에서 삭제
5. 해당 없는 섹션은 삭제

필수 체크리스트:
- [ ] 프론트매터 완성
- [ ] 개요 섹션 작성
- [ ] Breaking Changes 확인 (api-changes-diff.txt)
- [ ] 모든 feat 커밋에 대한 기능 문서화
- [ ] 모든 fix 커밋에 대한 버그 수정 문서화
- [ ] 모든 기능에 "Why this matters" 섹션 포함
- [ ] 모든 코드 샘플이 Uber 파일에서 검증됨
- [ ] 커밋 SHA 주석 추가

참조 문서:
- 작성 규칙: .release-notes/scripts/docs/phase4-writing.md
- 검증 기준: .release-notes/scripts/docs/phase5-validation.md
- Uber 파일: .analysis-output/api-changes-build-current/all-api-changes.txt
-->
