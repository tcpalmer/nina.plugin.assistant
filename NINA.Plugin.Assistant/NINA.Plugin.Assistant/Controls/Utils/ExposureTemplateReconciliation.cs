using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Assistant.NINAPlugin.Controls.Util {

    public class ExposureTemplatesReconciliation {

        /// <summary>
        /// Ensure that we have a ExposureTemplate for each defined filter on all profiles.
        /// </summary>
        /// <param name="profileService"></param>
        public static void ReconcileProfileExposureTemplate(IProfileService profileService) {

            /* NOTE - not currently used
             * - It was causing problems to automatically keep running this.
             * - For now, user is forced to create ETs before EPs can be added.
             * - May revisit later.
             */

            // TODO: find and remove ExposureTemplate records that are orphaned: original profile was removed?

            Dictionary<string, List<string>> missing = new Dictionary<string, List<string>>();

            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                foreach (ProfileMeta profileMeta in profileService.Profiles) {
                    IProfile profile = ProfileLoader.Load(profileService, profileMeta);
                    string profileId = profile.Id.ToString();

                    List<FilterInfo> filterInfos = profile.FilterWheelSettings.FilterWheelFilters.ToList();
                    List<ExposureTemplate> exposureTemplates = context.GetExposureTemplates(profileId);

                    foreach (FilterInfo filterInfo in filterInfos) {
                        ExposureTemplate exposureTemplate = exposureTemplates.Where(f => f.FilterName == filterInfo.Name).FirstOrDefault();
                        if (exposureTemplate == null) {
                            List<string> list;
                            if (missing.ContainsKey(profileId)) {
                                list = missing[profileId];
                            }
                            else {
                                list = new List<string>();
                                missing[profileId] = list;
                            }

                            list.Add(filterInfo.Name);
                        }
                    }
                }

                foreach (KeyValuePair<string, List<string>> entry in missing) {
                    List<ExposureTemplate> missingExposureTemplates = new List<ExposureTemplate>();
                    string profileId = entry.Key;
                    List<string> list = entry.Value;

                    if (list != null) {
                        list.ForEach(filterName => {
                            Logger.Debug($"Scheduler: ExposureTemplate missing for filter '{filterName}' in profile '{profileId}', adding default");
                            string name = $"{filterName} Default";
                            missingExposureTemplates.Add(new ExposureTemplate(profileId, name, filterName));
                        });
                    }

                    context.AddExposureTemplates(missingExposureTemplates);
                }
            }
        }

        private ExposureTemplatesReconciliation() { }
    }

}
