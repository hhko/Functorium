namespace LayeredArch.Domain.SharedKernel.Events;

/// <summary>
/// Aggregate에 태그가 할당될 때 발행되는 공유 도메인 이벤트
/// </summary>
public sealed record TagAssignedEvent(TagId TagId, TagName TagName) : DomainEvent;

/// <summary>
/// Aggregate에서 태그가 제거될 때 발행되는 공유 도메인 이벤트
/// </summary>
public sealed record TagRemovedEvent(TagId TagId) : DomainEvent;
