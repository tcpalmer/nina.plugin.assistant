using Assistant.NINAPlugin.Database.Schema;
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class TargetImportVM : BaseINPC {

        private IFramingAssistantVM framingAssistantVM;
        private IPlanetariumFactory planetariumFactory;

        public TargetImportVM(IDeepSkyObjectSearchVM deepSkyObjectSearchVM, IFramingAssistantVM framingAssistantVM, IPlanetariumFactory planetariumFactory) {
            DeepSkyObjectSearchVM = deepSkyObjectSearchVM;
            DeepSkyObjectSearchVM.PropertyChanged += DeepSkyObjectSearchVM_PropertyChanged;

            this.framingAssistantVM = framingAssistantVM;
            FramingAssistantImportCommand = new RelayCommand(FramingAssistantImport);

            this.planetariumFactory = planetariumFactory;
            PlanetariumImportCommand = new RelayCommand(PlanetariumImport);
        }

        private IDeepSkyObjectSearchVM deepSkyObjectSearchVM;
        public IDeepSkyObjectSearchVM DeepSkyObjectSearchVM { get => deepSkyObjectSearchVM; set => deepSkyObjectSearchVM = value; }

        private Target target;
        public Target Target {
            get { return target; }
            set {
                target = value;
                RaisePropertyChanged(nameof(Target));
            }
        }

        private void DeepSkyObjectSearchVM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(DeepSkyObjectSearchVM.Coordinates) && DeepSkyObjectSearchVM.Coordinates != null) {
                Target target = new Target();
                target.Coordinates = DeepSkyObjectSearchVM.Coordinates;
                target.Name = DeepSkyObjectSearchVM.TargetName;
                Target = target;
            }
        }

        public ICommand FramingAssistantImportCommand { get; private set; }

        private void FramingAssistantImport(object obj) {

            Target target = new Target();
            target.Coordinates = GetFramingAssistantCoordinates();
            target.Name = framingAssistantVM.DSO.Name;
            target.Rotation = framingAssistantVM.Rectangle.TotalRotation;

            Target = target;
        }

        private Coordinates GetFramingAssistantCoordinates() {
            int raHours = framingAssistantVM.RAHours;
            int raMinutes = framingAssistantVM.RAMinutes;
            double raSeconds = framingAssistantVM.RASeconds;
            int decDegrees = framingAssistantVM.DecDegrees;
            int decMinutes = framingAssistantVM.DecMinutes;
            double decSeconds = framingAssistantVM.DecSeconds;

            string hms = string.Format("{0:00}:{1:00}:{2:00}", raHours, raMinutes, raSeconds);
            string dms = string.Format("{0:00}:{1:00}:{2:00}", decDegrees, decMinutes, decSeconds);
            return new Coordinates(AstroUtil.HMSToDegrees(hms), AstroUtil.DMSToDegrees(dms), Epoch.J2000, Coordinates.RAType.Degrees);
        }

        public ICommand PlanetariumImportCommand { get; private set; }

        private async void PlanetariumImport(object obj) {
            Target target = await PlanetariumImport();
            if (target != null) {
                Target = target;
            }
        }

        private async Task<Target> PlanetariumImport() {
            try {
                IPlanetarium planetarium = planetariumFactory.GetPlanetarium();
                DeepSkyObject dso = await planetarium.GetTarget();

                if (dso != null) {
                    Target target = new Target();
                    target.Name = dso.Name;
                    target.Coordinates = dso.Coordinates;

                    if (planetarium.CanGetRotationAngle) {
                        double rotationAngle = await planetarium.GetRotationAngle();
                        if (!double.IsNaN(rotationAngle)) {
                            target.Rotation = rotationAngle;
                        }
                    }

                    return target;
                }

                return null;
            }
            catch (Exception e) {
                Logger.Error($"failed to get coordinates from planetarium: {e.Message}");
                return null;
            }
        }
    }
}
