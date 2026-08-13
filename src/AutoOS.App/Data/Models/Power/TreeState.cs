namespace AutoOS.App.Data.Models.Power;

internal sealed record TreeState(Node Root, Dictionary<Guid, Node> Subgroups, Dictionary<Setting, Node> SettingNodes);