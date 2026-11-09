using Procesos.ChatApi.Endpoints;
using Procesos.ChatApi.Sessions;
using Procesos.Llm;
using Procesos.McpClientCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// LLM intercambiable (Anthropic por defecto / Mock sin API key) + cliente MCP de servicio
// (OAuth2 client_credentials contra Procesos.TokenService + llamada HTTP a Procesos.McpServer).
builder.Services.AddProcesosChatClient(builder.Configuration);
builder.Services.AddProcesosMcpClient(builder.Configuration);
builder.Services.AddSingleton<ChatSessionStore>();

const string ChatWebCorsPolicy = "ChatWeb";
var allowedOrigins = builder.Configuration.GetSection("Chat:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(ChatWebCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseCors(ChatWebCorsPolicy);

app.MapChatEndpoints();

app.Run();
