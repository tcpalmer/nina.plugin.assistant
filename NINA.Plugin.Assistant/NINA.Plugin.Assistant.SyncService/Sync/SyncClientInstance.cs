using Scheduler.SyncService;
using System;

namespace Assistant.NINAPlugin.Sync {

    class SyncClientInstance {
        public ClientState ClientState { get; private set; }
        public string Guid { get; private set; }
        public int Pid { get; private set; }
        public string ProfileId { get; private set; }
        public DateTime RegistrationDate { get; private set; }
        public DateTime LastAliveDate { get; private set; }

        public SyncClientInstance(RegistrationRequest request) {
            ClientState = ClientState.Starting;
            Guid = request.Guid;
            Pid = request.Pid;
            ProfileId = request.ProfileId;
            DateTime dateTime = request.Timestamp.ToDateTime().ToLocalTime();
            RegistrationDate = dateTime;
            LastAliveDate = dateTime;
        }

        public void SetState(ClientIdRequest request) {
            ClientState = request.ClientState;
        }

        public void SetLastAliveDate(ClientIdRequest request) {
            LastAliveDate = DateTime.Now;
        }

        public override string ToString() {
            return $"guid={Guid}, state={ClientState}, reg date={RegistrationDate}, alive date={LastAliveDate}";
        }
    }
}
