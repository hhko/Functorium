# Aspire Dashboard 설정 가이드

이 문서는 .NET Aspire Dashboard를 Docker Compose를 사용하여 실행하는 방법을 설명합니다.

## 개요

Aspire Dashboard는 OpenTelemetry 원격 분석 데이터(로그, 메트릭, 트레이스)를 시각화하는 도구입니다.

## 포트 구성

| 포트 | 용도 | 설명 |
|------|------|------|
| 18888 | 프론트엔드 UI | 브라우저로 대시보드에 접속하는 포트 |
| 18889 | OTLP/gRPC | gRPC를 통한 원격 분석 데이터 수신 |
| 18890 | OTLP/HTTP | HTTP를 통한 원격 분석 데이터 수신 (Protobuf) |

## 시작하기

### 1. Aspire Dashboard 실행

```bash
cd Dashboard
docker compose up -d
```

### 2. 대시보드 접속

브라우저에서 다음 주소로 접속:
```
http://localhost:18888
```

### 3. 상태 확인

```bash
# 컨테이너 로그 확인
docker compose logs -f aspire-dashboard

# 컨테이너 상태 확인
docker compose ps
```

## 애플리케이션 설정

애플리케이션에서 Aspire Dashboard로 원격 분석을 전송하려면 `appsettings.json`에 다음과 같이 설정:

```json
{
  "OpenTelemetry": {
    "ServiceName": "YourServiceName",
    "OtlpCollectorHost": "http://127.0.0.1:18889"
  }
}
```

## 환경 변수 설명

| 환경 변수 | 기본값 | 설명 |
|-----------|--------|------|
| `ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS` | false | 인증 없이 대시보드 접근 허용 (개발용) |
| `DASHBOARD__TELEMETRYLIMITS__MAXLOGCOUNT` | 10,000 | 저장할 최대 로그 수 |
| `DASHBOARD__TELEMETRYLIMITS__MAXTRACECOUNT` | 10,000 | 저장할 최대 트레이스 수 |
| `DASHBOARD__TELEMETRYLIMITS__MAXMETRICSCOUNT` | 50,000 | 저장할 최대 메트릭 수 |
| `DASHBOARD__APPLICATIONNAME` | Aspire | 대시보드에 표시될 애플리케이션 이름 |

## 중지 및 제거

```bash
# 중지
docker compose stop

# 중지 및 컨테이너 제거
docker compose down

# 중지, 컨테이너 및 네트워크 제거
docker compose down -v
```

## 보안 주의사항

⚠️ **중요**: 현재 설정은 `ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true`로 인증 없이 구성되어 있습니다.

이 설정은 **개발 환경에서만** 사용해야 하며, 프로덕션 환경에서는 다음 중 하나를 사용해야 합니다:
- API 키 인증
- 클라이언트 인증서 인증
- Azure AD 통합

자세한 내용은 [Microsoft Learn 문서](https://learn.microsoft.com/ko-kr/dotnet/aspire/fundamentals/dashboard/configuration)를 참조하세요.

## 참고 자료

- [Aspire Dashboard 공식 문서](https://learn.microsoft.com/ko-kr/dotnet/aspire/fundamentals/dashboard/overview)
- [Dashboard 구성 가이드](https://learn.microsoft.com/ko-kr/dotnet/aspire/fundamentals/dashboard/configuration)
- [OpenTelemetry 문서](https://opentelemetry.io/docs/)

