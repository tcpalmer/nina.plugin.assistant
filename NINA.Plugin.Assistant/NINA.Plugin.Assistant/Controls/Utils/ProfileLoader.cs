using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System;
using System.IO;
using System.Runtime.Caching;
using System.Runtime.Serialization;
using System.Text;

namespace Assistant.NINAPlugin.Controls.Util {

    public class ProfileLoader {

        public static IProfile Load(IProfileService profileService, ProfileMeta profileMeta) {

            if (profileService.ActiveProfile.Id.ToString() == profileMeta.Id.ToString()) {
                return profileService.ActiveProfile;
            }

            string cacheKey = GetCacheKey(profileMeta.Location);
            IProfile profile = ProfileCache.Get(cacheKey);
            if (profile != null) {
                return profile;
            }

            FileStream fs = null;
            try {
                fs = new FileStream(profileMeta.Location, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                var serializer = new DataContractSerializer(typeof(Profile));
                profile = (Profile)serializer.ReadObject(fs);
                ProfileCache.Put(profile, cacheKey);
                return profile;
            }
            catch (Exception e) {
                Logger.Error($"failed to read profile at {profileMeta.Location}: {e.Message} {e.StackTrace}");
                if (fs != null) {
                    fs.Close();
                }

                return null;
            }
        }

        private static string GetCacheKey(string path) {
            DateTime lastWriteTime = File.GetLastWriteTime(path);
            StringBuilder sb = new StringBuilder();
            sb.Append($"{path}_");
            sb.Append($"{lastWriteTime:yyyy-MM-dd-HH-mm-ss}");
            return sb.ToString();
        }

        private ProfileLoader() { }

    }

    class ProfileCache {

        private static readonly TimeSpan ITEM_TIMEOUT = TimeSpan.FromHours(12);
        private static readonly MemoryCache _cache = new MemoryCache("Scheduler Profile");

        public static IProfile Get(string cacheKey) {
            return (IProfile)_cache.Get(cacheKey);
        }

        public static void Put(IProfile profile, string cacheKey) {
            _cache.Add(cacheKey, profile, DateTime.Now.Add(ITEM_TIMEOUT));
        }

        private ProfileCache() { }
    }

}
