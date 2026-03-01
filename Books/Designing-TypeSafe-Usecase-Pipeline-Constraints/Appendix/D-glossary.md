# D. 용어집

## 개요

이 책에서 사용하는 핵심 용어를 정리합니다.

---

## 제네릭 변성

### 공변성 (Covariance)

제네릭 타입 파라미터가 상속 관계를 **같은 방향**으로 유지하는 성질. C#에서는 `out` 키워드로 선언합니다. `Dog : Animal`이면, `IEnumerable<Dog>`을 `IEnumerable<Animal>`에 대입할 수 있습니다. **출력 위치**(반환 타입)에서만 사용 가능합니다.

### 반공변성 (Contravariance)

제네릭 타입 파라미터가 상속 관계를 **반대 방향**으로 유지하는 성질. C#에서는 `in` 키워드로 선언합니다. `Dog : Animal`이면, `Action<Animal>`을 `Action<Dog>`에 대입할 수 있습니다. **입력 위치**(파라미터)에서만 사용 가능합니다.

### 불변성 (Invariance)

제네릭 타입 파라미터가 상속 관계를 유지하지 않는 성질. `List<Dog>`를 `List<Animal>`에 대입할 수 없습니다. `in`도 `out`도 선언하지 않은 타입 파라미터는 불변입니다.

---

## 타입 시스템

### CRTP (Curiously Recurring Template Pattern)

자기 자신을 타입 파라미터로 전달하는 패턴. `IFinResponseFactory<TSelf> where TSelf : IFinResponseFactory<TSelf>`처럼 선언하여, 인터페이스에서 구현 타입을 반환하는 메서드를 정의할 수 있습니다.

### static abstract

C# 11에서 도입된 기능. 인터페이스에서 정적 메서드를 선언하고 구현 타입이 이를 구현하도록 강제합니다. 제네릭 제약 조건에서 `TSelf.CreateFail(error)`처럼 호출할 수 있어, 리플렉션 없이 팩토리 메서드를 사용할 수 있습니다.

### Discriminated Union (판별 합집합)

하나의 타입이 여러 개의 고정된 케이스 중 정확히 하나를 나타내는 타입. `FinResponse<A>`는 `Succ`와 `Fail` 두 케이스를 가진 Discriminated Union입니다. F#에서는 언어 수준에서 지원하고, C#에서는 sealed record 계층으로 구현합니다.

### sealed struct

상속이 불가능한 값 타입. `Fin<T>`가 sealed struct이므로 인터페이스 제약 조건(`where T : Fin<T>`)으로 사용할 수 없습니다. 이것이 `FinResponse<A>` 래퍼를 설계하게 된 핵심 원인입니다.

---

## 함수형 프로그래밍

### Monad (모나드)

값을 감싸고(`unit/return`), 감싼 값에 함수를 적용하는(`bind/flatMap`) 패턴. `FinResponse<A>`는 `Bind` 메서드를 통해 모나딕 합성을 지원합니다.

### Railway Oriented Programming (ROP)

모든 함수가 성공/실패 두 트랙을 반환하며, 실패 시 이후 단계를 건너뛰는 에러 처리 패턴. `FinResponse<A>`의 `Map`/`Bind` 체인이 이 패턴을 구현합니다.

### Match (패턴 매칭)

Discriminated Union의 각 케이스에 대해 서로 다른 함수를 실행하는 연산. `response.Match(Succ: ..., Fail: ...)`으로 성공/실패를 분기 처리합니다.

### Map (매핑)

컨텍스트(성공/실패)를 유지하면서 내부 값만 변환하는 연산. `response.Map(x => x.ToString())`은 성공 시 값을 변환하고, 실패 시 에러를 그대로 전달합니다.

### Bind (바인드)

값을 꺼내서 새로운 컨텍스트를 만드는 연산. `response.Bind(x => FindUser(x))`는 성공 시 `FindUser`를 호출하고(실패할 수 있음), 실패 시 에러를 그대로 전달합니다.

---

## 아키텍처

### Pipeline (파이프라인)

요청이 Handler에 도달하기 전/후에 실행되는 미들웨어 체인. Validation, Exception, Logging, Tracing, Metrics, Transaction, Caching의 7가지 Pipeline이 있습니다.

### Mediator (미디에이터)

요청자와 처리자 사이의 중재자. 요청을 적절한 Handler로 라우팅하고, Pipeline을 자동으로 적용합니다. Functorium은 [Mediator](https://github.com/martinothamar/Mediator) 라이브러리를 사용합니다.

### Pipeline Behavior

Mediator에서 Pipeline을 구현하는 인터페이스. `IPipelineBehavior<TMessage, TResponse>`를 구현하여 요청 전/후에 로직을 삽입합니다.

### CQRS (Command Query Responsibility Segregation)

명령(쓰기)과 조회(읽기)의 책임을 분리하는 패턴. `ICommandRequest<T>`는 상태 변경, `IQueryRequest<T>`는 데이터 조회를 담당합니다.

### Usecase (유스케이스)

하나의 비즈니스 작업을 나타내는 단위. Request, Response, Validator, Handler를 포함하며, Nested class 패턴으로 하나의 클래스에 응집합니다.

---

## 인터페이스

### IFinResponse

`IsSucc`/`IsFail` 속성을 제공하는 비제네릭 마커 인터페이스. Pipeline에서 응답 상태를 읽을 때 사용합니다.

### IFinResponse\<out A\>

공변 제네릭 인터페이스. `IFinResponse`를 상속하며, `out A`로 공변성을 지원합니다.

### IFinResponseFactory\<TSelf\>

CRTP 팩토리 인터페이스. `static abstract TSelf CreateFail(Error error)` 메서드를 정의합니다. Pipeline에서 리플렉션 없이 실패 응답을 생성할 때 사용합니다.

### IFinResponseWithError

`Error` 속성을 제공하는 인터페이스. `FinResponse<A>.Fail`에서만 구현되며, Logging Pipeline에서 에러 메시지에 접근할 때 사용합니다.

### ICommandRequest\<TSuccess\>

Command 요청 인터페이스. `ICommand<FinResponse<TSuccess>>`를 상속합니다.

### IQueryRequest\<TSuccess\>

Query 요청 인터페이스. `IQuery<FinResponse<TSuccess>>`를 상속합니다.

### ICacheable

캐싱 가능한 요청을 나타내는 인터페이스. `CacheKey`와 `Duration` 속성을 정의합니다. Query Request가 구현하면 Caching Pipeline이 자동으로 캐시를 적용합니다.

---

[← 이전: C. Railway Oriented Programming 참조](C-railway-oriented-programming.md) | [다음: E. 참고 자료 →](E-references.md)
