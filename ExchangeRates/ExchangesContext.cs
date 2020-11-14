using ExchangeRates.Models;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRates
{
    public class ExchangesContext : DbContext
    {
        public ExchangesContext(DbContextOptions<ExchangesContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EuroExchange>(entity =>
            {
                entity.HasKey(c => new { c.Currency, c.Date });
            });
        }

        public DbSet<EuroExchange> EuroExchanges { get; set; }
    }
}