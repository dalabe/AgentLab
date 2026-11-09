using Anthropic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Procesos.Llm;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra el <see cref="IChatClient"/> (abstracción de Microsoft.Extensions.AI) que usa
    /// Procesos.ChatApi para responder preguntas en lenguaje humano. El proveedor es
    /// intercambiable vía configuración ("Llm:Provider"): "Anthropic" (por defecto, requiere API
    /// key) o "Mock" (heurística local, sin LLM real, útil para probar el laboratorio sin costo).
    /// El cliente ya viene envuelto con <c>UseFunctionInvocation()</c>, así que el llamador solo
    /// necesita pasar <see cref="ChatOptions.Tools"/> y el bucle de tool-calling ocurre solo.
    /// </summary>
    public static IServiceCollection AddProcesosChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LlmOptions>(configuration.GetSection(LlmOptions.SectionName));

        services.AddSingleton<IChatClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<LlmOptions>>().Value;

            IChatClient inner = options.Provider.Equals("Mock", StringComparison.OrdinalIgnoreCase)
                ? new MockChatClient()
                : BuildAnthropicChatClient(options.Anthropic);

            return inner.AsBuilder().UseFunctionInvocation().Build();
        });

        return services;
    }

    private static IChatClient BuildAnthropicChatClient(AnthropicLlmOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException(
                "Falta 'Llm:Anthropic:ApiKey'. Configúrelo con `dotnet user-secrets set \"Llm:Anthropic:ApiKey\" \"<valor>\"` " +
                "o cambie 'Llm:Provider' a \"Mock\" para probar sin una API key real. Ver docs/RUNBOOK.md.");
        }

        return new AnthropicClient(new() { ApiKey = options.ApiKey })
            .AsIChatClient(options.Model, options.MaxOutputTokens);
    }
}
