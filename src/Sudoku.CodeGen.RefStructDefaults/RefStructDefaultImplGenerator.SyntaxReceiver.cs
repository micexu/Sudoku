﻿using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sudoku.CodeGen.RefStructDefaults
{
	public sealed partial class RefStructDefaultImplGenerator
	{
		/// <summary>
		/// Defines the syntax receiver.
		/// </summary>
		private sealed class SyntaxReceiver : ISyntaxReceiver
		{
			/// <summary>
			/// Indicates all possible candidate types used.
			/// </summary>
			public IList<StructDeclarationSyntax> CandidateRefStructs { get; } = new List<StructDeclarationSyntax>();


			/// <inheritdoc/>
			public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
			{
				if (syntaxNode is StructDeclarationSyntax { Modifiers: { Count: not 0 } modifiers } declaration
					&& modifiers.Any(SyntaxKind.RefKeyword) && modifiers.Any(SyntaxKind.PartialKeyword))
				{
					CandidateRefStructs.Add(declaration);
				}
			}
		}
	}
}
