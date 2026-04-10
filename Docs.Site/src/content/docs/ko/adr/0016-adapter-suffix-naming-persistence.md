---
title: "ADR-0016: Adapter - 영속성 클래스 접미사 네이밍 패턴"
status: "accepted"
date: 2026-03-27
---

## 맥락과 문제

Product Aggregate의 Repository를 수정하려고 IDE에서 `Ctrl+P`(Go to File)를 열고 "Product"를 입력하면, `EfCoreProductRepository`, `DapperProductQueryAdapter`, `InMemoryProductRepository`가 각각 E, D, I 위치에 흩어져 나타났다. 같은 도메인 주제의 파일을 찾으려면 기술 접두사를 일일이 기억해야 했고, 자동완성에서도 "EfCore..."를 먼저 타이핑해야 원하는 클래스에 도달할 수 있었다. 개발자의 사고 흐름은 "Product의 Repository를 찾겠다"인데, 네이밍이 "EfCore 중에서 Product를 찾겠다"를 강요하고 있었다.

프로젝트에 37개의 영속성 클래스가 존재했고, 접두사와 접미사 패턴이 혼재하여 파일 탐색 경험이 일관되지 않았다. 도메인 주제 중심의 탐색을 지원하는 통일된 네이밍 규칙이 필요했다.

## 검토한 옵션

- **옵션 1**: 접두사 패턴 (`EfCoreProductRepository`, `DapperProductQueryAdapter`)
- **옵션 2**: 접미사 패턴 (`ProductRepositoryEfCore`, `ProductQueryAdapterDapper`)
- **옵션 3**: 기술별 폴더 분리 (`EfCore/ProductRepository`, `Dapper/ProductQueryAdapter`)

## 결정

**옵션 2: `{Subject}{Role}{Variant}` 접미사 패턴을 채택하고, Aggregate/CQRS 폴더 구조와 결합한다.**

개발자가 코드를 탐색할 때 먼저 떠올리는 것은 기술(EfCore)이 아니라 도메인 주제(Product)다. 네이밍이 이 사고 흐름을 따라야 한다.

- **Subject**: 도메인 주제 (예: `Product`, `Order`)
- **Role**: 역할 (예: `Repository`, `QueryAdapter`, `Configuration`)
- **Variant**: 기술 변형 (예: `EfCore`, `Dapper`, `InMemory`)

폴더 구조는 Aggregate 단위로 나누고, CQRS 패턴에 따라 Command/Query를 분리한다. `Product`를 입력하면 `ProductRepositoryEfCore`, `ProductQueryAdapterDapper`, `ProductConfigurationEfCore`가 연속으로 나열되어, 해당 Aggregate의 영속성 구현 전체를 한눈에 파악할 수 있다.

### 결과

- **긍정적**: IDE에서 "Product"를 입력하면 Repository, QueryAdapter, Configuration이 알파벳순으로 인접하여 나타난다. 37개 클래스를 일괄 리네임한 결과, Go to File과 자동완성에서 도메인 주제 중심의 탐색이 즉시 가능해졌다. 새로 합류한 팀원이 특정 Aggregate의 영속성 구현 전체를 파악하는 시간이 단축되었다.
- **부정적**: 37개 클래스의 이름 변경과 함께 모든 참조, 테스트, 문서를 일괄 갱신하는 대규모 리네임 작업이 필요했다. "EfCore 기술의 모든 구현체"를 한눈에 보려면 기술명으로 별도 검색해야 한다.

### 확인

- 모든 영속성 클래스가 `{Subject}{Role}{Variant}` 패턴을 따르는지 네이밍 규칙 테스트로 검증한다.
- IDE의 Go to File에서 도메인 주제명으로 검색 시 관련 파일이 그룹화되어 나타나는지 확인한다.

## 옵션별 장단점

### 옵션 1: 접두사 패턴

- **장점**: "EfCore"를 입력하면 EfCore 기반 구현체가 모두 나열되어, 기술 교체 시 영향 범위를 한눈에 파악할 수 있다.
- **단점**: "Product"를 입력하면 `DapperProductQueryAdapter`(D), `EfCoreProductRepository`(E), `InMemoryProductRepository`(I) 등이 알파벳순으로 흩어진다. 같은 Aggregate의 Repository, Configuration, QueryAdapter가 분산되어 도메인 맥락에서의 탐색이 불편하다. 자동완성에서 기술명을 먼저 타이핑해야 하므로 개발자의 사고 흐름("Product의 Repository를 찾겠다")과 어긋난다.

### 옵션 2: 접미사 패턴

- **장점**: "Product"를 입력하면 `ProductConfigurationEfCore`, `ProductQueryAdapterDapper`, `ProductRepositoryEfCore`가 연속으로 나열된다. 자동완성에서 비즈니스 용어를 먼저 입력하는 개발자의 자연스러운 사고 흐름과 일치한다. Aggregate 폴더와 결합하면 파일 탐색기에서도 비즈니스 경계와 기술 구현이 동시에 드러난다.
- **단점**: "EfCore 기술의 전체 구현체"를 보려면 기술명으로 별도 검색해야 한다. 접미사가 길어질 수 있다(예: `ProductRepositoryEfCore`).

### 옵션 3: 기술별 폴더 분리

- **장점**: `EfCore/`, `Dapper/` 폴더 단위로 기술 의존성이 물리적으로 분리되어 기술 교체 시 폴더 단위로 작업할 수 있다.
- **단점**: Product의 Command Repository(`EfCore/ProductRepository`)와 Query Adapter(`Dapper/ProductQueryAdapter`)가 서로 다른 폴더에 위치하여, 하나의 Aggregate를 파악하려면 여러 폴더를 넘나들어야 한다. CQRS의 Command/Query 분리와 기술별 폴더 분리가 이중 계층을 만들어 폴더 깊이가 깊어지고 인지 부하가 증가한다.

## 관련 정보

- 커밋: a6a70539 (37개 클래스 리네임), f32318d0
