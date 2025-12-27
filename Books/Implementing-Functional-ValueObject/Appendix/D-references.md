# 부록 D. 참고 자료

> **부록** | [← 이전: C. 용어집](C-glossary.md) | [목차](../README.md) | [다음: E. FAQ →](E-faq.md)

---

## 공식 문서

### LanguageExt
- **GitHub**: https://github.com/louthy/language-ext
- **문서**: https://languageext.readthedocs.io/
- **NuGet**: https://www.nuget.org/packages/LanguageExt.Core

### Ardalis.SmartEnum
- **GitHub**: https://github.com/ardalis/SmartEnum
- **NuGet**: https://www.nuget.org/packages/Ardalis.SmartEnum

### ArchUnitNET
- **GitHub**: https://github.com/TNG/ArchUnitNET
- **NuGet**: https://www.nuget.org/packages/TngTech.ArchUnitNET.xUnit

---

## 도서

### Domain-Driven Design
- **제목**: Domain-Driven Design: Tackling Complexity in the Heart of Software
- **저자**: Eric Evans
- **출판**: Addison-Wesley, 2003
- **핵심 내용**: 값 객체, 엔티티, 애그리게이트의 원론적 정의

### Implementing Domain-Driven Design
- **제목**: Implementing Domain-Driven Design
- **저자**: Vaughn Vernon
- **출판**: Addison-Wesley, 2013
- **핵심 내용**: DDD 실전 구현 패턴, 값 객체 심화

### Functional Programming in C#
- **제목**: Functional Programming in C#: How to write better C# code
- **저자**: Enrico Buonanno
- **출판**: Manning, 2022 (2nd Edition)
- **핵심 내용**: `Option`, `Either`, Railway Oriented Programming

### Domain Modeling Made Functional
- **제목**: Domain Modeling Made Functional
- **저자**: Scott Wlaschin
- **출판**: Pragmatic Bookshelf, 2018
- **핵심 내용**: 함수형 DDD, 타입 주도 설계

---

## 온라인 리소스

### 블로그 & 아티클

**Railway Oriented Programming**
- https://fsharpforfunandprofit.com/rop/
- Scott Wlaschin의 오류 처리 패턴 시리즈

**값 객체 패턴**
- https://enterprisecraftsmanship.com/posts/value-objects-explained/
- Vladimir Khorikov의 값 객체 심층 분석

**Always Valid Domain Model**
- https://enterprisecraftsmanship.com/posts/always-valid-domain-model/
- 항상 유효한 도메인 모델 설계

### 비디오

**NDC Conferences**
- "Functional Programming in C#" - Enrico Buonanno
- "Domain Modeling Made Functional" - Scott Wlaschin

**Pluralsight**
- "Domain-Driven Design Fundamentals"
- "Applying Functional Principles in C#"

---

## 관련 라이브러리

### 함수형 프로그래밍

| 라이브러리 | 설명 | NuGet |
|-----------|------|-------|
| LanguageExt.Core | 핵심 함수형 타입 | ✅ |
| LanguageExt.Sys | 부수 효과 관리 | ✅ |
| CSharpFunctionalExtensions | 경량 Result 타입 | ✅ |
| Optional | 간단한 Option 구현 | ✅ |

### DDD & Clean Architecture

| 라이브러리 | 설명 | NuGet |
|-----------|------|-------|
| MediatR | CQRS/Mediator 패턴 | ✅ |
| Ardalis.Specification | Repository 패턴 | ✅ |
| FluentValidation | 검증 라이브러리 | ✅ |
| ErrorOr | 오류/결과 타입 | ✅ |

### 테스트

| 라이브러리 | 설명 | NuGet |
|-----------|------|-------|
| ArchUnitNET | 아키텍처 테스트 | ✅ |
| Shouldly | 단언문 라이브러리 | ✅ |
| xUnit | 테스트 프레임워크 | ✅ |
| NSubstitute | 목 라이브러리 | ✅ |

---

## Functorium 프로젝트 참조

### 소스 코드 위치

```
Src/Functorium/Domains/ValueObjects/
├── IValueObject.cs                    # 마커 인터페이스
├── AbstractValueObject.cs             # 기본 추상 클래스
├── ValueObject.cs                     # 복합 값 객체
├── SimpleValueObject.cs               # 단일 값 래퍼
├── ComparableValueObject.cs           # 비교 가능 복합
└── ComparableSimpleValueObject.cs     # 비교 가능 단일
```

### CQRS 통합

```
Src/Functorium/Applications/Cqrs/
├── FinExtensions.cs                   # Fin<T> → Response 변환
└── ValidationExtensions.cs            # Validation → Response 변환
```

### 튜토리얼 프로젝트

```
Books/Functional-ValueObject/
├── 01-Concept/                        # Part 1: 개념 (14개)
├── 02-Validation/                     # Part 2: 검증 (5개)
└── 03-Patterns/                       # Part 3: 패턴 (8개)
```

---

## 추가 학습 권장 순서

### 초급자 (함수형 입문)
1. LanguageExt 공식 문서의 Quick Start
2. "Functional Programming in C#" 책 1-5장
3. 본 도서 Part 1 (1-6장)

### 중급자 (실전 적용)
1. "Domain Modeling Made Functional" 책
2. 본 도서 Part 2-3
3. Railway Oriented Programming 블로그

### 고급자 (아키텍처 설계)
1. "Implementing Domain-Driven Design" 책
2. 본 도서 Part 4-5
3. ArchUnitNET으로 아키텍처 테스트 작성

---

## 커뮤니티

### GitHub Discussions
- LanguageExt: https://github.com/louthy/language-ext/discussions
- Functorium: https://github.com/your-org/functorium/discussions

### Stack Overflow 태그
- `languageext`
- `domain-driven-design`
- `value-objects`
- `functional-programming`

---

## 다음 단계

FAQ를 확인합니다.

→ [E. FAQ](E-faq.md)
