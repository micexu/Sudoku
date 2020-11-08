﻿using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Sudoku.Globalization;
using static System.Reflection.BindingFlags;
using static System.StringSplitOptions;

namespace Sudoku.Windows
{
	/// <summary>
	/// Indicates the resources used later but not in the namespace <see cref="Windows"/>.
	/// </summary>
	/// <seealso cref="Windows"/>
	public static partial class Resources
	{
		/// <summary>
		/// The field that means the field is <see langword="static"/> and non-<see langword="public"/>.
		/// </summary>
		private const BindingFlags StaticNonpublic = NonPublic | Static;


		/// <summary>
		/// Indicates the current source.
		/// </summary>
		private static IDictionary<string, string> _dicPointer = null!;


		/// <summary>
		/// Indicates the current country code.
		/// </summary>
		public static CountryCode CountryCode { get; private set; } = CountryCode.EnUs;


		/// <summary>
		/// To change the current language with the specified country code.
		/// </summary>
		/// <param name="countryCode">The country code.</param>
		public static void ChangeLanguage(CountryCode countryCode) => GetDictionary(CountryCode = countryCode);

		/// <summary>
		/// Get the value with the specified key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>The value.</returns>
		/// <exception cref="KeyNotFoundException">
		/// Throws when the key can't be found in neither the current language dictionary
		/// nor the default dictionary.
		/// </exception>
		public static string GetValue(string key) =>
			_dicPointer.TryGetValue(key, out string? result) || LangSourceEnUs.TryGetValue(key, out result)
			? result
			: throw new KeyNotFoundException();

		/// <summary>
		/// Get the value with the specified key, without any exception throws.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>
		/// The value. If the key can't be found in neither the current language dictionary
		/// nor the default dictionary, the return value will be <see langword="null"/>.
		/// </returns>
		public static string? GetValueWithoutExceptions(string key) =>
			_dicPointer.TryGetValue(key, out string? result) || LangSourceEnUs.TryGetValue(key, out result)
			? result
			: null;

		/// <summary>
		/// Get the dictionary with the specified globalization string.
		/// </summary>
		/// <param name="countryCode">The country code.</param>
		private static void GetDictionary(CountryCode countryCode)
		{
			if (countryCode == CountryCode.Default)
			{
				return;
			}

			// The implementation of the merged dictionary that is the same as the windows
			// is too difficult... Here we use reflection to implement this.
			string[] z = NameAttribute.GetName(countryCode)!.Split('-', RemoveEmptyEntries);
			var sb = new StringBuilder();
			for (int i = 0; i < z.Length; i++)
			{
				unsafe
				{
					fixed (char* c = z[i])
					{
						if (*c is >= 'a' and <= 'z')
						{
							// To upper case.
							*c &= (char)0x5F;
						}
					}
				}

				sb.Append(z[i]);
			}

			_dicPointer =
				typeof(Resources).GetField($"LangSource{sb}", StaticNonpublic)?.GetValue(null)
				as IDictionary<string, string> ?? LangSourceEnUs;
		}
	}
}
