using NzbDrone.Common;
using NzbDrone.Core.Parser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace NzbDrone.Core.Indexers.Newznab
{
    public class OzNzbParser : NewznabParser
    {
        protected override ReleaseUserRatings GetUserRatings(XElement item)
        {
            var spam_reports = GetNewznabAttribute(item, "oz_num_spam_reports");
            var spam_confirmed = GetNewznabAttribute(item, "oz_spam_confirmed");
            var passworded_reports = GetNewznabAttribute(item, "oz_num_passworded_reports");
            var passworded_confirmed = GetNewznabAttribute(item, "oz_passworded_confirmed");
            var up_votes = GetNewznabAttribute(item, "oz_up_votes");
            var down_votes = GetNewznabAttribute(item, "oz_down_votes");
            var video_quality_rating = GetNewznabAttribute(item, "oz_video_quality_rating");
            var audio_quality_rating = GetNewznabAttribute(item, "oz_audio_quality_rating");

            var userRatings = new ReleaseUserRatings();

            if (!spam_reports.IsNullOrWhiteSpace())
            {
                userRatings.SpamReports = Convert.ToInt32(spam_reports);
            }

            userRatings.IsSpamConfirmed = spam_confirmed == "yes";

            if (!passworded_reports.IsNullOrWhiteSpace())
            {
                userRatings.PasswordedReports = Convert.ToInt32(passworded_reports);
            }

            userRatings.IsPasswordedConfirmed = passworded_confirmed == "yes";
            
            if (!up_votes.IsNullOrWhiteSpace() && up_votes != "-")
            {
                userRatings.UpVotes = Convert.ToInt32(up_votes);
            }

            if (!down_votes.IsNullOrWhiteSpace() && down_votes != "-")
            {
                userRatings.DownVotes = Convert.ToInt32(down_votes);
            }
            
            if (!video_quality_rating.IsNullOrWhiteSpace() && video_quality_rating != "-")
            {
                userRatings.VideoRating = (Convert.ToInt32(video_quality_rating) - 1) / 9.0;
            }

            if (!audio_quality_rating.IsNullOrWhiteSpace() && audio_quality_rating != "-")
            {
                userRatings.AudioRating = (Convert.ToInt32(audio_quality_rating) - 1) / 9.0;
            }

            return userRatings;
        }
    }
}
