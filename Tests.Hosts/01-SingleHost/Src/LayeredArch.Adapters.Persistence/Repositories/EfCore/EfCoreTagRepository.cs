using LayeredArch.Domain.AggregateRoots.Tags;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using Microsoft.EntityFrameworkCore;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore;

/// <summary>
/// EF Core 기반 태그 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class EfCoreTagRepository : ITagRepository
{
    private readonly LayeredArchDbContext _dbContext;
    private readonly IDomainEventCollector _eventCollector;

    public string RequestCategory => "Repository";

    public EfCoreTagRepository(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
    {
        _dbContext = dbContext;
        _eventCollector = eventCollector;
    }

    public virtual FinT<IO, Tag> Create(Tag tag)
    {
        return IO.liftAsync(async () =>
        {
            _dbContext.Tags.Add(tag.ToModel());
            _eventCollector.Track(tag);
            return Fin.Succ(tag);
        });
    }

    public virtual FinT<IO, Tag> GetById(TagId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await _dbContext.Tags.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id.ToString());
            if (model is not null)
            {
                return Fin.Succ(model.ToDomain());
            }

            return AdapterError.For<EfCoreTagRepository>(
                new NotFound(),
                id.ToString(),
                $"태그 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, Tag> Update(Tag tag)
    {
        return IO.lift(() =>
        {
            _dbContext.Tags.Update(tag.ToModel());
            _eventCollector.Track(tag);
            return Fin.Succ(tag);
        });
    }

    public virtual FinT<IO, Unit> Delete(TagId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await _dbContext.Tags.FindAsync(id.ToString());
            if (model is null)
            {
                return AdapterError.For<EfCoreTagRepository>(
                    new NotFound(),
                    id.ToString(),
                    $"태그 ID '{id}'을(를) 찾을 수 없습니다");
            }

            _dbContext.Tags.Remove(model);
            return Fin.Succ(unit);
        });
    }

    public virtual FinT<IO, Seq<Tag>> CreateRange(IReadOnlyList<Tag> tags)
    {
        return IO.liftAsync(async () =>
        {
            _dbContext.Tags.AddRange(tags.Select(t => t.ToModel()));
            _eventCollector.TrackRange(tags);
            return Fin.Succ(toSeq(tags));
        });
    }

    public virtual FinT<IO, Seq<Tag>> GetByIds(IReadOnlyList<TagId> ids)
    {
        return IO.liftAsync(async () =>
        {
            var idStrings = ids.Select(id => id.ToString()).ToList();
            var models = await _dbContext.Tags.AsNoTracking()
                .Where(t => idStrings.Contains(t.Id))
                .ToListAsync();
            return Fin.Succ(toSeq(models.Select(m => m.ToDomain())));
        });
    }

    public virtual FinT<IO, Seq<Tag>> UpdateRange(IReadOnlyList<Tag> tags)
    {
        return IO.lift(() =>
        {
            _dbContext.Tags.UpdateRange(tags.Select(t => t.ToModel()));
            _eventCollector.TrackRange(tags);
            return Fin.Succ(toSeq(tags));
        });
    }

    public virtual FinT<IO, Unit> DeleteRange(IReadOnlyList<TagId> ids)
    {
        return IO.liftAsync(async () =>
        {
            var idStrings = ids.Select(id => id.ToString()).ToList();
            await _dbContext.Tags
                .Where(t => idStrings.Contains(t.Id))
                .ExecuteDeleteAsync();
            return Fin.Succ(unit);
        });
    }
}
