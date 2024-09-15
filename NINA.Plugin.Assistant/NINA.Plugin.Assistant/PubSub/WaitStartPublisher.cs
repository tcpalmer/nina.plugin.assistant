using NINA.Plugin.Interfaces;
using System;

namespace Assistant.NINAPlugin.PubSub {

    public class WaitStartPublisher : TSPublisher {

        public WaitStartPublisher(IMessageBroker messageBroker) : base(messageBroker) {
        }

        public override string Topic => "TargetScheduler-WaitStart";
        public override int Version => 1;

        public void Publish(DateTime waitUntil) {
            Publish(GetMessage(waitUntil));
        }

        private IMessage GetMessage(DateTime waitUntil) {
            TSMessage message = new TSMessage(Topic, waitUntil, MessageSender, MessageSenderId, Version);
            message.Expiration = waitUntil;
            return message;
        }
    }
}