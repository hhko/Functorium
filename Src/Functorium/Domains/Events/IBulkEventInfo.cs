namespace Functorium.Domains.Events;

/// <summary>
/// 벌크(Bulk) 이벤트의 메타데이터를 제공하는 인터페이스.
/// ObservableDomainEventNotificationPublisher에서 벌크(Bulk) 이벤트 인식에 사용합니다.
/// </summary>
internal interface IBulkEventInfo
{
    int Count { get; }
    string InnerEventTypeName { get; }
}
