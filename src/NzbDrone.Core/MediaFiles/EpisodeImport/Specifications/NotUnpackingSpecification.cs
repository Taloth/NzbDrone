﻿using System;
using System.IO;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Specifications
{
    public class NotUnpackingSpecification : IImportDecisionEngineSpecification
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public NotUnpackingSpecification(IDiskProvider diskProvider, IConfigService configService, Logger logger)
        {
            _diskProvider = diskProvider;
            _configService = configService;
            _logger = logger;
        }

        public string RejectionReason { get { return "File is still being unpacked"; } }

        public bool IsSatisfiedBy(LocalEpisode localEpisode)
        {
            if (localEpisode.ExistingFile)
            {
                _logger.Debug("{0} is in series folder, unpacking check", localEpisode.FileSet.VideoFile);
                return true;
            }

            foreach (var workingFolder in _configService.DownloadClientWorkingFolders.Split('|'))
            {
                if (Directory.GetParent(localEpisode.FileSet.VideoFile).Name.StartsWith(workingFolder))
                {
                    if (OsInfo.IsMono)
                    {
                        _logger.Debug("{0} is still being unpacked", localEpisode.FileSet.VideoFile);
                        return false;
                    }

                    if (_diskProvider.FileGetLastWriteUtc(localEpisode.FileSet.VideoFile) > DateTime.UtcNow.AddMinutes(-5))
                    {
                        _logger.Debug("{0} appears to be unpacking still", localEpisode.FileSet.VideoFile);
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
