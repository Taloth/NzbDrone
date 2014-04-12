﻿using System;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.EpisodeImport.Specifications;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;
using NzbDrone.Test.Common;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Test.MediaFiles.EpisodeImport.Specifications
{
    [TestFixture]
    public class NotUnpackingSpecificationFixture : CoreTest<NotUnpackingSpecification>
    {
        private LocalEpisode _localEpisode;

        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IConfigService>()
                .SetupGet(s => s.DownloadClientWorkingFolders)
                .Returns("_UNPACK_|_FAILED_");

            _localEpisode = new LocalEpisode
            {
                FileSet = new FileSet(@"C:\Test\Unsorted TV\30.rock\30.rock.s01e01.avi".AsOsAgnostic()),
                Size = 100,
                Series = Builder<Series>.CreateNew().Build()
            };
        }

        private void GivenInWorkingFolder()
        {
            _localEpisode.FileSet = new FileSet(@"C:\Test\Unsorted TV\_UNPACK_30.rock\30.rock.s01e01.avi".AsOsAgnostic());
        }

        private void GivenLastWriteTimeUtc(DateTime time)
        {
            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FileGetLastWriteUtc(It.IsAny<string>()))
                .Returns(time);
        }

        [Test]
        public void should_return_true_if_not_in_working_folder()
        {
            Subject.IsSatisfiedBy(_localEpisode).Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_in_old_working_folder()
        {
            WindowsOnly();

            GivenInWorkingFolder();
            GivenLastWriteTimeUtc(DateTime.UtcNow.AddHours(-1));

            Subject.IsSatisfiedBy(_localEpisode).Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_in_working_folder_and_last_write_time_was_recent()
        {
            GivenInWorkingFolder();
            GivenLastWriteTimeUtc(DateTime.UtcNow);

            Subject.IsSatisfiedBy(_localEpisode).Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_unopacking_on_linux()
        {
            MonoOnly();

            GivenInWorkingFolder();
            GivenLastWriteTimeUtc(DateTime.UtcNow.AddDays(-5));

            Subject.IsSatisfiedBy(_localEpisode).Should().BeFalse();
        }
    }
}
