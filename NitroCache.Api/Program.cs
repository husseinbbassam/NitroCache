using NitroCache.Api.Database;
using NitroCache.Api.Models;
using NitroCache.Api.Services;
using NitroCache.Library;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Configure NitroCache with Redis
builder.Services.AddNitroCache(options =>
{
    // Get Redis connection from configuration, default to localhost
    options.RedisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";
    options.DefaultExpiration = TimeSpan.FromMinutes(5);
    options.LocalCacheExpiration = TimeSpan.FromMinutes(1);
    options.MaximumLocalCacheSizeMB = 512;
});

// Configure JSON serialization with source generators
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, ProductJsonContext.Default);
});

// Register application services
builder.Services.AddSingleton<MockDatabase>();
builder.Services.AddSingleton<ProductService>();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();

// Product API endpoints
app.MapGet("/api/products", async (ProductService productService, CancellationToken ct) =>
{
    var products = await productService.GetAllProductsAsync(ct);
    return Results.Ok(products);
})
.WithName("GetAllProducts")
.WithOpenApi()
.Produces<List<Product>>();

app.MapGet("/api/products/{id:int}", async (int id, ProductService productService, CancellationToken ct) =>
{
    var product = await productService.GetProductByIdAsync(id, ct);
    return product is not null ? Results.Ok(product) : Results.NotFound();
})
.WithName("GetProductById")
.WithOpenApi()
.Produces<Product>()
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/api/products/category/{category}", async (string category, ProductService productService, CancellationToken ct) =>
{
    var products = await productService.GetProductsByCategoryAsync(category, ct);
    return Results.Ok(products);
})
.WithName("GetProductsByCategory")
.WithOpenApi()
.Produces<List<Product>>();

app.MapDelete("/api/products/{id:int}/cache", async (int id, ProductService productService, CancellationToken ct) =>
{
    await productService.InvalidateProductCacheAsync(id, ct);
    return Results.NoContent();
})
.WithName("InvalidateProductCache")
.WithOpenApi()
.Produces(StatusCodes.Status204NoContent);

app.MapDelete("/api/products/cache", async (ProductService productService, CancellationToken ct) =>
{
    await productService.InvalidateAllProductCachesAsync(ct);
    return Results.NoContent();
})
.WithName("InvalidateAllProductCaches")
.WithOpenApi()
.Produces(StatusCodes.Status204NoContent);

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();
