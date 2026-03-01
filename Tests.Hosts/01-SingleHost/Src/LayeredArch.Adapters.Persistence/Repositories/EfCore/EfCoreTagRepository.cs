using LayeredArch.Domain.AggregateRoots.Tags;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore;

/// <summary>
/// EF Core 기반 태그 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class EfCoreTagRepository
    : EfCoreRepositoryBase<Tag, TagId, TagModel>, ITagRepository
{
    private readonly LayeredArchDbContext _dbContext;

    public EfCoreTagRepository(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector)
        => _dbContext = dbContext;

    // ─── 필수 선언 ───────────────────────────────────

    protected override DbSet<TagModel> DbSet => _dbContext.Tags;

    protected override Tag ToDomain(TagModel model) => model.ToDomain();
    protected override TagModel ToModel(Tag tag) => tag.ToModel();
}
