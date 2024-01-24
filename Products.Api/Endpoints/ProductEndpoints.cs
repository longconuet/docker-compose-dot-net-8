using Microsoft.EntityFrameworkCore;
using Products.Api.Database;
using Products.Api.Entities;
using Products.Api.Requests;

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
        }
    }
}
