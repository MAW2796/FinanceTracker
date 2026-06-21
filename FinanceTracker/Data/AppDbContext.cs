using FinanceTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<MonthlyPlanning> MonthlyPlannings { get; set; }
        public DbSet<Goal> Goals { get; set; }
        public DbSet<GoalContribution> GoalContributions { get; set; }
        public DbSet<Installment> Installments { get; set; }
        public DbSet<DebtReceivable> DebtReceivables { get; set; }
        public DbSet<DebtPayment> DebtPayments { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<AssetValueHistory> AssetValueHistories { get; set; }
        public DbSet<Investment> Investments { get; set; }
        public DbSet<InvestmentTransaction> InvestmentTransactions { get; set; }
    }
}
