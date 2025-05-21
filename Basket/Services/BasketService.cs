global using Microsoft.Extensions.Caching.Distributed;
global using System.Text.Json;

namespace Basket.Services;

public class BasketService(IDistributedCache cache, CatalogApiClient catalogApiClient) 
{
    public async Task<ShoppingCart?> GetBasket(string userName)
    {
        var basket = await cache.GetStringAsync(userName);
        return string.IsNullOrEmpty(basket) ? null :
            JsonSerializer.Deserialize<ShoppingCart>(basket);
    }

    public async Task UpdateBasket(ShoppingCart basket)
    {
        // Before update(Add/Remove) into SC, we should call Catalog ms GetProductById method
        // Get latest product information and set Price, ProductName when adding item item into SC
        foreach (var item in basket.Items)
        {
            var product = await catalogApiClient.GetProductById(item.ProductId);
            item.Price = product.Price;
            item.ProductName = product.Name; 
        }

        await cache.SetStringAsync(basket.UserName, JsonSerializer.Serialize(basket));
    }

    public async Task DeleteBasket(string userName)
    {
        await cache.RemoveAsync(userName);
    }

    internal async Task UpdateBasketItemProductPrices(int productId, decimal price)
    {
        var basket = await GetBasket("swn");

        var item = basket!.Items.FirstOrDefault(x => x.ProductId == productId);
        if (item != null)
        {
            item.Price = price;
            await cache.SetStringAsync(basket.UserName, JsonSerializer.Serialize(basket)); 
        }
    }
}
