# Adapter 가이드 문서 개선 계획

## 완료된 작업

### adapter-guide.md 신규 작성

기존 `_IAdapter-Interface-Pattern.md` 문서를 `valueobject-guide.md`, `entity-guide.md`와 동일한 스타일로 전면 재작성했습니다.

**새 문서**: `.claude/guides/adapter-guide.md`

**문서 구조:**
- 개요 (왜 사용하는지, 문제점과 해결책)
- 인터페이스 계층 구조 (다이어그램)
- IAdapter 인터페이스 설명
- GeneratePipeline 소스 생성기 상세
- 구현 패턴 (Repository, Messaging, External API)
- 의존성 등록 방법
- 실전 예제 (Clean Architecture 전체 흐름)
- FAQ
- 참고 문서

---

## 추가 작업 필요

### 1. 기존 파일 정리

**삭제 대상**: `.claude/guides/_IAdapter-Interface-Pattern.md`
- 새 `adapter-guide.md`로 대체됨
- 언더스코어 prefix 파일은 Draft 상태를 의미했으므로 정리

### 2. domain-modeling-overview.md 업데이트

`§6. 가이드 문서 색인` 테이블에 Adapter 가이드 추가:

```markdown
| [adapter-guide.md](./adapter-guide.md) | Adapter 구현 | IAdapter 인터페이스, [GeneratePipeline], Pipeline 자동 생성 |
```

---

## 검증 방법

```powershell
# 1. 문서 링크 확인
# domain-modeling-overview.md에서 adapter-guide.md 링크 작동 확인

# 2. 빌드 확인 (문서 변경은 빌드에 영향 없음)
dotnet build Functorium.All.slnx
```

---

## 변경 파일 목록

| 파일 | 변경 내용 |
|------|----------|
| `.claude/guides/adapter-guide.md` | ✅ 신규 작성 완료 |
| `.claude/guides/_IAdapter-Interface-Pattern.md` | 삭제 예정 |
| `.claude/guides/domain-modeling-overview.md` | 가이드 색인 테이블에 추가 |
