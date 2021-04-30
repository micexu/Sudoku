﻿using System;
using System.Drawing;
using Sudoku.CodeGen;

[assembly: CLSCompliant(false)]

[module: AutoDeconstructExtension(typeof(Color), nameof(Color.A), nameof(Color.R), nameof(Color.G), nameof(Color.B))]
[module: AutoDeconstructExtension(typeof(Point), nameof(Point.X), nameof(Point.Y))]
[module: AutoDeconstructExtension(typeof(PointF), nameof(PointF.X), nameof(PointF.Y))]
[module: AutoDeconstructExtension(typeof(Size), nameof(Size.Width), nameof(Size.Height))]
[module: AutoDeconstructExtension(typeof(SizeF), nameof(SizeF.Width), nameof(SizeF.Height))]
[module: AutoDeconstructExtension(typeof(RectangleF), nameof(RectangleF.X), nameof(RectangleF.Y), nameof(RectangleF.Width), nameof(RectangleF.Height))]