#!/usr/bin/env pwsh
#Requires -Version 7.0

# Aspire Dashboard 시작 스크립트

Write-Host "=== Aspire Dashboard 시작 ===" -ForegroundColor Green
Write-Host ""

# Dashboard 디렉토리로 이동
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Docker Compose로 대시보드 시작
Write-Host "Docker Compose로 Aspire Dashboard를 시작합니다..." -ForegroundColor Cyan
docker compose up -d

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Aspire Dashboard가 성공적으로 시작되었습니다!" -ForegroundColor Green
    Write-Host ""
    Write-Host "접속 정보:" -ForegroundColor Yellow
    Write-Host "  - 대시보드 UI: http://localhost:18888" -ForegroundColor White
    Write-Host "  - OTLP gRPC:   http://localhost:18889" -ForegroundColor White
    Write-Host "  - OTLP HTTP:   http://localhost:18890" -ForegroundColor White
    Write-Host ""
    Write-Host "로그 확인: docker-compose logs -f aspire-dashboard" -ForegroundColor Gray
    Write-Host "중지:      docker-compose stop" -ForegroundColor Gray
    Write-Host ""

    # 5초 후 브라우저 열기
    Write-Host "5초 후 브라우저가 자동으로 열립니다..." -ForegroundColor Cyan
    Start-Sleep -Seconds 5
    Start-Process "http://localhost:18888"
} else {
    Write-Host ""
    Write-Host "Aspire Dashboard 시작 중 오류가 발생했습니다." -ForegroundColor Red
    Write-Host "Docker가 실행 중인지 확인하세요." -ForegroundColor Yellow
    exit 1
}

