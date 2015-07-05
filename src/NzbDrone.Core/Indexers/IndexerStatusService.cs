using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers
{
    public interface IIndexerStatusService
    {
        List<IndexerStatus> GetBlockedIndexers();
        IndexerStatus GetIndexerStatus(int indexerId);
        void ReportSuccess(int indexerId);
        void ReportFailure(int indexerId, TimeSpan minimumBackOff = default(TimeSpan));

        void UpdateRssSyncStatus(int indexerId, ReleaseInfo releaseInfo, bool fullyUpdated);
    }

    public class IndexerStatusService : IIndexerStatusService
    {
        private static readonly int[] EscalationBackOffPeriods = {
                                                                     0,
                                                                     5 * 60,
                                                                     15 * 60,
                                                                     30 * 60,
                                                                     60 * 60,
                                                                     3 * 60 * 60,
                                                                     6 * 60 * 60,
                                                                     12 * 60 * 60,
                                                                     24 * 60 * 60
                                                                 };
        private static readonly int MaximumEscalationLevel = EscalationBackOffPeriods.Length - 1;

        private static readonly object _syncRoot = new object();

        private readonly IIndexerStatusRepository _indexerStatusRepository;
        private readonly Logger _logger;

        public IndexerStatusService(IIndexerStatusRepository indexerStatusRepository, Logger logger)
        {
            _indexerStatusRepository = indexerStatusRepository;
            _logger = logger;
        }

        public List<IndexerStatus> GetBlockedIndexers()
        {
            return _indexerStatusRepository.All()
                .Where(v => v.DisabledTill.HasValue || v.DisabledTill.Value < DateTime.UtcNow)
                .ToList();
        }

        public IndexerStatus GetIndexerStatus(int indexerId)
        {
            return _indexerStatusRepository.FindByIndexerId(indexerId) ?? new IndexerStatus { IndexerId = indexerId };
        }

        private TimeSpan CalculateBackOffPeriod(IndexerStatus status)
        {
            var level = Math.Min(MaximumEscalationLevel, status.EscalationLevel);

            return TimeSpan.FromSeconds(EscalationBackOffPeriods[level]);
        }

        public void ReportSuccess(int indexerId)
        {
            lock (_syncRoot)
            {
                var status = GetIndexerStatus(indexerId);

                if (status.EscalationLevel == 0)
                {
                    return;
                }

                status.EscalationLevel--;
                status.DisabledTill = null;

                _indexerStatusRepository.Upsert(status);
            }
        }

        public void ReportFailure(int indexerId, TimeSpan minimumBackOff = default(TimeSpan))
        {
            lock (_syncRoot)
            {
                var status = GetIndexerStatus(indexerId);

                var now = DateTime.UtcNow;

                if (status.EscalationLevel == 0)
                {
                    status.InitialFailure = now;
                }

                status.MostRecentFailure = now;
                status.EscalationLevel = Math.Min(MaximumEscalationLevel, status.EscalationLevel + 1);

                if (minimumBackOff != TimeSpan.Zero)
                {
                    while (status.EscalationLevel < MaximumEscalationLevel && CalculateBackOffPeriod(status) < minimumBackOff)
                    {
                        status.EscalationLevel++;
                    }
                }

                status.DisabledTill = now + CalculateBackOffPeriod(status);

                _indexerStatusRepository.Upsert(status);
            }
        }

        public void UpdateRssSyncStatus(int indexerId, ReleaseInfo releaseInfo, bool fullyUpdated)
        {
            lock (_syncRoot)
            {
                var status = GetIndexerStatus(indexerId);

                if (fullyUpdated)
                {
                    status.LastContinuousRssSync = DateTime.UtcNow;
                }
                status.LastRssSyncReleaseInfo = releaseInfo;

                _indexerStatusRepository.Upsert(status);
            }
        }
    }
}
