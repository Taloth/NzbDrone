using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.EpisodeImport;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public class DownloadedEpisodesImportService : IExecute<DownloadedEpisodesScanCommand>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IDiskScanService _diskScanService;
        private readonly ISeriesService _seriesService;
        private readonly IParsingService _parsingService;
        private readonly IConfigService _configService;
        private readonly IMakeImportDecision _importDecisionMaker;
        private readonly IImportApprovedEpisodes _importApprovedEpisodes;
        private readonly ISampleService _sampleService;
        private readonly Logger _logger;

        public DownloadedEpisodesImportService(IDiskProvider diskProvider,
            IDiskScanService diskScanService,
            ISeriesService seriesService,
            IParsingService parsingService,
            IConfigService configService,
            IMakeImportDecision importDecisionMaker,
            IImportApprovedEpisodes importApprovedEpisodes,
            ISampleService sampleService,
            Logger logger)
        {
            _diskProvider = diskProvider;
            _diskScanService = diskScanService;
            _seriesService = seriesService;
            _parsingService = parsingService;
            _configService = configService;
            _importDecisionMaker = importDecisionMaker;
            _importApprovedEpisodes = importApprovedEpisodes;
            _sampleService = sampleService;
            _logger = logger;
        }

        private void ProcessDownloadedEpisodesFolder()
        {
            //TODO: We should also process the download client's category folder
            var downloadedEpisodesFolder = _configService.DownloadedEpisodesFolder;

            if (String.IsNullOrEmpty(downloadedEpisodesFolder))
            {
                _logger.Warn("Drone Factory folder is not configured");
                return;
            }

            if (!_diskProvider.FolderExists(downloadedEpisodesFolder))
            {
                _logger.Warn("Drone Factory folder [{0}] doesn't exist.", downloadedEpisodesFolder);
                return;
            }

            foreach (var subFolder in _diskProvider.GetDirectories(downloadedEpisodesFolder))
            {
                ProcessFolder(subFolder);
            }

            foreach (var fileSet in _diskScanService.GetFileSets(downloadedEpisodesFolder, false))
            {
                try
                {
                    ProcessRootFileSet(fileSet);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("An error has occurred while importing fileSet " + fileSet.VideoFile, ex);
                }
            }
        }

        private void ProcessFolder(string path)
        {
            Ensure.That(path, () => path).IsValidPath();

            try
            {
                var directoryInfo = new DirectoryInfo(path);

                foreach (var workingFolder in _configService.DownloadClientWorkingFolders.Split('|'))
                {
                    if (directoryInfo.Name.StartsWith(workingFolder))
                    {
                        _logger.Trace("{0} is still being unpacked", directoryInfo.FullName);
                        return;
                    }
                }

                if (_seriesService.SeriesPathExists(path))
                {
                    _logger.Warn("Unable to process folder that contains sorted TV Shows");
                    return;
                }

                var fileSets = _diskScanService.GetFileSets(directoryInfo.FullName, true);

                var importedFiles = ProcessFiles(fileSets, directoryInfo.Name);

                if (importedFiles.Any() && ShouldDeleteFolder(directoryInfo))
                {
                    _logger.Debug("Deleting folder after importing valid files");
                    _diskProvider.DeleteFolder(path, true);
                }
            }
            catch (Exception e)
            {
                _logger.ErrorException("An error has occurred while importing folder: " + path, e);
            }
        }

        private void ProcessRootFileSet(FileSet fileSet)
        {
            if (_diskProvider.IsFileLocked(fileSet.VideoFile))
            {
                _logger.Debug("[{0}] is currently locked by another process, skipping", fileSet.VideoFile);
                return;
            }

            ProcessFiles(new [] { fileSet });
        }

        private List<ImportDecision> ProcessFiles(IEnumerable<FileSet> fileSets, string directoryName = null)
        {
            // TODO: This belongs in the ImportDecisionMaker or ParsingService.

            Series directorySeries = null;
            ParsedEpisodeInfo directoryEpisodeInfo = null;
            Series fileSeries = null;
            
            if (directoryName != null)
            {
                directorySeries = _parsingService.GetSeries(directoryName);
                directoryEpisodeInfo = Parser.Parser.ParsePath(directoryName);
                
                if (directoryEpisodeInfo != null)
                _logger.Debug("{0} folder quality: {1}", directoryName, directoryEpisodeInfo.Quality);
            }
            
            foreach (var fileSet in fileSets)
            {
                fileSeries = _parsingService.GetSeries(Path.GetFileName(fileSet.VideoFile));

                if (fileSeries != null)
                    break;
            }

            var series = fileSeries ?? directorySeries;
            
            if (series == null)
            {
                _logger.Debug("Unknown Series: {0}", fileSets.First().VideoFile);
                return new List<ImportDecision>();
            }

            var decisions = _importDecisionMaker.GetImportDecisions(fileSets.Select(c => c.VideoFile).ToList(), series, true, directoryEpisodeInfo.Quality);
            return _importApprovedEpisodes.Import(decisions, true);
        }

        private bool ShouldDeleteFolder(DirectoryInfo directoryInfo)
        {
            var fileSets = _diskScanService.GetFileSets(directoryInfo.FullName);
            var cleanedUpName = directoryInfo.Name;
            var series = _parsingService.GetSeries(cleanedUpName);

            foreach (var fileSet in fileSets)
            {
                var episodeParseResult = Parser.Parser.ParseTitle(Path.GetFileName(fileSet.VideoFile));

                if (episodeParseResult == null)
                {
                    _logger.Warn("Unable to parse file on import: [{0}]", fileSet.VideoFile);
                    return false;
                }

                var size = _diskProvider.GetFileSize(fileSet.VideoFile);
                var quality = QualityParser.ParseQuality(fileSet.VideoFile);

                if (!_sampleService.IsSample(series, quality, fileSet.VideoFile, size, episodeParseResult.SeasonNumber))
                {
                    _logger.Warn("Non-sample file detected: [{0}]", fileSet.VideoFile);
                    return false;
                }
            }

            return true;
        }

        public void Execute(DownloadedEpisodesScanCommand message)
        {
            if (message.Path.IsNullOrWhiteSpace())
            {
                ProcessDownloadedEpisodesFolder();
            }
            else
            {
                ProcessFolder(message.Path);
            }
        }
    }
}