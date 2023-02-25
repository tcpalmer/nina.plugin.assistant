﻿using NINA.Core.Model.Equipment;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text;

namespace Assistant.NINAPlugin.Database.Schema {

    public class ExposurePlan : INotifyPropertyChanged {

        [Key] public int Id { get; set; }
        [Required] public string filterName { get; set; }
        [Required] public string profileId { get; set; }
        [Required] public double exposure { get; set; }

        public int gain { get; set; }
        public int offset { get; set; }
        public int? bin { get; set; }
        public int readoutMode { get; set; }

        public int desired { get; set; }
        public int acquired { get; set; }
        public int accepted { get; set; }

        [ForeignKey("Target")] public int TargetId { get; set; }
        public virtual Target Target { get; set; }

        [NotMapped]
        public string FilterName {
            get { return filterName; }
            set {
                filterName = value;
                RaisePropertyChanged(nameof(FilterName));
            }
        }

        [NotMapped]
        public string ProfileId {
            get { return profileId; }
            set {
                profileId = value;
                RaisePropertyChanged(nameof(ProfileId));
            }
        }

        [NotMapped]
        public double Exposure {
            get { return exposure; }
            set {
                exposure = value;
                RaisePropertyChanged(nameof(Exposure));
            }
        }

        [NotMapped]
        public int Gain {
            get { return gain; }
            set {
                gain = value;
                RaisePropertyChanged(nameof(Gain));
            }
        }

        [NotMapped]
        public int Offset {
            get { return offset; }
            set {
                offset = value;
                RaisePropertyChanged(nameof(Offset));
            }
        }

        [NotMapped]
        public BinningMode BinningMode {
            get { return new BinningMode((short)bin, (short)bin); }
            set {
                bin = value.X;
                RaisePropertyChanged(nameof(BinningMode));
            }
        }

        [NotMapped]
        public int ReadoutMode {
            get { return readoutMode; }
            set {
                readoutMode = value;
                RaisePropertyChanged(nameof(ReadoutMode));
            }
        }

        [NotMapped]
        public int Desired {
            get { return desired; }
            set {
                desired = value;
                RaisePropertyChanged(nameof(Desired));
            }
        }

        [NotMapped]
        public int Acquired {
            get { return acquired; }
            set {
                acquired = value;
                RaisePropertyChanged(nameof(Acquired));
            }
        }

        [NotMapped]
        public int Accepted {
            get { return accepted; }
            set {
                accepted = value;
                RaisePropertyChanged(nameof(Accepted));
            }
        }

        public ExposurePlan() { }

        public ExposurePlan(string profileId, string filterName) {
            this.ProfileId = profileId;
            this.FilterName = filterName;
            Exposure = 60;
            Gain = -1;
            Offset = -1;
            BinningMode = new BinningMode(1, 1);
            ReadoutMode = -1;
            Desired = 1;
            Acquired = 0;
            Accepted = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ExposurePlan GetPasteCopy(string newProfileId) {
            ExposurePlan exposurePlan = new ExposurePlan();

            exposurePlan.filterName = filterName;
            exposurePlan.profileId = newProfileId;
            exposurePlan.exposure = exposure;
            exposurePlan.gain = gain;
            exposurePlan.offset = offset;
            exposurePlan.bin = bin;
            exposurePlan.readoutMode = readoutMode;
            exposurePlan.desired = desired;
            exposurePlan.acquired = 0;
            exposurePlan.accepted = 0;

            return exposurePlan;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"FilterName: {FilterName}");
            sb.AppendLine($"ProfileId: {ProfileId}");
            sb.AppendLine($"Exposure: {Exposure}");
            sb.AppendLine($"Gain: {Gain}");
            sb.AppendLine($"Offset: {Offset}");
            sb.AppendLine($"BinningMode: {BinningMode}");
            sb.AppendLine($"ReadoutMode: {ReadoutMode}");
            sb.AppendLine($"Desired: {Desired}");
            sb.AppendLine($"Acquired: {Acquired}");
            sb.AppendLine($"Accepted: {Accepted}");

            return sb.ToString();
        }
    }
}