using CustomerManagement.Domain;
using CustomerManagement.Domain.ValueObjects;

namespace CustomerManagement;

public static class SampleCustomers
{
    public static readonly Customer 김철수 = new(
        CustomerId.New(), new CustomerName("김철수"), new Email("chulsoo@example.com"), IsActive: true);

    public static readonly Customer 이영희 = new(
        CustomerId.New(), new CustomerName("이영희"), new Email("younghee@example.com"), IsActive: true);

    public static readonly Customer 박지민 = new(
        CustomerId.New(), new CustomerName("박지민"), new Email("jimin@example.com"), IsActive: false);

    public static readonly Customer 최수진 = new(
        CustomerId.New(), new CustomerName("최수진"), new Email("soojin@company.co.kr"), IsActive: true);

    public static readonly Customer 정민호 = new(
        CustomerId.New(), new CustomerName("정민호"), new Email("minho@company.co.kr"), IsActive: false);

    public static readonly Customer 한소영 = new(
        CustomerId.New(), new CustomerName("한소영"), new Email("soyoung@example.com"), IsActive: true);

    public static IReadOnlyList<Customer> All =>
    [
        김철수, 이영희, 박지민, 최수진, 정민호, 한소영
    ];
}
