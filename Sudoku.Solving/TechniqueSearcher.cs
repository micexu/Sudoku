﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sudoku.Constants;
using Sudoku.Data;
using Sudoku.DocComments;
using Sudoku.Solving.Annotations;
using Sudoku.Solving.Manual;

namespace Sudoku.Solving
{
	/// <summary>
	/// Encapsulates a step finder that used in solving in
	/// <see cref="ManualSolver"/>.
	/// </summary>
	/// <seealso cref="ManualSolver"/>
	public abstract partial class TechniqueSearcher : IComparable<TechniqueSearcher?>, IEquatable<TechniqueSearcher?>
	{
		/// <summary>
		/// Take a technique step after searched all solving steps.
		/// </summary>
		/// <param name="grid">(<see langword="in"/> parameter) The grid to search steps.</param>
		/// <returns>A technique information.</returns>
		public TechniqueInfo? GetOne(in SudokuGrid grid)
		{
			var bag = new List<TechniqueInfo>();
			GetAll(bag, grid);
			return bag.FirstOrDefault();
		}

		/// <summary>
		/// Accumulate all technique information instances into the specified accumulator.
		/// </summary>
		/// <param name="accumulator">The accumulator to store technique information.</param>
		/// <param name="grid">(<see langword="in"/> parameter) The grid to search for techniques.</param>
		public abstract void GetAll(IList<TechniqueInfo> accumulator, in SudokuGrid grid);

		/// <inheritdoc/>
		public virtual int CompareTo(TechniqueSearcher? other) =>
			GetHashCode().CompareTo(other?.GetHashCode() ?? int.MaxValue);

		/// <inheritdoc/>
		public sealed override int GetHashCode() =>
			TechniqueProperties.GetPropertiesFrom(this)?.Priority ?? int.MaxValue;

		/// <inheritdoc/>
		public virtual bool Equals(TechniqueSearcher? other) => InternalEquals(this, other);

		/// <inheritdoc/>
		public sealed override bool Equals(object? obj) => Equals(obj as TechniqueSearcher);

		/// <inheritdoc/>
		public sealed override string? ToString() => TechniqueProperties.GetPropertiesFrom(this)!.DisplayLabel;


		/// <summary>
		/// Initialize the maps that used later.
		/// </summary>
		/// <param name="grid">(<see langword="in"/> parameter) The grid.</param>
		public static void InitializeMaps(in SudokuGrid grid) =>
			(EmptyMap, BivalueMap, CandMaps, DigitMaps, ValueMaps) = grid;

#nullable disable warnings
		/// <summary>
		/// Internal equals method.
		/// </summary>
		/// <param name="left">The left comparer.</param>
		/// <param name="right">The right comparer.</param>
		/// <returns>A <see cref="bool"/> value.</returns>
		private static bool InternalEquals(TechniqueSearcher? left, TechniqueSearcher? right) =>
			(left, right) switch
			{
				(null, null) => true,
				(not null, not null) => left.GetHashCode() == right.GetHashCode(),
				_ => false
			};
#nullable restore warnings


		/// <inheritdoc cref="Operators.operator =="/>
		public static bool operator ==(TechniqueSearcher? left, TechniqueSearcher? right) =>
			InternalEquals(left, right);

		/// <inheritdoc cref="Operators.operator !="/>
		public static bool operator !=(TechniqueSearcher? left, TechniqueSearcher? right) => !(left == right);

		/// <inheritdoc cref="Operators.operator &gt;"/>
		public static bool operator >(TechniqueSearcher left, TechniqueSearcher right) =>
			left.CompareTo(right) > 0;

		/// <inheritdoc cref="Operators.operator &gt;="/>
		public static bool operator >=(TechniqueSearcher left, TechniqueSearcher right) =>
			left.CompareTo(right) >= 0;

		/// <inheritdoc cref="Operators.operator &lt;"/>
		public static bool operator <(TechniqueSearcher left, TechniqueSearcher right) =>
			left.CompareTo(right) < 0;

		/// <inheritdoc cref="Operators.operator &lt;="/>
		public static bool operator <=(TechniqueSearcher left, TechniqueSearcher right) =>
			left.CompareTo(right) <= 0;
	}
}
