using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NzbDrone.Core.Parser.Model;
using System.Globalization;

namespace NzbDrone.Core.Indexers.Newznab
{
    public class NewznabParser : RssParserBase
    {
        private static readonly string[] IgnoredErrors =
        {
            "Request limit reached",
        };

        protected override String GetNzbInfoUrl(XElement item)
        {
            return item.Comments().Replace("#comments", "");
        }

        protected String GetNewznabAttribute(XElement item, String key)
        {
            var attr = item.Elements("attr").SingleOrDefault(e => e.Attribute("name").Value.Equals(key, StringComparison.CurrentCultureIgnoreCase));

            if (attr != null)
            {
                return attr.Attribute("value").Value;
            }

            return null;
        }

        protected override DateTime GetPublishDate(XElement item)
        {
            var attributes = item.Elements("attr").ToList();
            var dateString =  GetNewznabAttribute(item, "usenetdate");

            if (dateString != null)
            {
                return XElementExtensions.ParseDate(dateString);
            }

            return base.GetPublishDate(item);
        }

        protected override long GetSize(XElement item)
        {
            var sizeString = GetNewznabAttribute(item, "size");

            if (sizeString != null)
            {
                return Convert.ToInt64(sizeString);
            }

            return ParseSize(item.Description());
        }

        public override IEnumerable<ReleaseInfo> Process(string xml, string url)
        {
            try
            {
                return base.Process(xml, url);
            }
            catch (NewznabException e)
            {
                if (!IgnoredErrors.Any(ignoredError => e.Message.Contains(ignoredError)))
                {
                    throw;
                }
                _logger.Error(e.Message);
                return new List<ReleaseInfo>();
            }
        }

        protected override ReleaseInfo PostProcessor(XElement item, ReleaseInfo currentResult)
        {
            if (currentResult != null)
            {
                var rageIdString = GetNewznabAttribute(item, "rageId");

                if (rageIdString != null)
                {
                    int tvRageId;

                    if (Int32.TryParse(rageIdString, out tvRageId))
                    {
                        currentResult.TvRageId = tvRageId;
                    }
                }
            }

            return currentResult;
        }

        protected override void PreProcess(string source, string url)
        {
            NewznabPreProcessor.Process(source, url);
        }
    }
}