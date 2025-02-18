﻿using System.Collections.Generic;
using Sudoku.CodeGen;
using Sudoku.Data;
using Sudoku.Data.Collections;
using Sudoku.Drawing;
using Sudoku.Techniques;

namespace Sudoku.Solving.Manual.Sdps
{
	/// <summary>
	/// Provides a usage of <b>guardian</b> technique.
	/// </summary>
	/// <param name="Conclusions">All conclusions.</param>
	/// <param name="Views">All views.</param>
	/// <param name="Digit">The digit used.</param>
	/// <param name="Loop">The loop.</param>
	/// <param name="Guardians">All guardians.</param>
	[AutoHashCode(nameof(Digit), nameof(Loop), nameof(Guardians))]
	public sealed partial record GuardianStepInfo(
		IReadOnlyList<Conclusion> Conclusions, IReadOnlyList<View> Views, int Digit, in Cells Loop,
		in Cells Guardians
	) : SdpStepInfo(Conclusions, Views, Digit)
	{
		/// <inheritdoc/>
		public override decimal Difficulty => 5.5M + .1M * (Loop.Count + (Guardians.Count >> 1) >> 1);

		/// <inheritdoc/>
		public override Technique TechniqueCode => Technique.Guardian;

		/// <inheritdoc/>
		public override TechniqueTags TechniqueTags => base.TechniqueTags | TechniqueTags.LongChaining;

		/// <inheritdoc/>
		public override TechniqueGroup TechniqueGroup => TechniqueGroup.Guardian;

		/// <inheritdoc/>
		public override DifficultyLevel DifficultyLevel => DifficultyLevel.Fiendish;


		/// <inheritdoc/>
		public bool Equals(GuardianStepInfo? other) =>
			other is not null && Loop == other.Loop && Guardians == other.Guardians && Digit == other.Digit;


		/// <inheritdoc/>
		public override string ToString()
		{
			string cellsStr = Loop.ToString();
			string guardians = Guardians.ToString();
			string elimStr = new ConclusionCollection(Conclusions).ToString();
			string guardianSingularOrPlural = Guardians.Count == 1 ? "a guardian" : "guardians";
			return $"{Name}: Cells {cellsStr} with {guardianSingularOrPlural} {guardians} => {elimStr}";
		}
	}
}
