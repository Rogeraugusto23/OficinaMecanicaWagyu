using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OficinaMecanicaWagyu.Domain.Entities;

namespace OficinaMecanicaWagyu.Infrastructure.Data
{
    public class OficinaDbContext : DbContext
    {
        public OficinaDbContext(DbContextOptions<OficinaDbContext> options) : base(options) { }

        public DbSet<OrdemServico> OrdensServico { get; set; }
        public DbSet<ServicoItem> ServicosItem { get; set; }
        public DbSet<PecaItem> Pecas { get; set; }
        public DbSet<Cliente> Clientes { get; set; }   
        public DbSet<Veiculo> Veiculos { get; set; }
        public DbSet<Peca> PecasCatalogo { get; set; }
        public DbSet<Servico> Servicos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrdemServico>(entity =>
            {
                entity.ToTable("OrdensServico");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NumeroOS).IsRequired().HasMaxLength(20);
                entity.Property(e => e.ValorTotal).HasColumnType("decimal(18,2)");

                entity.HasMany(e => e.Servicos)
                      .WithOne()
                      .HasForeignKey("OrdemServicoId")
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Pecas)
                      .WithOne()
                      .HasForeignKey("OrdemServicoId")
                      .OnDelete(DeleteBehavior.Cascade);
                                
            });

            modelBuilder.Entity<ServicoItem>(entity =>
            {
                entity.ToTable("OrdemServico_Servicos");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Preco).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<PecaItem>(entity =>
            {
                entity.ToTable("OrdemServico_Pecas");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PrecoUnitario).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.ToTable("Clientes");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Documento).IsRequired().HasMaxLength(14);
                entity.HasIndex(e => e.Documento).IsUnique(); 
            });

            modelBuilder.Entity<Veiculo>(entity =>
            {
                entity.ToTable("Veiculos");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Placa).IsRequired().HasMaxLength(7);
                entity.Property(e => e.Marca).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Modelo).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Placa).IsUnique();
            });

            modelBuilder.Entity<Peca>(entity =>
            {
                entity.ToTable("Pecas_Catalogo");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Codigo).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PrecoUnitario).HasColumnType("decimal(18,2)");
                entity.HasIndex(e => e.Codigo).IsUnique(); 
            });

            modelBuilder.Entity<Servico>(entity =>
            {
                entity.ToTable("Servicos_Catalogo");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Descricao).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Preco).HasColumnType("decimal(18,2)");
            });

            base.OnModelCreating(modelBuilder);
        }
    }

    public class OficinaDbContextFactory : IDesignTimeDbContextFactory<OficinaDbContext>
    {
        public OficinaDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<OficinaDbContext>();
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=OficinaMecanicaDB;Trusted_Connection=True;MultipleActiveResultSets=true");
            return new OficinaDbContext(optionsBuilder.Options);
        }
    }
}