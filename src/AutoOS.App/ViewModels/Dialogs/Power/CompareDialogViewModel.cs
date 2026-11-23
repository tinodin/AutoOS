using AutoOS.Core.Data.Models.Power;

namespace AutoOS.App.ViewModels.Dialogs.Power;

public sealed partial class CompareDialogViewModel : BaseDialogViewModel
{
	public CompareDialogViewModel(IEnumerable<Plan> plans, Plan? selectedPlan1 = null, Plan? selectedPlan2 = null)
	{
		Plans = [.. plans];
		SelectedPlan1 = Plans.FirstOrDefault(plan => plan.Guid == selectedPlan1?.Guid) ?? Plans.FirstOrDefault();
		SelectedPlan2 = Plans.FirstOrDefault(plan => plan.Guid == selectedPlan2?.Guid) ?? Plans.FirstOrDefault(plan => plan.Guid != SelectedPlan1?.Guid) ?? Plans.FirstOrDefault();
		Title = "Compare";
		IsPrimaryButtonEnabled = PlansDiffer;
		PrimaryButtonText = "Compare";
		CloseButtonText = "Cancel";
	}

	public ObservableCollection<Plan> Plans { get; }

	[ObservableProperty]
	public partial Plan? SelectedPlan1 { get; set; }

	[ObservableProperty]
	public partial Plan? SelectedPlan2 { get; set; }

	private bool PlansDiffer => SelectedPlan1 is { } plan1 && SelectedPlan2 is { } plan2 && plan1.Guid != plan2.Guid;

	partial void OnSelectedPlan1Changed(Plan? value) => IsPrimaryButtonEnabled = PlansDiffer;

	partial void OnSelectedPlan2Changed(Plan? value) => IsPrimaryButtonEnabled = PlansDiffer;
}
