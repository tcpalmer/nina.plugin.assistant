using Assistant.NINAPlugin.Plan;
using System;
using System.ComponentModel;
using System.Text;

namespace Assistant.NINAPlugin.Sequencer {

    public class AssistantStatusMonitor : INotifyPropertyChanged {

        private IPlanTarget planTarget;
        private InstructionStatus activeInstruction;
        private StringBuilder completedHistory { get; set; }

        public string History {
            get {
                return activeInstruction != null ? completedHistory.ToString() + activeInstruction.ToString() : completedHistory.ToString();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public AssistantStatusMonitor(IPlanTarget planTarget) {
            this.planTarget = planTarget;
            this.completedHistory = new StringBuilder();
        }

        public string GetFilterName(string planItemId) {
            if (planTarget.PlanId == planItemId) {
                return "";
            }

            foreach (IPlanExposure planFilter in planTarget.ExposurePlans) {
                if (planFilter.PlanId == planItemId) {
                    return planFilter.FilterName;
                }
            }

            return "";
        }

        public void ItemStart(string itemId, string sequenceItemName) {
            activeInstruction = new InstructionStatus(sequenceItemName, GetFilterName(itemId)).Start(DateTime.Now);
            OnPropertyChanged();
        }

        public void ItemFinsh(string itemId, string sequenceItemName) {
            activeInstruction.End(DateTime.Now);
            completedHistory.AppendLine(activeInstruction.ToString());
            activeInstruction = null;
            OnPropertyChanged();
        }

        protected void OnPropertyChanged() {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MonitorHistory"));
        }
    }

    public class InstructionStatus {

        private string name { get; set; }
        private string filterName { get; set; }
        private DateTime start { get; set; }
        private DateTime end { get; set; }

        public InstructionStatus(string name, string filterName) {
            this.name = name;
            this.filterName = filterName;
        }

        public InstructionStatus Start(DateTime start) {
            this.start = start;
            this.end = DateTime.MinValue;
            return this;
        }

        public InstructionStatus End(DateTime end) {
            this.end = end;
            return this;
        }

        public override string ToString() {
            return end != DateTime.MinValue
             ? string.Format("{0,-20} {1,-5} {2:HH:mm:ss} {3:HH:mm:ss}", name, filterName, start, end)
             : string.Format("{0,-20} {1,-5} {2:HH:mm:ss}", name, filterName, start);
        }
    }

}
