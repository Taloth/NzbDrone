using NzbDrone.Common;
using NzbDrone.Core.Parser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NzbDrone.Core.DecisionEngine
{
    public class UserRatingsComparer : IComparer<ReleaseUserRatings>
    {
        public Int32 Compare(ReleaseUserRatings x, ReleaseUserRatings y)
        {
            if (x == null && y == null) return 0;
            if (x == null && y != null) return -1;
            if (x != null && y == null) return 1;

            var xSpam = IsSpamOrPassworded(x);
            var ySpam = IsSpamOrPassworded(y);
            var result = -xSpam.CompareTo(ySpam);
            if (result != 0) return result;

            var xVotes = GetUpDownVoteClassification(x);
            var yVotes = GetUpDownVoteClassification(y);
            result = xVotes.CompareTo(yVotes);
            if (result != 0) return result;

            var xRating = GetVideoAudioRating(x);
            var yRating = GetVideoAudioRating(y);
            if (Math.Abs(xRating - yRating) > 0.2)
            {
                return xRating.CompareTo(yRating);
            }

            return 0;
        }

        private Boolean IsSpamOrPassworded(ReleaseUserRatings x)
        {
            return x.IsSpamConfirmed || x.IsPasswordedConfirmed;
        }

        private Int32 GetUpDownVoteClassification(ReleaseUserRatings x)
        {
            if (!x.UpVotes.HasValue || !x.DownVotes.HasValue)
            {
                return 0;
            }

            var totalVotes = x.UpVotes + x.DownVotes;

            if (totalVotes < 5)
            {
                return 0;
            }

            var upVotesPercentage = x.UpVotes / (Double)totalVotes;
            var downVotesPercentage = x.DownVotes / (Double)totalVotes;

            if (upVotesPercentage >= 2 / 3.0)
            {
                return 1;
            }
            else if (downVotesPercentage >= 2 / 3.0)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        private Double GetVideoAudioRating(ReleaseUserRatings x)
        {
            Double totalRating = 0.5;

            if (x.VideoRating.HasValue && x.AudioRating.HasValue)
            {
                totalRating = (x.VideoRating.Value + x.AudioRating.Value) / 2.0;
            }
            else if (x.VideoRating.HasValue)
            {
                totalRating = x.VideoRating.Value;
            }
            else if (x.AudioRating.HasValue)
            {
                totalRating = x.AudioRating.Value;
            }

            return totalRating;
        }
    }
}
