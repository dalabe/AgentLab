using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Procesos.IntegrationTests;

/// <summary>
/// Invalid create/update bodies must be answered with 400 and a Spanish plain-string message
/// (specs/http-api.md), never with 500. Validation runs before any database access.
/// </summary>
public sealed class BackOfficeValidationTests(BackOfficeApiFactory factory) : IClassFixture<BackOfficeApiFactory>
{
    private static readonly string LongText = new('x', 2001);

    public static TheoryData<string, string, string, string> InvalidBodies => new()
    {
        // method, url, body, expected fragment of the error message
        { "POST", "/api/personas", "{}", "documentoIdentidad" },
        { "POST", "/api/personas", """{"documentoIdentidad":"123","nombres":" ","apellidos":"Ruiz"}""", "nombres" },
        { "POST", "/api/personas", """{"documentoIdentidad":"123","nombres":"Ana","apellidos":"Ruiz","email":"no-es-email"}""", "email" },
        { "PUT", "/api/personas/1", $$"""{"documentoIdentidad":"{{new string('9', 33)}}","nombres":"Ana","apellidos":"Ruiz"}""", "32" },
        { "POST", "/api/procesos", "{}", "numeroExpediente" },
        { "POST", "/api/procesos", """{"numeroExpediente":"EXP-1","personaId":1,"tipo":99,"estado":"Activo","entidadOJuzgado":"J","descripcion":"D","fechaInicio":"2024-01-01"}""", "tipo" },
        { "PUT", "/api/procesos/1", $$"""{"numeroExpediente":"EXP-1","personaId":1,"tipo":"Civil","estado":"Activo","entidadOJuzgado":"J","descripcion":"{{LongText}}","fechaInicio":"2024-01-01"}""", "descripcion" },
        { "POST", "/api/procesos/1/eventos", """{"tipo":"Audiencia","descripcion":"Audiencia inicial"}""", "fecha" },
        { "POST", "/api/procesos/1/eventos", """{"fecha":"2024-01-01","tipo":"Audiencia","descripcion":""}""", "descripcion" },
    };

    [Theory]
    [MemberData(nameof(InvalidBodies))]
    public async Task InvalidBody_Returns400WithMessage(string method, string url, string body, string expectedFragment)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), new Uri(url, UriKind.Relative))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var message = await response.Content.ReadFromJsonAsync<string>();
        Assert.NotNull(message);
        Assert.Contains(expectedFragment, message, StringComparison.Ordinal);
    }
}
