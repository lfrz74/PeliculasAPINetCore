using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PeliculasAPI.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PeliculasAPI
{
    public class ApplicationDbContext: IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> dbContextOptions)
            :base(dbContextOptions)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PeliculasActores>()
                .HasKey(pa => new { pa.ActorId, pa.PeliculaId });

            modelBuilder.Entity<PeliculasGeneros>()
                .HasKey(pg => new { pg.GeneroId, pg.PeliculaId });

            modelBuilder.Entity<PeliculasSalasDeCine>()
                .HasKey(psdc => new { psdc.PeliculaId, psdc.SalaDeCineId });

            SeedData(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var rolAdminId = "1c10ecde-174c-4258-bcd8-c44d9999adc0";
            var usuarioAdminId = "d61c7733-a352-435d-bec5-6137af0305e3";

            var rolAdmin = new IdentityRole()
            {
                Id = rolAdminId,
                Name = "Admin",
                NormalizedName = "Admin"
            };

            var passwordHash = new PasswordHasher<IdentityUser>();
            var username = "luisfernandoriverazapata@gmail.com";
            var usuarioAdmin = new IdentityUser()
            {
                Id = usuarioAdminId,
                UserName = username,
                NormalizedUserName = username,
                Email = username,
                NormalizedEmail = username,
                PasswordHash = passwordHash.HashPassword(null, "bols1alarge")
            };

            //Migration AdminData
            //modelBuilder.Entity<IdentityUser>().HasData(usuarioAdmin);

            //modelBuilder.Entity<IdentityRole>().HasData(rolAdmin);

            //modelBuilder.Entity<IdentityUserClaim<string>>()
            //    .HasData(new IdentityUserClaim<string>()
            //    {
            //        Id = 1,
            //        ClaimType = ClaimTypes.Role,
            //        UserId = usuarioAdminId,
            //        ClaimValue = "Admin"
            //    });

        }
        public DbSet<Genero> Generos { get; set; }
        public DbSet<Actor> Actores { get; set; }
        public DbSet<Pelicula> Peliculas { get; set; }
        public DbSet<PeliculasGeneros> PeliculasGeneros { get; set; }
        public DbSet<PeliculasActores> PeliculasActores { get; set; }
        public DbSet<SalaDeCine> SalasDeCines { get; set; }
        public DbSet<PeliculasSalasDeCine> PeliculasSalasDeCines { get; set; }
        public DbSet<Review> Reviews { get; set; }

    }
}
