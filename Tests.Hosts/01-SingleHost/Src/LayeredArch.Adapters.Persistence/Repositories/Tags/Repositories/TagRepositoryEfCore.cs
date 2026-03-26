using LayeredArch.Domain.AggregateRoots.Tags;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;

namespace LayeredArch.Adapters.Persistence.Repositories.Tags.Repositories;

/// <summary>
/// EF Core 기반 태그 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class TagRepositoryEfCore
    : EfCoreRepositoryBase<Tag, TagId, TagModel>, ITagRepository
{
    private readonly LayeredArchDbContext _dbContext;

    public TagRepositoryEfCore(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector)
        => _dbContext = dbContext;

    // ─── 필수 선언 ───────────────────────────────────

    protected override DbContext DbContext => _dbContext;
    protected override DbSet<TagModel> DbSet => _dbContext.Tags;

    protected override Tag ToDomain(TagModel model) => model.ToDomain();
    protected override TagModel ToModel(Tag tag) => tag.ToModel();
}
