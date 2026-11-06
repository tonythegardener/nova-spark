using NovaSpark.Core;
using NovaSpark.Game.Chain;
using NUnit.Framework;

namespace NovaSpark.Tests
{
    public sealed class SeamRulesTests
    {
        private const int Threshold = 3;

        private static MarbleColor[] Colors(params int[] values)
        {
            var colors = new MarbleColor[values.Length];
            for (int i = 0; i < values.Length; i++) colors[i] = (MarbleColor)values[i];
            return colors;
        }

        private static bool Freeze(MarbleColor[] chain, int headFlank, int tailFlank)
        {
            return SeamRules.ShouldFreeze(chain.Length, i => chain[i], headFlank, tailFlank, Threshold);
        }

        [Test]
        public void ExactlyThresholdAcrossSeam_Retreats_WhenTwoMarblesAreOnTheHeadSide()
        {
            // [X, A, A] gap [A, X]: closing the gap makes a run of exactly 3.
            var chain = Colors(1, 0, 0, 0, 1);
            Assert.IsFalse(Freeze(chain, headFlank: 2, tailFlank: 3));
        }

        [Test]
        public void ExactlyThresholdAcrossSeam_Retreats_WhenOneMarbleIsOnTheHeadSide()
        {
            // [X, A] gap [A, A]: the head flank itself must count toward the run.
            var chain = Colors(1, 0, 0, 0);
            Assert.IsFalse(Freeze(chain, headFlank: 1, tailFlank: 2));
        }

        [Test]
        public void RunBelowThreshold_Freezes()
        {
            // [X, A] gap [A, X]: closing the gap only makes a pair.
            var chain = Colors(1, 0, 0, 1);
            Assert.IsTrue(Freeze(chain, headFlank: 1, tailFlank: 2));
        }

        [Test]
        public void DifferentFlankColors_Freezes()
        {
            var chain = Colors(0, 0, 1, 1);
            Assert.IsTrue(Freeze(chain, headFlank: 1, tailFlank: 2));
        }

        [Test]
        public void CountRunAcrossSeam_StopsAtOtherColors()
        {
            var chain = Colors(0, 1, 0, 0, 0, 1, 0);
            int count = SeamRules.CountRunAcrossSeam(chain.Length, i => chain[i], 2, 3, (MarbleColor)0);
            Assert.AreEqual(3, count);
        }
    }
}
