using Assistant.NINAPlugin.Database.Schema;
using NINA.Plugin.Assistant.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class OverrideExposureOrder {
        public static readonly string DITHER = "Dither";
        public static readonly char SEP = '|';

        private List<OverrideItem> overrideItems = new List<OverrideItem>();

        public List<OverrideItem> OverrideItems {
            get => overrideItems; set => overrideItems = value;
        }

        public OverrideExposureOrder(List<ExposurePlan> exposurePlans) {
            for (int i = 0; i < exposurePlans.Count; i++) {
                OverrideItems.Add(new OverrideItem(exposurePlans[i], exposurePlans[i].Id));
            }
        }

        public OverrideExposureOrder(string serialized, List<ExposurePlan> exposurePlans) {
            if (String.IsNullOrEmpty(serialized)) {
                return;
            }

            string[] items = serialized.Split(SEP);
            foreach (string item in items) {
                if (item == DITHER) {
                    OverrideItems.Add(new OverrideItem());
                } else {
                    int databaseId = 0;
                    Int32.TryParse(item, out databaseId);
                    ExposurePlan ep = exposurePlans.Find(e => e.Id == databaseId);
                    if (ep != null) {
                        OverrideItems.Add(new OverrideItem(ep, databaseId));
                    }
                }
            }
        }

        public string Serialize() {
            if (OverrideItems?.Count == 0) {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            foreach (OverrideItem item in OverrideItems) {
                sb.Append(item.Serialize()).Append(SEP);
            }

            return sb.ToString().TrimEnd(SEP);
        }

        public ObservableCollection<OverrideItem> GetDisplayList() {
            return new ObservableCollection<OverrideItem>(OverrideItems);
        }

        public static string Remap(string srcOverrideExposureOrder, List<ExposurePlan> srcExposurePlans, List<ExposurePlan> newExposurePlans) {
            if (string.IsNullOrEmpty(srcOverrideExposureOrder)) { return null; }
            if (srcExposurePlans?.Count != newExposurePlans?.Count) { return null; }

            List<Tuple<int, int>> map = new List<Tuple<int, int>>();
            for (int i = 0; i < srcExposurePlans.Count; i++) {
                map.Add(new Tuple<int, int>(srcExposurePlans[i].Id, newExposurePlans[i].Id));
            }

            OverrideExposureOrder overrideExposureOrder = new OverrideExposureOrder(srcOverrideExposureOrder, srcExposurePlans);
            StringBuilder sb = new StringBuilder();

            foreach (OverrideItem item in overrideExposureOrder.OverrideItems) {
                if (item.IsDither) {
                    sb.Append(DITHER).Append(SEP);
                    continue;
                }

                Tuple<int, int> entry = map.FirstOrDefault(i => i.Item1 == item.ExposurePlanDatabaseId);
                if (entry != null) {
                    sb.Append(entry.Item2).Append(SEP);
                } else {
                    TSLogger.Warning($"failed to find EP ID while remapping pasted exposure plans");
                    return null;
                }
            }

            return sb.ToString().TrimEnd(SEP);
        }
    }

    public class OverrideItem {
        public int ExposurePlanDatabaseId { get; private set; }
        public bool IsDither { get; private set; }
        public string Name { get; private set; }

        public OverrideItem() {
            IsDither = true;
            ExposurePlanDatabaseId = -1;
            Name = OverrideExposureOrder.DITHER;
        }

        public OverrideItem(ExposurePlan exposurePlan, int exposurePlanDatabaseId) {
            IsDither = false;
            ExposurePlanDatabaseId = exposurePlanDatabaseId;
            Name = exposurePlan.ExposureTemplate.Name;
        }

        public OverrideItem Clone() {
            return new OverrideItem {
                IsDither = IsDither,
                ExposurePlanDatabaseId = ExposurePlanDatabaseId,
                Name = Name
            };
        }

        public string Serialize() {
            return IsDither ? OverrideExposureOrder.DITHER : ExposurePlanDatabaseId.ToString();
        }
    }
}