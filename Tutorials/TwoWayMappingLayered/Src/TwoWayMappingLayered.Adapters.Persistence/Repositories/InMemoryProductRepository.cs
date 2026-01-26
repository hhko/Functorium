using System.Collections.Concurrent;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerator;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using TwoWayMappingLayered.Adapters.Persistence.Entities;
using TwoWayMappingLayered.Adapters.Persistence.Mappers;
using TwoWayMappingLayered.Domains.Entities;
using TwoWayMappingLayered.Domains.Repositories;
using TwoWayMappingLayered.Domains.ValueObjects;
using static Functorium.Adapters.Errors.AdapterErrorType;
using static LanguageExt.Prelude;

namespace TwoWayMappingLayered.Adapters.Persistence.Repositories;

/// <summary>
/// 메모리 기반 상품 리포지토리 (Two-Way Mapping 적용)
///
/// Two-Way Mapping 전략:
/// - 내부 저장소는 ProductEntity(Adapter 모델) 사용
/// - 외부 인터페이스는 Product(Domain 모델) 사용
/// - ProductMapper로 양방향 변환 수행
///
/// HappyCoders 문서 원문:
/// "In my experience, this variant is the most suitable."
///
/// 장점:
/// - 명확한 아키텍처 경계: Domain이 기술 의존성 없음
/// - 완전한 Domain 객체 반환: 비즈니스 메서드 즉시 사용 가능
/// </summary>
[GeneratePipeline]
public class InMemoryProductRepository : IProductRepository
{
    private readonly ILogger<InMemoryProductRepository> _logger;

    // 내부 저장소: Adapter 모델(ProductEntity) 사용
    private static readonly ConcurrentDictionary<Guid, ProductEntity> _products = new();

    public string RequestCategory => "Repository";

    public InMemoryProductRepository(ILogger<InMemoryProductRepository> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 상품 생성
    /// Two-Way Mapping: Domain → Adapter 변환 후 저장
    /// </summary>
    public virtual FinT<IO, Product> Create(Product product)
    {
        return IO.lift(() =>
        {
            // Domain → Adapter: ProductMapper.ToEntity()
            ProductEntity entity = ProductMapper.ToEntity(product);
            _products[entity.Id] = entity;

            _logger.LogDebug("Product created: {ProductId}", entity.Id);

            // 저장된 엔티티를 다시 Domain으로 변환하여 반환
            // (실제 DB에서는 ID 생성 등의 부수효과 반영)
            return Fin.Succ(product);
        });
    }

    /// <summary>
    /// ID로 상품 조회
    /// Two-Way Mapping: Adapter → Domain 변환 후 반환
    /// </summary>
    public virtual FinT<IO, Product> GetById(ProductId id)
    {
        return IO.lift(() =>
        {
            if (_products.TryGetValue((Guid)id, out ProductEntity? entity))
            {
                // Adapter → Domain: ProductMapper.ToDomain()
                Product product = ProductMapper.ToDomain(entity);
                return Fin.Succ(product);
            }

            return Fin.Fail<Product>(AdapterError.For<InMemoryProductRepository>(
                new NotFound(),
                ((Guid)id).ToString(),
                $"상품 ID '{(Guid)id}'을(를) 찾을 수 없습니다"));
        });
    }

    /// <summary>
    /// 모든 상품 조회
    /// Two-Way Mapping: 각 ProductEntity를 Product로 변환 후 반환
    /// </summary>
    public virtual FinT<IO, Seq<Product>> GetAll()
    {
        return IO.lift(() =>
        {
            // Adapter → Domain: 각 엔티티를 Domain 모델로 변환
            Seq<Product> products = toSeq(
                _products.Values.Select(ProductMapper.ToDomain));

            return Fin.Succ(products);
        });
    }

    /// <summary>
    /// 상품 업데이트
    /// Two-Way Mapping: 기존 엔티티를 Domain 값으로 업데이트
    /// </summary>
    public virtual FinT<IO, Product> Update(Product product)
    {
        return IO.lift(() =>
        {
            if (!_products.TryGetValue((Guid)product.Id, out ProductEntity? entity))
            {
                return Fin.Fail<Product>(AdapterError.For<InMemoryProductRepository>(
                    new NotFound(),
                    ((Guid)product.Id).ToString(),
                    $"상품 ID '{(Guid)product.Id}'을(를) 찾을 수 없습니다"));
            }

            // Domain → Adapter: 기존 엔티티 업데이트
            ProductMapper.UpdateEntity(entity, product);

            _logger.LogDebug("Product updated: {ProductId}", entity.Id);

            return Fin.Succ(product);
        });
    }

    /// <summary>
    /// 상품명 중복 확인
    /// </summary>
    public virtual FinT<IO, bool> ExistsByName(string name)
    {
        return IO.lift(() =>
        {
            bool exists = _products.Values.Any(p =>
                p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return Fin.Succ(exists);
        });
    }
}
