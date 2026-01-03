# Functorium Release Notes

이 폴더는 Functorium 프로젝트의 릴리스 노트 문서와 생성 도구를 포함합니다.

## 폴더 구조

```
.release-notes/
├── README.md                           # 이 문서
├── TEMPLATE.md                         # 릴리스 노트 작성 템플릿
├── validate-release-notes.ps1          # 릴리스 노트 크기 검증 스크립트
├── RELEASE-v{VERSION}.md               # 영문 릴리스 노트
├── RELEASE-v{VERSION}-KR.md            # 한글 릴리스 노트
├── RELEASE-v{VERSION}-KR.mp4           # 릴리스 소개 영상
├── RELEASE-v{VERSION}-KR.m4a           # 릴리스 소개 음성
└── scripts/                            # 릴리스 노트 생성 스크립트
    ├── AnalyzeAllComponents.cs         # 컴포넌트 변경사항 분석
    ├── AnalyzeFolder.cs                # 개별 폴더 상세 분석
    ├── ExtractApiChanges.cs            # API 변경사항 추출
    ├── ApiGenerator.cs                 # Public API 생성
    ├── config/                         # 분석 설정 파일
    ├── docs/                           # 5-Phase 워크플로우 문서
    └── .analysis-output/               # 분석 결과 출력
```

## 릴리스 노트 목록

| 버전 | 문서 | 미디어 | 설명 |
|------|------|--------|------|
| v1.0.0-alpha.1 | [영문](RELEASE-v1.0.0-alpha.1.md) / [한글](RELEASE-v1.0.0-alpha.1-KR.md) | [MP4](RELEASE-v1.0.0-alpha.1-KR.mp4) / [M4A](RELEASE-v1.0.0-alpha.1-KR.m4a) | 첫 번째 알파 릴리스 |

## 릴리스 노트 작성 가이드

### 빠른 시작

1. **템플릿 복사**: `TEMPLATE.md`를 `RELEASE-v{VERSION}.md`로 복사
2. **데이터 수집**: `scripts/` 폴더의 분석 스크립트 실행
3. **내용 작성**: 5-Phase 워크플로우에 따라 작성
4. **검증**: `validate-release-notes.ps1`로 크기 검증

### 5-Phase 워크플로우

릴리스 노트 작성은 다음 5단계 워크플로우를 따릅니다:

| Phase | 문서 | 설명 |
|-------|------|------|
| 1 | [phase1-setup.md](scripts/docs/phase1-setup.md) | 환경 검증 및 준비 |
| 2 | [phase2-collection.md](scripts/docs/phase2-collection.md) | 데이터 수집 (컴포넌트/API 분석) |
| 3 | [phase3-analysis.md](scripts/docs/phase3-analysis.md) | 커밋 분석 및 기능 추출 |
| 4 | [phase4-writing.md](scripts/docs/phase4-writing.md) | 릴리스 노트 작성 규칙 |
| 5 | [phase5-validation.md](scripts/docs/phase5-validation.md) | 검증 및 품질 확인 |

상세 내용: [scripts/docs/README.md](scripts/docs/README.md)

### 스크립트 사용법

```bash
# scripts 폴더로 이동
cd .release-notes/scripts

# 컴포넌트 분석 실행
dotnet AnalyzeAllComponents.cs

# API 변경사항 추출
dotnet ExtractApiChanges.cs

# 릴리스 노트 크기 검증 (GitHub Release body 제한: 125,000자)
cd ..
.\validate-release-notes.ps1 -FilePath "RELEASE-v1.0.0-alpha.1.md"
```

## 핵심 원칙

### 정확성 우선

- 모든 문서화된 API는 분석 결과에 존재해야 합니다
- API를 임의로 만들어내지 않습니다
- 모든 기능은 커밋/PR로 추적 가능해야 합니다
- 코드 샘플은 `all-api-changes.txt`에서 검증합니다

### Breaking Changes 감지

- `.api` 폴더의 Git diff 분석을 우선합니다
- 커밋 메시지 패턴(`!:`, `breaking`)은 보조 수단입니다
- 삭제/변경된 API는 모두 Breaking Change로 처리합니다

### 문서 언어

- `RELEASE-v{VERSION}.md`: 영문 릴리스 노트 (GitHub Release 기본)
- `RELEASE-v{VERSION}-KR.md`: 한글 릴리스 노트 (한국어 사용자용)

## 미디어 파일

릴리스 노트에 포함되는 미디어 파일:

- **MP4**: 릴리스 소개 영상 (주요 기능 데모)
- **M4A**: 릴리스 소개 음성 (팟캐스트 형식 설명)

## 참고 자료

- [Conventional Commits](https://www.conventionalcommits.org/)
- [Keep a Changelog](https://keepachangelog.com/)
- [Semantic Versioning](https://semver.org/)
