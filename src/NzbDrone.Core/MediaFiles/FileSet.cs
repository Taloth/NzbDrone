﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NzbDrone.Core.MediaFiles
{
    public class FileSet
    {
        public FileSet(string mediaFile)
        {
            VideoFile = mediaFile;
            OtherFiles = new List<string>();
        }

        public string VideoFile { get; set; }
        public List<string> OtherFiles { get; set; }
    }
}
