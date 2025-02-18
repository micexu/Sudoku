﻿using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.CompilerServices;
using Sudoku.Data;

namespace Sudoku.Painting
{
	/// <summary>
	/// Indicates a generator that is used for painting the specified information onto the image,
	/// and returns the image instance.
	/// </summary>
	/// <param name="Converter">Indicates the translator.</param>
	/// <param name="Preferences">Indicates the default preferences that used for painting.</param>
	/// <remarks>
	/// Please note that eliminations will be colored with red, but all assignments won't be colored,
	/// because they will be colored using another method (draw candidates).
	/// </remarks>
	public sealed partial record GridImageGenerator(in DrawingPointConverter Converter, Settings Preferences)
	{
		/// <summary>
		/// Indicates the drawing width.
		/// </summary>
		public float Width => Converter.ControlSize.Width;

		/// <summary>
		/// Indicates the drawing height.
		/// </summary>
		public float Height => Converter.ControlSize.Height;

		/// <summary>
		/// Indicates the focused cells.
		/// </summary>
		public Cells FocusedCells { get; set; }

		/// <summary>
		/// Indicates the sudoku grid used.
		/// </summary>
		public SudokuGrid Grid { get; set; }

		/// <summary>
		/// Indicates the view.
		/// </summary>
		public PresentationData? View { get; set; }

		/// <summary>
		/// Indicates the custom view.
		/// </summary>
		public PresentationData? CustomView { get; set; }

		/// <summary>
		/// Indicates all conclusions.
		/// </summary>
		public IEnumerable<Conclusion>? Conclusions { get; set; }


		/// <summary>
		/// To paint the image using the data from the current instance.
		/// </summary>
		public Bitmap Paint()
		{
			var result = new Bitmap((int)Width, (int)Height);
			using var g = Graphics.FromImage(result);

			return Paint(result, g);
		}

		/// <summary>
		/// To paint the image.
		/// </summary>
		/// <param name="bitmap">The bitmap result.</param>
		/// <param name="g">The graphics instance.</param>
		/// <returns>
		/// The return value is the same as the parameter <paramref name="bitmap"/> when
		/// this parameter is not <see langword="null"/>.
		/// </returns>
		private Bitmap Paint(Bitmap? bitmap, Graphics g)
		{
			const float offset = 6F;

			// Creates a bitmap when null.
			bitmap ??= new((int)Width, (int)Height);

			// Draw background, grid and block lines (cell borders).
			PaintBackground(g);
			PaintGridAndBlockLines(g);

			// After painted the borders, we should change some values
			// to make the digits to display more clear.
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;
			g.CompositingQuality = CompositingQuality.HighQuality;

			// Then paint other information onto the grid.
			PaintPresentationData(g, View, offset);
			PaintPresentationData(g, CustomView, offset);
			PaintFocusedCells(g);
			PaintEliminations(g, offset);
			PaintValues(g);

			// Returns the result image instance.
			return bitmap;
		}


		/// <summary>
		/// Get the font via name, size, scale and the font style.
		/// </summary>
		/// <param name="fontName">The font name.</param>
		/// <param name="size">The size.</param>
		/// <param name="scale">The scale.</param>
		/// <param name="fontStyle">The font style. The default value is <see langword="null"/>.</param>
		/// <returns>The font.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Font GetFont(string fontName, float size, decimal scale, FontStyle? fontStyle = null) =>
			new(fontName, size * (float)scale, fontStyle ?? FontStyle.Regular);


		partial void PaintBackground(Graphics g);
		partial void PaintGridAndBlockLines(Graphics g);
		partial void PaintPresentationData(Graphics g, PresentationData? view, float offset);
		partial void PaintFocusedCells(Graphics g);
		partial void PaintEliminations(Graphics g, float offset);
		partial void PaintValues(Graphics g);
		partial void PaintCells(Graphics g, ICollection<PaintingPair<int>>? cells);
		partial void PaintCandidates(Graphics g, ICollection<PaintingPair<int>>? candidates, float offset);
		partial void PaintRegions(Graphics g, ICollection<PaintingPair<int>>? regions, float offset);
		partial void PaintLinks(Graphics g, ICollection<PaintingPair<Link>>? links, float offset);
		partial void PaintDirectLines(Graphics g, ICollection<PaintingPair<(Cells, Cells)>>? directLines, float offset);
		partial void PaintStepSketches(Graphics g, ICollection<PaintingPair<(int, char)>>? stepSketch);
	}
}
