#requires -Version 7.0

<#
.SYNOPSIS
    OrderService와 InventoryService 간 메시징 테스트 (간단 버전)

.DESCRIPTION
    Docker Compose로 RabbitMQ를 시작하고, 두 서비스를 순차적으로 실행하여
    메시지가 정상적으로 주고받히는지 확인합니다.
#>

Write-Host "=== CqrsObservability 메시징 테스트 ===" -ForegroundColor Cyan
Write-Host ""

# 1. Docker Compose로 RabbitMQ 시작
Write-Host "[1/3] RabbitMQ 컨테이너 시작 중..." -ForegroundColor Yellow
$composeFile = "Tutorials/CqrsObservability/docker-compose.yml"
docker-compose -f $composeFile up -d | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] RabbitMQ 컨테이너 시작 실패" -ForegroundColor Red
    exit 1
}

# RabbitMQ가 준비될 때까지 대기
Write-Host "[2/3] RabbitMQ 준비 대기 중 (5초)..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# 2. InventoryService 시작 (백그라운드)
Write-Host "[3/3] InventoryService 시작 중..." -ForegroundColor Yellow
$inventoryJob = Start-Job -ScriptBlock {
    Set-Location "E:\2025\Dev\DDD-Course\2025-12-30_Functorium\Tutorials\CqrsObservability\Src\InventoryService"
    dotnet run 2>&1
}

# InventoryService가 시작될 때까지 대기
Start-Sleep -Seconds 3

# 3. OrderService 시작 (데모 시나리오 자동 실행 후 종료)
Write-Host ""
Write-Host "OrderService 시작 중 (데모 시나리오 자동 실행)..." -ForegroundColor Yellow
Write-Host ""

Set-Location "Tutorials/CqrsObservability/Src/OrderService"
dotnet run 2>&1

# 4. InventoryService 로그 확인
Write-Host ""
Write-Host "=== InventoryService 로그 ===" -ForegroundColor Cyan
Receive-Job -Id $inventoryJob.Id -Keep | Select-Object -Last 30

# 5. 정리
Write-Host ""
Write-Host "=== 테스트 완료 ===" -ForegroundColor Cyan
Stop-Job -Id $inventoryJob.Id -ErrorAction SilentlyContinue
Remove-Job -Id $inventoryJob.Id -ErrorAction SilentlyContinue

