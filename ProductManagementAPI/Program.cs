using AutoMapper;
using Common.Mapping;
using Common.Middleware;
using Features.Products.DTOs;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Validators;
using MediatR;
using Features.Products;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationContext>(options =>
{
    options.UseInMemoryDatabase("ProductsDb");
});

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<AdvancedProductMappingProfile>();
});

builder.Services.AddMemoryCache();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<CreateProductHandler>();
});

builder.Services.AddScoped<IValidator<CreateProductProfileRequest>, CreateProductProfileValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductProfileValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/products", async (
        CreateProductProfileRequest request,
        IValidator<CreateProductProfileRequest> validator,
        IMediator mediator,
        CancellationToken ct) =>
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(request, ct);
        return Results.Created($"/products/{result.Id}", result);
    })
    .WithName("CreateProduct")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Create a new product";
        operation.Description = "Creates a new product with advanced validation, logging and mapping.";
        return operation;
    });

app.Run();