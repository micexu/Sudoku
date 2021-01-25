﻿using System;

namespace Sudoku.Solving.Manual.Fishes
{
	public sealed partial record ComplexFishStepInfo(
		IReadOnlyList<Conclusion> Conclusions, IReadOnlyList<View> Views, int Digit,
		IReadOnlyList<int> BaseSets, IReadOnlyList<int> CoverSets, in Cells Exofins,
		in Cells Endofins, bool IsFranken, bool? IsSashimi)
	{
		/// <summary>
		/// Indicates the fin modifiers.
		/// </summary>
		[Flags]
		private enum FinModifiers
		{
			/// <summary>
			/// Indicates the normal fish (i.e. no fins).
			/// </summary>
			Normal = 1,

			/// <summary>
			/// Indicates the finned fish
			/// (i.e. contains fins, but the fish may be regular when the fins are removed).
			/// </summary>
			Finned = 2,

			/// <summary>
			/// Indicates the sashimi fish
			/// (i.e. contains fins, and the fish may be degenerated to hidden singles when the fins are removed).
			/// </summary>
			Sashimi = 4,

			/// <summary>
			/// Indicates the siamese fish (i.e. two fish with different cover sets).
			/// </summary>
			Siamese = 8
		}
	}
}
