using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Assistant.NINAPlugin.Database.Schema {

    public class Project {

        public const int STATE_DRAFT = 0;
        public const int STATE_ACTIVE = 1;
        public const int STATE_INACTIVE = 2;
        public const int STATE_CLOSED = 3;

        public const int PRIORITY_LOW = 0;
        public const int PRIORITY_NORMAL = 1;
        public const int PRIORITY_HIGH = 2;

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
            state = STATE_DRAFT;
            priority = PRIORITY_NORMAL;
            createDate = AssistantDbContext.DateTimeToUnixSeconds(DateTime.Now);
            targets = new List<Target>();
        }

        public static string State(int state) {
            switch (state) {
                case STATE_DRAFT: return "draft";
                case STATE_ACTIVE: return "active";
                case STATE_INACTIVE: return "inactive";
                case STATE_CLOSED: return "closed";
                default: return "unknown";
            }
        }

        public static string Priority(int priority) {
            switch (priority) {
                case PRIORITY_LOW: return "low";
                case PRIORITY_NORMAL: return "normal";
                case PRIORITY_HIGH: return "high";
                default: return "unknown";
            }
        }

    }

}
