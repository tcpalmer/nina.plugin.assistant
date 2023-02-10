using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Util;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class ProjectViewVM : BaseINPC {

        private ProjectProxy projectProxy;
        public ProjectProxy ProjectProxy {
            get => projectProxy;
            set {
                projectProxy = value;
                RaisePropertyChanged(nameof(ProjectProxy));
            }
        }

        public ProjectViewVM(Project project) {
            project.Description = "hello description";
            ProjectProxy = new ProjectProxy(project);

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

            MinimumAltitudeChoices = new List<string>();
            for (int i = 0; i <= 60; i += 5) {
                MinimumAltitudeChoices.Add(i + "°");
            }

            MinimumTimeChoices = new List<string>();
            for (int i = 30; i <= 240; i += 30) {
                MinimumTimeChoices.Add(Utils.MtoHM(i));
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

        private List<string> _minimumTimeChoices;
        public List<string> MinimumTimeChoices {
            get => _minimumTimeChoices;
            set {
                _minimumTimeChoices = value;
                RaisePropertyChanged(nameof(MinimumTimeChoices));
            }
        }

        private List<string> _minimumAltitudeChoices;
        public List<string> MinimumAltitudeChoices {
            get {
                return _minimumAltitudeChoices;
            }
            set {
                _minimumAltitudeChoices = value;
                RaisePropertyChanged(nameof(MinimumAltitudeChoices));
            }
        }

    }
}
