using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using LayeredArch.Application.Usecases.Customers.Ports;
using LayeredArch.Domain.AggregateRoots.Customers;
using static Functorium.Adapters.Errors.AdapterErrorKind;

using LayeredArch.Adapters.Persistence.Repositories.Customers.Repositories;

namespace LayeredArch.Adapters.Persistence.Repositories.Customers.Queries;

/// <summary>
/// InMemory 기반 Customer 단건 조회 읽기 전용 어댑터.
/// CustomerRepositoryInMemory의 정적 저장소에서 데이터를 가져온 후 DTO로 프로젝션합니다.
/// </summary>
[GenerateObservablePort]
public class CustomerDetailQueryInMemory : ICustomerDetailQuery
{
    public string RequestCategory => "QueryAdapter";

    public virtual FinT<IO, CustomerDetailDto> GetById(CustomerId id)
    {
        return IO.lift(() =>
        {
            if (CustomerRepositoryInMemory.Customers.TryGetValue(id, out var customer))
            {
                return Fin.Succ(new CustomerDetailDto(
                    customer.Id.ToString(),
                    customer.Name,
                    customer.Email,
                    customer.CreditLimit,
                    customer.CreatedAt));
            }

            return AdapterError.For<CustomerDetailQueryInMemory>(
                new NotFound(),
                id.ToString(),
                $"고객 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }
}
