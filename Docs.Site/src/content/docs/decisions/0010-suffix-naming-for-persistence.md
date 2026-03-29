---
title: "ADR-0010: 영속성 클래스 접미사 네이밍 패턴"
status: "accepted"
date: 2026-03-27
---

## 맥락과 문제

영속성 계층의 클래스 이름을 지을 때 기술 변형(EfCore, Dapper 등)을 접두사로 붙일지 접미사로 붙일지에 따라 파일 탐색 경험이 달라진다. 예를 들어 `EfCoreProductRepository`는 기술별로 그룹화되지만, `ProductRepositoryEfCore`는 도메인 주제(Product)별로 그룹화된다.

IDE의 파일 탐색기, Go to File, 자동완성에서 같은 도메인 주제의 파일을 빠르게 찾으려면 주제가 이름 앞에 와야 한다. 프로젝트에 37개의 영속성 클래스가 있어 일관된 네이밍 규칙이 필요했다.

## 검토한 옵션

- **옵션 1**: 접두사 패턴 (`EfCoreProductRepository`, `DapperProductQueryAdapter`)
- **옵션 2**: 접미사 패턴 (`ProductRepositoryEfCore`, `ProductQueryAdapterDapper`)
- **옵션 3**: 기술별 폴더 분리 (`EfCore/ProductRepository`, `Dapper/ProductQueryAdapter`)

## 결정

**옵션 2: `{Subject}{Role}{Variant}` 접미사 패턴을 채택하고, Aggregate/CQRS 폴더 구조와 결합한다.**

- **Subject**: 도메인 주제 (예: `Product`, `Order`)
- **Role**: 역할 (예: `Repository`, `QueryAdapter`, `Configuration`)
- **Variant**: 기술 변형 (예: `EfCore`, `Dapper`, `InMemory`)

폴더 구조는 Aggregate 단위로 나누고, CQRS 패턴에 따라 Command/Query를 분리한다. 이를 통해 도메인 주제별로 관련 파일이 모이면서도 기술 변형을 명확히 식별할 수 있다.

### 결과

- **긍정적**: IDE에서 도메인 주제명으로 검색하면 관련 영속성 클래스가 함께 나열된다. 자동완성 시 주제를 먼저 입력하면 해당 도메인의 모든 구현체를 확인할 수 있다. 37개 클래스를 일관된 규칙으로 리네임하여 코드베이스 전체의 가독성이 향상되었다.
- **부정적**: 기존 코드와 문서의 클래스명을 모두 갱신해야 했다. 접미사 패턴에 익숙하지 않은 팀원에게 초기 학습이 필요하다.

### 확인

- 모든 영속성 클래스가 `{Subject}{Role}{Variant}` 패턴을 따르는지 네이밍 규칙 테스트로 검증한다.
- IDE의 Go to File에서 도메인 주제명으로 검색 시 관련 파일이 그룹화되어 나타나는지 확인한다.

## 옵션별 장단점

### 옵션 1: 접두사 패턴

- **장점**: 기술별로 그룹화되어 특정 기술의 모든 구현체를 한눈에 볼 수 있다. 기술 교체 시 영향 범위를 파악하기 쉽다.
- **단점**: IDE에서 도메인 주제로 검색하면 여러 기술 구현이 흩어져 나타난다. 자동완성 시 기술명을 먼저 입력해야 하므로 비즈니스 맥락에서의 탐색이 불편하다. 같은 Aggregate의 Repository, Configuration, QueryAdapter가 분산된다.

### 옵션 2: 접미사 패턴

- **장점**: 도메인 주제별로 자연스럽게 그룹화된다. IDE의 알파벳 정렬에서 같은 주제의 파일이 인접한다. 자동완성 시 비즈니스 용어를 먼저 입력하는 자연스러운 흐름과 일치한다. Aggregate 폴더와 결합하면 비즈니스 경계와 기술 구현이 동시에 드러난다.
- **단점**: 기술별 전체 목록을 보려면 별도 검색이 필요하다. 접미사가 길어질 수 있다(예: `ProductRepositoryEfCore`).

### 옵션 3: 기술별 폴더 분리

- **장점**: 폴더 단위로 기술 의존성이 명확히 분리된다. 기술 교체 시 폴더 단위로 작업할 수 있다.
- **단점**: 같은 도메인 주제의 Command Repository와 Query Adapter가 다른 폴더에 위치하여 비즈니스 맥락이 단절된다. Aggregate 경계를 넘나들며 파일을 열어야 하므로 인지 부하가 증가한다. CQRS 패턴과 기술 분리가 이중 폴더 계층을 만들어 깊이가 깊어진다.

## 관련 정보

- 커밋: a6a70539 (37개 클래스 리네임), f32318d0
