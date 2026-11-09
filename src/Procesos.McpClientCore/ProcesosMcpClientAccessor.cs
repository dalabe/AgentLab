using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Procesos.McpClientCore;

/// <summary>
/// Implementación lazy y thread-safe de <see cref="IProcesosMcpClientAccessor"/>: crea el
/// <see cref="McpClient"/> una sola vez (primera llamada) y lo reutiliza. El HttpClient usado ya
/// trae adjunto <see cref="BearerTokenHandler"/> vía el registro en <c>ServiceCollectionExtensions</c>.
/// </summary>
public sealed class ProcesosMcpClientAccessor : IProcesosMcpClientAccessor, IAsyncDisposable
{
    public const string HttpClientName = "ProcesosMcpServer";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly McpServerOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private McpClient? _client;

    public ProcesosMcpClientAccessor(
        IHttpClientFactory httpClientFactory,
        IOptions<McpServerOptions> options,
        ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _loggerFactory = loggerFactory;
    }

    public async Task<McpClient> GetClientAsync(CancellationToken cancellationToken = default)
    {
        if (_client is not null)
        {
            return _client;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_client is not null)
            {
                return _client;
            }

            if (string.IsNullOrWhiteSpace(_options.Endpoint))
            {
                throw new InvalidOperationException(
                    $"'{McpServerOptions.SectionName}:Endpoint' no está configurado (URL del Procesos.McpServer).");
            }

            var httpClient = _httpClientFactory.CreateClient(HttpClientName);

            var transport = new HttpClientTransport(
                new HttpClientTransportOptions { Endpoint = new Uri(_options.Endpoint) },
                httpClient,
                _loggerFactory);

            _client = await McpClient.CreateAsync(transport, loggerFactory: _loggerFactory, cancellationToken: cancellationToken);
            return _client;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }

        _initLock.Dispose();
    }
}
