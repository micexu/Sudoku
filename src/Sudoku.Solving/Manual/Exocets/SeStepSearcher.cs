﻿using System;
using System.Collections.Generic;
using System.Extensions;
using System.Runtime.CompilerServices;
using Sudoku.Data;
using Sudoku.DocComments;
using Sudoku.Drawing;
using Sudoku.Models;
using Sudoku.Solving.Manual.Extensions;
using Sudoku.Techniques;
using static System.Numerics.BitOperations;
using static Sudoku.Constants.Tables;
using static Sudoku.Solving.Manual.FastProperties;

namespace Sudoku.Solving.Manual.Exocets
{
	/// <summary>
	/// Encapsulates a <b>senior exocet</b> (SE) technique searcher.
	/// </summary>
	public sealed class SeStepSearcher : ExocetStepSearcher
	{
		/// <inheritdoc cref="SearchingProperties"/>
		public static TechniqueProperties Properties { get; } = new(35, nameof(Technique.Se))
		{
			DisplayLevel = 4
		};


		/// <inheritdoc/>
		public override unsafe void GetAll(IList<StepInfo> accumulator, in SudokuGrid grid)
		{
			// TODO: Extend SE eliminations checking.
			int* compatibleCells = stackalloc int[4], cover = stackalloc int[8];
			foreach (var exocet in Patterns)
			{
				var (b1, b2, tq1, tq2, tr1, tr2, s, mq1, mq2, mr1, mr2, baseMap, targetMap) = exocet;
				if (PopCount((uint)grid.GetCandidates(b1)) < 2 || PopCount((uint)grid.GetCandidates(b2)) < 2)
				{
					continue;
				}

				bool isRow = baseMap.CoveredLine < 18;
				var tempCrosslineMap = s | targetMap;
				short baseCandsMask = (short)(grid.GetCandidates(b1) | grid.GetCandidates(b2));

				int i = 0;
				int r = b1.ToRegion(RegionLabel.Row) - 9, c = b1.ToRegion(RegionLabel.Column) - 18;
				foreach (int pos in SudokuGrid.MaxCandidatesMask & ~(1 << (isRow ? r : c)))
				{
					cover[i++] = isRow ? pos + 9 : pos + 18;
				}

				i = 0;
				Cells temp;
				foreach (int digit in baseCandsMask)
				{
					if (i++ == 0)
					{
						*&temp = DigitMaps[digit];
					}
					else
					{
						*&temp |= DigitMaps[digit];
					}
				}
				*&temp &= tempCrosslineMap;

				var tempTarget = new List<int>();
				for (i = 0; i < 8; i++)
				{
					/*length-pattern*/
					if ((temp & RegionMaps[cover[i]]) is { Count: 1 } check)
					{
						tempTarget.Add(check[0]);
					}
				}
				if (tempTarget.Count == 0)
				{
					continue;
				}

				int bOrT = isRow ? b1 / 9 / 3 : b1 % 9 / 3; // Base or target (B or T).
				foreach (int[] comb in tempTarget.GetSubsets(2))
				{
					[MethodImpl(MethodImplOptions.AggressiveInlining)] static int a(int v) => v / 9 / 3;
					[MethodImpl(MethodImplOptions.AggressiveInlining)] static int b(int v) => v % 9 / 3;

					int v1 = comb[0], v2 = comb[1];
					if (isRow ? a(v1) == bOrT && a(v2) == bOrT : b(v1) == bOrT && b(v2) == bOrT)
					{
						continue;
					}

					int row1 = v1.ToRegion(RegionLabel.Row), column1 = v1.ToRegion(RegionLabel.Column);
					int row2 = v2.ToRegion(RegionLabel.Row), column2 = v2.ToRegion(RegionLabel.Column);
					if (isRow ? column1 == column2 : row1 == row2)
					{
						continue;
					}

					short elimDigits = (short)(
					(
						grid.GetCandidates(v1) | grid.GetCandidates(v2)
					) & ~baseCandsMask);
					if (!CheckCrossline(tempCrosslineMap, baseCandsMask, v1, v2, isRow))
					{
						continue;
					}

					// Get all target eliminations.
					var targetElims = Candidates.Empty;
					short cands = (short)(elimDigits & grid.GetCandidates(v1));
					if (cands != 0)
					{
						foreach (int digit in cands)
						{
							targetElims.AddAnyway(v1 * 9 + digit);
						}
					}
					cands = (short)(elimDigits & grid.GetCandidates(v2));
					if (cands != 0)
					{
						foreach (int digit in cands)
						{
							targetElims.AddAnyway(v2 * 9 + digit);
						}
					}

					short tbCands = 0;
					for (int j = 0; j < 2; j++)
					{
						if (PopCount((uint)grid.GetCandidates(comb[j])) == 1)
						{
							tbCands |= grid.GetCandidates(comb[j]);
						}
					}

					// Get all true base eliminations.
					var trueBaseElims = Candidates.Empty;
					if (tbCands != 0 && (
						grid.GetStatus(v1) != CellStatus.Empty || grid.GetStatus(v2) != CellStatus.Empty))
					{
						for (int j = 0; j < 2; j++)
						{
							if (grid.GetStatus(comb[j]) != CellStatus.Empty)
							{
								continue;
							}

							if ((cands = (short)(grid.GetCandidates(comb[j]) & tbCands)) == 0)
							{
								continue;
							}

							foreach (int digit in cands)
							{
								trueBaseElims.AddAnyway(comb[j] * 9 + digit);
							}
						}
					}

					if (tbCands != 0)
					{
						foreach (int digit in tbCands)
						{
							var elimMap = baseMap % CandMaps[digit];
							foreach (int cell in elimMap)
							{
								trueBaseElims.AddAnyway(cell * 9 + digit);
							}
						}
					}

					if (targetElims.IsEmpty && trueBaseElims.IsEmpty)
					{
						continue;
					}

					// Get mirror and compatibility test eliminations.
					var cellOffsets = new List<DrawingInfo> { new(0, b1), new(0, b2) };
					foreach (int cell in tempCrosslineMap)
					{
						cellOffsets.Add(new(cell == v1 || cell == v2 ? 1 : 2, cell));
					}
					var candidateOffsets = new List<DrawingInfo>();

					int endoTargetCell = comb[s.Contains(v1) ? 0 : 1];
					short m1 = grid.GetCandidates(b1), m2 = grid.GetCandidates(b2), m = (short)(m1 | m2);
					foreach (int digit in m1)
					{
						candidateOffsets.Add(new(0, b1 * 9 + digit));
					}
					foreach (int digit in m2)
					{
						candidateOffsets.Add(new(0, b2 * 9 + digit));
					}

					accumulator.Add(
						new SeStepInfo(
							new View[] { new() { Cells = cellOffsets, Candidates = candidateOffsets } },
							exocet,
							m.GetAllSets().ToArray(),
							endoTargetCell,
							null,
							new Elimination[]
							{
								new(targetElims, EliminatedReason.Basic),
								new(trueBaseElims, EliminatedReason.TrueBase)
							}));
				}
			}
		}

		/// <summary>
		/// Check the cross-line cells.
		/// </summary>
		/// <param name="tempCrossline">The cross-line map.</param>
		/// <param name="baseCandidatesMask">The base candidate mask.</param>
		/// <param name="t1">The target cell 1.</param>
		/// <param name="t2">The target cell 2.</param>
		/// <param name="isRow">Indicates whether the specified computation is for rows.</param>
		/// <returns>The <see cref="bool"/> result.</returns>
		private bool CheckCrossline(in Cells tempCrossline, short baseCandidatesMask, int t1, int t2, bool isRow)
		{
			var xx = new Cells { t1, t2 };
			foreach (int digit in baseCandidatesMask)
			{
				bool flag = true;
				var temp = (tempCrossline & DigitMaps[digit]) - xx;
				if (PopCount((uint)(isRow ? temp.RowMask : temp.ColumnMask)) > 2)
				{
					flag = false;
				}

				//if (flag)
				//{
				//	continue;
				//}

				if (!flag)
				{
					return false;
				}
			}

			return true;
		}
	}
}
