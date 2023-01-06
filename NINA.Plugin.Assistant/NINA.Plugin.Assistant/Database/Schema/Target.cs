using NINA.Astrometry;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Assistant.NINAPlugin.Database.Schema {

    public class Target {

        public const int EPOCH_JNOW = 0;
        public const int EPOCH_J2000 = 1;

        [Key]
        public int id { get; set; }

        [Required]
        public string name { get; set; }

        [Required]
        public double ra { get; set; }

        [Required]
        public double dec { get; set; }

        [Required]
        public int epochcode { get; set; }

        [NotMapped]
        public Epoch Epoch { get => MapEpoch(epochcode); }

        public double rotation { get; set; }
        public double roi { get; set; }

        public virtual Project project { get; set; }

        public List<FilterPlan> filterplans { get; set; }

        public Target() {
            epochcode = EPOCH_J2000;
            rotation = 0;
            roi = 1;
            filterplans = new List<FilterPlan>();
        }

        public Coordinates GetCoordinates() {
            return new Coordinates(Angle.ByHours(ra), Angle.ByDegree(dec), Epoch.J2000);
        }

        private Epoch MapEpoch(int epoch) {
            switch (epoch) {
                case EPOCH_JNOW: return Epoch.JNOW;
                case EPOCH_J2000: return Epoch.J2000;
                default: return Epoch.J2000;
            }
        }
    }

}
