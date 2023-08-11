using Assistant.NINAPlugin.Database.Schema;
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
            foreach (ExposurePlan item in exposurePlans) {
                OverrideItems.Add(new OverrideItem(item));
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
                    int id = 0;
                    Int32.TryParse(item, out id);
                    OverrideItems.Add(new OverrideItem(Lookup(id, exposurePlans)));
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

        private ExposurePlan Lookup(int id, List<ExposurePlan> exposurePlans) {
            foreach (ExposurePlan item in exposurePlans) {
                if (item.Id == id) {
                    return item;
                }
            }

            throw new Exception($"failed to find exposure plan for override order: {id}");
        }
    }

    public class OverrideItem {

        private ExposurePlan exposurePlan;
        public ExposurePlan ExposurePlan { get => exposurePlan; set => exposurePlan = value; }

        private bool isDither;
        public bool IsDither { get => isDither; set => isDither = value; }

        public OverrideItem() {
            IsDither = true;
            ExposurePlan = null;
        }

        public OverrideItem(ExposurePlan exposurePlan) {
            IsDither = false;
            ExposurePlan = exposurePlan;
        }

        public OverrideItem Clone() {
            return new OverrideItem {
                IsDither = IsDither,
                ExposurePlan = ExposurePlan
            };
        }

        public string Name {
            get {
                return IsDither ? OverrideExposureOrder.DITHER : ExposurePlan.ExposureTemplate.Name;
            }
        }

        public string Serialize() {
            return IsDither ? OverrideExposureOrder.DITHER : ExposurePlan.Id.ToString();
        }
    }
}
