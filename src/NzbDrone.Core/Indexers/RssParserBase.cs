using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using NLog;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers
{
    public abstract class RssParserBase : IParseFeed
    {
        protected readonly Logger _logger;

        protected virtual ReleaseInfo CreateNewReleaseInfo()
        {
            return new ReleaseInfo();
        }

        protected RssParserBase()
        {
            _logger = NzbDroneLogger.GetLogger(this);
        }

        public virtual IEnumerable<ReleaseInfo> Process(string xml, string url)
        {
            PreProcess(xml, url);

            using (var xmlTextReader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse, IgnoreComments = true }))
            {

                var document = XDocument.Load(xmlTextReader);
                var items = document.Descendants("item");

                var result = new List<ReleaseInfo>();

                foreach (var item in items)
                {
                    try
                    {
                        var reportInfo = ParseFeedItem(item.StripNameSpace(), url);

                        if (reportInfo != null)
                        {
                            reportInfo.DownloadUrl = GetNzbUrl(item);
                            reportInfo.InfoUrl = GetNzbInfoUrl(item);
                            result.Add(reportInfo);
                        }
                    }
                    catch (Exception itemEx)
                    {
                        itemEx.Data.Add("Item", item.Title());
                        _logger.ErrorException("An error occurred while processing feed item from " + url, itemEx);
                    }
                }

                return result;
            }
        }

        private ReleaseInfo ParseFeedItem(XElement item, string url)
        {
            var title = GetTitle(item);

            var reportInfo = CreateNewReleaseInfo();

            reportInfo.Title = title;
            reportInfo.PublishDate = GetPublishDate(item);
            reportInfo.DownloadUrl = GetNzbUrl(item);
            reportInfo.InfoUrl = GetNzbInfoUrl(item);

            if (url.Contains("oznzb.com"))
            {
                reportInfo.UserRatings = GetUserRatings(item);
                reportInfo.UserRatings.RatingCeiling = 10;
                if (reportInfo.UserRatings.IsPasswordedConfirmed == true || reportInfo.UserRatings.IsSpamConfirmed == true)
                {
                    return null;
                }
            }

            try
            {
                reportInfo.Size = GetSize(item);
            }
            catch (Exception)
            {
                throw new SizeParsingException("Unable to parse size from: {0} [{1}]", reportInfo.Title, url);
            }

            _logger.Trace("Parsed: {0}", item.Title());

            return PostProcessor(item, reportInfo);
        }

        protected virtual string GetTitle(XElement item)
        {
            return item.Title();
        }

        protected virtual DateTime GetPublishDate(XElement item)
        {
            return item.PublishDate();
        }

        protected virtual string GetNzbUrl(XElement item)
        {
            return item.Links().First();
        }

        protected virtual string GetNzbInfoUrl(XElement item)
        {
            return String.Empty;
        }

        protected abstract long GetSize(XElement item);

        protected virtual ReleaseUserRatings GetUserRatings(XElement item)
        {
            return null;
        }

        protected virtual void PreProcess(string source, string url)
        {
        }

        protected virtual ReleaseInfo PostProcessor(XElement item, ReleaseInfo currentResult)
        {
            return currentResult;
        }

        private static readonly Regex ReportSizeRegex = new Regex(@"(?<value>\d+\.\d{1,2}|\d+\,\d+\.\d{1,2}|\d+)\W?(?<unit>GB|MB|GiB|MiB)",
                                                                  RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static long ParseSize(string sizeString)
        {
            var match = ReportSizeRegex.Matches(sizeString);

            if (match.Count != 0)
            {
                var cultureInfo = new CultureInfo("en-US");
                var value = Decimal.Parse(Regex.Replace(match[0].Groups["value"].Value, "\\,", ""), cultureInfo);

                var unit = match[0].Groups["unit"].Value;

                if (unit.Equals("MB", StringComparison.InvariantCultureIgnoreCase) ||
                    unit.Equals("MiB", StringComparison.InvariantCultureIgnoreCase))
                {
                    return ConvertToBytes(Convert.ToDouble(value), 2);
                }

                if (unit.Equals("GB", StringComparison.InvariantCultureIgnoreCase) ||
                        unit.Equals("GiB", StringComparison.InvariantCultureIgnoreCase))
                {
                    return ConvertToBytes(Convert.ToDouble(value), 3);
                }
            }
            return 0;
        }

        private static long ConvertToBytes(double value, int power)
        {
            var multiplier = Math.Pow(1024, power);
            var result = value * multiplier;

            return Convert.ToInt64(result);
        }
    }
}
