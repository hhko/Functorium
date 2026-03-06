---
title: "참고 자료"
---
## 공식 문서

### Functorium
- **GitHub**: https://github.com/your-org/functorium
- **Specification 타입**: `Src/Functorium/Domains/Specifications/`

### .NET Expression Trees
- **공식 문서**: https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/expression-trees/
- **Expression Trees 심화**: https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/expression-trees/expression-trees-building

### ArchUnitNET
- **GitHub**: https://github.com/TNG/ArchUnitNET
- **NuGet**: https://www.nuget.org/packages/TngTech.ArchUnitNET.xUnit

---

## 참고 자료

### Domain-Driven Design
- **제목**: Domain-Driven Design: Tackling Complexity in the Heart of Software
- **저자**: Eric Evans
- **출판**: Addison-Wesley, 2003
- **핵심 내용**: Specification 패턴의 원론적 정의, Repository 패턴

### Implementing Domain-Driven Design
- **제목**: Implementing Domain-Driven Design
- **저자**: Vaughn Vernon
- **출판**: Addison-Wesley, 2013
- **핵심 내용**: DDD 실전 구현 패턴, Specification 활용 예제

### Patterns of Enterprise Application Architecture
- **제목**: Patterns of Enterprise Application Architecture
- **저자**: Martin Fowler
- **출판**: Addison-Wesley, 2002
- **핵심 내용**: Specification 패턴, Repository 패턴의 체계적 정의

### Functional Programming in C#
- **제목**: Functional Programming in C#: How to write better C# code
- **저자**: Enrico Buonanno
- **출판**: Manning, 2022 (2nd Edition)
- **핵심 내용**: Expression Tree, 함수형 조합 패턴

---

## 온라인 리소스

### 블로그 & 아티클

**Specification Pattern**
- https://www.martinfowler.com/apsupp/spec.pdf
- Eric Evans와 Martin Fowler의 Specification 패턴 원본 논문

**Specification Pattern in DDD**
- https://enterprisecraftsmanship.com/posts/specification-pattern-always-valid-domain-model/
- Vladimir Khorikov의 Specification 패턴과 항상 유효한 도메인 모델

**Expression Trees in C#**
- https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/expression-trees/
- .NET 공식 Expression Tree 가이드

---

## 관련 라이브러리

### Specification 패턴

| 라이브러리 | 설명 | NuGet |
|-----------|------|-------|
| Functorium | 본 튜토리얼의 Specification 프레임워크 | ✅ |
| Ardalis.Specification | Steve Smith의 Specification 라이브러리 | ✅ |

### DDD & Clean Architecture

| 라이브러리 | 설명 | NuGet |
|-----------|------|-------|
| MediatR | CQRS/Mediator 패턴 | ✅ |
| FluentValidation | 검증 라이브러리 | ✅ |
| LanguageExt.Core | 함수형 프로그래밍 타입 | ✅ |

### ORM

| 라이브러리 | 설명 | NuGet |
|-----------|------|-------|
| Microsoft.EntityFrameworkCore | EF Core ORM | ✅ |
| Microsoft.EntityFrameworkCore.InMemory | InMemory 테스트용 | ✅ |

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
Src/Functorium/Domains/Specifications/
├── Specification.cs                       # 기본 추상 클래스
├── ExpressionSpecification.cs             # Expression Tree 지원
├── SpecificationExpressionResolver.cs     # Expression 합성
└── PropertyMap.cs                         # Entity→Model 변환
```

### 튜토리얼 프로젝트

```
Docs/tutorials/Implementing-Specification-Pattern/
├── Part1-Specification-Basics/            # 기초 (4개)
├── Part2-Expression-Specification/        # Expression (4개)
├── Part3-Repository-Integration/          # Repository 통합 (4개)
├── Part4-Real-World-Patterns/             # 실전 패턴 (4개)
└── Part5-Domain-Examples/                 # 도메인 예제 (2개)
```

---

## 추가 학습 권장 순서

### 초급자 (패턴 입문)
1. Eric Evans DDD 책의 Specification 챕터
2. 본 튜토리얼 Part 1 (1~4장)
3. Functorium Specification 소스 코드 읽기

### 중급자 (실전 적용)
1. .NET Expression Trees 공식 문서
2. 본 튜토리얼 Part 2~3
3. Ardalis.Specification 비교 분석

### 고급자 (아키텍처 설계)
1. "Implementing Domain-Driven Design" 책
2. 본 튜토리얼 Part 4~5
3. ArchUnitNET으로 아키텍처 테스트 작성

---

이 튜토리얼은 Functorium 프로젝트의 실제 Specification 프레임워크 개발 경험을 바탕으로 작성되었습니다.
