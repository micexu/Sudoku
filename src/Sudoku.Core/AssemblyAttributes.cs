﻿using System;
using System.Runtime.CompilerServices;

[assembly: CLSCompliant(false)]

[assembly: InternalsVisibleTo("Sudoku.Bot")]
[assembly: InternalsVisibleTo("Sudoku.CodeAnalysis")]
[assembly: InternalsVisibleTo("Sudoku.Diagnostics")]
[assembly: InternalsVisibleTo("Sudoku.Drawing")]
[assembly: InternalsVisibleTo("Sudoku.Painting")]
[assembly: InternalsVisibleTo("Sudoku.Solving")]
[assembly: InternalsVisibleTo("Sudoku.Test")]
[assembly: InternalsVisibleTo("Sudoku.Windows")]
[assembly: InternalsVisibleTo("Sudoku.UI")]

[module: SkipLocalsInit]