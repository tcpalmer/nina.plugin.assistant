using NINA.Core.Utility;

namespace NINA.Plugin.Assistant.Shared.Utility {

    public class Common {
        public static readonly string PLUGIN_HOME = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "SchedulerPlugin");
        public static readonly bool USE_EMULATOR = true;

        public static string Base64Encode(string plainText) {
            var bytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(bytes);
        }

        public static string Base64Decode(string encoded) {
            var bytes = Convert.FromBase64String(encoded);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        private Common() {
        }
    }
}