#!/usr/bin/env pwsh
#Requires -Version 7.0

# Aspire Dashboard 중지 스크립트

Write-Host "=== Aspire Dashboard 중지 ===" -ForegroundColor Yellow
Write-Host ""

# Dashboard 디렉토리로 이동
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Docker Compose로 대시보드 중지
Write-Host "Aspire Dashboard를 중지합니다..." -ForegroundColor Cyan
docker-compose down -v

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Aspire Dashboard가 성공적으로 중지되었습니다!" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "Aspire Dashboard 중지 중 오류가 발생했습니다." -ForegroundColor Red
    exit 1
}

