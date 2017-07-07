using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace MonoBtlsMediaInfoTest
{
    class Program
    {
        const string MediaInfoLib = "libmediainfo.so";

        [DllImport(MediaInfoLib)]
        private static extern IntPtr MediaInfo_New();

        [DllImport(MediaInfoLib)]
        private static extern void MediaInfo_Delete(IntPtr handle);

        public static void Main(string[] args)
        {
            try
            {
                if (!args.Contains("--nomediainfo"))
                {
                    Console.WriteLine("Loading MediaInfo");
                    IntPtr ptr = MediaInfo_New();

                    if (ptr != IntPtr.Zero)
                    {
                        Console.WriteLine("Loaded MediaInfo, disposing.");
                        MediaInfo_Delete(ptr);
                    }
                    else
                    {
                        Console.WriteLine("Failed to load MediaInfo");
                    }
                }

                var url = @"https://sonarr.tv";

                Console.WriteLine("Trying url: " + url);
                var client = new WebClient();
                var result = client.DownloadString(url);
                Console.WriteLine(string.Format("Done: {0} bytes", result.Length));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed: " + ex.ToString());
            }
        }
    }
}
