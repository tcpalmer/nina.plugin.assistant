
namespace Assistant.NINAPlugin.Sequencer {

    public interface ISyncImageSaveWatcher {

        /// <summary>
        /// Start watching for image saves.
        /// </summary>
        void Start();

        /// <summary>
        /// Tell the watcher that it needs to wait on an exposure before stopping.
        /// </summary>
        /// <param name="imageId"></param>
        /// <param name="exposurePlanDatabaseId"></param>
        /// <param name="exposureId"></param>
        void WaitForExposure(int imageId, int targetDatabaseId, int exposurePlanDatabaseId, string exposureId);

        /// <summary>
        /// Wait for all exposures to be saved, then stop watching.
        /// </summary>
        void Stop();
    }
}
