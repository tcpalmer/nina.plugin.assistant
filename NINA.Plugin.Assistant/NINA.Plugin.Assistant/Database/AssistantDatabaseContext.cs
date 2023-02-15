using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace Assistant.NINAPlugin.Database {

    public class AssistantDatabaseContext : DbContext {

        public DbSet<Project> ProjectSet { get; set; }
        public DbSet<RuleWeight> RuleWeightSet { get; set; }
        public DbSet<Target> TargetSet { get; set; }
        public DbSet<FilterPlan> FilterPlanSet { get; set; }
        public DbSet<FilterPreference> FilterPreferenceSet { get; set; }
        public DbSet<AcquiredImage> AcquiredImageSet { get; set; }

        public AssistantDatabaseContext(string connectionString) : base(new SQLiteConnection() { ConnectionString = connectionString }, true) {
            Configuration.LazyLoadingEnabled = false;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            Logger.Debug("Assistant database: OnModelCreating");

            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Configurations.Add(new ProjectConfiguration());
            modelBuilder.Configurations.Add(new FilterPreferenceConfiguration());
            modelBuilder.Configurations.Add(new AcquiredImageConfiguration());

            var sqi = new CreateOrMigrateDatabaseInitializer<AssistantDatabaseContext>();
            System.Data.Entity.Database.SetInitializer(sqi);
        }

        public List<Project> GetAllProjects(string profileId) {
            var projects = ProjectSet.Include("targets.filterplans").Include("ruleweights").Where(p => p.ProfileId.Equals(profileId));
            return projects.ToList();
        }

        public List<Project> GetActiveProjects(string profileId, DateTime atTime) {
            long secs = DateTimeToUnixSeconds(atTime);
            var projects = ProjectSet.Include("targets.filterplans").Include("ruleweights").Where(p =>
                p.ProfileId.Equals(profileId) &&
                p.state_col == (int)ProjectState.Active &&
                p.startDate <= secs && secs <= p.endDate);
            return projects.ToList();
        }

        public List<FilterPreference> GetFilterPreferences(string profileId) {
            var filterPrefs = FilterPreferenceSet.Where(p => p.profileId == profileId);
            return filterPrefs.ToList();
        }

        public Target GetTarget(int projectId, int targetId) {
            return TargetSet.Include("filterplans").Where(t => t.Project.Id == projectId && t.Id == targetId).First();
        }

        public FilterPlan GetFilterPlan(int targetId, string filterName) {
            return FilterPlanSet.Where(f => f.targetId == targetId && f.filterName == filterName).First();
        }

        public List<AcquiredImage> GetAcquiredImages(int targetId, string filterName) {
            var images = AcquiredImageSet.Where(p =>
                p.TargetId == targetId &&
                p.FilterName == filterName)
              .OrderByDescending(p => p.acquiredDate);
            return images.ToList();
        }

        public bool SaveProject(Project project) {
            Logger.Debug($"Assistant: saving Project Id={project.Id} Name={project.Name}");
            using (var transaction = Database.BeginTransaction()) {
                try {
                    ProjectSet.AddOrUpdate(project);
                    project.RuleWeights.ForEach(item => RuleWeightSet.AddOrUpdate(item));
                    SaveChanges();
                    transaction.Commit();
                    return true;
                }
                catch (Exception e) {
                    Logger.Error($"Assistant: error persisting Project: {e.Message} {e.StackTrace}");
                    return false;
                }
            }
        }

        public bool SaveTarget(Target target) {
            Logger.Debug($"Assistant: saving Target Id={target.Id} Name={target.Name}");
            using (var transaction = Database.BeginTransaction()) {
                try {
                    TargetSet.AddOrUpdate(target);
                    SaveChanges();
                    transaction.Commit();
                    return true;
                }
                catch (Exception e) {
                    Logger.Error($"Assistant: error persisting Target: {e.Message} {e.StackTrace}");
                    return false;
                }
            }
        }

        public bool SaveFilterPlan(FilterPlan filterPlan) {
            Logger.Debug($"Assistant: saving Filter Plan Id={filterPlan.Id} Name={filterPlan.FilterName}");
            using (var transaction = Database.BeginTransaction()) {
                try {
                    FilterPlanSet.AddOrUpdate(filterPlan);
                    SaveChanges();
                    transaction.Commit();
                    return true;
                }
                catch (Exception e) {
                    Logger.Error($"Assistant: error persisting Filter Plan: {e.Message} {e.StackTrace}");
                    return false;
                }
            }
        }

        public bool SaveFilterPreference(FilterPreference filterPreference) {
            Logger.Debug($"Assistant: saving Filter Preferences Id={filterPreference.Id} Name={filterPreference.FilterName}");
            using (var transaction = Database.BeginTransaction()) {
                try {
                    FilterPreferenceSet.AddOrUpdate(filterPreference);
                    SaveChanges();
                    transaction.Commit();
                    return true;
                }
                catch (Exception e) {
                    Logger.Error($"Assistant: error persisting Filter Preferences: {e.Message} {e.StackTrace}");
                    return false;
                }
            }
        }

        public static long DateTimeToUnixSeconds(DateTime? dateTime) {
            return dateTime == null ? 0 : CoreUtil.DateTimeToUnixTimeStamp((DateTime)dateTime);
        }

        public static DateTime UnixSecondsToDateTime(long? seconds) {
            return CoreUtil.UnixTimeStampToDateTime(seconds == null ? 0 : seconds.Value);
        }

        private class CreateOrMigrateDatabaseInitializer<TContext> : CreateDatabaseIfNotExists<TContext>,
                IDatabaseInitializer<TContext> where TContext : AssistantDatabaseContext {

            void IDatabaseInitializer<TContext>.InitializeDatabase(TContext context) {

                if (!DatabaseExists(context)) {

                    Logger.Debug("Assistant database: creating database schema");
                    try {
                        context.Database.BeginTransaction();

                        // TODO: make this locate in the Assembly ...
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
