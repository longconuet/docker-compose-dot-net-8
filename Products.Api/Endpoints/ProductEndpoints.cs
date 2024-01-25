using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Products.Api.Database;
using Products.Api.Entities;
using Products.Api.Requests;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Products.Api.Endpoints
{
    public static class ProductEndpoints
    {
        public static void MapProductEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("products", async (ApplicationDbContext context) =>
            {
                var products = await context.Products.ToListAsync();

                return Results.Ok(products);
            });

            app.MapPost("products", async (CreateProductRequest request, ApplicationDbContext context) =>
            {
                var product = new Product
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Price = request.Price
                };

                context.Add(product);
                await context.SaveChangesAsync();

                return Results.Ok(product);
            });

            app.MapGet("products/{id}", async ([Required] Guid id, ApplicationDbContext context, IDistributedCache cache, CancellationToken ct) =>
            {
                var options = new DistributedCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(30));

                Product product;
                var productCache = await cache.GetAsync($"products-{id}");
                if (productCache != null)
                {
                    product = JsonSerializer.Deserialize<Product>(Encoding.UTF8.GetString(productCache));
                }
                else
                {
                    product = await context.Products
                       .AsNoTracking()
                       .FirstOrDefaultAsync(p => p.Id == id, ct);

                    string cachedDataString = JsonSerializer.Serialize(product);
                    var dataToCache = Encoding.UTF8.GetBytes(cachedDataString);
                    await cache.SetAsync($"products-{id}", dataToCache, options);
                }

                return product is null ? Results.NotFound() : Results.Ok(product);
            });

            app.MapPut("products/{id}", async ([Required] Guid id, [FromBody] UpdateProductRequest request, ApplicationDbContext context, IDistributedCache cache) =>
            {
                var product = context.Products.AsNoTracking().FirstOrDefault(p => p.Id == id);
                if (product is null)
                {
                    return Results.NotFound();
                }

                product.Name = request.Name;
                product.Price = request.Price;
                context.Update(product);
                await context.SaveChangesAsync();

                await cache.RemoveAsync($"products-{id}");

                return Results.NoContent();
            });

            app.MapDelete("products/{id}", async ([Required] Guid id, ApplicationDbContext context, IDistributedCache cache) =>
            {
                var product = context.Products.AsNoTracking().FirstOrDefault(p => p.Id == id);
                if (product is null)
                {
                    return Results.NotFound();
                }

                context.Remove(product);
                await context.SaveChangesAsync();

                await cache.RemoveAsync($"products-{id}");

                return Results.NoContent();
            });
        }
    }
}
