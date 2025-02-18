﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Markdown;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sudoku.CodeGen;
using Sudoku.Diagnostics;
using Sudoku.XmlDocs.Extensions;
using Sudoku.XmlDocs.Values;

namespace Sudoku.XmlDocs
{
	/// <summary>
	/// Indicates the service that outputs the solution-wide documentation comments, and converts them
	/// into the markdown files.
	/// </summary>
	[AutoGeneratePrimaryConstructor]
	public sealed partial class DocumentationCommentOutputService
	{
		/// <summary>
		/// Indicates the name of the source root path.
		/// </summary>
		private const string SourceRootName = "src";

		/// <summary>
		/// Indicates the default options.
		/// </summary>
		private const RegexOptions Options = RegexOptions.Compiled | RegexOptions.ExplicitCapture;


		/// <summary>
		/// Indicates the new line characters.
		/// </summary>
		private static readonly char[] NewLineCharacters = new[] { '\r', '\n', ' ' };

		/// <summary>
		/// Indicates the default time span.
		/// </summary>
		private static readonly TimeSpan TimeSpan = TimeSpan.FromSeconds(5);

		/// <summary>
		/// Indicates the empty chars regular expression instance.
		/// </summary>
		private static readonly Regex EmptyChars = new(@"\s*\r\n\s*///\s*", Options, TimeSpan);

		/// <summary>
		/// Indicates the leading triple slash characters "<c>///</c>" regular expression instance.
		/// </summary>
		private static readonly Regex LeadingTripleSlashes = new(@"(?<=\r\n)\s*(///\s+?)", Options, TimeSpan);


		/// <summary>
		/// Indicates the root path that stores all projects.
		/// </summary>
		public string RootPath { get; }


		/// <summary>
		/// Execute the service, and outputs the documentation files, asynchronously.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The task of the execution.</returns>
		public async Task ExecuteAsync(CancellationToken cancellationToken = default)
		{
#if CONSOLE
			Console.WriteLine("Start execution...");
#endif

			// Try to get all possible files in this whole solution.
			string[] files = (await new FileCounter(RootPath, "cs", false).CountUpAsync()).FileList.ToArray();

			// Store all possible compilations.
			var projectInfos = new (string FileName, string ProjectName, string Branch)[files.Length];
			for (int i = 0; i < files.Length; i++)
			{
				string file = files[i], dirName = Path.GetDirectoryName(file)!;
				int startIndex = dirName.IndexOf(SourceRootName) + SourceRootName.Length + 1;
				int endIndex = dirName.IndexOf('\\', startIndex) is var r and not -1 ? r : dirName.Length;

				string projectName = dirName[startIndex..endIndex];
				string branch = dirName[file.IndexOf(projectName)..];
				projectInfos[i] = (file, projectName, branch);
			}

			// Iterate on each file via the path.
			for (int i = 0; i < files.Length; i++)
			{
				string file = files[i];

#if CONSOLE
				double progress = (i + 1) * 100F / files.Length;
				Console.WriteLine($"Progress: {progress.ToString("0.00")}%");
#endif

				StringBuilder
					typeBuilder = new(),
					fieldBuilder = new(),
					primaryConstructorBuilder = new(),
					constructorBuilder = new(),
					propertyBuilder = new(),
					indexerBuilder = new(),
					eventBuilder = new(),
					methodBuilder = new(),
					operatorBuilder = new(),
					castBuilder = new();

				// Try to get the code.
				string text = await File.ReadAllTextAsync(file, cancellationToken);

				// Try to get the syntax tree.
				var tree = CSharpSyntaxTree.ParseText(text, cancellationToken: cancellationToken);

				// Try to get the syntax node of the root.
				var node = await tree.GetRootAsync(cancellationToken);

				// Try to get the type declarations.
				var typeDeclarationSyntaxes = node.DescendantNodes().OfType<TypeDeclarationSyntax>();

				// Iterate on each syntax node of declarations.
				foreach (var typeDeclaration in typeDeclarationSyntaxes)
				{
					// Gather all member information.
					// Check whether the type is a record. New we should extract its primary constructor.
					if (
						typeDeclaration is RecordDeclarationSyntax
						{
							ParameterList: var paramList
						} recordDeclaration
					)
					{
						// The record doesn't contain the primary constructor.
						if (paramList is not { Parameters: { Count: not 0 } parameters })
						{
							goto IterateOnMembers;
						}

						// Gather the primary constructor information.
					}

				IterateOnMembers:
					StringBuilder
						summaryBuilder = new StringBuilder().AppendMarkdownHeader(3, "Summary"),
						remarksBuilder = new StringBuilder().AppendMarkdownHeader(3, "Remarks"),
						returnsBuilder = new StringBuilder().AppendMarkdownHeader(3, "Returns"),
						valueBuilder = new StringBuilder().AppendMarkdownHeader(3, "Value"),
						exampleBuilder = new StringBuilder().AppendMarkdownHeader(3, "Example"),
						paramBuilder = new StringBuilder().AppendMarkdownHeader(3, "Parameters"),
						typeParamBuilder = new StringBuilder().AppendMarkdownHeader(3, "Type Parameters"),
						seeAlsoBuilder = new StringBuilder().AppendMarkdownHeader(3, "See Also"),
						exceptionBuilder = new StringBuilder().AppendMarkdownHeader(3, "Exceptions"),
						inheritDocBuilder = new StringBuilder().AppendMarkdownHeader(3, "Inherit Docs");

					typeDeclaration.VisitDocDescendants(
						summaryNodeVisitor: d => q(d, summaryBuilder),
						remarksNodeVisitor: d => q(d, remarksBuilder),
						returnsNodeVisitor: d => q(d, returnsBuilder),
						valueNodeVisitor: d => q(d, valueBuilder),
						exampleNodeVisitor: d => q(d, exampleBuilder),
						paramNodeVisitor: d => q(d, paramBuilder),
						typeParamNodeVisitor: d => q(d, typeParamBuilder),
						seeAlsoNodeVisitor: d => q(d, seeAlsoBuilder),
						exceptionNodeVisitor: d => q(d, exceptionBuilder),
						inheritDocNodeVisitor: null
					);

					typeBuilder
						.Append(summaryBuilder).AppendMarkdownNewLine()
						.Append(remarksBuilder).AppendMarkdownNewLine()
						.Append(returnsBuilder).AppendMarkdownNewLine()
						.Append(exampleBuilder).AppendMarkdownNewLine()
						.Append(typeParamBuilder).AppendMarkdownNewLine()
						.Append(inheritDocBuilder).AppendMarkdownNewLine()
						.Append(seeAlsoBuilder).AppendMarkdownNewLine();

					// Normal type (class, struct or interface). Now we should check its members.
					foreach (var memberDeclarationSyntax in typeDeclaration.GetMembers(checkNestedTypes: true))
					{
						var builderRef = getBuilder(memberDeclarationSyntax);

						// Append member title.
						builderRef.AppendMarkdownHeader(3, "Member");

						// Clear inner values.
						summaryBuilder.Clear().AppendMarkdownHeader(4, "Summary");
						remarksBuilder.Clear().AppendMarkdownHeader(4, "Remarks");
						returnsBuilder.Clear().AppendMarkdownHeader(4, "Returns");
						valueBuilder.Clear().AppendMarkdownHeader(4, "Value");
						exampleBuilder.Clear().AppendMarkdownHeader(4, "Example");
						paramBuilder.Clear().AppendMarkdownHeader(4, "Parameters");
						typeParamBuilder.Clear().AppendMarkdownHeader(4, "Type Parameters");
						seeAlsoBuilder.Clear().AppendMarkdownHeader(4, "See Also");
						exceptionBuilder.Clear().AppendMarkdownHeader(4, "Exceptions");
						inheritDocBuilder.Clear().AppendMarkdownHeader(4, "Inherit Docs");

						// Track values.
						memberDeclarationSyntax.VisitDocDescendants(
							summaryNodeVisitor: d => q(d, summaryBuilder),
							remarksNodeVisitor: d => q(d, remarksBuilder),
							returnsNodeVisitor: d => q(d, returnsBuilder),
							valueNodeVisitor: d => q(d, valueBuilder),
							exampleNodeVisitor: d => q(d, exampleBuilder),
							paramNodeVisitor: d => q(d, paramBuilder),
							typeParamNodeVisitor: d => q(d, typeParamBuilder),
							seeAlsoNodeVisitor: d => q(d, seeAlsoBuilder),
							exceptionNodeVisitor: d => q(d, exceptionBuilder),
							inheritDocNodeVisitor: null
						);

						// Append to the builder.
						builderRef
							.Append(summaryBuilder).AppendMarkdownNewLine()
							.Append(remarksBuilder).AppendMarkdownNewLine()
							.Append(returnsBuilder).AppendMarkdownNewLine()
							.Append(valueBuilder).AppendMarkdownNewLine()
							.Append(exampleBuilder).AppendMarkdownNewLine()
							.Append(paramBuilder).AppendMarkdownNewLine()
							.Append(typeParamBuilder).AppendMarkdownNewLine()
							.Append(exceptionBuilder).AppendMarkdownNewLine()
							.Append(inheritDocBuilder).AppendMarkdownNewLine()
							.Append(seeAlsoBuilder).AppendMarkdownNewLine();
					}

#if CONSOLE
					Console.WriteLine("Generate file...");
#endif

					var document = Document.Create()
						.AppendHeader(1, $"Type `{typeDeclaration.Identifier.ValueText}`")
						.AppendHeader(2, "Introduction")
						.AppendPlainText(typeBuilder.ToString())
						.AppendNewLine()
						.AppendHeader(2, "Fields")
						.AppendPlainText(fieldBuilder.ToString())
						.AppendNewLine()
						.AppendHeader(2, "Constructors")
						.AppendPlainText(constructorBuilder.ToString())
						.AppendNewLine()
						.AppendHeader(2, "Properties")
						.AppendPlainText(propertyBuilder.ToString())
						.AppendNewLine()
						.AppendHeader(2, "Indexers")
						.AppendPlainText(indexerBuilder.ToString())
						.AppendNewLine()
						.AppendHeader(2, "Events")
						.AppendPlainText(eventBuilder.ToString())
						.AppendNewLine()
						.AppendHeader(2, "Methods")
						.AppendPlainText(methodBuilder.ToString())
						.AppendNewLine()
						.AppendHeader(2, "Operators")
						.AppendPlainText(operatorBuilder.ToString())
						.AppendNewLine()
						.AppendHeader(2, "Casts")
						.AppendPlainText(castBuilder.ToString())
						.AppendNewLine()
						.Format(null);

#if CONSOLE
					Console.WriteLine("Output...");
#endif

					await document.SaveAsync(
#if DEBUG
						$@"C:\Users\Howdy\Desktop\docs\{projectInfos[i].Branch}\{typeDeclaration.Identifier.ValueText}",
#else
						$@"docs\{projectInfos[i].Branch}\{typeDeclaration.Identifier.ValueText}",
#endif
						cancellationToken
					);

#if CONSOLE
					Console.Clear();
#endif
				}


				StringBuilder getBuilder(MemberDeclarationSyntax memberDeclaration) => memberDeclaration switch
				{
					FieldDeclarationSyntax => fieldBuilder,
					ConstructorDeclarationSyntax => constructorBuilder,
					PropertyDeclarationSyntax => propertyBuilder,
					IndexerDeclarationSyntax => indexerBuilder,
					EventDeclarationSyntax => eventBuilder,
					MethodDeclarationSyntax => methodBuilder,
					OperatorDeclarationSyntax => operatorBuilder,
					ConversionOperatorDeclarationSyntax => castBuilder,
					_ => typeBuilder
				};
			}

			static void q(SyntaxList<XmlNodeSyntax> descendants, StringBuilder sb)
			{
				foreach (var descendant in descendants)
				{
					traverse(descendant, sb);
				}
			}

			static bool isWhiteOrTripleSlashOnly(XmlNodeSyntax node)
			{
				string s = node.ToString();
				var match = EmptyChars.Match(s);
				return match.Success && match.Value == s;
			}

			static bool traverse(XmlNodeSyntax descendant, StringBuilder sb)
			{
				if (isWhiteOrTripleSlashOnly(descendant))
				{
					return false;
				}

				switch (descendant)
				{
					case XmlTextSyntax { TextTokens: var tokens }:
					{
						foreach (var token in tokens)
						{
							string text = token.ValueText;
							if (!string.IsNullOrWhiteSpace(text) && text != Environment.NewLine)
							{
								sb.Append(text.TrimStart());
							}
						}

						break;
					}
					/*array-deconstruction-pattern*/
					case XmlEmptyElementSyntax
					{
						Name: { LocalName: { ValueText: var markup } },
						Attributes: var attributes
					}
					when attributes[0] is { Name: { LocalName: { ValueText: var xmlPrefixName } } } firstAttribute:
					{
						string attributeValueText = (SyntaxKind)firstAttribute.RawKind switch
						{
							SyntaxKind.XmlCrefAttribute when firstAttribute is XmlCrefAttributeSyntax
							{
								Cref: var crefNode
							} => crefNode.ToString(),
							SyntaxKind.XmlNameAttribute when firstAttribute is XmlNameAttributeSyntax
							{
								Identifier: { Identifier: { ValueText: var identifier } }
							} => identifier,
							/*array-deconstruction-pattern*/
							SyntaxKind.XmlTextAttribute when firstAttribute is XmlTextAttributeSyntax
							{
								Name: { LocalName: { ValueText: DocCommentAttributes.LangWord } },
								TextTokens: { Count: not 0 } tokenList
							} && tokenList[0] is { ValueText: var firstTokenText } => firstTokenText
						};

						switch (markup)
						{
							case DocCommentBlocks.See:
							{
								sb.AppendMarkdownInlineCodeBlock(attributeValueText);
								break;
							}
							case DocCommentBlocks.ParamRef:
							{
								sb.AppendMarkdownInlineCodeBlock(attributeValueText);
								break;
							}
							case DocCommentBlocks.TypeParamRef:
							{
								sb.AppendMarkdownInlineCodeBlock(attributeValueText);
								break;
							}
						}

						break;
					}
					case XmlElementSyntax
					{
						StartTag:
						{
							Name: { LocalName: { ValueText: var markup } },
							Attributes: var attributes
						},
						Content: var content
					}
					when content.ToString() is var contentText:
					{
						switch (markup)
						{
							/*array-deconstruction-pattern*/
							case DocCommentBlocks.List
							when attributes.Count != 0 && attributes[0] is XmlTextAttributeSyntax
							{
								Name: { LocalName: { ValueText: DocCommentAttributes.Type } },
								TextTokens: { Count: not 0 } listTypeNameTextTokens
							} && listTypeNameTextTokens[0] is { ValueText: var listTypeName }:
							{
								StringBuilder? listHeaderBuilder = null;
								switch (listTypeName)
								{
									case DocCommentValues.Table:
									{
										// Items:
										//   Allow <listheader> markup.
										//   Allow <item> markup.
										//   Allow nested <term> and <description> markup in the <item>.
										//   Allow <item> markup only. (But don't consider)

										// Primt title.
										sb.AppendLine("|---|---|").AppendLine("| Term | Description |");
										foreach (var node in content)
										{
											// Leading syntax nodes shouldn't match.
											switch (node)
											{
												case XmlTextSyntax: { continue; }
												case XmlElementSyntax
												{
													StartTag: { Name: { LocalName: { ValueText: var tagName } } },
													Content: var listHeaderContents
												}:
												{
													switch (tagName)
													{
														case DocCommentBlocks.ListHeader:
														{
															(listHeaderBuilder ??= new()).Append("<center>");

															foreach (var listHeaderContent in listHeaderContents)
															{
																switch (listHeaderContent)
																{
																	case XmlTextSyntax:
																	{
																		listHeaderBuilder.Append(listHeaderContent.ToString());

																		break;
																	}
																	/*array-deconstruction-pattern*/
																	case XmlEmptyElementSyntax
																	{
																		Name:
																		{
																			LocalName: { ValueText: DocCommentBlocks.See }
																		},
																		Attributes: { Count: 1 } langwordAttributes
																	}
																	when langwordAttributes[0] is XmlTextAttributeSyntax
																	{
																		Name:
																		{
																			LocalName:
																			{
																				ValueText: DocCommentAttributes.LangWord
																			}
																		},
																		TextTokens: { Count: not 0 } langAttributeTextTokens
																	} && langAttributeTextTokens[0] is
																	{
																		ValueText: var listHeaderNestedKeywordText
																	}:
																	{
																		sb.AppendMarkdownInlineCodeBlock(
																			listHeaderNestedKeywordText
																		);

																		break;
																	}
																}
															}

															listHeaderBuilder.Append("</center>").AppendMarkdownNewLine();

															break;
														}
														case DocCommentBlocks.Item:
														{
															var itemDescendants = node.DescendantNodes();
															/*array-deconstruction-pattern*/
															if (
																itemDescendants.OfType<XmlElementSyntax>().ToArray() is
																{
																	Length: 2
																} termAndDescriptionPair
																&& termAndDescriptionPair[0] is XmlElementSyntax
																{
																	StartTag:
																	{
																		Name:
																		{
																			LocalName: { ValueText: DocCommentBlocks.Term }
																		}
																	},
																	Content: { Count: 1 } termContents
																}
																&& termContents[0] is XmlTextSyntax
																{
																	TextTokens: var termTextTokens
																}
																&& termAndDescriptionPair[1] is XmlElementSyntax
																{
																	StartTag:
																	{
																		Name:
																		{
																			LocalName:
																			{
																				ValueText: DocCommentBlocks.Description
																			}
																		}
																	},
																	Content: var descriptionContents
																}
															)
															{
																// Item block contains both term and description markups.
																// Now integrate term and description part, and print them.
																var descriptionBuilder = new StringBuilder();
																foreach (var descriptionContent in descriptionContents)
																{
																	switch (descriptionContent)
																	{
																		case XmlTextSyntax:
																		{
																			descriptionBuilder.Append(descriptionContent);
																			break;
																		}
																		case XmlElementSyntax
																		{
																			StartTag:
																			{
																				Name:
																				{
																					LocalName:
																					{
																						ValueText: var descriptionContentTypeName
																					}
																				},
																				Attributes: var descriptionContentAttributes
																			},
																			Content: var descriptionContentInnerContent
																		}:
																		{
																			switch (descriptionContentTypeName)
																			{
																				case DocCommentBlocks.U
																				when descriptionContentAttributes.Count == 0:
																				{
																					descriptionBuilder.AppendMarkdownUnderlinedBlock(
																						descriptionContentInnerContent.ToString()
																					);

																					break;
																				}
																				case DocCommentBlocks.I
																				when descriptionContentAttributes.Count == 0:
																				{
																					descriptionBuilder.AppendMarkdownItalicBlock(
																						descriptionContentInnerContent.ToString()
																					);

																					break;
																				}
																				case DocCommentBlocks.B
																				when descriptionContentAttributes.Count == 0:
																				{
																					descriptionBuilder.AppendMarkdownBoldBlock(
																						descriptionContentInnerContent.ToString()
																					);

																					break;
																				}
																				case DocCommentBlocks.A
																				when descriptionContentAttributes.Count == 1
																				&& descriptionContentAttributes[0] is XmlTextAttributeSyntax
																				{
																					Name:
																					{
																						LocalName:
																						{
																							ValueText:
																								DocCommentAttributes.Href
																						}
																					},
																					TextTokens: var descriptionContentAttributesInnerTextTokens
																				}:
																				{
																					descriptionBuilder.AppendMarkdownHyperlink(
																						descriptionContentInnerContent.ToString(),
																						descriptionContentAttributesInnerTextTokens.ToString()
																					);

																					break;
																				}
																				case DocCommentBlocks.Del
																				when descriptionContentAttributes.Count == 0:
																				{
																					descriptionBuilder.AppendMarkdownDeleteBlock(
																						descriptionContentInnerContent.ToString()
																					);

																					break;
																				}
																			}

																			break;
																		}
																		/*array-deconstruction-pattern*/
																		case XmlEmptyElementSyntax
																		{
																			Name:
																			{
																				LocalName:
																				{
																					ValueText: DocCommentBlocks.See
																				}
																			},
																			Attributes:
																			{
																				Count: 1
																			} langWordAttributeInDescription
																		}
																		when langWordAttributeInDescription[0] is XmlTextAttributeSyntax
																		{
																			Name:
																			{
																				LocalName:
																				{
																					ValueText: DocCommentAttributes.LangWord
																				}
																			},
																			TextTokens: var langwordInDescriptionTextTokens
																		}:
																		{
																			descriptionBuilder.AppendMarkdownInlineCodeBlock(
																				langwordInDescriptionTextTokens.ToString()
																			);

																			break;
																		}
																	}
																}

																sb
																	.Append("| ")
																	.Append(termTextTokens.ToString())
																	.Append(" | ")
																	.Append(descriptionBuilder)
																	.AppendLine(" |");
															}

															break;
														}
													}

													break;
												}
											}
										}

										sb.AppendLine();

										if (listHeaderBuilder is not null)
										{
											sb.AppendLine(listHeaderBuilder.ToString());
										}

										sb.AppendLine();

										break;
									}
									case DocCommentValues.Bullet:
									{
										// Items:
										//   Disallow <listheader> markup.
										//   Disallow nested <term> and <description> markup in the <item>.
										//   Allow nested bullet list. (But don't consider)
										foreach (var node in content)
										{
											var bulletBuilder = new StringBuilder();

											switch (node)
											{
												case XmlElementSyntax
												{
													StartTag: { Name: { LocalName: { ValueText: var tagName } } },
													Content: var bulletContents
												}:
												{
													sb.Append(MarkdownSymbols.BulletListStart);

													switch (tagName)
													{
														case DocCommentBlocks.Item:
														{
															foreach (var bulletContent in bulletContents)
															{
																switch (bulletContent)
																{
																	case XmlTextSyntax:
																	{
																		bulletBuilder.Append(bulletContent);
																		break;
																	}
																	case XmlElementSyntax
																	{
																		StartTag:
																		{
																			Name:
																			{
																				LocalName:
																				{
																					ValueText: var bulletContentTypeName
																				}
																			},
																			Attributes: var bulletContentAttributes
																		},
																		Content: var bulletContentInnerContent
																	}:
																	{
																		switch (bulletContentTypeName)
																		{
																			case DocCommentBlocks.U
																			when bulletContentAttributes.Count == 0:
																			{
																				bulletBuilder.AppendMarkdownUnderlinedBlock(
																					bulletContentInnerContent.ToString()
																				);

																				break;
																			}
																			case DocCommentBlocks.I
																			when bulletContentAttributes.Count == 0:
																			{
																				bulletBuilder.AppendMarkdownItalicBlock(
																					bulletContentInnerContent.ToString()
																				);

																				break;
																			}
																			case DocCommentBlocks.B
																			when bulletContentAttributes.Count == 0:
																			{
																				bulletBuilder.AppendMarkdownBoldBlock(
																					bulletContentInnerContent.ToString()
																				);

																				break;
																			}
																			case DocCommentBlocks.A
																			when bulletContentAttributes.Count == 1
																			&& bulletContentAttributes[0] is XmlTextAttributeSyntax
																			{
																				Name:
																				{
																					LocalName:
																					{
																						ValueText: DocCommentAttributes.Href
																					}
																				},
																				TextTokens: var bulletContentAttributesInnerTextTokens
																			}:
																			{
																				bulletBuilder.AppendMarkdownHyperlink(
																					bulletContentInnerContent.ToString(),
																					bulletContentAttributesInnerTextTokens.ToString()
																				);

																				break;
																			}
																			case DocCommentBlocks.Del
																			when bulletContentAttributes.Count == 0:
																			{
																				bulletBuilder.AppendMarkdownDeleteBlock(
																					bulletContentInnerContent.ToString()
																				);

																				break;
																			}
																		}

																		break;
																	}
																	/*array-deconstruction-pattern*/
																	case XmlEmptyElementSyntax
																	{
																		Name:
																		{
																			LocalName:
																			{
																				ValueText: DocCommentBlocks.See
																			}
																		},
																		Attributes:
																		{
																			Count: 1
																		} langWordAttributeInDescription
																	}
																	when langWordAttributeInDescription[0] is XmlTextAttributeSyntax
																	{
																		Name:
																		{
																			LocalName:
																			{
																				ValueText: DocCommentAttributes.LangWord
																			}
																		},
																		TextTokens: var langwordInDescriptionTextTokens
																	}:
																	{
																		bulletBuilder.AppendMarkdownInlineCodeBlock(
																			langwordInDescriptionTextTokens.ToString()
																		);

																		break;
																	}
																}
															}

															break;
														}
													}

													break;
												}
											}

											if (bulletBuilder.ToString() is var bulletBuilderStr and not "")
											{
												sb.AppendLine(bulletBuilderStr);
											}
										}

										sb.AppendMarkdownNewLine();

										break;
									}
									case DocCommentValues.Number:
									{
										// Items:
										//   Disallow <listheader> markup.
										//   Disallow nested <term> and <description> markup in the <item>.
										//   Allow nested numbered list. (But don't consider)
										foreach (var node in content)
										{
											var numberBuilder = new StringBuilder();

											switch (node)
											{
												case XmlElementSyntax
												{
													StartTag: { Name: { LocalName: { ValueText: var tagName } } },
													Content: var bulletContents
												}:
												{
													sb.Append(MarkdownSymbols.NumberedListStart);

													switch (tagName)
													{
														case DocCommentBlocks.Item:
														{
															foreach (var bulletContent in bulletContents)
															{
																switch (bulletContent)
																{
																	case XmlTextSyntax:
																	{
																		numberBuilder.Append(bulletContent);
																		break;
																	}
																	case XmlElementSyntax
																	{
																		StartTag:
																		{
																			Name:
																			{
																				LocalName:
																				{
																					ValueText: var numberContentTypeName
																				}
																			},
																			Attributes: var numberContentAttributes
																		},
																		Content: var numberContentInnerContent
																	}:
																	{
																		switch (numberContentTypeName)
																		{
																			case DocCommentBlocks.U
																			when numberContentAttributes.Count == 0:
																			{
																				numberBuilder.AppendMarkdownUnderlinedBlock(
																					numberContentInnerContent.ToString()
																				);

																				break;
																			}
																			case DocCommentBlocks.I
																			when numberContentAttributes.Count == 0:
																			{
																				numberBuilder.AppendMarkdownItalicBlock(
																					numberContentInnerContent.ToString()
																				);

																				break;
																			}
																			case DocCommentBlocks.B
																			when numberContentAttributes.Count == 0:
																			{
																				numberBuilder.AppendMarkdownBoldBlock(
																					numberContentInnerContent.ToString()
																				);

																				break;
																			}
																			/*array-deconstruction-pattern*/
																			case DocCommentBlocks.A
																			when numberContentAttributes.Count == 1
																			&& numberContentAttributes[0] is XmlTextAttributeSyntax
																			{
																				Name:
																				{
																					LocalName:
																					{
																						ValueText: DocCommentAttributes.Href
																					}
																				},
																				TextTokens: var numberContentAttributesInnerTextTokens
																			}:
																			{
																				numberBuilder.AppendMarkdownHyperlink(
																					numberContentInnerContent.ToString(),
																					numberContentAttributesInnerTextTokens.ToString()
																				);

																				break;
																			}
																			case DocCommentBlocks.Del
																			when numberContentAttributes.Count == 0:
																			{
																				numberBuilder.AppendMarkdownDeleteBlock(
																					numberContentInnerContent.ToString()
																				);

																				break;
																			}
																		}

																		break;
																	}
																	/*array-deconstruction-pattern*/
																	case XmlEmptyElementSyntax
																	{
																		Name:
																		{
																			LocalName:
																			{
																				ValueText: DocCommentBlocks.See
																			}
																		},
																		Attributes:
																		{
																			Count: 1
																		} langWordAttributeInDescription
																	}
																	when langWordAttributeInDescription[0] is XmlTextAttributeSyntax
																	{
																		Name:
																		{
																			LocalName:
																			{
																				ValueText: DocCommentAttributes.LangWord
																			}
																		},
																		TextTokens: var langwordInDescriptionTextTokens
																	}:
																	{
																		numberBuilder.AppendMarkdownInlineCodeBlock(
																			langwordInDescriptionTextTokens.ToString()
																		);

																		break;
																	}
																}
															}

															break;
														}
													}

													break;
												}
											}

											if (numberBuilder.ToString() is var numberBuilderStr and not "")
											{
												sb.AppendLine(numberBuilderStr);
											}
										}

										sb.AppendMarkdownNewLine();

										break;
									}
								}

								break;
							}
							case DocCommentBlocks.Para when attributes.Count == 0:
							{
								foreach (var descendantInner in content)
								{
									// Handle it recursively.
									traverse(descendantInner, sb);
								}

								sb.AppendLine().AppendLine();

								break;
							}
							case DocCommentBlocks.C when attributes.Count == 0:
							{
								sb.AppendMarkdownInlineCodeBlock(contentText);
								break;
							}
							case DocCommentBlocks.U when attributes.Count == 0:
							{
								sb.AppendMarkdownUnderlinedBlock(contentText);
								break;
							}
							case DocCommentBlocks.I when attributes.Count == 0:
							{
								sb.AppendMarkdownItalicBlock(contentText);
								break;
							}
							case DocCommentBlocks.B when attributes.Count == 0:
							{
								sb.AppendMarkdownBoldBlock(contentText);
								break;
							}
							case DocCommentBlocks.Del when attributes.Count == 0:
							{
								sb.AppendMarkdownDeleteBlock(contentText);
								break;
							}
							/*array-deconstruction-pattern*/
							case DocCommentBlocks.A
							when attributes is { Count: 1 } && attributes[0] is XmlTextAttributeSyntax
							{
								Name: { LocalName: { ValueText: DocCommentAttributes.Href } },
								TextTokens: var hyperLinkTextTokens
							}:
							{
								sb.AppendMarkdownHyperlink(hyperLinkTextTokens.ToString(), contentText);

								break;
							}
							case DocCommentBlocks.Code when attributes.Count == 0:
							{
								// Trimming. We should remove all unncessary text.
								contentText = LeadingTripleSlashes
									.Replace(contentText, removeMatchItems)
									.Trim(NewLineCharacters);

								if (sb.ToString() != string.Empty)
								{
									// If the context contains any characters, we should turn to a new line
									// to output the code block.
									sb.AppendMarkdownNewLine();
								}

								sb.AppendMarkdownCodeBlock(contentText, "csharp");

								break;

								static string removeMatchItems(Match _) => string.Empty;
							}
						}

						break;
					}
				}

				return true;
			}
		}
	}
}
