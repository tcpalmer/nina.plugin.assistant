using Assistant.NINAPlugin.Database.Schema;
using CsvHelper;
using CsvHelper.Configuration;
using NINA.Astrometry;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Assistant.NINAPlugin.Controls.Util {
    // Approach cribbed from NINA SimpleSequenceVM.ImportTargets()

    public class CsvTargetLoader {

        public List<string> GetUniqueTypes(string filePath) {
            List<string> types = new List<string>();

            using (var reader = new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) {
                using (var csv = new CsvReader(reader, GetConfig())) {
                    csv.Read();
                    csv.ReadHeader();

                    while (csv.Read()) {
                        string type = string.Empty;
                        if (csv.TryGetField<string>("type", out type)) {
                            if (!string.IsNullOrEmpty(type) && !types.Contains(type)) {
                                types.Add(type);
                            }
                        }
                    }
                }
            }

            return types;
        }

        public List<Target> Load(string filePath, string typeFilter) {
            List<Target> targets = new List<Target>();

            using (var reader = new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) {
                using (var csv = new CsvReader(reader, GetConfig())) {
                    csv.Read();
                    csv.ReadHeader();

                    while (csv.Read()) {
                        string type = string.Empty;
                        if (typeFilter != null && csv.TryGetField<string>("type", out type) && type != typeFilter) {
                            continue;
                        }

                        string name = string.Empty;

                        // Telescopius format
                        if (csv.TryGetField<string>("familiar name", out name)) {
                            var catalogue = csv.GetField("catalogue entry");
                            var ra = AstroUtil.HMSToDegrees(csv.GetField("right ascension"));
                            var dec = AstroUtil.DMSToDegrees(csv.GetField("declination"));

                            double angle = 0;
                            if (csv.TryGetField<string>("position angle (east)", out var stringAngle) && !string.IsNullOrWhiteSpace(stringAngle)) {
                                angle = AstroUtil.EuclidianModulus(csv.GetField<double>("position angle (east)"), 360);
                            }

                            targets.Add(new Target {
                                Name = string.IsNullOrWhiteSpace(name) ? catalogue : name,
                                Coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000),
                                Rotation = angle
                            });
                        } else {
                            // Target Scheduler format
                            if (csv.TryGetField<string>("name", out name)) {
                                var ra = AstroUtil.HMSToDegrees(csv.GetField("ra"));
                                var dec = AstroUtil.DMSToDegrees(csv.GetField("dec"));

                                double rotation = 0;
                                if (csv.TryGetField<string>("rotation", out var stringRot) && !string.IsNullOrWhiteSpace(stringRot)) {
                                    rotation = AstroUtil.EuclidianModulus(csv.GetField<double>("rotation"), 360);
                                }

                                double roi = 100;
                                if (csv.TryGetField<string>("roi", out var stringRoi) && !string.IsNullOrWhiteSpace(stringRoi)) {
                                    roi = csv.GetField<double>("roi");
                                    if (roi < 1) { roi = 1; }
                                    if (roi > 100) { roi = 100; }
                                }

                                targets.Add(new Target {
                                    Name = name,
                                    Coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000),
                                    Rotation = rotation,
                                    ROI = roi
                                });
                            }
                        }
                    }
                }

                return targets;
            }
        }

        private CsvConfiguration GetConfig() {
            return new CsvConfiguration(CultureInfo.InvariantCulture) {
                BadDataFound = null,
                PrepareHeaderForMatch = args => args.Header.ToLower().Trim()
            };
        }
    }
}