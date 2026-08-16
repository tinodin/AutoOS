using AutoOS.Core.Data.Models.Bios;

namespace AutoOS.App.Data.Models.Bios;

internal sealed record TreeState(Node Root, Dictionary<string, Node> PathNodes, Dictionary<Setting, Node> SettingNodes);