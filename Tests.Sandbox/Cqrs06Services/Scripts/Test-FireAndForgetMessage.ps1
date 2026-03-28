#requires -Version 7.0

<#
.SYNOPSIS
    OrderService에서 Fire and Forget 메시지 전송 테스트

.DESCRIPTION
    PowerShell 7.x를 사용하여 OrderService의 ReserveInventoryCommand 메시지를
    RabbitMQ를 통해 전송하고 테스트합니다.

.PARAMETER OrderId
    주문 ID (Guid)

.PARAMETER ProductId
    상품 ID (Guid)

.PARAMETER Quantity
    수량 (정수)

.EXAMPLE
    .\Test-FireAndForgetMessage.ps1 -OrderId "123e4567-e89b-12d3-a456-426614174000" -ProductId "223e4567-e89b-12d3-a456-426614174001" -Quantity 10
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$OrderId = (New-Guid).ToString(),
    
    [Parameter(Mandatory = $false)]
    [string]$ProductId = (New-Guid).ToString(),
    
    [Parameter(Mandatory = $false)]
    [int]$Quantity = 5
)

# RabbitMQ 연결 정보
$rabbitmqHost = "localhost"
$rabbitmqPort = 5672
$rabbitmqUser = "guest"
$rabbitmqPass = "guest"
$queueName = "inventory.reserve"

# RabbitMQ Management API를 통한 메시지 전송
$managementUrl = "http://localhost:15672/api/exchanges/%2F/amq.default/publish"
$credentials = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${rabbitmqUser}:${rabbitmqPass}"))

# 메시지 페이로드 생성 (Wolverine 형식)
$messagePayload = @{
    OrderId = $OrderId
    ProductId = $ProductId
    Quantity = $Quantity
} | ConvertTo-Json -Compress

$body = @{
    properties = @{
        delivery_mode = 2  # Persistent
        content_type = "application/json"
    }
    routing_key = $queueName
    payload = $messagePayload
    payload_encoding = "string"
} | ConvertTo-Json -Depth 10

try {
    Write-Host "=== Fire and Forget 메시지 전송 테스트 ===" -ForegroundColor Cyan
    Write-Host "OrderId: $OrderId" -ForegroundColor Yellow
    Write-Host "ProductId: $ProductId" -ForegroundColor Yellow
    Write-Host "Quantity: $Quantity" -ForegroundColor Yellow
    Write-Host "Queue: $queueName" -ForegroundColor Yellow
    Write-Host ""

    $headers = @{
        "Authorization" = "Basic $credentials"
        "Content-Type" = "application/json"
    }

    $response = Invoke-RestMethod -Uri $managementUrl -Method Post -Headers $headers -Body $body

    if ($response.routed) {
        Write-Host "[SUCCESS] 메시지가 성공적으로 전송되었습니다!" -ForegroundColor Green
        Write-Host "응답: $($response | ConvertTo-Json)" -ForegroundColor Gray
    } else {
        Write-Host "[WARNING] 메시지가 라우팅되지 않았습니다." -ForegroundColor Yellow
        Write-Host "응답: $($response | ConvertTo-Json)" -ForegroundColor Gray
    }
} catch {
    Write-Host "[ERROR] 메시지 전송 실패: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "상세: $($_.Exception)" -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "=== 테스트 완료 ===" -ForegroundColor Cyan
Write-Host "InventoryService 로그를 확인하여 메시지 수신 여부를 확인하세요." -ForegroundColor Yellow

