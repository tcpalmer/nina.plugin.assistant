using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Plan.Scoring;
using Moq;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NINA.Plugin.Assistant.Test.Plan {

    class PlanMocks {

        public static List<IPlanProject> ProjectsList(params IPlanProject[] array) {
            return array.ToList();
        }

        public static Mock<IProfileService> GetMockProfileService(ObserverInfo location) {
            Mock<IProfileService> profileMock = new Mock<IProfileService>();
            profileMock.SetupProperty(m => m.ActiveProfile.Id, new Guid("01234567-0000-0000-0000-000000000000"));
            profileMock.SetupProperty(m => m.ActiveProfile.AstrometrySettings.Latitude, location.Latitude);
            profileMock.SetupProperty(m => m.ActiveProfile.AstrometrySettings.Longitude, location.Longitude);
            profileMock.SetupProperty(m => m.ActiveProfile.AstrometrySettings.Elevation, location.Elevation);
            return profileMock;
        }

        public static Mock<IPlanProject> GetMockPlanProject(string name, ProjectState state) {
            AssistantProjectPreferences app = new AssistantProjectPreferences();
            app.SetDefaults();

            Mock<IPlanProject> pp = new Mock<IPlanProject>();
            pp.SetupAllProperties();
            pp.SetupProperty(m => m.Name, name);
            pp.SetupProperty(m => m.State, state);
            pp.SetupProperty(m => m.Rejected, false);
            pp.SetupProperty(m => m.Preferences, app);
            pp.SetupProperty(m => m.HorizonDefinition, new HorizonDefinition(0));
            pp.SetupProperty(m => m.Targets, new List<IPlanTarget>());
            return pp;
        }

        public static Mock<IPlanTarget> GetMockPlanTarget(string name, Coordinates coordinates) {
            Mock<IPlanTarget> pt = new Mock<IPlanTarget>();
            pt.SetupAllProperties();
            pt.SetupProperty(m => m.Name, name);
            pt.SetupProperty(m => m.Coordinates, coordinates);
            pt.SetupProperty(m => m.FilterPlans, new List<IPlanFilter>());

            return pt;
        }

        public static Mock<IPlanFilter> GetMockPlanFilter(string filterName, int desired, int accepted) {
            return GetMockPlanFilter(filterName, desired, accepted, 30);
        }

        public static Mock<IPlanFilter> GetMockPlanFilter(string filterName, int desired, int accepted, int exposureLength) {
            AssistantFilterPreferences afp = new AssistantFilterPreferences();
            afp.SetDefaults();

            Mock<IPlanFilter> pf = new Mock<IPlanFilter>();
            pf.SetupAllProperties();
            pf.SetupProperty(m => m.FilterName, filterName);
            pf.SetupProperty(m => m.ExposureLength, exposureLength);
            pf.SetupProperty(m => m.Desired, desired);
            pf.SetupProperty(m => m.Acquired, 0);
            pf.SetupProperty(m => m.Accepted, accepted);
            pf.SetupProperty(m => m.Preferences, afp);
            pf.Setup(m => m.NeededExposures()).Returns(accepted > desired ? 0 : desired - accepted);
            pf.Setup(m => m.IsIncomplete()).Returns(accepted < desired);
            return pf;
        }

        public static void AddMockPlanFilter(Mock<IPlanTarget> pt, Mock<IPlanFilter> pf) {
            pf.SetupProperty(m => m.PlanTarget, pt.Object);
            pt.Object.FilterPlans.Add(pf.Object);
        }

        public static void AddMockPlanTarget(Mock<IPlanProject> pp, Mock<IPlanTarget> pt) {
            pt.SetupProperty(m => m.Project, pp.Object);
            pp.Object.Targets.Add(pt.Object);
        }

        public static Mock<IScoringEngine> GetMockScoringEnging() {
            Mock<IScoringEngine> mse = new Mock<IScoringEngine>();
            mse.SetupAllProperties();
            mse.SetupProperty(m => m.RuleWeights, new Dictionary<string, double>());
            return mse;
        }

        public static ImageSavedEventArgs GetImageSavedEventArgs(DateTime onDateTime, string filterName) {
            ImageMetaData metadata = new ImageMetaData();
            metadata.Image.ExposureStart = onDateTime;
            metadata.Image.Binning = "1x1";

            RMS rms = new RMS();
            rms.Total = 200;
            //rms.Scale = 1.5;
            rms.RA = 201;
            rms.Dec = 202;
            metadata.Image.RecordedRMS = rms;

            metadata.Focuser.Position = 300;
            metadata.Focuser.Temperature = 301;
            metadata.Rotator.Position = 302;
            metadata.Telescope.SideOfPier = NINA.Core.Enum.PierSide.pierWest;
            metadata.Telescope.Airmass = 303;
            metadata.Camera.Temperature = 304;
            metadata.Camera.SetPoint = 305;
            metadata.Camera.Gain = 306;
            metadata.Camera.Offset = 307;

            ImageSavedEventArgs msg = new ImageSavedEventArgs();
            msg.MetaData = metadata;
            msg.PathToImage = new Uri("file://path/to/image.fits");
            msg.Duration = 60;
            msg.Filter = filterName;

            Mock<IImageStatistics> isMock = new Mock<IImageStatistics>();
            isMock.SetupAllProperties();

            Mock<IStarDetectionAnalysis> sdaMock = new Mock<IStarDetectionAnalysis>();
            sdaMock.SetupAllProperties();
            sdaMock.SetupProperty(m => m.DetectedStars, 100);
            sdaMock.SetupProperty(m => m.HFR, 101);
            sdaMock.SetupProperty(m => m.HFRStDev, 102);

            msg.Statistics = isMock.Object;
            msg.StarDetectionAnalysis = sdaMock.Object;

            return msg;
        }
    }
}
