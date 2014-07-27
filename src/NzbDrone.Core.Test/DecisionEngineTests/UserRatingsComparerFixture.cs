using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class UserRatingsComparerFixture : CoreTest<UserRatingsComparer>
    {
        [Test]
        public void should_consider_null_equal()
        {
            Subject.Compare(null, null).Should().Be(0);
        }

        [Test]
        public void should_consider_not_null_better()
        {
            Subject.Compare(new ReleaseUserRatings(), null).Should().Be(1);
        }

        [Test]
        public void should_consider_non_passworded_better()
        {
            Subject.Compare(new ReleaseUserRatings(), new ReleaseUserRatings() { IsPasswordedConfirmed = true }).Should().Be(1);
        }

        [Test]
        public void should_consider_non_spam_better()
        {
            Subject.Compare(new ReleaseUserRatings(), new ReleaseUserRatings() { IsSpamConfirmed = true }).Should().Be(1);
        }

        [Test]
        public void should_ignore_too_few_votes()
        {
            Subject.Compare(new ReleaseUserRatings() { DownVotes = 4 }, new ReleaseUserRatings()).Should().Be(0);
        }

        [Test]
        public void should_consider_two_thirds_downvotes_bad()
        {
            Subject.Compare(new ReleaseUserRatings() { DownVotes = 4, UpVotes = 1 }, new ReleaseUserRatings()).Should().Be(-1);
        }

        [Test]
        public void should_consider_two_thirds_upvotes_good()
        {
            Subject.Compare(new ReleaseUserRatings() { DownVotes = 1, UpVotes = 4 }, new ReleaseUserRatings()).Should().Be(1);
        }

        [Test]
        public void should_consider_large_upvotes_equal()
        {
            Subject.Compare(new ReleaseUserRatings() { UpVotes = 10 }, new ReleaseUserRatings() { UpVotes = 20 }).Should().Be(0);
        }

        [Test]
        public void should_consider_high_rating_better()
        {
            Subject.Compare(new ReleaseUserRatings() { VideoRating = 0.8 }, new ReleaseUserRatings() { VideoRating = 0.4 }).Should().Be(1);
        }

        [Test]
        public void should_consider_comparable_rating_equal()
        {
            Subject.Compare(new ReleaseUserRatings() { VideoRating = 0.8 }, new ReleaseUserRatings() { VideoRating = 0.75 }).Should().Be(0);
        }

        [Test]
        public void should_sort_bad_to_good()
        {
            var releases = new List<ReleaseUserRatings>();
            releases.Add(new ReleaseUserRatings() { DownVotes = 0, UpVotes = 1 });
            releases.Add(new ReleaseUserRatings() { DownVotes = 0, UpVotes = 10 });
            releases.Add(new ReleaseUserRatings() { DownVotes = 10, UpVotes = 0 });

            releases.Sort(Subject);

            releases.First().DownVotes.Should().Be(10);
            releases.Last().UpVotes.Should().Be(10);
        }
    }
}
