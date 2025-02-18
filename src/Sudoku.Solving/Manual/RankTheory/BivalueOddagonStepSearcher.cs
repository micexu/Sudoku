﻿using System.Collections.Generic;
using System.Extensions;
using System.Linq;
using Sudoku.Data;
using Sudoku.DocComments;
using Sudoku.Solving.Manual.Extensions;
using Sudoku.Techniques;
using static System.Numerics.BitOperations;
using static Sudoku.Constants.Tables;
using static Sudoku.Solving.Manual.FastProperties;

namespace Sudoku.Solving.Manual.RankTheory
{
	/// <summary>
	/// Encapsulates a <b>bi-value oddagon</b> technique searcher.
	/// </summary>
	public sealed partial class BivalueOddagonStepSearcher : RankTheoryStepSearcher
	{
		/// <inheritdoc cref="SearchingProperties"/>
		public static TechniqueProperties Properties { get; } = new(14, nameof(Technique.BivalueOddagonType1))
		{
			DisplayLevel = 2
		};


		/// <inheritdoc/>
		public override void GetAll(IList<StepInfo> accumulator, in SudokuGrid grid)
		{
			if (BivalueMap.Count < 4)
			{
				return;
			}

			var resultAccumulator = new List<BivalueOddagonStepInfo>();
			var loops = new List<(Cells Map, IReadOnlyList<Link> Links)>();
			var tempLoop = new List<int>();
			foreach (int cell in BivalueMap)
			{
				short mask = grid.GetCandidates(cell);
				int d1 = TrailingZeroCount(mask), d2 = mask.GetNextSet(d1);
				var loopMap = Cells.Empty;
				loops.Clear();
				tempLoop.Clear();

				f(grid, d1, d2, cell, (RegionLabel)byte.MaxValue, default, 2);

				if (loops.Count == 0)
				{
					continue;
				}

				short comparer = (short)(1 << d1 | 1 << d2);
				foreach (var (currentLoop, links) in loops)
				{
					var extraCellsMap = currentLoop - BivalueMap;
					switch (extraCellsMap.Count)
					{
						//case <= 0:
						//{
						//	// Invalid grid status.
						//	throw NoSolutionException(grid);
						//}
						case 1:
						{
							// Type 1.
							CheckType1(resultAccumulator, grid, d1, d2, currentLoop, links, extraCellsMap);

							break;
						}
						default:
						{
							// Type 2, 3, 4.
							// Here use default label to ensure the order of
							// the handling will be 1->2->3->4.
							CheckType2(resultAccumulator, grid, d1, d2, currentLoop, links, extraCellsMap, comparer);

							if (extraCellsMap.Count == 2)
							{
								CheckType3(resultAccumulator, grid, d1, d2, currentLoop, links, extraCellsMap, comparer);
							}

							break;
						}
					}
				}

				if (resultAccumulator.Count == 0)
				{
					continue;
				}

				accumulator.AddRange(
					from info in resultAccumulator.RemoveDuplicateItems()
					orderby info.Loop.Count, info.TechniqueCode
					select info);

				/*Don't convert to method or static local function*/
				void f(
					in SudokuGrid grid,
					int d1, int d2, int cell, RegionLabel lastLabel, short exDigitsMask,
					int allowedExtraCellsCount)
				{
					loopMap.AddAnyway(cell);
					tempLoop.Add(cell);

					for (var label = RegionLabel.Block; label <= RegionLabel.Column; label++)
					{
						if (label == lastLabel)
						{
							continue;
						}

						int region = cell.ToRegion(label);
						var cellsMap = RegionMaps[region] & new Cells(EmptyMap) { ~cell };
						if (cellsMap.IsEmpty)
						{
							continue;
						}

						foreach (int nextCell in cellsMap)
						{
							if (tempLoop[0] == nextCell && tempLoop.Count >= 5 && (tempLoop.Count & 1) != 0
								&& IsValidLoop(loopMap))
							{
								// The loop is closed. Now construct the result pair.
								loops.Add((loopMap, tempLoop.GetLinks()));
							}
							else if (!loopMap.Contains(nextCell) && grid[nextCell, d1] && grid[nextCell, d2])
							{
								// Here, a loop can be found if and only if
								// two cells both contain 'd1' and 'd2'.
								// Incomplete ULs can't be found at present.
								short nextCellMask = grid.GetCandidates(nextCell);
								exDigitsMask |= nextCellMask;
								exDigitsMask &= (short)~((1 << d1) | (1 << d2));
								int digitsCount = PopCount((uint)nextCellMask);

								// We can continue if:
								// - The cell has exactly 2 digits of the loop.
								// - The cell has 1 extra digit, the same as all previous cells
								// with an extra digit (for type 2 only).
								// - The cell has extra digits and the maximum number of cells
								// with extra digits, 2, is not reached.
								if
								(
									digitsCount != 2 && (
										exDigitsMask == 0 || (exDigitsMask & exDigitsMask - 1) != 0
									) && allowedExtraCellsCount <= 0
								)
								{
									continue;
								}

								f(
									grid, d1, d2, nextCell, label, exDigitsMask,
									digitsCount > 2 ? allowedExtraCellsCount - 1 : allowedExtraCellsCount);
							}
						}
					}

					// Backtracking.
					loopMap.Remove(cell);
					tempLoop.RemoveLastElement();
				}
			}
		}


		/// <summary>
		/// To check whether the loop is valid.
		/// </summary>
		/// <param name="cells">The loop.</param>
		/// <returns>The <see cref="bool"/> result.</returns>
		private static bool IsValidLoop(in Cells cells)
		{
			foreach (int region in cells.Regions)
			{
				if ((cells & RegionMaps[region]).Count >= 3)
				{
					return false;
				}
			}

			return true;
		}


		partial void CheckType1(IList<BivalueOddagonStepInfo> accumulator, in SudokuGrid grid, int d1, int d2, in Cells loop, IReadOnlyList<Link> links, in Cells extraCellsMap);
		partial void CheckType2(IList<BivalueOddagonStepInfo> accumulator, in SudokuGrid grid, int d1, int d2, in Cells loop, IReadOnlyList<Link> links, in Cells extraCellsMap, short comparer);
		partial void CheckType3(IList<BivalueOddagonStepInfo> accumulator, in SudokuGrid grid, int d1, int d2, in Cells loop, IReadOnlyList<Link> links, in Cells extraCellsMap, short comparer);
	}
}
