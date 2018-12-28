using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;

namespace Test
{
    public partial class TestContext : DbContext
    {
        public TestContext()
        {
        }


        public static readonly LoggerFactory MyLoggerFactory = new LoggerFactory(new[]
        {
           new ConsoleLoggerProvider((category, level)=> category == DbLoggerCategory.Database.Command.Name && level == LogLevel.Information
                                     , false,false),
           (ILoggerProvider)new DebugLoggerProvider((category, level)=> category == DbLoggerCategory.Database.Command.Name && level == LogLevel.Information)
         });
        public TestContext(DbContextOptions<TestContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Department> Department { get; set; }
        public virtual DbSet<Role> Role { get; set; }
        public virtual DbSet<User> User { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseLoggerFactory(MyLoggerFactory).UseSqlServer("Data Source=.;Initial Catalog=Test;Integrated Security=True");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        { }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
