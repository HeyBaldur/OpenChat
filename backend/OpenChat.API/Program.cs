using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenChat.Ai.Interfaces;
using OpenChat.Ai.Services;
using OpenChat.Ai.Tools;
using OpenChat.Application.Interfaces.External;
using OpenChat.Application.Interfaces.Services;
using OpenChat.Application.Services;
using OpenChat.Domain.Interfaces.Repositories;
using OpenChat.Infrastructure.Auth;
using OpenChat.Infrastructure.Migrations;
using OpenChat.Infrastructure.Mongo.Repositories;
using OpenChat.Infrastructure.Mongo.Settings;
using OpenChat.Infrastructure.Ollama;
using OpenChat.Infrastructure.Web;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT token."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            []
        }
    });
});

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();

// --- Application services ---
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAllowlistService, AllowlistService>();
builder.Services.AddHttpClient<IModelCatalogService, ModelCatalogService>();

// --- Ai (agentic tool calling) ---
builder.Services.AddScoped<IAgenticChatService, AgenticChatService>();
builder.Services.AddScoped<IToolRegistry, ToolRegistry>();
builder.Services.AddScoped<IToolDefinition, FetchUrlTool>();

// --- Infrastructure: Mongo ---
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));
builder.Services.AddSingleton<IChatRepository, ChatRepository>();
builder.Services.AddSingleton<IConversationRepository, ConversationRepository>();
builder.Services.AddSingleton<ILogRepository, LogRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IAllowedDomainRepository, AllowedDomainRepository>();

// --- Infrastructure: External services ---
builder.Services.AddHttpClient<IOllamaService, OllamaService>();
builder.Services.AddHttpClient("WebFetcher", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "OpenChatAi/1.0");
    client.Timeout = TimeSpan.FromSeconds(15);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    MaxAutomaticRedirections = 3,
    AllowAutoRedirect = true
});
builder.Services.AddScoped<IWebFetcherService, WebFetcherService>();

// --- Infrastructure: Auth ---
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// --- Infrastructure: Migrations ---
builder.Services.AddHostedService<AllowlistMigrationV2>();
builder.Services.AddHostedService<AllowlistMigrationV3>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
