using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Controls.AcquiredImages;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan.Scoring.Rules;
using LinqKit;
using NINA.Core.Utility;
using NINA.Plugin.Assistant.Shared.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace Assistant.NINAPlugin.Database {

    public class SchedulerDatabaseContext : DbContext {

        public DbSet<ProfilePreference> ProfilePreferenceSet { get; set; }
        public DbSet<Project> ProjectSet { get; set; }
        public DbSet<RuleWeight> RuleWeightSet { get; set; }
        public DbSet<Target> TargetSet { get; set; }
        public DbSet<ExposurePlan> ExposurePlanSet { get; set; }
        public DbSet<ExposureTemplate> ExposureTemplateSet { get; set; }
        public DbSet<AcquiredImage> AcquiredImageSet { get; set; }
        public DbSet<FlatHistory> FlatHistorySet { get; set; }
        public DbSet<ImageData> ImageDataSet { get; set; }

        public SchedulerDatabaseContext(string connectionString) : base(new SQLiteConnection() { ConnectionString = connectionString }, true) {
            Configuration.LazyLoadingEnabled = false;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            TSLogger.Debug("Scheduler database: OnModelCreating");

            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Configurations.Add(new ProjectConfiguration());
            modelBuilder.Configurations.Add(new TargetConfiguration());
            modelBuilder.Configurations.Add(new ExposureTemplateConfiguration());
            modelBuilder.Configurations.Add(new AcquiredImageConfiguration());

            var sqi = new CreateOrMigrateDatabaseInitializer<SchedulerDatabaseContext>();
            System.Data.Entity.Database.SetInitializer(sqi);
        }

        public ProfilePreference GetProfilePreference(string profileId) {
            return ProfilePreferenceSet.Where(p => p.ProfileId.Equals(profileId)).FirstOrDefault();
        }

        public List<Project> GetAllProjects() {
            return ProjectSet
                .Include("targets.exposureplans.exposuretemplate")
                .Include("ruleweights")
                .ToList();
        }

        public List<Project> GetAllProjects(string profileId) {
            return ProjectSet
                .Include("targets.exposureplans.exposuretemplate")
                .Include("ruleweights")
                .Where(p => p.ProfileId.Equals(profileId))
                .ToList();
        }

        public List<Project> GetOrphanedProjects(List<string> currentProfileIdList) {
            return ProjectSet
                .Include("targets.exposureplans.exposuretemplate")
                .Include("ruleweights")
                .Where(p => !currentProfileIdList.Contains(p.ProfileId))
                .ToList();
        }

        public List<Project> GetActiveProjects(string profileId) {
            var projects = ProjectSet
                .Include("targets.exposureplans.exposuretemplate")
                .Include("ruleweights")
                .Where(p =>
                p.ProfileId.Equals(profileId) &&
                p.state_col == (int)ProjectState.Active);
            return projects.ToList();
        }

        public bool HasActiveTargets(string profileId) {
            List<Project> projects = ProjectSet
                .AsNoTracking()
                .Include("targets")
                .Where(p =>
                p.ProfileId.Equals(profileId) &&
                p.state_col == (int)ProjectState.Active).ToList();

            foreach (Project project in projects) {
                foreach (Target target in project.Targets) {
                    if (target.Enabled) { return true; }
                }
            }

            return false;
        }

        public List<ExposureTemplate> GetExposureTemplates(string profileId) {
            return ExposureTemplateSet.Where(p => p.profileId == profileId).ToList();
        }

        public Project GetProject(int projectId) {
            return ProjectSet
                .Include("targets.exposureplans.exposuretemplate")
                .Include("ruleweights")
                .Where(p => p.Id == projectId)
                .FirstOrDefault();
        }

        public Target GetTargetOnly(int targetId) {
            return TargetSet
                .Where(t => t.Id == targetId)
                .FirstOrDefault();
        }

        public Target GetTarget(int projectId, int targetId) {
            return TargetSet
                .Include("exposureplans.exposuretemplate")
                .Where(t => t.Project.Id == projectId && t.Id == targetId)
                .FirstOrDefault();
        }

        public Target GetTargetByProject(int projectId, int targetId) {
            Project project = GetProject(projectId);
            return project.Targets.Where(t => t.Id == targetId).FirstOrDefault();
        }

        public ExposurePlan GetExposurePlan(int id) {
            return ExposurePlanSet
                .Include("exposuretemplate")
                .Where(p => p.Id == id)
                .FirstOrDefault();
        }

        public ExposureTemplate GetExposureTemplate(int id) {
            return ExposureTemplateSet.Where(e => e.Id == id).FirstOrDefault();
        }

        public List<AcquiredImage> GetAcquiredImages(int targetId, string filterName) {
            var images = AcquiredImageSet.Where(p =>
                p.TargetId == targetId &&
                p.FilterName == filterName)
              .OrderByDescending(p => p.acquiredDate);
            return images.ToList();
        }

        public List<AcquiredImage> GetAcquiredImages(int targetId) {
            var images = AcquiredImageSet.Where(p => p.TargetId == targetId)
                .AsNoTracking()
                .OrderByDescending(p => p.acquiredDate);
            return images.ToList();
        }

        public List<AcquiredImage> GetAcquiredImagesForGrading(int targetId, string filterName) {
            var images = AcquiredImageSet.AsNoTracking().Where(p =>
                p.TargetId == targetId &&
                p.FilterName == filterName &&
                p.accepted == 1)
              .OrderByDescending(p => p.acquiredDate);
            return images.ToList();
        }

        public int GetAcquiredImagesCount(DateTime olderThan, int targetId) {
            var predicate = PredicateBuilder.New<AcquiredImage>();
            long olderThanSecs = DateTimeToUnixSeconds(olderThan);
            predicate = predicate.And(a => a.acquiredDate < olderThanSecs);
            if (targetId != 0) {
                predicate = predicate.And(a => a.TargetId == targetId);
            }

            return AcquiredImageSet.AsNoTracking().AsExpandable().Where(predicate).Count();
        }

        public void DeleteAcquiredImages(DateTime olderThan, int targetId) {
            using (var transaction = Database.BeginTransaction()) {
                try {
                    var predicate = PredicateBuilder.New<AcquiredImage>();
                    long olderThanSecs = DateTimeToUnixSeconds(olderThan);
                    predicate = predicate.And(a => a.acquiredDate < olderThanSecs);
                    if (targetId != 0) {
                        predicate = predicate.And(a => a.TargetId == targetId);
                    }

                    AcquiredImageSet.RemoveRange(AcquiredImageSet.Where(predicate));
                    SaveChanges();
                    transaction.Commit();
                }
                catch (Exception e) {
                    TSLogger.Error($"error deleting acquired images: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                }
            }
        }

        public List<FlatHistory> GetFlatsHistory(DateTime lightSessionDate) {
            var predicate = PredicateBuilder.New<FlatHistory>();
            long lightSessionDateSecs = DateTimeToUnixSeconds(lightSessionDate);
            predicate = predicate.And(f => f.lightSessionDate == lightSessionDateSecs);
            return FlatHistorySet.AsNoTracking().Where(predicate).ToList();
        }

        public List<FlatHistory> GetFlatsHistory(int targetId) {
            return FlatHistorySet.AsNoTracking().Where(fh => fh.targetId == targetId).ToList();
        }

        public ImageData GetImageData(int acquiredImageId) {
            return ImageDataSet.Where(d => d.AcquiredImageId == acquiredImageId).FirstOrDefault();
        }

        public ImageData GetImageData(int acquiredImageId, string tag) {
            return ImageDataSet.Where(d =>
                d.AcquiredImageId == acquiredImageId &&
                d.tag == tag).FirstOrDefault();
        }

        public ProfilePreference SaveProfilePreference(ProfilePreference profilePreference) {
            TSLogger.Debug($"saving ProfilePreference Id={profilePreference.Id}");
            using (var transaction = Database.BeginTransaction()) {
                try {
                    ProfilePreferenceSet.AddOrUpdate(profilePreference);
                    SaveChanges();
                    transaction.Commit();
                    return GetProfilePreference(profilePreference.ProfileId);
                }
                catch (Exception e) {
                    TSLogger.Error($"error persisting project: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return null;
                }
            }
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
                    TSLogger.Error($"error adding new project: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return null;
                }
            }
        }

        public Project SaveProject(Project project) {
            TSLogger.Debug($"saving Project Id={project.Id} Name={project.Name}");
            using (var transaction = Database.BeginTransaction()) {
                try {
                    ProjectSet.AddOrUpdate(project);
                    project.RuleWeights.ForEach(item => RuleWeightSet.AddOrUpdate(item));
                    SaveChanges();
                    transaction.Commit();
                    return GetProject(project.Id);
                }
                catch (Exception e) {
                    TSLogger.Error($"error persisting project: {e.Message} {e.StackTrace}");
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
                    TSLogger.Error($"error pasting project: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return null;
                }
            }
        }

        public Project MoveProject(Project project, string profileId) {
            using (var transaction = Database.BeginTransaction()) {
                try {
                    Project copy = project.GetPasteCopy(profileId);
                    ProjectSet.Add(copy);

                    project = GetProject(project.Id);
                    ProjectSet.Remove(project);
                    SaveChanges();
                    transaction.Commit();
                    return copy;
                }
                catch (Exception e) {
                    TSLogger.Error($"error moving project: {e.Message} {e.StackTrace}");
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
                    TSLogger.Error($"error deleting project: {e.Message} {e.StackTrace}");
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
                    TSLogger.Error($"error adding new target: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return null;
                }
            }
        }

        public Target SaveTarget(Target target) {
            TSLogger.Debug($"saving Target Id={target.Id} Name={target.Name}");
            using (var transaction = Database.BeginTransaction()) {
                try {
                    TargetSet.AddOrUpdate(target);
                    target.ExposurePlans.ForEach(plan => {
                        plan.ExposureTemplate = null; // clear this (ExposureTemplateId handles the relation)
                        ExposurePlanSet.AddOrUpdate(plan);
                        plan.ExposureTemplate = GetExposureTemplate(plan.ExposureTemplateId); // add back for UI
                    });

                    SaveChanges();
                    transaction.Commit();
                    return GetTarget(target.Project.Id, target.Id);
                }
                catch (Exception e) {
                    TSLogger.Error($"error persisting target: {e.Message} {e.StackTrace}");
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
                    TSLogger.Error($"error pasting target: {e.Message} {e.StackTrace}");
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
                    TSLogger.Error($"error deleting target: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return false;
                }
            }
        }

        public bool SaveExposurePlan(ExposurePlan exposurePlan) {
            TSLogger.Debug($"saving Exposure Plan Id={exposurePlan.Id}");
            using (var transaction = Database.BeginTransaction()) {
                try {
                    ExposurePlanSet.AddOrUpdate(exposurePlan);
                    SaveChanges();
                    transaction.Commit();
                    return true;
                }
                catch (Exception e) {
                    TSLogger.Error($"error persisting exposure plan: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return false;
                }
            }
        }

        public Target DeleteExposurePlan(Target target, ExposurePlan exposurePlan) {
            using (var transaction = Database.BeginTransaction()) {
                try {
                    TargetSet.AddOrUpdate(target);
                    exposurePlan = GetExposurePlan(exposurePlan.Id);
                    ExposurePlanSet.Remove(exposurePlan);
                    SaveChanges();
                    transaction.Commit();
                    return GetTargetByProject(target.ProjectId, target.Id);
                }
                catch (Exception e) {
                    TSLogger.Error($"error deleting exposure plan: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return null;
                }
            }
        }

        public Target DeleteAllExposurePlans(Target target) {
            using (var transaction = Database.BeginTransaction()) {
                try {
                    TargetSet.AddOrUpdate(target);

                    List<ExposurePlan> eps = ExposurePlanSet.Where(p => p.TargetId == target.Id).ToList();
                    foreach (ExposurePlan ep in eps) {
                        ExposurePlanSet.Remove(ep);
                    }

                    SaveChanges();
                    transaction.Commit();
                    return GetTargetByProject(target.ProjectId, target.Id);
                }
                catch (Exception e) {
                    TSLogger.Error($"error deleting all exposure plans: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return null;
                }
            }
        }

        public ExposureTemplate SaveExposureTemplate(ExposureTemplate exposureTemplate) {
            TSLogger.Debug($"saving Exposure Template Id={exposureTemplate.Id} Name={exposureTemplate.Name}");
            using (var transaction = Database.BeginTransaction()) {
                try {
                    ExposureTemplateSet.AddOrUpdate(exposureTemplate);
                    SaveChanges();
                    transaction.Commit();
                    return GetExposureTemplate(exposureTemplate.Id);
                }
                catch (Exception e) {
                    TSLogger.Error($"error persisting exposure template: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return null;
                }
            }
        }

        public ExposureTemplate PasteExposureTemplate(string profileId, ExposureTemplate source) {
            using (var transaction = Database.BeginTransaction()) {
                try {
                    ExposureTemplate exposureTemplate = source.GetPasteCopy(profileId);
                    ExposureTemplateSet.Add(exposureTemplate);
                    SaveChanges();
                    transaction.Commit();
                    return GetExposureTemplate(exposureTemplate.Id);
                }
                catch (Exception e) {
                    TSLogger.Error($"error pasting exposure template: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return null;
                }
            }
        }

        public bool DeleteExposureTemplate(ExposureTemplate exposureTemplate) {
            using (var transaction = Database.BeginTransaction()) {
                try {
                    exposureTemplate = GetExposureTemplate(exposureTemplate.Id);
                    ExposureTemplateSet.Remove(exposureTemplate);
                    SaveChanges();
                    transaction.Commit();
                    return true;
                }
                catch (Exception e) {
                    TSLogger.Error($"error deleting exposure template: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return false;
                }
            }
        }

        public void AddExposureTemplates(List<ExposureTemplate> exposureTemplates) {
            using (var transaction = Database.BeginTransaction()) {
                try {
                    exposureTemplates.ForEach(exposureTemplate => {
                        ExposureTemplateSet.AddOrUpdate(exposureTemplate);
                    });

                    SaveChanges();
                    transaction.Commit();
                }
                catch (Exception e) {
                    TSLogger.Error($"error adding exposure template: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                }
            }
        }

        public ExposureTemplate MoveExposureTemplate(ExposureTemplate exposureTemplate, string profileId) {
            using (var transaction = Database.BeginTransaction()) {
                try {
                    ExposureTemplate copy = exposureTemplate.GetPasteCopy(profileId);
                    ExposureTemplateSet.Add(copy);

                    exposureTemplate = GetExposureTemplate(exposureTemplate.Id);
                    ExposureTemplateSet.Remove(exposureTemplate);
                    SaveChanges();
                    transaction.Commit();
                    return copy;
                }
                catch (Exception e) {
                    TSLogger.Error($"error moving exposure template: {e.Message} {e.StackTrace}");
                    RollbackTransaction(transaction);
                    return null;
                }
            }
        }

        public List<ExposureTemplate> GetOrphanedExposureTemplates(List<string> currentProfileIdList) {
            return ExposureTemplateSet.Where(et => !currentProfileIdList.Contains(et.profileId)).ToList();
        }

        public static long DateTimeToUnixSeconds(DateTime? dateTime) {
            return dateTime == null ? 0 : CoreUtil.DateTimeToUnixTimeStamp((DateTime)dateTime);
        }

        public static DateTime UnixSecondsToDateTime(long? seconds) {
            return CoreUtil.UnixTimeStampToDateTime(seconds == null ? 0 : seconds.Value);
        }

        private static void RollbackTransaction(DbContextTransaction transaction) {
            try {
                TSLogger.Warning("rolling back database changes");
                transaction.Rollback();
            }
            catch (Exception e) {
                TSLogger.Error($"error executing transaction rollback: {e.Message} {e.StackTrace}");
            }
        }

        private class CreateOrMigrateDatabaseInitializer<TContext> : CreateDatabaseIfNotExists<TContext>,
                IDatabaseInitializer<TContext> where TContext : SchedulerDatabaseContext {

            void IDatabaseInitializer<TContext>.InitializeDatabase(TContext context) {

                if (!DatabaseExists(context)) {
                    TSLogger.Debug("creating database schema");
                    using (var transaction = context.Database.BeginTransaction()) {
                        try {
                            context.Database.ExecuteSqlCommand(GetInitialSQL());
                            transaction.Commit();
                        }
                        catch (Exception e) {
                            Logger.Error($"error creating or initializing database: {e.Message} {e.StackTrace}");
                            TSLogger.Error($"error creating or initializing database: {e.Message} {e.StackTrace}");
                            RollbackTransaction(transaction);
                        }
                    }
                }

                // Apply any new migration scripts
                int version = context.Database.SqlQuery<int>("PRAGMA user_version").First();
                Dictionary<int, string> migrationScripts = GetMigrationSQL();
                foreach (KeyValuePair<int, string> scriptEntry in migrationScripts.OrderBy(entry => entry.Key)) {

                    if (scriptEntry.Key <= version) {
                        continue;
                    }

                    TSLogger.Info($"applying database migration script number {scriptEntry.Key}");
                    using (var transaction = context.Database.BeginTransaction()) {
                        try {
                            context.Database.ExecuteSqlCommand(scriptEntry.Value);
                            transaction.Commit();
                        }
                        catch (Exception e) {
                            Logger.Error($"Scheduler: error applying database migration script number {scriptEntry.Key}: {e.Message} {e.StackTrace}");
                            TSLogger.Error($"error applying database migration script number {scriptEntry.Key}: {e.Message} {e.StackTrace}");
                            RollbackTransaction(transaction);
                        }
                    }
                }

                int newVersion = context.Database.SqlQuery<int>("PRAGMA user_version").First();

                // Other repairs/updates
                RepairAndUpdate(version, newVersion, context);

                if (newVersion != version) {
                    TSLogger.Info($"database updated: {version} -> {newVersion}");
                }
            }

            private bool DatabaseExists(TContext context) {
                int numTables = context.Database.SqlQuery<int>("SELECT COUNT(*) FROM sqlite_master AS TABLES WHERE TYPE = 'table'").First();
                return numTables > 0;
            }

            private string GetInitialSQL() {
                try {
                    ResourceManager rm = new ResourceManager("Assistant.NINAPlugin.Database.Initial.SQL", Assembly.GetExecutingAssembly());
                    return (string)rm.GetObject("initial_schema");
                }
                catch (Exception ex) {
                    Logger.Error($"failed to load Scheduler database initial SQL: {ex.Message}");
                    TSLogger.Error($"failed to load database initial SQL: {ex.Message}");
                    throw;
                }
            }

            private Dictionary<int, string> GetMigrationSQL() {
                try {
                    Dictionary<int, string> migrateScripts = new Dictionary<int, string>();
                    ResourceManager rm = new ResourceManager("Assistant.NINAPlugin.Database.Migrate.SQL", Assembly.GetExecutingAssembly());
                    ResourceSet rs = rm.GetResourceSet(System.Globalization.CultureInfo.InvariantCulture, true, false);

                    foreach (DictionaryEntry entry in rs) {
                        if (Int32.TryParse((string)entry.Key, out int migrateNum)) {
                            migrateScripts.Add(migrateNum, (string)entry.Value);
                        }
                    }

                    return migrateScripts;
                }
                catch (Exception ex) {
                    Logger.Error($"failed to load Scheduler database migration scripts: {ex.Message}");
                    TSLogger.Error($"failed to load database migration scripts: {ex.Message}");
                    throw;
                }
            }

            private void RepairAndUpdate(int oldVersion, int newVersion, TContext context) {

                // If a new scoring rule was added, we need to add a rule weight record to projects that don't have it
                List<Project> projects = context.GetAllProjects();
                if (projects != null && projects.Count > 0) {
                    bool updated = false;
                    Dictionary<string, IScoringRule> rules = ScoringRule.GetAllScoringRules();
                    foreach (Project project in projects) {
                        foreach (KeyValuePair<string, IScoringRule> item in rules) {
                            RuleWeight rw = project.RuleWeights.Where(r => r.Name == item.Key).FirstOrDefault();
                            if (rw == null) {
                                TSLogger.Info($"project '{project.Name}' is missing rule weight record: '{item.Value.Name}': adding");
                                rw = new RuleWeight(item.Value.Name, item.Value.DefaultWeight);
                                rw.Project = project;
                                context.RuleWeightSet.Add(rw);
                                updated = true;
                            }
                        }
                    }

                    if (updated) {
                        context.SaveChanges();
                    }
                }

                // Convert NINA 2 rotation to NINA 3 position angle
                if (oldVersion == 5 && newVersion == 6) {
                    projects = context.GetAllProjects();
                    if (projects != null && projects.Count > 0) {
                        bool updated = false;
                        foreach (Project project in projects) {
                            foreach (Target target in project.Targets) {
                                double rotation = target.Rotation;
                                if (rotation != 0) {
                                    target.Rotation = AstrometryUtils.ConvertRotation(rotation);
                                    updated = true;
                                }
                            }
                        }

                        if (updated) {
                            context.SaveChanges();
                            TSLogger.Info("updated target rotation values for NINA 3");
                        }
                    }
                }

                // Clear override exposure order (meaning changed with bug fix)
                if (oldVersion == 8 && newVersion == 9) {
                    projects = context.GetAllProjects();
                    if (projects != null && projects.Count > 0) {
                        bool updated = false;
                        foreach (Project project in projects) {
                            foreach (Target target in project.Targets) {
                                if (!string.IsNullOrEmpty(target.OverrideExposureOrder)) {
                                    target.OverrideExposureOrder = null;
                                    updated = true;
                                }
                            }
                        }

                        if (updated) {
                            context.SaveChanges();
                            TSLogger.Info("cleared override exposure ordering for bug fix");
                        }
                    }
                }
            }

        }
    }
}
