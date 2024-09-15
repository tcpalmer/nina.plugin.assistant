using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Interfaces;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.PubSub {

    public class TSLoggingSubscriber : ISubscriber {

        public Task OnMessageReceived(IMessage message) {
            TSLogger.Info($"Received message:\n{TSMessage.LogReceived(message)}");
            return Task.CompletedTask;
        }
    }
}