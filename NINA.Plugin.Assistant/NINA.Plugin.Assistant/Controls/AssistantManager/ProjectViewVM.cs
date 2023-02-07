using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Util;
using NINA.Core.Locale;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class ProjectViewVM : BaseINPC {

        private Project project;
        public Project Project {
            get => project;
            set {
                project = value;
                RaisePropertyChanged(nameof(Project));
            }
        }

        public ProjectViewVM(Project project) {
            Project = project;
            InitializeCombos();
        }

        private void InitializeCombos() {

            ProjectStateChoices = new AsyncObservableCollection<KeyValuePair<int, string>>();
            foreach (int i in Enum.GetValues(typeof(ProjectState))) {
                ProjectStateChoices.Add(new KeyValuePair<int, string>(i, Enum.GetName(typeof(ProjectState), i)));
            }

            ProjectPriorityChoices = new AsyncObservableCollection<KeyValuePair<int, string>>();
            foreach (int i in Enum.GetValues(typeof(ProjectPriority))) {
                ProjectPriorityChoices.Add(new KeyValuePair<int, string>(i, Enum.GetName(typeof(ProjectPriority), i)));
            }

            MinimumAltitudeChoices = new AsyncObservableCollection<KeyValuePair<double, string>>();
            for (int i = 0; i <= 60; i += 5) {
                MinimumAltitudeChoices.Add(new KeyValuePair<double, string>(i, i + "°"));
            }

            MinimumTimeChoices = new AsyncObservableCollection<KeyValuePair<int, string>>();
            MinimumTimeChoices.Add(new KeyValuePair<int, string>(0, Loc.Instance["LblAny"]));
            for (int i = 30; i <= 240; i += 30) {
                MinimumTimeChoices.Add(new KeyValuePair<int, string>(i, Utils.MtoHM(i)));
            }
        }

        private AsyncObservableCollection<KeyValuePair<int, string>> _projectStateChoices;
        public AsyncObservableCollection<KeyValuePair<int, string>> ProjectStateChoices {
            get {
                return _projectStateChoices;
            }
            set {
                _projectStateChoices = value;
                RaisePropertyChanged(nameof(ProjectStateChoices));
            }
        }

        private AsyncObservableCollection<KeyValuePair<int, string>> _projectPriorityChoices;
        public AsyncObservableCollection<KeyValuePair<int, string>> ProjectPriorityChoices {
            get {
                return _projectPriorityChoices;
            }
            set {
                _projectPriorityChoices = value;
                RaisePropertyChanged(nameof(ProjectPriorityChoices));
            }
        }

        private AsyncObservableCollection<KeyValuePair<int, string>> _minimumTimeChoices;
        public AsyncObservableCollection<KeyValuePair<int, string>> MinimumTimeChoices {
            get {
                return _minimumTimeChoices;
            }
            set {
                _minimumTimeChoices = value;
            }
        }

        private AsyncObservableCollection<KeyValuePair<double, string>> _minimumAltitudeChoices;
        public AsyncObservableCollection<KeyValuePair<double, string>> MinimumAltitudeChoices {
            get {
                return _minimumAltitudeChoices;
            }
            set {
                _minimumAltitudeChoices = value;
            }
        }

    }
}
