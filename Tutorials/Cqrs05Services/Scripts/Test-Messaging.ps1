#requires -Version 7.0

<#
.SYNOPSIS
    OrderService와 InventoryService 간 메시징 테스트

.DESCRIPTION
    Docker Compose로 RabbitMQ를 시작하고, 두 서비스를 실행하여
    메시지가 정상적으로 주고받히는지 확인합니다.
#>

Write-Host "=== Cqrs05Services 메시징 테스트 ===" -ForegroundColor Cyan
Write-Host ""

# 1. Docker Compose로 RabbitMQ 시작
Write-Host "[1/4] RabbitMQ 컨테이너 시작 중..." -ForegroundColor Yellow
$composeFile = "Tutorials/Cqrs05Services/docker-compose.yml"
docker-compose -f $composeFile up -d
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] RabbitMQ 컨테이너 시작 실패" -ForegroundColor Red
    exit 1
}

# RabbitMQ가 준비될 때까지 대기
Write-Host "[2/4] RabbitMQ 준비 대기 중..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# 2. InventoryService 시작 (백그라운드)
Write-Host "[3/4] InventoryService 시작 중..." -ForegroundColor Yellow
$inventoryServiceJob = Start-Job -ScriptBlock {
    Set-Location $using:PWD
    Set-Location "Tutorials/Cqrs05Services/Src/InventoryService"
    dotnet run 2>&1
}

# InventoryService가 시작될 때까지 대기
Start-Sleep -Seconds 5

# 3. OrderService 시작 (데모 시나리오 자동 실행)
Write-Host "[4/4] OrderService 시작 중 (데모 시나리오 자동 실행)..." -ForegroundColor Yellow
Set-Location "Tutorials/Cqrs05Services/Src/OrderService"
dotnet run

# 4. 정리
Write-Host ""
Write-Host "=== 테스트 완료 ===" -ForegroundColor Cyan
Write-Host "InventoryService 로그를 확인하려면:" -ForegroundColor Yellow
Write-Host "  Receive-Job -Id $($inventoryServiceJob.Id) -Keep" -ForegroundColor Gray

# InventoryService 작업 종료
Stop-Job -Id $inventoryServiceJob.Id
Remove-Job -Id $inventoryServiceJob.Id

