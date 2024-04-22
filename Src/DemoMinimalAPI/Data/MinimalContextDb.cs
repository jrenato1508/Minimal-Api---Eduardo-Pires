using DemoMinimalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoMinimalAPI.Data
{
    public class MinimalContextDb : DbContext
    {
        public MinimalContextDb( DbContextOptions<MinimalContextDb> options): base(options) { }

        public DbSet<Fornecedor> Fornecedores { get; set; }

        #region Mapeamento de entidade Fornecedores
        /*
            Aqui estamos mapeando a nossa entidade forcedor para o banco de dados. Como se trata de uma minimal Api, devemos envitar ficar criando
            camadas e camadas como por exemplo camadas de acesso a dados, camada de Negocios e etc... é uma API Minimal, enxuto, então tudo acaba sendo 
            implementado no mesmo projeto.

         */
        #endregion
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Fornecedor>()
                .HasKey(f => f.id);

            modelBuilder.Entity<Fornecedor>()
                .Property(f => f.Nome)
                .IsRequired()
                .HasColumnType("Varchar(100)");

            modelBuilder.Entity<Fornecedor>()
                .Property(f => f.Documento)
                .IsRequired()
                .HasColumnType("Varchar(14)");

            modelBuilder.Entity<Fornecedor>()
                .ToTable("Fornecedores");

            base.OnModelCreating(modelBuilder);
        }
    }
}
