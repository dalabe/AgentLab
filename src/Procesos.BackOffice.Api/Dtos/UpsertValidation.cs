using System.Net.Mail;

namespace Procesos.BackOffice.Api.Dtos;

/// <summary>
/// Request validation for the create/update DTOs. Endpoints call <c>Validar()</c> before touching
/// the database and answer <c>400</c> with the returned Spanish message (plain JSON string, see
/// specs/http-api.md). Length limits mirror the column sizes in <c>ProcesosDbContext.OnModelCreating</c>.
/// </summary>
public static class UpsertValidation
{
    public static string? Validar(this PersonaUpsertDto dto) =>
        Requerido(dto.DocumentoIdentidad, "documentoIdentidad", 32)
        ?? Requerido(dto.Nombres, "nombres", 120)
        ?? Requerido(dto.Apellidos, "apellidos", 120)
        ?? Opcional(dto.Email, "email", 200)
        ?? EmailValido(dto.Email)
        ?? Opcional(dto.Telefono, "telefono", 30);

    public static string? Validar(this ProcesoUpsertDto dto) =>
        Requerido(dto.NumeroExpediente, "numeroExpediente", 40)
        ?? (dto.PersonaId <= 0 ? "El campo 'personaId' es obligatorio." : null)
        ?? EnumValido(dto.Tipo, "tipo")
        ?? EnumValido(dto.Estado, "estado")
        ?? Requerido(dto.EntidadOJuzgado, "entidadOJuzgado", 200)
        ?? Requerido(dto.Descripcion, "descripcion", 2000)
        ?? (dto.FechaInicio == default ? "El campo 'fechaInicio' es obligatorio." : null)
        ?? Opcional(dto.Notas, "notas", 2000);

    public static string? Validar(this EventoUpsertDto dto) =>
        (dto.Fecha == default ? "El campo 'fecha' es obligatorio." : null)
        ?? EnumValido(dto.Tipo, "tipo")
        ?? Requerido(dto.Descripcion, "descripcion", 500);

    private static string? Requerido(string? valor, string campo, int maximo) =>
        string.IsNullOrWhiteSpace(valor)
            ? $"El campo '{campo}' es obligatorio."
            : Opcional(valor, campo, maximo);

    private static string? Opcional(string? valor, string campo, int maximo) =>
        valor is not null && valor.Trim().Length > maximo
            ? $"El campo '{campo}' admite como máximo {maximo} caracteres."
            : null;

    private static string? EmailValido(string? email) =>
        string.IsNullOrWhiteSpace(email) || MailAddress.TryCreate(email.Trim(), out _)
            ? null
            : "El campo 'email' no tiene un formato válido.";

    // JsonStringEnumConverter also accepts numbers, so out-of-range values must be rejected here.
    private static string? EnumValido<TEnum>(TEnum valor, string campo)
        where TEnum : struct, Enum =>
        Enum.IsDefined(valor) ? null : $"El campo '{campo}' tiene un valor no válido.";
}
