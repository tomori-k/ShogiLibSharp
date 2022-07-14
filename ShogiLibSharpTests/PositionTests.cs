﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShogiLibSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Tests
{
    [TestClass()]
    public class PositionTests
    {
        [TestMethod()]
        public void CanDeclareWinTest()
        {
            var testcases = new[]
            {
                // 1点足りない
                ("3+N5/5G+PB1/+PR+PPKP2+P/9/9/9/9/9/4k4 b G2SN2L3Prb2g2s2n2l9p 1", false),
                // 玉...
                ("3+N5/5G+PB1/+PR+PP1P2+P/4K4/9/9/9/9/4k4 b G2SN2L4Prb2g2s2n2l8p 1", false),
                // 王手がかかっている
                ("3+N5/5G+PB1/+PR+PPKP2+P/9/9/9/9/4r4/4k4 b G2SN2L4Pb2g2s2n2l8p 1", false),
                // 10枚ない
                ("3+N5/5G+PB1/+PR+P1KP2+P/9/9/9/9/9/4k4 b G2SN2L5Prb2g2s2n2l8p 1", false),
                // 1点足りない
                ("1S3K1B1/2+R1+B2S+N/1L2+L1R+N1/9/9/9/9/9/4k4 b G3g2s2n2l18p 1", false),
                ("1S3K1B1/2+R1+B2S+N/1L2+L1R+N1/9/9/9/9/9/4k4 b GP3g2s2n2l17p 1", true),
                ("3+N5/5G+PB1/+PR+PPKP2+P/9/9/9/9/9/4k4 b G2SN2L4Prb2g2s2n2l8p 1", true),
                // 後手は27点でいい
                ("4K4/9/9/9/9/9/nn+r1l1l2/1+s2b1r2/1b1k3s1 w 3G2S2N2L18Pg 1", true),
                // 1枚足りない
                ("4K4/9/9/9/9/9/nn+r1l1l2/1+s2b1r2/1b1k3s1 w 4G2S2N2L18P 1", false),
            };
            foreach (var (sfen, expected) in testcases)
            {
                var pos = new Position(sfen);
                var actual = pos.CanDeclareWin();
                Assert.AreEqual(actual, expected, $"failed: sfen={sfen},expected={expected},actual={actual}");
            }
        }

        [TestMethod()]
        [DeploymentItem(@"kifu/sennichite/1.csa")]
        [DeploymentItem(@"kifu/sennichite/2.csa")]
        [DeploymentItem(@"kifu/not_sennichite/1.csa")]
        [DeploymentItem(@"kifu/not_sennichite/2.csa")]
        public void CheckRepetitionTest()
        {
            CheckRepetitionTest1();
            CheckRepetitionTest2(new[] {
                @"kifu/sennichite/1.csa",
                @"kifu/sennichite/2.csa",
            }, true);
            CheckRepetitionTest2(new[] {
                @"kifu/not_sennichite/1.csa",
                @"kifu/not_sennichite/2.csa",
            }, false);
        }

        private void CheckRepetitionTest1()
        {
            var testcases = new[]
            {
                ("5k3/9/9/4B4/9/9/9/9/8K b RB2G2S2N3L6Pr2g2s2nl12p 1", new[] { "5d6c", "4a3b", "6c5d", "3b4a" }, Repetition.Lose),
                ("9/9/1k4+R2/9/9/9/9/9/8K w RB2G2S2N3L6Pb2g2s2nl12p 1", new[] { "8c8d", "3c3d", "8d8c", "3d3c" }, Repetition.Win),
                ("9/8r/9/9/7K1/9/9/9/k8 w RB3G2S2N3L7Pbg2snl11p 1", new[] { "1b2b", "R*2d", "2b2d", "2e2d", "R*1b", "2d2e" }, Repetition.Draw),
                ("9/8r/9/9/8K/9/9/9/k8 b RB3G2S2N3L7Pbg2snl11p 1", new[] { "1e2e", "1b2b", "2e3e", "2b3b", "3e4e", "3b4b", "4e5e", "4b5b", "5e4f", "5b4b", "4f3f", "4b3b", "3f2f", "3b2b", "2f1e", "2b1b" }, Repetition.Win),
                ("9/8r/9/9/8K/9/9/9/k8 b RB3G2S2N3L7Pbg2snl11p 1", new[] { "1e2e", "1b4b", "2e3e", "4b3b", "3e4e", "3b4b", "4e5e", "4b5b", "5e4f", "5b4b", "4f3f", "4b3b", "3f2f", "3b2b", "2f1e", "2b1b" }, Repetition.Draw),
            };
            foreach (var (sfen, cycle, expected) in testcases)
            {
                var pos = new Position(sfen);
                for (var i = 0; i < 3; ++i)
                {
                    foreach (var m in cycle)
                    {
                        Assert.AreEqual(Repetition.None, pos.CheckRepetition());
                        pos.DoMove(Usi.ParseMove(m));
                    }
                }
                Assert.AreEqual(expected, pos.CheckRepetition());
            }
        }

        private void CheckRepetitionTest2(string[] fileNames, bool repeated)
        {
            foreach (var fileName in fileNames)
            {
                var kifu = File.ReadAllText(fileName);
                var (sfen, moves) = Csa.ParseKifu(kifu);
                var pos = new Position(sfen);
                foreach (var m in moves)
                {
                    Assert.AreEqual(Repetition.None, pos.CheckRepetition());
                    pos.DoMove(m);
                }
                Assert.AreEqual(repeated, pos.CheckRepetition() == Repetition.Draw);
            }
        }
    }
}