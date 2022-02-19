﻿using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;

namespace YoutubeSegmentDownloader;

public class ExternalProgram
{
    public enum DependencyStatus
    {
        NotExist,
        Downloading,
        Exist
    }

    public static DependencyStatus Ytdlp_Status { get; private set; } = DependencyStatus.NotExist;
    public static DependencyStatus FFmpeg_Status { get; private set; } = DependencyStatus.NotExist;

    static private readonly DirectoryInfo TempDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), nameof(YoutubeSegmentDownloader)));

    internal static async Task DownloadYtdlp()
    {
        Ytdlp_Status = DependencyStatus.Downloading;

        string? ytdlpPath = TempDirectory.FullName;
        HttpClient client = new();
        HttpResponseMessage response = await client.GetAsync(@"https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe", HttpCompletionOption.ResponseHeadersRead);

        string filePath = Path.Combine(ytdlpPath, "yt-dlp.exe");
        using FileStream fs = new(filePath, FileMode.Create);
        //response.EnsureSuccessStatusCode();
        await response.Content.CopyToAsync(fs);

        if (File.Exists(filePath))
        {
            Ytdlp_Status = DependencyStatus.Exist;
            return;
        }
        else
        {
            // log error
        }
        Ytdlp_Status = DependencyStatus.NotExist;
    }

    internal static async Task DownloadFFmpeg()
    {
        FFmpeg_Status = DependencyStatus.Downloading;

        string? ffmpegPath = TempDirectory.FullName;
        HttpClient client = new();
        var response = await client.GetAsync(@"https://github.com/GyanD/codexffmpeg/releases/download/5.0/ffmpeg-5.0-full_build.7z", HttpCompletionOption.ResponseHeadersRead);

        string archivePath = Path.Combine(ffmpegPath, "ffmpeg-5.0-full_build.7z");
        using (FileStream fs = new(archivePath, FileMode.Create))
        {
            //response.EnsureSuccessStatusCode();
            await response.Content.CopyToAsync(fs);
        }

        try
        {
            using SevenZipArchive archive = SevenZipArchive.Open(archivePath);
            foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory
                                                                 && entry.Key.EndsWith("exe")))
            {
                entry.WriteToDirectory(ffmpegPath, new ExtractionOptions()
                {
                    ExtractFullPath = false,
                    Overwrite = true
                });
            }

            if (File.Exists(Path.Combine(ffmpegPath, "ffmpeg.exe")))
            {
                FFmpeg_Status = DependencyStatus.Exist;
                return;
            }
            else
            {
                // log error
            }
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            File.Delete(archivePath);
        }

        FFmpeg_Status = DependencyStatus.NotExist;
    }

    /// <summary>
    /// 尋找程式路徑
    /// </summary>
    /// <returns>Full path of yt-dlp</returns>
    public static (string?, string?) WhereIs()
    {
        // https://stackoverflow.com/a/63021455
        string[] paths = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? Array.Empty<string>();
        string[] extensions = Environment.GetEnvironmentVariable("PATHEXT")?.Split(';') ?? Array.Empty<string>();
        string? YtdlPath = (from p in new[] { Environment.CurrentDirectory, TempDirectory.FullName }.Concat(paths)
                            from e in extensions
                            let path = Path.Combine(p.Trim(), "yt-dlp" + e.ToLower())
                            where File.Exists(path)
                            select Path.GetDirectoryName(path))?.FirstOrDefault();
        string? FFmpegPath = (from p in new[] { Environment.CurrentDirectory, TempDirectory.FullName }.Concat(paths)
                              from e in extensions
                              let path = Path.Combine(p.Trim(), "ffmpeg" + e.ToLower())
                              where File.Exists(path)
                              select Path.GetDirectoryName(path))?.FirstOrDefault();

        Ytdlp_Status = string.IsNullOrEmpty(YtdlPath)
                        ? DependencyStatus.NotExist
                        : DependencyStatus.Exist;
        FFmpeg_Status = string.IsNullOrEmpty(FFmpegPath)
                        ? DependencyStatus.NotExist
                        : DependencyStatus.Exist;

        //logger.LogDebug("Found yt-dlp at {path}", YtdlPath);
        return (YtdlPath, FFmpegPath);
    }
}