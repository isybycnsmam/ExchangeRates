using ExchangeRates.Models;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRates
{
    public class ExchangesContext : DbContext
    {
        public ExchangesContext(DbContextOptions<ExchangesContext> options)
            : base(options) { }

        public DbSet<EuroExchange> EuroExchanges { get; set; }
    }
}