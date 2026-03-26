using Dapper;
using Functorium.Adapters.Repositories;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.AggregateRoots.Products.Specifications;

namespace LayeredArch.Adapters.Persistence.Repositories.Products;

/// <summary>
/// Product Specification → SQL WHERE 절 번역기.
/// 모든 Product 기반 Dapper 어댑터가 테이블 별칭만 달리하여 공유합니다.
/// </summary>
internal static class ProductSpecTranslator
{
    internal static readonly DapperSpecTranslator<Product> Instance = new DapperSpecTranslator<Product>()
        .WhenAll(alias =>
        {
            var p = DapperSpecTranslator<Product>.Prefix(alias);
            return ($"WHERE {p}DeletedAt IS NULL", new DynamicParameters());
        })
        .When<ProductPriceRangeSpec>((spec, alias) =>
        {
            var p = DapperSpecTranslator<Product>.Prefix(alias);
            return ($"WHERE {p}DeletedAt IS NULL AND {p}Price >= @MinPrice AND {p}Price <= @MaxPrice",
                DapperSpecTranslator<Product>.Params(
                    ("MinPrice", (decimal)spec.MinPrice),
                    ("MaxPrice", (decimal)spec.MaxPrice)));
        });
}
