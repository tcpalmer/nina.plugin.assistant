using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Plan.Scoring;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NINA.Plugin.Assistant.Test.Plan {

    [TestFixture]
    public class DitherInjectorTest {

        [Test]
        public void testNoOp() {
            List<IPlanInstruction> dithered = new DitherInjector(GetSequence("LLLsLLLsLLL"), 0).Inject();
            ExtractInstructions(dithered).Should().Be("LLLsLLLsLLL");

            Assert.IsNull(new DitherInjector(null, 1).Inject());

            dithered = new DitherInjector(GetSequence(""), 1).Inject();
            ExtractInstructions(dithered).Should().Be("");

            dithered = new DitherInjector(GetSequence("s"), 1).Inject();
            ExtractInstructions(dithered).Should().Be("s");

            dithered = new DitherInjector(GetSequence("sss"), 1).Inject();
            ExtractInstructions(dithered).Should().Be("sss");
        }

        [Test]
        public void testSingle() {
            string seq = "LLLsLLLsLLL";

            List<IPlanInstruction> dithered = new DitherInjector(GetSequence(seq), 1).Inject();
            ExtractInstructions(dithered).Should().Be("LdLdLsdLdLdLsdLdLdL");

            dithered = new DitherInjector(GetSequence(seq), 2).Inject();
            ExtractInstructions(dithered).Should().Be("LLdLsLdLLsdLLdL");

            dithered = new DitherInjector(GetSequence(seq), 3).Inject();
            ExtractInstructions(dithered).Should().Be("LLLsdLLLsdLLL");
        }

        [Test]
        public void testMultiple() {
            string seq = "LRGBLRGBLRGBLLL";
            List<IPlanInstruction> dithered = new DitherInjector(GetSequence(seq), 1).Inject();
            ExtractInstructions(dithered).Should().Be("LRGBdLRGBdLRGBdLdLdL");

            dithered = new DitherInjector(GetSequence(seq), 2).Inject();
            ExtractInstructions(dithered).Should().Be("LRGBLRGBdLRGBLdLL");

            dithered = new DitherInjector(GetSequence(seq), 3).Inject();
            ExtractInstructions(dithered).Should().Be("LRGBLRGBLRGBdLLL");

            seq = "LLRRGGBBLL";
            dithered = new DitherInjector(GetSequence(seq), 1).Inject();
            ExtractInstructions(dithered).Should().Be("LdLRdRGdGBdBLdL");

            seq = "LLRRGGBBLL";
            dithered = new DitherInjector(GetSequence(seq), 2).Inject();
            ExtractInstructions(dithered).Should().Be("LLRRGGBBdLL");

            seq = "LLLRRRGGGBBBLLLRRRGGGBBBLLLRRRGGGBBBL";
            dithered = new DitherInjector(GetSequence(seq), 3).Inject();
            ExtractInstructions(dithered).Should().Be("LLLRRRGGGBBBdLLLRRRGGGBBBdLLLRRRGGGBBBdL");

            seq = "HHOOHHOO";
            dithered = new DitherInjector(GetSequence(seq), 2).Inject();
            ExtractInstructions(dithered).Should().Be("HHOOdHHOO");

            seq = "HHHH";
            dithered = new DitherInjector(GetSequence(seq), 2).Inject();
            ExtractInstructions(dithered).Should().Be("HHdHH");

            seq = "H";
            dithered = new DitherInjector(GetSequence(seq), 1).Inject();
            ExtractInstructions(dithered).Should().Be("H");

            seq = "LRGBLRGBLLL";
            dithered = new DitherInjector(GetSequence(seq), 1).Inject();
            ExtractInstructions(dithered).Should().Be("LRGBdLRGBdLdLdL");
        }

        private List<IPlanInstruction> GetSequence(string seq) {
            List<IPlanInstruction> list = new List<IPlanInstruction>();

            char[] chars = seq.ToCharArray();
            foreach (char c in chars) {
                switch (c) {
                    case 's':
                        list.Add(GetPlanSwitchFilter());
                        break;
                    default:
                        list.Add(GetPlanExposure(c.ToString()));
                        break;
                }
            }

            return list;
        }

        private PlanTakeExposure GetPlanExposure(string filterName) {
            Mock<IPlanExposure> pi = new Mock<IPlanExposure>();
            pi.SetupAllProperties();
            pi.SetupProperty(i => i.FilterName, filterName);
            return new PlanTakeExposure(pi.Object);
        }

        private PlanSwitchFilter GetPlanSwitchFilter() {
            Mock<IPlanExposure> pi = new Mock<IPlanExposure>();
            pi.SetupAllProperties();
            pi.SetupProperty(i => i.FilterName, "X");
            return new PlanSwitchFilter(pi.Object);
        }

        private string ExtractInstructions(List<IPlanInstruction> instructions) {
            string s = string.Empty;
            foreach (IPlanInstruction instruction in instructions) {
                if (instruction is PlanTakeExposure) {
                    s += ((PlanTakeExposure)instruction).planExposure.FilterName;
                    continue;
                }

                if (instruction is PlanDither) {
                    s += "d";
                    continue;
                }

                if (instruction is PlanSwitchFilter) {
                    s += "s";
                    continue;
                }

                throw new Exception($"unknown instruction type: {instruction.GetType()}");
            }

            return s;
        }
    }
}
