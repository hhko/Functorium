using EcommerceFiltering.Domain;
using EcommerceFiltering.Domain.ValueObjects;

namespace EcommerceFiltering;

public static class SampleProducts
{
    public static readonly Product 맥북프로 = new(
        new ProductName("맥북 프로 16인치"), new Money(3_490_000m), new Quantity(5), new Category("전자기기"));

    public static readonly Product 아이패드 = new(
        new ProductName("아이패드 에어"), new Money(929_000m), new Quantity(12), new Category("전자기기"));

    public static readonly Product 에어팟 = new(
        new ProductName("에어팟 프로"), new Money(359_000m), new Quantity(0), new Category("전자기기"));

    public static readonly Product 운동화 = new(
        new ProductName("나이키 에어맥스"), new Money(189_000m), new Quantity(30), new Category("의류"));

    public static readonly Product 후드티 = new(
        new ProductName("오버핏 후드티"), new Money(49_000m), new Quantity(100), new Category("의류"));

    public static readonly Product 커피머신 = new(
        new ProductName("드롱기 커피머신"), new Money(890_000m), new Quantity(3), new Category("가전"));

    public static readonly Product 텀블러 = new(
        new ProductName("스탠리 텀블러"), new Money(45_000m), new Quantity(0), new Category("생활용품"));

    public static readonly Product 키보드 = new(
        new ProductName("리얼포스 키보드"), new Money(350_000m), new Quantity(8), new Category("전자기기"));

    public static IReadOnlyList<Product> All =>
    [
        맥북프로, 아이패드, 에어팟, 운동화, 후드티, 커피머신, 텀블러, 키보드
    ];
}
