﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Extensions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Sudoku.CodeGen;
using Sudoku.DocComments;
using static System.Numerics.BitOperations;
using static Sudoku.Constants;
using static Sudoku.Constants.Tables;

namespace Sudoku.Data
{
	/// <summary>
	/// Encapsulates a binary series of cell status table.
	/// </summary>
	/// <remarks>
	/// The instance stores two <see cref="long"/> values, consisting of 81 bits,
	/// where <see langword="true"/> bit (1) is for the cell having that digit,
	/// and the <see langword="false"/> bit (0) is for the cell not containing
	/// the digit.
	/// </remarks>
	[AutoDeconstruct(nameof(_high), nameof(_low))]
	[AutoEquality(nameof(_high), nameof(_low))]
	[AutoGetEnumerator(nameof(Offsets), MemberConversion = "((IEnumerable<int>)@).GetEnumerator()")]
	public partial struct Cells : IEnumerable<int>, IValueEquatable<Cells>, IFormattable
	{
		/// <summary>
		/// The cover table.
		/// </summary>
		private static readonly long[,] CoverTable =
		{
			{ 0b000000000_000000000_000000000_000000000_0000, 0b00000_000000000_000000111_000000111_000000111 },
			{ 0b000000000_000000000_000000000_000000000_0000, 0b00000_000000000_000111000_000111000_000111000 },
			{ 0b000000000_000000000_000000000_000000000_0000, 0b00000_000000000_111000000_111000000_111000000 },
			{ 0b000000000_000000000_000000000_000000111_0000, 0b00111_000000111_000000000_000000000_000000000 },
			{ 0b000000000_000000000_000000000_000111000_0001, 0b11000_000111000_000000000_000000000_000000000 },
			{ 0b000000000_000000000_000000000_111000000_1110, 0b00000_111000000_000000000_000000000_000000000 },
			{ 0b000000111_000000111_000000111_000000000_0000, 0b00000_000000000_000000000_000000000_000000000 },
			{ 0b000111000_000111000_000111000_000000000_0000, 0b00000_000000000_000000000_000000000_000000000 },
			{ 0b111000000_111000000_111000000_000000000_0000, 0b00000_000000000_000000000_000000000_000000000 },
			{ 0b000000000_000000000_000000000_000000000_0000, 0b00000_000000000_000000000_000000000_111111111 },
			{ 0b000000000_000000000_000000000_000000000_0000, 0b00000_000000000_000000000_111111111_000000000 },
			{ 0b000000000_000000000_000000000_000000000_0000, 0b00000_000000000_111111111_000000000_000000000 },
			{ 0b000000000_000000000_000000000_000000000_0000, 0b00000_111111111_000000000_000000000_000000000 },
			{ 0b000000000_000000000_000000000_000000000_1111, 0b11111_000000000_000000000_000000000_000000000 },
			{ 0b000000000_000000000_000000000_111111111_0000, 0b00000_000000000_000000000_000000000_000000000 },
			{ 0b000000000_000000000_111111111_000000000_0000, 0b00000_000000000_000000000_000000000_000000000 },
			{ 0b000000000_111111111_000000000_000000000_0000, 0b00000_000000000_000000000_000000000_000000000 },
			{ 0b111111111_000000000_000000000_000000000_0000, 0b00000_000000000_000000000_000000000_000000000 },
			{ 0b000000001_000000001_000000001_000000001_0000, 0b00001_000000001_000000001_000000001_000000001 },
			{ 0b000000010_000000010_000000010_000000010_0000, 0b00010_000000010_000000010_000000010_000000010 },
			{ 0b000000100_000000100_000000100_000000100_0000, 0b00100_000000100_000000100_000000100_000000100 },
			{ 0b000001000_000001000_000001000_000001000_0000, 0b01000_000001000_000001000_000001000_000001000 },
			{ 0b000010000_000010000_000010000_000010000_0000, 0b10000_000010000_000010000_000010000_000010000 },
			{ 0b000100000_000100000_000100000_000100000_0001, 0b00000_000100000_000100000_000100000_000100000 },
			{ 0b001000000_001000000_001000000_001000000_0010, 0b00000_001000000_001000000_001000000_001000000 },
			{ 0b010000000_010000000_010000000_010000000_0100, 0b00000_010000000_010000000_010000000_010000000 },
			{ 0b100000000_100000000_100000000_100000000_1000, 0b00000_100000000_100000000_100000000_100000000 },
		};

		/// <summary>
		/// <para>Indicates an empty instance (all bits are 0).</para>
		/// <para>
		/// I strongly recommend you <b>should</b> use this instance instead of default constructor
		/// <see cref="Cells()"/> and <see langword="default"/>(<see cref="Cells"/>).
		/// </para>
		/// </summary>
		/// <seealso cref="Cells()"/>
		public static readonly Cells Empty;


		/// <summary>
		/// The value used for shifting.
		/// </summary>
		private const int Shifting = 41;

		/// <summary>
		/// The value of offsets.
		/// </summary>
		private const int BlockOffset = 0, RowOffset = 9, ColumnOffset = 18, Limit = 27;


		/// <summary>
		/// Indicates the internal two <see cref="long"/> values,
		/// which represents 81 bits. <see cref="_high"/> represent the higher
		/// 40 bits and <see cref="_low"/> represents the lower 41 bits.
		/// </summary>
		private long _high, _low;


		/// <summary>
		/// Initializes an instance with the specified cell offset
		/// (Sets itself and all peers).
		/// </summary>
		/// <param name="cell">The cell offset.</param>
		public Cells(int cell) : this(cell, true)
		{
		}

		/// <summary>
		/// Initializes an instance with the candidate list specified as a pointer.
		/// </summary>
		/// <param name="cells">The pointer points to an array of elements.</param>
		/// <param name="length">The length of the array.</param>
		public unsafe Cells(int* cells, int length) : this()
		{
			for (int i = 0; i < length; i++)
			{
				InternalAdd(cells[i], true);
			}
		}

		/// <summary>
		/// Same behavior of the constructor as <see cref="Cells(IEnumerable{int})"/>:
		/// Initializes an instance with the specified array of cells.
		/// </summary>
		/// <param name="cells">All cells.</param>
		/// <remarks>
		/// This constructor is defined after another constructor with
		/// <see cref="ReadOnlySpan{T}"/> had defined. Although this constructor
		/// doesn't initialize something (use the other one instead),
		/// while initializing with the type <see cref="int"/>[], the compiler
		/// gives me an error without this constructor (ambiguity of two
		/// constructors). However, unfortunately, <see cref="ReadOnlySpan{T}"/>
		/// doesn't implemented the interface <see cref="IEnumerable{T}"/>.
		/// </remarks>
		/// <seealso cref="Cells(IEnumerable{int})"/>
		public unsafe Cells(int[] cells) : this()
		{
			fixed (int* ptr = cells)
			{
				int i = 0;
				for (int* p = ptr; i < cells.Length; i++, p++)
				{
					InternalAdd(*p, true);
				}
			}
		}

		/// <summary>
		/// Initializes an instance with the specified instance.
		/// </summary>
		/// <param name="another">Another instance.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Cells(in Cells another) => this = another;

		/// <summary>
		/// Initializes an instance with a series of cell offsets.
		/// </summary>
		/// <param name="cells">cell offsets.</param>
		/// <remarks>
		/// <para>
		/// Note that all offsets will be set <see langword="true"/>, but their own peers
		/// won't be set <see langword="true"/>.
		/// </para>
		/// <para>
		/// In some case, you can use object initializer instead.
		/// You can use the code
		/// <code>
		/// var map = new Cells { 0, 3, 5 };
		/// </code>
		/// instead of the code
		/// <code>
		/// var map = new Cells(stackalloc[] { 0, 3, 5 });
		/// </code>
		/// </para>
		/// </remarks>
		public Cells(in Span<int> cells) : this()
		{
			foreach (int offset in cells)
			{
				InternalAdd(offset, true);
			}
		}

		/// <summary>
		/// Initializes an instance with a series of cell offsets.
		/// </summary>
		/// <param name="cells">cell offsets.</param>
		/// <remarks>
		/// <para>
		/// Note that all offsets will be set <see langword="true"/>, but their own peers
		/// won't be set <see langword="true"/>.
		/// </para>
		/// <para>
		/// In some case, you can use object initializer instead.
		/// You can use the code
		/// <code>
		/// var map = new Cells { 0, 3, 5 };
		/// </code>
		/// instead of the code
		/// <code>
		/// var map = new Cells(stackalloc[] { 0, 3, 5 });
		/// </code>
		/// </para>
		/// </remarks>
		public Cells(in ReadOnlySpan<int> cells) : this()
		{
			foreach (int offset in cells)
			{
				InternalAdd(offset, true);
			}
		}

		/// <summary>
		/// Initializes an instance with a series of cell offsets.
		/// </summary>
		/// <param name="cells">cell offsets.</param>
		/// <remarks>
		/// Note that all offsets will be set <see langword="true"/>, but their own peers
		/// won't be set <see langword="true"/>.
		/// </remarks>
		public Cells(IEnumerable<int> cells) : this()
		{
			foreach (int offset in cells)
			{
				InternalAdd(offset, true);
			}
		}

		/// <summary>
		/// Initializes an instance with two binary values.
		/// </summary>
		/// <param name="high">Higher 40 bits.</param>
		/// <param name="low">Lower 41 bits.</param>
		public Cells(long high, long low)
		{
			_high = high;
			_low = low;
			Count = PopCount((ulong)_high) + PopCount((ulong)_low);
		}

		/// <summary>
		/// Initializes an instance with three binary values.
		/// </summary>
		/// <param name="high">Higher 27 bits.</param>
		/// <param name="mid">Medium 27 bits.</param>
		/// <param name="low">Lower 27 bits.</param>
		public Cells(int high, int mid, int low) : this(
			(high & 0x7FFFFFFL) << 13 | (mid >> 14 & 0x1FFFL),
			(mid & 0x3FFFL) << 27 | (low & 0x7FFFFFFL))
		{
		}

		/// <summary>
		/// Initializes an instance with the specified cell offset.
		/// This will set all bits of all peers of this cell. Another
		/// <see cref="bool"/> value indicates whether this initialization
		/// will set the bit of itself.
		/// </summary>
		/// <param name="cell">The cell offset.</param>
		/// <param name="setItself">
		/// A <see cref="bool"/> value indicating whether this initialization
		/// will set the bit of itself.
		/// </param>
		/// <remarks>
		/// If you want to use this constructor, please use <see cref="PeerMaps"/> instead.
		/// </remarks>
		/// <seealso cref="PeerMaps"/>
		private Cells(int cell, bool setItself)
		{
			// Don't merge those two to one.
			//(this = PeerMaps[cell]).InternalAdd(cell, setItself);
			this = PeerMaps[cell];
			InternalAdd(cell, setItself);
		}


		/// <summary>
		/// Indicates whether the map has no set bits.
		/// </summary>
		public readonly bool IsEmpty
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Count == 0;
		}

		/// <summary>
		/// Same as <see cref="AllSetsAreInOneRegion(out int)"/>, but only contains
		/// the <see cref="bool"/> result.
		/// </summary>
		/// <seealso cref="AllSetsAreInOneRegion(out int)"/>
		public readonly bool InOneRegion
		{
			get
			{
				for (int i = BlockOffset; i < Limit; i++)
				{
					if ((_high & ~CoverTable[i, 0]) == 0 && (_low & ~CoverTable[i, 1]) == 0)
					{
						return true;
					}
				}

				return false;
			}
		}

		/// <summary>
		/// Indicates the mask of block that all cells in this collection spanned.
		/// </summary>
		/// <remarks>
		/// For example, if the cells are { 0, 1, 27, 28 }, all spanned blocks are 0 and 3, so the return
		/// mask is <c>0b000001001</c> (i.e. 9).
		/// </remarks>
		public readonly short BlockMask
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => CreateMask(BlockOffset, RowOffset);
		}

		/// <summary>
		/// Indicates the mask of row that all cells in this collection spanned.
		/// </summary>
		/// <remarks>
		/// For example, if the cells are { 0, 1, 27, 28 }, all spanned rows are 0 and 3, so the return
		/// mask is <c>0b000001001</c> (i.e. 9).
		/// </remarks>
		public readonly short RowMask
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => CreateMask(RowOffset, ColumnOffset);
		}

		/// <summary>
		/// Indicates the mask of column that all cells in this collection spanned.
		/// </summary>
		/// <remarks>
		/// For example, if the cells are { 0, 1, 27, 28 }, all spanned columns are 0 and 1, so the return
		/// mask is <c>0b000000011</c> (i.e. 3).
		/// </remarks>
		public readonly short ColumnMask
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => CreateMask(ColumnOffset, Limit);
		}

		/// <summary>
		/// Indicates the covered line.
		/// </summary>
		/// <remarks>
		/// When the covered region can't be found, it'll return <see cref="InvalidFirstSet"/>
		/// (i.e. 32) always.
		/// </remarks>
		/// <seealso cref="InvalidFirstSet"/>
		public readonly int CoveredLine
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => TrailingZeroCount(CoveredRegions & ~511);
		}

		/// <summary>
		/// Indicates the total number of cells where the corresponding
		/// value are set <see langword="true"/>.
		/// </summary>
		public int Count { get; private set; }

		/// <summary>
		/// Indicates all regions covered. This property is used to check all regions that all cells
		/// of this instance covered. For example, if the cells are { 0, 1 }, the property
		/// <see cref="CoveredRegions"/> will return the region 0 (block 1) and region 9 (row 1);
		/// however, if cells spanned two regions or more (e.g. cells { 0, 1, 27 }), this property won't contain
		/// any regions.
		/// </summary>
		/// <remarks>
		/// The return value will be an <see cref="int"/> value indicating each regions. Bits set 1 are
		/// covered regions.
		/// </remarks>
		public readonly int CoveredRegions
		{
			get
			{
				int resultRegions = 0;
				for (int i = BlockOffset; i < Limit; i++)
				{
					if ((_high & ~CoverTable[i, 0]) == 0 && (_low & ~CoverTable[i, 1]) == 0)
					{
						resultRegions |= 1 << i;
					}
				}

				return resultRegions;
			}
		}

		/// <summary>
		/// All regions that the map spanned. This property is used to check all regions that all cells of
		/// this instance spanned. For example, if the cells are { 0, 1 }, the property
		/// <see cref="Regions"/> will return the region 0 (block 1), region 9 (row 1), region 18 (column 1)
		/// and the region 19 (column 2).
		/// </summary>
		public readonly int Regions
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (int)BlockMask | RowMask << RowOffset | ColumnMask << ColumnOffset;
		}

		/// <summary>
		/// Indicates the map of cells, which is the peer intersections.
		/// </summary>
		public readonly Cells PeerIntersection
		{
			get
			{
				long lowerBits = 0, higherBits = 0;
				int i = 0;
				foreach (int offset in Offsets)
				{
					long low = 0, high = 0;
					foreach (int peer in Peers[offset])
					{
						(peer / Shifting == 0 ? ref low : ref high) |= 1L << peer % Shifting;
					}

					if (i++ == 0)
					{
						lowerBits = low;
						higherBits = high;
					}
					else
					{
						lowerBits &= low;
						higherBits &= high;
					}
				}

				return new(higherBits, lowerBits);
			}
		}

		/// <summary>
		/// Indicates all cell offsets whose corresponding value are set <see langword="true"/>.
		/// </summary>
		private readonly unsafe int[] Offsets
		{
			get
			{
				if (IsEmpty)
				{
					return Array.Empty<int>();
				}

				long value;
				int i, pos = 0;
				int* result = stackalloc int[Count];
				if (_low != 0)
				{
					for (value = _low, i = 0; i < Shifting; i++, value >>= 1)
					{
						if ((value & 1) != 0)
						{
							result[pos++] = i;
						}
					}
				}
				if (_high != 0)
				{
					for (value = _high, i = Shifting; i < 81; i++, value >>= 1)
					{
						if ((value & 1) != 0)
						{
							result[pos++] = i;
						}
					}
				}

				int[] arr = new int[Count];
				fixed (int* ptr = arr)
				{
					Unsafe.CopyBlock(ptr, result, (uint)(sizeof(int) * Count));
				}

				return arr;
			}
		}


		/// <summary>
		/// Get the cell offset at the specified position index.
		/// </summary>
		/// <param name="index">The index of position of all set bits.</param>
		/// <returns>
		/// This cell offset at the specified position index. If the value is invalid,
		/// the return value will be <c>-1</c>.
		/// </returns>
		[IndexerName("SetOffset")]
		public readonly int this[int index]
		{
			get
			{
				if (IsEmpty)
				{
					return -1;
				}

				long value;
				int i, pos = -1;
				if (_low != 0)
				{
					for (value = _low, i = 0; i < Shifting; i++, value >>= 1)
					{
						if ((value & 1) != 0 && ++pos == index)
						{
							return i;
						}
					}
				}
				if (_high != 0)
				{
					for (value = _high, i = Shifting; i < 81; i++, value >>= 1)
					{
						if ((value & 1) != 0 && ++pos == index)
						{
							return i;
						}
					}
				}

				return -1;
			}
		}


		/// <summary>
		/// Copies the current instance to the tagret array specified as an <see cref="int"/>*.
		/// </summary>
		/// <param name="arr">The pointer that points to an array of type <see cref="int"/>.</param>
		/// <param name="length">The length of that array.</param>
		public readonly unsafe void CopyTo(int* arr, int length)
		{
			if (IsEmpty)
			{
				return;
			}

			if (Count > length)
			{
				throw new ArgumentException("The capacity is not enough.", nameof(arr));
			}

			long value;
			int i, pos = 0;
			if (_low != 0)
			{
				for (value = _low, i = 0; i < Shifting; i++, value >>= 1)
				{
					if ((value & 1) != 0)
					{
						arr[pos++] = i;
					}
				}
			}
			if (_high != 0)
			{
				for (value = _high, i = Shifting; i < 81; i++, value >>= 1)
				{
					if ((value & 1) != 0)
					{
						arr[pos++] = i;
					}
				}
			}
		}

		/// <summary>
		/// Copies the current instance to the tagret <see cref="Span{T}"/> instance.
		/// </summary>
		/// <param name="span">
		/// The target <see cref="Span{T}"/> instance.
		/// </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly unsafe void CopyTo(ref Span<int> span)
		{
			fixed (int* arr = span)
			{
				CopyTo(arr, span.Length);
			}
		}

		/// <summary>
		/// Indicates whether all cells in this instance are in one region.
		/// </summary>
		/// <param name="region">
		/// The region covered. If the return value
		/// is false, this value will be the constant -1.
		/// </param>
		/// <returns>A <see cref="bool"/> result.</returns>
		/// <remarks>
		/// If you don't want to use the <see langword="out"/> parameter value, please
		/// use the property <see cref="InOneRegion"/> to improve the performance.
		/// </remarks>
		/// <seealso cref="InOneRegion"/>
		public readonly bool AllSetsAreInOneRegion(out int region)
		{
			for (int i = BlockOffset; i < Limit; i++)
			{
				if ((_high & ~CoverTable[i, 0]) == 0 && (_low & ~CoverTable[i, 1]) == 0)
				{
					region = i;
					return true;
				}
			}

			region = -1;
			return false;
		}

		/// <summary>
		/// Determine whether the map contains the specified cell.
		/// </summary>
		/// <param name="cell">The cell.</param>
		/// <returns>A <see cref="bool"/> value indicating that.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly unsafe bool Contains(int cell) =>
			((cell / Shifting == 0 ? _low : _high) >> cell % Shifting & 1) != 0;

		/// <summary>
		/// Get the subview mask of this map.
		/// </summary>
		/// <param name="region">The region.</param>
		/// <returns>The mask.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly short GetSubviewMask(int region) => this / region;

		/// <summary>
		/// To gets the cells that is in the cells that both <see langword="this"/>
		/// and <paramref name="limit"/> sees (i.e. peer intersection of <c>this &amp; limit</c>),
		/// and gets the result map that is in the map above, and only lies in <paramref name="limit"/>.
		/// </summary>
		/// <param name="limit">
		/// The map to limit the result peer intersection.
		/// </param>
		/// <returns>The result map.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly Cells PeerIntersectionLimitsWith(in Cells limit) => this % limit;

		/// <inheritdoc cref="object.GetHashCode"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override readonly int GetHashCode() => ToString("b").GetHashCode();

		/// <summary>
		/// Get all set cell offsets and returns them as an array.
		/// </summary>
		/// <returns>An array of all set cell offsets.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly int[] ToArray() => Offsets;

		/// <inheritdoc cref="object.ToString"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override readonly string ToString() => ToString(null);

		/// <inheritdoc cref="Formattable.ToString(string?)"/>
		/// <exception cref="FormatException">Throws when the format is invalid.</exception>
		public readonly string ToString(string? format)
		{
			return format switch
			{
				null or "N" or "n" => Count switch
				{
					0 => "{ }",
					/*length-pattern*/
					1 when Offsets[0] is var cell => $"r{(cell / 9 + 1).ToString()}c{(cell % 9 + 1).ToString()}",
					_ => normalToString(this)
				},
				"B" or "b" => binaryToString(this, false),
				"T" or "t" => tableToString(this),
				_ => throw new FormatException("The specified format is invalid.")
			};

			static string tableToString(in Cells @this)
			{
				var sb = new ValueStringBuilder(stackalloc char[(3 * 7 + 2) * 13]);
				for (int i = 0; i < 3; i++)
				{
					for (int bandLn = 0; bandLn < 3; bandLn++)
					{
						for (int j = 0; j < 3; j++)
						{
							for (int columnLn = 0; columnLn < 3; columnLn++)
							{
								sb.Append(@this.Contains((i * 3 + bandLn) * 9 + j * 3 + columnLn) ? '*' : '.');
								sb.Append(' ');
							}

							if (j != 2)
							{
								sb.Append("| ");
							}
							else
							{
								sb.AppendLine();
							}
						}
					}

					if (i != 2)
					{
						sb.AppendLine("------+-------+------");
					}
				}

				return sb.ToString();
			}

			static unsafe string normalToString(in Cells @this)
			{
				const string leftCurlyBrace = "{ ", rightCurlyBrace = " }", separator = ", ";
				var sbRow = new ValueStringBuilder(stackalloc char[50]);
				var dic = new Dictionary<int, ICollection<int>>();
				foreach (int cell in @this)
				{
					if (!dic.ContainsKey(cell / 9))
					{
						dic.Add(cell / 9, new List<int>());
					}

					dic[cell / 9].Add(cell % 9);
				}
				bool addCurlyBraces = dic.Count > 1;
				if (addCurlyBraces)
				{
					sbRow.Append(leftCurlyBrace);
				}
				foreach (int row in dic.Keys)
				{
					sbRow.Append('r');
					sbRow.Append(row + 1);
					sbRow.Append('c');
					sbRow.AppendRange(dic[row], &g);
					sbRow.Append(separator);
				}
				sbRow.RemoveFromEnd(separator.Length);
				if (addCurlyBraces)
				{
					sbRow.Append(rightCurlyBrace);
				}

				dic.Clear();
				var sbColumn = new ValueStringBuilder(stackalloc char[50]);
				foreach (int cell in @this)
				{
					if (!dic.ContainsKey(cell % 9))
					{
						dic.Add(cell % 9, new List<int>());
					}

					dic[cell % 9].Add(cell / 9);
				}
				addCurlyBraces = dic.Count > 1;
				if (addCurlyBraces)
				{
					sbColumn.Append(leftCurlyBrace);
				}

				foreach (int column in dic.Keys)
				{
					sbColumn.Append('r');
					sbColumn.AppendRange(dic[column], &g);
					sbColumn.Append('c');
					sbColumn.Append(column + 1);
					sbColumn.Append(separator);
				}
				sbColumn.RemoveFromEnd(separator.Length);
				if (addCurlyBraces)
				{
					sbColumn.Append(rightCurlyBrace);
				}

				return (sbRow.Length > sbColumn.Length ? sbColumn : sbRow).ToString();

				static string g(int v) => (v + 1).ToString();
			}

			static string binaryToString(in Cells @this, bool withSeparator)
			{
				var sb = new ValueStringBuilder(stackalloc char[81]);
				int i;
				long value = @this._low;
				for (i = 0; i < 27; i++, value >>= 1)
				{
					sb.Append(value & 1);
				}
				if (withSeparator)
				{
					sb.Append(' ');
				}
				for (; i < 41; i++, value >>= 1)
				{
					sb.Append(value & 1);
				}
				for (value = @this._high; i < 54; i++, value >>= 1)
				{
					sb.Append(value & 1);
				}
				if (withSeparator)
				{
					sb.Append(' ');
				}
				for (; i < 81; i++, value >>= 1)
				{
					sb.Append(value & 1);
				}

				sb.Reverse();
				return sb.ToString();
			}
		}

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly string ToString(string? format, IFormatProvider? formatProvider) =>
			formatProvider.HasFormatted(this, format, out string? result) ? result : ToString(format);

		/// <summary>
		/// Converts the current instance to a <see cref="Span{T}"/> of type <see cref="int"/>.
		/// </summary>
		/// <returns>The <see cref="Span{T}"/> of <see cref="int"/> result.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly Span<int> ToSpan() => Offsets.AsSpan();

		/// <summary>
		/// Converts the current instance to a <see cref="ReadOnlySpan{T}"/> of type <see cref="int"/>.
		/// </summary>
		/// <returns>The <see cref="ReadOnlySpan{T}"/> of <see cref="int"/> result.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly ReadOnlySpan<int> ToReadOnlySpan() => Offsets.AsSpan();

		/// <summary>
		/// Expands the current instance, using the specified digit.
		/// </summary>
		/// <param name="digit">The digit.</param>
		/// <returns>The candidate list.</returns>
		public readonly Candidates Expand(int digit) => this * digit;

		/// <summary>
		/// Being called by <see cref="RowMask"/>, <see cref="ColumnMask"/> and <see cref="BlockMask"/>.
		/// </summary>
		/// <param name="start">The start index.</param>
		/// <param name="end">The end index.</param>
		/// <returns>The region mask.</returns>
		/// <seealso cref="RowMask"/>
		/// <seealso cref="ColumnMask"/>
		/// <seealso cref="BlockMask"/>
		private readonly short CreateMask(int start, int end)
		{
			short result = 0;
			for (int i = start; i < end; i++)
			{
				if (!(this & RegionMaps[i]).IsEmpty)
				{
					result |= (short)(1 << i - start);
				}
			}

			return result;
		}

		/// <summary>
		/// Set the specified cell as <see langword="true"/> or <see langword="false"/> value.
		/// </summary>
		/// <param name="offset">
		/// The cell offset. This value can be positive and negative. If 
		/// negative, the offset will be assigned <see langword="false"/>
		/// into the corresponding bit position of its absolute value.
		/// </param>
		/// <remarks>
		/// <para>
		/// For example, if the offset is -2 (~1), the [1] will be assigned <see langword="false"/>:
		/// <code>
		/// var map = new Cells(xxx) { ~1 };
		/// </code>
		/// which is equivalent to:
		/// <code>
		/// var map = new Cells(xxx);
		/// map[1] = false;
		/// </code>
		/// </para>
		/// <para>
		/// Note: The argument <paramref name="offset"/> should be with the bit-complement operator <c>~</c>
		/// to describe the value is a negative one. As the example below, -2 is described as <c>~1</c>,
		/// so the offset is 1, rather than 2.
		/// </para>
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void Add(int offset)
		{
			if (offset >= 0) // Positive or zero.
			{
				InternalAdd(offset, true);
			}
			else // Negative values.
			{
				InternalAdd(~offset, false);
			}
		}

		/// <summary>
		/// Set the specified cell as <see langword="true"/> value.
		/// </summary>
		/// <param name="offset">The cell offset.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddAnyway(int offset) => InternalAdd(offset, true);

		/// <summary>
		/// Set the specified cells as <see langword="true"/> value.
		/// </summary>
		/// <param name="offsets">The cells to add.</param>
		public void AddRange(in ReadOnlySpan<int> offsets)
		{
			foreach (int cell in offsets)
			{
				AddAnyway(cell);
			}
		}

		/// <summary>
		/// Set the specified cells as <see langword="true"/> value.
		/// </summary>
		/// <param name="offsets">The cells to add.</param>
		public void AddRange(IEnumerable<int> offsets)
		{
			foreach (int cell in offsets)
			{
				AddAnyway(cell);
			}
		}

		/// <summary>
		/// Set the specified cell as <see langword="false"/> value.
		/// </summary>
		/// <param name="offset">The cell offset.</param>
		/// <remarks>
		/// Different with <see cref="Add(int)"/>, this method <b>can't</b> receive
		/// the negative value as the parameter.
		/// </remarks>
		/// <seealso cref="Add(int)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Remove(int offset) => InternalAdd(offset, false);

		/// <summary>
		/// Clear all bits.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear() => _low = _high = Count = 0;

		/// <summary>
		/// The internal operation for adding a cell.
		/// </summary>
		/// <param name="cell">The cell to add into.</param>
		/// <param name="value">The value to add.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InternalAdd(int cell, bool value)
		{
			if (cell is >= 0 and < 81)
			{
				ref long v = ref cell / Shifting == 0 ? ref _low : ref _high;
				bool older = Contains(cell);
				if (value)
				{
					v |= 1L << cell % Shifting;
					if (!older)
					{
						Count++;
					}
				}
				else
				{
					v &= ~(1L << cell % Shifting);
					if (older)
					{
						Count--;
					}
				}
			}
		}


		/// <summary>
		/// Parse a <see cref="string"/> and convert to the <see cref="Cells"/> instance.
		/// </summary>
		/// <param name="str">The string text.</param>
		/// <returns>The result cell instance.</returns>
		/// <exception cref="FormatException">Throws when the specified text is invalid to parse.</exception>
		public static unsafe Cells Parse(string str)
		{
			var regex = new Regex(
				RegularExpressions.CellOrCellList,
				RegexOptions.ExplicitCapture,
				TimeSpan.FromSeconds(5)
			);

			// Check whether the match is successful.
			var matches = regex.Matches(str);
			if (matches.Count == 0)
			{
				throw new FormatException("The specified string can't match any cell instance.");
			}

			// Declare the buffer.
			int* bufferRows = stackalloc int[9], bufferColumns = stackalloc int[9];

			// Declare the result variable.
			var result = Empty;

			// Iterate on each match instance.
			foreach (Match match in matches)
			{
				string value = match.Value;
				char* anchorR, anchorC;
				fixed (char* pValue = value)
				{
					anchorR = anchorC = pValue;

					// Find the index of the character 'C'.
					// The regular expression guaranteed the string must contain the character 'C' or 'c',
					// so we don't need to check '*p != '\0''.
					for (; *anchorC is not ('C' or 'c'/* or '\0'*/); anchorC++) ;
				}

				// Stores the possible values into the buffer.
				int rIndex = 0, cIndex = 0;
				for (char* p = anchorR + 1; *p is not ('C' or 'c'); p++, rIndex++)
				{
					bufferRows[rIndex] = *p - '1';
				}
				for (char* p = anchorC + 1; *p != '\0'; p++, cIndex++)
				{
					bufferColumns[cIndex] = *p - '1';
				}

				// Now combine two buffers.
				for (int i = 0; i < rIndex; i++)
				{
					for (int j = 0; j < cIndex; j++)
					{
						result.Add(bufferRows[i] * 9 + bufferColumns[j]);
					}
				}
			}

			// Returns the result.
			return result;
		}

		/// <summary>
		/// Try to parse the specified <see cref="string"/>, and convert it to the <see cref="Cells"/>
		/// instance.
		/// </summary>
		/// <param name="str">The string to parse.</param>
		/// <param name="result">The result that converted.</param>
		/// <returns>
		/// A <see cref="bool"/> result indicating whether the parsing operation
		/// has been successfully executed.
		/// </returns>
		public static bool TryParse(string str, out Cells result)
		{
			try
			{
				result = Parse(str);
				return true;
			}
			catch (FormatException)
			{
				result = Empty;
				return false;
			}
		}


		/// <summary>
		/// Reverse status for all cells, which means all <see langword="true"/> bits
		/// will be set <see langword="false"/>, and all <see langword="false"/> bits
		/// will be set <see langword="true"/>.
		/// </summary>
		/// <param name="gridMap">The instance to negate.</param>
		/// <returns>The negative result.</returns>
		/// <remarks>
		/// While reversing the higher 40 bits, the unused bits will be fixed and never be modified the state,
		/// that is why using the code "<c>higherBits &amp; 0xFFFFFFFFFFL</c>".
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Cells operator ~(in Cells gridMap) =>
			new(~gridMap._high & 0xFFFFFFFFFFL, ~gridMap._low & 0x1FFFFFFFFFFL);

		/// <summary>
		/// The syntactic sugar for <c>!(<paramref name="left"/> - <paramref name="right"/>).IsEmpty</c>.
		/// </summary>
		/// <param name="left">The subtrahend.</param>
		/// <param name="right">The subtractor.</param>
		/// <returns>The <see cref="bool"/> value indicating that.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator >(in Cells left, in Cells right) => !(left - right).IsEmpty;

		/// <summary>
		/// The syntactic sugar for <c>(<paramref name="left"/> - <paramref name="right"/>).IsEmpty</c>.
		/// </summary>
		/// <param name="left">The subtrahend.</param>
		/// <param name="right">The subtractor.</param>
		/// <returns>The <see cref="bool"/> value indicating that.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator <(in Cells left, in Cells right) => (left - right).IsEmpty;

		/// <summary>
		/// Get a <see cref="Cells"/> that contains all <paramref name="left"/> instance
		/// but not in <paramref name="right"/> instance.
		/// </summary>
		/// <param name="left">The left instance.</param>
		/// <param name="right">The right instance.</param>
		/// <returns>The result.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Cells operator -(in Cells left, in Cells right) => left & ~right;

		/// <summary>
		/// Get all cells that two <see cref="Cells"/> instances both contain.
		/// </summary>
		/// <param name="left">The left instance.</param>
		/// <param name="right">The right instance.</param>
		/// <returns>The intersection result.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Cells operator &(in Cells left, in Cells right) =>
			new(left._high & right._high, left._low & right._low);

		/// <summary>
		/// Get all cells from two <see cref="Cells"/> instances.
		/// </summary>
		/// <param name="left">The left instance.</param>
		/// <param name="right">The right instance.</param>
		/// <returns>The union result.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Cells operator |(in Cells left, in Cells right) =>
			new(left._high | right._high, left._low | right._low);

		/// <summary>
		/// Get all cells that only appears once in two <see cref="Cells"/> instances.
		/// </summary>
		/// <param name="left">The left instance.</param>
		/// <param name="right">The right instance.</param>
		/// <returns>The symmetrical difference result.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Cells operator ^(in Cells left, in Cells right) =>
			new(left._high ^ right._high, left._low ^ right._low);

		/// <summary>
		/// Expands via the specified digit.
		/// </summary>
		/// <param name="base">The base map.</param>
		/// <param name="digit">The digit.</param>
		/// <returns>The result instance.</returns>
		public static unsafe Candidates operator *(in Cells @base, int digit)
		{
			var result = Candidates.Empty;
			int[] cells = @base.Offsets;
			fixed (int* p = cells)
			{
				int* ptr = p;
				for (int i = 0, length = cells.Length; i < length; ptr++)
				{
					result.AddAnyway(*ptr * 9 + digit);
				}
			}

			return result;
		}

		/// <summary>
		/// Get the subview mask of this map.
		/// </summary>
		/// <param name="map">The map.</param>
		/// <param name="region">The region.</param>
		/// <returns>The mask.</returns>
		public static short operator /(in Cells map, int region)
		{
			short p = 0, i = 0;
			foreach (int cell in RegionCells[region])
			{
				if (map.Contains(cell))
				{
					p |= (short)(1 << i);
				}

				i++;
			}

			return p;
		}

		/// <summary>
		/// Simply calls <c>-(a &amp; b) &amp; b</c>. The operator is used for searching and checking
		/// eliminations.
		/// </summary>
		/// <param name="base">The base map.</param>
		/// <param name="limit">The limit map that the base map sees.</param>
		/// <returns>The result map.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Cells operator %(in Cells @base, in Cells limit) =>
			(@base & limit).PeerIntersection & limit;


		/// <summary>
		/// Implicit cast from <see cref="int"/>[] to <see cref="Cells"/>.
		/// </summary>
		/// <param name="cells">The cells.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Cells(int[] cells) => new(cells);

		/// <summary>
		/// Implicit cast from <see cref="Span{T}"/> to <see cref="Cells"/>.
		/// </summary>
		/// <param name="cells">The cells.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Cells(in Span<int> cells) => new(cells);

		/// <summary>
		/// Implicit cast from <see cref="ReadOnlySpan{T}"/> to <see cref="Cells"/>.
		/// </summary>
		/// <param name="cells">The cells.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Cells(in ReadOnlySpan<int> cells) => new(cells);

		/// <summary>
		/// Implicit cast from <see cref="Cells"/> to <see cref="Span{T}"/>.
		/// </summary>
		/// <param name="map">The map.</param>
		public static implicit operator Span<int>(in Cells map) => map.ToSpan();

		/// <summary>
		/// Implicit cast from <see cref="Cells"/> to <see cref="ReadOnlySpan{T}"/>.
		/// </summary>
		/// <param name="map">The map.</param>
		public static implicit operator ReadOnlySpan<int>(in Cells map) => map.ToReadOnlySpan();
	}
}
