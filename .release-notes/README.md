# Functorium Release Notes

이 폴더는 Functorium 프로젝트의 릴리스 노트 문서와 생성 도구를 포함합니다.

## 폴더 구조

```
.release-notes/
├── README.md                           # 이 문서
├── TEMPLATE.md                         # 릴리스 노트 작성 템플릿
├── validate-release-notes.ps1          # 릴리스 노트 크기 검증 스크립트
├── v1/                                 # v1.x 릴리스 노트
│   ├── v1.0.0-alpha.1/
│   │   ├── RELEASE-v1.0.0-alpha.1.md      # 영문
│   │   ├── RELEASE-v1.0.0-alpha.1-KR.md   # 한글
│   │   ├── RELEASE-v1.0.0-alpha.1-KR.mp4  # 소개 영상
│   │   └── RELEASE-v1.0.0-alpha.1-KR.mp3  # 소개 음성
│   ├── v1.0.0-alpha.2/
│   │   ├── RELEASE-v1.0.0-alpha.2.md
│   │   ├── RELEASE-v1.0.0-alpha.2-KR.md
│   │   ├── RELEASE-v1.0.0-alpha.2-KR.mp4
│   │   ├── RELEASE-v1.0.0-alpha.2-KR.m4a
│   │   ├── RELEASE-v1.0.0-alpha.2-KR.pdf
│   │   └── RELEASE-v1.0.0-alpha.2-KR.pptx
│   └── v1.0.0-alpha.3/
│       ├── RELEASE-v1.0.0-alpha.3.md
│       └── RELEASE-v1.0.0-alpha.3-KR.md
└── scripts/                            # 릴리스 노트 생성 스크립트
    ├── AnalyzeAllComponents.cs         # 컴포넌트 변경사항 분석
    ├── AnalyzeFolder.cs                # 개별 폴더 상세 분석
    ├── ExtractApiChanges.cs            # API 변경사항 추출
    ├── ApiGenerator.cs                 # Public API 생성
    ├── config/                         # 분석 설정 파일
    ├── docs/                           # 5-Phase 워크플로우 문서
    └── .analysis-output/               # 분석 결과 출력 (gitignore)
```

## 릴리스 노트 목록

| 버전 | 날짜 | 문서 | 미디어 | 주요 변경 |
|------|------|------|--------|----------|
| v1.0.0-alpha.3 | 2026-04-19 | [EN](v1/v1.0.0-alpha.3/RELEASE-v1.0.0-alpha.3.md) / [KR](v1/v1.0.0-alpha.3/RELEASE-v1.0.0-alpha.3-KR.md) | — | IRepository 재설계, EF Core 성능 최적화, [GenerateSetters] Source Generator |
| v1.0.0-alpha.2 | 2026-03-28 | [EN](v1/v1.0.0-alpha.2/RELEASE-v1.0.0-alpha.2.md) / [KR](v1/v1.0.0-alpha.2/RELEASE-v1.0.0-alpha.2-KR.md) | [MP4](v1/v1.0.0-alpha.2/RELEASE-v1.0.0-alpha.2-KR.mp4) | ErrorType partial 분리, Pipeline opt-in, 폴더 재구성 |
| v1.0.0-alpha.1 | 2026-03-15 | [EN](v1/v1.0.0-alpha.1/RELEASE-v1.0.0-alpha.1.md) / [KR](v1/v1.0.0-alpha.1/RELEASE-v1.0.0-alpha.1-KR.md) | [MP4](v1/v1.0.0-alpha.1/RELEASE-v1.0.0-alpha.1-KR.mp4) / [MP3](v1/v1.0.0-alpha.1/RELEASE-v1.0.0-alpha.1-KR.mp3) | 첫 번째 알파 릴리스 |

## 릴리스 노트 작성 가이드

### 빠른 시작

1. **템플릿 복사**: `TEMPLATE.md`를 `v{MAJOR}/v{VERSION}/RELEASE-v{VERSION}.md`로 복사
2. **데이터 수집**: `scripts/` 폴더의 분석 스크립트 실행
3. **내용 작성**: 5-Phase 워크플로우에 따라 작성
4. **검증**: `validate-release-notes.ps1`로 크기 검증

### 5-Phase 워크플로우

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
dotnet AnalyzeAllComponents.cs --base v1.0.0-alpha.2 --target HEAD

# API 변경사항 추출
dotnet ExtractApiChanges.cs

# 릴리스 노트 크기 검증 (GitHub Release body 제한: 125,000자)
cd ..
powershell.exe -File validate-release-notes.ps1 -FilePath "v1/v1.0.0-alpha.3/RELEASE-v1.0.0-alpha.3.md"
```

## 핵심 원칙

### 정확성 우선

- 모든 문서화된 API는 분석 결과에 존재해야 합니다
- API를 임의로 만들어내지 않습니다
- 모든 기능은 커밋/PR로 추적 가능해야 합니다
- 코드 예제는 `all-api-changes.txt`에서 검증합니다

### Breaking Changes 감지

- `.api` 폴더의 Git diff 분석을 우선합니다
- 커밋 메시지 패턴(`!:`, `breaking`)은 보조 수단입니다
- 삭제/변경된 API는 모두 Breaking Change로 처리합니다

### 버전별 폴더 구조

릴리스 노트는 메이저 버전별로 폴더를 분리합니다:

```
v1/v1.0.0-alpha.1/RELEASE-v1.0.0-alpha.1.md     # v1.x 프리릴리스
v1/v1.0.0/RELEASE-v1.0.0.md                     # v1.0.0 정식
v2/v2.0.0-beta.1/RELEASE-v2.0.0-beta.1.md       # v2.x 프리릴리스
```

### 문서 언어

- `RELEASE-v{VERSION}.md`: 영문 릴리스 노트 (GitHub Release 기본)
- `RELEASE-v{VERSION}-KR.md`: 한글 릴리스 노트 (한국어 사용자용)

## 미디어 파일

릴리스 노트에 포함되는 미디어 파일:

| 확장자 | 용도 |
|--------|------|
| `.mp4` | 릴리스 소개 영상 (주요 기능 데모) |
| `.mp3` / `.m4a` | 릴리스 소개 음성 (팟캐스트 형식) |
| `.pdf` | 릴리스 소개 슬라이드 (PDF) |
| `.pptx` | 릴리스 소개 슬라이드 (원본) |

## 참고 자료

- [Conventional Commits](https://www.conventionalcommits.org/)
- [Keep a Changelog](https://keepachangelog.com/)
- [Semantic Versioning](https://semver.org/)
