using AutoOS.Core.Helpers.BIOS;
using AutoOS.Core.Helpers.Logging;
using AutoOS.Core.Helpers.Picker;
using AutoOS.Views.Installer.Stages;
using AutoOS.Views.Settings.BIOS;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using Syncfusion.UI.Xaml.Data;
using Syncfusion.UI.Xaml.DataGrid;
using Syncfusion.UI.Xaml.Grids;
using Syncfusion.UI.Xaml.TreeGrid;
using System.Diagnostics;
using Windows.System;

namespace AutoOS.Views.Settings;

public sealed partial class BiosSettingPage : Page
{
	private readonly string nvram = Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "nvram.txt");
	private readonly string backupFolder = Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "Backups");
	public BiosSettingViewModel ViewModel { get; } = new();

	public BiosSettingPage()
	{
		InitializeComponent();
		ViewModel.RefreshFilterAction = RefreshSearchFilter;
		ViewModel.ExpandDiffNodesAction = () =>
		{
			if (BiosDiffTreeGrid.View != null)
				BiosDiffTreeGrid.ExpandAllNodes();
		};
		ViewModel.ExpandAllNodesAction = () =>
		{
			if (BiosTreeGrid.View != null)
				BiosTreeGrid.ExpandAllNodes();
		};
		_ = Export();
	}

	private async Task Export()
	{
		SwitchPresenter.Value = "Export";
		ViewModel.SetIsLoaded(false);
		ViewModel.MergeCount = 0;

		// copy scewin to localstate due to permissions
		if (!Directory.Exists(Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN")))
			FileSystem.CopyDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "SCEWIN"), Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN"), true);

		// export nvram
		Process.GetProcessesByName("SCEWIN_64").FirstOrDefault()?.Kill();

		using var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "SCEWIN_64.exe"),
				Arguments = @$"/o /s ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "nvram.txt")}""",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true
			}
		};

		process.Start();
		string errorOutput = await process.StandardError.ReadToEndAsync();
		string output = await process.StandardOutput.ReadToEndAsync();
		await process.WaitForExitAsync();

		string manufacturer = "Unknown";
		string product = "Unknown";

		using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS"))
		{
			if (key != null)
			{
				manufacturer = key.GetValue("BaseBoardManufacturer")?.ToString().ToLowerInvariant() ?? "Unknown";
				product = key.GetValue("BaseBoardProduct")?.ToString().ToUpperInvariant() ?? "Unknown";
			}
		}

		if (output.Contains("AMISCE is not supported on this system.", StringComparison.OrdinalIgnoreCase) ||
			errorOutput.Contains("BIOS not compatible", StringComparison.OrdinalIgnoreCase))
		{
			SwitchPresenter.Value = "Unsupported";
			return;
		}

		if (errorOutput.Contains("WARNING: HII data does not have setup questions information", StringComparison.OrdinalIgnoreCase))
		{
			if (manufacturer.Contains("asus") || manufacturer.Contains("asustek"))
			{
				var protectedChipsets = new[] { "Z790", "B760", "H770", "X870", "X670", "B650", "A620" };
				SwitchPresenter.Value = protectedChipsets.Any(chipset => product.Contains(chipset))
					? "HII Resources (Protected)"
					: "HII Resources (Regular)";
			}
			else
			{
				SwitchPresenter.Value = "HII Resources (Other)";
			}
			return;
		}

		if (errorOutput.Contains("Platform identification failed.", StringComparison.OrdinalIgnoreCase))
		{
			using var process2 = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "SCEWIN_64.exe"),
					Arguments = @$"/o /s ""{nvram}"" /d",
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardError = true,
					RedirectStandardOutput = true
				}
			};

			process2.Start();
			errorOutput = await process2.StandardError.ReadToEndAsync();
			output = await process2.StandardOutput.ReadToEndAsync();
			await process2.WaitForExitAsync();
		}

		if (!errorOutput.Contains("Script file exported successfully.", StringComparison.OrdinalIgnoreCase))
			return;

		if (new FileInfo(nvram).Length <= 100 * 1024)
		{
			if (manufacturer.Contains("asus") || manufacturer.Contains("asustek"))
			{
				var protectedChipsets = new[] { "Z790", "B760", "H770", "X870", "X670", "B650", "A620" };
				SwitchPresenter.Value = protectedChipsets.Any(chipset => product.Contains(chipset)) ? "HII Resources (Protected)" : "HII Resources (Regular)";
			}
			else
			{
				SwitchPresenter.Value = "HII Resources (Other)";
			}
			return;
		}

		// backup
		if (!Directory.Exists(backupFolder))
			Directory.CreateDirectory(backupFolder);

		var currentLines = await File.ReadAllLinesAsync(nvram);

		var existingBackups = Directory.GetFiles(backupFolder, "*.txt").OrderByDescending(file => Path.GetFileName(file)).ToList();

		bool needsBackup = true;

		if (existingBackups.Count > 0)
		{
			var lastBackupLines = await File.ReadAllLinesAsync(existingBackups[0]);
			var currentSettings = BiosSettingParser.ParseFromLines(currentLines).ToList();
			var backupSettings = BiosSettingParser.ParseFromLines(lastBackupLines).ToList();

			bool isEqual = true;
			if (currentSettings.Count != backupSettings.Count)
			{
				isEqual = false;
			}
			else
			{
				for (int i = 0; i < currentSettings.Count; i++)
				{
					var currentSetting = currentSettings[i];
					var backupSetting = backupSettings[i];

					if (currentSetting.SetupQuestion != backupSetting.SetupQuestion || currentSetting.Value != backupSetting.Value || currentSetting.Options.Count != backupSetting.Options.Count)
					{
						isEqual = false;
						break;
					}

					for (int j = 0; j < currentSetting.Options.Count; j++)
					{
						if (currentSetting.Options[j].Label != backupSetting.Options[j].Label || currentSetting.Options[j].IsSelected != backupSetting.Options[j].IsSelected)
						{
							isEqual = false;
							break;
						}
					}
					if (!isEqual) break;
				}
			}
			needsBackup = !isEqual;
		}
		else
		{
			try
			{
				_ = LogHelper.Log(PreparingStage.GPUs, true);
			}
			catch (Exception ex)
			{
				await LogHelper.LogFallbackError(ex);
			}
		}

		if (needsBackup)
		{
			await File.WriteAllLinesAsync(Path.Combine(backupFolder, $"{DateTime.Now.ToLocalTime():yyyy-MM-dd_HH-mm-ss}.txt"), currentLines);
		}

		// parse
		List<BiosSettingModel> parsedList;

		using var stream = File.OpenRead(nvram);
		parsedList = await Task.Run(() =>
		{
			var settings = BiosSettingParser.ParseFromStream(stream).ToList();

			foreach (var setting in settings)
			{
				foreach (var option in setting.Options)
					option.Parent = setting;

				setting.InitializeSelectedOption();

				if (setting.HasValueField)
					setting.OriginalValue = setting.Value;

				if (setting.HasOptions)
					setting.OriginalSelectedOption = setting.SelectedOption;

				var matchingRules = BiosSettingRecommendationsList.Rules
					.Where(rule => string.Equals(rule.SetupQuestion?.Trim(), setting.SetupQuestion?.Trim(), StringComparison.OrdinalIgnoreCase))
					.Where(rule => rule.Condition == null || rule.Condition(settings))
					.OrderByDescending(rule => rule.Condition != null)
					.ToList();

				foreach (var rule in matchingRules)
				{
					string recommendedLabel = rule.RecommendedOption?.Trim().ToLowerInvariant();
					bool ruleApplicable = false;

					if ((rule.Type?.Equals("Option", StringComparison.OrdinalIgnoreCase) ?? false) && setting.HasOptions)
					{
						var recommended = setting.Options
							.FirstOrDefault(option => option.Label?.Trim().ToLowerInvariant() == recommendedLabel);

						if (recommended != null)
						{
							ruleApplicable = true;
							setting.RecommendedOption = recommended;

							if (setting.SelectedOption?.Label?.Trim().ToLowerInvariant() != recommended.Label?.ToLowerInvariant())
							{
								setting.IsRecommended = true;
							}
						}
					}

					if ((rule.Type?.Equals("Value", StringComparison.OrdinalIgnoreCase) ?? false) && setting.HasValueField)
					{
						ruleApplicable = true;
						string currentValue = setting.Value?.Trim().ToLowerInvariant();
						setting.RecommendedValue = rule.RecommendedOption;

						if (!string.IsNullOrEmpty(currentValue) && currentValue != recommendedLabel)
						{
							setting.IsRecommended = true;
						}
					}

					if (ruleApplicable)
						break;
				}

				setting.MarkLoaded();
			}

			return settings;
		});

		ViewModel.BuildTree(parsedList);
		ViewModel.SetIsLoaded(true);
		SwitchPresenter.Value = "Loaded";
	}

	private void Search_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	{
		Search.Focus(FocusState.Programmatic);
		args.Handled = true;
	}

	private void Search_TextChanged(object sender, TextChangedEventArgs e)
	{
		RefreshSearchFilter();
		Search.Focus(FocusState.Programmatic);
	}

	private void Undo_Click(object sender, RoutedEventArgs e)
	{
		ViewModel.Undo();
		RefreshSearchFilter();
		EnsureNodesExpanded();
	}

	private void Redo_Click(object sender, RoutedEventArgs e)
	{
		ViewModel.Redo();
		RefreshSearchFilter();
		EnsureNodesExpanded();
	}

	private void MergeCountBox_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	{
		if (sender.Key == VirtualKey.Up)
		{
			if (ViewModel.MergeCount < ViewModel.RecommendedCount)
				ViewModel.MergeCount++;
		}
		else if (sender.Key == VirtualKey.Down)
		{
			if (ViewModel.MergeCount > 0)
				ViewModel.MergeCount--;
		}
		args.Handled = true;
	}

	private void Merge_Click(object sender, RoutedEventArgs e)
	{
		ViewModel.ApplyRecommendations(ViewModel.MergeCount);
		RefreshSearchFilter();
		EnsureNodesExpanded();
	}

	private void ViewChanges_Checked(object sender, RoutedEventArgs e)
	{
		RefreshSearchFilter();
		EnsureNodesExpanded();
	}

	private void ViewChanges_Unchecked(object sender, RoutedEventArgs e)
	{
		RefreshSearchFilter();
		EnsureNodesExpanded();
	}

	private async void Restore_Click(object sender, RoutedEventArgs e)
	{
		ViewModel.SetIsLoaded(false);

		var picker = new FilePicker(App.MainWindow)
		{
			ShowAllFilesOption = false,
			InitialDirectory = backupFolder
		};
		picker.FileTypeChoices.Add("NVRAM Backup", ["*.txt"]);
		var file = await picker.PickSingleFileAsync();

		if (file != null)
		{
			SwitchPresenter.Value = "Import";
			ViewChanges.IsChecked = false;

			using var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "SCEWIN", "SCEWIN_64.exe"),
					Arguments = @$"/i /s ""{file.Path}""",
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardError = true,
					RedirectStandardOutput = true
				}
			};

			process.Start();
			string errorOutput = await process.StandardError.ReadToEndAsync();
			string output = await process.StandardOutput.ReadToEndAsync();
			await process.WaitForExitAsync();

			string manufacturer = "Unknown";
			string product = "Unknown";

			using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS"))
			{
				if (key != null)
				{
					manufacturer = key.GetValue("BaseBoardManufacturer")?.ToString().ToLowerInvariant() ?? "Unknown";
					product = key.GetValue("BaseBoardProduct")?.ToString().ToUpperInvariant() ?? "Unknown";
				}
			}

			if (errorOutput.Contains("Warning: Error in writing variable", StringComparison.OrdinalIgnoreCase))
			{
				SwitchPresenter.Value = manufacturer.Contains("asus") || manufacturer.Contains("asustek") ? "Write Protected (ASUS)" : manufacturer.Contains("asrock") ? "Write Protected (ASRock)" : "Write Protected (Other)";
				ViewModel.SetIsLoaded(true);
			}
			else if (errorOutput.Contains("Script file imported successfully.", StringComparison.OrdinalIgnoreCase) ||
					 errorOutput.Contains("System configuration not modified.", StringComparison.OrdinalIgnoreCase))
			{
				await Export();
				if (BiosTreeGrid.View != null) BiosTreeGrid.ExpandAllNodes();
			}
			else
			{
				ViewModel.SetIsLoaded(true);
			}
		}
		else
		{
			ViewModel.SetIsLoaded(true);
		}
	}

	private async void Import_Click(object sender, RoutedEventArgs e)
	{
		SwitchPresenter.Value = "Import";
		Search.Text = string.Empty;
		ViewModel.MergeCount = 0;
		ViewChanges.IsChecked = false;
		ViewModel.SetIsLoaded(false);
		ViewModel.ApplyChangesToLines();
		ViewModel.WriteToNvram(nvram);

		using var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "SCEWIN", "SCEWIN_64.exe"),
				Arguments = @$"/i /s ""{nvram}""",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true
			}
		};

		process.Start();
		string errorOutput = await process.StandardError.ReadToEndAsync();
		string output = await process.StandardOutput.ReadToEndAsync();
		await process.WaitForExitAsync();

		string manufacturer = "Unknown";
		string product = "Unknown";

		using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS"))
		{
			if (key != null)
			{
				manufacturer = key.GetValue("BaseBoardManufacturer")?.ToString().ToLowerInvariant() ?? "Unknown";
				product = key.GetValue("BaseBoardProduct")?.ToString().ToUpperInvariant() ?? "Unknown";
			}
		}

		if ((errorOutput.Contains("WARNING : Cannot update protected variable", StringComparison.OrdinalIgnoreCase) ||
			 errorOutput.Contains("WARNING : Error in writing variable", StringComparison.OrdinalIgnoreCase)) &&
			!errorOutput.Contains("Script file imported successfully.", StringComparison.OrdinalIgnoreCase))
		{
			SwitchPresenter.Value = manufacturer.Contains("asus") || manufacturer.Contains("asustek") ? "Write Protected (ASUS)" : manufacturer.Contains("asrock") ? "Write Protected (ASRock)" : "Write Protected (Other)";
			ViewModel.SetIsLoaded(true);
		}
		else
		{
			await Export();
			if (BiosTreeGrid.View != null) BiosTreeGrid.ExpandAllNodes();
		}
	}

	private void BiosTreeGrid_Loaded(object sender, RoutedEventArgs e)
	{
		BiosTreeGrid.View?.Filter = FilterNode;
	}

	private void BiosDiffTreeGrid_Loaded(object sender, RoutedEventArgs e)
	{
		BiosDiffTreeGrid.View?.Filter = FilterDiffNode;
	}

	private void BiosTreeGrid_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.NewSize.Width > 0)
		{
			foreach (var col in BiosTreeGrid.Columns)
				col.Width = double.NaN;
			BiosTreeGrid.InvalidateMeasure();
			BiosTreeGrid.UpdateLayout();
		}
	}

	private void BiosDiffTreeGrid_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.NewSize.Width > 0)
		{
			foreach (var col in BiosDiffTreeGrid.Columns)
				col.Width = double.NaN;
			BiosDiffTreeGrid.InvalidateMeasure();
			BiosDiffTreeGrid.UpdateLayout();
		}
	}

	private void BiosTreeGrid_NodeCollapsing(object sender, NodeCollapsingEventArgs e)
	{
		if (e.Node?.Item is BiosTreeNode node && node.NodeKind == NodeKind.Root)
		{
			node.IsExpanded = false;
		}
	}

	private void BiosDiffTreeGrid_NodeCollapsing(object sender, NodeCollapsingEventArgs e)
	{
		if (e.Node?.Item is BiosTreeNode node && node.NodeKind == NodeKind.Root)
		{
			node.IsExpanded = false;
		}
	}

	private void BiosTreeGrid_CellToolTipOpening(object sender, TreeGridCellToolTipOpeningEventArgs e)
	{
		if (e.Record is not BiosTreeNode node)
		{
			e.ToolTip.Visibility = Visibility.Collapsed;
			return;
		}

		string tooltipContent = e.Column?.MappingName switch
		{
			"DisplayName" => node.ToolTipText,
			"DisplayCurrent" => node.DisplayCurrent,
			"DisplayRecommended" => node.DisplayRecommended,
			"DisplayDefault" => node.DisplayDefault,
			_ => null
		};

		if (string.IsNullOrWhiteSpace(tooltipContent))
		{
			e.ToolTip.Visibility = Visibility.Collapsed;
		}
		else
		{
			e.ToolTip.Content = tooltipContent;
			e.ToolTip.Visibility = Visibility.Visible;
		}
	}

	private void BiosDiffTreeGrid_CellToolTipOpening(object sender, TreeGridCellToolTipOpeningEventArgs e)
	{
		if (e.Record is not BiosTreeNode node)
		{
			e.ToolTip.Visibility = Visibility.Collapsed;
			return;
		}

		string tooltipContent = e.Column?.MappingName switch
		{
			"DisplayName" => node.ToolTipText,
			"DisplayOriginal" => node.DisplayOriginal,
			"DisplayCurrent" => node.DisplayCurrent,
			"DisplayDefault" => node.DisplayDefault,
			_ => null
		};

		if (string.IsNullOrWhiteSpace(tooltipContent))
		{
			e.ToolTip.Visibility = Visibility.Collapsed;
		}
		else
		{
			e.ToolTip.Content = tooltipContent;
			e.ToolTip.Visibility = Visibility.Visible;
		}
	}

	private void BiosTreeGrid_CurrentCellBeginEdit(object sender, TreeGridCurrentCellBeginEditEventArgs e)
	{
		var node = BiosTreeGrid.GetNodeAtRowIndex(e.RowColumnIndex.RowIndex)?.Item as BiosTreeNode ?? BiosTreeGrid.CurrentItem as BiosTreeNode;
		if (node?.NodeKind == NodeKind.Root)
		{
			e.Cancel = true;
			return;
		}
		node?.BeginCellEdit();
	}

	private void BiosDiffTreeGrid_CurrentCellBeginEdit(object sender, TreeGridCurrentCellBeginEditEventArgs e)
	{
		var node = BiosDiffTreeGrid.GetNodeAtRowIndex(e.RowColumnIndex.RowIndex)?.Item as BiosTreeNode ?? BiosDiffTreeGrid.CurrentItem as BiosTreeNode;
		if (node?.NodeKind == NodeKind.Root)
		{
			e.Cancel = true;
			return;
		}
		node?.BeginCellEdit();
	}

	private void BiosTreeGrid_CurrentCellEndEdit(object sender, CurrentCellEndEditEventArgs e)
	{
		var node = BiosTreeGrid.GetNodeAtRowIndex(e.RowColumnIndex.RowIndex)?.Item as BiosTreeNode ?? BiosTreeGrid.CurrentItem as BiosTreeNode;
		if (node?.NodeKind == NodeKind.Group)
			ViewModel.BatchEdit(() => node.CommitCellEdit());
		else
			node?.CommitCellEdit();
	}

	private void BiosDiffTreeGrid_CurrentCellEndEdit(object sender, CurrentCellEndEditEventArgs e)
	{
		var node = BiosDiffTreeGrid.GetNodeAtRowIndex(e.RowColumnIndex.RowIndex)?.Item as BiosTreeNode ?? BiosDiffTreeGrid.CurrentItem as BiosTreeNode;
		if (node?.NodeKind == NodeKind.Group)
			ViewModel.BatchEdit(() => node.CommitCellEdit());
		else
			node?.CommitCellEdit();
	}

	private void EditControl_Loaded(object sender, RoutedEventArgs e)
	{
		if (sender is Control control)
			control.Focus(FocusState.Programmatic);

		if (sender is Microsoft.UI.Xaml.Controls.TextBox textBox)
			textBox.SelectAll();
	}

	private void BiosTreeGrid_TreeGridContextFlyoutOpening(object sender, TreeGridContextFlyoutEventArgs e)
	{
		CreateContextMenu(e, BiosTreeGrid);
	}

	private void BiosDiffTreeGrid_TreeGridContextFlyoutOpening(object sender, TreeGridContextFlyoutEventArgs e)
	{
		CreateContextMenu(e, BiosDiffTreeGrid);
	}

	private static void CreateContextMenu(TreeGridContextFlyoutEventArgs e, SfTreeGrid grid)
	{
		if (e.ContextFlyoutType != Syncfusion.UI.Xaml.TreeGrid.ContextFlyoutType.HeaderCell)
			return;

		var col = grid.Columns[grid.ResolveToGridVisibleColumnIndex(e.RowColumnIndex.ColumnIndex)];

		e.ContextFlyout.Items.Clear();

		if (col == null || !col.AllowSorting)
			return;

		var isAscending = grid.SortColumnDescriptions.Any(description => description.ColumnName == col.MappingName && description.SortDirection == SortDirection.Ascending);
		var isDescending = grid.SortColumnDescriptions.Any(description => description.ColumnName == col.MappingName && description.SortDirection == SortDirection.Descending);

		var sortAsc = new RadioMenuFlyoutItem
		{
			Text = "Sort Ascending",
			GroupName = "SortGroup",
			IsChecked = isAscending && !isDescending
		};
		sortAsc.Click += (_, _) =>
		{
			grid.SortColumnDescriptions.Clear();
			grid.SortColumnDescriptions.Add(new SortColumnDescription
			{
				ColumnName = col.MappingName,
				SortDirection = SortDirection.Ascending
			});
		};
		e.ContextFlyout.Items.Add(sortAsc);

		var sortDesc = new RadioMenuFlyoutItem
		{
			Text = "Sort Descending",
			GroupName = "SortGroup",
			IsChecked = isDescending
		};
		sortDesc.Click += (_, _) =>
		{
			grid.SortColumnDescriptions.Clear();
			grid.SortColumnDescriptions.Add(new SortColumnDescription
			{
				ColumnName = col.MappingName,
				SortDirection = SortDirection.Descending
			});
		};
		e.ContextFlyout.Items.Add(sortDesc);

		e.ContextFlyout.Items.Add(new MenuFlyoutSeparator());

		var clearSort = new MenuFlyoutItem { Text = "Clear Sorting" };
		clearSort.Click += (_, _) => grid.SortColumnDescriptions.Clear();
		e.ContextFlyout.Items.Add(clearSort);
	}

	private void EnsureNodesExpanded()
	{
		foreach (var root in ViewModel.TreeNodes.Where(node => node.NodeKind == NodeKind.Root))
		{
			if (root.IsExpanded)
			{
				var node = BiosTreeGrid.View?.Nodes?.FirstOrDefault(treeNode => treeNode.Item == root);
				if (node != null && !node.IsExpanded)
					BiosTreeGrid.ExpandNode(node);

				foreach (var child in root.Children.Where(c => c.NodeKind == NodeKind.Group))
				{
					if (child.IsExpanded)
					{
						var childNode = BiosTreeGrid.View?.Nodes?.FirstOrDefault(treeNode => treeNode.Item == child);
						if (childNode != null && !childNode.IsExpanded)
							BiosTreeGrid.ExpandNode(childNode);
					}
				}
			}
		}
	}

	private void RefreshSearchFilter()
	{
		BiosTreeGrid.View?.Filter = FilterNode;
		BiosDiffTreeGrid.View?.Filter = FilterDiffNode;
		BiosTreeGrid.View?.RefreshFilter();
		BiosDiffTreeGrid.View?.RefreshFilter();

		var searchText = Search.Text;
		bool isSearchEmpty = string.IsNullOrWhiteSpace(searchText);

		var allRoot = ViewModel.TreeNodes.LastOrDefault();
		if (allRoot != null)
		{
			int totalCount = CountMatchingNodes(allRoot, searchText, isSearchEmpty);
			allRoot.DisplayName = $"{(isSearchEmpty ? "All Settings" : "Results")} ({totalCount})";
		}

		if (ViewModel.DiffNodes.FirstOrDefault() is { } changesRoot)
		{
			int diffCount = CountMatchingNodes(changesRoot, searchText, isSearchEmpty);
			changesRoot.DisplayName = $"{(isSearchEmpty ? "Changes" : "Results")} ({diffCount})";

			foreach (var child in changesRoot.Children)
			{
				if (child.NodeKind != NodeKind.Group) continue;
				int childCount = CountMatchingNodes(child, searchText, isSearchEmpty);
				child.DisplayName = $"{child.DiffGroupKey} ({childCount})";
			}
		}
	}

	private int CountMatchingNodes(BiosTreeNode node, string searchText, bool isSearchEmpty)
	{
		if (node == null) return 0;

		if (node.NodeKind == NodeKind.Leaf)
		{
			if (ViewChanges.IsChecked == true && node.Model?.IsModified != true)
				return 0;

			if (isSearchEmpty) return 1;

			bool matches = false;

			if (ViewModel.FilterSetting && TextMatches(node.DisplayName, searchText))
				matches = true;

			if (!matches && ViewModel.FilterDescription && TextMatches(node.Model?.HelpString, searchText))
				matches = true;

			if (!matches && ViewModel.FilterCurrent && TextMatches(node.DisplayCurrent, searchText))
				matches = true;

			return matches ? 1 : 0;
		}

		int count = 0;
		if (node.Children != null)
		{
			foreach (var child in node.Children)
			{
				count += CountMatchingNodes(child, searchText, isSearchEmpty);
			}
		}

		return count;
	}

	private bool TextMatches(string text, string searchText)
	{
		if (text == null) return false;
		return ViewModel.FilterMode == BiosSettingViewModel.FilterModeType.ExactMatch ? text.Equals(searchText, StringComparison.OrdinalIgnoreCase) : text.Contains(searchText, StringComparison.OrdinalIgnoreCase);
	}

	private bool FilterDiffNode(object obj)
	{
		if (obj is not BiosTreeNode node) return true;

		var searchText = Search.Text;
		var hasSearch = !string.IsNullOrWhiteSpace(searchText);

		if (node.NodeKind == NodeKind.Root) return true;

		if (!hasSearch) return true;

		if (ViewModel.FilterSetting)
		{
			if (node.NodeKind == NodeKind.Leaf && TextMatches(node.DisplayName, searchText)) return true;
			if (node.NodeKind == NodeKind.Group && node.Children.Any(child => TextMatches(child.DisplayName, searchText))) return true;
		}

		if (ViewModel.FilterDescription)
		{
			if (node.NodeKind == NodeKind.Leaf && TextMatches(node.Model?.HelpString, searchText)) return true;
			if (node.NodeKind == NodeKind.Group && node.Children.Any(child => TextMatches(child.Model?.HelpString, searchText))) return true;
		}

		if (ViewModel.FilterCurrent)
		{
			if (node.NodeKind == NodeKind.Leaf && TextMatches(node.DisplayCurrent, searchText)) return true;
			if (node.NodeKind == NodeKind.Group && node.Children.Any(child => TextMatches(child.DisplayCurrent, searchText))) return true;
		}

		return false;
	}

	private bool FilterNode(object obj)
	{
		if (obj is not BiosTreeNode node) return true;

		var searchText = Search.Text;
		var hasSearch = !string.IsNullOrWhiteSpace(searchText);

		if (node.NodeKind == NodeKind.Root)
		{
			var isRecommended = node.DisplayName.StartsWith("Recommended");
			if ((ViewChanges.IsChecked == true || hasSearch) && isRecommended) return false;
			return true;
		}

		if (ViewChanges.IsChecked == true)
		{
			if (node.NodeKind == NodeKind.Group && !node.Children.Any(child => child.Model?.IsModified == true)) return false;
			if (node.NodeKind == NodeKind.Leaf && node.Model?.IsModified != true) return false;
		}

		if (!hasSearch) return true;

		if (ViewModel.FilterSetting)
		{
			if (node.NodeKind == NodeKind.Leaf && TextMatches(node.DisplayName, searchText)) return true;
			if (node.NodeKind == NodeKind.Group && node.Children.Any(child => TextMatches(child.DisplayName, searchText))) return true;
		}

		if (ViewModel.FilterDescription)
		{
			if (node.NodeKind == NodeKind.Leaf && TextMatches(node.Model?.HelpString, searchText)) return true;
			if (node.NodeKind == NodeKind.Group && node.Children.Any(child => TextMatches(child.Model?.HelpString, searchText))) return true;
		}

		if (ViewModel.FilterCurrent)
		{
			if (node.NodeKind == NodeKind.Leaf && TextMatches(node.DisplayCurrent, searchText)) return true;
			if (node.NodeKind == NodeKind.Group && node.Children.Any(child => TextMatches(child.DisplayCurrent, searchText))) return true;
		}

		return false;
	}
}
