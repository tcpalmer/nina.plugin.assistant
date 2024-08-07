using FluentAssertions;
using NINA.Plugin.Assistant.Shared.Utility;
using NUnit.Framework;
using System;

namespace NINA.Plugin.Assistant.Test.EventContainer {

    [TestFixture]
    public class EventContainerHelperTest {

        [Test]
        public void TestConvert() {
            EventContainerHelper.Convert(EventContainerType.BeforeWait.ToString()).Should().Be(EventContainerType.BeforeWait);
            EventContainerHelper.Convert(EventContainerType.AfterWait.ToString()).Should().Be(EventContainerType.AfterWait);
            EventContainerHelper.Convert(EventContainerType.BeforeTarget.ToString()).Should().Be(EventContainerType.BeforeTarget);
            EventContainerHelper.Convert(EventContainerType.AfterTarget.ToString()).Should().Be(EventContainerType.AfterTarget);
            EventContainerHelper.Convert(EventContainerType.AfterEachTarget.ToString()).Should().Be(EventContainerType.AfterEachTarget);
        }

        [Test]
        public void TestConvertBad() {
            Action convert = () => EventContainerHelper.Convert(null);
            convert
                .Should().Throw<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'eventContainerType')");

            convert = () => EventContainerHelper.Convert("");
            convert
                .Should().Throw<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'eventContainerType')");

            convert = () => EventContainerHelper.Convert("foo");
            convert
                .Should().Throw<ArgumentException>()
                .WithMessage("unknown event container type : foo");
        }
    }
}