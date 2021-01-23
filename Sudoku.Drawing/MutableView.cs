﻿using System.Collections.Generic;
using Sudoku.Data;
using Sudoku.DocComments;
using Sudoku.Models;

namespace Sudoku.Drawing
{
	/// <summary>
	/// Encapsulates a view when displaying the information on forms.
	/// Different with <see cref="View"/>, this data structure can add and remove the items
	/// in the current collection.
	/// </summary>
	/// <seealso cref="View"/>
	public sealed class MutableView
	{
		/// <inheritdoc cref="DefaultConstructor"/>
		public MutableView()
		{
			Cells = new List<DrawingInfo>();
			Candidates = new List<DrawingInfo>();
			Regions = new List<DrawingInfo>();
			Links = new List<Link>();
			DirectLines = new List<(Cells, Cells)>();
		}


		/// <summary>
		/// Indicates all cells used.
		/// </summary>
		public ICollection<DrawingInfo>? Cells { get; set; }

		/// <summary>
		/// Indicates all candidates used.
		/// </summary>
		public ICollection<DrawingInfo>? Candidates { get; set; }

		/// <summary>
		/// Indicates all regions used.
		/// </summary>
		public ICollection<DrawingInfo>? Regions { get; set; }

		/// <summary>
		/// Indicates all links used.
		/// </summary>
		public ICollection<Link>? Links { get; set; }

		/// <summary>
		/// Indicates all direct lines.
		/// </summary>
		public ICollection<(Cells Start, Cells End)>? DirectLines { get; set; }


		/// <summary>
		/// Add a cell into the list.
		/// </summary>
		/// <param name="id">The color ID.</param>
		/// <param name="cell">The cell.</param>
		public void AddCell(long id, int cell) => (Cells ??= new List<DrawingInfo>()).Add(new(id, cell));

		/// <summary>
		/// Add a candidate into the list.
		/// </summary>
		/// <param name="id">The color ID.</param>
		/// <param name="candidate">The cell.</param>
		public void AddCandidate(long id, int candidate) =>
			(Candidates ??= new List<DrawingInfo>()).Add(new(id, candidate));

		/// <summary>
		/// Add a region into the list.
		/// </summary>
		/// <param name="id">The color ID.</param>
		/// <param name="region">The region.</param>
		public void AddRegion(long id, int region) => (Regions ??= new List<DrawingInfo>()).Add(new(id, region));

		/// <summary>
		/// Add a link into the list.
		/// </summary>
		/// <param name="inference">The link.</param>
		public void AddLink(in Link inference) => (Links ??= new List<Link>()).Add(inference);

		/// <summary>
		/// Add a direct link into the list.
		/// </summary>
		/// <param name="start">(<see langword="in"/> parameter) The start map.</param>
		/// <param name="end">(<see langword="in"/> parameter) The end map.</param>
		public void AddDirectLine(in Cells start, in Cells end) =>
			(DirectLines ??= new List<(Cells, Cells)>()).Add((start, end));

		/// <summary>
		/// Remove the cell from the list.
		/// </summary>
		/// <param name="cell">The cell.</param>
		public void RemoveCell(int cell) => (Cells as List<DrawingInfo>)?.RemoveAll(p => p.Value == cell);

		/// <summary>
		/// Remove the candidate from the list.
		/// </summary>
		/// <param name="candidate">The candidate.</param>
		public void RemoveCandidate(int candidate) =>
			(Candidates as List<DrawingInfo>)?.RemoveAll(p => p.Value == candidate);

		/// <summary>
		/// Remove the region from the list.
		/// </summary>
		/// <param name="region">The region.</param>
		public void RemoveRegion(int region) =>
			(Regions as List<DrawingInfo>)?.RemoveAll(p => p.Value == region);

		/// <summary>
		/// Remove the link from the list, where the link is specified as a start candidate.
		/// </summary>
		/// <param name="startCandidate">The start candidate to check.</param>
		public void RemoveLink(int startCandidate)
		{
			if (Links is null)
			{
				return;
			}

			Link? linkToRemove = null;
			foreach (var link in Links)
			{
				var (start, _) = link;
				if (start == startCandidate)
				{
					linkToRemove = link;
					break;
				}
			}

			if (linkToRemove is not { } removeOne)
			{
				return;
			}

			Links.Remove(removeOne);
		}

		/// <summary>
		/// Remove the link from the list.
		/// </summary>
		/// <param name="link">(<see langword="in"/> parameter) The link.</param>
		public void RemoveLink(in Link link) => Links?.Remove(link);

		/// <summary>
		/// Remove the direct link from the list.
		/// </summary>
		/// <param name="start">(<see langword="in"/> parameter) The start map.</param>
		/// <param name="end">(<see langword="in"/> parameter) The end map.</param>
		public void RemoveDirectLine(in Cells start, in Cells end) => DirectLines?.Remove((start, end));

		/// <summary>
		/// Clear all elements.
		/// </summary>
		public void Clear()
		{
			Cells?.Clear();
			Candidates?.Clear();
			Regions?.Clear();
			Links?.Clear();
			DirectLines?.Clear();
		}

		/// <summary>
		/// Indicates whether the list contains the specified cell.
		/// </summary>
		/// <param name="cell">The cell.</param>
		/// <returns>A <see cref="bool"/> value.</returns>
		public bool ContainsCell(int cell)
		{
			if (Cells is null)
			{
				goto Returns;
			}

			foreach (var (_, value) in Cells)
			{
				if (value == cell)
				{
					return true;
				}
			}

		Returns:
			return false;
		}

		/// <summary>
		/// Indicates whether the list contains the specified candidate.
		/// </summary>
		/// <param name="candidate">The candidate.</param>
		/// <returns>A <see cref="bool"/> value.</returns>
		public bool ContainsCandidate(int candidate)
		{
			if (Candidates is null)
			{
				goto Returns;
			}

			foreach (var (_, value) in Candidates)
			{
				if (value == candidate)
				{
					return true;
				}
			}

		Returns:
			return false;
		}

		/// <summary>
		/// Indicates whether the list contains the specified region.
		/// </summary>
		/// <param name="region">The region.</param>
		/// <returns>A <see cref="bool"/> value.</returns>
		public bool ContainsRegion(int region)
		{
			if (Regions is null)
			{
				goto Returns;
			}

			foreach (var (_, value) in Regions)
			{
				if (value == region)
				{
					return true;
				}
			}

		Returns:
			return false;
		}

		/// <summary>
		/// Indicates whether the list contains the specified link where the start candidate is same
		/// as the argument <paramref name="startCandidate"/>.
		/// </summary>
		/// <param name="startCandidate">The start candidate.</param>
		/// <returns>A <see cref="bool"/> result.</returns>
		public bool ContainsLink(int startCandidate)
		{
			if (Links is null)
			{
				return false;
			}

			foreach (var (start, _) in Links)
			{
				if (start == startCandidate)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Indicates whether the list contains the specified link.
		/// </summary>
		/// <param name="inference">(<see langword="in"/> parameter) The link.</param>
		/// <returns>A <see cref="bool"/> value.</returns>
		public bool ContainsLink(in Link inference) => Links?.Contains(inference) ?? false;

		/// <summary>
		/// Indicates whether the list contains the specified direct line.
		/// </summary>
		/// <param name="start">(<see langword="in"/> parameter) The start map.</param>
		/// <param name="end">(<see langword="in"/> parameter) The end map.</param>
		/// <returns>A <see cref="bool"/> value.</returns>
		public bool ContainsDirectLine(in Cells start, in Cells end) =>
			DirectLines?.Contains((start, end)) ?? false;
	}
}
