namespace NINA.Plugin.Assistant.Shared.Utility {

    public enum EventContainerType {
        BeforeWait,
        AfterWait,
        BeforeNewTarget,
        AfterNewTarget,
        AfterEachTarget
    }

    public static class EventContainerHelper {

        public static EventContainerType Convert(string eventContainerType) {
            if (string.IsNullOrEmpty(eventContainerType)) {
                throw new ArgumentNullException(nameof(eventContainerType));
            }

            if (eventContainerType == EventContainerType.BeforeWait.ToString()) {
                return EventContainerType.BeforeWait;
            }

            if (eventContainerType == EventContainerType.AfterWait.ToString()) {
                return EventContainerType.AfterWait;
            }

            if (eventContainerType == EventContainerType.BeforeNewTarget.ToString()) {
                return EventContainerType.BeforeNewTarget;
            }

            if (eventContainerType == EventContainerType.AfterNewTarget.ToString()) {
                return EventContainerType.AfterNewTarget;
            }

            if (eventContainerType == EventContainerType.AfterEachTarget.ToString()) {
                return EventContainerType.AfterEachTarget;
            }

            throw new ArgumentException($"unknown event container type : {eventContainerType}");
        }
    }
}