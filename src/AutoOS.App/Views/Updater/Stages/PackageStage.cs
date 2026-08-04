using System.Security.Cryptography.X509Certificates;
using Windows.Foundation;
using Windows.Management.Deployment;
using Windows.Win32;

namespace AutoOS.App.Views.Updater.Stages;

public static class PackageStage
{
	public static async Task PackageActions(string downloadUrl, UpdateDialog dialog)
	{
		string tempFolderPath = Path.Combine(Path.GetTempPath(), "AutoOS Updater");
		Directory.CreateDirectory(tempFolderPath);
		string tempFilePath = Path.Combine(tempFolderPath, "AutoOS.msix");
		string cerFilePath = Path.Combine(tempFolderPath, "AutoOS.cer");

		await dialog.Download(downloadUrl.Replace("AutoOS.msix", "AutoOS.cer"), tempFolderPath, "AutoOS.cer", "Downloading Certificate...", 0, 25);
		dialog.SetStatus("Installing Certificate...");
		using (X509Store store = new(StoreName.Root, StoreLocation.LocalMachine))
		{
			store.Open(OpenFlags.ReadWrite);
			X509Certificate2 cert = X509CertificateLoader.LoadCertificateFromFile(cerFilePath);
			foreach (X509Certificate2 oldCert in store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, cert.Subject, false))
			{
				if (oldCert.Thumbprint != cert.Thumbprint)
					store.Remove(oldCert);
			}
			store.Add(cert);
		}
		await Task.Delay(150);
		await dialog.Download(downloadUrl, tempFolderPath, "AutoOS.msix", "Downloading Update", 50, 75);

		dialog.SetStatus("Installing Update...");
		PInvoke.RegisterApplicationRestart(null, 0);
		var packageManager = new PackageManager();
		IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> deploymentOperation = packageManager.AddPackageAsync(new Uri(tempFilePath), null, DeploymentOptions.ForceApplicationShutdown);
		deploymentOperation.Progress = (info, progress) =>
		{
			_ = dialog.DispatcherQueue.TryEnqueue(() =>
			{
				if (progress.percentage > 80)
				{
					dialog.SetProgress(100);
					dialog.SetSuccess();
				}
				else
				{
					double scaledProgress = 75 + (progress.percentage / 80.0 * 25);
					dialog.SetProgress(scaledProgress);
					dialog.SetStatus($"Installing Update ({Math.Round(progress.percentage / 80.0 * 100)}%)...");
				}
			});
		};
		await deploymentOperation;
	}
}
