using NINA.Core.Model.Equipment;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Assistant.NINAPlugin.Database.Schema {

    public class FlatHistory : IComparable {

        [Key] public int Id { get; set; }

        public int targetId { get; set; }
        public long lightSessionDate { get; set; }
        public long flatsTakenDate { get; set; }
        public string profileId { get; set; }
        public int numberFlatsTaken { get; set; }
        public string flatsType { get; set; }

        public string filterName { get; set; }
        public int gain { get; set; }
        public int offset { get; set; }
        public int? bin { get; set; }
        public int readoutMode { get; set; }
        public double rotation { get; set; }
        public double roi { get; set; }

        public FlatHistory() { }

        public FlatHistory(int targetId,
                           DateTime lightSessionDate,
                           DateTime flatsTakenDate,
                           string profileId,
                           int numberFlatsTaken,
                           string flatsType,
                           string filterName,
                           int gain,
                           int offset,
                           BinningMode binningMode,
                           int readoutMode,
                           double rotation,
                           double roi) {

            TargetId = targetId;
            LightSessionDate = lightSessionDate;
            FlatsTakenDate = flatsTakenDate;
            ProfileId = profileId;
            NumberFlatsTaken = numberFlatsTaken;
            FlatsType = flatsType;
            FilterName = filterName;
            Gain = gain;
            Offset = offset;
            BinningMode = binningMode;
            ReadoutMode = readoutMode;
            Rotation = rotation;
            ROI = roi;
        }

        [NotMapped]
        public int TargetId {
            get => targetId;
            set { targetId = value; }
        }

        [NotMapped]
        public DateTime LightSessionDate {
            get { return SchedulerDatabaseContext.UnixSecondsToDateTime(lightSessionDate); }
            set { lightSessionDate = SchedulerDatabaseContext.DateTimeToUnixSeconds(value); }
        }

        [NotMapped]
        public DateTime FlatsTakenDate {
            get { return SchedulerDatabaseContext.UnixSecondsToDateTime(flatsTakenDate); }
            set { flatsTakenDate = SchedulerDatabaseContext.DateTimeToUnixSeconds(value); }
        }

        [NotMapped]
        public string ProfileId {
            get { return profileId == null ? "" : profileId; }
            set {
                profileId = value;
            }
        }

        [NotMapped]
        public int NumberFlatsTaken {
            get => numberFlatsTaken;
            set { numberFlatsTaken = value; }
        }

        [NotMapped]
        public string FlatsType {
            get => flatsType;
            set { flatsType = value; }
        }

        [NotMapped]
        public string FilterName {
            get => filterName;
            set { filterName = value; }
        }

        [NotMapped]
        public int Gain {
            get { return gain; }
            set { gain = value; }
        }

        [NotMapped]
        public int Offset {
            get { return offset; }
            set { offset = value; }
        }

        [NotMapped]
        public BinningMode BinningMode {
            get { return new BinningMode((short)bin, (short)bin); }
            set { bin = value.X; }
        }

        [NotMapped]
        public int ReadoutMode {
            get { return readoutMode; }
            set { readoutMode = value; }
        }

        [NotMapped]
        public double Rotation {
            get { return rotation; }
            set { rotation = value; }
        }

        [NotMapped]
        public double ROI {
            get { return roi; }
            set { roi = value; }
        }

        public int CompareTo(object obj) {
            FlatHistory flatHistory = obj as FlatHistory;
            return (flatHistory != null) ? FlatsTakenDate.CompareTo(flatHistory.FlatsTakenDate) : 0;
        }
    }
}
