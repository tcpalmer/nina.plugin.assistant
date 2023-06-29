using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Util;
using NINA.Core.Utility;
using System;
using System.Text;

namespace Assistant.NINAPlugin.Sequencer {

    public class SchedulerStatusMonitor : BaseINPC {

        public SchedulerStatusMonitor() {
            StatusItemList.CollectionChanged += StatusItemList_CollectionChanged;
        }

        private void StatusItemList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            RaisePropertyChanged(nameof(StatusItemList));
        }

        private AsyncObservableCollection<IStatusItem> statusItemList = new AsyncObservableCollection<IStatusItem>();
        public AsyncObservableCollection<IStatusItem> StatusItemList {
            get => statusItemList;
            set {
                statusItemList = value;
            }
        }

        public void Reset() {
            StatusItemList.Clear();
        }

        private WaitStatus currentWaitStatus;
        public WaitStatus CurrentWaitStatus {
            get => currentWaitStatus;
            set {
                currentWaitStatus = value;
                RaisePropertyChanged(nameof(CurrentWaitStatus));
                RaisePropertyChanged(nameof(Summary));
            }
        }

        private TargetStatus currentTargetStatus;
        public TargetStatus CurrentTargetStatus {
            get => currentTargetStatus;
            set {
                currentTargetStatus = value;
                RaisePropertyChanged(nameof(CurrentTargetStatus));
                RaisePropertyChanged(nameof(Summary));
            }
        }

        public string Summary {
            get { return currentTargetStatus != null ? currentTargetStatus.Name : ""; }
        }

        public void BeginWait(DateTime waitUntil) {
            WaitStatus waitStatus = new WaitStatus($"Waiting until {Utils.FormatDateTimeFull(waitUntil)} for next target availability");
            StatusItemList.Add(waitStatus);
            CurrentWaitStatus = waitStatus;
        }

        public void EndWait() {
            CurrentWaitStatus.History = CurrentWaitStatus.History + " done";
            CurrentWaitStatus = null;
        }

        public void BeginTarget(IPlanTarget planTarget) {
            TargetStatus targetStatus = new TargetStatus(planTarget);
            StatusItemList.Add(targetStatus);
            CurrentTargetStatus = targetStatus;
        }

        public void EndTarget() {
            CurrentTargetStatus = null;
        }

        public string GetFilterName(string planItemId) {
            if (CurrentTargetStatus == null) {
                TSLogger.Error("currentTargetStatus is unexpectedly null");
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
                TSLogger.Error("currentTargetStatus is unexpectedly null");
                throw new Exception("currentTargetStatus is unexpectedly null");
            }

            CurrentTargetStatus.StartInstruction(new InstructionStatus(sequenceItemName, GetFilterName(itemId)).Start(DateTime.Now));
            RaisePropertyChanged(nameof(CurrentTargetStatus));
        }

        public void ItemFinish(string itemId, string sequenceItemName) {
            if (CurrentTargetStatus == null) {
                TSLogger.Error("currentTargetStatus is unexpectedly null");
                throw new Exception("currentTargetStatus is unexpectedly null");
            }

            CurrentTargetStatus.EndInstruction(DateTime.Now);
            RaisePropertyChanged(nameof(CurrentTargetStatus));
        }
    }

    public interface IStatusItem {
        string Name { get; }
        string History { get; }
    }

    public class WaitStatus : BaseINPC, IStatusItem {

        public WaitStatus(string name) {
            Name = name;
            History = "waiting ...";
        }

        public string Name { get; set; }

        private string history;
        public string History {
            get {
                return history;
            }
            set {
                history = value;
                RaisePropertyChanged(nameof(History));
            }
        }
    }

    public class TargetStatus : BaseINPC, IStatusItem {
        public IPlanTarget PlanTarget { get; private set; }
        private InstructionStatus activeInstruction;
        private StringBuilder completedHistory;

        private string history;
        public string History {
            get {
                return history;
            }
            set {
                history = value;
                RaisePropertyChanged(nameof(History));
            }
        }

        public TargetStatus(IPlanTarget planTarget) {
            this.PlanTarget = planTarget;
            this.completedHistory = new StringBuilder();
        }

        // TODO: get start (and later end) times into the Name too
        public string Name { get { return $"{PlanTarget.Project.Name} / {PlanTarget.Name}"; } }

        public void StartInstruction(InstructionStatus instructionStatus) {
            activeInstruction = instructionStatus;
            History = completedHistory.ToString() + activeInstruction.ToString();
        }

        public void EndInstruction(DateTime endTime) {
            activeInstruction.End(endTime);
            completedHistory.AppendLine(activeInstruction.ToString());
            History = completedHistory.ToString();
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
