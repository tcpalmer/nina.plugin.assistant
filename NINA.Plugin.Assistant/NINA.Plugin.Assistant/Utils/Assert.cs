using System;

namespace Assistant.NINAPlugin.Util {

    public static class Assert {

        public static void isTrue(bool expression, string message) {
            if (!expression) {
                throw new ArgumentException(message);
            }
        }

        public static void isFalse(bool expression, string message) {
            if (expression) {
                throw new ArgumentException(message);
            }
        }

        public static void notNull<T>(T obj, string message) {
            if (obj == null) {
                throw new ArgumentException(message);
            }
        }
    }
}