using System.Diagnostics;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Instrumentation.Extensions;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.EpisodeImport;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Tv.Events;
using System.Collections.Generic;
using System.Globalization;

namespace NzbDrone.Core.MediaFiles
{
    public interface IDiskScanService
    {
        void Scan(Series series);
        IEnumerable<FileSet> GetFileSets(string path, bool allDirectories = true);
    }

    public class DiskScanService :
        IDiskScanService,
        IHandle<SeriesUpdatedEvent>,
        IExecute<RescanSeriesCommand>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IMakeImportDecision _importDecisionMaker;
        private readonly IImportApprovedEpisodes _importApprovedEpisodes;
        private readonly ICommandExecutor _commandExecutor;
        private readonly IConfigService _configService;
        private readonly ISeriesService _seriesService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public DiskScanService(IDiskProvider diskProvider,
                               IMakeImportDecision importDecisionMaker,
                               IImportApprovedEpisodes importApprovedEpisodes,
                               ICommandExecutor commandExecutor,
                               IConfigService configService,
                               ISeriesService seriesService,
                               IEventAggregator eventAggregator,
                               Logger logger)
        {
            _diskProvider = diskProvider;
            _importDecisionMaker = importDecisionMaker;
            _importApprovedEpisodes = importApprovedEpisodes;
            _commandExecutor = commandExecutor;
            _configService = configService;
            _seriesService = seriesService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public void Scan(Series series)
        {
            _logger.ProgressInfo("Scanning disk for {0}", series.Title);
            _commandExecutor.PublishCommand(new CleanMediaFileDb(series.Id));

            if (!_diskProvider.FolderExists(series.Path))
            {
                if (_configService.CreateEmptySeriesFolders &&
                    _diskProvider.FolderExists(_diskProvider.GetParentFolder(series.Path)))
                {
                    _logger.Debug("Creating missing series folder: {0}", series.Path);
                    _diskProvider.CreateFolder(series.Path);
                }
                else
                {
                    _logger.Debug("Series folder doesn't exist: {0}", series.Path);
                }

                return;
            }

            var videoFilesStopwatch = Stopwatch.StartNew();
            var mediaFileList = GetFileSets(series.Path).ToList();
            videoFilesStopwatch.Stop();
            _logger.Trace("Finished getting episode files for: {0} [{1}]", series, videoFilesStopwatch.Elapsed);

            var decisionsStopwatch = Stopwatch.StartNew();
            var decisions = _importDecisionMaker.GetImportDecisions(mediaFileList.Select(v => v.VideoFile).ToList(), series, false);
            decisionsStopwatch.Stop();
            _logger.Trace("Import decisions complete for: {0} [{1}]", series, decisionsStopwatch.Elapsed);

            _importApprovedEpisodes.Import(decisions);

            _logger.Info("Completed scanning disk for {0}", series.Title);
            _eventAggregator.PublishEvent(new SeriesScannedEvent(series));
        }

        public IEnumerable<FileSet> GetFileSets(string path, bool allDirectories = true)
        {
            _logger.Debug("Scanning '{0}' for media files", path);

            var searchOption = allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var filesOnDisk = _diskProvider.GetFiles(path, searchOption);

            var mediaFileList = filesOnDisk
                .Where(c => MediaFileExtensions.Extensions.Contains(Path.GetExtension(c).ToLower()))
                .OrderByDescending(c => c.Length)
                .ToList();

            _logger.Debug("{0} media files were found in {1}", mediaFileList.Count, path);

            var remainingFileList = filesOnDisk.Except(mediaFileList).ToList();

            foreach (var mediaFile in mediaFileList)
            {
                var fileSet = new FileSet(mediaFile);
                var nameWithoutExtension = Path.Combine(Path.GetDirectoryName(mediaFile), Path.GetFileNameWithoutExtension(mediaFile)) + ".";

                for (int i = 0; i < remainingFileList.Count; i++)
                {
                    if (remainingFileList[i].StartsWith(nameWithoutExtension, true, CultureInfo.InvariantCulture))
                    {
                        fileSet.OtherFiles.Add(remainingFileList[i]);
                        remainingFileList.RemoveAt(i--);
                    }
                }

                yield return fileSet;
            }

            if (remainingFileList.Count != 0)
                _logger.Debug("{0} files in {1} could not be associated with a media file and will be ignored", remainingFileList.Count, path);
        }

        public void Handle(SeriesUpdatedEvent message)
        {
            Scan(message.Series);
        }

        public void Execute(RescanSeriesCommand message)
        {
            if (message.SeriesId.HasValue)
            {
                var series = _seriesService.GetSeries(message.SeriesId.Value);
                Scan(series);
            }

            else
            {
                var allSeries = _seriesService.GetAllSeries();

                foreach (var series in allSeries)
                {
                    Scan(series);
                }
            }
        }
    }
}