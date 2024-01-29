namespace Assistant.NINAPlugin.Sequencer {

    public interface IImageSaveWatcher {

        /// <summary>
        /// Start watching for image saves.
        /// </summary>
        void Start();

        /// <summary>
        /// Tell the watcher that it needs to wait on an exposure before stopping.
        /// </summary>
        /// <param name="imageId"></param>
        /// <param name="exposurePlanDatabaseId"></param>
        void WaitForExposure(int imageId, int exposurePlanDatabaseId);

        /// <summary>
        /// Wait for all exposures to be saved, then stop watching.
        /// </summary>
        void Stop();
    }
}