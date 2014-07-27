using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.Test.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NzbDrone.Core.Test.IndexerTests.NewznabTests
{
    [TestFixture]
    public class OzNzbParserFixture : CoreTest<OzNzbParser>
    {
        private String _xml;

        [SetUp]
        public void SetUp()
        {
            _xml = ReadAllText(@"Files\RSS\oznzb.xml");
        }

        [Test]
        public void should_return_upvotes_if_available()
        {
            var result = Subject.Process(_xml, "");
            
            result.First().UserRatings.UpVotes.HasValue.Should().BeTrue();
            result.First().UserRatings.UpVotes.Value.Should().Be(2);
        }

        [Test]
        public void should_not_return_upvotes_if_unavailable()
        {
            _xml = _xml.Replace("oz_up_votes", "__ignore__");

            var result = Subject.Process(_xml, "");

            result.First().UserRatings.UpVotes.HasValue.Should().BeFalse();
        }

        [TestCase(1, 0.0)]
        [TestCase(10, 1.0)]
        [TestCase(5, 4 / 9.0)]
        public void should_normalize_video_and_audio_rating(Int32 rating, Double expectedFactor)
        {
            _xml = _xml.Replace("\"oz_video_quality_rating\" value=\"6\"", "\"oz_video_quality_rating\" value=\"" + rating + "\"");
            _xml = _xml.Replace("\"oz_audio_quality_rating\" value=\"6\"", "\"oz_audio_quality_rating\" value=\"" + rating + "\"");

            var result = Subject.Process(_xml, "");

            result.First().UserRatings.VideoRating.HasValue.Should().BeTrue();
            result.First().UserRatings.AudioRating.HasValue.Should().BeTrue();
            result.First().UserRatings.VideoRating.Value.Should().Be(expectedFactor);
            result.First().UserRatings.AudioRating.Value.Should().Be(expectedFactor);
        }
    }
}
