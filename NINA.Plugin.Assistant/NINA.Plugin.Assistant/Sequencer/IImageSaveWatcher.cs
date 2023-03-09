namespace Assistant.NINAPlugin.Sequencer {

    public interface IImageSaveWatcher {

        int PlanExposureDatabaseId { get; set; }
        void Start();
        void Stop();

    }
}
