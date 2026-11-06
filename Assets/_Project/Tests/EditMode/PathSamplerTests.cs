using NovaSpark.Game.Level;
using NUnit.Framework;
using UnityEngine;

namespace NovaSpark.Tests
{
    public sealed class PathSamplerTests
    {
        private static PathSampler LShapedPath() => new PathSampler(new[]
        {
            new Vector2(0f, 0f),
            new Vector2(10f, 0f),
            new Vector2(10f, 5f),
        });

        [Test]
        public void TotalLength_SumsSegments()
        {
            Assert.AreEqual(15f, LShapedPath().TotalLength, 0.0001f);
        }

        [Test]
        public void SampleByDistance_InterpolatesWithinSegments()
        {
            var path = LShapedPath();
            Assert.That(Vector2.Distance(path.SampleByDistance(4f), new Vector2(4f, 0f)), Is.LessThan(0.0001f));
            Assert.That(Vector2.Distance(path.SampleByDistance(12f), new Vector2(10f, 2f)), Is.LessThan(0.0001f));
        }

        [Test]
        public void SampleByDistance_NegativeArc_ExtendsBehindPathStart()
        {
            var sample = LShapedPath().SampleByDistance(-3f);
            Assert.That(Vector2.Distance(sample, new Vector2(-3f, 0f)), Is.LessThan(0.0001f));
        }

        [Test]
        public void SampleByDistance_PastEnd_ClampsToLastPoint()
        {
            var sample = LShapedPath().SampleByDistance(100f);
            Assert.That(Vector2.Distance(sample, new Vector2(10f, 5f)), Is.LessThan(0.0001f));
        }

        [Test]
        public void SampleTangent_FollowsCurrentSegment()
        {
            var path = LShapedPath();
            Assert.That(Vector2.Distance(path.SampleTangent(4f), Vector2.right), Is.LessThan(0.0001f));
            Assert.That(Vector2.Distance(path.SampleTangent(12f), Vector2.up), Is.LessThan(0.0001f));
        }

        [Test]
        public void Constructor_RejectsPathsWithFewerThanTwoPoints()
        {
            Assert.Throws<System.ArgumentException>(() => new PathSampler(new[] { Vector2.zero }));
        }
    }
}
