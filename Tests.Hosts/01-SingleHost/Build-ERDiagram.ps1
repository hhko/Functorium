#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
  SingleHost EF Core DbContext 기반 Mermaid ER 다이어그램을 생성합니다.

.DESCRIPTION
  LayeredArchDbContext의 Entity 구성을 기반으로 Mermaid ER 다이어그램을
  ER-Diagram.md 파일로 생성합니다.

  참고: Siren 도구는 Migration 필수(어셈블리 모드) 또는 SQL Server 전용(연결 문자열 모드)
  제약이 있어, 이 스크립트는 EF Core Configuration을 기반으로 직접 생성합니다.

.PARAMETER Help
  도움말을 표시합니다.

.EXAMPLE
  ./Build-ERDiagram.ps1
  ER-Diagram.md를 생성합니다.

.NOTES
  Version: 1.0.0
  Requirements: PowerShell 7+
  License: MIT

  스키마 변경 시 이 스크립트의 $erDiagram 변수를 업데이트하세요.
  EF Core Configuration 파일 위치:
    Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/Configurations/
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory = $false, HelpMessage = "도움말 표시")]
  [Alias("h", "?")]
  [switch]$Help
)

# Strict mode settings
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Set console encoding to UTF-8 for proper Korean character display
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# Load common modules
$repoRoot = (Resolve-Path "$PSScriptRoot/../..").Path
. "$repoRoot/.scripts/Write-Console.ps1"

#region Constants

$script:OUTPUT_FILE = "$PSScriptRoot/ER-Diagram.md"

#endregion

#region Helper Functions

function Show-Help {
  $help = @"

================================================================================
 SingleHost ER Diagram Generator
================================================================================

DESCRIPTION
  Generate Mermaid ER diagram from SingleHost EF Core DbContext configuration.
  Output: ER-Diagram.md

USAGE
  ./Build-ERDiagram.ps1 [options]

OPTIONS
  -Help, -h, -?      Show this help message

NOTE
  Schema changes require updating the diagram template in this script.
  Reference: Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/Configurations/

================================================================================
"@
  Write-Host $help
}

#endregion

#region ER Diagram Template

# LayeredArchDbContext EF Core Configuration 기반
# 변경 시 아래 Configuration 파일을 참조하여 업데이트:
#   - ProductConfiguration.cs
#   - CustomerConfiguration.cs
#   - OrderConfiguration.cs
#   - TagConfiguration.cs
$erDiagram = @'
# SingleHost ER Diagram

> EF Core `LayeredArchDbContext` 기반 Entity-Relationship 다이어그램

```mermaid
erDiagram
    Products {
        string Id PK "ProductId (Ulid, 26자)"
        string Name "ProductName (max 100)"
        string Description "ProductDescription (max 1000)"
        decimal Price "Money (precision 18,4)"
        int StockQuantity "Quantity"
        datetime CreatedAt
        datetime UpdatedAt "nullable"
    }

    Customers {
        string Id PK "CustomerId (Ulid, 26자)"
        string Name "CustomerName (max 100)"
        string Email "Email (max 320)"
        decimal CreditLimit "Money (precision 18,4)"
        datetime CreatedAt
        datetime UpdatedAt "nullable"
    }

    Orders {
        string Id PK "OrderId (Ulid, 26자)"
        string ProductId FK "ProductId (Ulid, 26자)"
        int Quantity "Quantity"
        decimal UnitPrice "Money (precision 18,4)"
        decimal TotalAmount "Money (precision 18,4)"
        string ShippingAddress "ShippingAddress (max 500)"
        datetime CreatedAt
        datetime UpdatedAt "nullable"
    }

    Tags {
        string Id PK "TagId (Ulid, 26자)"
        string ProductId FK "shadow property"
        string Name "TagName (max 50)"
    }

    Products ||--o{ Tags : "HasMany (Cascade Delete)"
    Products ||--o{ Orders : "referenced by ProductId"
```

## Entity 설명

| 테이블 | Aggregate | 설명 |
|--------|-----------|------|
| `Products` | `Product` (Aggregate Root) | 상품 정보. Tag 컬렉션 소유 |
| `Customers` | `Customer` (Aggregate Root) | 고객 정보 |
| `Orders` | `Order` (Aggregate Root) | 주문. ProductId로 상품 참조 (Navigation 없음) |
| `Tags` | `Tag` (Entity) | 태그. Product가 소유하는 하위 Entity |

## 공유 Value Object

| Value Object | 저장 타입 | 사용 위치 |
|-------------|----------|----------|
| `Money` | `decimal(18,4)` | Product.Price, Customer.CreditLimit, Order.UnitPrice, Order.TotalAmount |
| `Quantity` | `int` | Product.StockQuantity, Order.Quantity |

## 관계

- **Products → Tags**: 1:N 소유 관계. `ProductId` shadow FK, Cascade Delete
- **Products → Orders**: 1:N 참조 관계. `ProductId` FK, Navigation property 없음 (Cross-Aggregate 참조)
'@

#endregion

#region Main

function Main {
  Write-StartMessage -Title "ER Diagram Generation..."

  Set-Content -Path $script:OUTPUT_FILE -Value $erDiagram -Encoding UTF8

  $lineCount = (Get-Content $script:OUTPUT_FILE | Measure-Object -Line).Lines
  Write-Success "Generated: ER-Diagram.md ($lineCount lines)"

  Write-DoneMessage -Title "ER diagram generation completed"
}

#endregion

#region Entry Point

if ($Help) {
  Show-Help
  exit 0
}

try {
  Main
  exit 0
}
catch {
  Write-ErrorMessage -ErrorRecord $_
  exit 1
}

#endregion
