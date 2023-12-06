using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Util;
using Newtonsoft.Json;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    [ExportMetadata("Name", "Target Scheduler Flats")]
    [ExportMetadata("Description", "Flats automation for Target Scheduler")]
    [ExportMetadata("Icon", "Scheduler.SchedulerSVG")]
    [ExportMetadata("Category", "Target Scheduler")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TargetSchedulerFlats : TargetSchedulerFlatsBase {

        [ImportingConstructor]
        public TargetSchedulerFlats(IProfileService profileService,
                                    ICameraMediator cameraMediator,
                                    IImagingMediator imagingMediator,
                                    IImageSaveMediator imageSaveMediator,
                                    IImageHistoryVM imageHistoryVM,
                                    IFilterWheelMediator filterWheelMediator,
                                    IRotatorMediator rotatorMediator,
                                    IFlatDeviceMediator flatDeviceMediator) :
            base(profileService,
                 cameraMediator,
                 imagingMediator,
                 imageSaveMediator,
                 imageHistoryVM,
                 filterWheelMediator,
                 rotatorMediator,
                 flatDeviceMediator) { }

        public TargetSchedulerFlats(TargetSchedulerFlats cloneMe) : this(
            cloneMe.profileService,
            cloneMe.cameraMediator,
            cloneMe.imagingMediator,
            cloneMe.imageSaveMediator,
            cloneMe.imageHistoryVM,
            cloneMe.filterWheelMediator,
            cloneMe.rotatorMediator,
            cloneMe.flatDeviceMediator) {
            CopyMetaData(cloneMe);
            AlwaysRepeatFlatSet = cloneMe.AlwaysRepeatFlatSet;
        }

        public override object Clone() {
            return new TargetSchedulerFlats(this);
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            try {
                DisplayText = "Determining needed flats";
                List<LightSession> neededFlats = GetNeededFlats();
                if (neededFlats == null) {
                    DisplayText = "";
                    return;
                }

                TotalFlatSets = neededFlats.Count;
                CompletedFlatSets = 0;

                LogTrainedFlatDetails();

                // Prep the flat device
                DisplayText = "Preparing flat device";
                await CloseCover(progress, token);
                await ToggleLight(true, progress, token);

                imageSaveMediator.BeforeImageSaved += BeforeImageSaved;
                imageSaveMediator.BeforeFinalizeImageSaved += BeforeFinalizeImageSaved;

                List<FlatSpec> takenFlats = new List<FlatSpec>();
                foreach (LightSession neededFlat in neededFlats) {
                    bool success = true;

                    if (AlwaysRepeatFlatSet || !takenFlats.Contains(neededFlat.FlatSpec)) {
                        SetTargetName(neededFlat.TargetId);
                        SessionId = neededFlat.SessionId;

                        success = await TakeFlatSet(neededFlat.FlatSpec, true, progress, token);
                        if (success) {
                            takenFlats.Add(neededFlat.FlatSpec);
                        }
                    }
                    else {
                        TSLogger.Info($"TS Flats: flat already taken, skipping: {neededFlat}");
                    }

                    if (success) {
                        CompletedFlatSets++;
                        SaveFlatHistory(neededFlat);
                    }
                }

                await ToggleLight(false, progress, token);
            }
            catch (Exception ex) {
                DisplayText = "";

                if (Utils.IsCancelException(ex)) {
                    TSLogger.Warning("TS Flats: sequence was canceled/interrupted");
                    Status = SequenceEntityStatus.CREATED;
                    token.ThrowIfCancellationRequested();
                }
                else {
                    TSLogger.Error($"Exception taking flats: {ex.Message}:\n{ex.StackTrace}");
                }

                if (ex is SequenceEntityFailedException) {
                    throw;
                }

                throw new SequenceEntityFailedException($"exception taking flats: {ex.Message}", ex);
            }
            finally {
                DisplayText = "";
                TotalFlatSets = 0;
                CompletedFlatSets = 0;
                Iterations = 0;
                CompletedIterations = 0;

                imageSaveMediator.BeforeImageSaved -= BeforeImageSaved;
                imageSaveMediator.BeforeFinalizeImageSaved += BeforeFinalizeImageSaved;
            }

            return;
        }

        private List<LightSession> GetNeededFlats() {
            List<LightSession> neededFlats = new List<LightSession>();
            FlatsExpert flatsExpert = new FlatsExpert();
            DateTime cutoff = DateTime.Now.Date.AddDays(FlatsExpert.ACQUIRED_IMAGES_CUTOFF_DAYS);
            string profileId = profileService.ActiveProfile.Id.ToString();

            using (var context = database.GetContext()) {
                List<Project> activeProjects = context.GetActiveProjects(profileId);
                List<AcquiredImage> acquiredImages = context.GetAcquiredImages(profileId, cutoff);

                // Handle flats taken periodically
                List<Target> targets = flatsExpert.GetTargetsForPeriodicFlats(activeProjects);
                if (targets.Count > 0) {
                    List<LightSession> lightSessions = flatsExpert.GetLightSessions(targets, acquiredImages);
                    if (lightSessions.Count > 0) {
                        List<FlatHistory> takenFlats = context.GetFlatsHistory(targets);
                        neededFlats.AddRange(flatsExpert.GetNeededPeriodicFlats(DateTime.Now, targets, lightSessions, takenFlats));
                    }
                    else {
                        TSLogger.Info("TS Flats: no light sessions for targets active for periodic flats");
                    }
                }
                else {
                    TSLogger.Info("TS Flats: no targets active for periodic flats");
                }

                // Add any flats needed for target completion targets
                targets = flatsExpert.GetCompletedTargetsForFlats(activeProjects);
                if (targets.Count > 0) {
                    List<LightSession> lightSessions = flatsExpert.GetLightSessions(targets, acquiredImages);
                    if (lightSessions.Count > 0) {
                        List<FlatHistory> takenFlats = context.GetFlatsHistory(targets);
                        // TODO: implement AlwaysRepeatFlatSet here
                        // BUT what does it mean here?  What's the 'repeat time span'?  Same as cadence?
                        neededFlats.AddRange(flatsExpert.GetNeededTargetCompletionFlats(targets, lightSessions, takenFlats));
                    }
                    else {
                        TSLogger.Info("TS Flats: no light sessions for targets active for target completed flats");
                    }
                }
                else {
                    TSLogger.Info("TS Flats: no targets active for target completed flats");
                }

                if (neededFlats.Count == 0) {
                    TSLogger.Info("TS Flats: no flats needed");
                    return null;
                }

                // Sort in increasing rotation angle order to minimize rotator movements
                neededFlats.Sort(delegate (LightSession x, LightSession y) {
                    return x.FlatSpec.Rotation.CompareTo(y.FlatSpec.Rotation);
                });

                return neededFlats;
            }
        }
    }
}
