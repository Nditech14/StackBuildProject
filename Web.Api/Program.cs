using FluentValidation;
using Microsoft.OpenApi.Models;
using StackBuld.Application.Extensions;
using StackBuld.Application.Validators;
using StackBuld.Core.DTOs.OrderDtos;
using StackBuld.Core.DTOs.ProductDtos;
using StackBuld.Infrastructure.Extensions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Web.Api.Extensions;


 var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
   builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "StackBuld E-Commerce API",
                Version = "v1",
                Description = "A comprehensive e-commerce API built with Clean Architecture",
                Contact = new OpenApiContact
                {
                    Name = "StackBuld Development Team",
                    Email = "itzdominion@gmail.com"
                }
            });

        
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

           

           
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        });
        });

builder.Services.AddControllers()
     .AddJsonOptions(options =>
     {
         options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
         options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
         options.JsonSerializerOptions.WriteIndented = true;
     });

builder.Services.AddHttpContextAccessor();
builder.Services.AddJWTAuthentication(builder.Configuration);

builder.Services.AddScoped<IValidator<CreateProductDto>, CreateProductDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateProductDto>, UpdateProductDtoValidator>();
builder.Services.AddScoped<IValidator<CreateOrderDto>, CreateOrderDtoValidator>();
builder.Services.AddScoped<IValidator<ProductStockUpdateDto>, ProductStockUpdateDtoValidator>();

// Register our custom services
builder.Services.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddApplicationServices();
      
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigin", policy =>
            {
                policy.WithOrigins("http://localhost:3000", "https://localhost:3001") 
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });
        var app = builder.Build();

        // Configure pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "StackBuld E-Commerce API v1");
                c.RoutePrefix = "swagger";
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                c.DefaultModelExpandDepth(2);
                c.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
            });
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowSpecificOrigin");
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    
