```shell
Remove-Item -LiteralPath '\\?\C:\ ... \nul'
```

```
DDD/Hexagonal Architecture 관점에서 각 레이어가 자체 DTO를 소유하도록 개선
```
```
 Evans DDD 원칙 적용

│         패턴          │               Evans 원칙               │             현재 위반             │                   개선                    │
├────────┼─────────────────┼──────────────────┼────────────────────┤
│ Specification (Ch.9)  │ 선택·검증·구성의 단일 진원지           │ 쿼리 규칙이 Service에도 중복      │ Specification이 쿼리 규칙의 유일한 소유자 │
│ Repository (Ch.6)     │ 컬렉션 인터페이스로 Aggregate 접근     │ Exists(spec) 선언만 존재          │ Repository가 Specification을 DB에서 실행  │
│ Domain Service (Ch.5) │ 여러 Aggregate에 걸친 순수 도메인 로직 │ 인메모리 필터링(인프라 역할) 수행 │ 순수 검증만 수행 (boolean → 도메인 에러)  │

Functorium 프레임워크 제약

- IDomainService: "순수 함수로 구현 (외부 I/O 없음)", Repository 의존 금지
- Usecase가 Repository 호출 → Domain Service에 결과 전달" 

  Functorium 제약과 Evans의 차이:

  ┌────────────────────┬────────────────────────────────────────┬─────────────────────────────────┐
  │        관점        │               Evans DDD                │           Functorium            │
  ├────────────────────┼────────────────────────────────────────┼─────────────────────────────────┤
  │ Domain Service     │ Stateless (I/O 허용)                   │ Pure (I/O 금지)                 │
  ├────────────────────┼────────────────────────────────────────┼─────────────────────────────────┤
  │ Repository 의존    │ 허용 (인터페이스는 도메인 레이어)      │ 금지                            │
  ├────────────────────┼────────────────────────────────────────┼─────────────────────────────────┤
  │ 도메인 개념 응집도 │ 높음 (Service가 완전한 검증 흐름 소유) │ 낮음 (Usecase + Service로 분산) │
  ├────────────────────┼────────────────────────────────────────┼─────────────────────────────────┤
  │ 테스트 용이성      │ Mock 필요                              │ Mock 불필요                     │
  └────────────────────┴────────────────────────────────────────┴─────────────────────────────────┘
```
- [x] 메일 고유성 개선
- [x] 도메인 서비스 설계 원칙 개선(`순수 -> Stateless (I/O 허용)`)
- [x] 아키텍처 테스트 샘플 추가
- [x] 아키텍처 테스트 재사용 개선: domain, application
- [ ] 아키텍처 테스트 재사용 개선: adapter
- [x] application 레이어 도메인 유효성 반영
- [x] application 레이어 도메인 유효성 entity id
- [ ] ddd 예제 usecase 리뷰
- [ ] 예제에 ddd 출처 명시
- [ ] 예제에 hosts 출처 제거
- [x] 아키텍처 테스트 가이드 개선
---
- [ ] vo 복합 객체도 entity와 동일하게 외부에서 유효성 검사해야하는거 아닐까?
---
- [ ] md와 사이트 링크 표시법 동기화
---
- [x] LayeredArch 예제 작성
- [ ] 도메인 폴더 구성 규칙 통일
- [ ] 애플리케이션 폴더 구성 규칙
- [ ] 코어 레이어(도메인, 애플리케이션) 문서화 규칙
- [ ] domain with types에서 사용하지 않는 클래스 개선
- [ ] domain with types 문서 반영
- [ ] domain with types 폴더 구성 개선
- [ ] domain with types 테스트 폴더 재구성
---
- [ ] public
- [ ] 문서 사이트 오픈?
- [ ] nuget 배포
---
- [ ] application 스킬
- [ ] application 스킬 -> domain skill
- [ ] functorium plugin
  - functorium:application-develop
  - functorium:domain-develop
---
- [ ] release note 스킬
- [ ] release note 스킬 문서화
- [ ] domain 예제 추가(Tests.Hosts)
- [ ] domain skill 보강
  - 문서 생성 4개
  - 요구사항 분석 방법?
  - 트랜잭션?
  - 에러 코드
  - 프로젝트 이름
  - Aggregates 루트 폴더 구성
---
- [x] domain 유효성 검사 설계 원칙 정립
- [x] union 타입 제공
- [x] union 타입 가이드
- [x] Domain 폴더 구성
- [x] Domain skill 초안 개발
- [x] Domain skill 문서화
---
- [x] `아키텍처 -> 가이드`
- [x] `9장 -> 1장`
- [x] 튜토리얼 idnex.md
- [x] 튜토리얼 가이드 문서
- [x] designing-with-types 튜토리얼 (삭제 → samples로 통합)
- [x] designing-with-types 문서화: `요구사항 -> ddd 관점 분석 -> 클래스 설계`
- [x] designing-with-types 리팩토링
---
```
 Plan: Part4-Conclusion 문서 3단계 구조화

 Context

 현재 Part4-Conclusion/00-requirements.md는 개발자 관점의 기술 요구사항 문서다 (나이브 필드 → 문제 → 해결 패턴 → C# 타입). 사용자는 이를 비즈니스
 요구사항 → DDD 분석 → 코드 설계 3단계 문서로 분리하여, 요구사항에서 코드로 가는 사고 과정을 보여주길 원한다.

 파일 구조 변경

 Part4-Conclusion/
 ├── 00-business-requirements.md   ← NEW: 순수 비즈니스 요구사항
 ├── 01-domain-analysis.md         ← NEW: DDD 관점 도메인 분석
 ├── 02-code-design.md             ← NEW: 00-requirements.md 내용 진화
 ├── 03-before-after.md            ← RENAME: 01-before-after.md (내용 유지)
 ├── 03-Final-Contact/             ← UNCHANGED: 코드 디렉토리
 ├── 04-next-steps.md              ← RENAME: 02-next-steps.md (내용 유지)

 - Starlight sidebar: autogenerate 사용 중 (astro.config.mjs:298) → 파일 이름순 자동 정렬, 설정 변경 불필요
 - 03-Final-Contact/는 코드 디렉토리 — Starlight가 .md만 표시하므로 이름 충돌 없음
 - 00-requirements.md는 삭제 (내용이 02-code-design.md로 이동)

 개선 사항 (기존 플랜 대비)

 1. 다이어그램 통일: 3개 문서 모두 Mermaid 사용 (ASCII 트리/다이어그램 제거)
   - 문서 1: stateDiagram-v2 (이메일 인증 상태 기계), classDiagram (개념적 도메인 모델)
   - 문서 2: classDiagram (최종 C# 타입 구조)
 2. 시나리오 추가 (문서 0): 구체적인 사용 시나리오로 비즈니스 규칙을 생생하게 전달
 3. 문서 간 연결 흐름: 각 문서 끝에 다음 문서로의 자연스러운 전환 문장
 4. 불변식 → 구현 패턴 의사결정 (문서 1 → 문서 2 브릿지): 불변식 유형별로 어떤 구현 전략이 필요한지 판단 기준 제시

 ---
 Step 1: 00-business-requirements.md 생성

 title: "비즈니스 요구사항"

 코드 없음, 타입 이름 없음, 기술 패턴 없음. 도메인 전문가가 개발팀에 전달하는 문서 형식.

 구성

 1. 배경 — 연락처 관리 시스템이 필요한 이유 (2~3문장)
 2. 연락처 정보 구성 — Contact가 담는 데이터를 비즈니스 용어로 기술
   - 개인 이름: 이름(필수), 성(필수), 중간 이니셜(선택)
   - 이메일 주소
   - 우편 주소: 주소, 도시, 주, 우편번호
 3. 비즈니스 규칙 — 3개 카테고리로 번호 매김
   - 데이터 유효성 규칙 (이름 50자 제한, 이메일 형식, 주 코드 2자리, 우편번호 5자리)
   - 연락 수단 규칙 (최소 하나 필수, 이메일만/우편만/둘 다 허용)
   - 이메일 인증 규칙 (단방향: 미인증→인증, 재인증 불가, 인증 시점 기록)
 4. 시나리오 — 비즈니스 규칙을 구체적 사례로 보여줌
   - 정상: 이메일만 등록 → 인증 완료
   - 정상: 우편 주소만 등록
   - 정상: 이메일 + 우편 주소 모두 등록
   - 거부: 연락 수단 없이 등록 시도 → 실패
   - 거부: 이미 인증된 이메일을 다시 인증 → 실패
 5. 존재해서는 안 되는 상태 — 비즈니스 언어로 불법 상태 나열
   - 이메일 없이 인증된 상태
   - 연락 수단 없는 연락처
   - 인증된 이메일의 재인증

 핵심 원칙

 - Part0 02-the-contact-domain.md의 9개 필드와 정확히 대응 (MiddleInitial 포함)
 - 비개발자도 읽을 수 있는 수준
 - 마지막에 문서 1로의 전환 문장: "이 요구사항을 소프트웨어로 구현하려면 도메인 모델링이 필요합니다"

 Step 2: 01-domain-analysis.md 생성

 title: "도메인 분석"

 비즈니스 요구사항을 DDD 관점에서 해석. 코드 없음, 하지만 DDD 용어 사용.

 구성

 1. 개요 — 비즈니스 요구사항을 DDD 빌딩블록으로 분해하는 목적 (1~2문장)
 2. 유비쿼터스 언어 — 비즈니스 용어 → 도메인 모델 용어 매핑 테이블
   - 연락처→Contact, 개인 이름→PersonalName, 이메일 주소→EmailAddress, 우편 주소→PostalAddress, 연락 수단→ContactInfo, 이메일 인증
 상태→EmailVerificationState, 주 코드→StateCode, 우편번호→ZipCode
 3. 빌딩블록 분류 — 값 객체 식별 + 판단 근거
   - 값 객체 판단 기준 제시 (식별자 없음, 값 동등성, 불변, 자기 검증)
   - 각 도메인 개념이 값 객체인 이유를 테이블로
   - Contact의 위치: 이 튜토리얼 범위에서는 값 객체 계층에 집중 (Entity/Aggregate 논의는 간략히)
 4. 불변식 분석 — 비즈니스 규칙을 3가지 불변식으로 분류
   - 단일 값 불변식: 이름 50자, 이메일 형식, 주 코드, 우편번호
   - 구조 불변식: 최소 하나의 연락 수단 (이메일만 | 우편만 | 둘 다, "없음" 불가)
   - 상태 전이 불변식: 미인증→인증 단방향, 재인증 불가
 5. 불변식 → 구현 전략 의사결정 — 불변식 유형별 어떤 접근이 필요한지 (브릿지 섹션)
   - 단일 값 불변식 → "생성 시 검증하고 이후 불변으로 보장"
   - 구조 불변식 → "허용된 조합만 표현 가능한 구조가 필요"
   - 상태 전이 불변식 → "상태별 데이터를 분리하고 전이 함수로 규칙을 강제"
 6. 상태 기계 발견 — Mermaid stateDiagram-v2
 stateDiagram-v2
     [*] --> 미인증 : 이메일 등록
     미인증 --> 인증됨 : 인증 완료(시점 기록)
     인증됨 --> 인증됨 : 재인증 시도 ✗ 거부
 7. 도메인 모델 구조 — Mermaid classDiagram (개념적, C# 없음)
 classDiagram
     class Contact {
         개인 이름
         연락 수단
     }
     class 개인 이름 {
         이름
         성
         중간 이니셜?
     }
     class 연락 수단 {
         <<택일>>
         이메일만
         우편 주소만
         이메일 + 우편 주소
     }
     ...

 핵심 원칙

 - 추론 과정을 보여줌 ("왜 값 객체인가?" 근거 제시)
 - Repository, Domain Service 등 이 튜토리얼 범위 밖의 DDD 개념은 다루지 않음
 - 문서 0과 어휘 레지스터가 다름: 비즈니스 언어 → DDD 용어
 - 마지막에 문서 2로의 전환: "이 도메인 모델을 C# 타입으로 구현합니다"

 Step 3: 02-code-design.md 생성

 title: "코드 설계"

 현재 00-requirements.md 내용을 기반으로 진화. DDD 분석 → C# 타입 패턴 매핑 추가. 다이어그램은 Mermaid만 사용.

 구성

 1. 도입부 — "도메인 분석에서 식별한 빌딩블록을 C# 타입 패턴으로 매핑합니다"
 2. DDD 빌딩블록 → C# 패턴 매핑 (새 섹션, 테이블)
   - 단일 값 객체(제약) → SimpleValueObject<T> + Validate
   - 복합 값 객체 → sealed record
   - 구조 불변식(택일) → sealed record union (DU)
   - 상태 기계 → sealed record union + 전이 함수 + Fin<T>
 3. 불변식별 타입 설계 — 기존 Part 1/2/3 테이블 내용을 빌딩블록 기준으로 재배치
   - 단일 값 불변식 → String50, EmailAddress, StateCode, ZipCode
   - 구조 불변식 → ContactInfo union, PersonalName, PostalAddress
   - 상태 전이 불변식 → EmailVerificationState union + Verify 전이 함수
 4. 최종 타입 구조 — Mermaid classDiagram (C# 타입명 사용)
 classDiagram
     class Contact {
         +PersonalName Name
         +ContactInfo ContactInfo
     }
     class PersonalName {
         +String50 FirstName
         +String50 LastName
         +string? MiddleInitial
     }
     class ContactInfo {
         <<abstract>>
     }
     ContactInfo <|-- EmailOnly
     ContactInfo <|-- PostalOnly
     ContactInfo <|-- EmailAndPostal
     class EmailOnly {
         +EmailVerificationState EmailState
     }
     ...
 5. 나이브 필드 → 최종 타입 추적표 — 기존 매핑 테이블 유지

 기존 00-requirements.md 대비 변경점

 - 추가: DDD→C# 매핑 테이블, Mermaid classDiagram
 - 재구성: Part 1/2/3 기준 → 불변식 유형 기준으로 정보 배치
 - 삭제: 나이브 Contact C# 코드 블록 (문서 0의 시나리오가 대체)
 - 삭제: ASCII 트리 (Mermaid classDiagram으로 대체)

 Step 4: 파일 이름 변경

 ┌────────────────────┬────────────────────┬──────────────────────────┐
 │        현재        │      변경 후       │           비고           │
 ├────────────────────┼────────────────────┼──────────────────────────┤
 │ 00-requirements.md │ 삭제               │ 내용 → 02-code-design.md │
 ├────────────────────┼────────────────────┼──────────────────────────┤
 │ 01-before-after.md │ 03-before-after.md │ 내용 변경 없음           │
 ├────────────────────┼────────────────────┼──────────────────────────┤
 │ 02-next-steps.md   │ 04-next-steps.md   │ 내용 변경 없음           │
 └────────────────────┴────────────────────┴──────────────────────────┘

 Step 5: 튜토리얼 루트 index.md 업데이트

 Part 4 행 내용을 업데이트:
 | 4 | 결론 | 비즈니스 요구사항, 도메인 분석, 코드 설계, Before/After 비교, 다음 단계 | 2개 |

 ---
 검증

 1. Starlight 사이드바에 5개 문서가 올바른 순서로 표시되는지 확인
 2. 3개 문서 간 어휘 레지스터 분리 확인:
   - 문서 0: 비즈니스 언어만 (코드/타입 이름 없음)
   - 문서 1: DDD 용어 + Mermaid 개념 다이어그램 (코드 없음)
   - 문서 2: C# 타입 패턴 + Mermaid 타입 다이어그램 (코드 있음)
 3. Mermaid 다이어그램 렌더링 확인 (Starlight은 Mermaid 지원)
 4. 빌드/테스트 영향 없음 확인 (문서 변경만, 코드 변경 없음)
```
```
● Excellent analysis. The Plan agent identified a genuinely better approach: replace the "DDD 분석" document with a "타입 설계 의사결정" document
  organized by invariant categories instead of DDD building blocks. This avoids vocabulary discontinuity (the tutorial never uses DDD terms like
  "유비쿼터스 언어") and directly mirrors the tutorial's discovery narrative.
```

```
 Plan: Part4-Conclusion 문서 3단계 구조화

 Context

 현재 Part4-Conclusion/00-requirements.md는 개발자 관점의 기술 요구사항 문서다. 사용자는 이를 요구사항에서 코드까지의 사고 과정을 보여주는 다단계 문서로
 재구성하길 원한다.

 방법론 선택: 불변식 기반 설계 의사결정

 왜 DDD 분석이 아닌가?

 튜토리얼 본문(Part 1~3)은 DDD 용어("유비쿼터스 언어", "값 객체", "빌딩블록")를 사용하지 않는다. 도메인 개념은 나이브 구현의 버그를 수정하는 과정에서
 발견된다 ("IsEmailVerified=true인데 Email이 null이면?" → union type으로 해결). 결론 문서에 DDD 용어를 도입하면 어휘 단절이 생긴다.

 채택한 접근: 불변식 카테고리 기반

 튜토리얼 자체의 발견 과정과 일치하는 3가지 불변식 카테고리를 축으로 구성:

 ┌──────────────────┬────────────────────────────────┬─────────────────────────────────┬───────────────┐
 │   불변식 유형    │          나이브 문제           │          설계 의사결정          │ 튜토리얼 Part │
 ├──────────────────┼────────────────────────────────┼─────────────────────────────────┼───────────────┤
 │ 단일 값 불변식   │ string이 아무 값이나 허용      │ 생성 시 검증, 이후 불변         │       1       │
 ├──────────────────┼────────────────────────────────┼─────────────────────────────────┼───────────────┤
 │ 구조 불변식      │ nullable 필드로 불법 조합 가능 │ 허용된 조합만 표현 가능한 union │       2       │
 ├──────────────────┼────────────────────────────────┼─────────────────────────────────┼───────────────┤
 │ 상태 전이 불변식 │ bool 플래그로 무효 전이 가능   │ 상태별 타입 + 전이 함수         │       3       │
 └──────────────────┴────────────────────────────────┴─────────────────────────────────┴───────────────┘

 추적 체인

 비즈니스 규칙 (문서 0) → 불변식 + 설계 의사결정 (문서 1) → C# 타입 (문서 2) → 코드 (FinalContact/) → 테스트

 ---
 파일 구조 변경

 Part4-Conclusion/
 ├── 00-business-requirements.md   ← NEW: 순수 비즈니스 요구사항
 ├── 01-type-design-decisions.md   ← NEW: 불변식 기반 설계 의사결정
 ├── 02-code-design.md             ← NEW: 00-requirements.md 내용 진화
 ├── 03-before-after.md            ← RENAME: 01-before-after.md (내용 유지)
 ├── 03-Final-Contact/             ← UNCHANGED
 ├── 04-next-steps.md              ← RENAME: 02-next-steps.md (내용 유지)

 - autogenerate (astro.config.mjs:298) → 파일 이름순 자동 정렬, 설정 변경 불필요
 - 00-requirements.md 삭제 (내용이 02-code-design.md로 이동)

 ---
 Step 1: 00-business-requirements.md 생성

 title: "비즈니스 요구사항"

 코드 없음, 타입 이름 없음, 기술 패턴 없음. 도메인 전문가 관점.

 구성

 1. 배경 — 연락처 관리 시스템이 필요한 이유 (2~3문장)
 2. 연락처 정보 구성 — 비즈니스 용어로 데이터 기술
   - 개인 이름: 이름(필수), 성(필수), 중간 이니셜(선택)
   - 이메일 주소
   - 우편 주소: 주소, 도시, 주, 우편번호
 3. 비즈니스 규칙 — 3개 카테고리, 번호 매김
   - 데이터 유효성: 이름 50자 제한, 이메일 형식, 주 코드 2자리 대문자, 우편번호 5자리
   - 연락 수단: 최소 하나 필수 (이메일만/우편만/둘 다)
   - 이메일 인증: 미인증→인증 단방향, 재인증 불가, 인증 시점 기록
 4. 시나리오 — 규칙을 구체적 사례로
   - 정상: 이메일만 등록 → 인증 완료
   - 정상: 우편 주소만 등록
   - 정상: 이메일 + 우편 주소 등록
   - 거부: 연락 수단 없이 등록 → 실패
   - 거부: 인증된 이메일 재인증 → 실패
 5. 존재해서는 안 되는 상태
   - 이메일 없이 인증된 상태
   - 연락 수단 없는 연락처
   - 인증된 이메일의 재인증

 → 02-the-contact-domain.md의 9개 필드(MiddleInitial 포함)와 정확히 대응

 Step 2: 01-type-design-decisions.md 생성

 title: "타입 설계 의사결정"

 핵심 변경: DDD 분석 대신 불변식 카테고리 기반 설계 의사결정 문서. 튜토리얼 본문의 어휘("제약된 타입", "union type", "상태 기계")를 그대로 사용. DDD 용어
  사용하지 않음.

 구성

 1. 개요 — 비즈니스 규칙을 소프트웨어로 보장하려면, 규칙을 불변식으로 분류하고 각 유형에 맞는 타입 전략을 선택해야 한다 (2~3문장)
 2. 단일 값 불변식 — 개별 필드의 제약
   - 비즈니스 규칙: "이름은 50자 이하", "이메일은 유효 형식", "주 코드 2자리 대문자", "우편번호 5자리"
   - 나이브 문제: string은 아무 값이나 허용 → 형식 위반을 런타임까지 모름
   - 설계 의사결정: 생성 시 검증하고 이후 불변으로 보장 — 유효하지 않은 값은 생성 자체가 불가능
   - 결과 타입: String50, EmailAddress, StateCode, ZipCode
 3. 구조 불변식 — 필드 조합의 제약
   - 비즈니스 규칙: "최소 하나의 연락 수단 필수", "이름 구성요소는 항상 함께"
   - 나이브 문제: nullable 필드로 Email=null + Address=null 허용 → 연락 불가 상태 존재
   - 설계 의사결정: 허용된 조합만 표현 가능한 구조 — 불법 조합을 타입으로 배제
   - 결과 타입:
       - PersonalName (원자적 그룹화)
     - PostalAddress (원자적 그룹화)
     - ContactInfo (EmailOnly | PostalOnly | EmailAndPostal) — "없음" 케이스 부재가 곧 불변식
 4. 상태 전이 불변식 — 시간에 따른 변화의 제약
   - 비즈니스 규칙: "미인증→인증 단방향", "재인증 불가", "인증 시점 기록"
   - 나이브 문제: bool IsEmailVerified는 아무 때나 true/false 전환 가능
   - 설계 의사결정: 상태별 데이터를 분리하고 전이 함수로 규칙 강제
   - 결과: EmailVerificationState (Unverified | Verified) + Verify 전이 함수
   - Mermaid stateDiagram-v2:
   stateDiagram-v2
     [*] --> Unverified : 이메일 등록
     Unverified --> Verified : Verify(timestamp)
     Verified --> Verified : Verify 시도 ✗ 거부
 5. 도메인 모델 구조 — Mermaid classDiagram (불변식 경계 표시, C# 타입명 없음)

 핵심 원칙

 - 각 섹션이 동일 구조: 비즈니스 규칙 → 나이브 문제 → 설계 의사결정 → 결과
 - DDD 용어 대신 튜토리얼 본문의 어휘 사용 ("제약된 타입", "union type", "전이 함수")
 - "왜 이 설계인가?"를 명시 (분류 결과만 나열하지 않음)

 Step 3: 02-code-design.md 생성

 title: "코드 설계"

 설계 의사결정을 C# 타입 패턴으로 매핑. Mermaid classDiagram으로 최종 구조 시각화.

 구성

 1. 도입부 — 설계 의사결정을 C# 구현 패턴으로 매핑 (1~2문장)
 2. 설계 의사결정 → C# 패턴 매핑 테이블

 | 설계 의사결정         | C# 구현 패턴                         | 적용                                       |
 |-----------------------|--------------------------------------|--------------------------------------------|
 | 생성 시 검증 + 불변   | SimpleValueObject<T> + Validate      | String50, EmailAddress, StateCode, ZipCode |
 | 원자적 그룹화         | sealed record + required             | PersonalName, PostalAddress                |
 | 허용 조합만 표현      | abstract record + sealed 케이스 (DU) | ContactInfo                                |
 | 상태 분리 + 전이 함수 | abstract record + Fin<T> 반환 전이   | EmailVerificationState                     |

 3. 불변식별 타입 상세 — 각 불변식 유형별 구체적 타입 설계
   - 단일 값: String50, EmailAddress, StateCode, ZipCode의 Validate 체인
   - 구조: ContactInfo union 3개 케이스, PersonalName, PostalAddress
   - 상태 전이: EmailVerificationState.Verify의 switch 로직
 4. 최종 타입 구조 — Mermaid classDiagram (C# 타입명)
 5. 나이브 필드 → 최종 타입 추적표 — 기존 매핑 테이블 유지

 기존 00-requirements.md 대비 변경점

 - 추가: 설계 의사결정→C# 매핑 테이블, Mermaid classDiagram
 - 재구성: Part 1/2/3 기준 → 불변식 유형 기준으로 배치
 - 삭제: 나이브 Contact C# 코드 블록 (문서 0의 시나리오가 대체)
 - 삭제: ASCII 트리 (Mermaid로 대체)

 Step 4: 파일 이름 변경

 ┌────────────────────┬────────────────────┬──────────────────────────┐
 │        현재        │      변경 후       │           비고           │
 ├────────────────────┼────────────────────┼──────────────────────────┤
 │ 00-requirements.md │ 삭제               │ 내용 → 02-code-design.md │
 ├────────────────────┼────────────────────┼──────────────────────────┤
 │ 01-before-after.md │ 03-before-after.md │ 내용 변경 없음           │
 ├────────────────────┼────────────────────┼──────────────────────────┤
 │ 02-next-steps.md   │ 04-next-steps.md   │ 내용 변경 없음           │
 └────────────────────┴────────────────────┴──────────────────────────┘

 Step 5: 튜토리얼 루트 index.md 업데이트

 Part 4 행:
 | 4 | 결론 | 비즈니스 요구사항, 타입 설계 의사결정, 코드 설계, Before/After 비교, 다음 단계 | 2개 |

 ---
 검증

 1. Starlight 사이드바에 5개 문서가 올바른 순서로 표시되는지 확인
 2. 3개 문서 간 어휘 레지스터 분리 확인:
   - 문서 0: 비즈니스 언어만 (코드/타입 이름 없음)
   - 문서 1: 설계 의사결정 언어 + Mermaid (코드 없음, 결과 타입명만 언급)
   - 문서 2: C# 구현 패턴 + Mermaid classDiagram
 3. 추적 체인 완결성: 문서 0의 모든 시나리오가 문서 1의 불변식에 매핑, 문서 1의 모든 결과가 문서 2의 타입에 매핑
 4. 빌드/테스트 영향 없음 (문서 변경만)
```


- [ ] `Tests.Hosts -> Tests.Samples` ddd 관점 README 문서
- [ ] 예제 문서, 소스 링크
---
- [ ] 사용자 정의 유스케이스 파이프라인
- [ ] 관찰 가능성 튜토리얼

```
ultrathink @Docs.Site\src\content\docs\tutorials\architecture-rules 문서를 기술도서 출판 관점으로 내용을 개선해줘. 단편적인 사실만을 전달하는 관점이
  아닌 독자를 위한 읽기 쉽고, 이해하기 쉽은 방향으로 문장을 다듬어줘. 대부분 사양서(spec) 문체로 작성되어 있다. 단편적 사실을 나열하는 방식을 개선해줘.
  내러티브 품질(Narrative Quality) 개선해줘.
```

```
  - architecture-rules — 8개 파일 누락 (주로 Appendix, index, Part5 결론)
  - cqrs-repository — 10개 파일 누락 (주로 Appendix, index, Part0)
  - functional-valueobject — 17개 파일 누락 (주로 Appendix, index, Part0, 섹션 index)
  - specification-pattern — 8개 파일 누락 (주로 Appendix, index, Part0)
```

- [ ] 커스텀 유스케이스 관찰 가능성
- [ ] IAduit 로그 출력?
- [ ] CRTP 패턴 사례 개선 Book
- [ ] Traverse, TraverseM 이해 LanaguageExt


```
 rm -rf node_modules/.astro

node_modules/.astro

  # 개발 서버 (HMR 지원)
  npm run dev          # http://localhost:4321

  # 프로덕션 빌드
  npm run build

  # 빌드 결과 미리보기
  npm run preview

  현재 npm에서 Segmentation fault가 발생하고 있어서 재시작이 필요하다면 기존 프로세스(PID 41720)를 종료 후 다시 npm run
  dev를 실행해야 합니다. 지금은 이미 실행 중이니 브라우저에서 http://localhost:4321로 접속하시면 됩니다.

npx astro dev
powershell.exe -Command "cd C:\Workspace\Github\Functorium\Docs.Site; npm install"

powershell.exe -Command "Get-Process -Name node -ErrorAction SilentlyContinue | Stop-Process -Force; Start-Sleep -Seconds 3; Get-NetTCPConnection -LocalPort 432…
powershell.exe -Command "cd C:\Workspace\Github\Functorium\Docs.Site; npx astro dev --host 0.0.0.0"

rm -rf .astro node_modules/.astro 2>/dev/null && timeout 60 npx astro dev 2>&1
```

- [ ] 1. 문서 내 상대 링크(.md 확장자)를 Starlight 슬러그 형식으로 변환 (현재 errorOnRelativeLinks: false로 우회)
- [ ] 2. sourcegen-observability 중복 ID 해결 (폴더/index.md + standalone .md 공존)
- [x] Book 폴더 전체 구성 동기화
- [x] 아키텍처 테스트 개선
- [ ] 아키텍처 대상 문서화(비교표)
- [ ] 아키텍처 Book
- [ ] Src\Functorium\Adapters\SourceGenerators\GenerateObservablePortAttribute.cs 폴더 이동
  ```
  Src\Functorium\Adapters\Observabilities\GenerateObservablePortAttribute.cs
  ```
- [ ] https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-10.0 설정 연동
- [x] 레파지토리 패턴 Book
  - N+1 문제 이해 코드
  - DTO
  - Command(레파지토리 패턴 + DTO) vs Query(전용 인터페이스 + No DTO)
- [ ] N+1 정의, 문제점, 개선 방향 Book 개선
- [ ] CRTP 패턴 배경 보강
- [x] 파이프라인 Book
  - 공변성/반공변성
- [ ] 폴더 정리
- [ ] IAudit 로그 출력?
- [ ] 사용자 정의 유스케이스 관찰 가능성
- [x] Bulk 성능 개선
- [x] N+1 개선
- [x] 레파지토리 패턴 구현 베스트 프렉티스
  - 올바른 예제
  - 잘못된 예제
  - FAQ
- [x] Domains 레이어 관점 폴저 정리
- [x] Application 레이어 관점 폴더 정리
- [ ] 01-SingleHost\Src\LayeredArch.Adapters.Presentation\Abstractions\Extensions\FinResponseExtensions.cs Http 전용 확장 메서드
- [ ] DTO Book
- [ ] 공병성 Book
- [ ] 유스케이스 구현 패턴.md
- [ ] cowork 통합?
  - prd - ddd 구현 - ddd 테스트
  - adapter 구현 - 통합 테스트
  - dashboard 통합
  - [ ] 커스텀 Pipeline
- [ ] Adapters 프로젝트 분리
---
- [ ] 정적 사이트 개발
- [ ] ddd 프로제트 포팅: cowork 통합으로
---
- [ ] 복수 서비스 예제
- [ ] 컨테이너 기반 통합 테스트
- [ ] 도메인 이벤트 확장: 내부/외부

---
- [x] `services.AddSingleton<OrderCreditCheckService>();` 도메인 서비스를 의존성 등록한것은 너무 과한것 같다
- [x] 도메일 모데리 nullable 원칙
- [x] ISoftDeletable 적용 사례
  ```
  ISoftDeletable이 DDD에서 왜 필요한가?

  핵심 이유는 참조 무결성과 이력 보존입니다. 이번 구현이 좋은 예시입니다:

  1. 물리 삭제 불가: Product가 삭제되더라도 기존 Order의 OrderLine이 ProductId를 참조하고 있으므로 물리적으로 삭제하면 참조가 깨짐
  2. 비즈니스 의미 분리: "단종(discontinued)"은 도메인 개념이지 데이터 소멸이 아님. 인터페이스로 이 개념을 명시적으로 표현
  3. 복원 가능성: 실수로 삭제하거나 일시적으로 판매 중지할 때 Restore로 복구 가능
  4. 감사 추적: DeletedBy로 누가 삭제했는지 추적 (ISoftDeletableWithUser)
  5. 인프라 관심사 분리: 인터페이스를 통해 Repository/ORM 레이어에서 IsDeleted 기반 자동 필터링을 일관되게 적용할 수 있음 (EF Core의 Global Query Filter 등)
  ```
  ```
  #: 1
  가치: 물리 삭제 불가 (참조 무결성)
  적용 여부: 설계 동기만
  설명: OrderLine→ProductId 참조 관계가 존재하므로 soft delete를 선택한 이유일 뿐, 물리 삭제를 방지하는 코드는 없음
  ────────────────────────────────────────
  #: 2
  가치: 비즈니스 의미 분리
  적용 여부: 적용됨
  설명: Delete()/Restore() 도메인 메서드 + 도메인 이벤트로 "단종" 개념을 명시적으로 모델링
  ────────────────────────────────────────
  #: 3
  가치: 복원 가능성
  적용 여부: 적용됨
  설명: Restore() 메서드, 멱등성, 테스트 검증 완료
  ────────────────────────────────────────
  #: 4
  가치: 감사 추적
  적용 여부: 적용됨
  설명: ISoftDeletableWithUser의 DeletedBy 속성으로 삭제자 추적
  ────────────────────────────────────────
  #: 5
  가치: 인프라 관심사 분리 (Global Query Filter)
  적용 여부: 적용됨
  설명: EF Core Global Query Filter + Dapper BuildWhereClause에서 DeletedAt IS NULL 자동 필터링 적용

  모든 5가지 가치가 적용되었습니다. EF Core Global Query Filter와 Dapper BuildWhereClause에서
  DeletedAt IS NULL 기반 자동 필터링을 일관되게 적용합니다.
  ```
  ```
  1. 참조 무결성 - OK (물리 삭제 없음)
  2. 비즈니스 의미 분리 - OK (도메인 모델에서 Delete/Restore 분리)
  3. 복원 가능성 - OK (Restore 커맨드/엔드포인트 구현 완료)
  4. 감사 추적 - OK (DeletedBy 추적, DeleteProductCommand에서 deletedBy 전달)
  5. 인프라 관심사 분리 - OK (EF Core Global Query Filter + Dapper WHERE DeletedAt IS NULL)
  ```
- [x] 도메인 모델 삭제 패턴 정리????
- [x] IAuditable 적용 사례
- [x] IConcurrencyAware 적용 사례
- [ ] Specification 적용 사례
- [ ] 도메인 모델 패턴
  - ISoftDeletable
  - IAuditable
  - IConcurrencyAware
  - Specification
- [x] Src\Functorium\Applications\Observabilities\IObservablePort.cs 인터페이스 정의 위치를 Domain으로 이동(Repositories 때문에)
- [x] ObservablePortGenerator, ObservablePortRegistration, GenerateObservablePortAttribute???
---
- [ ] 유스케이스 전체 리팩토링
- [ ] .Adapters 프로젝트 분리
- [ ] 도메인 시나리오 문서화
- [ ] 도메인 시나리오 + PRD 문서
- [ ] 도메인 요구사항 + PRD 통합
- [x] 도메일 모델 ㄹ용어집
- [x] 도메인 요구사항
- [ ] 릴리스 노트

---
- [ ] Functorium + Functorium.Adapters 분리
- [ ] `도메인 시나리오(Usecase) -> 도메인 모델(Aggregate Root)` 플러그인

---

- [x] 도메인 모델 정의서 md
- [x] 도메인 모델 정의서 템플릿 md
- [x] 도메인 모델 정의서 템플릿 가이드 md
- [x] 도메인 요구사항 md 문서 개요 개선
  ```
  1. 시스템 목적 — 전자상거래 주문 처리 단일 BC (기존 유지)
  2. 5개 핵심 영역 역할 요약 — 각 애그리거트의 역할과 핵심 속성을 볼드 제목과 함께 서술
  3. 공유 값 객체 — Money, Quantity의 역할과 불변 조건 보장 설명
  4. 애그리거트 간 결합 원칙 — ID 참조만 사용, 구체적 참조 관계 명시
  5. 핵심 비즈니스 규칙 — 단일 애그리거트 규칙(상태 전이, 재고 차감) vs 교차 규칙(신용한도 → 도메인 서비스) 구분
  6. 도메인 이벤트 — 상태 변경 시 이벤트 발행으로 추적/확장 지원

  요구사항 ID(CUS-R01 등)는 포함하지 않았으며, 문서의 나머지 부분을 읽지 않아도 도메인 전체 윤곽을 파악할 수 있는 수준입니다.
  ```

## 01-SingleHost 이해
- [x] 01-SingleHost Doamin 프로젝트 폴더 정리
  - AggregateRoots
    - {Entity}s
      - Specifications/
      - ValueObjects/
      - {Entity}.cs
      - I{Entity}Repository.cs
  - SharedModels
    - Entities
    - ValueObjects
    - Services
- [x] 01-SingleHost Application 프로젝트 폴더 정리
  - Usecases
    - {Entity}s
      - Commands
      - Events
      - Queries
      - I{Entity}Query.cs: DTO 타입
- [x] `On--- -> ---Event` 이벤트 핸들러 이름 변경
- [x] `IProductDetailQueryAdapter -> IProductDetailQuery` Query 인터페이스 이름 변경
- [x] Enum타입 유효성 검사 강화: 표준 값 객체, SortDirection
- [x] IAuditable 인터페이스에 정의된 nullable
- [x] `Specification? -> Option<T>`

## Book
- [ ] 파이프라인 & 공변성
- [ ] 업데이트
- [ ] Lanaguage-Ext 101

## 플러그인
- [ ] 스킬
- [ ] Cowork 통합

## 03-MultipleHosts 예제
- [ ] N개 서비스 프로젝트 생성
- [ ] 도메인 이벤트 RabbitMQ
- [ ] 도메인 이벤트 구분: 내부, 외부
- [ ] 컨테이너 기반 통합 테스트
- [ ] Polly: 타임 아웃, 재시도, 서킷브레이커, ...
- [ ] 릴리스 노트

## 컨터이너 강화
- [ ] Aspire
- [ ] 인증/인가
- [ ] 캐시
- [ ] OpenSearch
- [ ] Api Geteway

---
- [ ] UsecaseTransactionPipeline.cs 제네릭 제약 조건으로 필터링
- [ ] UsecaseTransactionPipeline.cs 로그 개선
---
- [ ] DTO 저장소 적용?
- [ ] UoW 지금은 인터페이스만 제공, 추가 개선 사항이 없을까?
- [ ] Entity & Aggregate 생성 패턴 확인?
---
- [ ] 복수개 서비스 예제 추가
- [ ] Integration Events vs Domain Events
---
- [ ] LanaguageExt RunAsync 취소 처리?
- [ ] usecase 유효성 검사 단순화
- [ ] usecase 파이프라인 유효성 검사 오픈소스 재확인
---
- [ ] MinVer 재도입
- [ ] 빌드 자동화에 코드 품질 분석 포함
  - 코드 포맷
  - 코드 커버리지
  - 정적 분석
  - 코드 복잡도

```
  관심사 3: CorrelationId / CausationId의 위치

  현재 코드 (IDomainEvent.cs:26-33):
  public interface IDomainEvent
  {
      DateTimeOffset OccurredAt { get; }
      Ulid EventId { get; }
      string? CorrelationId { get; }  // ← 도메인 관심사인가?
      string? CausationId { get; }    // ← 도메인 관심사인가?
  }

  Evans 관점: 두 가지 해석이 가능합니다.

  ┌───────────────┬────────────────────────────────────────────────────────────────────┐
  │     해석      │                                근거                                │
  ├───────────────┼────────────────────────────────────────────────────────────────────┤
  │ 도메인에 유지 │ Saga/Process Manager에서 이벤트 인과관계 추적은 도메인 로직의 일부 │
  ├───────────────┼────────────────────────────────────────────────────────────────────┤
  │ 인프라로 분리 │ 분산 추적, 로깅을 위한 메타데이터이며 도메인 전문가의 언어에 없음  │
  └───────────────┴────────────────────────────────────────────────────────────────────┘

  현재 구현에서 CorrelationId와 CausationId는 도메인 핸들러에서 사용되지 않고 Observability 계층(ObservableDomainEventNotificationPublisher)에서만
  참조됩니다. 이는 인프라 메타데이터에 가깝다는 증거입니다.

  개선 방향: Envelope 패턴으로 분리
  // 순수 도메인 이벤트
  public interface IDomainEvent
  {
      DateTimeOffset OccurredAt { get; }
      Ulid EventId { get; }
  }

  // 인프라 envelope (Publisher/Pipeline에서 래핑)
  public record DomainEventEnvelope<T>(
      T Event,
      string? CorrelationId,
      string? CausationId) where T : IDomainEvent;

  단, 프로젝트가 향후 Saga/Process Manager를 도메인 수준에서 구현할 계획이라면 현재 위치가 적절합니다.
```
```
  관심사 4: 시간 의존성의 암묵적 결합

  현재 코드 (DomainEvent.cs:20):
  protected DomainEvent() : this(DateTimeOffset.UtcNow, Ulid.NewUlid(), null, null) { }

  Evans 관점: 도메인 객체가 시스템 시계에 암묵적으로 의존합니다. 이는 두 가지 문제를 야기합니다:

  1. 테스트에서 시간 제어 불가: 이벤트 발생 시각을 검증하려면 근사치 비교(±1초)에 의존해야 함
  2. 도메인 규칙과 시간: "주문은 30분 내에 확인되어야 한다" 같은 시간 기반 규칙을 테스트할 때 비결정적

  개선 방향: .NET 8의 TimeProvider 활용
  protected DomainEvent(TimeProvider? timeProvider = null)
      : this(
          (timeProvider ?? TimeProvider.System).GetUtcNow(),
          Ulid.NewUlid(),
          null, null) { }

  단, 현재 프로젝트에서 시간 기반 도메인 규칙이 없고 이벤트 시각을 정밀하게 검증하는 테스트도 없다면, 실질적 필요가 생길 때 도입해도 충분합니다.

---
  관심사 4: 시간 암묵적 의존 — 보류

  DomainEvent record에 TimeProvider를 주입하려면 두 가지 anti-pattern 중 하나를 선택해야 합니다:
  - 서비스 주입: record(값 타입)에 서비스 의존성 → record 패턴 파괴
  - mutable static: DomainEvent.Clock = fakeTimeProvider → 전역 상태, 스레드 안전성 문제

  현재 코드에 시간 기반 도메인 규칙이 없고, 테스트에서 with { OccurredAt = ... }로 시간 제어가 가능하므로 실질적 필요가 생길 때 도입하는 것이 CLAUDE.md의
  "Simplicity First" 원칙에 부합합니다.
```

- [x] Specification 패턴 문서화
- [x] ER 다이어그램 도구
- [x] UoW 패턴 개선
- [x] UOW과 도메인 이벤트를 유스케이스 파이프라인으로 책임 이동 
- [x] Specifications 패턴 개선
  ```
  ddd-tactical-improvements.md §7에서 식별된 개선 사항: 쿼리 조건이 Repository 메서드에 하드코딩되어 있어 재사용/조합이 불가능합니다.
  
  현재 문제:
  - ExistsByName(ProductName, ProductId?), ExistsByEmail(Email) 등 조건별 메서드 추가 필요
  - 동일 필터 조건의 Repository 간 중복
  - AND/OR 조합 시 새 메서드 생성 필요
  ```
- [x] 도메인 이벤트 인터페이스 적용
- [x] Entity & Aggregate 생성 패턴 개선
- [x] Sqlite 추가
- [x] 옵션 처리 문서화 보강 
---
- [x] SourceGenerator -> SourceGenerators 폴더 이름 변경
- [x] Functorium\SourceGenerators -> Functorium\Adapters\SourceGenerators 폴더 경로 조정
- [x] Src\Functorium\Abstractions\Diagnostics에 CrashDumpHandler 추가
- [x] CrashDumpHandler 가이드 문서 작성
- [x] CrashDumpHandler 테스트 목적의 Tests.Hosts 프로젝트 생성
- [x] CrashDumpHandler 테스트 목적 프로젝트 대상으로 테스트

- [x] `event.id, event.type -> request., response.`
- [x] `event.id -> ` 로그 통합
- [ ] `response.event.type` 확인 필요?
- [ ] GitHub Action에 코드 품질 분석 포함
- [x] 소스 생성기 폴더 이름 변경
- [ ] 소스 생성기 Book 업데이트
- [ ] https://code.claude.com/docs/ko/memory
- [ ] [Claude 메모리 아키텍처 살펴보기 - CLAUDE.md, Memory Tool까지](https://goddaehee.tistory.com/433)
- [x] crash
- [ ] io + polly: timeout, retry, 서킷브레이커, ...
- [ ] 동적 입출력 테스트 보강
- [ ] opensearch 모든 로그 수집
- [ ] 필드 개선
---
- [ ] crash 이해
- [ ] 도메인 이벤트 publisher adapter
- [ ] 도메인 이벤트 부분 실패 처리? PublishEventsWithResult?
- [ ] 로그 문서화
---
- [ ] 도메인 이벤트 지표
- [ ] 도메인 이벤트 추적: 부모/자식 관계
---
- [ ] `ValidationRules<ProductName>.NotEmpty(value ?? "")  value ?? "" -> value`
- [ ] `FinT<IO, bool> ExistsByName(ProductName name, ProductId? excludeId = null); -> Option<ProductId> excludeId`
- [ ] LINQ 과정에서 발생하는 예외 -> Fin<T> 실패로 처리
- [ ] As().ToFin()
  ```cs
  return (name, price, stockQuantity, description.Value)
    .Apply((name, price, stockQuantity, description) =>
        Product.Create(
            ProductName.Create(name).ThrowIfFail(),
            description,
            Money.Create(price).ThrowIfFail(),
            Quantity.Create(stockQuantity).ThrowIfFail()))
    .As()
    .ToFin();

  .Bind()
  .(...)
    .Apply()
  ```
---
- [x] IObservablePort 인터페이스 구현 가이드 문서 통합
- [x] Tests.Hosts\01-SingleHost 프로젝트의 레이어 폴더 재구성
- [x] 서비스 레이어 구성 가이드 문서
- [x] Guide 폴더 README 문서 업데이트
- [x] 01-SingleHost 도메인 이벤트 핸들러에서 예외 추가
- [x] 관찰 가능성 문서 분리
- [x] 기존 관찰 가능성 문서 개선 "_" -> 제거
- [x] product-management 플러그인 설치
  ```
  claude plugin marketplace add anthropics/knowledge-work-plugins
  claude plugin install product-management@knowledge-work-plugins
  ```
- [x] 동적 로그 값 위치를 마지막으로 이동: with
- [x] Usecase, Domain 이벤트 발생과 수신 관련 로그 형식 테스트 추가
- [x] {request.layer} {request.category} {request.handler}.{request.handler.method} responded failure
- [x] `domain_event.handler -> usecase`
- [x] `request.handler.cqrs -> request.category.type`
- [x] request.category.type: event
- [x] requestCqrs -> requestCategoryType 변수명
- [x] `application -> adapter`
- [x] `request.category: domain_event.publisher -> domain_event`
- [x] EvnetIds 조정
  ```
  Request	3001	event.request
  Success	3002	event.response.success
  Warning	3003	event.response.warning
  Error	3004	event.response.error
  ```
- [x] 도메인 이벤트
  - `aggregate.type`
  - `event.type`
  - `event.id`
  - `event.occurred_at`
  ```
  ObservableDomainEventPublisher.cs:
    │  라인   │         하드코딩된 키          │       용도       │
    │ 55, 182 │ "aggregate.type"               │ Aggregate 타입명 │
    │ 118     │ "event.type"                   │ 이벤트 타입명    │
    │ 119     │ "event.occurred_at"            │ 이벤트 발생 시간 │
    │ 56, 183 │ "request.event.count"          │ 요청 이벤트 개수 │
    │ 206     │ "response.event.success_count" │ 성공 이벤트 개수 │
    │ 207     │ "response.event.failure_count" │ 실패 이벤트 개수 │
  ObservableDomainEventNotificationPublisher.cs:
    │ 라인 │ 하드코딩된 키 │      용도      │
    │ 164  │ "event.type"  │ 이벤트 타입명  │
    │ 165  │ "event.id"    │ 이벤트 고유 ID │
  ```
- [x] ~~publish 메서드 통합~~
---
- [ ] entity-guide.md 재구성
- [ ] usecase-implementation-guide.md 재구성
---
- [ ] DTO Usecase
- [ ] DTO FastEndpoint
- [ ] DTO IObservablePort
- [ ] DTO EFCore + Entity?
- [ ] DTO 성능 고려
---
- [ ] 커스텀 관찰 가능성 Usecase
- [ ] 커스텀 관찰 가능성 IObservablePort
---
- [x] 소스 생성기 프로젝트 이름 변경 또는 통합?
  - Functorium.SourceGenerators
- [ ] 스케줄러
- [ ] 2개 호스트
---
- [ ] Functorium.Adapters 프로젝트 분리
- [ ] Functorium.SourceGenerators
- [ ] Error 구조적 로그 예외


<br/>
<br/>
<br/>



- [x] SelectMany 확장 메서드 개선
  ```
  │ Source              │     Selector        │ 현재 상태 │
  -----------------------------------------------------------
  │ Fin<A>              │ FinT<M, B>           │ ✅ 있음   │
  │ IO<A>               │ FinT<IO, B>          │ ✅ 있음   │
  │ Validation<Error, A> │ FinT<M, B>           │ ✅ 있음   │
  │ FinT<M, A>           │ Fin<B>               │ ❌ 누락   │
  │ FinT<IO, A>          │ IO<B>                │ ❌ 누락   │
  │ FinT<M, A>           │ Validation<Error, B> │ ❌ 누락   │
  ```
- [x] 도메인 이벤트 트랜잭션과 관계? 트랜잭션 후
- [ ] Traverse, TraverseM
- [ ] 내부/외부 도메인 이벤트 구분?
- [ ] 2.1 Request/Response 데이터가 단일 속성에 중첩
  ```
  **현상**:
  ```json
  {
    "Properties": {
      "Data": {
        "Value": {
          "ProductId": "01KGGKDX...",
          "Name": "TestProduct",
          "Price": 100000
        },
        "IsSucc": true,
        "IsFail": false,
        "$type": "Succ"
      }
    }
  }

  **문제**:
  - 로그 분석 시스템에서 `ProductId` 필드로 직접 필터링 불가
  - `Data.Value.ProductId`처럼 깊은 경로 탐색 필요
  - 검색 인덱싱 비효율

  **개선안**:
  ```json
  {
    "Properties": {
      "Layer": "application",
      "Category": "usecase.command",
      "Handler": "CreateProductCommand",
      "Status": "success",
      "DurationMs": 248.7,
      "Request": {
        "Name": "TestProduct",
        "Price": 100000
      },
      "Response": {
        "ProductId": "01KGGKDXN48CHR54KF22P4AM9G"
      },
      "ProductId": "01KGGKDXN48CHR54KF22P4AM9G",
      "ProductName": "TestProduct"
    }
  }
  ```
- [x] 도메인 이벤트 관찰 가능성 클래스 위치 조정
- [x] 도메인 이벤트 관찰 가능성 테스트 클래스 위치 조정
- [x] 도메인 이벤트 발생 로그 클래스 이름 개선: PublisherLoggerExtensions.cs
- [x] 도메인 이벤트 핸들러 로그 의존성 버그 해결
- [x] 관찰 가능성 로그 메서드 이름 통일화
- [x] 로그 확장 메서드 이름 규칙 문서
- [x] 소스 생성기에 로그 확장 메서드 이름 규칙 적용
- [x] snapshot 테스트 결과 폴더 그룹화
- [x] ~~RegisterDomainEventPublisher 연속 함수?~~
- [ ] options.ServiceLifetime = ServiceLifetime.Scoped; 필요성?
- [ ] options.ServiceLifetime = ServiceLifetime.Scoped; 기본이 singletone인데. 파이프라인할 때도 변경했던것 같은데???
  ```
  services.AddMediator(options =>
          {
              options.ServiceLifetime = ServiceLifetime.Scoped;
              options.NotificationPublisherType = typeof(ObservableDomainEventNotificationPublisher);
          });
          // =================================================================
          // 도메인 이벤트 발행자 등록 (Publisher 관점 관찰 가능성 활성화)
          // =================================================================
          services.RegisterDomainEventPublisher(enableObservability: true);
  ```

- [x] 2.5 타임스탬프 정밀도 및 형식
  ```
  **현상**:
  ```json
  {
    "@t": "2026-02-03T01:56:16.000Z"
  }
  ```

  **개선안**:
  ```json
  {
    "@t": "2026-02-03T01:56:16.1658878Z",
  ```
- [x] 지표 등록 기본 코드로 흡수: services.AddMetrics()
- [x] 소스 생성기에서 Attribute 중복 제거
- [x] 튜토리얼 번호 정정 05 <-> 06
- [x] 빌드 스크립트에서 레이어별 코드 커버리지 기능 제거
- [x] 소스 생성기 Generator 코드 위치 정정
- [x] Tests.Hosts - SingleHost 대상으로 .md 문서 적용, 문서 개선
- [x] 엔티티 기본 생성자 경고
- [x] `Fin.Fail<T> -> AdapterError`
- [x] 도메인 이벤트 중첩 클래스
- [x] 도메인 이벤트 Pub
- [x] 도메인 이벤트 구현 관련 테스트 구현
- [x] ~~도메인 이벤트 Sub `Handler -> subscriber~~`
- [X] 도메인 이벤트 Sub `namespace LayeredArch.Application.EventHandlers;` Usecase???
- [x] 도메인 이벤트 핸들러는 CQRS에서 Query?
- [x] 도메인 이벤트 비동기 wait?
- [ ] 도메인 이벤트 실패 인식이 유스케이스에 전파 및 처리할 필요가 있는가?
- [x] 도메인 이벤트 핸들러 등록 방법은?

<br/>

- [x] FinResponse.cs 파일 정리
- [x] Books ValueObject 13 ~ 15 Framework 제거해서 개선
- [x] 값 객체 유효성 검사 폴더 재구성: Typed, Contextual
- [x] 문서 정리: 에러
- [x] 문서 정리: 값 객체
- [x] 문서 생성: 엔티티
- [ ] Tutorials 폴더에 Entity, EntityId, ... 등 적용
---
- [ ] AggregateRoot 사례
- [ ] DomainEvent 사례
- [ ] ISoftDeletable 사례
- [ ] IAuditable 사례
---
- [ ] IObservablePort.md
- [ ] IObservablePort.DTO.md
- [ ] Usecase
- [ ] Usecase.DTO.md
---
- [ ] MinVer
- [ ] 아키텍처 다이어그램
---
- [ ] Scheduling
- [ ] HTTP Method
---
- [ ] 1개 Host: Scheduling
- [ ] 3개 Host: Scheduling + HTTP + RabbitMQ
---
- [ ] Observability Usecase 커스텀
- [ ] Observability IObservablePort 커스텀
---
- [ ] dotnet new template
- [ ] Usecase 구현 스킬
  - [ ] ValueObject
  - [ ] Entity
  - [ ] Aggregate Root
  - [ ] DomainEvent
- [ ] VS Code 개발 환경
- [ ] GitHub NuGet 배포
- [ ] 유스케이스 구현 SKILL
- [ ] 유스케이스 문서 SKILL
- [ ] Books ValueObject 재구성
- [ ] Books 관찰 가능성
- [ ] Books Entity
- [ ] SoftDelete 이해?
- [ ] Cache
- [ ] ORM
- [ ] ORM CQRS
- [ ] Outbox
- [ ] Scheduling
- [ ] Http
- [ ] 문서 사이트
- [ ] 예제 변환 ddd
- [ ] 예제 변환 eShop
- [ ] 예제 변환 ...
---
- [ ] Aspire 통합
- [ ] Dapr 통합


```
솔루션 구성
서비스 구성
  - 레이어 구성
  - 레이별 프로젝트 참조
  - using.cs
  - AssemblyReference.cs
  - 레이어별 프로젝트 주/부수 목표 정의
  - 부수 목표: 의존성 등록


- 용어 문서
- 유스케이스 문서
```

## 로드맵
### AI 도구
- [ ] 개발 관리: https://github.com/glittercowboy/get-shit-done
- [ ] 웹앱 개발: https://github.com/opactorai/Claudable
- [ ] 서버 개발: 자체
- [ ] 로컬 검색: https://github.com/marcoaapfortes/Mantic.sh

### 1.0-alpha.2
- [x] 관찰 가능성
- [x] Error Fluent 적용
- [x] 값 객체 Validation 통합
- [ ] Entity
- [ ] DTO
- [ ] MinVer

### 1.0-alpha.3
- [ ] 관찰 가능성 시스템: OpenSearch, Kafka, Flink SQL
- [ ] 컨테이너 기반 테스트 .Testing
- [ ] Adapters 프로젝트 분리

### 1.0-alpah.4
- [ ] 프로젝트 변환
  - [How We Fixed Our Cache Stampede Problem](https://medium.com/@aman.toumaj/mastering-domain-driven-design-a-tactical-ddd-implementation-5255d71d609f)
  - [Clean Architecture in .NET: The Foundation That Changes Everything](https://medium.com/@compileandconquer/clean-architecture-in-net-the-foundation-that-changes-everything-6fb4425fa402)
  - [Clean Architecture in .NET: Building the Domain & Application Layers](https://medium.com/@compileandconquer/clean-architecture-in-net-building-the-domain-application-layers-d97c6d4928bc)
  - [https://medium.com/@compileandconquer/clean-architecture-in-net-infrastructure-presentation-layers-69b6fb37ac3f](https://medium.com/@compileandconquer/clean-architecture-in-net-infrastructure-presentation-layers-69b6fb37ac3f)
  - [Clean Architecture in .NET: Testing, Best Practices & Final Thoughts](https://medium.com/@compileandconquer/clean-architecture-in-net-testing-best-practices-final-thoughts-1ae7316e0004)
- [ ] Entity, EntityId, EFCore 통합
- [ ] Event: Internal(Mediator) vs External(RabbitMQ)
- [ ] 관찰 가능성 Adpater HTTP FastEndpoint
- [ ] 관찰 가능성 Adpater DB EFCore
- [ ] 관찰 가능성 Adpater DB Dapper
- [ ] 관찰 가능성 Adpater MQ Wolverine

### 1.0-alpha.5
- [ ] TngTech.ArchUnitNET 다이어그램
- [ ] VSCode 개발 환경 구축: 테스트, 코드 커버리지, DevKit
- [ ] 단일 호스트 예제
- [ ] Unit of Work

### 1.0-alpha.6
- [ ] Cache: https://medium.com/@skd9000/how-we-fixed-our-cache-stampede-problem-3b2e6ac01b27
- [ ] Validation Pipeline만 Applications 레이어에 배치
- [ ] 문서 사이트: Astro, Starlight
  - 한국어 검색(하이라이트)
  - 이미지 확대


### 1.0-alpha.7
- [ ] Aspire 통합
- [ ] 복수 호스트 예제
- [ ] Outbox
- [ ] Wolvernine

### 1.0-alpha.8
- [ ] GitHub을 이용한 성능 회귀 테스트

### 그 외
- [ ] 프로젝트 변환
  - [eShop](https://github.com/dotnet/eShop)
- [ ] LanaguageExt 101
  - [ ] Unit
  - [ ] Error
  - [ ] Validation
  - [ ] Fin
  - [ ] IO
  - [ ] FinT
  - [ ] Seq
  - [ ] **Traverse**
  - [ ] Option
- [ ] Feature Flag
- [ ] Claude Code 스킬로 만들기
- [ ] https://github.com/cjo4m06/mcp-shrimp-task-manager
- [ ] https://github.com/glittercowboy/get-shit-done

## 목표
- [ ] 관찰 가능성 문서화 및 테스트 자동화

## 할일
- [x] 이름 충돌 개선: `로컬 Validate 메서드와 Functorium.Domains.ValueObjects.Validations.Validate<T> 제네릭 클래스 간의 이름 충돌`
- [ ] 타입 캐스팅 개선
  ```
  문제 1: .As() 필요 - Apply 패턴 후 K<Validation<Error>, T> → Validation<Error, T> 변환

  문제 2: (Validation<Error, T>) 명시적 캐스팅 필요:
  - LINQ query expression (from...in)에서 implicit 변환 미작동
  - 튜플에서 TypedValidation과 Validation 혼합 시 타입 추론 실패


  문제 1.
  4. TypedValidation과 Apply: 튜플 내에서 TypedValidation을 Validation으로 명시적 캐스팅 필요

    public static Validation<Error, (string BaseCurrency, string QuoteCurrency, decimal Rate)> Validate(
        string baseCurrency, string quoteCurrency, decimal rate) =>
        (ValidateCurrency(baseCurrency, "BaseCurrency"),
         ValidateCurrency(quoteCurrency, "QuoteCurrency"),
         (Validation<Error, decimal>)Validations.Validate<ExchangeRate>.Positive(rate))
            .Apply((b, q, r) => (BaseCurrency: b, QuoteCurrency: q, Rate: r))
            .As()
            .Bind(v => ValidateDifferentCurrencies(v.BaseCurrency, v.QuoteCurrency)
                .Map(_ => (v.BaseCurrency, v.QuoteCurrency, v.Rate)));
  ```
- [ ] As() 필요 여부
  ```
  사용 패턴 요약
  ┌────────────────────────────────────────────┬─────────────────┐
  │                    패턴                  │ .As() 필요 여부 │
  ├────────────────────────────────────────────┼─────────────────┤
  │ LINQ query expression with TypedValidation │ 불필요          │
  ├────────────────────────────────────────────┼─────────────────┤
  │ Tuple에 TypedValidation 포함               │ 불필요          │
  ├────────────────────────────────────────────┼─────────────────┤
  │ Tuple 전체가 Validation<Error, T>          │ 필요            │
  └────────────────────────────────────────────┴─────────────────┘
  ```
- [ ] As() 필요성
- [ ] 확장 메서드 C# 14 기반으로 업데이트
- [ ] GitHub\DomainDrivenDesignUniversity-master\src\Shopway.Application\Utilities\FluentValidationUtilities.cs 코드 확인
- [ ] 13 ~ 16 프로젝트 개선 Framework 자체 코드
- [ ] ?? ""
- [ ] using 구문 개선?
  ```
  using DomainError = Functorium.Domains.Errors.DomainError;
  using DomainErrorType = Functorium.Domains.Errors.DomainErrorType;
  ```
- [ ] DTO IRequest/IResponse
- [ ] DTO IObservablePort
- [x] 16-Architecture-Test 을 15번 기반으로 개선
- [x] Validate.cs -> Validate.{범주}.cs
  ```
    전체 솔루션 (Functorium.All.slnx):
  - Books 프로젝트에 306개의 기존 에러가 있습니다
  - 이 에러들은 현재 리팩터링 이전부터 존재하던 것입니다
  - 문제:
    a. 잘못된 타입 alias: Functorium.Domains.ValueObjects.DomainError → Functorium.Domains.Errors.DomainError
    b. 누락된 namespace: using Functorium.Domains.Errors;
    c. 누락된 namespace: using Functorium.Domains.ValueObjects.Validations;

  PowerShell 스크립트로 자동 수정을 시도했으나 Windows의 인코딩 문제로 파일 손상이 발생했습니다.

  권장 사항:
  Books 프로젝트의 기존 에러는 수동으로 하나씩 수정하거나, IDE의 일괄 리팩터링 기능을 사용하는 것이 안전합니다.
  ```
---

---
- [x] 개선한 Error 표준 타입과 Validate 문서화 업데이트
- [x] 값 객체 Validate 관련 내용 Book 추가
- [x] 관찰 가능성 매뉴얼
- [x] 도메인 에러 개선 DoaminErrorTtype
- [x] DoaminErrorTtype Should 전체 타입 대상으로 확대
- [x] ApplicationError 정의 및 테스트 방법 확장
- [x] AdapterError 정의 및 테스트 방법 확장
- [x] 에러 `개발 가이드 문서`
- [x] RegisterSingletonObservablePortFor: Src\Functorium\Abstractions\Registrations\ObservablePortRegistration.cs
- [x] 값 객체 ValidationPipeline
- [x] Enum 값 객체 ValidationPipeline
- [x] Validation<Error, Currency> Validate(string? value) -> Validation<Error, string?> Validate(string? value)
- [x] ~~Validate<T> Domain, Application, Adapter 통합?~~
- [x] valueobject-implementation-guide.md 문서 업데이트
  - 유효성 검사
  - Enum 타입
- [x] Validate 성능 비교 예제 추가
- [x] `MustSatisfyValueObjectValidation<Request, decimal, decimal>` 타입 노출 최소화
- [ ] Validation<Error, T> Validate(T value) 검토을 위한 아키텍처 테스트 구현
---
- [ ] IObservablePort 인터페이스 `개발 가이드 문서`
---
- [x] ValidationPipeline에 Domain Validate 통합
- [x] 값 객체 구현 개발 가이드 문서
---
- [ ] 로그 테스트 학습
- [ ] 예외 Error 타입으로 표현?? 구조화 검증
- [ ] IOption<T> 학습
- [ ] 히스토그램 학습
---
- [x] Category??? 대문자
- [ ] Http 의존성 등록 + Pipeline
- [x] Validation LINQ 확장
---
- [ ] public partial class Program { } 인터페이스화
- [ ] Extensions 이름 개선
- [ ] Pipeline 로그 생성자?
- [ ] 관찰 가능성 usecase 확장 1개, response._ <- 커스텀 타입
  - Logging
  - Tracing
  - Metrics
- [ ] 관찰 가능성 adapter 확장
  - Logging
  - Tracing
  - Metrics
---
- [x] service.name, service.namespace ??? 서비스 묶음?
- [x] snapshot 테스트 코드 정리
- [x] Meter 이름 형식 개선
- [x] 테스트 전용 프로젝트 추가: Tests.Hosts
- [x] 테스트 전용 프로젝트 개선: Using.cs
- [x] 테스트 전용 프로젝트 개선: Application 레이어에서 로깅 제거
- [x] 테스트 전용 프로젝트 개선: Application과 Presentation -> Feature 기반 폴더 구성
---
- [x] 테스트 전용 프로젝트 기반으로 Logging, Tracing, Metrics 필드 통합 테스트
  Item    | Application | Adapter
  ---     | ---         | ---
  Logging | x           | x
  Tracing | x           | x
  Metrics | x           | x
- [x] 기존 Logging, Tracing, Metrics 필드 단위 테스트 제거
---
- [ ] LayeredArch from _ in guard(!exists, ApplicationErrors.ProductNameAlreadyExists(request.Name)) ??? 도메인 로직
- [ ] LayeredArch DTO?
- [ ] https://medium.com/@aman.toumaj/mastering-domain-driven-design-a-tactical-ddd-implementation-5255d71d609f
- [ ] CleanArch 프로젝트 정리
---
- [ ] EntityId
- [ ] Entity
---
- [ ] Mediator 경고 이해 Tip 폴더
- [ ] MinVer
---
- [x] ElapsedTimeCalculator 클래스 네임스페이스 변경 -> namespace Functorium.Adapters.Observabilities;
- [x] internal record ErrorCodeExpected -> public ???, IHasErrorCode
- [x] 컴파일러 경고 제거
---
- [x] 로그 @error? 테스트 -> error.type, error.code, @error로 개선
- [ ] LoggerMessage.Define 6개 제한 -> 직접 개발
---
- [ ] opensearch 관찰 가능성 시스템 구축
- [ ] opensearch + kafka 관찰 가능성 시스템 구축
- [ ] opensearch + kafka + flink sql 관찰 가능성 시스템 구축
- [ ] prometheus + ... + grafana 관찰 가능성 시스템 구축
---
- [ ] 관찰 가능성 중에서 ValidatorPipeline만 Application 레이어 배치
- [ ] 값 객체와 ValidatorPipeline 통합
---
- [x] Pipeline을 Application 레이어에서 Adapter 레이어도 이동 시킴
- [x] Pipeline 의존성 등록을 OpenTelemetry 등록과 통합 시킴
- [x] Observability 구현에서 기존 인터페이스들을 모두 OpenTelemetry 구체 기술로 변경(Adapter 레이어여 변경되었기 때문)
- [x] Pipeline 테스트 정리
- [x] 솔루션 파일 분리 2개(핵심, 전체)
- [x] Usecase 접근 제어자 public으로 변경(Mediator 패키지 제약 조건)
- [x] LanguageExt Tracing 일 때 Activity 손신 관련 코드 제거(불필요)
- [x] Logging 필드 이름 형식을 Metrics와 Tracing과 통일 시킴(PascalCase -> snake_case + dot)
- [x] 입출력 데이터 형식
  - application 레이어: message
  - adapter 레이어: params/result
- [x] Metrics Application과 Adapter 필드 통일
- [x] Metrics Adapter에 @error?
- [x] 관찰 가능성 이름 정의 클래스 정리
- [x] 관찰 가능성 시간을 모두 "초" 단위로 통일
- [x] 정규식 소스 생성기 기반으로 개선
- [x] 필드 정의 문서화
  Item    | Application | Adapter
  ---     | ---         | ---
  Logging | O           | O
  Tracing | O           | O
  Metrics | O           | O
- [x] Metrics SLO 정의 및 설정?
---
- [ ] Mediator Singleton + Scoped(Factory로 통합)
- [x] UsecaseMetricsPipeline 클래스 정리
- [x] UsecaseMetricsPipeline 태그 구조 테스트
- [x] usecase request/response 태그 통일
- [ ] 에러 코드 일반화?
- [x] usecase request/response 값 소문자 -> 파스칼?
- [ ] 데이터 적합?
- [x] 로그 태그 통일?
- [ ] RequestHandlerMethod vs ResponseStatus 불일치 - 메트릭 그룹화 시 문제 가능?
- [x] aspire 대시보드 확인(계층 구조)
- [x] 로그/추적/지표 md 문서 기준으로 비교
- [ ] 코드 이해를 위한 예제 코드 작성
- [ ] 코드 이해를 위한 학습 문서 작성
- [ ] 관찰 가능성 코드 리뷰: 최적화
- [x] elapsed 단위
- [ ] wolverine IHost?
- [ ] Mediator 어셈블리 단위로 의존성 등록?
- [ ] Testcontainers을 이용한 RabbitMQ 통합 테스트
- [ ] Program 클래스 가시성 -> I인터페이스
- [ ] Testing 프로젝트 기능 단위로 폴더 재구성
- [ ] Testing 프로젝트 RabbitMQ 통합 테스트 재사용 코드
- [ ] Testing 프로젝트 관찰 가능성 단위 테스트 재사용 코드
- [ ] wolverine 관찰 가능성 의존성 등록 패턴 학습
- [ ] wolverine 관찰 가능성 데이터 확인: 지표, 추적, 로그?
- [ ] wolverine 관찰 가능성 의존성 등록 패턴 적용
- [ ] IObservablePortMetric, IObservablePortTrace 이해
- [ ] IObservablePort 구현 가이드
- [ ] 의존성 등록 가이드
- [ ] 커스텀 유스케이스 로그
- [ ] 커스텀 유스케이스 지표
- [ ] 커스텀 유스케이스 추적
- [ ] 유스케이스 진단 대시보드?
  - 바쁜가?
  - 빠른가?
  - 실패율?
- [ ] Application 레이어 테스트
- [ ] Observability 코드 리뷰
- [ ] IObservablePortMetric/IObservablePortTrace 인터페이스 의존성 등록 코드 정리

## Framework
### Abstractions
- [x] Structured Error
- [ ] Dependency Registration

### Adapter Layer
- [x] Configuration Option
- [x] Observability(Logging, Tracing, Metrics)
- [ ] Scheduling
- [ ] HTTP Method
- [ ] MQ
- [ ] ORM

### Application Layer
- [x] CQRS
- [x] Pipeline
- [x] IObservablePort(Observability)
- [ ] Usecase(LINQ: FinT, Fin, IO, Guard, Validation)

### Domain Layer
- [x] Value Object
- [ ] Entity

## Infrastructure
### AI
- [x] Commit
- [x] Release Note
- [ ] Sprint Plan

### Testing
- [x] MTP(Microsoft Testing Platform)
- [x] Code Coverage
- [x] Architecture Testing
- [x] Snapshot Testing
- [ ] Container Testing
- [ ] Performance Testing

### CI/CD
- [ ] Versioning
- [x] Local Build
- [x] Remote Build
- [ ] Remote Deployment

### System
- [ ] Observability

## Entity
### 목표
- Entity Id
- Entity

---
- [x] release note 정리
- [x] 네임스페이스 업데이트
- [x] GitHub Release 스크립트
- [x] GitHub Release
---
- [x] Pipeline 의존성 등록 개선
- [x] Cqrs04Endpoint 프로젝트 생성
- [x] Cqrs04Endpoint 기준으로 Trace 코드 검증
- [x] 계층 구조 테스트
- [x] aspire 대시보드 구축
- [x] Cqrs06Services -> Cqrs06Services 변경
- [x] 메서드 이름 개선 MetricRecorder
  - RecordRequest
  - RecordResponseSuccess
  - RecordResponseFailure

```
높음 (권장)
미사용 메서드 정리: ISpanFactory.CreateSpan() 주석 제거
Span 이름 형식 통일: "application usecase" → "application.usecase" (점 구분자)

중간 (선택)
밀리초→초 변환 주석 명확화 (MetricRecorder)
ObservabilityNaming 클래스 분리 (288줄 → 기능별 분리)

낮음 (선택)
SmartEnum Protocol 선택 기준 문서화
```

### Observability 코드 최적화 (관찰 가능성 코드 리뷰 결과)

#### 높은 우선순위 (권장)

- [x] **OpenTelemetryMetricRecorder - TagList 구조체 사용**
  - 현재: `KeyValuePair<string, object?>[]` 배열 할당 (힙 메모리)
  - 개선: `TagList` 구조체 사용 (스택 메모리, GC 부담 감소)
  - 대상 메서드: `RecordRequestStart`, `RecordSuccess`, `RecordFailure`
  ```csharp
  // 변경 전
  KeyValuePair<string, object?>[] tags = [ ... ];

  // 변경 후
  TagList tags = new()
  {
      { ObservabilityNaming.CustomAttributes.RequestLayer, ... },
      // ...
  };
  ```
- [x] **OpenTelemetryMetricRecorder - Dictionary TryGetValue 패턴**
  - 현재: `ContainsKey` + 인덱서 (2회 조회)
  - 개선: `TryGetValue` 패턴 (1회 조회)
  - 대상 메서드: `EnsureMetricsForCategory`
  ```csharp
  // 변경 전
  if (_metrics.ContainsKey(category))
      return;

  // 변경 후
  if (_metrics.TryGetValue(category, out _))
      return;
  ```

#### 중간 우선순위 (선택)

- [ ] **OpenTelemetryMetricRecorder - ConcurrentDictionary 고려**
  - 현재: `Dictionary` + `lock` (write 동기화)
  - 개선: `ConcurrentDictionary` (read 락 없음, 읽기 집약적 워크로드에 적합)
  - 조건: 메트릭 조회가 쓰기보다 훨씬 빈번한 경우

#### 낮은 우선순위 (선택)

- [x] **OpenTelemetrySpanFactory - ActivityTagsCollection 최적화**
  - 현재: `new ActivityTagsCollection()` 초기화 구문
  - 개선: 정적 태그 배열 재사용 또는 `TagList` 사용
  - 영향: Span 생성 시마다 발생하는 작은 할당
- [ ] ~~**ActivityContextHolder - Scope 구조체 변환**~~
  - 현재: `ActivityScope`, `ContextScope` 클래스 (힙 할당)
  - 개선: `ref struct` 변환 (스택 할당)
  - 제약: `IDisposable` 인터페이스 구현 불가, `using var` 사용 불가
  - 대안: 수동 `try-finally` 패턴 필요

---
- [x] 경고 제거
---
- [x] CqrsObservability -> Cqrs06Services
---
- [ ] ObservabilityNaming 정리(Logger 통합?)
- [ ] Release할 때 NuGet 패키지 버전 불일치
  - Functorium.1.0.0.nupkg
  - Functorium.1.0.0-alpha.1.nupkg
---
- [x] Unit Testing 가이드 문서 업데이트
- [x] ValueObject Book 프로젝트 통합
  - 프로젝트 참조 변경
  - ,NET 10 변경
  - Directory.Build.props 적용
  - Directory.Package.props 적용
  - MediatR 제거 -> Mediator 적용
  - MTP 기반 프로젝트 구성
  - README 문서 보강(가이드 기반)
  - 기존 Books 폴더 재구성
  - 가이드 문서 업데이트
  - README 문서 가이드 통합`
  - .NET 9.0 -> .NET 10
  - .Apply -> + fun<>` 스파일 Apply 검증
  - 2장/3장 테스트 프로젝트
- [x] 배열을 값 객체 동등성 비교 처리 개선
- [x] errorMessage = "" 개선
- [x] LanguageExt 패키지 버전 업데이트
- [x] 경고 제거
- [x] reportgenerator 출력 최소화 verbosity: 'Info'
- [x] CQRS 개선 IFinResponse, ...
- [x] ValueObject 예제 코드 개선
- [x] docs | 값 객체 개발 가이드
- [x] docs | 유스케이스 함수형
- [x] 유스케이스 함수형 예제
- [x] ~~LINQ 확장 Guard~~(자체 제공), Validation
---
## 가이드
- [ ] 계획
---
- [ ] 레이어
- [ ] 단위 테스트
- [ ] 통합 테스트
---
- [ ] 에러
- [ ] 의존성 등록
- [ ] 애플리케이션 레이어: CQRS, Validation, 관찰 가능성, 애플리케이션 에러
- [ ] 도메인 레이어: 값 객체, 엔티티, 도메인 에러
- [ ] 어댑터 레이어: 인터페이스 소스 생성기, 의존성 등록, 관찰 가능성, 어댑터 에러, IO
---
- [ ] Scheduling
- [ ] RabbitMQ
- [ ] HTTP Mehtod
- [ ] ORM(EFCore, Dapper)
- [ ] LanguageExt
  - Error
  - Fin
  - Validation
  - **IO**
    - 병렬 처리
    - 비동기
    - 예외 처리
    - 재시도
    - 자원 회수
  - FinT
  - Guard
  - Option
  - Seq
  - Traverse
---
- [x] Book 문서 값객체 동기화
- [x] Book 추가 문서 확인?
---
- [ ] "Request/Reply vs Fire and Forget 패턴"
  - 성공: 데이터 유/무
  - 실패
    - 경고: 정의할 수 있는 실패
    - 에러: 정의할 수 없는 실패(예외)
- [ ] guard 실패 처리 확인: 예외 발생?
- [ ] guard 튜토리얼 작성
---
- [ ] IO 튜토리얼
  - 취소: Run().RunAsync(취소)
  - 예외: Run().RunSafeAsync(취소): Flatten
- [ ] 파이프라인 생성자 타입 중복 테스트
---
---
- [x] 용어 통일 Metrics, Traces, Loggers
- [ ] 용어 통일 Success, Failure
---
- [ ] Functorium 접근 제어사 최적화
  - private
  - internal
  - protected
---
- [ ] docs | 유스케이스 with 함수형 book
---
- [ ] .sprint 양식 개선
- [ ] Docs/guides 문서 표준 양식
- [ ] Serilog 단위 테스트 이해
- [ ] Docs 폴더 정리
- [ ] README 도메인 중심의 함수형 아키텍처 다이어그램
- [ ] FinResponse 테스트 확장 메서드?
---
- [ ] Entity ID 소스 생성기
- [ ] EFCore 통합
- [ ] SQLite 예제(appsettings.json 선택)
- [ ] PostgreSQL 예제(appsettings.json 선택)
- [ ] Dapper CQRS 적용: C EFCore, Q Dapper
- [ ] ER 다이어그램 자동화
- [ ] 컨테이너 기반 테스트 자동화
- [ ] DTO
---
- [ ] RabbitMQ
---
- [ ] MinVer
- [ ] ChatOps
- [ ] BenchmarkDotNet GitHub Actions 통합(회귀 품질)
- [ ] 코드 품질 GitHub Actions 통합(회귀 품질)
---
- [ ] 예제 포팅
- [ ] 코드 품질 CLI
- [ ] WebSite: Astro, Starlight
- [ ] VSCode 개발 환경 구축
- [ ] VSCode 테스트
- [ ] VSCode 코드 커버리지
- [ ] VSCode 단축키
- [ ] 시각화 TngTech.ArchUnitNET

---
- Abstraction
  - [x] Error
    - Type: ErrorCodeExpected, ErrorCodeExceptional, ErrorCodeFactory
    - Structured Logging
- Adapter Layer
  - [x] Option
    - Validation
    - Startup Logging
  - [ ] Observability
- Adapters.SourceGenerator
  - [x] IObservablePort Observability
- Application Layer
  - [ ] CQRS
  - [ ] Pipeline
  - [ ] Observability Pipeline
  - [ ] Observability Port
  - [ ] LINQ
- Domain Layer
  - Value Object
  - [ ] Entity
  - [ ] Aggregate Root
  - [ ] Domain Event
- Testing
  - [x] Architecture
  - [x] Source Generator
  - [ ] Quartz
  - [ ] HostTestFixture
  - [ ] Structured Logging
- Book
  - [x] Release Note: Claude Code + .NET 10 File-based App
  - [x] Observability Code: Source Generator
  - [ ] 단위 테스트
    - xunt v3
    - mtp
      - https://learn.microsoft.com/en-us/dotnet/core/testing/migrating-vstest-microsoft-testing-platform
      - overview: https://xunit.net/docs/getting-started/v3/microsoft-testing-platform
      - coverage: https://xunit.net/docs/getting-started/v3/code-coverage-with-mtp
      ```json
      {
        "sdk": {
          "rollForward": "latestFeature",
          "version": "10.0.100"
        },
        "test": {
          "runner": "Microsoft.Testing.Platform"
        }
      }
      ```
      ```xml
      <!-- Microsoft Testing Platform -->
      <PropertyGroup Condition="'$(IsTestProject)' == 'true'">
        <OutputType>Exe</OutputType>
        <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
      </PropertyGroup>
      ```
      ```
      coverlet.collector  -> Microsoft.Testing.Extensions.CodeCoverage
      .                   -> Microsoft.Testing.Extensions.TrxReport
      ```
      ```shell
      dotnet test --project Tests/Functorium.Tests.Unit -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --report-trx
      ```
    - architecture
    - snapshot
    - logging?
    - container?
    - performance???
    - Build-Local.ps1
    - GitHub Action
  - [ ] GitHub CI/CD
    - ChatOps
  - [ ] 성공주도 유스케이스 개발 with 함수형(LINQ + LanaguageExt)
  - [ ] LanaguageExt
    - Fin
    - FinT
    - IO
    - Validation
    - Guard
    - Traverse
    - Error
    - Option
- ?
  - [x] Layer
  - [x] Success-Driven Usecase Implementation by Functional
  - [ ] FastEndpoint
  - [ ] RabbitMQ
  - [ ] DTO
  - [ ] EF Core
  - [ ] Dapper
  - [ ] Aspire
  - [ ] gRPC
  - [ ] Featrue Flag

## Feature
- [x] Error
  - ErrorCodeFactory
    - ErrorCodeExpected
    - ErrorCodeExceptional
  - Serilog
    - Serilog.Core.IDestructuringPolicy
    - IErrorDestructurer
- [x] Option
  - OptionsConfigurator
    - GetOptions
    - RegisterConfigureOptions
- [x] Observability 의존성 등록
  - OpenTelemetryOptions
  - OpenTelemetryBuilder
    - LoggerOpenTelemetryBuilder
    - TraceOpenTelemetryBuilder
    - MetricOpenTelemetryBuilder
  - Logging
    - IStartupOptionsLogger
    - StartupLogger : IHostedService
- [ ] Mediator 패턴 Pipeline
- [ ] ValueObject
- [x] Example: Observability 로그 출력
- [ ] VSCode 개발 환경
  - 확장 도구
    - .NET Install Tool
	    - C#
	    - C# Dev Kit
    - Coverage Gutters
    - Test Explorer UI
      - .Net Core Test Explorer
    - Remote Development
	    - Remote - SSH
	    - Remote - Tunnels
	    - Dev Containers
	    - WSL
    - GitHub Actions
    - Markdown??
    - REST Client Api
    - Peek Hidden Files
    - Paste Image
    - Trailing Spaces
    - Code Spell Checker
      ```
    	"cSpell.ignoreWords": [
        "Functorium",
        "Observabilities"
      ]
      ```
  - Hide Folders and Files: https://marketplace.visualstudio.com/items?itemName=tylim88.folder-hide
  - .vscode
    - launch.json: VSCode 디버깅 환경 설정
    - settings.json
    - tasks.json

## TODO
- [x] claude: claude commit 명령어
- [x] doc: 아키텍처 문서
- [x] doc: git command guide 문서: `Git-Commands.md`
- [x] doc: git commit guide 문서: `Git-Commit.md`
- [x] doc: guide 문서 작성을 위한 guide 문서: `Guide-Writing.md`
- [x] dev: 솔루션 구성
- [x] dev: 코드 품질((코드 스타일, 코드 분석 규칙)) 빌드 통합: .editorconfig을 이용한 "코드 품질" 컴파일 과정 통합
- [x] doc: 코드 품질((코드 스타일, 코드 분석 규칙)) 빌드 통합 문서: `Code-Quality.md`
- [x] doc: DOTNET SDK 빌드 명시 문서: `Build-SdkVersion-GlobalJson.md
- [x] dev: 솔루션 구성: global.json SDK 버전 허용 범위 지정
- [X] dev: 솔루션 구성: nuget.config 파일 생성
- [x] dev: 커밋 이력 ps1
- [x] dev: Build-CommitSummary 대상 브랜치 지정
- [x] dev: Build-CommitSummary 커밋 작성자 추가
- [x] dev: Build-CommitSummary 커밋 소스 브랜치 추가
- [x] dev: Build-CommitSummary 태그 없을 때 버그 수정
- [x] dev: Build-CommitSummary 출력 경로 매개변수화
- [x] dev: Build-CommitSummary 타겟 브랜치 이름 출력
- [ ] dev: Build-CommitSummary --no-merges
- [x] claude: commit 주제 전달일 때는 주체만 commit하기
- [x] dev: ci.yml -> build.yml
- [x] dev: build.yml 실패 처리
- [x] std: MinVer 이해(형상관리 tag 연동)
- [x] dev: 로컬 빌드
- [x] dev: GitHub actions build
- [x] dev: GitHub actions publish
- [x] doc: GitHub actions 문서
- [x] dev: Functorium.Testing 프로젝트 소스 추가
- [x] dev: Functorium.Testing xunit.v3 기반으로 패키지 참조 및 소스 개선
- [x] dev: 패키지 .NET 10 기준으로 최신 버전 업그레이드
- [x] doc: 단위 테스트 가이드 문서
- [x] dev: Build-Local.ps1 dotnet cli 외부 명령 출력이 버퍼링되어 함수 반환 시까지 표시되지 않는 븍구 수정(| Out-Host)
- [x] dev: ErrorCode 개발 이해
- [x] dev: ErrorCode 테스트 자동화 이해
- [x] dev: Build-VerifyAccept.ps1 파일
- [x] dev: ps1 공통 모듈 분리
- [x] doc: ps1 파일 작성 가이드
- [ ] dev: ps1 출력을 실시간 처리하기 위해 명령을 함수 밖으로 이동 배치
- [x] dev: 관찰 가능성 의존성 등록 코드: Logger, Trrace, Metric
- [ ] dev: 관찰 가능성 의존성 등록 리뷰
- [ ] doc: 옵션, 로그 출력 중심으로 문서화
- [ ] std: Functorium.Testing 애해: 아키텍처 단위 테스트
- [ ] std: Functorium.Testing 애해:  구조적 로그 단위 테스팅
- [ ] std: Functorium.Testing 애해:  WebApi 통합 테스트
- [ ] std: Functorium.Testing 애해:  ScheduledJob 통합 테스트
- [ ] DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true, DOTNET_CLI_TELEMETRY_OPTOUT: true 이해
- [ ] powershell 학습 문서
- [ ] powershell 가이드 문서
- [ ] powershell 가이드 문서 기준 개선
- [ ] 로컬 빌드 문서(dotnet 명령어)
- [ ] 솔루션 구성: .editorconfig 폴더 단위 개별 지정
- [ ] 솔루션 구성: Directory.Packages.props 하위 폴더 새로 시작, 버전 재정의
- OpenTelemetryOptions
  - [x] OpenTelemetryOptions 문서
  - [x] OpenTelemetryOptions 의존성 등록
  - [x] 용어 정리
    - logging 접두사
    - logger 접미사
  - [ ] Observability 의존성 등록과 Builder 관련 테스트
  - [ ] Serilog Destructure 깊이, 배열, ... 제약 조건 테스트
- NuGet 패키지
  - [x] NuGet 배포를 위한 프로젝트 설정
  - [x] 로컬 NuGet 패키지 배포 스크립트
  - [x] publish.yml 개선
  - [x] NuGet 문서
  - [x] NuGet 계정
  - [x] png 아이콘
  - [ ] Release 노트 생성기?
  - [ ] Release 배포
  - [ ] NuGet 배포
- Example 관찰 가능성
  - [x] 예제 프로젝트 구성
  - [x] 소스 정리
  - [x] 로그
  - [x] FtpOptions Startup 로그
  - [x] FtpOptions 통합 테스트
  - [x] OpenTelemetryOptions 통합 테스트
  - [x] 통합 테스트 문서
  - [x] .vscode 구성 문서
- Dashboard
  - [x] Aspire 대시보드 구성
  - [x] OpenSearch 대시보드 구성
- Release Notes 자동화
  - [x] Aspire Release Notes 자동화 이해
  - [x] analyze-all-components.sh/ps1 포팅
  - [x] analyze-folder.sh/ps1 포팅
  - [x] extract-api-changes.sh/ps1 포팅
  - [x] GenApi -> PublicApiGenerator 패키지 교체
  - [x] Docs 폴더 .md 문서 한글화
  - [x] Docs 폴더 포팅 출력 기준 업데이트
  - [ ] PublicApiGenerator 단일 파일 코드 .NET 10
  - [ ] analyze-all-components.sh/ps1 이전 결과 삭제
  - [x] 릴리스 노트 자동 생성을 위한 AI 프럼프트
    ```
    1단계: 데이터 수집 (사람이 먼저 실행)

      # 컴포넌트 분석
      ./analyze-all-components.sh origin/release/9.4 origin/main

      $FIRST_COMMIT = git rev-list --max-parents=0 HEAD
      .\analyze-all-components.ps1 -BaseBranch $FIRST_COMMIT -TargetBranch origin/main

      # API 변경사항 추출
      ./extract-api-changes.sh

    2단계: AI에게 요청

      @Tools\ReleaseNotes\Docs\  폴더의 모든 문서를 참고하여
      analysis-output/ 폴더에 있는 분석 결과를 기반으로
      Functorium {version} 릴리스 노트를 작성해줘.

      핵심 원칙:
      1. 모든 API는 all-api-changes.txt (Uber 파일)에서 검증할 것
      2. API를 임의로 만들어내지 말 것
      3. 모든 기능은 커밋/PR로 추적 가능해야 함
      4. writing-guidelines.md의 템플릿 구조를 따를 것
      5. validation-checklist.md로 최종 검증할 것

      문서 구조의 의도

      | 문서                      | AI에게 주는 역할           |
      |-------------------------|----------------------|
      | data-collection.md      | 입력 데이터 위치와 구조 설명     |
      | commit-analysis.md      | 커밋 → 기능 변환 방법        |
      | api-documentation.md    | API 검증 및 코드 샘플 작성 규칙 |
      | writing-guidelines.md   | 출력 문서 템플릿과 스타일       |
      | validation-checklist.md | 품질 검증 체크리스트          |
    ```
  - [ ] 세부 버전까지 표시
    - 파일명: RELEASE-v1.0.0-alpha.0.132.md
    - 문서: v1.0.0-alpha.0 <--- x
  - [ ] markdownlint-cli@0.45.0 사전 설치
  - [x] ExtractApiChanges 이모지 제거
  - [x] ExtractApiChanges CommandLine 패키지 버전 업그레이드
  - [x] ExtractApiChanges.cs 정렬
  - [x] ExtractApiChanges.cs .api 비교
  - [x] ExtractApiChanges 콘솔 출력 색상
  - [x] analyze-all-components.ps1, analyze-folder 포팅
  - [x] ~~Docs -> Reference 폴더 이름 변경~~
  - [x] .NET 10 File-based 실행 오류 개선
  - [x] ReleaseNotes 문서 내용 업데이트
  - [x] Tools/ReleaseNotes -> .release-notes/script로 이동
  - [x] aspire 릴리스 노트 한글화
  - [x] AnalyzeAllComponents.cs base branch 유효성 검사 추가
  - [x] config -> Config
  - [x] 첫 배포일 때 cli 명령어를 한줄로(스크립트 변수 제거)
  - [x] analysis_output -> .analysis_output
  - [x] C# 10 File-based 트러블슈팅 추가
  - [x] 스크립트에서 사용한 Git 명령어 문서 추가 반영(Git.md)
  - [x] PublicApiGenerator 패키지 버전 최신화
  - [x] commit 타입 통일 시킴
    - commit.md
    - data-collection.md
    - AnalyzeAllComponents.cs
  - [x] dotnet tool 설치 방법 변경
    - 변경 전: ps1 파일을 이용해서 명시적 도구 설치
    - 변경 후: .config/dotnet-tools.json
  - [x] .config/dotnet-tools.json 에서 사용하지 않는 도구 제거 publicapigenerator.tool
  - [x] .gitignore 자동 생성 폴더 추가 .release-notes/scripts/.analysis-output/
  - [x] 릴리즈 노트 생성
  - [x] Breaking Changes 감지에 git diff 추가(권장)
  - [x] release note phase 기반으로 재구성
  - [ ] 버전?
  - [ ] 브랜치 전략?
  - [ ] GitHub Release 배포
  - [ ] NuGet 배포
  - [ ] git 명령어 시나리오
- [x] .config 폴더를 이용해서 ReportGenerator 설치(ps1 파일 개선)
- [x] commit.md와 .release-notes/scripts/docs 문서와 커밋 태그 통일
- [x] commit.md와 .release-notes/scripts/docs 문서와 커밋 내용 통일(영문, 한글)
- Functorium.SourceGenerators
  - [x] 파일 기반 네임스페이스
  - [x] 입출력 튜플 타입 제외
  - [x] 디버깅 방법 학습
  - [x] 테스트 케이스 재구성
  - [x] 타입 출력 전체 이름 일관성 버그 해결



Item                                      | Type    | File                          | todo
---                                       | ---     | ---                           | ---
Build-CommitSummary.ps1                   | Manual  | Build-CommitSummary.md        | done
Build-Local.ps1                           | Manual  | Build-Local.md                | done
.claude/commands/commit.md                | Manual  | Command-Commit.md             | done
.claude/commands/suggest-next-version.md  | Manual  | Command-SuggestNextVersion.md | x
.editorconfig                             |         | Code-Quality.md               |
CLAUDE.md                                 |         |                               |
Directory.Build.props                     |         |                               |
Directory.Packages.props                  |         |                               |
global.json                               |         | SdkVersion.md                 |
nuget.config                              |         |                               |
.github/workflows/build.yml               |         | GitHub Actions.md             |
.github/workflows/publish.md              |         | GitHub Actions.md             |
                                          |         | Git.md                        |
                                          |         | MinVer.md                     |
                                          |         | UnitTesting.md                |
                                          |         | xUnitV3.md                    |

- [x] Language-Ext 업그레이드
  - `FinT<M, A>.Lift(fin)` → `FinT.lift<M, A>(fin)`
  - `Fin<A>.Succ` (메서드 참조) → `Fin.Succ` (람다 사용)
  - `Fin<A>.Fail(error)` → `Fin.Fail<A>(error)`
  - `FinT<M, A>.Fail(error)` → `FinT.Fail<M, A>(error)`

```shell
rm -rf ~/AppData/Local/Temp/dotnet/runfile/ExtractApiChanges*
%LOCALAPPDATA%\Temp\dotnet\runfile\<스크립트명>*
dotnet ExtractApiChanges.cs

# 실행 전 캐시 정리 스크립트
  Remove-Item -Recurse -Force "$env:LOCALAPPDATA\Temp\dotnet\runfile\*" -ErrorAction SilentlyContinue


● 파일 잠금 해결 방법

  1. 프로세스 확인 및 종료

  # 잠금 프로세스 확인
  tasklist | findstr "ExtractApiChanges"
  tasklist | findstr "ApiGenerator"
  tasklist | findstr "AnalyzeAllComponents"

  # 프로세스 강제 종료
  taskkill /F /IM ExtractApiChanges.exe
  taskkill /F /IM ApiGenerator.exe

  2. 캐시 폴더 삭제

  # 특정 스크립트 캐시만 삭제
  Remove-Item -Recurse -Force "$env:LOCALAPPDATA\Temp\dotnet\runfile\ExtractApiChanges*"

  # 모든 file-based program 캐시 삭제
  Remove-Item -Recurse -Force "$env:LOCALAPPDATA\Temp\dotnet\runfile\*"

  3. 그래도 안 되면

  # 파일 잠금 프로세스 찾기 (Handle 도구 필요)
  handle.exe "ExtractApiChanges"

  # 또는 Resource Monitor 사용
  resmon.exe
  # CPU → Associated Handles → 검색

  4. 최후의 수단

  # 재부팅 없이 Explorer 재시작
  taskkill /F /IM explorer.exe
  start explorer.exe

  # 또는 PC 재시작

  권장: 실행 스크립트에 정리 로직 추가

  # run-extract-api.ps1
  $scriptName = "ExtractApiChanges"
  $cachePattern = "$env:LOCALAPPDATA\Temp\dotnet\runfile\$scriptName*"

  # 기존 프로세스 종료
  Get-Process -Name $scriptName -ErrorAction SilentlyContinue | Stop-Process -Force

  # 캐시 정리
  Remove-Item -Recurse -Force $cachePattern -ErrorAction SilentlyContinue

  # 실행
  dotnet ExtractApiChanges.cs

```


```
configurator.AddTrigger(t => t
  .ForJob(jobKey)
  .WithIdentity($"{jobConfig.TriggerName}-Immediate", jobConfig.TriggerGroup)
  .StartNow()
  .WithSimpleSchedule(x => x.WithRepeatCount(0))
  .WithDescription($"{jobConfig.TriggerDescription} (즉시 실행)")
);
```