#!/usr/bin/env pwsh
#Requires -Version 7.0

# OpenSearch Dashboard 시작 스크립트

Write-Host "=== OpenSearch Dashboard 시작 ===" -ForegroundColor Green
Write-Host ""

# Dashboard 디렉토리로 이동
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Docker Compose로 대시보드 시작
Write-Host "Docker Compose로 OpenSearch 스택을 시작합니다..." -ForegroundColor Cyan
Write-Host ""
Write-Host "다음 서비스가 시작됩니다:" -ForegroundColor Yellow
Write-Host "  - OpenSearch (검색 엔진)" -ForegroundColor White
Write-Host "  - OpenSearch Dashboards (UI)" -ForegroundColor White
Write-Host "  - Data Prepper (OTLP 수신기)" -ForegroundColor White
Write-Host "  - Fluent-bit (로그 수집기)" -ForegroundColor White
Write-Host ""

docker compose up -d

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "OpenSearch 스택이 시작되었습니다!" -ForegroundColor Green
    Write-Host ""
    Write-Host "서비스 초기화를 기다리는 중... (약 60초)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "접속 정보:" -ForegroundColor Yellow
    Write-Host "  - OpenSearch Dashboards: http://localhost:5601" -ForegroundColor White
    Write-Host "  - OpenSearch API:        http://localhost:9200" -ForegroundColor White
    Write-Host ""
    Write-Host "Data Prepper (OTLP 수신):" -ForegroundColor Yellow
    Write-Host "  - Logs:    http://localhost:2021" -ForegroundColor White
    Write-Host "  - Metrics: http://localhost:2023" -ForegroundColor White
    Write-Host "  - Traces:  http://localhost:21890" -ForegroundColor White
    Write-Host ""
    Write-Host "상태 확인: docker compose ps" -ForegroundColor Gray
    Write-Host "로그 확인: docker compose logs -f" -ForegroundColor Gray
    Write-Host "중지:      ./stop-dashboard.ps1" -ForegroundColor Gray
    Write-Host ""

    # 서비스 상태 확인
    Write-Host "서비스 상태 확인 중..." -ForegroundColor Cyan
    $maxRetries = 30
    $retryCount = 0
    $ready = $false

    while (-not $ready -and $retryCount -lt $maxRetries) {
        Start-Sleep -Seconds 5
        $retryCount++

        try {
            $response = Invoke-WebRequest -Uri "http://localhost:5601/api/status" -UseBasicParsing -TimeoutSec 5 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                $ready = $true
            }
        } catch {
            Write-Host "  대기 중... ($retryCount/$maxRetries)" -ForegroundColor DarkGray
        }
    }

    if ($ready) {
        Write-Host ""
        Write-Host "OpenSearch Dashboards가 준비되었습니다!" -ForegroundColor Green
        Write-Host "브라우저를 열어 http://localhost:5601 에 접속합니다..." -ForegroundColor Cyan
        Start-Sleep -Seconds 2
        Start-Process "http://localhost:5601"
    } else {
        Write-Host ""
        Write-Host "서비스 시작에 시간이 걸리고 있습니다." -ForegroundColor Yellow
        Write-Host "잠시 후 http://localhost:5601 에 접속해주세요." -ForegroundColor Yellow
        Write-Host "상태 확인: docker compose ps" -ForegroundColor Gray
    }
} else {
    Write-Host ""
    Write-Host "OpenSearch 스택 시작 중 오류가 발생했습니다." -ForegroundColor Red
    Write-Host "Docker가 실행 중인지 확인하세요." -ForegroundColor Yellow
    exit 1
}
