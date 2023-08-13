using Accord.IO;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                OverrideItems.Add(new OverrideItem(exposurePlans[i], i));
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
                }
                else {
                    int index = 0;
                    Int32.TryParse(item, out index);
                    OverrideItems.Add(new OverrideItem(exposurePlans[index], index));
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
    }

    public class OverrideItem {

        public int ExposurePlanIndex { get; private set; }
        public bool IsDither { get; private set; }
        public string Name { get; private set; }

        public OverrideItem() {
            IsDither = true;
            ExposurePlanIndex = -1;
            Name = OverrideExposureOrder.DITHER;
        }

        public OverrideItem(ExposurePlan exposurePlan, int exposurePlanIndex) {
            IsDither = false;
            ExposurePlanIndex = exposurePlanIndex;
            Name = exposurePlan.ExposureTemplate.Name;
        }

        public OverrideItem Clone() {
            return new OverrideItem {
                IsDither = IsDither,
                ExposurePlanIndex = ExposurePlanIndex,
                Name = Name
            };
        }

        public string Serialize() {
            return IsDither ? OverrideExposureOrder.DITHER : ExposurePlanIndex.ToString();
        }
    }
}
