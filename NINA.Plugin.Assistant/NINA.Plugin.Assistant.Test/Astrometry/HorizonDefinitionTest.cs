using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Astrometry.Solver;
using FluentAssertions;
using NINA.Core.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Plugin.Assistant.Test.Astrometry {

    [TestFixture]
    public class HorizonDefinitionTest {

        [Test]
        public void TestMinimumAltitudeOnly() {
            HorizonDefinition sut = new HorizonDefinition(11);
            sut.IsCustom().Should().BeFalse();
            sut.GetFixedMinimumAltitude().Should().Be(11);
            sut.GetTargetAltitude(null).Should().Be(11);
        }

        [Test]
        public void TestConstantCustomHorizon() {
            CustomHorizon ch = HorizonDefinition.GetConstantHorizon(21);
            HorizonDefinition sut = new HorizonDefinition(ch, 0);
            sut.IsCustom().Should().BeTrue();
            sut.GetTargetAltitude(GetAAT(10)).Should().Be(21);
            sut.GetTargetAltitude(GetAAT(36)).Should().Be(21);
            sut.GetTargetAltitude(GetAAT(188)).Should().Be(21);
            sut.GetTargetAltitude(GetAAT(301)).Should().Be(21);

            var ex = Assert.Throws<ArgumentException>(() => sut.GetFixedMinimumAltitude());
            Assert.AreEqual("minimumAltitude n/a in this context", ex.Message);
        }

        [Test]
        public void TestConstantCustomHorizonWithOffset() {
            CustomHorizon ch = HorizonDefinition.GetConstantHorizon(21);
            HorizonDefinition sut = new HorizonDefinition(ch, 11);
            sut.IsCustom().Should().BeTrue();
            sut.GetTargetAltitude(GetAAT(10)).Should().Be(32);
            sut.GetTargetAltitude(GetAAT(36)).Should().Be(32);
            sut.GetTargetAltitude(GetAAT(188)).Should().Be(32);
            sut.GetTargetAltitude(GetAAT(301)).Should().Be(32);
        }

        [Test]
        public void TestCustomHorizon() {
            SortedDictionary<double, double> altitudes = new SortedDictionary<double, double>();
            altitudes.Add(0, 10);
            altitudes.Add(90, 30);
            altitudes.Add(180, 20);
            altitudes.Add(270, 20);

            CustomHorizon ch = GetCustomHorizon(altitudes);
            HorizonDefinition sut = new HorizonDefinition(ch, 0);
            sut.GetTargetAltitude(GetAAT(10)).Should().BeApproximately(12.222, 0.001);
            sut.GetTargetAltitude(GetAAT(36)).Should().BeApproximately(18.0, 0.001);
            sut.GetTargetAltitude(GetAAT(188)).Should().BeApproximately(20.0, 0.001);
            sut.GetTargetAltitude(GetAAT(301)).Should().BeApproximately(16.555, 0.001);
        }

        [Test]
        public void TestCustomHorizonWithOffset() {
            SortedDictionary<double, double> altitudes = new SortedDictionary<double, double>();
            altitudes.Add(0, 10);
            altitudes.Add(90, 30);
            altitudes.Add(180, 20);
            altitudes.Add(270, 20);

            CustomHorizon ch = GetCustomHorizon(altitudes);
            HorizonDefinition sut = new HorizonDefinition(ch, 11);
            sut.GetTargetAltitude(GetAAT(10)).Should().BeApproximately(23.222, 0.001);
            sut.GetTargetAltitude(GetAAT(36)).Should().BeApproximately(29.0, 0.001);
            sut.GetTargetAltitude(GetAAT(188)).Should().BeApproximately(31.0, 0.001);
            sut.GetTargetAltitude(GetAAT(301)).Should().BeApproximately(27.555, 0.001);
        }

        [Test]
        public void TestCustomHorizonWithMinimum() {
            SortedDictionary<double, double> altitudes = new SortedDictionary<double, double>();
            altitudes.Add(0, 10);
            altitudes.Add(90, 30);
            altitudes.Add(180, 20);
            altitudes.Add(270, 20);

            CustomHorizon ch = GetCustomHorizon(altitudes);
            HorizonDefinition sut = new HorizonDefinition(ch, 0, 17);
            sut.GetTargetAltitude(GetAAT(10)).Should().BeApproximately(17, 0.001);
            sut.GetTargetAltitude(GetAAT(36)).Should().BeApproximately(18.0, 0.001);
            sut.GetTargetAltitude(GetAAT(188)).Should().BeApproximately(20.0, 0.001);
            sut.GetTargetAltitude(GetAAT(301)).Should().BeApproximately(17.0, 0.001);
        }

        [Test]
        public void TestCustomHorizonWithOffsetAndMinimum() {
            SortedDictionary<double, double> altitudes = new SortedDictionary<double, double>();
            altitudes.Add(0, 10);
            altitudes.Add(90, 30);
            altitudes.Add(180, 20);
            altitudes.Add(270, 20);

            CustomHorizon ch = GetCustomHorizon(altitudes);
            HorizonDefinition sut = new HorizonDefinition(ch, 6, 25);
            sut.GetTargetAltitude(GetAAT(10)).Should().BeApproximately(25, 0.001);
            sut.GetTargetAltitude(GetAAT(36)).Should().BeApproximately(25.0, 0.001);
            sut.GetTargetAltitude(GetAAT(188)).Should().BeApproximately(26.0, 0.001);
            sut.GetTargetAltitude(GetAAT(301)).Should().BeApproximately(25.0, 0.001);
        }

        private AltitudeAtTime GetAAT(double azimuth) {
            return new AltitudeAtTime(0, azimuth, DateTime.MinValue);
        }

        private CustomHorizon GetCustomHorizon(SortedDictionary<double, double> altitudes) {
            StringBuilder horizonDefinition = new StringBuilder();
            foreach (var entry in altitudes) {
                string az = String.Format("{0:F0}", entry.Key);
                string alt = String.Format("{0:F0}", entry.Value);
                horizonDefinition.Append($"{az} {alt}{Environment.NewLine}");
            }

            using (var sr = new StringReader(horizonDefinition.ToString())) {
                return CustomHorizon.FromReader_Standard(sr);
            }
        }

    }
}
