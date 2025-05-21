using MassTransit;
using ServiceDefaults.Messaging.Events;

namespace Catalog.Services
{
    public class ProductService(ProductDbContext dbContext, IBus bus)
    {
        public async Task<IEnumerable<Product>> GetProductsAsync()
        {
            return await dbContext.Products.ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await dbContext.Products.FindAsync(id);
        }

        public async Task CreateProductAsync(Product product)
        {
            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();
        }

        public async Task UpdateProductAsync(Product updateProduct, Product inputProduct)
        {
            // if price has changed, raise ProductPriceChanged Integration event
            if (updateProduct.Price != inputProduct.Price)
            {
                // publish event
                var integrationEvent = new ProductPriceChangedIntegrationEvent
                {
                    ProductId = updateProduct.Id,
                    Price = inputProduct.Price,
                    Description = inputProduct.Description,
                    Name = inputProduct.Name,
                    ImageUrl = inputProduct.ImageUrl
                };
                await bus.Publish(integrationEvent);
            }

            // update product with new values
            updateProduct.Name = inputProduct.Name;
            updateProduct.Description = inputProduct.Description;
            updateProduct.ImageUrl = inputProduct.ImageUrl;
            updateProduct.Price = inputProduct.Price;

            dbContext.Products.Update(updateProduct);
            await dbContext.SaveChangesAsync();
        }

        public async Task DeleteProductAsync(Product deletedProduct)
        {
            dbContext.Products.Remove(deletedProduct);
            await dbContext.SaveChangesAsync();
        }
    }
}
