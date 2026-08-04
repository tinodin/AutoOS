using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AutoOS.Core.Common;
using AutoOS.Core.Helpers.Logging;
using DevWinUI;
using Downloader;

namespace AutoOS.Core.Helpers.Download;

public static partial class DownloadHelper
{
	private static readonly HttpClient httpClient = new()
	{
		DefaultRequestHeaders =
		{
			UserAgent =
			{
				new ProductInfoHeaderValue("AutoOS", ProcessInfoHelper.Version)
			}
		}
	};

	public static async Task Download(string url, string path, string file = null, IStatusReporter reporter = null)
	{
		await Download([url], path, file != null ? new[] { file } : null, reporter);
	}

	public static async Task Download(IEnumerable<string> urls, string path, IEnumerable<string> files = null, IStatusReporter reporter = null)
	{
		Directory.CreateDirectory(path);
		var urlList = urls.ToList();
		List<string> fileList = files?.ToList() ?? [];
		long totalBytesDownloaded = 0;
		DateTime lastLoggedTime = DateTime.MinValue;
		double lastSpeedMB = 0;
		DateTime startTime = DateTime.Now;

		long totalBytes = 0;
		foreach (string url in urlList)
		{
			try
			{
				using var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
				headRequest.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");
				using HttpResponseMessage response = await httpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead);
				if (response.Content.Headers.ContentLength.HasValue)
				{
					totalBytes += response.Content.Headers.ContentLength.Value;
				}
			}
			catch { }
		}
		double totalSizeMB = totalBytes / (1024.0 * 1024.0);

		for (int i = 0; i < urlList.Count; i++)
		{
			string url = urlList[i];
			string file = fileList.Count > i ? fileList[i] : null;
			string fileName = string.IsNullOrWhiteSpace(file) ? Path.GetFileName(url) : file;
			string destination = Path.Combine(path, fileName);

			if (url.Contains("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase))
			{
				using (HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
				{
					response.EnsureSuccessStatusCode();
					using Stream contentStream = await response.Content.ReadAsStreamAsync();
					using var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
					await contentStream.CopyToAsync(fileStream);
					totalBytesDownloaded = new FileInfo(destination).Length;
				}
				if (urlList.Count == 1)
				{
					reporter?.Report(progress: 100);
				}
				continue;
			}

			DownloadConfiguration config = new()
			{
				MaxTryAgainOnFailure = 8,
				EnableAutoResumeDownload = true,
				ParallelDownload = true,
				ChunkCount = 4,
				ParallelCount = 4,
				HttpClientTimeout = 300000,
				CheckDiskSizeBeforeDownload = true,
				MinimumChunkSize = 1024 * 1024,
				RequestConfiguration = new RequestConfiguration
				{
					Headers = new WebHeaderCollection
					{
						{ "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36" },
						{ "Accept", "*/*" },
						{ "Accept-Language", "en-US,en;q=0.9" },
						{ "Connection", "keep-alive" }
					}
				}
			};

			if (url.Contains("www2.ati.com", StringComparison.OrdinalIgnoreCase))
			{
				config.RequestConfiguration = new RequestConfiguration
				{
					Headers = new WebHeaderCollection
					{
						{ "Referer", "http://support.amd.com" },
						{ "Accept", "*/*" },
						{ "User-Agent", "AMD Catalyst Install Manager/0.0" },
						{ "Cache-Control", "no-cache" },
						{ "Connection", "Keep-Alive" }
					}
				};
			}

			DownloadBuilder downloadBuilder = DownloadBuilder.New()
				.WithUrl(url)
				.WithDirectory(path)
				.WithFileName(file)
				.WithConfiguration(config);

			IDownload download = downloadBuilder.Build();
			long fileBytesDownloaded = 0;
			long fileTotalBytes = 0;
			Exception downloaderError = null;
			DateTime downloaderStartTime = DateTime.Now;

			if (urlList.Count == 1)
			{
				double singleTotalSizeMB = 0;
				download.DownloadProgressChanged += (sender, e) =>
				{
					if ((DateTime.Now - lastLoggedTime).TotalMilliseconds < 100) return;
					lastLoggedTime = DateTime.Now;

					lastSpeedMB = e.BytesPerSecondSpeed / (1024.0 * 1024.0);
					double receivedMB = e.ReceivedBytesSize / (1024.0 * 1024.0);
					singleTotalSizeMB = e.TotalBytesToReceive / (1024.0 * 1024.0);
					double percentage = e.ProgressPercentage;

					reporter?.Report($"{lastSpeedMB:F1} MB/s - {receivedMB:F2} MB of {singleTotalSizeMB:F2} MB", percentage, false);
				};

				download.DownloadFileCompleted += (sender, e) =>
				{
					if (e.Error == null)
					{
						reporter?.Report($"{lastSpeedMB:F1} MB/s - {singleTotalSizeMB:F2} MB of {singleTotalSizeMB:F2} MB", 100, false);
					}
					else
					{
						downloaderError = e.Error;
					}
				};
			}
			else
			{
				download.DownloadProgressChanged += (sender, e) =>
				{
					if ((DateTime.Now - lastLoggedTime).TotalMilliseconds < 100) return;
					lastLoggedTime = DateTime.Now;

					fileBytesDownloaded = e.ReceivedBytesSize;
					fileTotalBytes = e.TotalBytesToReceive;
					lastSpeedMB = e.BytesPerSecondSpeed / (1024.0 * 1024.0);
					double combinedDownloadedMB = (totalBytesDownloaded + fileBytesDownloaded) / (1024.0 * 1024.0);
					double percentage = totalSizeMB > 0 ? (combinedDownloadedMB / totalSizeMB) * 100.0 : ((double)(i + 1) / urlList.Count) * 100.0;

					reporter?.Report($"{lastSpeedMB:F1} MB/s - {combinedDownloadedMB:F2} MB of {totalSizeMB:F2} MB", percentage, false);
				};

				download.DownloadFileCompleted += (sender, e) =>
				{
					if (e.Error == null)
					{
						totalBytesDownloaded += fileTotalBytes;
					}
					else
					{
						downloaderError = e.Error;
					}
				};
			}

			await download.StartAsync();
			DateTime downloaderEndTime = DateTime.Now;

			if (urlList.Count == 1)
			{
				string singleFileName = download.Package?.FileName ?? (!string.IsNullOrEmpty(file) ? Path.Combine(path, file) : null);
				if (!File.Exists(singleFileName))
				{
					var errorDetails = new StringBuilder();
					DownloadPackage? package = download.Package;

					errorDetails.AppendLine($"Primary download failed for: {url}");
					errorDetails.AppendLine($"Package: Status={package?.Status}, SaveComplete={package?.IsSaveComplete}, FileName={package?.FileName}");
					if (downloaderError != null)
						errorDetails.AppendLine($"Downloader error: {downloaderError.GetType().Name}: {downloaderError.Message}");
					files = Directory.Exists(path) ? Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly) : [];
					errorDetails.AppendLine($"Files in path: [{string.Join(", ", files.Select(Path.GetFileName))}]");

					HttpStatusCode? statusCode = null;
					string contentLength = "", acceptRanges = "", contentRange = "";
					try
					{
						using var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
						if (config.RequestConfiguration?.Headers != null)
						{
							foreach (string headerName in config.RequestConfiguration.Headers.AllKeys)
								headRequest.Headers.TryAddWithoutValidation(headerName, config.RequestConfiguration.Headers[headerName]);
						}
						using HttpResponseMessage response = await httpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead);
						statusCode = response.StatusCode;
						try { contentLength = response.Content.Headers.ContentLength?.ToString() ?? ""; } catch { }
						try { acceptRanges = response.Headers.AcceptRanges.FirstOrDefault() ?? ""; } catch { }
						try { contentRange = response.Content.Headers.ContentRange?.ToString() ?? ""; } catch { }
					}
					catch { }
					errorDetails.AppendLine(statusCode.HasValue ? $"HTTP Status Code: {(int)statusCode.Value} ({statusCode.Value})" : "HTTP status unknown");
					errorDetails.AppendLine($"Content-Length: {contentLength}, Accept-Ranges: {acceptRanges}, Content-Range: {contentRange}");

					Exception fallbackError = null;
					DateTime httpClientStartTime = DateTime.Now;
					DateTime httpClientEndTime = DateTime.Now;
					if (statusCode.HasValue && (int)statusCode.Value >= 200 && (int)statusCode.Value <= 299)
					{
						try
						{
							using var request = new HttpRequestMessage(HttpMethod.Get, url);
							if (config.RequestConfiguration?.Headers != null)
							{
								foreach (string headerName in config.RequestConfiguration.Headers.AllKeys)
									request.Headers.TryAddWithoutValidation(headerName, config.RequestConfiguration.Headers[headerName]);
							}
							using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
							if (response.IsSuccessStatusCode)
							{
								try { contentLength = response.Content.Headers.ContentLength?.ToString() ?? ""; } catch { }
								try { acceptRanges = response.Headers.AcceptRanges.FirstOrDefault() ?? ""; } catch { }
								try { contentRange = response.Content.Headers.ContentRange?.ToString() ?? ""; } catch { }

								Directory.CreateDirectory(Path.GetDirectoryName(singleFileName)!);
								using Stream contentStream = await response.Content.ReadAsStreamAsync();
								using var fileStream = new FileStream(singleFileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

								totalBytes = response.Content.Headers.ContentLength ?? -1L;
								double clientTotalSizeMB = totalBytes / (1024.0 * 1024.0);
								byte[] buffer = new byte[81920];
								int bytesRead;
								long totalRead = 0;
								DateTime clientLastLoggedTime = DateTime.MinValue;

								while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
								{
									await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
									totalRead += bytesRead;

									if ((DateTime.Now - clientLastLoggedTime).TotalMilliseconds >= 100)
									{
										clientLastLoggedTime = DateTime.Now;
										double elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;
										double speedMB = elapsedSeconds > 0 ? (totalRead / (1024.0 * 1024.0)) / elapsedSeconds : 0;
										double receivedMB = totalRead / (1024.0 * 1024.0);
										double percentage = totalBytes > 0 ? (double)totalRead / totalBytes * 100.0 : 0;
										reporter?.Report(totalBytes > 0 ? $"{speedMB:F1} MB/s - {receivedMB:F2} MB of {clientTotalSizeMB:F2} MB" : $"{speedMB:F1} MB/s - {receivedMB:F2} MB", percentage, false);
									}
								}

								await fileStream.FlushAsync();
								httpClientEndTime = DateTime.Now;
							}
						}
						catch (Exception ex)
						{
							fallbackError = ex;
						}
					}

					if (File.Exists(singleFileName) && new FileInfo(singleFileName).Length != 0)
					{
						errorDetails.AppendLine("Fallback download succeeded");
						TimeSpan downloaderTime = downloaderEndTime - downloaderStartTime;
						TimeSpan httpClientTime = httpClientEndTime - httpClientStartTime;
						errorDetails.AppendLine($"Downloader took: {(downloaderTime.TotalMinutes >= 1 ? $"{(int)downloaderTime.TotalMinutes}min {downloaderTime.Seconds}sec" : $"{downloaderTime.Seconds}sec")}");
						errorDetails.AppendLine($"HttpClient took: {(httpClientTime.TotalMinutes >= 1 ? $"{(int)httpClientTime.TotalMinutes}min {httpClientTime.Seconds}sec" : $"{httpClientTime.Seconds}sec")}");
						await LogHelper.LogError(new Exception(errorDetails.ToString(), downloaderError));
					}
					else
					{
						if (fallbackError != null)
							errorDetails.AppendLine($"Fallback download error: {fallbackError.GetType().Name}: {fallbackError.Message}");
						else if (statusCode.HasValue && (int)statusCode.Value >= 200 && (int)statusCode.Value <= 299)
							errorDetails.AppendLine("Fallback download completed but file still not found");
						else
							errorDetails.AppendLine("Fallback download not attempted (non-success HTTP status or unknown)");

						await LogHelper.LogError(new FileNotFoundException(errorDetails.ToString(), singleFileName!, downloaderError));
					}
				}

				reporter?.Report(progress: 100, isIndeterminate: true);
			}
			else
			{
				if (downloaderError != null)
				{
					throw downloaderError;
				}
			}
		}

		if (urlList.Count > 1)
		{
			reporter?.Report($"{totalSizeMB:F2} MB of {totalSizeMB:F2} MB", 100, false);
		}
	}
}
