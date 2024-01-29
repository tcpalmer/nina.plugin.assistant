using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System;
using System.IO;
using System.Runtime.Caching;
using System.Runtime.Serialization;
using System.Text;

namespace Assistant.NINAPlugin.Controls.Util {

    public class ProfileLoader {
        private static readonly ProfileLoader Instance = new ProfileLoader();
        private static bool initialized = false;
        private ProfileCache cache;

        public static IProfile Load(IProfileService profileService, ProfileMeta profileMeta) {
            if (!initialized) {
                profileService.ProfileChanged += Instance.ProfileService_ProfileChanged;
                profileService.Profiles.CollectionChanged += Instance.ProfileService_ProfileChanged;
                initialized = true;
            }

            if (profileService.ActiveProfile.Id.ToString() == profileMeta.Id.ToString()) {
                return profileService.ActiveProfile;
            }

            string cacheKey = Instance.GetCacheKey(profileMeta.Location);
            IProfile profile = Instance.cache.Get(cacheKey);
            if (profile != null) {
                return profile;
            }

            try {
                using FileStream fs = new FileStream(profileMeta.Location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var serializer = new DataContractSerializer(typeof(Profile));
                profile = (Profile)serializer.ReadObject(fs);
                Instance.cache.Put(profile, cacheKey);
                return profile;
            } catch (Exception e) {
                TSLogger.Error($"failed to read profile at {profileMeta.Location}: {e.Message} {e.StackTrace}");
                throw;
            }
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            cache.Clear();
        }

        private string GetCacheKey(string path) {
            DateTime lastWriteTime = File.GetLastWriteTime(path);
            StringBuilder sb = new StringBuilder();
            sb.Append($"{path}_");
            sb.Append($"{lastWriteTime:yyyy-MM-dd-HH-mm-ss}");
            return sb.ToString();
        }

        private ProfileLoader() {
            cache = new ProfileCache();
        }
    }

    internal class ProfileCache {
        private static readonly TimeSpan ITEM_TIMEOUT = TimeSpan.FromHours(2);
        private static MemoryCache _cache = new MemoryCache("Scheduler Profile");

        public IProfile Get(string cacheKey) {
            return (IProfile)_cache.Get(cacheKey);
        }

        public void Put(IProfile profile, string cacheKey) {
            if (!_cache.Add(cacheKey, profile, DateTime.Now.Add(ITEM_TIMEOUT))) {
                _cache.Remove(cacheKey);
                _cache.Add(cacheKey, profile, DateTime.Now.Add(ITEM_TIMEOUT));
            }
        }

        public void Clear() {
            _cache.Dispose();
            _cache = new MemoryCache("Scheduler Profile");
        }

        internal ProfileCache() {
        }
    }
}