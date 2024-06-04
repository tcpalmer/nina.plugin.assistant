using Assistant.NINAPlugin.Plan;
using NINA.Plugin.Interfaces;

namespace Assistant.NINAPlugin.PubSub {

    public class TargetStartPublisher : TSPublisher {

        public TargetStartPublisher(IMessageBroker messageBroker) : base(messageBroker) {
        }

        public override string Topic => "TargetScheduler-TargetStart";
        public override int Version => 1;

        public void Publish(SchedulerPlan plan) {
            Publish(GetMessage(plan));
        }

        private IMessage GetMessage(SchedulerPlan plan) {
            TSMessage message = new TSMessage(Topic, plan.PlanTarget.Name, MessageSender, MessageSenderId, Version);
            message.Expiration = plan.PlanTarget.EndTime;
            message.CustomHeaders.Add("ProjectName", plan.PlanTarget.Project.Name);
            message.CustomHeaders.Add("Coordinates", plan.PlanTarget.Coordinates);
            message.CustomHeaders.Add("Rotation", plan.PlanTarget.Rotation);
            return message;
        }
    }
}