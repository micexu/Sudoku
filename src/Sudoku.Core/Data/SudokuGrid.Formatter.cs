﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Extensions;
using System.Runtime.CompilerServices;
using System.Text;
using Sudoku.Data.Extensions;
using static System.Numerics.BitOperations;

namespace Sudoku.Data
{
	partial struct SudokuGrid
	{
		/// <summary>
		/// Provides operations for grid formatting.
		/// </summary>
		public readonly ref struct Formatter
		{
			/// <summary>
			/// Initializes an instance with a <see cref="bool"/> value
			/// indicating multi-line.
			/// </summary>
			/// <param name="multiline">
			/// The multi-line identifier. If the value is <see langword="true"/>, the output will
			/// be multi-line.
			/// </param>
			public Formatter(bool multiline) : this(
				placeholder: '.',
				multiline: multiline,
				withModifiables: false,
				withCandidates: false,
				treatValueAsGiven: false,
				subtleGridLines: false,
				hodokuCompatible: false,
				sukaku: false,
				excel: false,
				openSudoku: false)
			{
			}

			/// <summary>
			/// Initialize an instance with the specified information.
			/// </summary>
			/// <param name="placeholder">The placeholder.</param>
			/// <param name="multiline">Indicates whether the formatter will use multiple lines mode.</param>
			/// <param name="withModifiables">Indicates whether the formatter will output modifiables.</param>
			/// <param name="withCandidates">
			/// Indicates whether the formatter will output candidates list.
			/// </param>
			/// <param name="treatValueAsGiven">
			/// Indicates whether the formatter will treat values as givens always.
			/// </param>
			/// <param name="subtleGridLines">
			/// Indicates whether the formatter will process outline corner of the multiline grid.
			/// </param>
			/// <param name="hodokuCompatible">
			/// Indicates whether the formatter will use hodoku library mode to output.
			/// </param>
			/// <param name="sukaku">Indicates whether the formatter will output as sukaku.</param>
			/// <param name="excel">Indicates whether the formatter will output as excel.</param>
			/// <param name="openSudoku">
			/// Indicates whether the formatter will output as open sudoku format.
			/// </param>
			private Formatter(
				char placeholder, bool multiline, bool withModifiables, bool withCandidates,
				bool treatValueAsGiven, bool subtleGridLines, bool hodokuCompatible, bool sukaku,
				bool excel, bool openSudoku)
			{
				Placeholder = placeholder;
				Multiline = multiline;
				WithModifiables = withModifiables;
				WithCandidates = withCandidates;
				TreatValueAsGiven = treatValueAsGiven;
				SubtleGridLines = subtleGridLines;
				HodokuCompatible = hodokuCompatible;
				Sukaku = sukaku;
				Excel = excel;
				OpenSudoku = openSudoku;
			}


			/// <summary>
			/// The place holder.
			/// </summary>
			public char Placeholder { get; init; }

			/// <summary>
			/// Indicates whether the output should be multi-line.
			/// </summary>
			public bool Multiline { get; }

			/// <summary>
			/// Indicates the output should be with modifiable values.
			/// </summary>
			public bool WithModifiables { get; init; }

			/// <summary>
			/// <para>
			/// Indicates the output should be with candidates.
			/// If the output is single line, the candidates will indicate
			/// the candidates-have-eliminated before the current grid status;
			/// if the output is multi-line, the candidates will indicate
			/// the real candidate at the current grid status.
			/// </para>
			/// <para>
			/// If the output is single line, the output will append the candidates
			/// value at the tail of the string in '<c>:candidate list</c>'. In addition,
			/// candidates will be represented as 'digit', 'row offset' and
			/// 'column offset' in order.
			/// </para>
			/// </summary>
			public bool WithCandidates { get; init; }

			/// <summary>
			/// Indicates the output will treat modifiable values as given ones.
			/// If the output is single line, the output will remove all plus marks '+'.
			/// If the output is multi-line, the output will use '<c>&lt;digit&gt;</c>' instead
			/// of '<c>*digit*</c>'.
			/// </summary>
			public bool TreatValueAsGiven { get; init; }

			/// <summary>
			/// Indicates whether need to handle all grid outlines while outputting.
			/// See <a href="https://github.com/Sunnie-Shine/Sudoku/wiki/Grid-Format-String">this link</a>
			/// for more information.
			/// </summary>
			public bool SubtleGridLines { get; init; }

			/// <summary>
			/// Indicates whether the output will be compatible with Hodoku library format.
			/// </summary>
			public bool HodokuCompatible { get; init; }

			/// <summary>
			/// Indicates the output will be sukaku format (all single-valued digit will
			/// be all treated as candidates).
			/// </summary>
			public bool Sukaku { get; init; }

			/// <summary>
			/// Indicates the output will be Excel format.
			/// </summary>
			public bool Excel { get; init; }

			/// <summary>
			/// Indicates whether the current output mode is aiming to open sudoku format.
			/// </summary>
			public bool OpenSudoku { get; init; }


			/// <summary>
			/// Represents a string value indicating this instance.
			/// </summary>
			/// <param name="grid">The grid.</param>
			/// <returns>The string.</returns>
			public string ToString(in SudokuGrid grid) => Sukaku
				? ToSukakuString(grid)
				: Multiline
					? WithCandidates
						? ToMultiLineStringCore(grid)
						: Excel
							? ToExcelString(grid)
							: ToMultiLineSimpleGridCore(grid)
					: HodokuCompatible
						? ToHodokuLibraryFormatString(grid)
						: OpenSudoku
							? ToOpenSudokuString(grid)
							: ToSingleLineStringCore(grid);

			/// <summary>
			/// Represents a string value indicating this instance, with the specified format string.
			/// </summary>
			/// <param name="grid">The grid.</param>
			/// <param name="format">The string format.</param>
			/// <returns>The string.</returns>
			public string ToString(in SudokuGrid grid, string? format) => Create(format).ToString(grid);

			/// <summary>
			/// To Excel format string.
			/// </summary>
			/// <param name="grid">The grid.</param>
			/// <returns>The string.</returns>
			private string ToExcelString(in SudokuGrid grid)
			{
				var span = grid.ToString("0").AsSpan();
				var sb = new ValueStringBuilder(stackalloc char[81 + 72 + 9]);
				for (int i = 0; i < 9; i++)
				{
					for (int j = 0; j < 9; j++)
					{
						if (span[i * 9 + j] - '0' is var digit and not 0)
						{
							sb.Append(digit);
						}

						sb.Append('\t');
					}

					sb.RemoveFromEnd(1);
					sb.AppendLine();
				}

				return sb.ToString();
			}

			/// <summary>
			/// To open sudoku format string.
			/// </summary>
			/// <param name="grid">The grid.</param>
			/// <returns>The string.</returns>
			/// <exception cref="InvalidPuzzleException">Throws when the specified grid is invalid.</exception>
			private unsafe string ToOpenSudokuString(in SudokuGrid grid)
			{
				// Calculates the length of the result string.
				const int length = 1 + (81 * 3 - 1 << 1);

				// Creates a string instance as a buffer.
				string result = new('\0', length);

				// Modify the string value via pointers.
				fixed (char* pResult = result)
				{
					// Replace the base character with the separator.
					for (int pos = 1; pos < length; pos += 2)
					{
						pResult[pos] = '|';
					}

					// Now replace some positions with the specified values.
					for (int i = 0, pos = 0; i < 81; i++, pos += 6)
					{
						switch (grid.GetStatus(i))
						{
							case CellStatus.Empty:
							{
								pResult[pos] = '0';
								pResult[pos + 2] = '0';
								pResult[pos + 4] = '1';

								break;
							}
							case CellStatus.Modifiable:
							case CellStatus.Given:
							{
								pResult[pos] = (char)(grid[i] + '1');
								pResult[pos + 2] = '0';
								pResult[pos + 4] = '0';

								break;
							}
							default:
							{
								throw new InvalidPuzzleException(
									grid,
									"The specified grid is invalid to output the formatted string."
								);
							}
						}
					}
				}

				// Returns the result.
				return result;
			}

			/// <summary>
			/// To string with Hodoku library format compatible string.
			/// </summary>
			/// <param name="grid">The grid.</param>
			/// <returns>The string.</returns>
			private string ToHodokuLibraryFormatString(in SudokuGrid grid) =>
				$":0000:x:{ToSingleLineStringCore(grid)}:::";

			/// <summary>
			/// To string with the sukaku format.
			/// </summary>
			/// <param name="grid">The grid.</param>
			/// <returns>The string.</returns>
			/// <exception cref="ArgumentException">
			/// Throws when the puzzle is an invalid sukaku puzzle (at least one cell is given or modifiable).
			/// </exception>
			private string ToSukakuString(in SudokuGrid grid)
			{
				if (Multiline)
				{
					// Append all digits.
					var builders = new StringBuilder[81];
					for (int i = 0; i < 81; i++)
					{
						builders[i] = new();
						foreach (int digit in grid.GetCandidates(i))
						{
							builders[i].Append(digit + 1);
						}
					}

					// Now consider the alignment for each column of output text.
					var sb = new StringBuilder();
					var span = (stackalloc int[9]);
					for (int column = 0; column < 9; column++)
					{
						int maxLength = 0;
						for (int p = 0; p < 9; p++)
						{
							maxLength = Math.Max(maxLength, builders[p * 9 + column].Length);
						}

						span[column] = maxLength;
					}
					for (int row = 0; row < 9; row++)
					{
						for (int column = 0; column < 9; column++)
						{
							int cell = row * 9 + column;
							sb.Append(builders[cell].ToString().PadLeft(span[column])).Append(' ');
						}
						sb.RemoveFromEnd(1).AppendLine(); // Remove last whitespace.
					}

					return sb.ToString();
				}
				else
				{
					var sb = new StringBuilder();
					for (int i = 0; i < 81; i++)
					{
						sb.Append("123456789");
					}

					for (int i = 0; i < 729; i++)
					{
						if (!grid[i / 9, i % 9])
						{
							sb[i] = Placeholder;
						}
					}

					return sb.ToString();
				}
			}

			/// <summary>
			/// To single line string.
			/// </summary>
			/// <param name="grid">The grid.</param>
			/// <returns>The result.</returns>
			private unsafe string ToSingleLineStringCore(in SudokuGrid grid)
			{
				var sb = new ValueStringBuilder(stackalloc char[162]);
				var elims = new StringBuilder();
				var originalGrid = WithCandidates ? Parse(grid.ToString(".+")) : Undefined;

				for (int c = 0; c < 81; c++)
				{
					var status = grid.GetStatus(c);
					if (status == CellStatus.Empty && !originalGrid.IsUndefined && WithCandidates)
					{
						// Check if the value has been set 'true'
						// and the value has already deleted at the grid
						// with only givens and modifiables.
						foreach (int i in originalGrid._values[c] & MaxCandidatesMask)
						{
							if (!grid[c, i])
							{
								// The value is 'true', which means the digit has already been deleted.
								elims.Append(i + 1).Append(c / 9 + 1).Append(c % 9 + 1).Append(' ');
							}
						}
					}

					sb.Append(status switch
					{
						CellStatus.Empty => Placeholder.ToString(),
						CellStatus.Modifiable =>
							WithModifiables ? $"+{(grid[c] + 1).ToString()}" : $"{Placeholder.ToString()}",
						CellStatus.Given => $"{(grid[c] + 1).ToString()}"
					});
				}

				string elimsStr = elims.Length <= 3 ? elims.ToString() : elims.RemoveFromEnd(1).ToString();
				return $"{sb.ToString()}{(string.IsNullOrEmpty(elimsStr) ? string.Empty : $":{elimsStr}")}";
			}

			/// <summary>
			/// To multi-line string with candidates.
			/// </summary>
			/// <param name="grid">The grid.</param>
			/// <returns>The result.</returns>
			private unsafe string ToMultiLineStringCore(in SudokuGrid grid)
			{
				// Step 1: gets the candidates information grouped by columns.
				Dictionary<int, IList<short>> valuesByColumn = new()
				{
					[0] = new List<short>(),
					[1] = new List<short>(),
					[2] = new List<short>(),
					[3] = new List<short>(),
					[4] = new List<short>(),
					[5] = new List<short>(),
					[6] = new List<short>(),
					[7] = new List<short>(),
					[8] = new List<short>(),
				}, valuesByRow = new()
				{
					[0] = new List<short>(),
					[1] = new List<short>(),
					[2] = new List<short>(),
					[3] = new List<short>(),
					[4] = new List<short>(),
					[5] = new List<short>(),
					[6] = new List<short>(),
					[7] = new List<short>(),
					[8] = new List<short>(),
				};

				for (int i = 0; i < 81; i++)
				{
					short value = grid.GetMask(i);
					valuesByRow[i / 9].Add(value);
					valuesByColumn[i % 9].Add(value);
				}

				// Step 2: gets the maximal number of candidates in a cell,
				// which is used for aligning by columns.
				const int bufferLength = 9;
				int* maxLengths = stackalloc int[bufferLength];
				Unsafe.InitBlock(maxLengths, 0, sizeof(int) * bufferLength);

				foreach (var (i, _) in valuesByColumn)
				{
					int* maxLength = maxLengths + i;

					// Iteration on row index.
					for (int j = 0; j < 9; j++)
					{
						// Gets the number of candidates.
						int candidatesCount = 0;
						short value = valuesByColumn[i][j];

						// Iteration on each candidate.
						// Counts the number of candidates.
						candidatesCount += PopCount((uint)value);

						// Compares the values.
						if (
							Math.Max(candidatesCount, value.MaskToStatus() switch
							{
								// The output will be '<digit>' and consist of 3 characters.
								CellStatus.Given => Math.Max(candidatesCount, 3),
								// The output will be '*digit*' and consist of 3 characters.
								CellStatus.Modifiable => Math.Max(candidatesCount, 3),
								// Normal output: 'series' (at least 1 character).
								_ => candidatesCount,
							}) is var comparer && comparer > *maxLength
						)
						{
							*maxLength = comparer;
						}
					}
				}

				// Step 3: outputs all characters.
				var sb = new StringBuilder();
				for (int i = 0; i < 13; i++)
				{
					switch (i)
					{
						case 0: // Print tabs of the first line.
						{
							if (SubtleGridLines) printTabLines('.', '.', '-', maxLengths);
							else printTabLines('+', '+', '-', maxLengths);
							break;
						}
						case 4:
						case 8: // Print tabs of mediate lines.
						{
							if (SubtleGridLines) printTabLines(':', '+', '-', maxLengths);
							else printTabLines('+', '+', '-', maxLengths);
							break;
						}
						case 12: // Print tabs of the foot line.
						{
							if (SubtleGridLines) printTabLines('\'', '\'', '-', maxLengths);
							else printTabLines('+', '+', '-', maxLengths);
							break;
						}
						default: // Print values and tabs.
						{
							p(
								this, valuesByRow[i switch
								{
									1 or 2 or 3 or 4 => i - 1,
									5 or 6 or 7 or 8 => i - 2,
									9 or 10 or 11 or 12 => i - 3
								}], '|', '|', maxLengths
							);

							break;

							void p(
								in Formatter formatter, IList<short> valuesByRow, char c1, char c2,
								int* maxLengths)
							{
								sb.Append(c1);
								printValues(formatter, valuesByRow, 0, 2, maxLengths);
								sb.Append(c2);
								printValues(formatter, valuesByRow, 3, 5, maxLengths);
								sb.Append(c2);
								printValues(formatter, valuesByRow, 6, 8, maxLengths);
								sb.AppendLine(c1);

								void printValues(
									in Formatter formatter, IList<short> valuesByRow,
									int start, int end, int* maxLengths)
								{
									sb.Append(' ');
									for (int i = start; i <= end; i++)
									{
										// Get digit.
										short value = valuesByRow[i];
										var status = value.MaskToStatus();

										value &= MaxCandidatesMask;
										int d = value == 0
											? -1
											: (status != CellStatus.Empty ? TrailingZeroCount(value) : -1) + 1;
										string s;
										switch (status)
										{
											case CellStatus.Given:
											case CellStatus.Modifiable when formatter.TreatValueAsGiven:
											{
												s = $"<{d.ToString()}>";
												break;
											}
											case CellStatus.Modifiable:
											{
												s = $"*{d.ToString()}*";
												break;
											}
											default:
											{
												var innerSb = new ValueStringBuilder(stackalloc char[9]);
												foreach (int z in value)
												{
													innerSb.Append(z + 1);
												}

												s = innerSb.ToString();

												break;
											}
										}

										sb.Append(s.PadRight(maxLengths[i])).Append(i != end ? "  " : " ");
									}
								}
							}
						}
					}
				}

				// The last step: returns the value.
				return sb.ToString();

				void printTabLines(char c1, char c2, char fillingChar, int* m) => sb
					.Append(c1)
					.Append(string.Empty.PadRight(m[0] + m[1] + m[2] + 6, fillingChar))
					.Append(c2)
					.Append(string.Empty.PadRight(m[3] + m[4] + m[5] + 6, fillingChar))
					.Append(c2)
					.Append(string.Empty.PadRight(m[6] + m[7] + m[8] + 6, fillingChar))
					.AppendLine(c1);
			}

			/// <summary>
			/// To multi-line normal grid string without any candidates.
			/// </summary>
			/// <param name="grid">The grid.</param>
			/// <returns>The result.</returns>
			private string ToMultiLineSimpleGridCore(in SudokuGrid grid)
			{
				string t = grid.ToString(TreatValueAsGiven ? $"{Placeholder.ToString()}!" : Placeholder.ToString());
				return new StringBuilder()
					.AppendLine(SubtleGridLines ? ".-------+-------+-------." : "+-------+-------+-------+")
					.Append("| ").Append(t[0]).Append(' ').Append(t[1]).Append(' ').Append(t[2])
					.Append(" | ").Append(t[3]).Append(' ').Append(t[4]).Append(' ').Append(t[5])
					.Append(" | ").Append(t[6]).Append(' ').Append(t[7]).Append(' ').Append(t[8])
					.AppendLine(" |")
					.Append("| ").Append(t[9]).Append(' ').Append(t[10]).Append(' ').Append(t[11])
					.Append(" | ").Append(t[12]).Append(' ').Append(t[13]).Append(' ').Append(t[14])
					.Append(" | ").Append(t[15]).Append(' ').Append(t[16]).Append(' ').Append(t[17])
					.AppendLine(" |")
					.Append("| ").Append(t[18]).Append(' ').Append(t[19]).Append(' ').Append(t[20])
					.Append(" | ").Append(t[21]).Append(' ').Append(t[22]).Append(' ').Append(t[23])
					.Append(" | ").Append(t[24]).Append(' ').Append(t[25]).Append(' ').Append(t[26])
					.AppendLine(" |")
					.AppendLine(SubtleGridLines ? ":-------+-------+-------:" : "+-------+-------+-------+")
					.Append("| ").Append(t[27]).Append(' ').Append(t[28]).Append(' ').Append(t[29])
					.Append(" | ").Append(t[30]).Append(' ').Append(t[31]).Append(' ').Append(t[32])
					.Append(" | ").Append(t[33]).Append(' ').Append(t[34]).Append(' ').Append(t[35])
					.AppendLine(" |")
					.Append("| ").Append(t[36]).Append(' ').Append(t[37]).Append(' ').Append(t[38])
					.Append(" | ").Append(t[39]).Append(' ').Append(t[40]).Append(' ').Append(t[41])
					.Append(" | ").Append(t[42]).Append(' ').Append(t[43]).Append(' ').Append(t[44])
					.AppendLine(" |")
					.Append("| ").Append(t[45]).Append(' ').Append(t[46]).Append(' ').Append(t[47])
					.Append(" | ").Append(t[48]).Append(' ').Append(t[49]).Append(' ').Append(t[50])
					.Append(" | ").Append(t[51]).Append(' ').Append(t[52]).Append(' ').Append(t[53])
					.AppendLine(" |")
					.AppendLine(SubtleGridLines ? ":-------+-------+-------:" : "+-------+-------+-------+")
					.Append("| ").Append(t[54]).Append(' ').Append(t[55]).Append(' ').Append(t[56])
					.Append(" | ").Append(t[57]).Append(' ').Append(t[58]).Append(' ').Append(t[59])
					.Append(" | ").Append(t[60]).Append(' ').Append(t[61]).Append(' ').Append(t[62])
					.AppendLine(" |")
					.Append("| ").Append(t[63]).Append(' ').Append(t[64]).Append(' ').Append(t[65])
					.Append(" | ").Append(t[66]).Append(' ').Append(t[67]).Append(' ').Append(t[68])
					.Append(" | ").Append(t[69]).Append(' ').Append(t[70]).Append(' ').Append(t[71])
					.AppendLine(" |")
					.Append("| ").Append(t[72]).Append(' ').Append(t[73]).Append(' ').Append(t[74])
					.Append(" | ").Append(t[75]).Append(' ').Append(t[76]).Append(' ').Append(t[77])
					.Append(" | ").Append(t[78]).Append(' ').Append(t[79]).Append(' ').Append(t[80])
					.AppendLine(" |")
					.AppendLine(SubtleGridLines ? "'-------+-------+-------'" : "+-------+-------+-------+")
					.ToString();
			}


			/// <summary>
			/// Create a <see cref="Formatter"/> according to the specified grid output options.
			/// </summary>
			/// <param name="gridOutputOption">The grid output options.</param>
			/// <returns>The grid formatter.</returns>
			public static Formatter Create(GridFormattingOptions gridOutputOption) => gridOutputOption switch
			{
				GridFormattingOptions.Excel => new(true) { Excel = true },
				GridFormattingOptions.OpenSudoku => new(false) { OpenSudoku = true },
				_ => new(gridOutputOption.Flags(GridFormattingOptions.Multiline))
				{
					WithModifiables = gridOutputOption.Flags(GridFormattingOptions.WithModifiers),
					WithCandidates = gridOutputOption.Flags(GridFormattingOptions.WithCandidates),
					TreatValueAsGiven = gridOutputOption.Flags(GridFormattingOptions.TreatValueAsGiven),
					SubtleGridLines = gridOutputOption.Flags(GridFormattingOptions.SubtleGridLines),
					HodokuCompatible = gridOutputOption.Flags(GridFormattingOptions.HodokuCompatible),
					Sukaku = gridOutputOption == GridFormattingOptions.Sukaku,
					Placeholder = gridOutputOption.Flags(GridFormattingOptions.DotPlaceholder) ? '.' : '0'
				}
			};

			/// <summary>
			/// Create a <see cref="Formatter"/> according to the specified format.
			/// </summary>
			/// <param name="format">The format.</param>
			/// <returns>The grid formatter.</returns>
			/// <exception cref="FormatException">Throws when the format string is invalid.</exception>
			public static Formatter Create(string? format) => format switch
			{
				null or "." => new(false),
				"+" or ".+" or "+." => new(false) { WithModifiables = true },
				"0" => new(false) { Placeholder = '0' },
				":" => new(false) { WithCandidates = true },
				"!" or ".!" or "!." => new(false) { WithModifiables = true, TreatValueAsGiven = true },
				"0!" or "!0" => new(false) { Placeholder = '0', WithModifiables = true, TreatValueAsGiven = true },
				".:" => new(false) { WithCandidates = true },
				"0:" => new(false) { Placeholder = '0', WithCandidates = true },
				"0+" or "+0" => new(false) { Placeholder = '0', WithModifiables = true },
				"+:" or "+.:" or ".+:" or "#" or "#." => new(false) { WithModifiables = true, WithCandidates = true },
				"0+:" or "+0:" or "#0" => new(false) { Placeholder = '0', WithModifiables = true, WithCandidates = true },
				".!:" or "!.:" => new(false) { WithModifiables = true, TreatValueAsGiven = true },
				"0!:" or "!0:" => new(false) { Placeholder = '0', WithModifiables = true, TreatValueAsGiven = true },
				"@" or "@." => new(true) { SubtleGridLines = true },
				"@0" => new(true) { Placeholder = '0', SubtleGridLines = true },
				"@!" or "@.!" or "@!." => new(true) { TreatValueAsGiven = true, SubtleGridLines = true },
				"@0!" or "@!0" => new(true) { Placeholder = '0', TreatValueAsGiven = true, SubtleGridLines = true },
				"@*" or "@.*" or "@*." => new(true),
				"@0*" or "@*0" => new(true) { Placeholder = '0' },
				"@!*" or "@*!" => new(true) { TreatValueAsGiven = true },
				"@:" => new(true) { WithCandidates = true, SubtleGridLines = true },
				"@:!" or "@!:" => new(true) { WithCandidates = true, TreatValueAsGiven = true, SubtleGridLines = true },
				"@*:" or "@:*" => new(true) { WithCandidates = true },
				"@!*:" or "@*!:" or "@!:*" or "@*:!" or "@:!*" or "@:*!" => new(true) { WithCandidates = true, TreatValueAsGiven = true },
				"~" or "~0" => new(false) { Sukaku = true, Placeholder = '0' },
				"~." => new(false) { Sukaku = true },
				"@~" or "~@" => new(true) { Sukaku = true },
				"@~0" or "@0~" or "~@0" or "~0@" => new(true) { Sukaku = true, Placeholder = '0' },
				"@~." or "@.~" or "~@." or "~.@" => new(true) { Sukaku = true },
				"%" => new(true) { Excel = true },
				"^" => new(false) { OpenSudoku = true },
				_ => throw new FormatException("The specified format is invalid.")
			};

			#region Default overrides
#pragma warning disable CS1591
#pragma warning disable CS0809
			[CompilerGenerated]
			[DoesNotReturn]
			[EditorBrowsable(EditorBrowsableState.Never)]
			[Obsolete("You can't use or call this method.", true, DiagnosticId = "BAN")]
			public override readonly bool Equals(object? other) => throw new NotSupportedException();

			[CompilerGenerated]
			[DoesNotReturn]
			[EditorBrowsable(EditorBrowsableState.Never)]
			[Obsolete("You can't use or call this method.", true, DiagnosticId = "BAN")]
			public override readonly int GetHashCode() => throw new NotSupportedException();

			[CompilerGenerated]
			[DoesNotReturn]
			[EditorBrowsable(EditorBrowsableState.Never)]
			[Obsolete("You can't use or call this method.", true, DiagnosticId = "BAN")]
			public override readonly string? ToString() => throw new NotSupportedException();
#pragma warning restore CS0809
#pragma warning restore CS1591
			#endregion
		}
	}
}
