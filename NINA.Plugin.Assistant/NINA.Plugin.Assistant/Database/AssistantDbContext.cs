using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Utility;
using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace Assistant.NINAPlugin.Database {

    public class AssistantDbContext : DbContext {

        public DbSet<Project> ProjectSet { get; set; }
        public DbSet<Target> TargetSet { get; set; }
        public DbSet<ExposurePlan> ExposurePlanSet { get; set; }
        public DbSet<Preference> PreferencePlanSet { get; set; }

        public AssistantDbContext(string connectionString) : base(new SQLiteConnection() { ConnectionString = connectionString }, true) {
            Configuration.LazyLoadingEnabled = false;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            Logger.Debug("Assistant database: OnModelCreating");

            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            var sqi = new CreateOrMigrateDatabaseInitializer<AssistantDbContext>();
            System.Data.Entity.Database.SetInitializer(sqi);
        }

        public static long DateTimeToUnixSeconds(DateTime dateTime) {
            return CoreUtil.DateTimeToUnixTimeStamp(dateTime);
        }

        public static DateTime UnixSecondsToDateTime(long seconds) {
            return CoreUtil.UnixTimeStampToDateTime(seconds);
        }

        private class CreateOrMigrateDatabaseInitializer<TContext> : CreateDatabaseIfNotExists<TContext>,
                IDatabaseInitializer<TContext> where TContext : AssistantDbContext {

            void IDatabaseInitializer<TContext>.InitializeDatabase(TContext context) {

                if (!DatabaseExists(context)) {

                    Logger.Debug("Assistant database: creating database schema");
                    try {
                        context.Database.BeginTransaction();

                        string initial = "C:\\Users\\Tom\\source\\repos\\nina.plugin.assistant\\NINA.Plugin.Assistant\\NINA.Plugin.Assistant\\Database\\Initial";
                        var initial_schema = Path.Combine(initial, "initial_schema.sql");
                        context.Database.ExecuteSqlCommand(File.ReadAllText(initial_schema));

                        context.Database.CurrentTransaction.Commit();
                    }
                    catch (Exception ex) {
                        context.Database.CurrentTransaction.Rollback();
                        Logger.Error(ex);
                    }
                }
            }

            private bool DatabaseExists(TContext context) {
                int numTables = context.Database.SqlQuery<int>("SELECT COUNT(*) FROM sqlite_master AS TABLES WHERE TYPE = 'table'").First();
                return numTables > 0;
            }

        }
    }
}
