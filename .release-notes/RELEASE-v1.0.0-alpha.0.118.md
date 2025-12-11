# Release Notes - v1.0.0-alpha.0.118

**Release Date**: 2025-12-11

## Overview

이번 릴리스에서는 Observability 기능이 새롭게 추가되어 OpenTelemetry와 Serilog를 통한 애플리케이션 모니터링이 가능해졌습니다. VSCode 개발 환경 지원이 대폭 강화되어 복수 프로젝트 동시 실행, 단축키 설정, 프로젝트 자동 추가/제거 스크립트 등 개발 생산성을 높이는 기능들이 추가되었습니다. 또한 빌드 시스템과 NuGet 패키지 생성 기능이 개선되었으며, LanguageExt 5.0 베타 버전으로 업그레이드되었습니다.

---

## New Features

- **build**: 로컬 빌드 스크립트에 NuGet 패키지 생성 단계가 추가되어 패키지를 쉽게 생성할 수 있습니다
- **build**: 빌드 스크립트가 개선되어 버전 정보가 명확하게 표시되며, MinVer 설정이 최적화되었습니다
- **claude**: 릴리스 노트를 자동으로 생성하는 명령어가 추가되었습니다
- **claude**: 다음 버전을 제안하는 명령어가 추가되고 커밋 명령어가 개선되었습니다
- **claude**: 버전 제안 명령어에 파라미터 유효성 검사가 추가되어 더 안정적으로 동작합니다
- **claude**: Major 버전 자동 증가 기능이 비활성화되어 버전 관리가 더 명확해졌습니다
- **functorium**: 핵심 라이브러리의 패키지 참조 및 소스 구조가 추가되었습니다
- **lang-ext**: LanguageExt 라이브러리가 5.0.0-beta-58 버전으로 업그레이드되었습니다
- **observability**: OpenTelemetry 의존성을 쉽게 등록할 수 있는 확장 메서드가 추가되었습니다
- **observability**: OpenTelemetry와 Serilog가 통합 구성되어 애플리케이션 모니터링이 가능해졌습니다
- **observability**: Observability 기능을 활용한 예제 프로젝트가 추가되었습니다
- **script**: 커밋 요약 스크립트가 개선되어 더 유용한 정보를 제공합니다
- **testing**: 테스트를 위한 헬퍼 라이브러리 소스 구조가 추가되었습니다
- **vscode**: 복수 프로젝트와 compounds를 동시에 실행할 수 있는 기능이 추가되었습니다
- **vscode**: 프로젝트 설정을 자동으로 추가하는 스크립트가 작성되었습니다
- **vscode**: 단축키 설정에 keybindings.json 업데이트가 자동으로 추가됩니다
- **vscode**: Publish 작업을 위한 단축키가 추가되었습니다
- **vscode**: 프로젝트 설정을 제거하는 스크립트가 추가되었습니다

## Bug Fixes

- **build**: NuGet 패키지 아이콘 경로가 올바르게 수정되었습니다
- **build**: 브랜치 커버리지 계산 방식이 정확하게 수정되었습니다
- **build**: dotnet 명령 출력이 실시간으로 스트리밍되도록 수정되었습니다

---

**Full Changelog**: 6712dec...v1.0.0-alpha.0.118
