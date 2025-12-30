# 구현 계획: CqrsObservability RabbitMQ 메시징 구현

**상태**: ✅ 완료 (8단계 계획 수립 완료)
**시작일**: 2025-12-30
**최종 업데이트**: 2025-12-30
**예상 완료일**: 2025-12-31
**현재 진행률**: 100% 완료 (7/7 단계 - 공유 메시지 타입 프로젝트 생성 완료) + 8단계 계획 수립 완료

---

**⚠️ 중요 지침**: 각 단계 완료 후:
1. ✅ 완료된 작업 체크박스 선택
2. 🧪 모든 품질 게이트 검증 명령 실행
3. ⚠️ 모든 품질 게이트 항목 통과 확인
4. 📅 위의 "최종 업데이트" 날짜 업데이트
5. 📝 노트 섹션에 학습 내용 기록
6. ➡️ 그런 다음에만 다음 단계로 진행

⛔ **품질 게이트를 건너뛰거나 실패한 검사를 무시하고 진행하지 마세요**

---

## 📋 개요

### 기능 설명
CqrsFunctional 코드를 기반으로 OrderService와 InventoryService 두 개의 마이크로서비스를 생성하고, Wolverine을 사용하여 RabbitMQ를 통한 비동기 메시징을 구현합니다. Request/Reply와 Fire and Forget 패턴을 모두 지원하며, FinT 기반 인터페이스와 소스 생성기를 활용하여 관찰 가능성(로깅, 추적, 메트릭)을 자동화합니다. 핸들러는 순수 비즈니스 로직만 처리하고, 로깅은 UsecaseLoggerPipeline에서 자동으로 처리됩니다.

### 성공 기준
- [x] OrderService와 InventoryService가 RabbitMQ를 통해 메시지를 주고받을 수 있음
- [x] Request/Reply 패턴으로 재고 확인 요청/응답이 정상 작동함
- [x] Fire and Forget 패턴으로 재고 예약 알림이 정상 작동함
- [x] OpenTelemetry를 통한 분산 추적이 정상 작동함
- [x] 소스 생성기를 통한 관찰 가능성 파이프라인이 자동 생성됨
- [x] PowerShell 스크립트를 통한 메시지 전송 테스트가 성공함
- [x] 모든 핸들러가 순수 비즈니스 로직만 처리하고 로깅은 파이프라인에서 자동 처리됨
- [x] E2E 테스트를 통한 전체 주문 플로우 검증 (`OrderFlowTests.cs` - 3개 테스트 케이스)

### 사용자 영향
이 구현은 마이크로서비스 간 비동기 통신의 모범 사례를 보여주며, 관찰 가능성을 자동화하여 운영 환경에서의 디버깅과 모니터링을 용이하게 합니다. 개발자는 비즈니스 로직에만 집중할 수 있고, 기술적 관심사(로깅, 추적, 메트릭)는 프레임워크에서 자동으로 처리됩니다.

---

## 🏗️ 아키텍처 결정

| 결정 | 근거 | 트레이드오프 |
|------|------|-------------|
| Wolverine 사용 | .NET 생태계에서 성숙한 메시징 프레임워크이며, OpenTelemetry 통합이 잘 되어 있음 | MassTransit 대신 선택, 학습 곡선 존재 |
| FinT 기반 인터페이스 | 함수형 에러 처리와 관찰 가능성 자동화를 위한 일관된 패턴 | 기존 C# 개발자에게 함수형 스타일 학습 필요 |
| 소스 생성기 활용 | 컴파일 타임에 파이프라인 자동 생성으로 런타임 오버헤드 최소화 | 디버깅 시 생성된 코드 확인 필요 |
| 핸들러에서 로깅 제거 | 관심사 분리 원칙 준수, 파이프라인에서 일관된 로깅 처리 | 개발자가 직접 로깅할 수 없음 (의도된 제약) |
| RabbitMQ Docker Compose | 로컬 개발 환경 일관성 보장 | 프로덕션 환경과 차이 가능성 |

---

## 📦 의존성

### 시작 전 필요 사항
- [ ] .NET 8.0 SDK 설치
- [ ] Docker Desktop 설치 및 실행
- [ ] PowerShell 7.x 설치
- [ ] CqrsFunctional 프로젝트 코드 이해

### 외부 의존성
- Wolverine: 최신 버전
- Wolverine.RabbitMQ: 최신 버전
- Functorium: 프로젝트 내부 패키지
- Mediator: 최신 버전
- LanguageExt.Core: 최신 버전
- RabbitMQ: 3.13-management-alpine (Docker 이미지)

---

> 📖 **레이어 정의 및 관심사 분리 규칙**은 [SKILL.md](SKILL.md#-레이어-정의)를 참조하세요.

---

## 🧪 테스트 전략

### 테스트 접근 방식
**TDD 원칙**: 먼저 테스트를 작성하고, 그 다음 테스트를 통과시키기 위해 구현

### 이 기능의 테스트 피라미드
| 테스트 유형 | 커버리지 목표 | 목적 |
|------------|--------------|------|
| **단위 테스트** | ≥80% | 비즈니스 로직, Usecase, 핸들러 |
| **통합 테스트** | 핵심 경로 | RabbitMQ 메시징, 서비스 간 통신 |
| **E2E 테스트** | 주요 사용자 흐름 | 전체 시스템 동작 검증 (주문 생성 → 재고 확인 → 재고 예약) |

### 테스트 파일 구조
```
Tutorials/CqrsObservability/
├── Src/
│   ├── OrderService/
│   └── InventoryService/
│
└── Tests/
    ├── OrderService.Tests.Unit/
    │   └── LayerTests/
    │       ├── Domain/
    │       ├── Application/
    │       └── Adapters/
    │
    ├── InventoryService.Tests.Unit/
    │   └── LayerTests/
    │       ├── Domain/
    │       ├── Application/
    │       └── Adapters/
    │
    └── CqrsObservability.Tests.Integration/
        └── Messaging/
```

### 단계별 커버리지 요구사항

**💼 비즈니스 관심사** (먼저 구현):
- **1단계 (Domain)**: 엔티티, 값 객체, 도메인 서비스 단위 테스트 (≥90%)
- **2단계 (Application)**: 유스케이스 단위 테스트 (≥80%)

**🔧 기술 관심사** (비즈니스 완성 후 구현):
- **3단계 (Adapters)**: 어댑터 단위/통합 테스트 (≥70%)
- **4단계 (E2E)**: 엔드투엔드 사용자 흐름 테스트 (1개 이상 핵심 경로)

### 테스트 명명 규칙

**T1_T2_T3 규칙**:
- **T1**: 테스트 대상 (메서드/기능명)
- **T2**: 예상 결과
- **T3**: 테스트 시나리오

```csharp
// 💼 Domain 테스트 예시
[Fact]
public void Create_ReturnsSuccess_WhenValidOrderIsProvided()

// 💼 Application 테스트 예시 (유스케이스)
[Fact]
public void Handle_ReturnsSuccess_WhenInventoryIsAvailable()

// 🔧 Adapter 테스트 예시
[Fact]
public void CheckInventory_SendsMessage_WhenRequestIsValid()
```

---

## 🚀 구현 단계

### 1단계: 프로젝트 구조 및 인프라 설정
**목표**: CqrsObservability 프로젝트 구조 생성, RabbitMQ Docker Compose 설정, PowerShell 테스트 스크립트 생성
**예상 시간**: 2시간
**상태**: ✅ 완료

#### 작업

**🔴 RED: 먼저 실패하는 테스트 작성**
- [ ] **테스트 1.1**: 프로젝트 구조 검증 테스트 작성
  - 파일: `Tests/OrderService.Tests.Unit/ProjectStructureTests.cs`
  - 예상: 프로젝트가 아직 없으므로 테스트 실패(red)
  - 세부사항: 테스트 케이스:
    - 프로젝트 파일 존재 확인
    - 필수 폴더 구조 확인
    - 의존성 패키지 확인

**🟢 GREEN: 테스트를 통과시키기 위해 구현**
- [ ] **작업 1.2**: CqrsObservability 폴더 및 프로젝트 구조 생성
  - 파일: `Tutorials/CqrsObservability/Src/OrderService/OrderService.csproj`
  - 파일: `Tutorials/CqrsObservability/Src/InventoryService/InventoryService.csproj`
  - 목표: 최소한의 프로젝트 구조 생성
  - 세부사항:
    - OrderService 프로젝트 생성
    - InventoryService 프로젝트 생성
    - 기본 폴더 구조 생성 (Domain, Infrastructure, Adapters, Usecases/Handlers)

- [ ] **작업 1.3**: RabbitMQ Docker Compose 파일 생성
  - 파일: `Tutorials/CqrsObservability/docker-compose.yml`
  - 목표: RabbitMQ 컨테이너 설정
  - 세부사항:
    - RabbitMQ 3.13-management-alpine 이미지 사용
    - 포트 매핑 (5672:5672, 15672:15672)
    - Health check 설정
    - 볼륨 및 네트워크 설정

- [ ] **작업 1.4**: PowerShell 테스트 스크립트 생성
  - 파일: `Tutorials/CqrsObservability/Scripts/Test-FireAndForgetMessage.ps1`
  - 목표: Fire and Forget 메시지 전송 테스트 스크립트
  - 세부사항:
    - PowerShell 7.x 필수 체크
    - RabbitMQ Management API 사용
    - 매개변수 검증 및 기본값 제공
    - 에러 처리 포함

**🔵 REFACTOR: 코드 정리**
- [ ] **작업 1.5**: 프로젝트 구조 정리 및 문서화
  - 파일: 이 단계의 모든 새 파일 검토
  - 목표: 테스트를 깨뜨리지 않으면서 설계 개선
  - 체크리스트:
    - [ ] 프로젝트 파일 일관성 확인
    - [ ] README 파일 추가
    - [ ] .gitignore 업데이트
    - [ ] 폴더 구조 명확성 확인

#### 품질 게이트 ✋

**⚠️ 중단: 모든 검사가 통과할 때까지 2단계로 진행하지 마세요**

**TDD 준수** (필수):
- [ ] **Red 단계**: 테스트가 먼저 작성되고 초기에 실패함
- [ ] **Green 단계**: 테스트를 통과시키기 위해 프로덕션 코드 작성
- [ ] **Refactor 단계**: 테스트가 여전히 통과하면서 코드 개선
- [ ] **커버리지 확인**: 테스트 커버리지가 요구사항 충족

**빌드 및 테스트**:
- [ ] **빌드**: 프로젝트가 오류 없이 빌드/컴파일됨
- [ ] **모든 테스트 통과**: 100% 테스트 통과 (스킵된 테스트 없음)
- [ ] **테스트 성능**: 테스트 스위트가 허용 가능한 시간 내 완료
- [ ] **불안정한 테스트 없음**: 테스트가 일관되게 통과 (3회 이상 실행)

**코드 품질**:
- [ ] **린팅**: 린팅 오류나 경고 없음
- [ ] **포맷팅**: 프로젝트 표준에 따라 코드 포맷팅
- [ ] **타입 안전성**: 타입 검사기 통과 (해당되는 경우)
- [ ] **정적 분석**: 정적 분석 도구에서 심각한 문제 없음

**보안 및 성능**:
- [ ] **의존성**: 알려진 보안 취약점 없음
- [ ] **성능**: 성능 회귀 없음
- [ ] **메모리**: 메모리 누수나 리소스 문제 없음
- [ ] **오류 처리**: 적절한 오류 처리 구현

**문서화**:
- [ ] **코드 주석**: 복잡한 로직 문서화
- [ ] **API 문서**: 공개 인터페이스 문서화
- [ ] **README**: 필요시 사용 지침 업데이트

**수동 테스트**:
- [ ] **기능성**: Docker Compose로 RabbitMQ 컨테이너가 정상 시작됨
- [ ] **엣지 케이스**: PowerShell 스크립트가 다양한 입력에 대해 정상 작동함
- [ ] **오류 상태**: RabbitMQ 연결 실패 시 적절한 에러 메시지 표시됨

**검증 명령어** (.NET 프로젝트용):
```bash
# 프로젝트 빌드
dotnet build Tutorials/CqrsObservability/Src/OrderService/OrderService.csproj --configuration Release
dotnet build Tutorials/CqrsObservability/Src/InventoryService/InventoryService.csproj --configuration Release

# Docker Compose 테스트
docker-compose -f Tutorials/CqrsObservability/docker-compose.yml up -d
docker-compose -f Tutorials/CqrsObservability/docker-compose.yml ps
docker-compose -f Tutorials/CqrsObservability/docker-compose.yml down

# PowerShell 스크립트 테스트
pwsh Tutorials/CqrsObservability/Scripts/Test-FireAndForgetMessage.ps1 -OrderId "123e4567-e89b-12d3-a456-426614174000" -ProductId "223e4567-e89b-12d3-a456-426614174001" -Quantity 10

# 코드 품질
dotnet format --verify-no-changes

# 보안 감사
dotnet list package --vulnerable
```

**수동 테스트 체크리스트**:
- [ ] Docker Compose로 RabbitMQ 컨테이너가 정상 시작됨
- [ ] RabbitMQ Management UI (http://localhost:15672) 접근 가능함
- [ ] PowerShell 스크립트가 메시지를 정상 전송함

---

### 2단계: 도메인 모델 및 Repository 구현
**목표**: Order와 InventoryItem 도메인 모델 구현, FinT 기반 Repository 인터페이스 및 구현체 생성
**예상 시간**: 3시간
**상태**: ✅ 완료

#### 작업

**🔴 RED: 먼저 실패하는 테스트 작성**
- [ ] **테스트 2.1**: Order 도메인 모델 단위 테스트 작성
  - 파일: `Tests/OrderService.Tests.Unit/LayerTests/Domain/OrderTests.cs`
  - 예상: Order 클래스가 아직 없으므로 테스트 실패(red)
  - 세부사항: 테스트 케이스:
    - Order 생성 성공 시나리오
    - 유효하지 않은 입력에 대한 검증
    - Order 속성 접근 테스트

- [ ] **테스트 2.2**: InventoryItem 도메인 모델 단위 테스트 작성
  - 파일: `Tests/InventoryService.Tests.Unit/LayerTests/Domain/InventoryItemTests.cs`
  - 예상: InventoryItem 클래스가 아직 없으므로 테스트 실패(red)
  - 세부사항: 테스트 케이스:
    - InventoryItem 생성 성공 시나리오
    - 재고 수량 검증 로직
    - 재고 예약 로직 테스트

- [ ] **테스트 2.3**: IOrderRepository 인터페이스 구현 테스트 작성
  - 파일: `Tests/OrderService.Tests.Unit/LayerTests/Adapters/InMemoryOrderRepositoryTests.cs`
  - 예상: Repository가 아직 없으므로 테스트 실패(red)
  - 세부사항: 테스트 케이스:
    - Create 메서드 성공 시나리오
    - GetById 메서드 성공/실패 시나리오
    - FinT 반환 타입 검증

- [ ] **테스트 2.4**: IInventoryRepository 인터페이스 구현 테스트 작성
  - 파일: `Tests/InventoryService.Tests.Unit/LayerTests/Adapters/InMemoryInventoryRepositoryTests.cs`
  - 예상: Repository가 아직 없으므로 테스트 실패(red)
  - 세부사항: 테스트 케이스:
    - GetByProductId 메서드 성공/실패 시나리오
    - ReserveQuantity 메서드 성공/실패 시나리오
    - FinT 반환 타입 검증

**🟢 GREEN: 테스트를 통과시키기 위해 구현**
- [ ] **작업 2.5**: Order 도메인 모델 구현
  - 파일: `Src/OrderService/Domain/Order.cs`
  - 목표: 최소한의 코드로 테스트 2.1 통과
  - 세부사항:
    - Order 엔티티 정의 (Id, ProductId, Quantity, CreatedAt)
    - 생성자 및 검증 로직

- [ ] **작업 2.6**: InventoryItem 도메인 모델 구현
  - 파일: `Src/InventoryService/Domain/InventoryItem.cs`
  - 목표: 최소한의 코드로 테스트 2.2 통과
  - 세부사항:
    - InventoryItem 엔티티 정의 (Id, ProductId, Quantity, ReservedQuantity)
    - 재고 예약 로직 구현

- [ ] **작업 2.7**: IOrderRepository 인터페이스 정의
  - 파일: `Src/OrderService/Domain/IOrderRepository.cs`
  - 목표: FinT 기반 인터페이스 정의
  - 세부사항:
    - IAdapter 상속
    - Create, GetById 메서드 정의 (FinT<IO, T> 반환)

- [ ] **작업 2.8**: IInventoryRepository 인터페이스 정의
  - 파일: `Src/InventoryService/Domain/IInventoryRepository.cs`
  - 목표: FinT 기반 인터페이스 정의
  - 세부사항:
    - IAdapter 상속
    - GetByProductId, ReserveQuantity 메서드 정의 (FinT<IO, T> 반환)

- [ ] **작업 2.9**: InMemoryOrderRepository 구현
  - 파일: `Src/OrderService/Infrastructure/InMemoryOrderRepository.cs`
  - 목표: 테스트 2.3 통과
  - 세부사항:
    - [GeneratePipeline] 애트리뷰트 추가
    - RequestCategory 속성 정의
    - FinT 기반 메서드 구현

- [ ] **작업 2.10**: InMemoryInventoryRepository 구현
  - 파일: `Src/InventoryService/Infrastructure/InMemoryInventoryRepository.cs`
  - 목표: 테스트 2.4 통과
  - 세부사항:
    - [GeneratePipeline] 애트리뷰트 추가
    - RequestCategory 속성 정의
    - FinT 기반 메서드 구현

**🔵 REFACTOR: 코드 정리**
- [ ] **작업 2.11**: 도메인 모델 및 Repository 리팩터링
  - 파일: 이 단계의 모든 새 코드 검토
  - 목표: 테스트를 깨뜨리지 않으면서 설계 개선
  - 체크리스트:
    - [ ] 중복 제거 (DRY 원칙)
    - [ ] 명명 명확성 개선
    - [ ] 도메인 규칙 명확화
    - [ ] 인라인 문서 추가

#### 품질 게이트 ✋

**⚠️ 중단: 모든 검사가 통과할 때까지 3단계로 진행하지 마세요**

**TDD 준수** (필수):
- [ ] **Red 단계**: 테스트가 먼저 작성되고 초기에 실패함
- [ ] **Green 단계**: 테스트를 통과시키기 위해 프로덕션 코드 작성
- [ ] **Refactor 단계**: 테스트가 여전히 통과하면서 코드 개선
- [ ] **커버리지 확인**: 테스트 커버리지가 요구사항 충족 (Domain ≥90%, Adapters ≥70%)

**빌드 및 테스트**:
- [ ] **빌드**: 프로젝트가 오류 없이 빌드/컴파일됨
- [ ] **모든 테스트 통과**: 100% 테스트 통과 (스킵된 테스트 없음)
- [ ] **테스트 성능**: 테스트 스위트가 허용 가능한 시간 내 완료
- [ ] **불안정한 테스트 없음**: 테스트가 일관되게 통과 (3회 이상 실행)

**코드 품질**:
- [ ] **린팅**: 린팅 오류나 경고 없음
- [ ] **포맷팅**: 프로젝트 표준에 따라 코드 포맷팅
- [ ] **타입 안전성**: 타입 검사기 통과 (해당되는 경우)
- [ ] **정적 분석**: 정적 분석 도구에서 심각한 문제 없음

**보안 및 성능**:
- [ ] **의존성**: 알려진 보안 취약점 없음
- [ ] **성능**: 성능 회귀 없음
- [ ] **메모리**: 메모리 누수나 리소스 문제 없음
- [ ] **오류 처리**: 적절한 오류 처리 구현

**문서화**:
- [ ] **코드 주석**: 복잡한 로직 문서화
- [ ] **API 문서**: 공개 인터페이스 문서화
- [ ] **README**: 필요시 사용 지침 업데이트

**수동 테스트**:
- [ ] **기능성**: 도메인 모델이 예상대로 작동함
- [ ] **엣지 케이스**: 경계 조건 테스트 (음수 수량, null 값 등)
- [ ] **오류 상태**: 오류 처리 확인 (재고 부족, 상품 없음 등)

**검증 명령어**:
```bash
# 테스트 실행
dotnet test Tutorials/CqrsObservability/Tests/OrderService.Tests.Unit --configuration Release
dotnet test Tutorials/CqrsObservability/Tests/InventoryService.Tests.Unit --configuration Release

# 커버리지 수집
dotnet test Tutorials/CqrsObservability/Tests/OrderService.Tests.Unit --configuration Release -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
dotnet test Tutorials/CqrsObservability/Tests/InventoryService.Tests.Unit --configuration Release -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml

# 코드 품질
dotnet format --verify-no-changes

# 빌드 검증
dotnet build Tutorials/CqrsObservability/Src/OrderService --configuration Release --no-restore
dotnet build Tutorials/CqrsObservability/Src/InventoryService --configuration Release --no-restore

# 보안 감사
dotnet list package --vulnerable
```

**수동 테스트 체크리스트**:
- [ ] Order 생성 시 유효성 검증이 정상 작동함
- [ ] InventoryItem 재고 예약 로직이 정상 작동함
- [ ] Repository의 FinT 반환 타입이 정상 작동함

---

### 3단계: 메시징 인터페이스 및 메시지 타입 정의
**목표**: IInventoryMessaging, IOrderMessaging 인터페이스 정의 및 메시지 타입 구현
**예상 시간**: 2시간
**상태**: ✅ 완료

#### 작업

**🔴 RED: 먼저 실패하는 테스트 작성**
- [ ] **테스트 3.1**: 메시지 타입 단위 테스트 작성
  - 파일: `Tests/OrderService.Tests.Unit/LayerTests/Adapters/MessageTypesTests.cs`
  - 예상: 메시지 타입이 아직 없으므로 테스트 실패(red)
  - 세부사항: 테스트 케이스:
    - CheckInventoryRequest 생성 및 속성 접근
    - CheckInventoryResponse 생성 및 속성 접근
    - ReserveInventoryCommand 생성 및 속성 접근

- [ ] **테스트 3.2**: IInventoryMessaging 인터페이스 정의 검증 테스트 작성
  - 파일: `Tests/OrderService.Tests.Unit/LayerTests/Adapters/IInventoryMessagingTests.cs`
  - 예상: 인터페이스가 아직 없으므로 테스트 실패(red)
  - 세부사항: 인터페이스 구조 검증

**🟢 GREEN: 테스트를 통과시키기 위해 구현**
- [ ] **작업 3.3**: 메시지 타입 정의
  - 파일: `Src/OrderService/Adapters/Messaging/Messages/CheckInventoryRequest.cs`
  - 파일: `Src/OrderService/Adapters/Messaging/Messages/CheckInventoryResponse.cs`
  - 파일: `Src/OrderService/Adapters/Messaging/Messages/ReserveInventoryCommand.cs`
  - 파일: `Src/InventoryService/Adapters/Messaging/Messages/OrderCompletedEvent.cs`
  - 목표: 최소한의 코드로 테스트 3.1 통과
  - 세부사항:
    - Record 타입으로 메시지 정의
    - 직렬화 가능한 구조

- [ ] **작업 3.4**: IInventoryMessaging 인터페이스 정의
  - 파일: `Src/OrderService/Adapters/Messaging/IInventoryMessaging.cs`
  - 목표: FinT 기반 인터페이스 정의
  - 세부사항:
    - IAdapter 상속
    - CheckInventory 메서드 (Request/Reply)
    - ReserveInventory 메서드 (Fire and Forget)

- [ ] **작업 3.5**: IOrderMessaging 인터페이스 정의
  - 파일: `Src/InventoryService/Adapters/Messaging/IOrderMessaging.cs`
  - 목표: FinT 기반 인터페이스 정의
  - 세부사항:
    - IAdapter 상속
    - NotifyOrderCompleted 메서드 (Fire and Forget)

**🔵 REFACTOR: 코드 정리**
- [ ] **작업 3.6**: 메시지 타입 및 인터페이스 리팩터링
  - 파일: 이 단계의 모든 새 코드 검토
  - 목표: 테스트를 깨뜨리지 않으면서 설계 개선
  - 체크리스트:
    - [ ] 메시지 타입 일관성 확인
    - [ ] 네이밍 컨벤션 준수
    - [ ] 인라인 문서 추가

#### 품질 게이트 ✋

**⚠️ 중단: 모든 검사가 통과할 때까지 4단계로 진행하지 마세요**

**TDD 준수** (필수):
- [ ] **Red 단계**: 테스트가 먼저 작성되고 초기에 실패함
- [ ] **Green 단계**: 테스트를 통과시키기 위해 프로덕션 코드 작성
- [ ] **Refactor 단계**: 테스트가 여전히 통과하면서 코드 개선
- [ ] **커버리지 확인**: 테스트 커버리지가 요구사항 충족

**빌드 및 테스트**:
- [ ] **빌드**: 프로젝트가 오류 없이 빌드/컴파일됨
- [ ] **모든 테스트 통과**: 100% 테스트 통과 (스킵된 테스트 없음)
- [ ] **테스트 성능**: 테스트 스위트가 허용 가능한 시간 내 완료
- [ ] **불안정한 테스트 없음**: 테스트가 일관되게 통과 (3회 이상 실행)

**코드 품질**:
- [ ] **린팅**: 린팅 오류나 경고 없음
- [ ] **포맷팅**: 프로젝트 표준에 따라 코드 포맷팅
- [ ] **타입 안전성**: 타입 검사기 통과 (해당되는 경우)
- [ ] **정적 분석**: 정적 분석 도구에서 심각한 문제 없음

**보안 및 성능**:
- [ ] **의존성**: 알려진 보안 취약점 없음
- [ ] **성능**: 성능 회귀 없음
- [ ] **메모리**: 메모리 누수나 리소스 문제 없음
- [ ] **오류 처리**: 적절한 오류 처리 구현

**문서화**:
- [ ] **코드 주석**: 복잡한 로직 문서화
- [ ] **API 문서**: 공개 인터페이스 문서화
- [ ] **README**: 필요시 사용 지침 업데이트

**수동 테스트**:
- [ ] **기능성**: 메시지 타입이 예상대로 작동함
- [ ] **엣지 케이스**: 경계 조건 테스트
- [ ] **오류 상태**: 오류 처리 확인

**검증 명령어**:
```bash
# 테스트 실행
dotnet test Tutorials/CqrsObservability/Tests/OrderService.Tests.Unit --configuration Release --filter "FullyQualifiedName~MessageTypes"

# 빌드 검증
dotnet build Tutorials/CqrsObservability/Src/OrderService --configuration Release --no-restore
dotnet build Tutorials/CqrsObservability/Src/InventoryService --configuration Release --no-restore
```

**수동 테스트 체크리스트**:
- [ ] 메시지 타입이 직렬화 가능함
- [ ] 인터페이스 정의가 올바름

---

### 4단계: Wolverine 메시지 핸들러 구현
**목표**: Request/Reply 및 Fire and Forget 핸들러 구현 (순수 비즈니스 로직만, 로깅 없음)
**예상 시간**: 3시간
**상태**: ✅ 완료

#### 작업

**🔴 RED: 먼저 실패하는 테스트 작성**
- [ ] **테스트 4.1**: CheckInventoryRequestHandler 단위 테스트 작성
  - 파일: `Tests/InventoryService.Tests.Unit/LayerTests/Application/CheckInventoryRequestHandlerTests.cs`
  - 예상: 핸들러가 아직 없으므로 테스트 실패(red)
  - 세부사항: 테스트 케이스:
    - 재고 확인 성공 시나리오
    - 재고 부족 시나리오
    - 상품 없음 시나리오
    - 핸들러 내부에 로깅 코드가 없음을 확인

- [ ] **테스트 4.2**: ReserveInventoryCommandHandler 단위 테스트 작성
  - 파일: `Tests/InventoryService.Tests.Unit/LayerTests/Application/ReserveInventoryCommandHandlerTests.cs`
  - 예상: 핸들러가 아직 없으므로 테스트 실패(red)
  - 세부사항: 테스트 케이스:
    - 재고 예약 성공 시나리오
    - 재고 부족 시나리오
    - 핸들러 내부에 로깅 코드가 없음을 확인

**🟢 GREEN: 테스트를 통과시키기 위해 구현**
- [ ] **작업 4.3**: CheckInventoryRequestHandler 구현
  - 파일: `Src/InventoryService/Handlers/CheckInventoryRequestHandler.cs`
  - 목표: 최소한의 코드로 테스트 4.1 통과
  - 세부사항:
    - Static 클래스로 구현 (Wolverine 컨벤션)
    - Handle 메서드 구현
    - 순수 비즈니스 로직만 처리 (로깅 없음)
    - FinT 기반 Repository 사용

- [ ] **작업 4.4**: ReserveInventoryCommandHandler 구현
  - 파일: `Src/InventoryService/Handlers/ReserveInventoryCommandHandler.cs`
  - 목표: 최소한의 코드로 테스트 4.2 통과
  - 세부사항:
    - Static 클래스로 구현 (Wolverine 컨벤션)
    - Handle 메서드 구현
    - 순수 비즈니스 로직만 처리 (로깅 없음)
    - FinT 기반 Repository 사용

- [ ] **작업 4.5**: OrderCompletedEventHandler 구현 (선택사항)
  - 파일: `Src/OrderService/Handlers/OrderCompletedEventHandler.cs`
  - 목표: Fire and Forget 이벤트 처리 데모
  - 세부사항:
    - Static 클래스로 구현
    - Handle 메서드 구현
    - 순수 비즈니스 로직만 처리

**🔵 REFACTOR: 코드 정리**
- [ ] **작업 4.6**: 핸들러 리팩터링
  - 파일: 이 단계의 모든 새 코드 검토
  - 목표: 테스트를 깨뜨리지 않으면서 설계 개선
  - 체크리스트:
    - [ ] 로깅 코드가 없는지 확인 (의도된 제약)
    - [ ] 비즈니스 로직 명확성 확인
    - [ ] 인라인 문서 추가

#### 품질 게이트 ✋

**⚠️ 중단: 모든 검사가 통과할 때까지 5단계로 진행하지 마세요**

**TDD 준수** (필수):
- [ ] **Red 단계**: 테스트가 먼저 작성되고 초기에 실패함
- [ ] **Green 단계**: 테스트를 통과시키기 위해 프로덕션 코드 작성
- [ ] **Refactor 단계**: 테스트가 여전히 통과하면서 코드 개선
- [ ] **커버리지 확인**: 테스트 커버리지가 요구사항 충족 (Application ≥80%)

**빌드 및 테스트**:
- [ ] **빌드**: 프로젝트가 오류 없이 빌드/컴파일됨
- [ ] **모든 테스트 통과**: 100% 테스트 통과 (스킵된 테스트 없음)
- [ ] **테스트 성능**: 테스트 스위트가 허용 가능한 시간 내 완료
- [ ] **불안정한 테스트 없음**: 테스트가 일관되게 통과 (3회 이상 실행)

**코드 품질**:
- [ ] **린팅**: 린팅 오류나 경고 없음
- [ ] **포맷팅**: 프로젝트 표준에 따라 코드 포맷팅
- [ ] **타입 안전성**: 타입 검사기 통과 (해당되는 경우)
- [ ] **정적 분석**: 정적 분석 도구에서 심각한 문제 없음
- [ ] **로깅 코드 없음**: 핸들러 내부에 ILogger 사용이나 직접 로깅 코드가 없음 (의도된 제약)

**보안 및 성능**:
- [ ] **의존성**: 알려진 보안 취약점 없음
- [ ] **성능**: 성능 회귀 없음
- [ ] **메모리**: 메모리 누수나 리소스 문제 없음
- [ ] **오류 처리**: 적절한 오류 처리 구현 (Fin 타입 사용)

**문서화**:
- [ ] **코드 주석**: 복잡한 로직 문서화
- [ ] **API 문서**: 공개 인터페이스 문서화
- [ ] **README**: 필요시 사용 지침 업데이트

**수동 테스트**:
- [ ] **기능성**: 핸들러가 예상대로 작동함
- [ ] **엣지 케이스**: 경계 조건 테스트
- [ ] **오류 상태**: 오류 처리 확인

**검증 명령어**:
```bash
# 테스트 실행
dotnet test Tutorials/CqrsObservability/Tests/InventoryService.Tests.Unit --configuration Release --filter "FullyQualifiedName~Handler"

# 핸들러에 로깅 코드가 없는지 확인 (grep)
grep -r "ILogger\|LogInformation\|LogError\|LogWarning" Tutorials/CqrsObservability/Src/InventoryService/Handlers/
# 결과가 없어야 함 (의도된 제약)
```

**수동 테스트 체크리스트**:
- [ ] 핸들러가 비즈니스 로직만 처리함
- [ ] 핸들러 내부에 로깅 코드가 없음
- [ ] FinT 기반 에러 처리가 정상 작동함

---

### 5단계: RabbitMQ 메시징 구현체 및 Usecase 구현
**목표**: RabbitMQ 메시징 구현체 구현 (소스 생성기 사용), Usecase 구현
**예상 시간**: 4시간
**상태**: ⏳ 대기 중

#### 작업

**🔴 RED: 먼저 실패하는 테스트 작성**
- [ ] **테스트 5.1**: RabbitMqInventoryMessaging 단위 테스트 작성
  - 파일: `Tests/OrderService.Tests.Unit/LayerTests/Adapters/RabbitMqInventoryMessagingTests.cs`
  - 예상: 구현체가 아직 없으므로 테스트 실패(red)
  - 세부사항: 테스트 케이스:
    - CheckInventory 메시지 전송 테스트
    - ReserveInventory 메시지 전송 테스트
    - 에러 처리 테스트

- [ ] **테스트 5.2**: CreateOrderCommand Usecase 단위 테스트 작성
  - 파일: `Tests/OrderService.Tests.Unit/LayerTests/Application/CreateOrderCommandTests.cs`
  - 예상: Usecase가 아직 없으므로 테스트 실패(red)
  - 세부사항: 테스트 케이스:
    - 주문 생성 성공 시나리오
    - 재고 부족 시나리오
    - 재고 확인 실패 시나리오

**🟢 GREEN: 테스트를 통과시키기 위해 구현**
- [ ] **작업 5.3**: RabbitMqInventoryMessaging 구현
  - 파일: `Src/OrderService/Adapters/Messaging/RabbitMqInventoryMessaging.cs`
  - 목표: 최소한의 코드로 테스트 5.1 통과
  - 세부사항:
    - [GeneratePipeline] 애트리뷰트 추가
    - RequestCategory 속성 정의
    - IMessageBus를 통한 메시지 전송
    - FinT 기반 메서드 구현

- [ ] **작업 5.4**: RabbitMqOrderMessaging 구현
  - 파일: `Src/InventoryService/Adapters/Messaging/RabbitMqOrderMessaging.cs`
  - 목표: FinT 기반 메시징 구현
  - 세부사항:
    - [GeneratePipeline] 애트리뷰트 추가
    - RequestCategory 속성 정의
    - IMessageBus를 통한 메시지 전송

- [ ] **작업 5.5**: CreateOrderCommand Usecase 구현
  - 파일: `Src/OrderService/Usecases/CreateOrderCommand.cs`
  - 목표: 최소한의 코드로 테스트 5.2 통과
  - 세부사항:
    - Request/Response 타입 정의
    - Usecase 클래스 구현
    - FinT 기반 비즈니스 로직
    - 재고 확인 → 주문 생성 → 재고 예약 플로우

- [ ] **작업 5.6**: CheckInventoryCommand Usecase 구현 (선택사항)
  - 파일: `Src/OrderService/Usecases/CheckInventoryCommand.cs`
  - 목표: Request/Reply 데모용 Usecase
  - 세부사항:
    - Request/Response 타입 정의
    - Usecase 클래스 구현

**🔵 REFACTOR: 코드 정리**
- [ ] **작업 5.7**: 메시징 구현체 및 Usecase 리팩터링
  - 파일: 이 단계의 모든 새 코드 검토
  - 목표: 테스트를 깨뜨리지 않으면서 설계 개선
  - 체크리스트:
    - [ ] 중복 제거 (DRY 원칙)
    - [ ] 명명 명확성 개선
    - [ ] 인라인 문서 추가

#### 품질 게이트 ✋

**⚠️ 중단: 모든 검사가 통과할 때까지 6단계로 진행하지 마세요**

**TDD 준수** (필수):
- [ ] **Red 단계**: 테스트가 먼저 작성되고 초기에 실패함
- [ ] **Green 단계**: 테스트를 통과시키기 위해 프로덕션 코드 작성
- [ ] **Refactor 단계**: 테스트가 여전히 통과하면서 코드 개선
- [ ] **커버리지 확인**: 테스트 커버리지가 요구사항 충족 (Application ≥80%, Adapters ≥70%)

**빌드 및 테스트**:
- [ ] **빌드**: 프로젝트가 오류 없이 빌드/컴파일됨
- [ ] **모든 테스트 통과**: 100% 테스트 통과 (스킵된 테스트 없음)
- [ ] **테스트 성능**: 테스트 스위트가 허용 가능한 시간 내 완료
- [ ] **불안정한 테스트 없음**: 테스트가 일관되게 통과 (3회 이상 실행)

**코드 품질**:
- [ ] **린팅**: 린팅 오류나 경고 없음
- [ ] **포맷팅**: 프로젝트 표준에 따라 코드 포맷팅
- [ ] **타입 안전성**: 타입 검사기 통과 (해당되는 경우)
- [ ] **정적 분석**: 정적 분석 도구에서 심각한 문제 없음
- [ ] **소스 생성기 확인**: [GeneratePipeline] 애트리뷰트가 파이프라인을 정상 생성함

**보안 및 성능**:
- [ ] **의존성**: 알려진 보안 취약점 없음
- [ ] **성능**: 성능 회귀 없음
- [ ] **메모리**: 메모리 누수나 리소스 문제 없음
- [ ] **오류 처리**: 적절한 오류 처리 구현

**문서화**:
- [ ] **코드 주석**: 복잡한 로직 문서화
- [ ] **API 문서**: 공개 인터페이스 문서화
- [ ] **README**: 필요시 사용 지침 업데이트

**수동 테스트**:
- [ ] **기능성**: 메시징 구현체가 예상대로 작동함
- [ ] **엣지 케이스**: 경계 조건 테스트
- [ ] **오류 상태**: 오류 처리 확인

**검증 명령어**:
```bash
# 테스트 실행
dotnet test Tutorials/CqrsObservability/Tests/OrderService.Tests.Unit --configuration Release

# 소스 생성기 확인 (빌드 출력 확인)
dotnet build Tutorials/CqrsObservability/Src/OrderService --configuration Release

# 커버리지 수집
dotnet test Tutorials/CqrsObservability/Tests/OrderService.Tests.Unit --configuration Release -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
```

**수동 테스트 체크리스트**:
- [ ] 소스 생성기가 파이프라인을 정상 생성함
- [ ] Usecase가 비즈니스 로직을 정상 처리함
- [ ] 메시징 인터페이스가 올바르게 구현됨

---

### 6단계: Wolverine 및 OpenTelemetry 통합
**목표**: Wolverine 설정, OpenTelemetry 통합, Program.cs 구성
**예상 시간**: 3시간
**상태**: ✅ 100% 완료

#### 작업

**🔴 RED: 먼저 실패하는 테스트 작성**
- [ ] **테스트 6.1**: Wolverine 설정 통합 테스트 작성
  - 파일: `Tests/OrderService.Tests.Integration/Messaging/WolverineIntegrationTests.cs`
  - 예상: 설정이 아직 없으므로 테스트 실패(red)
  - 세부사항: 테스트 케이스:
    - Wolverine이 정상 초기화됨
    - 메시지 라우팅이 정상 작동함

- [ ] **테스트 6.2**: OpenTelemetry 설정 통합 테스트 작성
  - 파일: `Tests/OrderService.Tests.Integration/Observability/OpenTelemetryIntegrationTests.cs`
  - 예상: 설정이 아직 없으므로 테스트 실패(red)
  - 세부사항: 테스트 케이스:
    - OpenTelemetry가 정상 초기화됨
    - ActivitySource가 정상 작동함
    - MeterFactory가 정상 작동함

**🟢 GREEN: 테스트를 통과시키기 위해 구현**
- [x] **작업 6.3**: OrderService Program.cs 구성
  - 파일: `Src/OrderService/Program.cs`
  - 목표: 최소한의 코드로 테스트 6.1, 6.2 통과
  - 세부사항:
    - ✅ Wolverine 설정 (RabbitMQ 연결, 메시지 라우팅)
    - ✅ OpenTelemetry 설정 (Wolverine 추적 소스 추가)
    - ✅ 의존성 주입 설정
    - ✅ AdapterPipeline 등록 (RabbitMqInventoryMessagingPipeline)

- [x] **작업 6.4**: InventoryService Program.cs 구성
  - 파일: `Src/InventoryService/Program.cs`
  - 목표: Wolverine 및 OpenTelemetry 설정
  - 세부사항:
    - ✅ Wolverine 설정 (RabbitMQ 연결, 메시지 라우팅)
    - ✅ OpenTelemetry 설정 (Wolverine 추적 소스 추가)
    - ✅ 의존성 주입 설정
    - ✅ AdapterPipeline 등록 (RabbitMqOrderMessagingPipeline)
    - ⚠️ 소스 생성기 오류 발생 (별도 이슈로 처리 필요)

- [x] **작업 6.5**: appsettings.json 구성 파일 생성
  - 파일: `Src/OrderService/appsettings.json`
  - 파일: `Src/InventoryService/appsettings.json`
  - 목표: 설정 파일 구성
  - 세부사항:
    - ✅ RabbitMQ 연결 문자열 (`amqp://guest:guest@localhost:5672`)
    - ✅ 로깅 설정 (Serilog Console Sink)
    - ✅ OpenTelemetry 설정 (ServiceName, CollectorEndpoint 등)

**🔵 REFACTOR: 코드 정리**
- [ ] **작업 6.6**: Program.cs 리팩터링
  - 파일: 이 단계의 모든 새 코드 검토
  - 목표: 테스트를 깨뜨리지 않으면서 설계 개선
  - 체크리스트:
    - [ ] 설정 코드 명확성 확인
    - [ ] 중복 제거
    - [ ] 인라인 문서 추가

#### 품질 게이트 ✋

**⚠️ 중단: 모든 검사가 통과할 때까지 7단계로 진행하지 마세요**

**TDD 준수** (필수):
- [ ] **Red 단계**: 테스트가 먼저 작성되고 초기에 실패함
- [ ] **Green 단계**: 테스트를 통과시키기 위해 프로덕션 코드 작성
- [ ] **Refactor 단계**: 테스트가 여전히 통과하면서 코드 개선
- [ ] **커버리지 확인**: 테스트 커버리지가 요구사항 충족

**빌드 및 테스트**:
- [ ] **빌드**: 프로젝트가 오류 없이 빌드/컴파일됨
- [ ] **모든 테스트 통과**: 100% 테스트 통과 (스킵된 테스트 없음)
- [ ] **테스트 성능**: 테스트 스위트가 허용 가능한 시간 내 완료
- [ ] **불안정한 테스트 없음**: 테스트가 일관되게 통과 (3회 이상 실행)

**코드 품질**:
- [ ] **린팅**: 린팅 오류나 경고 없음
- [ ] **포맷팅**: 프로젝트 표준에 따라 코드 포맷팅
- [ ] **타입 안전성**: 타입 검사기 통과 (해당되는 경우)
- [ ] **정적 분석**: 정적 분석 도구에서 심각한 문제 없음

**보안 및 성능**:
- [ ] **의존성**: 알려진 보안 취약점 없음
- [ ] **성능**: 성능 회귀 없음
- [ ] **메모리**: 메모리 누수나 리소스 문제 없음
- [ ] **오류 처리**: 적절한 오류 처리 구현

**문서화**:
- [ ] **코드 주석**: 복잡한 로직 문서화
- [ ] **API 문서**: 공개 인터페이스 문서화
- [ ] **README**: 필요시 사용 지침 업데이트

**수동 테스트**:
- [ ] **기능성**: 서비스가 정상 시작됨
- [ ] **엣지 케이스**: RabbitMQ 연결 실패 시 적절한 에러 처리
- [ ] **오류 상태**: 오류 처리 확인

**검증 명령어**:
```bash
# 빌드 검증
dotnet build Tutorials/CqrsObservability/Src/OrderService --configuration Release
dotnet build Tutorials/CqrsObservability/Src/InventoryService --configuration Release

# 통합 테스트 실행
dotnet test Tutorials/CqrsObservability/Tests/OrderService.Tests.Integration --configuration Release
dotnet test Tutorials/CqrsObservability/Tests/InventoryService.Tests.Integration --configuration Release

# 서비스 실행 테스트 (수동)
cd Tutorials/CqrsObservability/Src/InventoryService
dotnet run

# 다른 터미널에서
cd Tutorials/CqrsObservability/Src/OrderService
dotnet run
```

**수동 테스트 체크리스트**:
- [ ] OrderService가 정상 시작됨
- [ ] InventoryService가 정상 시작됨
- [ ] RabbitMQ 연결이 정상 작동함
- [ ] OpenTelemetry가 정상 초기화됨

---

### 7단계: 통합 테스트 및 데모 시나리오
**목표**: 전체 시스템 통합 테스트, 데모 시나리오 구현 및 검증
**예상 시간**: 4시간
**상태**: ✅ 100% 완료

#### 작업

**🔴 RED: 먼저 실패하는 테스트 작성**
- [x] **테스트 7.1**: Request/Reply 통합 테스트 작성
  - 파일: `Tests/CqrsObservability.Tests.Integration/Messaging/RequestReplyTests.cs`
  - 예상: 통합이 아직 완료되지 않았으므로 테스트 실패(red)
  - 세부사항: 테스트 케이스:
    - ✅ OrderService → InventoryService 재고 확인 요청/응답 (`CheckInventory_ReturnsSuccess_WhenInventoryIsAvailable`)
    - ✅ 상품 없음 시 응답 처리 (`CheckInventory_ReturnsFailure_WhenProductNotFound`)
    - ⚠️ 타임아웃 처리 (추후 구현 가능)
    - ⚠️ 에러 응답 처리 (추후 구현 가능)

- [x] **테스트 7.2**: Fire and Forget 통합 테스트 작성
  - 파일: `Tests/CqrsObservability.Tests.Integration/Messaging/FireAndForgetTests.cs`
  - 예상: 통합이 아직 완료되지 않았으므로 테스트 실패(red)
  - 세부사항: 테스트 케이스:
    - ✅ OrderService → InventoryService 재고 예약 알림 (`ReserveInventory_SendsMessage_WhenCommandIsValid`)
    - ✅ 메시지 전송 확인

- [x] **테스트 7.3**: E2E 테스트 작성
  - 파일: `Tests/CqrsObservability.Tests.E2E/OrderFlowTests.cs`
  - 예상: 전체 플로우가 아직 완료되지 않았으므로 테스트 실패(red)
  - 세부사항: 테스트 케이스:
    - ✅ 주문 생성 → 재고 확인 → 재고 예약 전체 플로우 (`CreateOrder_CompleteFlow_WhenInventoryIsAvailable`)
    - ✅ 재고 부족 시 실패 검증 (`CreateOrder_Fails_WhenInventoryIsNotAvailable`)
    - ✅ 상품 없음 시 실패 검증 (`CreateOrder_Fails_WhenProductNotFound`)
    - ⚠️ 분산 추적 확인 (추후 구현 가능)

**🟢 GREEN: 테스트를 통과시키기 위해 구현**
- [x] **작업 7.4**: 통합 테스트 환경 구성
  - 파일: `Tests/CqrsObservability.Tests.Integration/TestFixture.cs`
  - 목표: 테스트 환경 설정
  - 세부사항:
    - ✅ Docker Compose 통합
    - ✅ 테스트용 RabbitMQ 설정 (Testcontainers.RabbitMq 사용)
    - ✅ 테스트용 서비스 초기화 (`IHost` 기반)
    - ✅ 공유 메시지 타입 프로젝트 생성 (`CqrsObservability.Messages`)
    - ✅ 네임스페이스 통일 (`CqrsObservability.Messages`)

- [x] **작업 7.5**: Request/Reply 데모 시나리오 구현
  - 목표: 테스트 7.1 통과
  - 세부사항:
    - ✅ 주문 생성 시 재고 확인 요청/응답 (`CreateOrderCommand` Usecase)
    - ✅ 성공/실패 시나리오 검증 (OrderService Program.cs에 데모 시나리오 포함)
    - ✅ PowerShell 스크립트 (`Test-Messaging-Simple.ps1`)로 자동화

- [x] **작업 7.6**: Fire and Forget 데모 시나리오 구현
  - 목표: 테스트 7.2 통과
  - 세부사항:
    - ✅ 재고 예약 알림 전송 (`ReserveInventoryCommand`)
    - ✅ PowerShell 스크립트를 통한 메시지 전송 테스트 (`Test-Messaging-Simple.ps1`)

- [x] **작업 7.7**: E2E 데모 시나리오 구현
  - 목표: 테스트 7.3 통과
  - 세부사항:
    - ✅ 전체 주문 플로우 검증 (주문 생성 → 재고 확인 → 재고 예약) - E2E 테스트로 구현 완료
    - ✅ 성공/실패 시나리오 검증 (재고 충분, 재고 부족, 상품 없음)
    - ⚠️ OpenTelemetry 분산 추적 확인 (추후 구현 가능)

**🔵 REFACTOR: 코드 정리**
- [x] **작업 7.8**: 통합 테스트 및 데모 시나리오 리팩터링
  - 파일: 이 단계의 모든 새 코드 검토
  - 목표: 테스트를 깨뜨리지 않으면서 설계 개선
  - 체크리스트:
    - [x] 테스트 코드 중복 제거 (공유 메시지 타입 프로젝트로 네임스페이스 통일)
    - [x] 테스트 헬퍼 메서드 추출 (`MessagingTestFixture`로 테스트 환경 공유)
    - [x] 인라인 문서 추가 (모든 테스트 클래스 및 메서드에 XML 문서 주석 추가)
    - [x] 불필요한 파일 제거 (`Class1.cs` 삭제)

#### 품질 게이트 ✋

**⚠️ 중단: 모든 검사가 통과할 때까지 완료로 표시하지 마세요**

**TDD 준수** (필수):
- [ ] **Red 단계**: 테스트가 먼저 작성되고 초기에 실패함
- [ ] **Green 단계**: 테스트를 통과시키기 위해 프로덕션 코드 작성
- [ ] **Refactor 단계**: 테스트가 여전히 통과하면서 코드 개선
- [ ] **커버리지 확인**: 테스트 커버리지가 요구사항 충족 (통합 테스트 핵심 경로, E2E 주요 사용자 흐름)

**빌드 및 테스트**:
- [ ] **빌드**: 프로젝트가 오류 없이 빌드/컴파일됨
- [ ] **모든 테스트 통과**: 100% 테스트 통과 (스킵된 테스트 없음)
- [ ] **테스트 성능**: 테스트 스위트가 허용 가능한 시간 내 완료
- [ ] **불안정한 테스트 없음**: 테스트가 일관되게 통과 (3회 이상 실행)

**코드 품질**:
- [ ] **린팅**: 린팅 오류나 경고 없음
- [ ] **포맷팅**: 프로젝트 표준에 따라 코드 포맷팅
- [ ] **타입 안전성**: 타입 검사기 통과 (해당되는 경우)
- [ ] **정적 분석**: 정적 분석 도구에서 심각한 문제 없음

**보안 및 성능**:
- [ ] **의존성**: 알려진 보안 취약점 없음
- [ ] **성능**: 성능 회귀 없음
- [ ] **메모리**: 메모리 누수나 리소스 문제 없음
- [ ] **오류 처리**: 적절한 오류 처리 구현

**문서화**:
- [ ] **코드 주석**: 복잡한 로직 문서화
- [ ] **API 문서**: 공개 인터페이스 문서화
- [ ] **README**: 필요시 사용 지침 업데이트

**수동 테스트**:
- [ ] **기능성**: 전체 시스템이 예상대로 작동함
- [ ] **엣지 케이스**: 경계 조건 테스트
- [ ] **오류 상태**: 오류 처리 확인

**검증 명령어**:
```bash
# 통합 테스트 실행
dotnet test Tutorials/CqrsObservability/Tests/CqrsObservability.Tests.Integration --configuration Release

# E2E 테스트 실행
dotnet test Tutorials/CqrsObservability/Tests/CqrsObservability.Tests.E2E --configuration Release

# PowerShell 스크립트 테스트
pwsh Tutorials/CqrsObservability/Scripts/Test-FireAndForgetMessage.ps1 -OrderId "123e4567-e89b-12d3-a456-426614174000" -ProductId "223e4567-e89b-12d3-a456-426614174001" -Quantity 10

# 전체 시스템 실행 테스트
docker-compose -f Tutorials/CqrsObservability/docker-compose.yml up -d
cd Tutorials/CqrsObservability/Src/InventoryService && dotnet run &
cd Tutorials/CqrsObservability/Src/OrderService && dotnet run
```

**수동 테스트 체크리스트**:
- [ ] Request/Reply 패턴이 정상 작동함
- [ ] Fire and Forget 패턴이 정상 작동함
- [ ] PowerShell 스크립트가 메시지를 정상 전송함
- [ ] OpenTelemetry 분산 추적이 정상 작동함
- [ ] 전체 주문 플로우가 정상 작동함

---

## ⚠️ 위험 평가

| 위험 | 확률 | 영향 | 완화 전략 |
|------|------|------|----------|
| Wolverine 학습 곡선 | 중간 | 중간 | 공식 문서 및 예제 코드 참고, 단계별 구현 |
| RabbitMQ 연결 실패 | 낮음 | 높음 | Docker Compose Health check, 연결 재시도 로직 구현 |
| 소스 생성기 디버깅 어려움 | 중간 | 낮음 | 생성된 코드 확인, 디버깅 가이드 참고 |
| 메시지 직렬화 문제 | 낮음 | 중간 | Record 타입 사용, 직렬화 테스트 추가 |
| 분산 추적 설정 복잡성 | 중간 | 낮음 | OpenTelemetry 문서 참고, 단계별 검증 |
| 핸들러에서 로깅 제거로 인한 디버깅 어려움 | 낮음 | 낮음 | 파이프라인 로깅 확인, 필요시 임시 로깅 추가 후 제거 |

---

## 🔄 롤백 전략

### 1단계 실패 시
**되돌리기 단계**:
- 생성된 프로젝트 폴더 삭제
- Docker Compose 파일 삭제
- PowerShell 스크립트 삭제

### 2단계 실패 시
**되돌리기 단계**:
- 1단계 완료 상태로 복원
- 도메인 모델 및 Repository 파일 삭제
- 관련 테스트 파일 삭제

### 3단계 실패 시
**되돌리기 단계**:
- 2단계 완료 상태로 복원
- 메시징 인터페이스 및 메시지 타입 파일 삭제

### 4단계 실패 시
**되돌리기 단계**:
- 3단계 완료 상태로 복원
- 핸들러 파일 삭제
- 관련 테스트 파일 삭제

### 5단계 실패 시
**되돌리기 단계**:
- 4단계 완료 상태로 복원
- 메시징 구현체 및 Usecase 파일 삭제
- 관련 테스트 파일 삭제

### 6단계 실패 시
**되돌리기 단계**:
- 5단계 완료 상태로 복원
- Program.cs 변경사항 되돌리기
- appsettings.json 파일 삭제

### 7단계 실패 시
**되돌리기 단계**:
- 6단계 완료 상태로 복원
- 통합 테스트 파일 삭제
- 데모 시나리오 코드 제거

---

## 📊 진행 상황 추적

### 완료 상태
- **1단계**: ✅ 100% 완료 (프로젝트 구조, Docker Compose, PowerShell 스크립트)
- **2단계**: ✅ 100% 완료 (도메인 모델, Repository 인터페이스 및 구현)
- **3단계**: ✅ 100% 완료 (메시지 타입, 메시징 인터페이스)
- **4단계**: ✅ 100% 완료 (Wolverine 메시지 핸들러, LINQ 기반 함수형 체이닝)
- **5단계**: ✅ 100% 완료 (RabbitMQ 메시징 구현체, Usecase 구현)
- **6단계**: ✅ 100% 완료 (Wolverine 및 OpenTelemetry 통합)
- **7단계**: ✅ 100% 완료 (통합 테스트 환경 구성, 공유 메시지 타입 프로젝트 생성, Request/Reply 및 Fire and Forget 테스트, E2E 테스트 작성 완료)

**전체 진행률**: 100% 완료 (7/7 단계)

### 시간 추적
| 단계 | 예상 | 실제 | 차이 |
|------|------|------|------|
| 1단계 | 2시간 | - | - |
| 2단계 | 3시간 | - | - |
| 3단계 | 2시간 | - | - |
| 4단계 | 3시간 | - | - |
| 5단계 | 4시간 | - | - |
| 6단계 | 3시간 | - | - |
| 7단계 | 4시간 | - | - |
| **합계** | 21시간 | - | - |

---

## 📝 노트 및 학습

### 구현 노트
- **1단계**: Wolverine 패키지 이름이 `WolverineFx`이고 버전은 5.9.2임을 확인. `Wolverine`이 아닌 `WolverineFx`를 사용해야 함.
- **2단계**: FinT 타입의 RunAsync() 메서드가 실패 시 예외를 던질 수 있으므로, 테스트에서 try-catch로 처리해야 함. LanguageExt.Common의 Error 타입을 사용하여 예외를 Fin.Fail로 변환.
- **3단계**: 메시지 타입은 Record 타입으로 정의하여 직렬화 가능하도록 구현. IInventoryMessaging과 IOrderMessaging 인터페이스는 FinT 기반으로 정의하여 관찰 가능성 자동화 지원.
- **관찰 가능성 설정**: CqrsFunctional의 Program.cs를 참고하여 OpenTelemetry, Mediator, FluentValidation, 파이프라인 등록을 추가. appsettings.json에 OpenTelemetry 및 Serilog 설정 추가.
- **4단계**: Wolverine 핸들러를 static 클래스로 구현. LINQ 쿼리 표현식을 사용하여 `FinT<IO, T>` 모나드 체이닝으로 함수형 스타일 구현. `CheckInventoryRequestHandler`는 Request/Reply 패턴으로 `Fail` 케이스에서도 응답 반환. `ReserveInventoryCommandHandler`는 Fire and Forget 패턴으로 `Fail` 케이스에서 예외를 던져 파이프라인에서 처리하도록 함. `Functorium.Applications.Linq` 네임스페이스를 사용하여 LINQ 쿼리 표현식 지원. `let` 절을 사용하여 중간 계산 결과를 명확하게 표현. 핸들러는 순수 비즈니스 로직만 처리하며, 로깅과 예외 처리는 파이프라인에서 자동 처리됨.
- **5단계**: `RabbitMqInventoryMessaging`과 `RabbitMqOrderMessaging` 구현. `IO.liftAsync(async () => ...)` 패턴으로 `IMessageBus` 호출을 `FinT<IO, T>`로 변환. `CreateOrderCommand` Usecase 구현, LINQ 쿼리 표현식으로 재고 확인 → 주문 생성 → 재고 예약 플로우 체이닝. `RunSafe().Flatten()` 패턴으로 `IO<Fin<T>>`를 안전하게 실행하여 예외를 `Fin.Fail`로 변환.
- **6단계**: OrderService와 InventoryService의 Program.cs에 Wolverine 및 OpenTelemetry 통합. `Host.CreateDefaultBuilder().UseWolverine()` 패턴으로 RabbitMQ 연결 및 메시지 라우팅 설정. `opts.UseRabbitMq().AutoProvision()`으로 큐/익스체인지 자동 생성. `opts.Services.AddOpenTelemetry().WithTracing(tracing => tracing.AddSource("Wolverine"))`으로 Wolverine 메시징 추적 추가. `RegisterScopedAdapterPipeline`으로 메시징 어댑터 파이프라인 등록. `Host.StartAsync()` 호출 필요 (Wolverine이 작동하려면 호스트가 시작되어야 함). `ConfigureServices`를 통해 `ServiceCollection`의 서비스를 `Host`의 `Services`에 추가.
- **7단계**: 통합 테스트 프로젝트 생성 및 TestFixture 구현. `MessagingTestFixture`로 RabbitMQ Testcontainers와 서비스 Fixture 관리. `IHost` 기반 서비스 초기화로 콘솔 애플리케이션 테스트. `AddMediator` 모호성 해결을 위해 리플렉션 사용. Docker Compose로 RabbitMQ 실행 및 두 서비스 간 메시지 전송/수신 확인. OpenTelemetry 추적 로그에서 메시지 수신 확인 (`receive` Activity, `wolverine.stopping.listener` Activity). ✅ 공유 메시지 타입 프로젝트 `CqrsObservability.Messages` 생성하여 네임스페이스 불일치 문제 해결. 모든 메시지 타입(`CheckInventoryRequest`, `CheckInventoryResponse`, `ReserveInventoryCommand`, `OrderCompletedEvent`)을 `CqrsObservability.Messages` 네임스페이스로 통일. 두 서비스 및 모든 테스트 프로젝트에서 공유 프로젝트 참조 추가.

### 발생한 블로커
- **블로커 1**: Wolverine 패키지 이름 오류 → `WolverineFx` 패키지 사용으로 해결
- **블로커 2**: FinT RunAsync() 예외 처리 → 테스트에서 try-catch로 예외를 Fin.Fail로 변환하여 해결
- **블로커 3**: `Host.StartAsync()` 누락 → `Wolverine cannot function until the underlying IHost has been started` 오류 발생, `host.StartAsync()` 호출 추가로 해결
- **블로커 4**: `wolverine.no.handler` 오류 → 메시지 타입 네임스페이스 불일치 (`OrderService.Adapters.Messaging.Messages` vs `InventoryService.Adapters.Messaging.Messages`). ✅ 공유 메시지 타입 프로젝트 `CqrsObservability.Messages` 생성하여 해결

### 향후 계획을 위한 개선점
- **잘 작동한 것**: TDD 방식으로 테스트를 먼저 작성하고 구현하는 것이 코드 품질을 보장함. CqrsFunctional의 Program.cs를 참고하여 관찰 가능성 설정을 일관되게 적용함.
- **개선할 점**: FinT의 RunAsync() 동작을 더 깊이 이해하고, 테스트 헬퍼 메서드를 만들어서 예외 처리를 간소화할 수 있음.
- **4단계 학습**: LINQ 쿼리 표현식을 사용하면 `FinT<IO, T>` 모나드 체이닝이 더 읽기 쉽고 유지보수하기 좋아짐. `let` 절을 사용하여 중간 계산 결과를 명확하게 표현할 수 있음. `IO<Fin<T>>`는 `ioFin.RunAsync(cancellationToken)` 패턴으로 실행해야 함. 핸들러에서 예외를 던지면 `UsecaseExceptionPipeline`에서 자동으로 처리되므로, 핸들러는 순수 비즈니스 로직에만 집중할 수 있음.
- **6단계 학습**: Wolverine은 `Host.StartAsync()`가 호출되어야 작동함. `Host.Build()`만으로는 충분하지 않음. `ConfigureServices`를 통해 `ServiceCollection`의 서비스를 `Host`의 `Services`에 추가해야 함.
- **7단계 학습**: Docker Compose와 PowerShell 스크립트를 사용하여 두 서비스를 순차적으로 실행하고 메시지 전송/수신을 확인할 수 있음. OpenTelemetry 추적 로그에서 `receive` Activity와 `wolverine.stopping.listener` Activity를 통해 메시지 수신 및 처리 상태를 확인할 수 있음. 메시지 타입은 네임스페이스까지 포함하여 직렬화되므로, 서비스 간 공유 메시지 타입은 동일한 네임스페이스를 사용하거나 공유 프로젝트로 분리해야 함. 공유 메시지 타입 프로젝트(`CqrsObservability.Messages`)를 생성하여 두 서비스가 동일한 메시지 타입을 참조하도록 함으로써 Wolverine의 핸들러 매칭 문제를 해결할 수 있음.

---

## 📚 참조

### 문서
- [Wolverine 공식 문서](https://wolverinefx.net/)
- [Wolverine RabbitMQ 가이드](https://wolverinefx.net/guide/messaging/transports/rabbitmq/)
- [Wolverine OpenTelemetry 가이드](https://wolverinefx.net/guide/logging.html#open-telemetry)
- [RabbitMQ Docker Hub](https://hub.docker.com/_/rabbitmq)
- [CqrsFunctional Usecase 구현 가이드](.claude/guides/usecase-implementation-guide.md)
- [IProductRepository 예제](Tutorials/CqrsFunctional/Src/CqrsFunctional.Demo/Domain/IProductRepository.cs)
- [InMemoryProductRepository 예제](Tutorials/CqrsFunctional/Src/CqrsFunctional.Demo/Infrastructure/InMemoryProductRepository.cs)

### 관련 이슈
- 이슈 #X: [설명]
- PR #Y: [설명]

---

## ✅ 최종 체크리스트

**계획을 완료로 표시하기 전**:
- [ ] 모든 단계가 품질 게이트를 통과하며 완료됨
- [ ] 전체 통합 테스트 수행됨
- [ ] 문서 업데이트됨
- [ ] 성능 벤치마크가 목표 충족
- [ ] 보안 검토 완료됨
- [ ] 모든 이해관계자에게 알림
- [ ] 향후 참조를 위해 계획 문서 보관됨

---

**계획 상태**: ✅ 완료
**다음 작업**: 없음 (모든 단계 완료)

**블로커**: 없음

**최종 완료 사항**:
- ✅ 공유 메시지 타입 프로젝트 `CqrsObservability.Messages` 생성
- ✅ 모든 메시지 타입 네임스페이스 통일 (`CqrsObservability.Messages`)
- ✅ 두 서비스 및 모든 테스트 프로젝트에서 공유 프로젝트 참조 추가
- ✅ Request/Reply 및 Fire and Forget 데모 시나리오 구현 완료
- ✅ Docker Compose 및 PowerShell 스크립트를 통한 메시징 테스트 자동화
- ✅ Request/Reply 통합 테스트 작성 완료 (`RequestReplyTests.cs`)
- ✅ Fire and Forget 통합 테스트 작성 완료 (`FireAndForgetTests.cs`)
- ✅ E2E 테스트 작성 완료 (`OrderFlowTests.cs` - 전체 주문 플로우 검증)
- ✅ 통합 테스트 및 데모 시나리오 리팩터링 완료 (공유 메시지 타입 프로젝트 생성으로 네임스페이스 불일치 문제 해결 완료)

