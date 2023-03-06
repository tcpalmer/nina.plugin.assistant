using Assistant.NINAPlugin.Plan;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.NINAPlugin.Sequencer {

    public class AssistantStatusMonitor : BaseINPC {

        public AssistantStatusMonitor() { }

        private List<TargetStatus> targetStatusList = new List<TargetStatus>();
        public List<TargetStatus> TargetStatusList {
            get => targetStatusList;
            set {
                targetStatusList = value;
            }
        }

        TargetStatus currentTargetStatus;
        public TargetStatus CurrentTargetStatus {
            get => currentTargetStatus;
            set {
                currentTargetStatus = value;
                RaisePropertyChanged(nameof(CurrentTargetStatus));
            }
        }

        public void BeginTarget(IPlanTarget planTarget) {
            TargetStatus targetStatus = new TargetStatus(planTarget);
            TargetStatusList.Add(targetStatus);
            RaisePropertyChanged(nameof(TargetStatusList));
            CurrentTargetStatus = targetStatus;
        }

        public void EndTarget() {
            CurrentTargetStatus = null;
        }

        public string GetFilterName(string planItemId) {
            if (CurrentTargetStatus == null) {
                throw new Exception("currentTargetStatus is unexpectedly null");
            }

            IPlanTarget planTarget = CurrentTargetStatus.PlanTarget;
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
            if (CurrentTargetStatus == null) {
                throw new Exception("currentTargetStatus is unexpectedly null");
            }

            CurrentTargetStatus.StartInstruction(new InstructionStatus(sequenceItemName, GetFilterName(itemId)).Start(DateTime.Now));
            RaisePropertyChanged(nameof(CurrentTargetStatus));
        }

        public void ItemFinish(string itemId, string sequenceItemName) {
            if (CurrentTargetStatus == null) {
                throw new Exception("currentTargetStatus is unexpectedly null");
            }

            CurrentTargetStatus.EndInstruction(DateTime.Now);
            RaisePropertyChanged(nameof(CurrentTargetStatus));
        }
    }

    public class TargetStatus : BaseINPC {
        public IPlanTarget PlanTarget { get; private set; }
        private InstructionStatus activeInstruction;
        private StringBuilder completedHistory;

        public string History {
            get {
                return activeInstruction != null ? completedHistory.ToString() + activeInstruction.ToString() : completedHistory.ToString();
            }
        }

        public TargetStatus(IPlanTarget planTarget) {
            this.PlanTarget = planTarget;
            this.completedHistory = new StringBuilder();
        }

        public string Name { get { return PlanTarget.Name; } }

        public void StartInstruction(InstructionStatus instructionStatus) {
            activeInstruction = instructionStatus;
        }

        public void EndInstruction(DateTime endTime) {
            activeInstruction.End(endTime);
            completedHistory.AppendLine(activeInstruction.ToString());
            activeInstruction = null;
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
