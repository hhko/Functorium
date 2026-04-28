using Functorium.Domains.Specifications.Expressions;

namespace PropertyMapDemo;

public static class ProductPropertyMap
{
    public static PropertyMap<Product, ProductDbModel> Create()
    {
        var map = new PropertyMap<Product, ProductDbModel>();
        map.Map(p => p.Name, m => m.ProductName);
        map.Map(p => p.Price, m => m.UnitPrice);
        map.Map(p => p.Stock, m => m.StockQuantity);
        map.Map(p => p.Category, m => m.CategoryCode);
        return map;
    }
}
