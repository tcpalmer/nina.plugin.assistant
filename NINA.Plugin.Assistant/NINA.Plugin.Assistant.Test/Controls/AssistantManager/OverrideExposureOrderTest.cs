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
            sut.Serialize().Should().Be("0|1|2|3");
        }

        [Test]
        public void TestDeserialize() {
            string[] items = { "0", "0", "0", OverrideExposureOrder.DITHER, "1", "2", "3", OverrideExposureOrder.DITHER };
            string serialized = string.Join(OverrideExposureOrder.SEP, items);

            var sut = new OverrideExposureOrder(serialized, GetEPList());

            sut.OverrideItems.Count.Should().Be(8);

            OverrideItem oi = sut.OverrideItems[0];
            oi.IsDither.Should().BeFalse();
            oi.ExposurePlanIndex.Should().Be(0);

            oi = sut.OverrideItems[1];
            oi.IsDither.Should().BeFalse();
            oi.ExposurePlanIndex.Should().Be(0);

            oi = sut.OverrideItems[2];
            oi.IsDither.Should().BeFalse();
            oi.ExposurePlanIndex.Should().Be(0);

            oi = sut.OverrideItems[3];
            oi.IsDither.Should().BeTrue();
            oi.ExposurePlanIndex.Should().Be(-1);

            oi = sut.OverrideItems[4];
            oi.IsDither.Should().BeFalse();
            oi.ExposurePlanIndex.Should().Be(1);

            oi = sut.OverrideItems[5];
            oi.IsDither.Should().BeFalse();
            oi.ExposurePlanIndex.Should().Be(2);

            oi = sut.OverrideItems[6];
            oi.IsDither.Should().BeFalse();
            oi.ExposurePlanIndex.Should().Be(3);

            oi = sut.OverrideItems[7];
            oi.IsDither.Should().BeTrue();
            oi.ExposurePlanIndex.Should().Be(-1);
        }

        [Test]
        public void TestSerialize() {
            string[] items = { "0", "0", "0", OverrideExposureOrder.DITHER, "1", "2", "3", OverrideExposureOrder.DITHER };
            string serialized = string.Join(OverrideExposureOrder.SEP, items);

            var sut = new OverrideExposureOrder(serialized, GetEPList());

            string serialized2 = sut.Serialize();
            serialized2.Should().Be(serialized);
            serialized2.Should().Be("0|0|0|Dither|1|2|3|Dither");

            sut = new OverrideExposureOrder(serialized2, GetEPList());
            sut.OverrideItems.Count.Should().Be(8);
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
