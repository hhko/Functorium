using CustomerManagement;
using CustomerManagement.Domain;
using CustomerManagement.Domain.Specifications;
using CustomerManagement.Domain.ValueObjects;
using CustomerManagement.Infrastructure;

Console.WriteLine("=== 고객 관리 필터링 ===\n");

ICustomerRepository repository = new InMemoryCustomerRepository(SampleCustomers.All);

// 1. 활성 고객 조회
Console.WriteLine("--- 활성 고객 ---");
var activeSpec = new CustomerActiveSpec();
foreach (var customer in repository.FindAll(activeSpec))
    Console.WriteLine($"  {customer.Name} ({customer.Email})");

Console.WriteLine();

// 2. 이메일로 고객 검색
Console.WriteLine("--- 이메일 검색: chulsoo@example.com ---");
var emailSpec = new CustomerEmailSpec(new Email("chulsoo@example.com"));
Console.WriteLine($"  존재 여부: {repository.Exists(emailSpec)}");

Console.WriteLine();

// 3. 이름 검색 (부분 일치)
Console.WriteLine("--- 이름에 '민' 포함 ---");
var nameSpec = new CustomerNameContainsSpec(new CustomerName("민"));
foreach (var customer in repository.FindAll(nameSpec))
    Console.WriteLine($"  {customer.Name} (활성: {customer.IsActive})");

Console.WriteLine();

// 4. 복합 조건: 활성 AND 이름에 '수' 포함
Console.WriteLine("--- 활성 고객 중 이름에 '수' 포함 ---");
var compositeSpec = new CustomerActiveSpec()
    & new CustomerNameContainsSpec(new CustomerName("수"));
foreach (var customer in repository.FindAll(compositeSpec))
    Console.WriteLine($"  {customer.Name} ({customer.Email})");

Console.WriteLine();

// 5. 비활성 고객 (NOT 활성)
Console.WriteLine("--- 비활성 고객 ---");
var inactiveSpec = !new CustomerActiveSpec();
foreach (var customer in repository.FindAll(inactiveSpec))
    Console.WriteLine($"  {customer.Name} ({customer.Email})");
