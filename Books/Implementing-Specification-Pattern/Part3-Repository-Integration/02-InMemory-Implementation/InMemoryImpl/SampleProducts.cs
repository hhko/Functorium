namespace InMemoryImpl;

public static class SampleProducts
{
    public static List<Product> Create() =>
    [
        new("무선 마우스", 15_000, 50, "전자제품"),
        new("기계식 키보드", 89_000, 30, "전자제품"),
        new("USB 케이블", 3_000, 200, "전자제품"),
        new("볼펜 세트", 5_000, 100, "문구류"),
        new("노트", 2_000, 150, "문구류"),
        new("프리미엄 만년필", 120_000, 0, "문구류"),
        new("에르고 의자", 350_000, 5, "가구"),
        new("모니터 암", 45_000, 0, "가구"),
    ];
}
