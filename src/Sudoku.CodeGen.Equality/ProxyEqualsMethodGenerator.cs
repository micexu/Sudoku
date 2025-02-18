﻿#pragma warning disable IDE0057

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Sudoku.CodeGen.Equality.Extensions;

namespace Sudoku.CodeGen.Equality
{
	/// <summary>
	/// Indicates a generator that generates the code about the equality method.
	/// </summary>
	[Generator]
	public sealed partial class ProxyEqualsMethodGenerator : ISourceGenerator
	{
		/// <inheritdoc/>
		public void Execute(GeneratorExecutionContext context)
		{
			if (context.SyntaxReceiver is not SyntaxReceiver receiver)
			{
				return;
			}

			var nameDic = new Dictionary<string, int>();
			var processedList = new List<INamedTypeSymbol>();
			foreach (var symbol in g(context, receiver))
			{
				if (processedList.Contains(symbol, SymbolEqualityComparer.Default))
				{
					continue;
				}

				_ = nameDic.TryGetValue(symbol.Name, out int i);
				string name = i == 0 ? symbol.Name : $"{symbol.Name}{(i + 1).ToString()}";
				nameDic[symbol.Name] = i + 1;

				if (getEqualityMethodsCode(context, symbol) is { } c)
				{
					context.AddSource($"{name}.ProxyEquality.g.cs", c);

					processedList.Add(symbol);
				}
			}

			static IEnumerable<INamedTypeSymbol> g(in GeneratorExecutionContext context, SyntaxReceiver receiver)
			{
				var compilation = context.Compilation;

				return
					from candidate in receiver.Candidates
					let model = compilation.GetSemanticModel(candidate.SyntaxTree)
					select (INamedTypeSymbol)model.GetDeclaredSymbol(candidate)! into symbol
					from member in symbol.GetMembers().OfType<IMethodSymbol>()
					where member.Marks<ProxyEqualityAttribute>()
					let boolSymbol = compilation.GetSpecialType(SpecialType.System_Boolean)
					let returnTypeSymbol = member.ReturnType
					where SymbolEqualityComparer.Default.Equals(returnTypeSymbol, boolSymbol)
					let parameters = member.Parameters
					where parameters.Length == 2 && parameters.All(p => SymbolEqualityComparer.Default.Equals(p.Type, symbol))
					select symbol;
			}

			static string? getEqualityMethodsCode(in GeneratorExecutionContext context, INamedTypeSymbol symbol)
			{
				var methodSymbol = (
					from member in symbol.GetMembers().OfType<IMethodSymbol>()
					where member.Marks<ProxyEqualityAttribute>()
					select member
				).First();

				/*slice-pattern*/
				if (
					symbol.IsReferenceType && (
						methodSymbol.Parameters[0].NullableAnnotation != NullableAnnotation.Annotated
						|| methodSymbol.Parameters[1].NullableAnnotation != NullableAnnotation.Annotated
					)
				)
				{
					return null;
				}

				string namespaceName = symbol.ContainingNamespace.ToDisplayString();
				string fullTypeName = symbol.ToDisplayString(FormatOptions.TypeFormat);
				int i = fullTypeName.IndexOf('<');
				string genericParametersList = i == -1 ? string.Empty : fullTypeName.Substring(i);
				int j = fullTypeName.IndexOf('>');
				string genericParametersListWithoutConstraint = j == -1 ? string.Empty : fullTypeName.Substring(i, j - i + 1);
				string typeName = symbol.Name;
				string typeKind = symbol switch
				{
					{ IsRecord: true } => "record",
					{ TypeKind: TypeKind.Class } => "class",
					{ TypeKind: TypeKind.Struct } => "struct"
				};

				string readonlyModifier = methodSymbol.IsReadOnly && !symbol.IsReadOnly
					? "readonly "
					: string.Empty;
				string methodName = methodSymbol.Name;
				string inModifier = symbol.TypeKind == TypeKind.Struct ? "in " : string.Empty;
				string nullableMark = symbol.TypeKind == TypeKind.Class || symbol.IsRecord ? "?" : string.Empty;
				string objectEqualityMethod = symbol.IsRefLikeType
					? "// This type is a ref struct, so 'bool Equals(object?) is useless."
					: $@"[CompilerGenerated]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override {readonlyModifier}bool Equals(object? obj) => obj is {typeName}{genericParametersListWithoutConstraint} comparer && {methodName}(this, comparer);";

				return $@"#pragma warning disable 1591

using System.Runtime.CompilerServices;

#nullable enable

namespace {namespaceName}
{{
	partial {typeKind} {symbol.Name}{genericParametersList}
	{{
		{objectEqualityMethod}

		[CompilerGenerated]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals({inModifier}{typeName}{genericParametersListWithoutConstraint}{nullableMark} other) => {methodName}(this, other);


		[CompilerGenerated]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==({inModifier}{typeName}{genericParametersListWithoutConstraint} left, {inModifier}{typeName}{genericParametersListWithoutConstraint} right) => {methodName}(left, right);

		[CompilerGenerated]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=({inModifier}{typeName}{genericParametersListWithoutConstraint} left, {inModifier}{typeName}{genericParametersListWithoutConstraint} right) => !(left == right);
	}}
}}";
			}
		}

		/// <inheritdoc/>
		public void Initialize(GeneratorInitializationContext context) =>
			context.RegisterForSyntaxNotifications(static () => new SyntaxReceiver());
	}
}
