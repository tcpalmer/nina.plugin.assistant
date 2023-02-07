using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Assistant.NINAPlugin.Database.Schema {

    public enum ProjectState {
        Draft, Active, Inactive, Closed
    }

    public enum ProjectPriority {
        Low, Normal, High
    }

    public class Project {

        [Key]
        public int id { get; set; }

        [Required]
        public string profileid { get; set; }

        [Required]
        public string name { get; set; }

        public string description { get; set; }

        [Required]
        public int state { get; set; }

        [Required]
        public int priority { get; set; }

        public string profileId { get; set; }

        public long createDate { get; set; }
        public long? activeDate { get; set; }
        public long? inactiveDate { get; set; }
        public long? startDate { get; set; }
        public long? endDate { get; set; }

        [NotMapped]
        public ProjectState State {
            get { return (ProjectState)state; }
            set {
                state = (int)value;
            }
        }

        [NotMapped]
        public ProjectPriority Priority {
            get { return (ProjectPriority)priority; }
            set {
                state = (int)value;
            }
        }

        [NotMapped]
        public DateTime CreateDate {
            get { return AssistantDbContext.UnixSecondsToDateTime(createDate); }
            set { createDate = AssistantDbContext.DateTimeToUnixSeconds(value); }
        }

        [NotMapped]
        public DateTime? ActiveDate {
            get { return AssistantDbContext.UnixSecondsToDateTime(activeDate); }
            set { activeDate = AssistantDbContext.DateTimeToUnixSeconds(value); }
        }

        [NotMapped]
        public DateTime? InactiveDate {
            get { return AssistantDbContext.UnixSecondsToDateTime(inactiveDate); }
            set { inactiveDate = AssistantDbContext.DateTimeToUnixSeconds(value); }
        }

        [NotMapped]
        public DateTime? StartDate {
            get { return AssistantDbContext.UnixSecondsToDateTime(startDate); }
            set { startDate = AssistantDbContext.DateTimeToUnixSeconds(value); }
        }

        [NotMapped]
        public DateTime? EndDate {
            get { return AssistantDbContext.UnixSecondsToDateTime(endDate); }
            set { endDate = AssistantDbContext.DateTimeToUnixSeconds(value); }
        }

        public ProjectPreference preferences { get; set; }

        [NotMapped]
        public AssistantProjectPreferences ProjectPreferences {
            get { return preferences.Preferences; }
            set { preferences.Preferences = value; }
        }

        public List<Target> targets { get; set; }

        public Project() { }

        public Project(string profileId) {
            profileid = profileId;
            state = (int)ProjectState.Draft;
            priority = (int)ProjectPriority.Normal;
            createDate = AssistantDbContext.DateTimeToUnixSeconds(DateTime.Now);
            targets = new List<Target>();
        }

        public static string StateToString(ProjectState state) {
            switch (state) {
                case ProjectState.Draft: return "draft";
                case ProjectState.Active: return "active";
                case ProjectState.Inactive: return "inactive";
                case ProjectState.Closed: return "closed";
                default: return "unknown";
            }
        }

        public static string PriorityToString(ProjectPriority priority) {
            switch (priority) {
                case ProjectPriority.Low: return "low";
                case ProjectPriority.Normal: return "normal";
                case ProjectPriority.High: return "high";
                default: return "unknown";
            }
        }

    }

}
