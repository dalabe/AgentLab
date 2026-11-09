using Microsoft.EntityFrameworkCore;
using Procesos.Data.Entities;

namespace Procesos.Data;

public class ProcesosDbContext : DbContext
{
    public ProcesosDbContext(DbContextOptions<ProcesosDbContext> options) : base(options)
    {
    }

    public DbSet<Persona> Personas => Set<Persona>();

    public DbSet<Proceso> Procesos => Set<Proceso>();

    public DbSet<DocumentoProceso> Documentos => Set<DocumentoProceso>();

    public DbSet<ProcesoEvento> Eventos => Set<ProcesoEvento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Persona>(b =>
        {
            b.ToTable("Personas");
            b.HasIndex(p => p.DocumentoIdentidad).IsUnique();
            b.Property(p => p.DocumentoIdentidad).HasMaxLength(32).IsRequired();
            b.Property(p => p.Nombres).HasMaxLength(120).IsRequired();
            b.Property(p => p.Apellidos).HasMaxLength(120).IsRequired();
            b.Property(p => p.Email).HasMaxLength(200);
            b.Property(p => p.Telefono).HasMaxLength(30);
            b.Ignore(p => p.NombreCompleto);
        });

        modelBuilder.Entity<Proceso>(b =>
        {
            b.ToTable("Procesos");
            b.HasIndex(p => p.NumeroExpediente).IsUnique();
            b.Property(p => p.NumeroExpediente).HasMaxLength(40).IsRequired();
            b.Property(p => p.EntidadOJuzgado).HasMaxLength(200).IsRequired();
            b.Property(p => p.Descripcion).HasMaxLength(2000).IsRequired();
            b.Property(p => p.Notas).HasMaxLength(2000);
            b.Property(p => p.Tipo).HasConversion<string>().HasMaxLength(30);
            b.Property(p => p.Estado).HasConversion<string>().HasMaxLength(30);

            b.HasOne(p => p.Persona)
                .WithMany(persona => persona.Procesos)
                .HasForeignKey(p => p.PersonaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentoProceso>(b =>
        {
            b.ToTable("Documentos");
            b.Property(d => d.Nombre).HasMaxLength(200).IsRequired();
            b.Property(d => d.RutaRelativa).HasMaxLength(400).IsRequired();
            b.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
            b.Property(d => d.Tipo).HasConversion<string>().HasMaxLength(30);

            b.HasOne(d => d.Proceso)
                .WithMany(p => p.Documentos)
                .HasForeignKey(d => d.ProcesoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProcesoEvento>(b =>
        {
            b.ToTable("Eventos");
            b.Property(e => e.Descripcion).HasMaxLength(500).IsRequired();
            b.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(30);

            b.HasOne(e => e.Proceso)
                .WithMany(p => p.Eventos)
                .HasForeignKey(e => e.ProcesoId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
