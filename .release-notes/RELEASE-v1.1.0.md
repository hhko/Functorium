# Release Notes - v1.1.0

**Release Date**: 2025-12-11

## Overview

이번 릴리스에서는 Observability 기능이 새롭게 추가되어 OpenTelemetry와 Serilog를 통한 애플리케이션 모니터링이 가능해졌습니다. VSCode 개발 환경 지원이 대폭 강화되어 복수 프로젝트 동시 실행, 단축키 설정, 자동화 스크립트 등 개발 생산성을 높이는 기능들이 추가되었습니다. 또한 빌드 시스템과 NuGet 패키지 생성 기능이 개선되었습니다.

---

## New Features

- **build**: NuGet 패키지 생성 단계가 빌드 스크립트에 추가되었습니다
- **build**: 빌드 스크립트 및 MinVer 버전 관리 설정이 개선되었습니다
- **build**: 버전 정보 표시가 개선되었습니다
- **claude**: 버전 제안 명령어가 추가되고 커밋 명령어가 개선되었습니다
- **claude**: suggest-next-version 명령어에 파라미터 유효성 검사가 추가되었습니다
- **claude**: Major 버전 자동 증가 기능이 비활성화되었습니다
- **functorium**: 핵심 라이브러리 패키지 참조 및 소스 구조가 추가되었습니다
- **lang-ext**: LanguageExt 5.0.0-beta-58로 업그레이드되었습니다
- **observability**: OpenTelemetry 및 Serilog 통합 구성이 추가되었습니다
- **observability**: OpenTelemetry 의존성 등록 확장 메서드가 추가되었습니다
- **observability**: Observability 예제 프로젝트가 추가되었습니다
- **script**: 커밋 요약 스크립트 기능이 개선되었습니다
- **testing**: 테스트 헬퍼 라이브러리 소스 구조가 추가되었습니다
- **vscode**: 프로젝트 설정 자동 추가 스크립트가 추가되었습니다
- **vscode**: Add-VscodeProject 스크립트에 keybindings.json 업데이트가 추가되었습니다
- **vscode**: keybindings에 publish 단축키가 추가되었습니다
- **vscode**: 프로젝트 설정 제거 스크립트가 추가되었습니다
- **vscode**: 복수 프로젝트 및 compounds 동시 실행이 지원됩니다

## Bug Fixes

- **build**: NuGet 패키지 아이콘 경로 문제가 수정되었습니다
- **build**: dotnet 명령 출력이 실시간으로 스트리밍되도록 수정되었습니다
- **build**: 브랜치 커버리지 계산 방식이 수정되었습니다

---

**Full Changelog**: v1.0.0...v1.1.0
