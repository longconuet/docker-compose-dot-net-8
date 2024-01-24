using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Products.Api.Database;
using Products.Api.Entities;
using Products.Api.Requests;
using System.ComponentModel.DataAnnotations;

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
                //var product = await context.Products.FirstOrDefaultAsync(x => x.Id == id);
                var product = await cache.GetAsync($"products-{id}", async token =>
                {
                    var product = await context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, token);
                    return product;
                },
                ct);

                return product is null ? Results.NotFound() : Results.Ok(product);
            });
        }
    }
}
