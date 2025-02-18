﻿using System.Collections.Generic;
using System.Text;
using Sudoku.Data;
using Sudoku.Data.Collections;
using Sudoku.Drawing;
using Sudoku.Techniques;

namespace Sudoku.Solving.Manual.Alses
{
	/// <summary>
	/// Provides a usage of <b>death blossom</b> technique.
	/// </summary>
	/// <param name="Conclusions">All conclusions.</param>
	/// <param name="Views">All views.</param>
	/// <param name="Pivot">The pivot cell.</param>
	/// <param name="Petals">All ALSes used.</param>
	public sealed record DbStepInfo(
		IReadOnlyList<Conclusion> Conclusions, IReadOnlyList<View> Views, int Pivot,
		IReadOnlyDictionary<int, Als> Petals
	) : AlsStepInfo(Conclusions, Views)
	{
		/// <inheritdoc/>
		public override decimal Difficulty => 8.0M + Petals.Count * .1M;

		/// <inheritdoc/>
		public override TechniqueTags TechniqueTags => base.TechniqueTags | TechniqueTags.LongChaining;

		/// <inheritdoc/>
		public override Technique TechniqueCode => Technique.DeathBlossom;

		/// <inheritdoc/>
		public override TechniqueGroup TechniqueGroup => TechniqueGroup.AlsChainingLike;

		/// <inheritdoc/>
		public override DifficultyLevel DifficultyLevel => DifficultyLevel.Nightmare;


		/// <inheritdoc/>
		public override string ToString()
		{
			string pivotStr = new Cells { Pivot }.ToString();
			string elimStr = new ConclusionCollection(Conclusions).ToString();
			return $"{Name}: Cell {pivotStr} - {AlsPetalsToString()} => {elimStr}";
		}

		/// <summary>
		/// Get the string result from those ALS petals and their own branches.
		/// </summary>
		/// <returns>The string result.</returns>
		private unsafe string? AlsPetalsToString()
		{
			const string separator = ", ";

			var sb = new ValueStringBuilder(stackalloc char[50]);
			sb.AppendRange(Petals, &appender, separator);
			return sb.ToString();

			static string appender(KeyValuePair<int, Als> pair)
			{
				var sb = new ValueStringBuilder(stackalloc char[15]);
				sb.Append(pair.Key + 1);
				sb.Append(" - ");
				sb.Append(pair.Value.ToString());

				return sb.ToString();
			}
		}
	}
}
