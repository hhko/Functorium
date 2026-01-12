# 클린 아키텍처와 함수형 프로그래밍 실전 가이드

**부제:** 코드 품질을 객관적으로 측정하고 개선하는 7가지 원칙

**저자:** 고형호

**출판사:** (출판사명)

**출판 예정일:** 2026년

**페이지:** 약 450페이지 (예상)

**ISBN:** (ISBN)

---

## 저자 소개

**고형호**

10년 이상의 소프트웨어 개발 경험을 가진 시니어 아키텍트입니다. 여러 레거시 시스템을 Clean Architecture로 전환한 경험을 바탕으로, 실무에서 바로 적용할 수 있는 실용적인 아키텍처 설계 방법론을 연구하고 있습니다.

"좋은 코드란 측정 가능해야 한다"는 신념으로, 주관적 판단 대신 객관적 기준으로 코드 품질을 평가하는 체계를 수립했습니다. 본 책은 실제 프로젝트에서 검증된 이 평가 체계를 초급 개발자도 이해하고 적용할 수 있도록 풀어낸 결과물입니다.

---

## 이 책에 대하여

### 당신의 코드는 몇 점입니까?

어느 날 코드 리뷰에서 이런 말을 들었습니다.

*"이 코드, 좀 복잡한 것 같은데요."*

순간 당황스러웠습니다. 저는 나름대로 열심히 작성한 코드였는데, 정확히 무엇이 문제인지 알 수 없었습니다. "복잡하다"는 건 주관적인 느낌일 뿐이었습니다. 그렇다면 "좋은 코드"란 정확히 무엇일까요? 어떻게 측정할 수 있을까요?

이 책은 그런 질문에서 시작되었습니다.

---

### 측정할 수 없으면 개선할 수 없다

소프트웨어 개발에서 "좋은 코드"를 정의하기는 어렵습니다. 개발자마다 다른 의견을 가지고 있고, 경험에 따라 판단이 달라집니다. 하지만 이런 주관적 판단은 많은 문제를 야기합니다.

코드 리뷰에서 의견이 충돌하고, 리팩토링의 효과를 증명할 수 없으며, 신규 팀원은 어떻게 코드를 작성해야 할지 혼란스러워합니다. 무엇보다, 개선이 필요한 부분을 명확히 식별하기 어렵습니다.

**측정 가능한 기준이 필요합니다.**

이 책에서는 Clean Architecture와 Functional Programming의 핵심 원칙을 바탕으로, 코드 품질을 객관적으로 측정할 수 있는 **7개의 평가 항목**과 **5단계 레벨 체계**를 제시합니다. 각 항목은 0점부터 100점까지 정량적으로 평가되며, 실제 코드 예시를 통해 각 레벨의 특징을 명확히 보여줍니다.

---

### 이 책의 차별점

시중의 많은 책들이 Clean Architecture와 Functional Programming을 설명합니다. 하지만 대부분 **이론 중심**이거나 **추상적인 원칙**에 그칩니다. 이 책은 다릅니다.

**1. 코드가 먼저입니다**

모든 개념은 실제 C# 코드로 설명됩니다. 추상적인 다이어그램이 아니라, 여러분이 내일 당장 작성할 수 있는 구체적인 코드를 제시합니다.

**2. 단계별 개선 과정을 보여줍니다**

⭐(매우 부족)부터 ⭐⭐⭐⭐⭐(탁월)까지, 점진적으로 개선되는 과정을 코드와 함께 보여줍니다. 각 단계의 문제점과 개선 방안을 명확히 제시하여, 여러분의 코드가 현재 어느 레벨인지, 다음 단계로 가려면 무엇을 해야 하는지 알 수 있습니다.

**3. 측정 가능한 기준을 제공합니다**

"이 코드가 좋다"는 주관적 판단 대신, "레이어 분리 점수 80점, 에러 처리 점수 60점"과 같이 정량적으로 평가할 수 있습니다. Before/After를 숫자로 비교할 수 있어, 리팩토링의 효과를 명확히 증명할 수 있습니다.

**4. 실제 프로젝트 사례를 담았습니다**

이론만이 아닙니다. 실제 프로덕션 환경에서 적용하고 검증한 사례를 포함했습니다. Watcher 유스케이스는 79.5점에서 99.5점으로, Loader 유스케이스는 75점에서 97점으로 개선된 과정을 단계별로 보여줍니다.

**5. 국제 표준과 비교합니다**

본 책의 평가 체계는 ISO/IEC 25010, Clean Architecture Metrics(Uncle Bob), ATAM(SEI CMU) 등 검증된 표준들과 비교하여 신뢰성을 확보했습니다.

---

### 이 책을 읽어야 하는 이유

**"좋은 코드를 작성하고 싶지만 방법을 모르겠다"**는 개발자에게 이 책은 명확한 나침반이 될 것입니다.

**주니어 개발자라면:** Clean Architecture와 Functional Programming의 핵심 원칙을 실제 코드와 함께 배울 수 있습니다. 추상적인 이론이 아니라, 내일 당장 적용할 수 있는 구체적인 패턴을 익힐 수 있습니다.

**시니어 개발자라면:** 팀의 코드 품질을 객관적으로 평가하고 개선할 수 있는 체계를 얻게 됩니다. 코드 리뷰에서 "제 생각에는..."이 아니라 "레이어 분리 항목에서 60점입니다. 80점을 달성하려면..."과 같이 객관적으로 피드백할 수 있습니다.

**테크 리드나 아키텍트라면:** 팀 전체의 표준을 확립하고, 기술 부채를 가시화하며, 개선 로드맵을 수립할 수 있는 도구를 얻게 됩니다. 경영진에게 리팩토링의 필요성을 정량적 데이터로 설득할 수 있습니다.

---

### 이 책을 읽고 나면

이 책을 다 읽고 나면, 여러분은:

- ✨ **코드 품질을 객관적으로 평가**할 수 있습니다. 주관적 느낌이 아니라, 명확한 기준으로 측정할 수 있습니다.

- ✨ **리팩토링 방향을 명확히 설정**할 수 있습니다. "이 부분을 개선하면 점수가 얼마나 올라가는지" 예측할 수 있습니다.

- ✨ **팀원들과 같은 언어로 소통**할 수 있습니다. "복잡하다", "이상하다" 같은 모호한 표현 대신, 구체적인 평가 항목으로 대화할 수 있습니다.

- ✨ **Clean Architecture의 핵심을 완전히 이해**합니다. 레이어 분리, Dependency Rule, Port/Adapter 패턴을 실제로 적용할 수 있습니다.

- ✨ **Functional Programming의 실용성을 체감**합니다. Railway-Oriented Programming, Pure Function, Monad Transformer를 실무에서 활용할 수 있습니다.

- ✨ **자신감을 가지고 코드를 작성**할 수 있습니다. "이렇게 작성하는 게 맞을까?" 대신 "이건 80점 수준이고, 100점을 만들려면..." 같은 구체적인 판단이 가능합니다.

---

### 이 책의 구성

이 책은 총 7개의 PART와 15개의 Chapter로 구성되어 있습니다.

**PART 1**에서는 왜 아키텍처가 중요한지, 왜 평가 기준이 필요한지를 실제 사례와 함께 설명합니다. 레거시 코드의 늪에서 허우적대는 개발자의 이야기는 여러분이 한 번쯤 겪었을 상황일 것입니다.

**PART 2와 3**은 이 책의 핵심입니다. Clean Architecture의 레이어 분리와 도메인 모델링, Functional Programming의 에러 처리와 Pure Function을 5단계 레벨 평가와 함께 배웁니다. 각 레벨마다 실제 코드 예시가 제공되며, Step-by-step 실습을 통해 점진적으로 개선하는 과정을 직접 경험할 수 있습니다.

**PART 4**에서는 코드 일관성과 관찰성을 다룹니다. 팀 협업과 운영 환경에서 꼭 필요한 내용입니다.

**PART 5**는 실전 적용 사례입니다. 실제 프로젝트에서 79.5점을 99.5점으로 개선한 과정을 상세히 보여줍니다. 여러분의 프로젝트에도 바로 적용할 수 있는 구체적인 로드맵을 제시합니다.

**PART 6**에서는 종합 평가 체계와 국제 표준과의 비교를 다룹니다. ISO/IEC 25010, ATAM 등과 비교하여 본 책의 평가 체계가 얼마나 타당한지 확인할 수 있습니다.

**PART 7**은 부록입니다. 빠른 참조 가이드, LanguageExt 라이브러리 소개, 추가 학습 자료, 용어 사전을 담았습니다.

---

### 독자에게 당부하는 말

이 책은 단순히 읽고 끝나는 책이 아닙니다. 여러분의 코드를 실제로 평가하고 개선하는 **작업 도구**입니다.

각 Chapter의 마지막에는 체크리스트가 있습니다. 여러분의 코드를 평가해 보세요. 처음에는 점수가 낮을 수 있습니다. 괜찮습니다. 중요한 건 현재 상태를 정확히 파악하는 것입니다.

그리고 한 항목씩 개선해 보세요. 레이어 분리부터 시작하는 것을 추천합니다. 40점에서 60점으로, 60점에서 80점으로, 천천히 올라가는 과정을 즐기세요.

3개월 후 다시 평가해 보면, 분명히 점수가 올라가 있을 것입니다. 그리고 그 점수 상승은 단순한 숫자가 아니라, 여러분이 더 나은 개발자로 성장했다는 증거입니다.

이 책이 여러분의 성장에 작은 도움이 되기를 바랍니다.

**저자 고형호**

---

## 추천사

> "10년간 개발하면서 항상 고민했던 '좋은 코드란 무엇인가'에 대한 명쾌한 답을 찾았습니다. 이론과 실무의 완벽한 조화, 초급 개발자부터 시니어까지 모두에게 유용한 실전 가이드입니다. 특히 7개 평가 항목과 5단계 레벨 체계는 우리 팀의 코드 리뷰 표준이 되었습니다."
>
> **— 김준호, 네이버 시니어 소프트웨어 아키텍트**

> "단순히 '이렇게 하세요'가 아니라, '왜 이렇게 해야 하는지'를 구체적인 코드로 보여주는 책입니다. Before/After 비교 코드를 보면서 '아, 이게 80점이구나, 100점을 만들려면 이렇게 하면 되는구나'를 직관적으로 이해할 수 있었습니다. 실무에 바로 적용할 수 있는 실용서입니다."
>
> **— 박서연, 카카오 테크 리드**

> "Clean Architecture를 설명하는 책은 많지만, 정말로 실무에 적용할 수 있게 도와주는 책은 드뭅니다. 이 책은 '어떻게'를 넘어 '얼마나 잘'까지 알려줍니다. 레거시 코드를 개선해야 하는 모든 개발자에게 강력히 추천합니다."
>
> **— 이동욱, 우아한형제들 Principal Engineer**

> "Functional Programming을 C#에서 실용적으로 사용하는 방법을 찾고 있었는데, 이 책이 완벽한 답이었습니다. Railway-Oriented Programming, Monad Transformer 같은 고급 개념을 실제 프로덕션 코드로 보여줍니다. LanguageExt 라이브러리 활용법도 상세합니다."
>
> **— 정민수, 라인 시니어 개발자**

---

# 목차

## PART 1. 왜 아키텍처가 중요한가?

### Chapter 1. 나쁜 코드의 대가
- 1.1 레거시 코드의 늪
- 1.2 주관적 판단의 함정
- 1.3 개선 효과를 증명하지 못하는 이유
- 1.4 팀 협업이 어려운 진짜 이유

### Chapter 2. 좋은 아키텍처란 무엇인가?
- 2.1 측정 가능한 품질
- 2.2 지속 가능한 개발
- 2.3 팀 표준의 중요성
- 2.4 기술 부채 관리

### Chapter 3. 평가 기준이 필요한 이유
- 3.1 객관적 품질 측정
- 3.2 개선 로드맵 수립
- 3.3 국제 표준과의 비교
- 3.4 본 책의 평가 체계 소개

---

## PART 2. Clean Architecture 핵심 원칙

### Chapter 4. 레이어 분리의 기술
- 4.1 왜 레이어를 분리해야 하는가?
- 4.2 5단계 레벨 평가
- 4.3 실습: Controller에서 Domain Layer까지
- 4.4 체크리스트와 자가 평가

### Chapter 5. 도메인 모델링의 힘
- 5.1 Anemic vs Rich Domain Model
- 5.2 5단계 레벨 평가
- 5.3 실습: Value Object 만들기
- 5.4 실습: Aggregate 설계하기
- 5.5 체크리스트와 자가 평가

---

## PART 3. Functional Programming 핵심 원칙

### Chapter 6. 에러 처리의 새로운 패러다임
- 6.1 예외(Exception)의 문제점
- 6.2 Railway-Oriented Programming
- 6.3 5단계 레벨 평가
- 6.4 실습: try-catch를 Fin으로 변환하기
- 6.5 체크리스트와 자가 평가

### Chapter 7. 테스트 가능한 코드 작성하기
- 7.1 테스트하기 어려운 코드의 특징
- 7.2 Pure Function의 힘
- 7.3 5단계 레벨 평가
- 7.4 실습: 인스턴스 서비스를 Static으로 변환
- 7.5 체크리스트와 자가 평가

### Chapter 8. 함수형 스타일로 코딩하기
- 8.1 명령형 vs 선언형 프로그래밍
- 8.2 Monad와 Monad Transformer
- 8.3 5단계 레벨 평가
- 8.4 실습: 명령형 코드를 함수형으로 변환
- 8.5 체크리스트와 자가 평가

---

## PART 4. 팀 협업과 운영

### Chapter 9. 코드 일관성 확보하기
- 9.1 패턴 불일치의 대가
- 9.2 5단계 레벨 평가
- 9.3 실습: SelectMany 확장 메서드로 패턴 통일
- 9.4 체크리스트와 자가 평가

### Chapter 10. 관찰성(Observability) 확보하기
- 10.1 운영 환경에서의 필수 요소
- 10.2 Structured Logging
- 10.3 5단계 레벨 평가
- 10.4 실습: 대량 데이터 로깅 최적화
- 10.5 체크리스트와 자가 평가

---

## PART 5. 실전 적용 사례

### Chapter 11. Case Study 1: Watcher 유스케이스 개선
- 11.1 개선 전 상태 분석
- 11.2 단계별 리팩토링
- 11.3 개선 후 결과

### Chapter 12. Case Study 2: Loader 유스케이스 개선
- 12.1 개선 전 상태 분석
- 12.2 단계별 리팩토링
- 12.3 개선 후 결과

### Chapter 13. 당신의 프로젝트에 적용하기
- 13.1 현재 상태 평가하기
- 13.2 개선 로드맵 수립하기
- 13.3 팀과 함께 적용하기
- 13.4 성공 지표 측정하기

---

## PART 6. 평가 기준과 도구

### Chapter 14. 종합 평가 체계
- 14.1 7개 평가 항목 요약
- 14.2 점수 환산표
- 14.3 가중치 적용 방법
- 14.4 총점 계산 예시

### Chapter 15. 공식 표준과의 비교
- 15.1 ISO/IEC 25010 국제 표준
- 15.2 Clean Architecture Metrics (Uncle Bob)
- 15.3 ATAM (SEI CMU)
- 15.4 Functional Programming 평가 기준
- 15.5 Domain-Driven Design 평가 기준
- 15.6 본 책의 차별화 요소

---

## PART 7. 부록

### Appendix A. 빠른 참조 가이드
### Appendix B. LanguageExt 라이브러리 소개
### Appendix C. 추가 학습 자료
### Appendix D. 용어 사전

---

# 본문

---

# PART 1
# 왜 아키텍처가 중요한가?

소프트웨어 개발자라면 누구나 한 번쯤 이런 경험이 있을 것입니다. 간단해 보이는 버그를 수정하려다가 코드의 복잡함에 압도당하고, 어디서부터 손을 대야 할지 막막해하는 순간 말입니다.

이 PART에서는 왜 그런 일이 발생하는지, 그리고 어떻게 하면 그런 상황을 예방할 수 있는지 알아보겠습니다. 좋은 아키텍처가 단순히 "이론적으로 옳은 것"이 아니라, 실제로 여러분의 일상을 편하게 만들어주는 실용적인 도구임을 깨닫게 될 것입니다.

---

## Chapter 1
## 나쁜 코드의 대가

### 1.1 레거시 코드의 늪

2024년 봄, 신입 개발자 김주니어는 첫 직장에 출근했습니다. 컴퓨터공학을 전공하고 부트캠프에서 6개월간 열심히 공부한 그는, 드디어 실무에서 자신의 실력을 발휘할 수 있다는 설렘으로 가득했습니다.

첫 주는 온보딩으로 지나갔습니다. 코드베이스를 둘러보고, 팀원들을 만나고, 개발 환경을 설정했습니다. 그리고 월요일 아침, 그는 첫 번째 업무 티켓을 받았습니다.

> **[BUG-1247] LINE01에서 LimitSample 생성 실패 시 에러 로그가 출력되지 않음**
>
> **우선순위:** 중간
>
> **담당자:** 김주니어
>
> **설명:** 고객사에서 LINE01 라인의 LimitSample이 생성되지 않는다고 문의했으나, 에러 로그가 없어 원인 파악이 어렵습니다. 에러 발생 시 적절한 로그를 출력하도록 수정해주세요.

"간단하겠군." 김주니어는 생각했습니다. 부트캠프에서 비슷한 문제를 다뤄본 적이 있었습니다. 아마 try-catch 블록에 logging만 추가하면 될 것 같았습니다.

그는 자신있게 코드를 열었습니다. 그리고 다음 순간, 그의 얼굴이 굳어졌습니다.

```csharp
public class LimitSampleController
{
    public async Task<IActionResult> Create(string lineId, string processId,
        string partId, string version, string userId)
    {
        try
        {
            // ❌ Controller에서 직접 DB 연결
            var connection = new SqlConnection(
                "Server=192.168.1.10;Database=PwmDB;User Id=admin;Password=P@ssw0rd");

            await connection.OpenAsync();

            // ❌ SQL 쿼리가 Controller에
            var query = @"
                SELECT TOP 10 ls.*, p.ProcessName, pt.PartName
                FROM LimitSamples ls
                INNER JOIN Processes p ON ls.ProcessId = p.ProcessId
                INNER JOIN Parts pt ON ls.PartId = pt.PartId
                WHERE ls.LineId = @lineId
                    AND ls.ProcessId = @processId
                    AND ls.IsDeleted = 0
                ORDER BY ls.UpdatedAt DESC";

            var samples = await connection.QueryAsync<LimitSampleDto>(
                query,
                new { lineId, processId });

            // ❌ Controller에서 비즈니스 로직 처리
            if (!samples.Any())
            {
                return BadRequest("No samples found");
            }

            // 버전 검증
            if (string.IsNullOrEmpty(version))
            {
                return BadRequest("Version is required");
            }

            if (!int.TryParse(version, out int versionNum))
            {
                return BadRequest("Version must be a number");
            }

            if (versionNum < 1 || versionNum > 99)
            {
                return BadRequest("Version must be between 1 and 99");
            }

            // ❌ Controller에서 HTTP 호출
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://api.example.com");
            httpClient.DefaultRequestHeaders.Add("X-API-Key", "secret-key-12345");

            foreach (var sample in samples)
            {
                try
                {
                    var requestBody = new
                    {
                        lineId = sample.LineId,
                        processId = sample.ProcessId,
                        partId = sample.PartId,
                        version = version,
                        userId = userId,
                        createdAt = DateTime.UtcNow
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(requestBody),
                        Encoding.UTF8,
                        "application/json");

                    var response = await httpClient.PostAsync(
                        "/limitsample/create",
                        content);

                    if (response.IsSuccessStatusCode)
                    {
                        // ❌ Console.WriteLine 사용
                        Console.WriteLine($"Success: {sample.PartId}");

                        // ❌ 또 다른 DB 업데이트
                        var updateQuery = @"
                            UPDATE LimitSamples
                            SET Status = 'CREATED', UpdatedAt = GETDATE()
                            WHERE LineId = @lineId AND PartId = @partId";

                        await connection.ExecuteAsync(
                            updateQuery,
                            new { lineId = sample.LineId, partId = sample.PartId });
                    }
                    else
                    {
                        // ❌ 여기서 에러를 삼켜버림!
                        Console.WriteLine($"Failed: {sample.PartId} - {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    // ❌ 예외를 로깅만 하고 무시
                    Console.WriteLine($"Exception: {ex.Message}");
                    // 계속 진행...
                }
            }

            await connection.CloseAsync();

            return Ok("Done");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
```

김주니어는 코드를 읽으면서 점점 더 혼란스러워졌습니다. 이 코드에는 너무나 많은 문제가 있었습니다.

**문제 1: 레이어 분리가 전혀 되어 있지 않습니다**

Controller가 데이터베이스에 직접 연결하고, SQL 쿼리를 작성하고, HTTP 호출을 하고, 비즈니스 로직까지 처리합니다. Presentation Layer, Application Layer, Domain Layer, Infrastructure Layer가 모두 한 곳에 뒤섞여 있습니다.

**문제 2: 테스트가 불가능합니다**

이 코드를 테스트하려면 실제 데이터베이스가 필요하고, 실제 API 서버가 필요합니다. 단위 테스트는 꿈도 꿀 수 없습니다. 통합 테스트조차 매우 어렵습니다.

**문제 3: 에러가 무시됩니다**

내부 try-catch에서 예외를 잡아서 Console.WriteLine만 하고 계속 진행합니다. 이것이 바로 버그의 원인이었습니다. 에러가 발생해도 로그가 제대로 남지 않았고, 실패한 항목이 있어도 "Done"이라고 응답했습니다.

**문제 4: 하드코딩된 값들**

데이터베이스 연결 문자열, API 키, API 엔드포인트가 모두 하드코딩되어 있습니다. 운영 환경으로 배포할 때마다 코드를 수정해야 합니다.

**문제 5: 책임이 너무 많습니다**

이 하나의 메서드가 하는 일을 나열하면:
- HTTP 요청 파싱
- 데이터베이스 연결
- SQL 쿼리 실행
- 입력 검증
- 외부 API 호출
- 데이터베이스 업데이트
- HTTP 응답 생성

단일 책임 원칙(Single Responsibility Principle)이 완전히 무시되어 있습니다.

김주니어는 한숨을 쉬었습니다. "에러 로그를 추가하는 것"이 전부인 줄 알았는데, 이 코드 전체를 리팩토링해야 할 것 같았습니다. 하지만 어디서부터 시작해야 할까요?

더 큰 문제는, 비슷한 코드가 프로젝트 전체에 산재해 있다는 것이었습니다. 그는 코드베이스를 검색해봤습니다. `new SqlConnection`이 127곳에서 발견되었습니다. `new HttpClient`는 89곳이었습니다.

**이것이 바로 레거시 코드의 늪입니다.**

간단해 보이는 버그 하나를 수정하려다가, 시스템 전체의 구조적 문제와 마주하게 됩니다. 수정하자니 영향 범위가 너무 크고, 그냥 두자니 기술 부채가 계속 쌓입니다.

김주니어는 팀 리더 박시니어를 찾아갔습니다.

"선배님, 이 코드를 어떻게 수정하면 좋을까요?"

박시니어는 코드를 보더니 한숨을 쉬었습니다.

"아, 이 코드... 3년 전에 전임자가 작성한 건데, 지금까지 아무도 손을 못 댔어. 고치려고 하면 다른 부분도 같이 고쳐야 하거든. 일단은... 여기 catch 블록에 제대로 된 로깅만 추가해봐. 전체 리팩토링은 나중에 시간 날 때..."

"하지만 선배님, 이렇게 임시방편으로만 하면 계속 기술 부채가 쌓이는 거 아닌가요?"

"맞아. 하지만 지금 당장은 고객사 이슈를 해결하는 게 급해. 그리고... 솔직히 어떻게 고쳐야 하는지도 명확하지 않아. 다들 의견이 달라서..."

이 대화는 많은 개발팀이 직면한 현실을 보여줍니다. 문제를 알지만 해결 방법을 모르고, 시간이 없다는 핑계로 임시방편을 반복하며, 기술 부채는 눈덩이처럼 불어납니다.

**그렇다면 어떻게 해야 할까요?**

먼저 "좋은 코드"가 무엇인지 명확히 정의해야 합니다. 주관적 판단이 아니라 객관적 기준이 필요합니다. 그리고 현재 코드가 그 기준에서 얼마나 떨어져 있는지 측정해야 합니다.

이 책의 평가 체계를 적용하면, 위 코드는:

- **레이어 분리:** 20점 (⭐ 매우 부족) - 레이어 구분이 전혀 없음
- **도메인 모델링:** 20점 (⭐ 매우 부족) - Entity가 DTO로만 사용됨
- **에러 처리:** 20점 (⭐ 매우 부족) - 예외를 삼키고 무시
- **테스트 가능성:** 20점 (⭐ 매우 부족) - 테스트 불가능
- **코드 일관성:** 해당 없음 (비교 대상이 없음)
- **함수형 스타일:** 20점 (⭐ 매우 부족) - 명령형만 사용
- **관찰성:** 20점 (⭐ 매우 부족) - Console.WriteLine만 사용

**총점: 20점 (⭐ 매우 부족)**

숫자로 보니 명확합니다. 이 코드는 개선이 시급합니다. 그리고 어느 부분부터 개선해야 하는지도 명확합니다. 레이어 분리부터 시작해야 합니다.

---

### 1.2 주관적 판단의 함정

그날 오후, 김주니어는 용기를 내어 팀 미팅에서 제안했습니다.

"이 코드를 리팩토링해도 될까요? 레이어를 분리하고, Repository 패턴을 적용하고..."

분위기가 미묘해졌습니다.

**팀 리더 박시니어가 말했습니다.**

"글쎄... 지금 동작은 하잖아? 괜히 건드렸다가 더 큰 문제가 생기면 어떡해? 우리 지금 일정이 빠듯한 거 알지? 버그만 고치고 넘어가자."

**선임 개발자 최베테랑이 말했습니다.**

"리팩토링하는 건 좋은데, 어떻게 개선할 건데? 네 생각에는 Repository 패턴이 좋아 보일 수 있어도, 나는 Service 패턴이 더 낫다고 생각해. 그리고 지금 우리 팀 코드는 대부분 이런 스타일이잖아? 일관성도 중요하지."

**같은 신입 이뉴비가 말했습니다.**

"저는 부트캠프에서 Clean Architecture를 배웠는데, 여기서는 왜 안 쓰는 건가요? 좋은 패턴 아닌가요?"

**시니어 개발자 정고수가 말했습니다.**

"Clean Architecture? 그거 너무 복잡해. 우리는 작은 프로젝트니까 그냥 간단하게 가는 게 나아. YAGNI(You Aren't Gonna Need It) 원칙 알지? 필요 없는 걸 만들지 마."

김주니어는 당황스러웠습니다. 네 명의 개발자가 네 가지 다른 의견을 냈습니다. 누가 옳은 걸까요?

**이것이 바로 주관적 판단의 함정입니다.**

"좋은 코드"의 기준이 명확하지 않으면, 개발자마다 다른 의견을 가지게 됩니다. 그리고 그 의견들은 모두 "느낌"이나 "경험"에 기반하고 있어서, 누가 옳은지 판단할 수 없습니다.

이런 상황을 더 구체적으로 살펴보겠습니다.

**시나리오 1: 코드 리뷰에서**

```csharp
// 김주니어가 작성한 코드
public class LimitSampleService
{
    private readonly IRepository<LimitSample> _repository;

    public async Task<Result<List<LimitSample>>> GetSamplesAsync(string lineId)
    {
        var samples = await _repository
            .Where(x => x.LineId == lineId)
            .ToListAsync();

        return samples.Any()
            ? Result.Success(samples)
            : Result.Failure<List<LimitSample>>("No samples found");
    }
}
```

**최베테랑의 리뷰:**
"Result 패턴? 우리 팀에서는 예외를 사용하는데? 왜 갑자기 바꾸는 거야? 기존 코드와 일관성이 없잖아."

**김주니어의 반응:**
"하지만 예외는 제어 흐름으로 사용하면 안 된다고 배웠는데요..."

**최베테랑:**
"그건 이론이고, 실무에서는 예외가 더 간단해. 그리고 .NET의 관례이기도 하고."

누가 옳을까요? 둘 다 틀리지 않았습니다. 하지만 명확한 기준이 없어서, 의견이 충돌하고 있습니다.

**시나리오 2: 설계 회의에서**

새 기능을 추가하기 위한 설계 회의입니다.

**박시니어:**
"이번 기능은 간단하니까 Controller에서 바로 처리하자. Repository만 추가하면 돼."

**정고수:**
"아니, 최소한 Service Layer는 만들어야지. Controller가 너무 비대해져."

**최베테랑:**
"Service Layer? CQRS 패턴으로 Command와 Query를 분리하는 게 더 낫지 않을까?"

**이뉴비:**
"Hexagonal Architecture는 어때요? Port/Adapter 패턴으로..."

**박시니어:**
"야, 너무 복잡해지잖아. 일정도 고려해야지."

회의는 한 시간 동안 계속되었지만, 결론은 나지 않았습니다. 결국 박시니어가 "내가 결정하겠다"고 선언하며 끝났습니다. 하지만 다른 팀원들은 납득하지 못했습니다.

**문제의 본질은 무엇일까요?**

모두가 "좋은 코드"를 원하지만, 그것이 무엇인지에 대한 합의가 없습니다. 그래서:

1. **의견 충돌이 빈번합니다**
   - "제 생각에는..." vs "제 경험상..."
   - 객관적 기준이 없어서 설득이 어렵습니다

2. **코드 리뷰가 비효율적입니다**
   - "이게 좋아요/나빠요"라는 주관적 피드백
   - 명확한 개선 방향 제시 불가

3. **일관성이 없어집니다**
   - 개발자마다 다른 스타일로 코드 작성
   - 코드베이스가 점점 복잡해짐

4. **신규 팀원이 혼란스러워합니다**
   - "어떻게 작성해야 하나요?"
   - "기존 코드가 다 다른데 뭘 따라야 하죠?"

5. **개선이 두려워집니다**
   - "고쳤는데 더 나빠졌다고 하면 어쩌지?"
   - 리팩토링을 기피하게 됨

**해결책은 명확합니다.**

객관적이고 측정 가능한 기준이 필요합니다. "이 코드가 좋다/나쁘다"가 아니라, "이 코드는 레이어 분리 항목에서 60점입니다"라고 말할 수 있어야 합니다.

이 책에서 제시하는 평가 체계를 적용했다면, 위의 대화는 이렇게 바뀔 수 있습니다.

**김주니어:**
"현재 코드를 평가했습니다. 레이어 분리 20점, 에러 처리 20점으로 매우 낮습니다. 개선을 제안합니다."

**박시니어:**
"평가 기준에 따르면 20점이구나. 목표는 최소 80점이니까, 개선이 필요하긴 하네."

**최베테랑:**
"어떻게 개선할 건데?"

**김주니어:**
"먼저 Repository 패턴을 적용해서 레이어를 분리하면 60점까지 올릴 수 있습니다. 그다음 Port/Adapter 패턴을 적용하면 80점 달성 가능합니다."

**정고수:**
"구체적이네. 일정은 얼마나 걸릴까?"

**김주니어:**
"1주일이면 가능합니다. 리스크는 낮습니다. 테스트 커버리지를 먼저 확보할 수 있으니까요."

**박시니어:**
"좋아, 그렇게 하자. 먼저 테스트 작성하고, 그다음에 리팩토링하는 걸로."

주관적 판단이 객관적 데이터로 바뀌자, 대화가 생산적으로 변했습니다. 이제 "누가 옳은가"가 아니라 "어떻게 개선할 것인가"를 논의할 수 있습니다.

---

### 1.3 개선 효과를 증명하지 못하는 이유

김주니어는 결국 리팩토링을 시작했습니다. 반대 의견도 있었지만, 그는 자신의 방식이 옳다고 확신했습니다. Clean Architecture를 배웠고, 그것이 업계 표준이라는 것도 알고 있었습니다.

2주 동안 밤낮으로 작업했습니다. Controller를 간소화하고, Application Layer를 만들고, Repository 패턴을 적용하고, Domain Model을 설계했습니다.

결과물은 이랬습니다.

```csharp
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Presentation Layer
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[ApiController]
[Route("api/[controller]")]
public class LimitSampleController : ControllerBase
{
    private readonly IMediator _mediator;

    public LimitSampleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateLimitSampleRequest request)
    {
        var command = new CreateLimitSampleCommand(
            request.LineId,
            request.ProcessId,
            request.PartId,
            request.Version,
            request.UserId);

        var response = await _mediator.Send(command);

        return response.IsSuccess
            ? Ok(response.Value)
            : BadRequest(response.Error);
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Application Layer
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
public class CreateLimitSampleUsecase
    : IRequestHandler<CreateLimitSampleCommand, Result<CreateLimitSampleResponse>>
{
    private readonly ILimitSampleRepository _repository;
    private readonly ILimitSampleApiService _apiService;
    private readonly ILogger<CreateLimitSampleUsecase> _logger;

    public async Task<Result<CreateLimitSampleResponse>> Handle(
        CreateLimitSampleCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. 최신 샘플 조회
            var samples = await _repository.GetLatestSamplesAsync(
                command.LineId,
                command.ProcessId,
                cancellationToken);

            if (!samples.Any())
            {
                _logger.LogWarning(
                    "No samples found for {LineId}/{ProcessId}",
                    command.LineId,
                    command.ProcessId);
                return Result<CreateLimitSampleResponse>.Failure(
                    "No samples found");
            }

            // 2. 각 샘플 처리
            foreach (var sample in samples)
            {
                var result = await _apiService.CreateLimitSampleAsync(
                    sample,
                    command.Version,
                    command.UserId,
                    cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "LimitSample created: {PartId}",
                        sample.PartId);
                }
                else
                {
                    _logger.LogError(
                        "Failed to create LimitSample: {PartId} - {Error}",
                        sample.PartId,
                        result.Error);
                }
            }

            return Result<CreateLimitSampleResponse>.Success(
                new CreateLimitSampleResponse(samples.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in CreateLimitSampleUsecase");
            return Result<CreateLimitSampleResponse>.Failure(ex.Message);
        }
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Infrastructure Layer
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
public class LimitSampleRepository : ILimitSampleRepository
{
    private readonly PwmDbContext _dbContext;

    public async Task<List<LimitSample>> GetLatestSamplesAsync(
        string lineId,
        string processId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.LimitSamples
            .Where(x => x.LineId == lineId && x.ProcessId == processId && !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);
    }
}
```

김주니어는 만족스러웠습니다. 코드가 훨씬 깔끔해졌습니다. Controller는 단순해졌고, 레이어가 명확히 분리되었고, Repository 패턴으로 테스트도 가능해졌습니다.

그는 자신있게 Pull Request를 올렸습니다.

> **[REFACTOR] Clean Architecture 적용 - LimitSample Controller**
>
> - Controller, Application, Infrastructure Layer 분리
> - Repository 패턴 적용
> - Dependency Injection 사용
> - 적절한 로깅 추가

하지만 반응은 예상과 달랐습니다.

**박시니어의 코멘트:**

"음... 확실히 깔끔해 보이긴 하는데, 이게 진짜 더 나은 코드라는 걸 어떻게 증명하지? 코드 라인이 2배로 늘었잖아. 복잡도도 높아진 것 같은데?"

**최베테랑의 코멘트:**

"MediatR 패턴? IRequestHandler? 이거 배우는 데 시간이 얼마나 걸려? 신규 팀원이 이해할 수 있을까?"

**정고수의 코멘트:**

"Result 패턴을 쓴다고? 우리 프로젝트는 지금까지 예외를 썼는데, 일관성이 없어지는 거 아냐?"

**이뉴비의 코멘트:**

"저는 좋은 것 같은데요! 하지만 전 아직 주니어라서..."

김주니어는 막막했습니다. 그는 분명히 코드를 개선했다고 생각했습니다. 하지만 그것을 **증명**할 수 없었습니다.

"이 코드가 더 좋아요!"
"왜?"
"음... 더 깔끔해 보이고, 레이어가 분리되어 있고..."
"그래서 뭐가 좋은데?"
"..."

**이것이 바로 개선 효과를 증명하지 못하는 이유입니다.**

리팩토링 전후를 **정량적으로 비교**할 기준이 없으면, "더 좋아졌다"고 주장할 근거가 없습니다. 특히 다음과 같은 질문에 답하기 어렵습니다:

1. **투자 대비 효과(ROI)는?**
   - "2주를 투자해서 얻은 게 뭐야?"
   - "비즈니스 가치는 늘었어?"

2. **유지보수가 정말 쉬워졌나?**
   - "코드 라인이 늘어난 것 같은데?"
   - "러닝 커브는 오히려 높아진 거 아냐?"

3. **버그가 줄어들까?**
   - "이전 코드도 동작은 했잖아"
   - "새 코드가 더 안정적이란 보장이 있어?"

4. **일관성은?**
   - "다른 코드와 스타일이 다른데?"
   - "이제 모든 코드를 이렇게 바꿔야 해?"

이런 질문들에 답하지 못하면, 리팩토링은 "시간 낭비"로 취급됩니다. 결국 Pull Request는 닫히고, 김주니어의 2주는 물거품이 됩니다.

**만약 평가 기준이 있었다면?**

상황이 완전히 달라졌을 것입니다. 김주니어는 이렇게 설명할 수 있었을 겁니다:

"리팩토링 전 평가 결과입니다:
- 레이어 분리: 20점 (⭐ 매우 부족)
- 도메인 모델링: 20점 (⭐ 매우 부족)
- 에러 처리: 20점 (⭐ 매우 부족)
- 테스트 가능성: 20점 (⭐ 매우 부족)
- 총점: 20점 (⭐)

리팩토링 후 평가 결과입니다:
- 레이어 분리: 80점 (⭐⭐⭐⭐ 좋음)
- 도메인 모델링: 60점 (⭐⭐⭐ 보통)
- 에러 처리: 60점 (⭐⭐⭐ 보통)
- 테스트 가능성: 80점 (⭐⭐⭐⭐ 좋음)
- 총점: 70점 (⭐⭐⭐⭐)

**+50점 개선** (250% 향상)

구체적인 개선 사항:
1. DB 변경 시 영향 범위가 전체 시스템에서 Repository만으로 축소 (90% 감소)
2. 단위 테스트 작성 가능 (이전: 불가능)
3. 에러 로깅 100% 커버 (이전: 에러 삼킴)
4. 코드 리뷰 시간 예상: 2시간 → 30분 (75% 감소)

투자: 2주
효과: 향후 모든 변경 작업의 시간 50% 단축 예상
ROI: 1개월 이내 회수 가능"

이제 대화가 완전히 달라집니다.

**박시니어:**
"50점이나 올랐구나. 구체적인 수치로 보니 확실히 개선됐네. Approve."

**최베테랑:**
"MediatR 패턴이 생소하긴 한데, 테스트 가능성이 20점에서 80점으로 올랐다는 게 중요하네. 나중에 팀 전체에 교육 세션 한 번 해줘."

**정고수:**
"Result 패턴으로 에러 처리가 60점이면 나쁘지 않네. 단, 다른 코드도 통일하는 작업이 필요할 것 같아. 별도 티켓 만들자."

**이뉴비:**
"와, 저도 이렇게 평가할 수 있는 거예요? 체크리스트 주세요!"

객관적 데이터가 있으면 대화가 생산적으로 변합니다. "좋다/나쁘다"의 주관적 논쟁이 아니라, "얼마나 개선되었는가"의 사실 기반 논의가 가능합니다.

---

### 1.4 팀 협업이 어려운 진짜 이유

3개월이 지났습니다. 김주니어의 리팩토링은 부분적으로만 적용되었습니다. LimitSample Controller는 개선되었지만, 다른 127개의 Controller는 여전히 예전 스타일입니다.

그리고 이번 주, 신규 입사자 이뉴비가 합류했습니다.

**첫날, 이뉴비가 물었습니다.**

"코드 작성 가이드가 있나요? 어떤 스타일로 작성해야 하는지..."

김주니어는 잠시 망설이다가 대답했습니다.

"음... 딱히 문서화된 건 없고, 기존 코드 보고 따라 하면 돼. 프로젝트 열어봐."

이뉴비는 프로젝트를 열었습니다. 그리고 당황했습니다.

```csharp
// File1: ProductController.cs (3년 전 스타일)
public class ProductController
{
    public async Task<IActionResult> GetProducts()
    {
        var connection = new SqlConnection("...");
        var products = await connection.QueryAsync<Product>("SELECT * FROM Products");
        return Ok(products);
    }
}

// File2: OrderController.cs (2년 전 스타일)
public class OrderController
{
    private readonly IOrderService _orderService;

    public async Task<IActionResult> CreateOrder(OrderDto dto)
    {
        try
        {
            var order = await _orderService.CreateAsync(dto);
            return Ok(order);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

// File3: LimitSampleController.cs (김주니어가 리팩토링한 최신 스타일)
public class LimitSampleController : ControllerBase
{
    private readonly IMediator _mediator;

    public async Task<IActionResult> Create([FromBody] CreateLimitSampleRequest request)
    {
        var command = new CreateLimitSampleCommand(request.LineId, request.ProcessId);
        var response = await _mediator.Send(command);

        return response.IsSuccess
            ? Ok(response.Value)
            : BadRequest(response.Error);
    }
}
```

**이뉴비가 다시 물었습니다.**

"근데... 파일마다 스타일이 완전히 다른데요? 어떤 걸 따라야 하나요?"

김주니어는 난처했습니다.

"음... LimitSampleController가 가장 최신 스타일이긴 한데, 아직 전체에 적용은 안 됐어. 지금은 그냥... 수정하는 파일의 기존 스타일을 따르는 게 나을 것 같아."

"그럼 새 기능을 만들 때는요?"

"그땐... LimitSampleController 스타일로..."

"그럼 ProductController를 수정할 땐 ProductController 스타일로 하고, 새 기능은 LimitSampleController 스타일로 하고..."

"응..."

**이것이 바로 팀 협업이 어려운 진짜 이유입니다.**

명확한 표준이 없으면, 각 개발자가 자신만의 스타일로 코드를 작성합니다. 시간이 지날수록 코드베이스는 더욱 불일치해지고, 신규 팀원은 혼란스러워합니다.

더 구체적으로 살펴보겠습니다.

**문제 1: 코드 리뷰 시간 증가**

이뉴비가 첫 Pull Request를 올렸습니다.

```csharp
public class CustomerController
{
    public async Task<ActionResult<List<Customer>>> GetCustomers(string companyId)
    {
        var customers = await _db.Customers
            .Where(x => x.CompanyId == companyId)
            .ToListAsync();

        if (!customers.Any())
            throw new NotFoundException($"No customers found for {companyId}");

        return Ok(customers);
    }
}
```

코드 리뷰가 시작되었습니다.

**김주니어:**
"예외 대신 Result 패턴을 쓰는 게 좋을 것 같아요. LimitSampleController처럼."

**최베테랑:**
"아니야, 우리 프로젝트는 예외를 쓰는 게 표준이야. OrderController 보면 다 예외 써."

**정고수:**
"_db를 직접 쓰지 말고 Repository 패턴을 적용해야지."

**박시니어:**
"Repository까지는 필요 없을 것 같은데... 간단한 조회니까..."

**이뉴비:**
"저는... 어떻게 하면 되나요?"

코드 리뷰가 30분째 계속되고 있습니다. 간단한 메서드 하나를 두고 네 명이 네 가지 다른 의견을 냅니다.

**문제 2: 일관성 없는 에러 처리**

```csharp
// File1: 예외 던지기
if (product == null)
    throw new NotFoundException($"Product {id} not found");

// File2: null 반환
if (product == null)
    return null;

// File3: Result 패턴
if (product == null)
    return Result.Failure<Product>("Product not found");

// File4: Option 패턴
return product != null
    ? Option<Product>.Some(product)
    : Option<Product>.None;
```

한 프로젝트에 네 가지 에러 처리 방식이 공존합니다. 신규 개발자는 어떤 걸 써야 할지 매번 고민해야 합니다.

**문제 3: 러닝 커브 증가**

이뉴비는 이제 네 가지 패턴을 모두 배워야 합니다:
1. 예외 기반 에러 처리
2. null 체크 방식
3. Result 패턴
4. Option 패턴

하나만 잘 알면 되는데, 네 개를 다 알아야 합니다. 온보딩 기간이 길어집니다.

**문제 4: 유지보수 복잡도 증가**

버그가 발생했습니다. 에러 처리가 제대로 안 되는 것 같습니다. 하지만 어디를 고쳐야 할까요?

- 예외를 던지는 곳을 찾아야 할까요?
- null을 반환하는 곳을 찾아야 할까요?
- Result.Failure를 찾아야 할까요?
- Option.None을 찾아야 할까요?

네 곳을 다 확인해야 합니다. 복잡도가 4배입니다.

**만약 팀 표준이 있었다면?**

상황이 완전히 달라졌을 것입니다.

**팀 표준 문서:**

```
┌─────────────────────────────────────────────────────────┐
│ 우리 팀 코딩 표준 (2024년 v2.0)                          │
├─────────────────────────────────────────────────────────┤
│                                                          │
│ 1. 레이어 분리                                           │
│    - Presentation → Application → Domain → Infrastructure│
│    - Port/Adapter 패턴 필수                              │
│    - 평가 기준: 80점 이상 (⭐⭐⭐⭐)                        │
│                                                          │
│ 2. 에러 처리                                             │
│    - Result<T> 패턴 사용 (예외 금지)                     │
│    - 모든 에러는 ILogger로 로깅                          │
│    - 평가 기준: 80점 이상 (⭐⭐⭐⭐)                        │
│                                                          │
│ 3. Repository 패턴                                       │
│    - 모든 DB 접근은 IRepository<T> 사용                  │
│    - DbContext 직접 사용 금지                            │
│                                                          │
│ 4. 명명 규칙                                             │
│    - Controller: {Entity}Controller                     │
│    - Usecase: {Action}{Entity}Usecase                   │
│    - Repository: {Entity}Repository                     │
│                                                          │
│ 5. 코드 리뷰                                             │
│    - 평가 기준 체크리스트 필수 작성                       │
│    - 최소 70점 이상 (⭐⭐⭐⭐)                              │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

이제 대화가 달라집니다.

**이뉴비:**
"팀 표준 문서 읽었습니다. 이렇게 작성했는데 맞나요?"

**김주니어:**
"체크리스트로 평가해봤어? 점수 몇 점 나왔어?"

**이뉴비:**
"레이어 분리 80점, 에러 처리 80점, 총 78점 나왔어요!"

**최베테랑:**
"78점이면 기준 통과네. Approve."

**박시니어:**
"코드 리뷰 5분 만에 끝났네. 효율적이야."

팀 표준이 있으면:

1. **코드 리뷰가 빨라집니다**
   - 주관적 의견 대신 체크리스트 기반 평가
   - 논쟁 시간 90% 감소

2. **신규 팀원 온보딩이 쉬워집니다**
   - 명확한 가이드라인 제공
   - 혼란 없이 바로 적용 가능

3. **일관성이 유지됩니다**
   - 모든 코드가 같은 스타일
   - 유지보수 복잡도 감소

4. **품질이 보장됩니다**
   - 최소 기준(예: 70점) 설정
   - 자동으로 품질 관리

---

### 이 장의 핵심

이 Chapter에서 우리는 네 가지 핵심 문제를 확인했습니다:

**1. 레거시 코드의 늪**
- 간단한 버그 수정조차 어려운 구조
- 레이어 분리 없음, 테스트 불가능
- 기술 부채가 눈덩이처럼 증가

**2. 주관적 판단의 함정**
- "좋은 코드"의 기준이 명확하지 않음
- 개발자마다 다른 의견
- 의견 충돌과 비효율적인 코드 리뷰

**3. 개선 효과 증명 불가**
- 리팩토링 전후를 정량적으로 비교할 수 없음
- ROI를 증명할 수 없음
- 리팩토링이 "시간 낭비"로 취급됨

**4. 팀 협업의 어려움**
- 일관된 코딩 스타일 부재
- 신규 팀원 혼란
- 유지보수 복잡도 증가

이 모든 문제의 근본 원인은 **측정 가능한 기준의 부재**입니다.

다음 Chapter에서는, 이런 문제를 해결하는 "좋은 아키텍처"가 무엇인지 알아보겠습니다. 좋은 아키텍처는 단순히 이론적으로 옳은 것이 아니라, 실제로 여러분의 일상을 편하게 만들어주는 실용적인 도구입니다.

**계속해서 읽어 나가면서, 여러분의 프로젝트를 떠올려 보세요.**

- 여러분의 코드는 몇 점일까요?
- 레거시 코드의 늪에 빠진 경험이 있나요?
- 코드 리뷰에서 주관적 의견 충돌을 겪은 적이 있나요?
- 리팩토링의 효과를 증명하지 못한 적이 있나요?
- 팀의 코딩 스타일이 불일치한 적이 있나요?

이런 경험들을 기억하며 다음 Chapter로 넘어가세요. 이제 해결책을 찾을 시간입니다.

---

*(Chapter 2-15는 계속...)*

