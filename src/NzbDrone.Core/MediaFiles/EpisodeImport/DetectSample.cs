﻿using System;
using System.IO;
using NLog;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.MediaFiles.EpisodeImport
{
    public interface IDetectSample
    {
        DetectSampleResult IsSample(Series series, string path, bool isSpecial);
    }

    public class DetectSample : IDetectSample
    {
        private readonly Logger _logger;

        public DetectSample(Logger logger)
        {
            _logger = logger;
        }

        public DetectSampleResult IsSample(Series series, string path, bool isSpecial)
        {
            if (isSpecial)
            {
                _logger.Debug("Special, skipping sample check");
                return DetectSampleResult.NotSample;
            }

            var extension = Path.GetExtension(path);

            if (extension != null && extension.Equals(".flv", StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.Debug("Skipping sample check for .flv file");
                return DetectSampleResult.NotSample;
            }

            if (extension != null && extension.Equals(".strm", StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.Debug("Skipping sample check for .strm file");
                return DetectSampleResult.NotSample;
            }

            _logger.Debug("Runtime is over 90 seconds");
            return DetectSampleResult.NotSample;
        }

        private int GetMinimumAllowedRuntime(Series series)
        {
            //Anime short - 15 seconds
            if (series.Runtime <= 3)
            {
                return 15;
            }

            //Webisodes - 90 seconds
            if (series.Runtime <= 10)
            {
                return 90;
            }

            //30 minute episodes - 5 minutes
            if (series.Runtime <= 30)
            {
                return 300;
            }

            //60 minute episodes - 10 minutes
            return 600;
        }
    }
}
