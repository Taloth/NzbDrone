﻿using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;


namespace NzbDrone.Core.MediaFiles.EpisodeImport
{
    public interface IMakeImportDecision
    {
        List<ImportDecision> GetImportDecisions(List<FileSet> fileSets, Series series, bool sceneSource, QualityModel quality = null);
    }

    public class ImportDecisionMaker : IMakeImportDecision
    {
        private readonly IEnumerable<IRejectWithReason> _specifications;
        private readonly IParsingService _parsingService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public ImportDecisionMaker(IEnumerable<IRejectWithReason> specifications,
                                   IParsingService parsingService,
                                   IMediaFileService mediaFileService,
                                   IDiskProvider diskProvider,

                                   Logger logger)
        {
            _specifications = specifications;
            _parsingService = parsingService;
            _mediaFileService = mediaFileService;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public List<ImportDecision> GetImportDecisions(List<FileSet> fileSets, Series series, bool sceneSource, QualityModel quality = null)
        {
            var newFiles = _mediaFileService.FilterExistingFiles(videoFiles.ToList(), series.Id);

            _logger.Debug("Analyzing {0}/{1} files.", newFiles.Count, fileSets.Count());

            List<ImportDecision> importDecisions = new List<ImportDecision>();

            foreach (var fileSet in fileSets)
            {
                ImportDecision decision = GetDecision(fileSet, series, sceneSource, quality);

                if (decision != null)
                    importDecisions.Add(decision);
            }

            return importDecisions;
        }

        private ImportDecision GetDecision(FileSet fileSet, Series series, bool sceneSource, QualityModel quality = null)
        {
            ImportDecision decision = null;

            try
            {
                var parsedEpisode = _parsingService.GetLocalEpisode(fileSet.VideoFile, series, sceneSource);
                    
                if (parsedEpisode != null)
                {
                    if (quality != null && new QualityModelComparer(parsedEpisode.Series.QualityProfile).Compare(quality, parsedEpisode.Quality) > 0)
                    {
                        _logger.Debug("Using quality from folder: {0}", quality);
                        parsedEpisode.Quality = quality;
                    }

                    parsedEpisode.Size = _diskProvider.GetFileSize(fileSet.VideoFile);
                    _logger.Debug("Size: {0}", parsedEpisode.Size);

                    decision = GetDecision(parsedEpisode);
                }

                else
                {
                    parsedEpisode = new LocalEpisode();
                    parsedEpisode.FileSet = fileSet;

                    decision = new ImportDecision(parsedEpisode, "Unable to parse file");
                }
            }
            catch (Exception e)
            {
                _logger.ErrorException("Couldn't import file: " + fileSet.VideoFile, e);
            }

            return decision;
        }

        private ImportDecision GetDecision(LocalEpisode localEpisode)
        {
            var reasons = _specifications.Select(c => EvaluateSpec(c, localEpisode))
                .Where(c => !string.IsNullOrWhiteSpace(c));

            return new ImportDecision(localEpisode, reasons.ToArray());
        }

        private string EvaluateSpec(IRejectWithReason spec, LocalEpisode localEpisode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(spec.RejectionReason))
                {
                    throw new InvalidOperationException("[Need Rejection Text]");
                }

                var generalSpecification = spec as IImportDecisionEngineSpecification;
                if (generalSpecification != null && !generalSpecification.IsSatisfiedBy(localEpisode))
                {
                    return spec.RejectionReason;
                }
            }
            catch (Exception e)
            {
                //e.Data.Add("report", remoteEpisode.Report.ToJson());
                //e.Data.Add("parsed", remoteEpisode.ParsedEpisodeInfo.ToJson());
                _logger.ErrorException("Couldn't evaluate decision on " + localEpisode.FileSet.VideoFile, e);
                return string.Format("{0}: {1}", spec.GetType().Name, e.Message);
            }

            return null;
        }
    }
}
