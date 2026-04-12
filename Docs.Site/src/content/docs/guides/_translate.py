#!/usr/bin/env python3
"""Translate Korean text to English in markdown documentation files.

This script performs line-by-line and phrase-by-phrase Korean-to-English translation
using a comprehensive dictionary of known Korean phrases from the Functorium docs.
"""
import re
import sys
import os

# Comprehensive Korean to English translation dictionary
# Organized by category for maintainability
TRANSLATIONS = {
    # --- Type hierarchy / code comments (inside ```) ---
    "인터페이스": "interface",
    "추상 클래스": "abstract class",
    "동등성 컴포넌트": "equality components",
    "값 기반 동등성": "value-based equality",
    "연산자": "operators",
    "프록시 타입 처리": "proxy type handling",
    "헬퍼": "helper",
    "읽기 전용": "read-only",
    "이벤트 정리": "event cleanup",
    "이벤트 조회": "event retrieval",
    "Entity 식별자": "Entity identifier",
    "상수": "constant",
    "속성": "property",
    "ID 기반 동등성": "ID-based equality",
    "ORM 프록시 지원": "ORM proxy support",
    "기본 생성자": "default constructor",
    "선택적 지정": "optional specification",
    "읽기 전용 이벤트 조회": "read-only event retrieval",
    "공통": "common",
    "권한": "authorization",
    "검증": "validation",
    "비즈니스 규칙": "business rules",
    "커스텀": "custom",
    "외부 서비스": "external service",
    "데이터": "data",
    "조합 메서드": "composition methods",
    "연산자 오버로드": "operator overloads",
    "추상 메서드": "abstract method",
    "자동 구현": "auto-implemented",
    "추상": "abstract",
    "권장": "recommended",

    # --- Section headers ---
    "타입 계층 구조": "Type Hierarchy",
    "계층": "Hierarchy",
    "관계도": "Relationship Diagram",
    "개요": "Overview",
    "생성 패턴": "Creation Patterns",
    "핵심 패턴": "Core Pattern",
    "구현 패턴": "Implementation Patterns",
    "클래스 계층": "Class Hierarchy",
    "부가 인터페이스": "Supplementary Interfaces",

    # --- Common Korean phrases (longer first for greedy matching) ---
    "설계 철학과 핵심 개념을 이해했으니, 이제 각 빌딩블록이 실제 프로젝트의 어느 레이어에 배치되는지 알아봅니다.":
        "Now that we understand the design philosophy and core concepts, let us examine which layer each building block is placed in within an actual project.",
    "배치되는 빌딩블록": "Building blocks placed here",
    "의존성": "Dependencies",
    "없음 (가장 안쪽 레이어)": "None (innermost layer)",
    "Domain Layer만 의존": "Depends only on Domain Layer",
    "Domain Layer, Application Layer 의존": "Depends on Domain Layer and Application Layer",
    "Adapter 구현체, Pipeline (자동 생성), Adapter Error":
        "Adapter implementations, Pipeline (auto-generated), Adapter Error",
    "(바깥)           (중간)              (안쪽)":
        "(Outer)          (Middle)             (Inner)",
    "Inner layers never reference outer layers. Application Layer가 Adapter 기능이 필요할 때는 Port(인터페이스)를 정의하고, Adapter Layer가 이를 구현합니다.":
        "Inner layers never reference outer layers. When the Application Layer needs Adapter functionality, it defines a Port (interface), and the Adapter Layer implements it.",

    # --- Evans Module section ---
    "에릭 에반스는 Module을 **도메인 개념의 응집도를** 기준으로 그룹화하는 단위로 정의합니다. Module은 패키지나 네임스페이스가 아니라 **의미론적 경계입니다.**":
        "Eric Evans defines a Module as a unit for grouping based on **the cohesion of domain concepts.** A Module is not a package or namespace but a **semantic boundary.**",
    "높은 응집도": "High cohesion",
    "같은 Module 안의 요소는 하나의 도메인 개념을 표현": "Elements within the same Module express a single domain concept",
    "낮은 결합도": "Low coupling",
    "Module 간 의존은 최소화하고, 필요 시 Port/Interface로 소통": "Minimize dependencies between Modules; communicate via Port/Interface when needed",
    "커뮤니케이션": "Communication",
    "Module 이름이 유비쿼터스 언어를 반영하여 코드 구조만으로 도메인 경계 전달": "Module names reflect the ubiquitous language, conveying domain boundaries through code structure alone",

    # --- Dual axis ---
    "이중 축: Layer × Module": "Dual Axis: Layer x Module",
    "Functorium는 **Layer(수평 축)** 와 **Module(수직 축)** 의 이중 축으로 코드를 배치합니다.":
        "Functorium arranges code along a dual axis of **Layer (horizontal)** and **Module (vertical).**",
    "**Layer** — .csproj 단위. 기술적 관심사(Domain, Application, Adapter)를 분리":
        "**Layer** -- .csproj unit. Separates technical concerns (Domain, Application, Adapter)",
    "**Module** — 폴더/네임스페이스 단위. 도메인 개념(Products, Orders 등)의 응집도를 유지":
        "**Module** -- Folder/namespace unit. Maintains cohesion of domain concepts (Products, Orders, etc.)",
    "Layer (수평)": "Layer (horizontal)",
    "기술적 관심사, 의존성 방향": "Technical concerns, dependency direction",
    "Module (수직)": "Module (vertical)",
    "폴더/네임스페이스": "Folder/namespace",
    "도메인 개념 응집도": "Domain concept cohesion",

    # --- SingleHost Module ---
    "SingleHost 프로젝트의 실제 모듈 구성입니다.":
        "This is the actual module composition of the SingleHost project.",
    "공유 VO, Entity, Event": "Shared VO, Entity, Event",
    "> **패턴**: 각 Module은 Domain → Application → Adapter 전 Layer를 관통하는 **수직 슬라이스입니다.** 폴더 이름이 곧 Module 이름이고, Module 이름이 곧 유비쿼터스 언어입니다.":
        "> **Pattern**: Each Module is a **vertical slice** that cuts through all layers from Domain to Application to Adapter. The folder name is the Module name, and the Module name is the ubiquitous language.",
    "호스트 프로젝트의 도메인 계층은 다음과 같은 폴더 구조를 따릅니다.":
        "The domain layer of a host project follows this folder structure.",
    "**참조 예시** (01-SingleHost `LayeredArch.Domain/`):":
        "**Reference example** (01-SingleHost `LayeredArch.Domain/`):",
    "**구조 요약**:": "**Structure summary**:",
    "`AggregateRoots/{Aggregate}/` — 애그리거트 루트, 리포지토리 인터페이스, 하위 `Specifications/`와 `ValueObjects/`":
        "`AggregateRoots/{Aggregate}/` -- Aggregate root, repository interface, sub-folders `Specifications/` and `ValueObjects/`",
    "`SharedModels/` — 여러 애그리거트가 공유하는 `Entities/`, `Services/`, `ValueObjects/`":
        "`SharedModels/` -- `Entities/`, `Services/`, `ValueObjects/` shared across multiple Aggregates",
    "루트 — `DOMAIN-GLOSSARY.md`, `Using.cs`, `AssemblyReference.cs`":
        "Root -- `DOMAIN-GLOSSARY.md`, `Using.cs`, `AssemblyReference.cs`",

    # --- Module Cohesion Rules ---
    "**Module 내부 배치 (기본)**": "**Intra-Module placement (default)**",
    "특정 Aggregate 전용 타입 → 해당 Aggregate 폴더 내부": "Types specific to an Aggregate -> Inside that Aggregate's folder",
    "예:": "Example:",
    "**SharedModels 이동 기준**": "**Criteria for moving to SharedModels**",
    "2개 이상 Aggregate에서 공유하는 타입 → `SharedModels/`": "Types shared by 2 or more Aggregates -> `SharedModels/`",
    "**프로젝트 루트 이동 기준**": "**Criteria for moving to project root**",
    "교차 Aggregate Port → `Domain/Ports/` (예: `IProductCatalog` — Order에서 Product 검증용)":
        "Cross-Aggregate Port -> `Domain/Ports/` (e.g., `IProductCatalog` -- for Product validation from Order)",
    "Domain Service → `Domain/Services/` (예: `OrderCreditCheckService` — 교차 Aggregate 순수 로직)":
        "Domain Service -> `Domain/Services/` (e.g., `OrderCreditCheckService` -- cross-Aggregate pure logic)",
    "이 규칙의 상세 판단 기준은 [01-project-structure.md FAQ §3](../architecture/01-project-structure)을 참조하세요.":
        "For detailed criteria of this rule, refer to [01-project-structure.md FAQ #3](../architecture/01-project-structure).",

    # --- Multi-Aggregate Expansion ---
    "서비스가 성장할 때 모듈 구조의 3단계 진화 경로입니다.":
        "This is the 3-stage evolution path of module structure as the service grows.",
    "구조": "Structure",
    "1단계": "Stage 1",
    "**단일 Aggregate**": "**Single Aggregate**",
    "하나의 Aggregate가 하나의 Module. SingleHost 초기 Product 구조":
        "One Aggregate per Module. Initial Product structure in SingleHost",
    "2단계": "Stage 2",
    "**Multi-Aggregate 동일 서비스**": "**Multi-Aggregate same service**",
    "여러 Aggregate가 폴더로 분리되지만 동일 서비스(프로세스) 내 배치. SingleHost 현재 구조":
        "Multiple Aggregates separated into folders but within the same service (process). Current SingleHost structure",
    "3단계": "Stage 3",
    "**별도 Bounded Context**": "**Separate Bounded Context**",
    "Module이 독립 서비스(.sln)로 분리. Context Map 패턴 필요":
        "Module separated into an independent service (.sln). Context Map pattern required",
    "**2단계 → 3단계 분리 판단 기준:**": "**Stage 2 to Stage 3 separation criteria:**",
    "동일 서비스 유지": "Keep same service",
    "별도 서비스 분리": "Separate into distinct services",
    "배포 주기": "Deployment cycle",
    "동일": "Same",
    "Module별 독립 배포 필요": "Independent deployment per Module needed",
    "트랜잭션 경계": "Transaction boundary",
    "Aggregate 간 같은 DB 공유 가능": "Aggregates can share the same DB",
    "독립 DB/스키마 필요": "Independent DB/schema needed",
    "팀 소유권": "Team ownership",
    "같은 팀": "Same team",
    "다른 팀이 독립적으로 개발": "Different teams develop independently",
    "유비쿼터스 언어": "Ubiquitous language",
    "용어 충돌 없음": "No terminology conflicts",
    "같은 용어가 다른 의미": "Same terms with different meanings",
    "데이터 저장소": "Data storage",
    "동종 (예: 모두 PostgreSQL)": "Homogeneous (e.g., all PostgreSQL)",
    "이종 (예: SQL + NoSQL)": "Heterogeneous (e.g., SQL + NoSQL)",

    # --- Naming / Glossary ---
    "각 빌딩블록의 상세 네이밍 규칙은 개별 가이드에 기술되어 있습니다. 이 섹션은 **모든 빌딩블록의 네이밍 패턴을 한 곳에서 참조할** 수 있는 중앙 색인 역할을 합니다.":
        "Detailed naming rules for each building block are described in their respective guides. This section serves as a central index where **all building block naming patterns can be referenced in one place.**",
    "The following table 모든 빌딩블록의 네이밍 규칙을 한 곳에 모은 중앙 색인입니다. 새 타입을 추가할 때 이 표를 참조하여 일관된 이름을 부여하세요.":
        "The following table is a central index gathering all building block naming rules in one place. When adding a new type, refer to this table to assign a consistent name.",
    "Module (폴더)": "Module (folder)",
    "복수 명사 (유비쿼터스 언어)": "Plural noun (ubiquitous language)",
    "도메인 전문가와 개발자가 공유하는 용어집을 유지하면, 코드 네이밍과 비즈니스 용어의 괴리를 방지할 수 있습니다.":
        "Maintaining a glossary shared by domain experts and developers prevents gaps between code naming and business terminology.",
    "상품": "Product",
    "판매 카탈로그의 개별 항목": "Individual item in the sales catalog",
    "재고": "Inventory",
    "상품의 가용 수량": "Available quantity of a product",
    "Product와 1:1": "1:1 with Product",
    "주문": "Order",
    "고객의 구매 요청": "Customer's purchase request",
    "금액": "Amount",
    "통화 + 수치 조합": "Currency + numeric value combination",
    "수량": "Quantity",
    "0 이상의 정수 값": "Integer value of 0 or greater",
    "> **활용**: 프로젝트별 용어집을 위 형식으로 작성하여 도메인 전문가와 공유합니다. 용어가 변경되면 코드 타입명도 함께 변경합니다.":
        "> **Usage**: Write a project-specific glossary in the format above and share with domain experts. When a term changes, rename the code type accordingly.",

    # --- Domain Expert ---
    "- 용어집은 도메인 전문가와 개발자가 **반복적으로 합의하여** 유지합니다.":
        "- The glossary is maintained through **iterative agreement** between domain experts and developers.",
    "- 코드에서 도메인 용어와 다른 이름을 사용하면 커뮤니케이션 비용이 증가합니다. 용어 충돌 발견 시 즉시 용어집을 갱신하고 코드를 리네이밍합니다.":
        "- Using names different from domain terms in code increases communication costs. When terminology conflicts are found, immediately update the glossary and rename the code.",
    "- 새 빌딩블록 추가 시 위의 네이밍 패턴 테이블을 참조하여 일관된 이름을 부여합니다.":
        "- When adding new building blocks, refer to the naming pattern table above to assign consistent names.",

    # --- Bounded Context ---
    "현재 SingleHost 프로젝트는 **단일 Bounded Context 내에서** 여러 Module(Products, Orders 등)을 운영합니다. 이 섹션은 서비스가 성장하여 다중 Bounded Context로 분리될 때 적용할 **Context Map 패턴을** 정의하고, 기존 코드에서 이미 존재하는 선행 패턴을 식별합니다.":
        "The current SingleHost project operates multiple Modules (Products, Orders, etc.) **within a single Bounded Context.** This section defines **Context Map patterns** to apply when the service grows and splits into multiple Bounded Contexts, and identifies precedent patterns that already exist in the codebase.",
    "Functorium 매핑": "Functorium Mapping",
    "두 BC가 공유하는 도메인 모델 부분집합": "Subset of domain model shared by two BCs",
    "`SharedModels/` 폴더 (`Money`, `Quantity`)": "`SharedModels/` folder (`Money`, `Quantity`)",
    "상류 BC가 하류 BC에 API 제공": "Upstream BC provides API to downstream BC",
    "미구현 (향후 서비스 간 REST API)": "Not implemented (future inter-service REST API)",
    "외부 모델 오염 방지 변환 계층": "Translation layer preventing external model contamination",
    "표준 프로토콜로 공개 API 제공": "Public API via standard protocol",
    "BC 간 공유 언어 (이벤트/스키마)": "Shared language between BCs (events/schema)",
    "Domain Events (향후 Integration Event)": "Domain Events (future Integration Event)",
    "하류가 상류 모델을 그대로 수용": "Downstream accepts upstream model as-is",
    "미구현": "Not implemented",
    "BC 간 통합 없이 독립 운영": "Independent operation without BC integration",

    # --- Precedent patterns ---
    "기존 코드에는 이미 Context Map 패턴의 **단일 서비스 내 선행 구현이** 존재합니다. 서비스 분리 시 이 패턴들이 BC 간 통합 지점이 됩니다.":
        "The existing code already contains **precedent implementations of Context Map patterns within a single service.** These patterns become integration points between BCs when services are separated.",
    "`Money`, `Quantity` 등 여러 Module이 공유하는 Value Object가 `SharedModels/` 폴더에 배치되어 있습니다. 서비스 분리 시 NuGet 패키지로 추출하거나 각 BC에 복제하는 결정이 필요합니다.":
        "`Money`, `Quantity`, and other Value Objects shared across multiple Modules are placed in the `SharedModels/` folder. When separating services, a decision is needed whether to extract them as a NuGet package or duplicate them in each BC.",
    "Order Module이 Product 데이터를 조회할 때 `IProductCatalog` Port를 통해 접근합니다. 현재는 동일 프로세스 내 EF Core 구현이지만, 서비스 분리 시 원격 API 호출 + 응답 변환 계층(ACL)으로 교체됩니다.":
        "When the Order Module queries Product data, it accesses it through the `IProductCatalog` Port. Currently this is an EF Core implementation within the same process, but when services are separated it is replaced with remote API calls + a response translation layer (ACL).",
    "현재 Domain Event는 in-process Mediator로 발행됩니다. 서비스 분리 시 메시지 브로커(RabbitMQ, Kafka 등)를 통한 Integration Event로 전환되며, 이때 Domain Event와 Integration Event의 분리가 필요합니다.":
        "Currently Domain Events are published via in-process Mediator. When services are separated, they are converted to Integration Events via a message broker (RabbitMQ, Kafka, etc.), requiring separation of Domain Events and Integration Events.",
    "다중 Bounded Context로 분리될 때의 개념적 프로젝트 구조입니다.":
        "This is the conceptual project structure when separating into multiple Bounded Contexts.",
    "← BC 1 (기존 3-Layer 구조 동일)": "<- BC 1 (same 3-Layer structure as before)",
    "← BC 2": "<- BC 2",
    "공유 NuGet 패키지": "Shared NuGet package",
    "Published Language (BC 간 공유 이벤트 스키마)": "Published Language (shared event schema between BCs)",
    "> 각 BC는 §5의 3-Layer 구조(Domain → Application → Adapter)를 그대로 유지합니다. BC 간 통신만 Cross-Aggregate Port 대신 Integration Event 또는 REST API로 교체됩니다.":
        "> Each BC maintains the 3-Layer structure (Domain -> Application -> Adapter) from section 5. Only inter-BC communication is replaced from Cross-Aggregate Port to Integration Event or REST API.",
    "§6의 Multi-Aggregate 확장 가이드에서 **3단계 분리 판단 기준(WHEN)** 을 제시했습니다. 이 섹션의 Context Map 패턴은 분리를 결정한 후 **어떻게(HOW)** 구현할지를 안내합니다.":
        "The Multi-Aggregate expansion guide in section 6 presented **the criteria for Stage 3 separation (WHEN).** The Context Map patterns in this section guide **how (HOW)** to implement after deciding to separate.",
    "- **WHEN**: 배포 주기, 트랜잭션 경계, 팀 소유권, 유비쿼터스 언어 충돌, 데이터 저장소 이종성 → §6 판단 기준 테이블":
        "- **WHEN**: Deployment cycle, transaction boundary, team ownership, ubiquitous language conflicts, data storage heterogeneity -> Section 6 criteria table",
    "- **HOW**: Shared Kernel, ACL, Published Language, Open Host Service → 이 섹션의 Context Map 패턴":
        "- **HOW**: Shared Kernel, ACL, Published Language, Open Host Service -> Context Map patterns in this section",

    # --- Quick Start ---
    "### 간단한 Email 값 객체": "### Simple Email Value Object",
    "// private 생성자 - 외부 생성 차단": "// Private constructor - blocks external creation",
    "// 팩토리 메서드": "// Factory method",
    "// 검증 메서드 (원시 타입 반환)": "// Validation method (returns primitive type)",
    "// 암시적 변환 (선택적)": "// Implicit conversion (optional)",
    "### 사용 예시": "### Usage Example",
    "// 성공": "// Success",
    "// 실패": "// Failure",
    "### 테스트 예시": "### Test Example",

    # --- Guide Document Index ---
    "주요 내용": "Key Topics",
    "값 객체 구현": "Value Object implementation",
    "기반 클래스, 검증 시스템, 구현 패턴, 실전 예제": "Base classes, validation system, implementation patterns, practical examples",
    "값 객체 검증·열거형": "Value Object validation/enumeration",
    "열거형 구현, Application 검증, FAQ": "Enumeration implementation, Application validation, FAQ",
    "Aggregate 설계": "Aggregate design",
    "설계 원칙, 경계 설정, 안티패턴": "Design principles, boundary setting, anti-patterns",
    "Entity/Aggregate 핵심 패턴": "Entity/Aggregate core patterns",
    "클래스 계층, ID 시스템, 생성 패턴, 도메인 이벤트": "Class hierarchy, ID system, creation patterns, domain events",
    "Entity/Aggregate 고급 패턴": "Entity/Aggregate advanced patterns",
    "Cross-Aggregate 관계, 부가 인터페이스, 실전 예제": "Cross-Aggregate relationships, supplementary interfaces, practical examples",
    "도메인 이벤트": "Domain events",
    "이벤트 정의, 발행, 핸들러 구현": "Event definition, publishing, handler implementation",
    "에러 시스템: 기초와 네이밍": "Error system: fundamentals and naming",
    "에러 처리 원칙, Fin 패턴, 네이밍 규칙": "Error handling principles, Fin pattern, naming rules",
    "에러 시스템: Domain/Application 에러": "Error system: Domain/Application errors",
    "Domain/Application/Event 에러 정의와 테스트": "Domain/Application/Event error definitions and testing",
    "에러 시스템: Adapter 에러와 테스트": "Error system: Adapter errors and testing",
    "Adapter 에러, Custom 에러, 테스트 모범 사례, 체크리스트": "Adapter errors, Custom errors, testing best practices, checklist",
    "Usecase 구현": "Usecase implementation",
    "CQRS 패턴, Apply 병합": "CQRS pattern, Apply composition",
    "Port 아키텍처": "Port architecture",
    "Port 정의, IObservablePort 계층": "Port definition, IObservablePort hierarchy",
    "Adapter 구현": "Adapter implementation",
    "Adapter 연결": "Adapter connection",
    "Adapter 테스트": "Adapter testing",
    "단위 테스트, E2E Walkthrough": "Unit testing, E2E Walkthrough",
    "도메인 서비스": "Domain services",
    "IDomainService, 교차 Aggregate 로직, Usecase 통합": "IDomainService, cross-Aggregate logic, Usecase integration",
    "비즈니스 규칙 캡슐화, And/Or/Not 조합, Repository 통합": "Business rule encapsulation, And/Or/Not composition, Repository integration",
    "단위 테스트": "Unit testing",
    "테스트 규칙, 네이밍, 체크리스트": "Test rules, naming, checklist",
    "테스트 라이브러리": "Testing library",
    "로그/아키텍처/소스생성기/Job 테스트": "Log/architecture/source generator/Job testing",

    # --- Practical example section ---
    "## 실전 예제 프로젝트": "## Practical Example Projects",
    "LayeredArch 예제 프로젝트에서 실제 구현을 확인할 수 있습니다:":
        "You can examine actual implementations in the LayeredArch example project:",
    "예제 파일": "Example File",
    "값 객체": "Value Object",
    "Repository (공통)": "Repository (common)",

    # --- Troubleshooting ---
    "### Value Object의 Create()가 항상 실패한다": "### Value Object's Create() always fails",
    "**Cause:** `Validate()` 메서드에서 `null`이나 빈 문자열을 처리하지 않거나, 정규식 패턴이 잘못되었을 수 있습니다.":
        "**Cause:** The `Validate()` method may not handle `null` or empty strings, or the regex pattern may be incorrect.",
    "**Solution:** `Validate()` 메서드에서 `null` 처리를 확인하고, `ValidationRules<T>.NotEmpty(value ?? \"\")` 패턴을 사용하세요. 정규식 패턴은 별도 단위 테스트로 검증하세요.":
        "**Solution:** Check null handling in the `Validate()` method, and use the `ValidationRules<T>.NotEmpty(value ?? \"\")` pattern. Verify the regex pattern with a separate unit test.",
    "### Entity의 ID가 비교되지 않는다 (동등성 실패)": "### Entity ID comparison fails (equality failure)",
    "**Cause:** `[GenerateEntityId]` 어트리뷰트 없이 직접 ID 타입을 정의했거나, `IEntityId<T>`를 구현하지 않았을 수 있습니다.":
        "**Cause:** The ID type may have been defined directly without the `[GenerateEntityId]` attribute, or `IEntityId<T>` may not have been implemented.",
    "**Solution:** Entity ID는 반드시 `[GenerateEntityId]` 소스 생성기를 사용하세요. 소스 생성기가 `Equals()`, `GetHashCode()`, `==`, `!=` 연산자를 자동 생성합니다.":
        "**Solution:** Always use the `[GenerateEntityId]` source generator for Entity IDs. The source generator automatically creates `Equals()`, `GetHashCode()`, `==`, and `!=` operators.",
    "### 도메인 로직을 어디에 배치해야 할지 모르겠다": "### Unsure where to place domain logic",
    "**Cause:** 빌딩블록 간 역할 구분이 명확하지 않을 때 발생합니다.":
        "**Cause:** This occurs when the role distinction between building blocks is not clear.",
    "**Solution:** 다음 판단 기준을 따르세요:": "**Solution:** Follow these criteria:",
    "1. 단일 Aggregate 내부 → Entity 메서드 또는 Value Object":
        "1. Within a single Aggregate -> Entity method or Value Object",
    "2. 여러 Aggregate 참조 + I/O 없음 → Domain Service":
        "2. Multiple Aggregate references + no I/O -> Domain Service",
    "3. I/O 필요 (Repository, 외부 API) → Usecase에서 조율":
        "3. I/O required (Repository, external API) -> Orchestrate in Usecase",
    "4. 상태 변경 후 부수 효과 → Domain Event + Event Handler":
        "4. Side effects after state change -> Domain Event + Event Handler",

    # --- FAQ ---
    "### Q1. Value Object와 Entity의 Optional 기준은?": "### Q1. What are the criteria for choosing between Value Object and Entity?",
    "식별자(ID)가 필요한지 여부가 핵심입니다. `Money`, `Email`처럼 값 자체로 동등성을 판단하면 Value Object, `Order`, `Product`처럼 고유 ID로 추적해야 하면 Entity입니다. 일반적으로 Value Object가 더 많고, Entity가 소수여야 합니다.":
        "The key criterion is whether an identifier (ID) is needed. If equality is determined by the value itself, like `Money` or `Email`, use a Value Object. If it needs to be tracked by a unique ID, like `Order` or `Product`, use an Entity. Generally, there should be more Value Objects and fewer Entities.",
    "### Q2. Aggregate 경계를 어떻게 설정하나요?": "### Q2. How do you set Aggregate boundaries?",
    "하나의 트랜잭션에서 일관성을 보장해야 하는 범위가 Aggregate 경계입니다. Aggregate를 작게 유지하고, Aggregate 간 참조는 ID만 사용하세요. 상세 설계 원칙은 [06a-aggregate-design.md](./06a-aggregate-design)를 참조하세요.":
        "The scope that must guarantee consistency within a single transaction is the Aggregate boundary. Keep Aggregates small and only use IDs for cross-Aggregate references. For detailed design principles, refer to [06a-aggregate-design.md](./06a-aggregate-design).",
    "### Q3. SharedModels에 배치해야 할 타입의 기준은?": "### Q3. What are the criteria for types that should be placed in SharedModels?",
    "2개 이상의 Aggregate에서 공유하는 Value Object나 Entity가 대상입니다. 처음에는 특정 Aggregate 내부에 배치하고, 실제로 공유가 필요해진 시점에 `SharedModels/`로 이동하세요.":
        "Value Objects or Entities shared by 2 or more Aggregates are candidates. Initially place them inside a specific Aggregate, and move them to `SharedModels/` when sharing actually becomes necessary.",
    "### Q4. `Fin<T>`와 `Validation<Error, T>`는 언제 사용하나요?": "### Q4. When should `Fin<T>` and `Validation<Error, T>` be used?",
    "`Fin<T>`는 최종 결과(성공 또는 단일 에러)에, `Validation<Error, T>`는 검증 결과(여러 에러 누적)에 사용합니다. Value Object의 `Create()`는 `Fin<T>`를, `Validate()`는 `Validation<Error, T>`를 반환합니다.":
        "`Fin<T>` is used for final results (success or single error), and `Validation<Error, T>` for validation results (multiple error accumulation). A Value Object's `Create()` returns `Fin<T>`, and `Validate()` returns `Validation<Error, T>`.",
    "### Q5. 다중 Bounded Context로 분리하는 시점은?": "### Q5. When should you separate into multiple Bounded Contexts?",
    "배포 주기가 다르거나, 팀 소유권이 분리되거나, 동일 용어가 다른 의미로 쓰이거나, 이종 데이터 저장소가 필요한 경우 분리를 검토하세요. 분리 방법은 Context Map 패턴(Shared Kernel, ACL, Published Language 등)을 적용합니다.":
        "Consider separation when deployment cycles differ, team ownership is divided, the same terms are used with different meanings, or heterogeneous data stores are needed. Apply Context Map patterns (Shared Kernel, ACL, Published Language, etc.) for separation methods.",
    "> **Note**: 3단계 Bounded Context 분리 패턴(Context Map, ACL 등)은 아래 §8 Bounded Context 경계 정의에서 다룹니다.":
        "> **Note**: Stage 3 Bounded Context separation patterns (Context Map, ACL, etc.) are covered in section 8 Bounded Context Boundary Definition below.",

    # --- References ---
    "함수형 프로그래밍 라이브러리": "Functional programming library",
    "타입 안전한 열거형": "Type-safe enumeration",

    # --- IEntity hierarchy comments ---
    "OccurredAt, EventId 자동 설정": "OccurredAt, EventId auto-set",
}

def translate_line(line):
    """Translate Korean phrases in a single line."""
    result = line
    # Sort by length descending to match longest phrases first
    for korean, english in sorted(TRANSLATIONS.items(), key=lambda x: len(x[0]), reverse=True):
        if korean in result:
            result = result.replace(korean, english)
    return result


def translate_file(filepath):
    """Translate all Korean text in a file."""
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    lines = content.split('\n')
    translated_lines = [translate_line(line) for line in lines]
    translated_content = '\n'.join(translated_lines)

    if translated_content != content:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(translated_content)
        return True
    return False


if __name__ == '__main__':
    base = r"C:\Workspace\Github\Functorium\Docs.Site\src\content\docs\guides"
    files = [
        os.path.join(base, "domain", "04-ddd-tactical-overview.md"),
    ]
    for filepath in files:
        if os.path.exists(filepath):
            changed = translate_file(filepath)
            print(f"{'Changed' if changed else 'No change'}: {os.path.basename(filepath)}")
        else:
            print(f"Not found: {filepath}")
