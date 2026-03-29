---
title: "ADR-0001: 아키텍처 의사결정 기록(ADR) 도입"
status: "accepted"
date: 2026-03-30
---

## 맥락과 문제

Functorium 프레임워크는 함수형 프로그래밍, CQRS, 소스 제너레이터, 관측성 등 다양한 아키텍처 결정을 내려왔습니다. 그러나 이러한 결정의 근거가 커밋 메시지, 코드 주석, PR 설명 등에 흩어져 있어 "왜 이 방식을 선택했는가"를 추적하기 어렵습니다.

새로운 기여자가 프로젝트에 합류하거나 기존 결정을 재검토할 때, 당시의 맥락과 검토한 대안을 복원하는 데 상당한 시간이 소요됩니다. 아키텍처 결정을 체계적으로 기록하고 관리할 방법이 필요합니다.

## 검토한 옵션

1. MADR v4.0 템플릿 기반 ADR
2. Nygard 원본 ADR 형식
3. Confluence Wiki 기반 기록
4. ADR 미도입 (현상 유지)

## 결정

**선택한 옵션: "MADR v4.0 템플릿 기반 ADR"**, 대안 비교 구조가 내장되어 있고 Markdown 기반이라 코드와 함께 버전 관리가 가능하기 때문입니다.

### 결과

- Good, because 결정의 맥락과 대안이 코드 저장소에 영구 보존됩니다.
- Good, because 새로운 기여자가 아키텍처 판단 근거를 빠르게 파악할 수 있습니다.
- Bad, because ADR 작성에 추가적인 시간이 소요됩니다.

### 확인

- `Docs.Site/src/content/docs/decisions/` 경로에 ADR 파일이 존재하는지 확인합니다.
- 새로운 아키텍처 결정 시 ADR이 함께 작성되는지 코드 리뷰에서 점검합니다.

## 옵션별 장단점

### MADR v4.0 템플릿 기반 ADR

- Good, because 옵션별 장단점 비교 구조가 내장되어 의사결정 근거가 명확합니다.
- Good, because Markdown 기반이라 코드 저장소에서 버전 관리됩니다.
- Good, because Docs.Site와 통합되어 문서 사이트에서 바로 열람 가능합니다.
- Bad, because 결정마다 별도 파일 작성이 필요하여 초기 작성 비용이 있습니다.

### Nygard 원본 ADR 형식

- Good, because 간결하고 작성이 빠릅니다.
- Bad, because 대안 비교 구조가 없어 "왜 다른 옵션을 선택하지 않았는가"가 누락됩니다.

### Confluence Wiki 기반 기록

- Good, because 비개발자도 쉽게 접근할 수 있습니다.
- Bad, because 코드와 분리되어 버전 관리가 되지 않습니다.
- Bad, because 코드 변경과 문서 업데이트가 동기화되지 않을 위험이 있습니다.

### ADR 미도입 (현상 유지)

- Good, because 추가 작업이 전혀 없습니다.
- Bad, because 시간이 지남에 따라 결정 근거가 사라지고 동일한 논의가 반복됩니다.

## 관련 정보

- MADR v4.0 템플릿: https://adr.github.io/madr/
- 관련 문서: `Docs.Site/src/content/docs/decisions/index.md`
