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
        public DbSet<ExposurePlan> ExposurePlanSet { get; set; }
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
            modelBuilder.Configurations.Add(new TargetConfiguration());
            modelBuilder.Configurations.Add(new FilterPreferenceConfiguration());
            modelBuilder.Configurations.Add(new AcquiredImageConfiguration());

            var sqi = new CreateOrMigrateDatabaseInitializer<AssistantDatabaseContext>();
            System.Data.Entity.Database.SetInitializer(sqi);
        }

        public List<Project> GetAllProjects(string profileId) {
            return ProjectSet
                .Include("targets.exposureplans")
                .Include("ruleweights")
                .Where(p => p.ProfileId
                .Equals(profileId))
                .ToList();
        }

        public List<Project> GetActiveProjects(string profileId, DateTime atTime) {
            long secs = DateTimeToUnixSeconds(atTime);
            var projects = ProjectSet.Include("targets.exposureplans").Include("ruleweights").Where(p =>
                p.ProfileId.Equals(profileId) &&
                p.state_col == (int)ProjectState.Active &&
                p.startDate <= secs && secs <= p.endDate);
            return projects.ToList();
        }

        public List<FilterPreference> GetFilterPreferences(string profileId) {
            return FilterPreferenceSet.Where(p => p.profileId == profileId).ToList();
        }

        public Project GetProject(int projectId) {
            return ProjectSet
                .Include("targets.exposureplans")
                .Include("ruleweights")
                .Where(p => p.Id == projectId)
                .FirstOrDefault();
        }

        public Target GetTarget(int projectId, int targetId) {
            return TargetSet
                .Include("exposureplans")
                .Where(t => t.Project.Id == projectId && t.Id == targetId)
                .FirstOrDefault();
        }

        public Target GetTargetByProject(int projectId, int targetId) {
            Project project = GetProject(projectId);
            return project.Targets.Where(t => t.Id == targetId).FirstOrDefault();
        }

        public ExposurePlan GetExposurePlan(int id) {
            return ExposurePlanSet
                .Where(p => p.Id == id)
                .FirstOrDefault();
        }

        public ExposurePlan GetExposurePlan(int targetId, string filterName) {
            return ExposurePlanSet
                .Where(e => e.TargetId == targetId && e.filterName == filterName)
                .FirstOrDefault();
        }

        public List<AcquiredImage> GetAcquiredImages(int targetId, string filterName) {
            var images = AcquiredImageSet.Where(p =>
                p.TargetId == targetId &&
                p.FilterName == filterName)
              .OrderByDescending(p => p.acquiredDate);
            return images.ToList();
        }

        public Project AddNewProject(Project project) {
            using (var transaction = Database.BeginTransaction()) {
                try {
                    ProjectSet.Add(project);
                    SaveChanges();
                    transaction.Commit();
                    return GetProject(project.Id);
                }
                catch (Exception e) {
                    Logger.Error($"Assistant: error adding new project: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return null;
                }
            }
        }

        public Project SaveProject(Project project) {
            Logger.Debug($"Assistant: saving Project Id={project.Id} Name={project.Name}");
            using (var transaction = Database.BeginTransaction()) {
                try {
                    ProjectSet.AddOrUpdate(project);
                    project.RuleWeights.ForEach(item => RuleWeightSet.AddOrUpdate(item));
                    SaveChanges();
                    transaction.Commit();
                    return GetProject(project.Id);
                }
                catch (Exception e) {
                    Logger.Error($"Assistant: error persisting Project: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return null;
                }
            }
        }

        public Project PasteProject(string profileId, Project source) {
            using (var transaction = Database.BeginTransaction()) {
                try {
                    Project project = source.GetPasteCopy(profileId);
                    ProjectSet.Add(project);
                    SaveChanges();
                    transaction.Commit();
                    return GetProject(project.Id);
                }
                catch (Exception e) {
                    Logger.Error($"Assistant: error pasting project: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return null;
                }
            }
        }

        public bool DeleteProject(Project project) {
            using (var transaction = Database.BeginTransaction()) {
                try {
                    project = GetProject(project.Id);
                    ProjectSet.Remove(project);
                    SaveChanges();
                    transaction.Commit();
                    return true;
                }
                catch (Exception e) {
                    Logger.Error($"Assistant: error deleting project: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return false;
                }
            }
        }

        public Target AddNewTarget(Project project, Target target) {
            using (var transaction = Database.BeginTransaction()) {
                try {
                    project = GetProject(project.Id);
                    project.Targets.Add(target);
                    SaveChanges();
                    transaction.Commit();
                    return GetTarget(target.Project.Id, target.Id);
                }
                catch (Exception e) {
                    Logger.Error($"Assistant: error adding new target: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return null;
                }
            }
        }

        public Target SaveTarget(Target target) {
            Logger.Debug($"Assistant: saving Target Id={target.Id} Name={target.Name}");
            using (var transaction = Database.BeginTransaction()) {
                try {
                    TargetSet.AddOrUpdate(target);
                    target.ExposurePlans.ForEach(plan => { ExposurePlanSet.AddOrUpdate(plan); });

                    SaveChanges();
                    transaction.Commit();
                    return GetTarget(target.Project.Id, target.Id);
                }
                catch (Exception e) {
                    Logger.Error($"Assistant: error persisting Target: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return null;
                }
            }
        }

        public Target PasteTarget(Project project, Target source) {
            using (var transaction = Database.BeginTransaction()) {
                try {
                    Target target = source.GetPasteCopy(project.ProfileId);
                    project = GetProject(project.Id);
                    project.Targets.Add(target);
                    SaveChanges();
                    transaction.Commit();
                    return GetTarget(project.Id, target.Id);
                }
                catch (Exception e) {
                    Logger.Error($"Assistant: error pasting target: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return null;
                }
            }
        }

        public bool DeleteTarget(Target target) {
            using (var transaction = Database.BeginTransaction()) {
                try {
                    target = GetTarget(target.ProjectId, target.Id);
                    TargetSet.Remove(target);
                    SaveChanges();
                    transaction.Commit();
                    return true;
                }
                catch (Exception e) {
                    Logger.Error($"Assistant: error deleting target: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return false;
                }
            }
        }

        public bool SaveExposurePlan(ExposurePlan exposurePlan) {
            Logger.Debug($"Assistant: saving Exposure Plan Id={exposurePlan.Id} Name={exposurePlan.FilterName}");
            using (var transaction = Database.BeginTransaction()) {
                try {
                    ExposurePlanSet.AddOrUpdate(exposurePlan);
                    SaveChanges();
                    transaction.Commit();
                    return true;
                }
                catch (Exception e) {
                    Logger.Error($"Assistant: error persisting Exposure Plan: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return false;
                }
            }
        }

        public Target DeleteExposurePlan(Target target, ExposurePlan exposurePlan) {
            using (var transaction = Database.BeginTransaction()) {
                try {
                    exposurePlan = GetExposurePlan(exposurePlan.Id);
                    ExposurePlanSet.Remove(exposurePlan);
                    SaveChanges();
                    transaction.Commit();
                    return GetTargetByProject(target.ProjectId, target.Id);
                }
                catch (Exception e) {
                    Logger.Error($"Assistant: error deleting Filter Plan: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return null;
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
                    RollbackTransaction(transaction);
                    return false;
                }
            }
        }

        public void AddFilterPreferences(List<FilterPreference> filterPreferences) {
            using (var transaction = Database.BeginTransaction()) {
                try {
                    filterPreferences.ForEach(filterPreference => {
                        FilterPreferenceSet.AddOrUpdate(filterPreference);
                    });

                    SaveChanges();
                    transaction.Commit();
                }
                catch (Exception e) {
                    Logger.Error($"Assistant: error persisting Filter Preferences: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                }
            }
        }

        public static long DateTimeToUnixSeconds(DateTime? dateTime) {
            return dateTime == null ? 0 : CoreUtil.DateTimeToUnixTimeStamp((DateTime)dateTime);
        }

        public static DateTime UnixSecondsToDateTime(long? seconds) {
            return CoreUtil.UnixTimeStampToDateTime(seconds == null ? 0 : seconds.Value);
        }

        private static void RollbackTransaction(DbContextTransaction transaction) {
            try {
                Logger.Warning("Assistant: rolling back database changes");
                transaction.Rollback();
            }
            catch (Exception e) {
                Logger.Error($"Assistant: error executing transaction rollback: {e.Message} {e.StackTrace}");
            }
        }

        private class CreateOrMigrateDatabaseInitializer<TContext> : CreateDatabaseIfNotExists<TContext>,
                IDatabaseInitializer<TContext> where TContext : AssistantDatabaseContext {

            void IDatabaseInitializer<TContext>.InitializeDatabase(TContext context) {

                if (!DatabaseExists(context)) {
                    Logger.Debug("Assistant database: creating database schema");
                    using (var transaction = context.Database.BeginTransaction()) {
                        try {
                            // TODO: make this locate in the Assembly ...
                            string initial = "C:\\Users\\Tom\\source\\repos\\nina.plugin.assistant\\NINA.Plugin.Assistant\\NINA.Plugin.Assistant\\Database\\Initial";
                            var initial_schema = Path.Combine(initial, "initial_schema.sql");
                            context.Database.ExecuteSqlCommand(File.ReadAllText(initial_schema));
                            transaction.Commit();
                        }
                        catch (Exception e) {
                            Logger.Error($"Assistant: error creating or initializing database: {e.Message} {e.StackTrace}");
                            RollbackTransaction(transaction);
                        }
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
