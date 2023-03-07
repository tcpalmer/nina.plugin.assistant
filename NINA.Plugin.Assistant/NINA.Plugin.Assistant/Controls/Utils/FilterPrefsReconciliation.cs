using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Assistant.NINAPlugin.Controls.Util {

    public class FilterPrefsReconciliation {

        /// <summary>
        /// Ensure that we have a FilterPreference for each defined filter on all profiles.
        /// </summary>
        /// <param name="profileService"></param>
        public static void ReconcileProfileFilterPrefs(IProfileService profileService) {

            // TODO: do we also remove filter pref records that no longer have a cooresponding filter in the profile?
            // TODO: find and remove FilterPreference records that are orphaned: original profile was removed?

            Dictionary<string, List<string>> missing = new Dictionary<string, List<string>>();

            using (var context = new AssistantDatabaseInteraction().GetContext()) {
                foreach (ProfileMeta profileMeta in profileService.Profiles) {
                    IProfile profile = ProfileLoader.Load(profileService, profileMeta);
                    string profileId = profile.Id.ToString();

                    List<FilterInfo> filterInfos = profile.FilterWheelSettings.FilterWheelFilters.ToList();
                    List<FilterPreference> filterPreferences = context.GetFilterPreferences(profileId);

                    foreach (FilterInfo filterInfo in filterInfos) {
                        FilterPreference filterPreference = filterPreferences.Where(f => f.FilterName == filterInfo.Name).FirstOrDefault();
                        if (filterPreference == null) {
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
                    List<FilterPreference> missingFilterPreferences = new List<FilterPreference>();
                    string profileId = entry.Key;
                    List<string> list = entry.Value;

                    if (list != null) {
                        list.ForEach(filterName => {
                            Logger.Debug($"Scheduler: filter pref missing for filter '{filterName}' in profile '{profileId}', adding default");
                            missingFilterPreferences.Add(new FilterPreference(profileId, filterName));
                        });
                    }

                    context.AddFilterPreferences(missingFilterPreferences);
                }
            }
        }

        private FilterPrefsReconciliation() { }
    }

}
