﻿using Assistant.NINAPlugin.Util;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NINA.Plugin.Assistant.Test.Util {

    [TestFixture]
    public class UtilsTest {

        [Test]
        [TestCase(0, "0h 0m")]
        [TestCase(32, "0h 32m")]
        [TestCase(61, "1h 1m")]
        [TestCase(719, "11h 59m")]
        public void TestMtoHM(int min, string expected) {
            Utils.MtoHM(min).Should().Be(expected);
        }

        [Test]
        [TestCase(null, 0)]
        [TestCase("", 0)]
        [TestCase("0h 0m", 0)]
        [TestCase("0h 32m", 32)]
        [TestCase("1h 1m", 61)]
        [TestCase("11h 59m", 719)]
        public void TestHMtoM(string hm, int expected) {
            Utils.HMtoM(hm).Should().Be(expected);
        }

        [Test]
        [TestCase(null, " (1)")]
        [TestCase("", " (1)")]
        [TestCase("foo", "foo (1)")]
        [TestCase("foo (1)", "foo (2)")]
        [TestCase("foo (99)", "foo (100)")]
        public void TestCopiedItemName(string name, string expected) {
            string sut = Utils.CopiedItemName(name);
            sut.Should().Be(expected);
        }

        [Test]
        public void TestMakeUniqueName() {
            Utils.MakeUniqueName(null, null).Should().Be(" (1)");
            Utils.MakeUniqueName(null, "").Should().Be(" (1)");
            Utils.MakeUniqueName(null, "foo").Should().Be("foo");
            Utils.MakeUniqueName(new List<string>(), "foo").Should().Be("foo");

            List<string> current = new List<string>() { "foo", "bar" };
            Utils.MakeUniqueName(current, "foo").Should().Be("foo (1)");

            current.Add("foo (16)");
            Utils.MakeUniqueName(current, "foo").Should().Be("foo (17)");

            current.Add("foo (1)");
            current.Add("foo (8)");
            Utils.MakeUniqueName(current, "foo").Should().Be("foo (17)");
            Utils.MakeUniqueName(current, "foo (7)").Should().Be("foo (7)");
            Utils.MakeUniqueName(current, "foo (8)").Should().Be("foo (17)");
        }

        [Test]
        public void TestMidpoint() {
            DateTime start = DateTime.Now;
            DateTime mid = Utils.GetMidpointTime(start, start.AddHours(1));
            mid.Should().Be(start.AddMinutes(30));
        }

        [Test]
        [TestCase(0, "0h 0m 0s")]
        [TestCase(75, "5h 0m 0s")]
        [TestCase(90.25, "6h 1m 0s")]
        [TestCase(345.25, "23h 1m 0s")]
        public void TestGetRAString(double raDegrees, string expected) {
            Utils.GetRAString(raDegrees).Should().Be(expected);
        }

        [Test]
        public void TestCancelException() {
            Utils.IsCancelException(null).Should().BeFalse();
            Utils.IsCancelException(new Exception("foo")).Should().BeFalse();

            Utils.IsCancelException(new TaskCanceledException()).Should().BeTrue();
            Utils.IsCancelException(new OperationCanceledException()).Should().BeTrue();

            Utils.IsCancelException(new Exception("A task was canceled.")).Should().BeTrue();
            Utils.IsCancelException(new Exception("cancelled")).Should().BeTrue();
        }

        [Test]
        public void TestMoveFile() {
            string tempDir = null;
            try {
                tempDir = Path.Combine(Path.GetTempPath(), "movefiletest");
                Directory.CreateDirectory(tempDir);

                string srcFile1 = Path.Combine(tempDir, "test1.txt");
                File.WriteAllText(srcFile1, "this is test file 1");
                string srcFile2 = Path.Combine(tempDir, "test2.txt");
                File.WriteAllText(srcFile2, "this is test file 2");

                string dstDir = Path.Combine(Path.GetDirectoryName(srcFile1), "rejected");
                Utils.MoveFile(srcFile1, dstDir).Should().BeTrue();
                Utils.MoveFile(srcFile2, dstDir).Should().BeTrue();

                string newFile1 = Path.Combine(dstDir, Path.GetFileName(srcFile1));
                File.Exists(newFile1).Should().BeTrue();

                string newFile2 = Path.Combine(dstDir, Path.GetFileName(srcFile2));
                File.Exists(newFile2).Should().BeTrue();
            } finally {
                Directory.Delete(tempDir, true);
            }
        }
    }
}