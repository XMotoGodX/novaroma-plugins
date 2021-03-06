﻿using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Novaroma.Interface.EventHandler;
using Novaroma.Interface.Model;
using Novaroma.Model;

namespace Novaroma.Plugins.MkvPackager {
    public class MkvPackagerPlugin : IDownloadEventHandler {
        public string ServiceName {
            get { return "Mkv Packager Plugin"; }
        }

        public void MovieDownloaded(Movie movie) {
        }

        public void MovieSubtitleDownloaded(Movie movie) {
            MkvPackage(movie);
        }

        public void TvShowEpisodeDownloaded(TvShowEpisode episode) {
        }

        public void TvShowEpisodeSubtitleDownloaded(TvShowEpisode episode) {
            MkvPackage(episode);
        }

        private void MkvPackage(IDownloadable media) {
            const string mergedString = "[merged]";

            var directory = Path.GetDirectoryName(media.FilePath);
            if (string.IsNullOrEmpty(directory)) return;

            var mediaFileNameWithoutExtension = Path.GetFileNameWithoutExtension(media.FilePath);
            if (!mediaFileNameWithoutExtension.Contains(mergedString)) mediaFileNameWithoutExtension += mergedString;
            var outputPath = Path.Combine(directory, mediaFileNameWithoutExtension + ".mkv");

            if (string.IsNullOrEmpty(media.FilePath) || !File.Exists(media.FilePath)) return;

            var mediafileName = Path.GetFileName(media.FilePath);
            if (string.IsNullOrEmpty(mediafileName)) return;

            var tempPath = Path.Combine(Path.GetTempPath(), "Novaroma Mkv Packager");
            if (!File.Exists(tempPath)) Directory.CreateDirectory(tempPath);

            var subtitleFilePath = Directory.GetFiles(directory).FirstOrDefault(Helper.IsSubtitleFile);
            if (string.IsNullOrEmpty(subtitleFilePath)) return;
            var subtitleFileName = Path.GetFileName(subtitleFilePath);


            new FileInfo(media.FilePath).MoveTo(Path.Combine(tempPath, mediafileName));
            new FileInfo(subtitleFilePath).MoveTo(Path.Combine(tempPath, subtitleFileName));

            var mediaFilePathNew = Path.Combine(tempPath, mediafileName);
            var subtitleFilePathNew = Path.Combine(tempPath, subtitleFileName);

            MkvMerge(mediaFilePathNew, subtitleFilePathNew, outputPath);



            media.FilePath = outputPath;
            media.BackgroundDownload = false;
            media.SubtitleDownloaded = false;

            File.Delete(mediaFilePathNew);
            File.Delete(subtitleFilePathNew);
        }

        private void MkvMerge(string videoInputPath, string subtitleInputPath, string mkvOutputPath) {
            var pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(pluginDirectory)) return;

            var mkvMergeExePath = Path.Combine(pluginDirectory, "mkvmerge.exe");
            var process = new Process();
            var startInfo = new ProcessStartInfo {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = mkvMergeExePath
            };

            var parameters = string.Format(" -o \"{0}\" --stracks 1,1 \"{1}\" \"{2}\"", mkvOutputPath, videoInputPath, subtitleInputPath);
            startInfo.Arguments = parameters;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }

    }
}
