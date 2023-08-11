using Assistant.NINAPlugin.Controls.AssistantManager;
using Assistant.NINAPlugin.Database.Schema;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NINA.Plugin.Assistant.Test.Controls.AssistantManager {

    [TestFixture]
    public class OverrideExposureOrderTest {

        [Test]
        public void TestInit() {
            var sut = new OverrideExposureOrder(GetEPList());
            sut.OverrideItems.Count.Should().Be(4);
            sut.Serialize().Should().Be("1|2|3|4");
        }

        [Test]
        public void TestDeserialize() {
            string[] items = { "1", "1", "1", OverrideExposureOrder.DITHER, "2", "3", "4", OverrideExposureOrder.DITHER };
            string serialized = string.Join(OverrideExposureOrder.SEP, items);

            var sut = new OverrideExposureOrder(serialized, GetEPList());

            sut.OverrideItems.Count.Should().Be(8);

            OverrideItem oi = sut.OverrideItems[0];
            oi.IsDither.Should().BeFalse();
            oi.ExposurePlan.Id.Should().Be(1);

            oi = sut.OverrideItems[1];
            oi.IsDither.Should().BeFalse();
            oi.ExposurePlan.Id.Should().Be(1);

            oi = sut.OverrideItems[2];
            oi.IsDither.Should().BeFalse();
            oi.ExposurePlan.Id.Should().Be(1);

            oi = sut.OverrideItems[3];
            oi.IsDither.Should().BeTrue();
            oi.ExposurePlan.Should().BeNull();

            oi = sut.OverrideItems[4];
            oi.IsDither.Should().BeFalse();
            oi.ExposurePlan.Id.Should().Be(2);

            oi = sut.OverrideItems[5];
            oi.IsDither.Should().BeFalse();
            oi.ExposurePlan.Id.Should().Be(3);

            oi = sut.OverrideItems[6];
            oi.IsDither.Should().BeFalse();
            oi.ExposurePlan.Id.Should().Be(4);

            oi = sut.OverrideItems[7];
            oi.IsDither.Should().BeTrue();
            oi.ExposurePlan.Should().BeNull();
        }

        [Test]
        public void TestSerialize() {
            string[] items = { "1", "1", "1", OverrideExposureOrder.DITHER, "2", "3", "4", OverrideExposureOrder.DITHER };
            string serialized = string.Join(OverrideExposureOrder.SEP, items);

            var sut = new OverrideExposureOrder(serialized, GetEPList());

            string serialized2 = sut.Serialize();
            serialized2.Should().Be(serialized);
            serialized2.Should().Be("1|1|1|Dither|2|3|4|Dither");

            sut = new OverrideExposureOrder(serialized2, GetEPList());
            sut.OverrideItems.Count.Should().Be(8);
        }

        [Test]
        public void TestDeserializeBreak() {
            string[] items = { "1", "1", "1", OverrideExposureOrder.DITHER, "2", "9", "4", OverrideExposureOrder.DITHER };
            string serialized = string.Join(OverrideExposureOrder.SEP, items);

            Action act = () => new OverrideExposureOrder(serialized, GetEPList());
            act.Should().Throw<Exception>().Where(e => e.Message == "failed to find exposure plan for override order: 9");
        }

        private List<ExposurePlan> GetEPList() {
            int i = 1;
            List<ExposurePlan> list = new List<ExposurePlan>();

            ExposurePlan ep = new ExposurePlan("PID");
            ep.Id = i++;
            ep.ExposureTemplate = new ExposureTemplate("PID", "Lum", "Lum");
            list.Add(ep);

            ep = new ExposurePlan("PID");
            ep.Id = i++;
            ep.ExposureTemplate = new ExposureTemplate("PID", "Red", "Red");
            list.Add(ep);

            ep = new ExposurePlan("PID");
            ep.Id = i++;
            ep.ExposureTemplate = new ExposureTemplate("PID", "Green", "Green");
            list.Add(ep);

            ep = new ExposurePlan("PID");
            ep.Id = i++;
            ep.ExposureTemplate = new ExposureTemplate("PID", "Blue", "Blue");
            list.Add(ep);

            return list;
        }
    }
}
