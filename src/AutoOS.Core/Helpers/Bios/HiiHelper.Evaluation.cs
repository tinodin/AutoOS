using AutoOS.Core.Data.Enums.Bios;
using AutoOS.Core.Data.Models.Bios;

namespace AutoOS.Core.Helpers.Bios;

public static partial class HiiHelper
{
	private static bool BlockHides(List<SuppressionBlock>? blocks, Func<ushort, byte?> currentValue)
	{
		if (blocks == null)
			return false;

		foreach (SuppressionBlock block in blocks)
		{
			if (EvaluateExpression(block.Tokens, currentValue) == true)
				return true;
		}

		return false;
	}

	private static bool? EvaluateExpression(IReadOnlyList<SuppressionToken> tokens, Func<ushort, byte?> currentValue)
	{
		var stack = new Stack<ExpressionValue>();

		foreach (SuppressionToken token in tokens)
		{
			switch (token.Opcode)
			{
				case (byte)IfrOpcode.UInt8:
				case (byte)IfrOpcode.UInt16:
				case (byte)IfrOpcode.UInt32:
				case (byte)IfrOpcode.UInt64:
					stack.Push(ExpressionValue.FromNumber(token.Value));
					break;
				case (byte)IfrOpcode.True:
					stack.Push(ExpressionValue.FromBoolean(true));
					break;
				case (byte)IfrOpcode.False:
					stack.Push(ExpressionValue.FromBoolean(false));
					break;
				case (byte)IfrOpcode.EqIdVal:
				{
					byte? current = currentValue(token.Qid);
					stack.Push(current is null ? ExpressionValue.Null : ExpressionValue.FromBoolean(current.Value == token.Value));
					break;
				}
				case (byte)IfrOpcode.EqIdId:
				{
					byte? left = currentValue(token.Qid);
					byte? right = currentValue((ushort)token.Value);
					stack.Push(left is null || right is null ? ExpressionValue.Null : ExpressionValue.FromBoolean(left.Value == right.Value));
					break;
				}
				case (byte)IfrOpcode.EqIdValList:
				{
					byte? current = currentValue(token.Qid);
					stack.Push(current is null ? ExpressionValue.Null : ExpressionValue.FromBoolean(token.Values?.Contains(current.Value) == true));
					break;
				}
				case (byte)IfrOpcode.QuestionRef1:
				{
					byte? current = currentValue(token.Qid);
					stack.Push(current is null ? ExpressionValue.Null : ExpressionValue.FromNumber(current.Value));
					break;
				}
				default:
					if (!ApplyOperator(stack, token.Opcode))
						return null;
					break;
			}
		}

		return stack.Count > 0 && stack.Peek().TryGetBoolean(out bool result) ? result : (bool?)null;
	}

	private static bool ApplyOperator(Stack<ExpressionValue> stack, byte op)
	{
		if (op == (byte)IfrOpcode.Not)
		{
			if (stack.Count < 1)
				return false;

			ExpressionValue operand = stack.Pop();
			stack.Push(operand.TryGetBoolean(out bool boolean) ? ExpressionValue.FromBoolean(!boolean) : ExpressionValue.Null);
			return true;
		}

		if (stack.Count < 2)
			return false;

		ExpressionValue right = stack.Pop();
		ExpressionValue left = stack.Pop();

		switch (op)
		{
			case (byte)IfrOpcode.And:
			{
				bool? a = left.TryGetBoolean(out bool aVal) ? aVal : null;
				bool? b = right.TryGetBoolean(out bool bVal) ? bVal : null;
				stack.Push(a == false || b == false ? ExpressionValue.FromBoolean(false)
					: a == true && b == true ? ExpressionValue.FromBoolean(true) : ExpressionValue.Null);
				break;
			}
			case (byte)IfrOpcode.Or:
			{
				bool? a = left.TryGetBoolean(out bool aVal) ? aVal : null;
				bool? b = right.TryGetBoolean(out bool bVal) ? bVal : null;
				stack.Push(a == true || b == true ? ExpressionValue.FromBoolean(true)
					: a == false && b == false ? ExpressionValue.FromBoolean(false) : ExpressionValue.Null);
				break;
			}
			case (byte)IfrOpcode.Equal:
				stack.Push(CompareNumbers(left, right, (a, b) => a == b));
				break;
			case (byte)IfrOpcode.NotEqual:
				stack.Push(CompareNumbers(left, right, (a, b) => a != b));
				break;
			case (byte)IfrOpcode.LessThan:
				stack.Push(CompareNumbers(left, right, (a, b) => a < b));
				break;
			case (byte)IfrOpcode.LessEqual:
				stack.Push(CompareNumbers(left, right, (a, b) => a <= b));
				break;
			case (byte)IfrOpcode.GreaterThan:
				stack.Push(CompareNumbers(left, right, (a, b) => a > b));
				break;
			case (byte)IfrOpcode.GreaterEqual:
				stack.Push(CompareNumbers(left, right, (a, b) => a >= b));
				break;
			default:
				return false;
		}

		return true;
	}

	private static ExpressionValue CompareNumbers(ExpressionValue left, ExpressionValue right, Func<ulong, ulong, bool> comparison)
	{
		if (!left.TryGetNumber(out ulong a) || !right.TryGetNumber(out ulong b))
			return ExpressionValue.Null;

		return ExpressionValue.FromBoolean(comparison(a, b));
	}

	private readonly struct ExpressionValue
	{
		public ExpressionValueKind Kind { get; }

		public ulong Number { get; }

		public bool Boolean { get; }

		private ExpressionValue(ExpressionValueKind kind, ulong number, bool boolean)
		{
			Kind = kind;
			Number = number;
			Boolean = boolean;
		}

		public static ExpressionValue FromNumber(ulong number) => new(ExpressionValueKind.Number, number, false);

		public static ExpressionValue FromBoolean(bool boolean) => new(ExpressionValueKind.Boolean, 0, boolean);

		public static ExpressionValue Null => new(ExpressionValueKind.Null, 0, false);

		public bool TryGetNumber(out ulong number)
		{
			if (Kind == ExpressionValueKind.Number)
			{
				number = Number;
				return true;
			}
			if (Kind == ExpressionValueKind.Boolean)
			{
				number = Boolean ? 1UL : 0UL;
				return true;
			}
			number = 0;
			return false;
		}

		public bool TryGetBoolean(out bool boolean)
		{
			if (Kind == ExpressionValueKind.Boolean)
			{
				boolean = Boolean;
				return true;
			}
			boolean = false;
			return false;
		}
	}

	private enum ExpressionValueKind
	{
		Number,
		Boolean,
		Null
	}
}