# Entity 가이드 - 여러 Primitive 필드 검증 패턴 추가

## 요청 사항

entity-guide.md의 "Value Object가 없는 필드의 검증" 섹션에 추가:
- **VO 없는 필드가 여러 개일 때** 검증 패턴

---

## 현재 상태

`entity-guide.md`의 "Value Object가 없는 필드의 검증" 섹션 (1056~1116행):
- VO + Primitive 혼합 예제: VO 1~2개 + Primitive 1개
- **누락**: VO 없는 필드가 여러 개일 때의 패턴

`valueobject-guide.md`의 759-767행에 참고할 패턴 존재:
```csharp
(ValidationRules.For("ProductName").NotEmpty(request.Name).ThenMaxLength(100),
 ValidationRules.For("Price").Positive(request.Price),
 ValidationRules.For("Category").NotEmpty(request.Category))
    .Apply((name, price, category) => request);
```

---

## 추가할 내용

### 여러 Primitive 필드 검증 시나리오

1. **VO 없음 + Primitive 여러 개**: 모든 필드가 Named Context 또는 Context Class로 검증
2. **VO + Primitive 여러 개 혼합**: VO와 여러 Primitive를 튜플로 병합

---

## 수정 파일

| 파일 | 변경 내용 |
|------|----------|
| `.claude/guides/entity-guide.md` | "여러 Primitive 필드 검증" 예제 추가 |

---

## 수정 위치

`entity-guide.md`의 "Context Class (재사용 가능한 검증)" 예제 이후, "검증 방식 선택 가이드" 테이블 이전에 추가

---

## 추가할 내용

```markdown
**여러 Primitive 필드 검증:**

VO 없는 필드가 여러 개일 때도 동일하게 튜플과 `Apply()`로 병합합니다.

```csharp
// VO 1개 + Primitive 여러 개
public async Task<Fin<Order>> CreateOrderAsync(CreateOrderCommand cmd)
{
    var amountResult = Money.Create(cmd.Amount, cmd.Currency);

    // VO 없는 필드 여러 개: 각각 Named Context 사용
    var noteResult = ValidationRules.For("Note")
        .NotEmpty(cmd.Note)
        .ThenMaxLength(500);

    var tagResult = ValidationRules.For("Tag")
        .NotEmpty(cmd.Tag)
        .ThenMaxLength(50);

    var priorityResult = ValidationRules.For("Priority")
        .InRange(cmd.Priority, 1, 5);

    // 모두 튜플로 병합
    return (amountResult, noteResult, tagResult, priorityResult)
        .Apply((amount, note, tag, priority) =>
            Order.Create(amount, note, tag, priority));
}
```
```

---

## 검증 방법

1. 마크다운 렌더링 확인
2. 코드 예제 문법 확인
