﻿using System.Collections.Generic;
using Sudoku.CodeGen;
using Sudoku.Data;

namespace Sudoku.Painting
{
	/// <summary>
	/// This is a data structure that stores the presentation data when drawing onto a picture.
	/// </summary>
	[AutoDeconstruct(nameof(Cells), nameof(Candidates), nameof(Regions), nameof(Links), nameof(DirectLines), nameof(StepSketch))]
	public sealed partial class PresentationData
	{
		/// <summary>
		/// The back field of <see cref="Cells"/>.
		/// </summary>
		/// <seealso cref="Cells"/>
		private ICollection<PaintingPair<int>>? _cells;

		/// <summary>
		/// The back field of <see cref="Candidates"/>.
		/// </summary>
		/// <seealso cref="Candidates"/>
		private ICollection<PaintingPair<int>>? _candidates;

		/// <summary>
		/// The back field of <see cref="Regions"/>.
		/// </summary>
		/// <seealso cref="Regions"/>
		private ICollection<PaintingPair<int>>? _regions;

		/// <summary>
		/// The back field of <see cref="Links"/>.
		/// </summary>
		/// <seealso cref="Links"/>
		private ICollection<PaintingPair<Link>>? _links;

		/// <summary>
		/// The back field of <see cref="DirectLines"/>.
		/// </summary>
		/// <seealso cref="DirectLines"/>
		private ICollection<PaintingPair<(Cells Start, Cells End)>>? _directLines;

		/// <summary>
		/// The back field of <see cref="StepSketch"/>.
		/// </summary>
		/// <seealso cref="StepSketch"/>
		private ICollection<PaintingPair<(int Cell, char Character)>>? _stepSketch;


		/// <summary>
		/// The cell information.
		/// </summary>
		/// <value>The value you want to set.</value>
		public ICollection<PaintingPair<int>>? Cells
		{
			get => _cells;

			set
			{
				_cells = value;

				CellsChanged?.Invoke(value);
			}
		}

		/// <summary>
		/// The candidate information.
		/// </summary>
		/// <value>The value you want to set.</value>
		public ICollection<PaintingPair<int>>? Candidates
		{
			get => _candidates;

			set
			{
				_candidates = value;

				CandidatesChanged?.Invoke(value);
			}
		}

		/// <summary>
		/// The region information.
		/// </summary>
		/// <value>The value you want to set.</value>
		public ICollection<PaintingPair<int>>? Regions
		{
			get => _regions;

			set
			{
				_regions = value;

				RegionsChanged?.Invoke(value);
			}
		}

		/// <summary>
		/// The link information.
		/// </summary>
		/// <value>The value you want to set.</value>
		public ICollection<PaintingPair<Link>>? Links
		{
			get => _links;

			set
			{
				_links = value;

				LinksChanged?.Invoke(value);
			}
		}

		/// <summary>
		/// The direct line information.
		/// </summary>
		/// <value>The value you want to set.</value>
		public ICollection<PaintingPair<(Cells Start, Cells End)>>? DirectLines
		{
			get => _directLines;

			set
			{
				_directLines = value;

				DirectLinesChanged?.Invoke(value);
			}
		}

		/// <summary>
		/// The step sketch.
		/// </summary>
		/// <value>The value you want to set.</value>
		public ICollection<PaintingPair<(int Cell, char Character)>>? StepSketch
		{
			get => _stepSketch;

			set
			{
				_stepSketch = value;

				StepSketchChanged?.Invoke(value);
			}
		}


		/// <summary>
		/// Indicates the event triggered when the cell list is changed.
		/// </summary>
		public event PresentationDataChangedEventHandler<int>? CellsChanged;

		/// <summary>
		/// Indicates the event triggered when the candidate list is changed.
		/// </summary>
		public event PresentationDataChangedEventHandler<int>? CandidatesChanged;

		/// <summary>
		/// Indicates the event triggered when the region list is changed.
		/// </summary>
		public event PresentationDataChangedEventHandler<int>? RegionsChanged;

		/// <summary>
		/// Indicates the event triggered when the link list is changed.
		/// </summary>
		public event PresentationDataChangedEventHandler<Link>? LinksChanged;

		/// <summary>
		/// Indicates the event triggered when the direct line list is changed.
		/// </summary>
		public event PresentationDataChangedEventHandler<(Cells Start, Cells End)>? DirectLinesChanged;

		/// <summary>
		/// Indicates the event triggered when the step sketch list is changed.
		/// </summary>
		public event PresentationDataChangedEventHandler<(int Cell, char Character)>? StepSketchChanged;


		/// <summary>
		/// Add a new instance into the collection.
		/// </summary>
		/// <typeparam name="TUnmanaged">The type of the value to add into.</typeparam>
		/// <param name="item">The property item.</param>
		/// <param name="value">The value to add into.</param>
		public bool Add<TUnmanaged>(PresentationDataItem item, in TUnmanaged value) where TUnmanaged : unmanaged
		{
			switch (item)
			{
				case PresentationDataItem.CellList when value is PaintingPair<int> i:
				{
					(Cells ??= new List<PaintingPair<int>>()).Add(i);
					CellsChanged?.Invoke(Cells);
					return true;
				}
				case PresentationDataItem.CandidateList when value is PaintingPair<int> i:
				{
					(Candidates ??= new List<PaintingPair<int>>()).Add(i);
					CandidatesChanged?.Invoke(Candidates);
					return true;
				}
				case PresentationDataItem.RegionList when value is PaintingPair<int> i:
				{
					(Regions ??= new List<PaintingPair<int>>()).Add(i);
					RegionsChanged?.Invoke(Regions);
					return true;
				}
				case PresentationDataItem.LinkList when value is PaintingPair<Link> i:
				{
					(Links ??= new List<PaintingPair<Link>>()).Add(i);
					LinksChanged?.Invoke(Links);
					return true;
				}
				case PresentationDataItem.DirectLineList when value is PaintingPair<(Cells, Cells)> i:
				{
					(DirectLines ??= new List<PaintingPair<(Cells, Cells)>>()).Add(i);
					DirectLinesChanged?.Invoke(DirectLines);
					return true;
				}
				case PresentationDataItem.StepSketchList when value is PaintingPair<(int, char)> i:
				{
					(StepSketch ??= new List<PaintingPair<(int, char)>>()).Add(i);
					StepSketchChanged?.Invoke(StepSketch);
					return true;
				}
				default:
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Add a series of elements into the collection.
		/// </summary>
		/// <typeparam name="TUnmanaged">The type of each element.</typeparam>
		/// <param name="item">The property you want to add into.</param>
		/// <param name="values">The values you want to add.</param>
		/// <returns>The number of elements that is successful to add.</returns>
		public int AddRange<TUnmanaged>(PresentationDataItem item, IEnumerable<TUnmanaged> values)
			where TUnmanaged : unmanaged
		{
			int result = 0;
			byte tag = 0;
			foreach (var value in values)
			{
				switch (item)
				{
					case PresentationDataItem.CellList when value is PaintingPair<int> i:
					{
						(Cells ??= new List<PaintingPair<int>>()).Add(i);
						if (tag == 0)
						{
							tag = 1;
						}

						result++;

						break;
					}
					case PresentationDataItem.CandidateList when value is PaintingPair<int> i:
					{
						(Candidates ??= new List<PaintingPair<int>>()).Add(i);
						if (tag == 0)
						{
							tag = 2;
						}

						result++;

						break;
					}
					case PresentationDataItem.RegionList when value is PaintingPair<int> i:
					{
						(Regions ??= new List<PaintingPair<int>>()).Add(i);
						if (tag == 0)
						{
							tag = 3;
						}

						result++;

						break;
					}
					case PresentationDataItem.LinkList when value is PaintingPair<Link> i:
					{
						(Links ??= new List<PaintingPair<Link>>()).Add(i);
						if (tag == 0)
						{
							tag = 4;
						}

						result++;

						break;
					}
					case PresentationDataItem.DirectLineList when value is PaintingPair<(Cells, Cells)> i:
					{
						(DirectLines ??= new List<PaintingPair<(Cells, Cells)>>()).Add(i);
						if (tag == 0)
						{
							tag = 5;
						}

						result++;

						break;
					}
					case PresentationDataItem.StepSketchList when value is PaintingPair<(int, char)> i:
					{
						(StepSketch ??= new List<PaintingPair<(int, char)>>()).Add(i);
						if (tag == 0)
						{
							tag = 6;
						}

						result++;

						break;
					}
				}
			}

			// Trigger the corresponding event.
			switch (tag)
			{
				case 1: CellsChanged?.Invoke(Cells); break;
				case 2: CandidatesChanged?.Invoke(Candidates); break;
				case 3: RegionsChanged?.Invoke(Regions); break;
				case 4: LinksChanged?.Invoke(Links); break;
				case 5: DirectLinesChanged?.Invoke(DirectLines); break;
				case 6: StepSketchChanged?.Invoke(StepSketch); break;
			}

			return result;
		}

		/// <summary>
		/// Remove a new instance from the collection.
		/// </summary>
		/// <typeparam name="TUnmanaged">The type of the value to remove.</typeparam>
		/// <param name="item">The property item.</param>
		/// <param name="value">The value to remove.</param>
		public bool Remove<TUnmanaged>(PresentationDataItem item, in TUnmanaged value)
			where TUnmanaged : unmanaged
		{
			switch (item)
			{
				case PresentationDataItem.CellList when value is PaintingPair<int> i && Cells is not null:
				{
					Cells.Remove(i);
					CellsChanged?.Invoke(Cells);
					return true;
				}
				case PresentationDataItem.CandidateList
				when value is PaintingPair<int> i && Candidates is not null:
				{
					Candidates.Remove(i);
					CandidatesChanged?.Invoke(Candidates);
					return true;
				}
				case PresentationDataItem.RegionList when value is PaintingPair<int> i && Regions is not null:
				{
					Regions.Remove(i);
					RegionsChanged?.Invoke(Regions);
					return true;
				}
				case PresentationDataItem.LinkList when value is PaintingPair<Link> i && Links is not null:
				{
					Links.Remove(i);
					LinksChanged?.Invoke(Links);
					return true;
				}
				case PresentationDataItem.DirectLineList
				when value is PaintingPair<(Cells, Cells)> i && DirectLines is not null:
				{
					DirectLines.Remove(i);
					DirectLinesChanged?.Invoke(DirectLines);
					return true;
				}
				case PresentationDataItem.StepSketchList
				when value is PaintingPair<(int, char)> i && StepSketch is not null:
				{
					StepSketch.Remove(i);
					StepSketchChanged?.Invoke(StepSketch);
					return true;
				}
				default:
				{
					return false;
				}
			}
		}
	}
}
