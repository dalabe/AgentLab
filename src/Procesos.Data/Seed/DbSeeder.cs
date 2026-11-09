using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Procesos.Data.Entities;
using Procesos.Data.Storage;

namespace Procesos.Data.Seed;

/// <summary>
/// Siembra datos mock idempotentes: personas, procesos legales, eventos de línea de tiempo y
/// documentos (filas en base de datos + archivos físicos de texto bajo la carpeta de
/// almacenamiento configurada). Pensado para ejecutarse una vez al iniciar
/// Procesos.BackOffice.Api en entorno de desarrollo.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(ProcesosDbContext db, DocumentPathResolver pathResolver, ILogger logger, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (await db.Personas.AnyAsync(ct))
        {
            logger.LogInformation("Seed omitido: ya existen personas en la base de datos.");
            return;
        }

        pathResolver.EnsureRootExists();

        var personas = BuildPersonas();
        db.Personas.AddRange(personas);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Seed: {Count} personas insertadas.", personas.Count);

        var documentosFisicosEscritos = 0;

        foreach (var persona in personas)
        {
            foreach (var proceso in persona.Procesos)
            {
                foreach (var documento in proceso.Documentos)
                {
                    var fullPath = pathResolver.ResolveFullPath(documento.RutaRelativa);
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

                    var contenido = BuildDocumentoContenido(persona, proceso, documento);
                    await File.WriteAllTextAsync(fullPath, contenido, ct);

                    documento.TamanoBytes = new FileInfo(fullPath).Length;
                    documentosFisicosEscritos++;
                }
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Seed completo: {Procesos} procesos, {Documentos} documentos físicos escritos bajo '{Root}'.",
            personas.Sum(p => p.Procesos.Count), documentosFisicosEscritos, pathResolver.RootPath);
    }

    private static List<Persona> BuildPersonas()
    {
        var now = new DateTime(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);

        Persona Persona(string doc, string nombres, string apellidos, string email, string tel, DateOnly nacimiento) => new()
        {
            DocumentoIdentidad = doc,
            Nombres = nombres,
            Apellidos = apellidos,
            Email = email,
            Telefono = tel,
            FechaNacimiento = nacimiento,
            CreatedAtUtc = now,
        };

        var mariaFernanda = Persona("1020304050", "María Fernanda", "Gómez Rivas", "maria.gomez@example.com", "+57 300 111 2233", new DateOnly(1985, 3, 14));
        var juanCarlos = Persona("1030405060", "Juan Carlos", "Pérez Londoño", "juan.perez@example.com", "+57 301 222 3344", new DateOnly(1979, 7, 2));
        var anaSofia = Persona("1040506070", "Ana Sofía", "Martínez Cárdenas", "ana.martinez@example.com", "+57 302 333 4455", new DateOnly(1992, 11, 23));
        var carlosEduardo = Persona("1050607080", "Carlos Eduardo", "Rodríguez Salazar", "carlos.rodriguez@example.com", "+57 303 444 5566", new DateOnly(1988, 1, 9));
        var lauraValentina = Persona("1060708090", "Laura Valentina", "Torres Jiménez", "laura.torres@example.com", "+57 304 555 6677", new DateOnly(1995, 5, 30));
        var andresFelipe = Persona("1070809010", "Andrés Felipe", "Ramírez Ocampo", "andres.ramirez@example.com", "+57 305 666 7788", new DateOnly(1983, 9, 17));
        var dianaMarcela = Persona("1080901020", "Diana Marcela", "Suárez Peña", "diana.suarez@example.com", "+57 306 777 8899", new DateOnly(1990, 4, 5));
        var jorgeLuis = Persona("1090102030", "Jorge Luis", "Castañeda Mora", "jorge.castaneda@example.com", "+57 307 888 9900", new DateOnly(1976, 12, 1));

        mariaFernanda.Procesos.Add(ProcesoCivilArriendo(mariaFernanda));
        mariaFernanda.Procesos.Add(ProcesoLaboralDespido(mariaFernanda));

        juanCarlos.Procesos.Add(ProcesoPenalHurto(juanCarlos));

        anaSofia.Procesos.Add(ProcesoFamiliaCustodia(anaSofia));
        anaSofia.Procesos.Add(ProcesoCivilSucesion(anaSofia));

        carlosEduardo.Procesos.Add(ProcesoComercialIncumplimiento(carlosEduardo));

        lauraValentina.Procesos.Add(ProcesoLaboralIndemnizacion(lauraValentina));
        lauraValentina.Procesos.Add(ProcesoAdministrativoMulta(lauraValentina));

        andresFelipe.Procesos.Add(ProcesoPenalLesiones(andresFelipe));

        dianaMarcela.Procesos.Add(ProcesoCivilArrendamientoComercial(dianaMarcela));
        dianaMarcela.Procesos.Add(ProcesoFamiliaAlimentos(dianaMarcela));

        jorgeLuis.Procesos.Add(ProcesoComercialSociedades(jorgeLuis));
        jorgeLuis.Procesos.Add(ProcesoAdministrativoLicencia(jorgeLuis));
        jorgeLuis.Procesos.Add(ProcesoCivilResponsabilidad(jorgeLuis));

        return [mariaFernanda, juanCarlos, anaSofia, carlosEduardo, lauraValentina, andresFelipe, dianaMarcela, jorgeLuis];
    }

    // ---- Definiciones de procesos (uno por método para que cada caso sea legible y editable) ----

    private static Proceso ProcesoCivilArriendo(Persona p) => NuevoProceso(
        p, "EXP-2023-00114", TipoProceso.Civil, EstadoProceso.Cerrado,
        "Juzgado 12 Civil Municipal de Bogotá",
        "Proceso de restitución de inmueble arrendado por incumplimiento en el pago de cánones durante tres meses consecutivos.",
        new DateOnly(2023, 2, 10), "Proceso cerrado con restitución efectiva del inmueble y acuerdo de pago de cánones adeudados.",
        docs: [
            (TipoDocumento.Demanda, "Demanda de restitución de inmueble arrendado.pdf"),
            (TipoDocumento.Sentencia, "Sentencia de primera instancia.pdf"),
        ],
        eventos: [
            (new DateOnly(2023, 2, 10), TipoEvento.Presentacion, "Se presenta demanda de restitución de inmueble arrendado."),
            (new DateOnly(2023, 4, 18), TipoEvento.Audiencia, "Audiencia de conciliación: no se llega a acuerdo."),
            (new DateOnly(2023, 8, 2), TipoEvento.Resolucion, "Se dicta sentencia ordenando la restitución del inmueble."),
            (new DateOnly(2023, 9, 15), TipoEvento.Otro, "Restitución material del inmueble verificada por el juzgado."),
        ]);

    private static Proceso ProcesoLaboralDespido(Persona p) => NuevoProceso(
        p, "EXP-2024-00031", TipoProceso.Laboral, EstadoProceso.Activo,
        "Juzgado 4 Laboral del Circuito de Medellín",
        "Demanda por despido sin justa causa, solicitando reintegro e indemnización equivalente a 90 días de salario.",
        new DateOnly(2024, 3, 5), "A la espera de audiencia de trámite y juzgamiento. Pendiente el dictamen pericial sobre liquidación de prestaciones.",
        docs: [
            (TipoDocumento.Demanda, "Demanda laboral por despido sin justa causa.pdf"),
            (TipoDocumento.Prueba, "Certificaciones laborales y comprobantes de pago.pdf"),
        ],
        eventos: [
            (new DateOnly(2024, 3, 5), TipoEvento.Presentacion, "Se presenta demanda laboral por despido sin justa causa."),
            (new DateOnly(2024, 5, 20), TipoEvento.Notificacion, "Se notifica a la empresa demandada."),
            (new DateOnly(2024, 9, 11), TipoEvento.Audiencia, "Audiencia de conciliación, decisión de excepciones previas y fijación del litigio."),
            (new DateOnly(2026, 2, 3), TipoEvento.Audiencia, "Programada audiencia de trámite y juzgamiento."),
        ]);

    private static Proceso ProcesoPenalHurto(Persona p) => NuevoProceso(
        p, "EXP-2024-00552", TipoProceso.Penal, EstadoProceso.EnRevision,
        "Fiscalía 23 Seccional - Bogotá",
        "Denuncia por hurto calificado de vehículo. Investigación en etapa de instrucción, pendiente de imputación formal.",
        new DateOnly(2024, 6, 22), "Se identificaron dos posibles implicados mediante cámaras de seguridad; pendiente orden de captura.",
        docs: [
            (TipoDocumento.Otro, "Denuncia penal por hurto calificado.pdf"),
            (TipoDocumento.Prueba, "Informe de policía judicial y registro fotográfico.pdf"),
        ],
        eventos: [
            (new DateOnly(2024, 6, 22), TipoEvento.Presentacion, "Se radica denuncia penal por hurto calificado de vehículo."),
            (new DateOnly(2024, 7, 30), TipoEvento.Otro, "Policía judicial recauda material probatorio (video de cámaras)."),
            (new DateOnly(2025, 1, 14), TipoEvento.Resolucion, "Fiscalía asigna investigador y abre etapa de instrucción."),
        ]);

    private static Proceso ProcesoFamiliaCustodia(Persona p) => NuevoProceso(
        p, "EXP-2024-00789", TipoProceso.Familia, EstadoProceso.Activo,
        "Comisaría de Familia de Chapinero",
        "Proceso de fijación de custodia y régimen de visitas respecto de un menor de edad, ante desacuerdo entre los padres.",
        new DateOnly(2024, 8, 1), "Pendiente informe de la psicóloga adscrita a la Comisaría antes de la audiencia de fallo.",
        docs: [
            (TipoDocumento.Demanda, "Solicitud de fijación de custodia y régimen de visitas.pdf"),
        ],
        eventos: [
            (new DateOnly(2024, 8, 1), TipoEvento.Presentacion, "Se radica solicitud de fijación de custodia y régimen de visitas."),
            (new DateOnly(2024, 10, 9), TipoEvento.Audiencia, "Audiencia de conciliación; se acuerda régimen de visitas provisional."),
            (new DateOnly(2026, 1, 20), TipoEvento.Otro, "Se ordena valoración psicológica al menor y a ambos padres."),
        ]);

    private static Proceso ProcesoCivilSucesion(Persona p) => NuevoProceso(
        p, "EXP-2022-00276", TipoProceso.Civil, EstadoProceso.Archivado,
        "Notaría 15 de Bogotá",
        "Proceso de sucesión intestada por el fallecimiento del padre de la solicitante, con tres herederos reconocidos.",
        new DateOnly(2022, 5, 3), "Sucesión liquidada y protocolizada; proceso archivado tras registro de la partición en la Oficina de Registro.",
        docs: [
            (TipoDocumento.Resolucion, "Acta de partición y adjudicación de la herencia.pdf"),
        ],
        eventos: [
            (new DateOnly(2022, 5, 3), TipoEvento.Presentacion, "Se inicia trámite de sucesión intestada."),
            (new DateOnly(2022, 11, 20), TipoEvento.Resolucion, "Se aprueba trabajo de partición y adjudicación."),
            (new DateOnly(2023, 1, 10), TipoEvento.Otro, "Protocolización e inscripción registral de la partición."),
        ]);

    private static Proceso ProcesoComercialIncumplimiento(Persona p) => NuevoProceso(
        p, "EXP-2025-00042", TipoProceso.Comercial, EstadoProceso.Activo,
        "Centro de Arbitraje y Conciliación - Cámara de Comercio de Cali",
        "Arbitraje por incumplimiento de contrato de suministro de materiales de construcción; se reclama el pago de facturas vencidas más intereses moratorios.",
        new DateOnly(2025, 2, 18), "Tribunal arbitral conformado; en etapa probatoria con dictamen contable en curso.",
        docs: [
            (TipoDocumento.Contrato, "Contrato de suministro de materiales.pdf"),
            (TipoDocumento.Demanda, "Demanda arbitral por incumplimiento contractual.pdf"),
        ],
        eventos: [
            (new DateOnly(2025, 2, 18), TipoEvento.Presentacion, "Se presenta demanda arbitral por incumplimiento contractual."),
            (new DateOnly(2025, 4, 2), TipoEvento.Otro, "Instalación del tribunal arbitral."),
            (new DateOnly(2025, 10, 28), TipoEvento.Otro, "Se ordena dictamen pericial contable sobre las facturas en disputa."),
        ]);

    private static Proceso ProcesoLaboralIndemnizacion(Persona p) => NuevoProceso(
        p, "EXP-2023-00901", TipoProceso.Laboral, EstadoProceso.Cerrado,
        "Juzgado 7 Laboral del Circuito de Cali",
        "Demanda por indemnización por accidente de trabajo con incapacidad permanente parcial reconocida por la ARL.",
        new DateOnly(2023, 3, 29), "Conciliado: la empresa pagó la indemnización acordada y el proceso se cerró por transacción.",
        docs: [
            (TipoDocumento.Demanda, "Demanda por accidente de trabajo.pdf"),
            (TipoDocumento.Resolucion, "Acta de conciliación y acuerdo de pago.pdf"),
        ],
        eventos: [
            (new DateOnly(2023, 3, 29), TipoEvento.Presentacion, "Se presenta demanda por accidente de trabajo."),
            (new DateOnly(2023, 6, 14), TipoEvento.Audiencia, "Audiencia de conciliación: las partes llegan a acuerdo."),
            (new DateOnly(2023, 7, 5), TipoEvento.Pago, "La empresa realiza el pago acordado y se da por terminado el proceso."),
        ]);

    private static Proceso ProcesoAdministrativoMulta(Persona p) => NuevoProceso(
        p, "EXP-2025-00187", TipoProceso.Administrativo, EstadoProceso.EnRevision,
        "Secretaría de Movilidad de Cali",
        "Recurso de reposición contra una multa de tránsito impuesta por presunto exceso de velocidad, cuestionando la calibración del equipo de medición.",
        new DateOnly(2025, 6, 11), "Pendiente respuesta de la Secretaría sobre la solicitud de certificado de calibración del fotodetector.",
        docs: [
            (TipoDocumento.Otro, "Recurso de reposición contra comparendo.pdf"),
        ],
        eventos: [
            (new DateOnly(2025, 6, 11), TipoEvento.Presentacion, "Se radica recurso de reposición contra el comparendo."),
            (new DateOnly(2025, 9, 3), TipoEvento.Otro, "Se solicita certificado de calibración del equipo de medición de velocidad."),
        ]);

    private static Proceso ProcesoPenalLesiones(Persona p) => NuevoProceso(
        p, "EXP-2024-01122", TipoProceso.Penal, EstadoProceso.Suspendido,
        "Fiscalía 5 Seccional - Medellín",
        "Proceso por lesiones personales culposas derivadas de un accidente de tránsito; suspendido mientras se resuelve una incidencia de prejudicialidad civil.",
        new DateOnly(2024, 11, 2), "Suspendido hasta que el proceso civil de responsabilidad determine la distribución de culpas.",
        docs: [
            (TipoDocumento.Otro, "Informe médico legal de lesiones.pdf"),
        ],
        eventos: [
            (new DateOnly(2024, 11, 2), TipoEvento.Presentacion, "Se formula denuncia por lesiones personales culposas."),
            (new DateOnly(2025, 3, 19), TipoEvento.Otro, "Se decreta la suspensión por prejudicialidad civil."),
        ]);

    private static Proceso ProcesoCivilArrendamientoComercial(Persona p) => NuevoProceso(
        p, "EXP-2025-00310", TipoProceso.Civil, EstadoProceso.Activo,
        "Juzgado 3 Civil del Circuito de Barranquilla",
        "Proceso declarativo por incumplimiento de cláusulas de un contrato de arrendamiento de local comercial, incluyendo daños al inmueble.",
        new DateOnly(2025, 4, 7), "En etapa de contestación de la demanda; se espera réplica de la parte demandada.",
        docs: [
            (TipoDocumento.Contrato, "Contrato de arrendamiento de local comercial.pdf"),
            (TipoDocumento.Demanda, "Demanda declarativa por incumplimiento contractual.pdf"),
        ],
        eventos: [
            (new DateOnly(2025, 4, 7), TipoEvento.Presentacion, "Se presenta demanda declarativa por incumplimiento contractual."),
            (new DateOnly(2025, 7, 22), TipoEvento.Notificacion, "Se notifica personalmente a la parte demandada."),
        ]);

    private static Proceso ProcesoFamiliaAlimentos(Persona p) => NuevoProceso(
        p, "EXP-2026-00019", TipoProceso.Familia, EstadoProceso.Activo,
        "Comisaría de Familia de Kennedy",
        "Proceso de fijación de cuota de alimentos a favor de dos menores de edad, ante incumplimiento reiterado del obligado.",
        new DateOnly(2026, 1, 15), "Se decretó embargo parcial de salario como medida cautelar mientras se resuelve el fondo.",
        docs: [
            (TipoDocumento.Resolucion, "Resolución que decreta medida cautelar de embargo de salario.pdf"),
        ],
        eventos: [
            (new DateOnly(2026, 1, 15), TipoEvento.Presentacion, "Se radica solicitud de fijación de cuota de alimentos."),
            (new DateOnly(2026, 3, 2), TipoEvento.Resolucion, "Se decreta medida cautelar de embargo parcial de salario."),
        ]);

    private static Proceso ProcesoComercialSociedades(Persona p) => NuevoProceso(
        p, "EXP-2023-00450", TipoProceso.Comercial, EstadoProceso.Cerrado,
        "Superintendencia de Sociedades - Bogotá",
        "Conflicto societario entre socios de una SAS por exclusión indebida de un socio minoritario; resuelto mediante acuerdo de recompra de participaciones.",
        new DateOnly(2023, 1, 20), "Cerrado por transacción: la sociedad recompró las cuotas del socio minoritario en los términos acordados.",
        docs: [
            (TipoDocumento.Demanda, "Demanda de impugnación de asamblea de socios.pdf"),
            (TipoDocumento.Resolucion, "Acuerdo transaccional de recompra de participaciones.pdf"),
        ],
        eventos: [
            (new DateOnly(2023, 1, 20), TipoEvento.Presentacion, "Se presenta demanda de impugnación de la asamblea que excluyó al socio."),
            (new DateOnly(2023, 5, 11), TipoEvento.Audiencia, "Audiencia de conciliación; se perfila acuerdo de recompra."),
            (new DateOnly(2023, 6, 30), TipoEvento.Pago, "Se formaliza y paga la recompra de participaciones; proceso cerrado."),
        ]);

    private static Proceso ProcesoAdministrativoLicencia(Persona p) => NuevoProceso(
        p, "EXP-2025-00533", TipoProceso.Administrativo, EstadoProceso.Activo,
        "Curaduría Urbana 1 de Bogotá",
        "Recurso contra la negación de una licencia de construcción por presunta afectación a norma de alturas en zona residencial.",
        new DateOnly(2025, 8, 14), "Pendiente concepto técnico de la Secretaría de Planeación sobre la norma urbanística aplicable al predio.",
        docs: [
            (TipoDocumento.Otro, "Recurso de apelación contra negación de licencia de construcción.pdf"),
        ],
        eventos: [
            (new DateOnly(2025, 8, 14), TipoEvento.Presentacion, "Se radica recurso de apelación contra la negación de la licencia."),
            (new DateOnly(2025, 11, 2), TipoEvento.Otro, "Se solicita concepto técnico a la Secretaría de Planeación."),
        ]);

    private static Proceso ProcesoCivilResponsabilidad(Persona p) => NuevoProceso(
        p, "EXP-2026-00067", TipoProceso.Civil, EstadoProceso.EnRevision,
        "Juzgado 9 Civil del Circuito de Bogotá",
        "Demanda de responsabilidad civil extracontractual por daños materiales derivados de una inundación proveniente de un predio vecino.",
        new DateOnly(2026, 2, 9), "En estudio de admisión de la demanda; pendiente que el juzgado decida sobre la medida cautelar solicitada.",
        docs: [
            (TipoDocumento.Demanda, "Demanda de responsabilidad civil extracontractual.pdf"),
            (TipoDocumento.Prueba, "Dictamen técnico de daños e informe fotográfico.pdf"),
        ],
        eventos: [
            (new DateOnly(2026, 2, 9), TipoEvento.Presentacion, "Se presenta demanda de responsabilidad civil extracontractual."),
            (new DateOnly(2026, 4, 3), TipoEvento.Otro, "El juzgado admite la demanda y corre traslado a la parte demandada."),
        ]);

    // ---- Helpers de construcción ----

    private static Proceso NuevoProceso(
        Persona persona,
        string numeroExpediente,
        TipoProceso tipo,
        EstadoProceso estado,
        string entidadOJuzgado,
        string descripcion,
        DateOnly fechaInicio,
        string notas,
        (TipoDocumento Tipo, string Nombre)[] docs,
        (DateOnly Fecha, TipoEvento Tipo, string Descripcion)[] eventos)
    {
        var ultimaFecha = eventos.Length > 0 ? eventos.Max(e => e.Fecha) : fechaInicio;

        var proceso = new Proceso
        {
            NumeroExpediente = numeroExpediente,
            Tipo = tipo,
            Estado = estado,
            EntidadOJuzgado = entidadOJuzgado,
            Descripcion = descripcion,
            FechaInicio = fechaInicio,
            FechaUltimaActualizacionUtc = ultimaFecha.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            Notas = notas,
        };

        var index = 1;
        foreach (var (docTipo, nombre) in docs)
        {
            var archivo = SlugArchivo(nombre);
            proceso.Documentos.Add(new DocumentoProceso
            {
                Nombre = nombre,
                Tipo = docTipo,
                RutaRelativa = Path.Combine(numeroExpediente, $"{index:00}_{archivo}.txt"),
                ContentType = "text/plain",
                FechaCargaUtc = fechaInicio.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            });
            index++;
        }

        foreach (var (fecha, evTipo, evDescripcion) in eventos)
        {
            proceso.Eventos.Add(new ProcesoEvento
            {
                Fecha = fecha,
                Tipo = evTipo,
                Descripcion = evDescripcion,
            });
        }

        return proceso;
    }

    private static string SlugArchivo(string nombre)
    {
        var sinExtension = Path.GetFileNameWithoutExtension(nombre);
        var normalizado = sinExtension
            .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
            .Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U")
            .Replace("ñ", "n").Replace("Ñ", "N");

        var chars = normalizado.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
        return new string(chars).Trim('_');
    }

    /// <summary>
    /// Genera el contenido de texto plano del documento mock (simula el texto de un PDF/Word real).
    /// El contenido referencia datos concretos del proceso para que el MCP tool de lectura de
    /// documentos tenga algo específico y verificable que citar.
    /// </summary>
    private static string BuildDocumentoContenido(Persona persona, Proceso proceso, DocumentoProceso documento)
    {
        var encabezado =
            $"""
            =====================================================================
            DOCUMENTO MOCK - LABORATORIO MCP (contenido ficticio, solo para pruebas)
            =====================================================================
            Expediente:        {proceso.NumeroExpediente}
            Tipo de documento:  {documento.Tipo}
            Nombre de archivo:  {documento.Nombre}
            Proceso:            {proceso.Tipo} - {proceso.Estado}
            Entidad/Juzgado:    {proceso.EntidadOJuzgado}
            Parte:              {persona.NombreCompleto} (doc. identidad {persona.DocumentoIdentidad})
            Fecha de carga:     {documento.FechaCargaUtc:yyyy-MM-dd}
            =====================================================================

            """;

        var cuerpo = documento.Tipo switch
        {
            TipoDocumento.Demanda =>
                $"""
                Señor Juez / Despacho ({proceso.EntidadOJuzgado}):

                {persona.NombreCompleto}, identificado(a) con documento de identidad No. {persona.DocumentoIdentidad},
                por medio del presente escrito formula demanda dentro del proceso {proceso.NumeroExpediente},
                con fundamento en los siguientes hechos:

                {proceso.Descripcion}

                Con base en lo anterior, se solicita al despacho admitir la presente demanda, dar el trámite
                legal correspondiente y, en su oportunidad, proferir sentencia favorable a las pretensiones
                de la parte demandante.
                """,
            TipoDocumento.Contestacion =>
                $"""
                Dentro del término legal, la parte demandada da respuesta a la demanda instaurada en el proceso
                {proceso.NumeroExpediente}, pronunciándose sobre los hechos relacionados con:

                {proceso.Descripcion}

                Se proponen las excepciones de mérito que el despacho estime pertinentes y se solicita
                decretar las pruebas aquí relacionadas.
                """,
            TipoDocumento.Sentencia =>
                $"""
                El {proceso.EntidadOJuzgado}, administrando justicia en nombre de la República, dentro del
                proceso {proceso.NumeroExpediente} y una vez analizados los hechos relativos a:

                {proceso.Descripcion}

                RESUELVE: proferir la presente decisión de fondo conforme a lo debatido y probado en el
                proceso, quedando notificada a las partes en la forma legalmente prevista.

                Nota del caso: {proceso.Notas}
                """,
            TipoDocumento.Contrato =>
                $"""
                CONTRATO relacionado con el proceso {proceso.NumeroExpediente}, celebrado entre las partes
                involucradas, cuyo objeto y antecedentes dieron origen a la controversia descrita a continuación:

                {proceso.Descripcion}

                Las partes se obligan a lo aquí estipulado, sujeto a las cláusulas pactadas y a la normativa
                aplicable en la materia.
                """,
            TipoDocumento.Prueba =>
                $"""
                Material probatorio aportado dentro del proceso {proceso.NumeroExpediente}, relacionado con
                los hechos objeto de controversia:

                {proceso.Descripcion}

                Este documento se incorpora al expediente como soporte de las afirmaciones de la parte que
                lo presenta, para que sea valorado por el despacho/entidad competente.
                """,
            TipoDocumento.Resolucion =>
                $"""
                RESOLUCIÓN emitida dentro del proceso {proceso.NumeroExpediente} por {proceso.EntidadOJuzgado},
                en relación con:

                {proceso.Descripcion}

                Estado actual del trámite: {proceso.Estado}. {proceso.Notas}
                """,
            TipoDocumento.Notificacion =>
                $"""
                NOTIFICACIÓN librada dentro del proceso {proceso.NumeroExpediente} a {persona.NombreCompleto},
                informando sobre las actuaciones surtidas en relación con:

                {proceso.Descripcion}

                Se advierte a la parte notificada sobre los términos y recursos legales disponibles.
                """,
            _ =>
                $"""
                Documento asociado al proceso {proceso.NumeroExpediente}, relacionado con:

                {proceso.Descripcion}
                """,
        };

        return encabezado + cuerpo + Environment.NewLine;
    }
}
