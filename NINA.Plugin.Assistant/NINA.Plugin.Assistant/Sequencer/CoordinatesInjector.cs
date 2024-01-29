using NINA.Astrometry;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.Sequencer.SequenceItem.Telescope;

namespace Assistant.NINAPlugin.Sequencer {

    public class CoordinatesInjector {
        private InputTarget target;

        public CoordinatesInjector(InputTarget target) {
            this.target = target;
        }

        public void Inject(ISequenceContainer container) {
            if (container == null || container.Items.Count == 0) { return; }

            foreach (ISequenceItem item in container.Items) {
                SlewScopeToRaDec slewScopeToRaDec = item as SlewScopeToRaDec;
                if (slewScopeToRaDec != null) {
                    Log("SlewScopeToRaDec");
                    slewScopeToRaDec.Coordinates = target.InputCoordinates;
                    slewScopeToRaDec.Inherited = true;
                    slewScopeToRaDec.SequenceBlockInitialize();
                }

                Center center = item as Center;
                if (center != null) {
                    Log("Center");
                    center.Coordinates = target.InputCoordinates;
                    center.Inherited = true;
                    center.SequenceBlockInitialize();
                }

                CenterAndRotate centerAndRotate = item as CenterAndRotate;
                if (centerAndRotate != null) {
                    Log("CenterAndRotate");
                    centerAndRotate.Coordinates = target.InputCoordinates;
                    centerAndRotate.Inherited = true;
                    centerAndRotate.SequenceBlockInitialize();
                }

                ISequenceContainer subContainer = item as ISequenceContainer;
                if (subContainer != null) {
                    Inject(subContainer);
                }
            }
        }

        private void Log(string instruction) {
            TSLogger.Debug($"injecting coordinates for {target.TargetName} into {instruction}: {target.InputCoordinates.Coordinates}");
        }
    }
}