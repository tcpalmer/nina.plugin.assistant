using CsvHelper;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace NINA.Plugin.Assistant.Test.Util {

    [TestFixture]
    public class MessierMarathon2024 {
        /*
         * Messier source list was from https://astropixels.com/messier/messiercat.html
         * Unfortunately, it only has RA to fractional arcminutes and Dec to just arcminutes so
         * have to expand to cover arcseconds.
         *
         * Massaged into CSV and added a couple of other known common names.  Following converts
         * that to TS CSV format for bulk import.
         */

        private const string DIR = @"G:\Photography\Astrophotography\Notes\Target Scheduler\Messier Marathon 2024";

        //[Test]
        public void ConvertCSV() {
            Dictionary<string, string> typeMap = GetTypeMap();
            string filePath = Path.Combine(DIR, "mm-refined.csv");

            List<CSVInRecord> records = Read(filePath);
            records.Count.Should().Be(110);

            List<CSVOutRecord> outRecords = new List<CSVOutRecord>();
            foreach (CSVInRecord record in records) {
                outRecords.Add(new CSVOutRecord(MapType(typeMap, record.Type), GetName(record.M, record.Common), FixRa(record.Ra), FixDec(record.Dec), "0", "100"));
            }

            TestContext.WriteLine(CSVOutRecord.ToHeader());
            foreach (CSVOutRecord record in outRecords) {
                TestContext.WriteLine(record.ToCSV());
            }
        }

        private string MapType(Dictionary<string, string> typeMap, string type) {
            if (typeMap.ContainsKey(type)) { return typeMap[type]; }
            return type;
        }

        private Dictionary<string, string> GetTypeMap() {
            Dictionary<string, string> typeMap = new Dictionary<string, string>();
            typeMap.Add("As", "Asterism");
            typeMap.Add("Ba", "Barred Galaxy");
            typeMap.Add("Di", "Diffuse Nebula");
            typeMap.Add("Ds", "Double Star");
            typeMap.Add("El", "Elliptical Galaxy");
            typeMap.Add("Gc", "Globular Cluster");
            typeMap.Add("Ir", "Irregular Galaxy");
            typeMap.Add("Ln", "Lenticular Galaxy");
            typeMap.Add("MW", "Milky Way Patch");
            typeMap.Add("Oc", "Open Cluster");
            typeMap.Add("Pl", "Planetary Nebula");
            typeMap.Add("Sn", "Supernova Remnant");
            typeMap.Add("Sp", "Spiral Galaxy");
            return typeMap;
        }

        private string GetName(string m, string common) {
            if (string.IsNullOrEmpty(common)) { return m; }
            return $"{m} {common}";
        }

        private string FixRa(string ra) {
            Regex re = new Regex(@"^(\d+)h ([0-9.]+)m$");
            Match match = re.Match(ra.Trim());
            if (match.Success) {
                string hours = match.Groups[1].Value;
                string minutes = match.Groups[2].Value;

                double arcmin = double.Parse(minutes);
                double arcsec = Math.Round((arcmin - Math.Truncate(arcmin)) * 60);
                arcmin = Math.Round(Math.Truncate(arcmin));

                return $"{hours}h {arcmin}m {arcsec}s";
            }

            return $"{ra} 0s";
        }

        private string FixDec(string dec) {
            return $"{dec} 00\"";
        }

        private List<CSVInRecord> Read(string filePath) {
            List<CSVInRecord> records = new List<CSVInRecord>();

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture)) {
                var recs = csv.GetRecords<CSVInRecord>();
                foreach (var rec in recs) {
                    records.Add(rec);
                }
            }

            return records;
        }
    }

    public class CSVInRecord {
        public string M { get; set; }
        public string Type { get; set; }
        public string Ra { get; set; }
        public string Dec { get; set; }
        public string Common { get; set; }
    }

    public class CSVOutRecord {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Ra { get; set; }
        public string Dec { get; set; }
        public string Rotation { get; set; }
        public string ROI { get; set; }

        public CSVOutRecord(string type, string name, string ra, string dec, string rotation, string roi) {
            Type = type;
            Name = name;
            Ra = ra;
            Dec = dec;
            Rotation = rotation;
            ROI = roi;
        }

        public string ToCSV() {
            return $"{Type},{Name},{Ra},{Dec},{Rotation},{ROI}";
        }

        public static string ToHeader() {
            return "Type,Name,Ra,Dec,Rotation,ROI";
        }
    }
}