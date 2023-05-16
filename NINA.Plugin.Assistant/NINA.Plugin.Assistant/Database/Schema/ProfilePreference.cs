using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Assistant.NINAPlugin.Database.Schema {

    public class ProfilePreference : INotifyPropertyChanged {

        [Key] public int Id { get; set; }
        [Required] public string ProfileId { get; set; }

        public int parkOnWait { get; set; }
        public double exposureThrottle { get; set; }

        public int enableGradeRMS { get; set; }
        public int enableGradeStars { get; set; }
        public int enableGradeHFR { get; set; }
        public int acceptimprovement { get; set; }

        public int maxGradingSampleSize { get; set; }
        public double rmsPixelThreshold { get; set; }
        public double detectedStarsSigmaFactor { get; set; }
        public double hfrSigmaFactor { get; set; }

        public ProfilePreference() { }

        public ProfilePreference(string profileId) {
            ProfileId = profileId;
            ParkOnWait = false;
            ExposureThrottle = 125;
            EnableGradeRMS = true;
            EnableGradeStars = true;
            EnableGradeHFR = true;
            AcceptImprovement = true;
            MaxGradingSampleSize = 10;
            RMSPixelThreshold = 8;
            DetectedStarsSigmaFactor = 4;
            HFRSigmaFactor = 4;
        }

        [NotMapped]
        public bool ParkOnWait {
            get { return parkOnWait == 1; }
            set {
                parkOnWait = value ? 1 : 0;
                RaisePropertyChanged(nameof(ParkOnWait));
            }
        }

        // exposurethrottle
        [NotMapped]
        public double ExposureThrottle {
            get { return exposureThrottle; }
            set {
                exposureThrottle = value;
                RaisePropertyChanged(nameof(ExposureThrottle));
            }
        }

        [NotMapped]
        public bool EnableGradeRMS {
            get { return enableGradeRMS == 1; }
            set {
                enableGradeRMS = value ? 1 : 0;
                RaisePropertyChanged(nameof(EnableGradeRMS));
            }
        }

        [NotMapped]
        public bool EnableGradeStars {
            get { return enableGradeStars == 1; }
            set {
                enableGradeStars = value ? 1 : 0;
                RaisePropertyChanged(nameof(EnableGradeStars));
            }
        }

        [NotMapped]
        public bool EnableGradeHFR {
            get { return enableGradeHFR == 1; }
            set {
                enableGradeHFR = value ? 1 : 0;
                RaisePropertyChanged(nameof(EnableGradeHFR));
            }
        }

        [NotMapped]
        public bool AcceptImprovement {
            get { return acceptimprovement == 1; }
            set {
                acceptimprovement = value ? 1 : 0;
                RaisePropertyChanged(nameof(AcceptImprovement));
            }
        }

        [NotMapped]
        public int MaxGradingSampleSize {
            get { return maxGradingSampleSize; }
            set {
                maxGradingSampleSize = value;
                RaisePropertyChanged(nameof(MaxGradingSampleSize));
            }
        }

        [NotMapped]
        public double RMSPixelThreshold {
            get { return rmsPixelThreshold; }
            set {
                rmsPixelThreshold = value;
                RaisePropertyChanged(nameof(RMSPixelThreshold));
            }
        }

        [NotMapped]
        public double DetectedStarsSigmaFactor {
            get { return detectedStarsSigmaFactor; }
            set {
                detectedStarsSigmaFactor = value;
                RaisePropertyChanged(nameof(DetectedStarsSigmaFactor));
            }
        }

        [NotMapped]
        public double HFRSigmaFactor {
            get { return hfrSigmaFactor; }
            set {
                hfrSigmaFactor = value;
                RaisePropertyChanged(nameof(HFRSigmaFactor));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
