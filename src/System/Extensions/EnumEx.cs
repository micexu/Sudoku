﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Extensions
{
	/// <summary>
	/// Provides extension methods on <see cref="Enum"/>.
	/// </summary>
	/// <seealso cref="Enum"/>
	public static class EnumEx
	{
		/// <summary>
		/// Checks whether the current enumeration field is a flag.
		/// </summary>
		/// <typeparam name="TEnum">The type of the current field.</typeparam>
		/// <param name="this">The current field to check.</param>
		/// <returns>A <see cref="bool"/> result indicating that.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe bool IsFlag<TEnum>(this TEnum @this) where TEnum : unmanaged, Enum
		{
			switch (sizeof(TEnum))
			{
				case 1:
				case 2:
				case 4:
				{
					int l = Unsafe.As<TEnum, int>(ref @this);
					return (l & l - 1) == 0;
				}
				case 8:
				{
					long l = Unsafe.As<TEnum, long>(ref @this);
					return (l & l - 1) == 0;
				}
				default:
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Check which enumeration field is less.
		/// </summary>
		/// <typeparam name="TEnum">The type of the enumeration field to compare.</typeparam>
		/// <param name="left">The left one.</param>
		/// <param name="right">The right one.</param>
		/// <returns>The comparison result.</returns>
		public static unsafe TEnum Min<TEnum>(TEnum left, TEnum right) where TEnum : unmanaged, Enum
		{
			switch (sizeof(TEnum))
			{
				case 1:
				case 2:
				case 4:
				{
					int l = Unsafe.As<TEnum, int>(ref left);
					int r = Unsafe.As<TEnum, int>(ref right);
					return l < r ? left : right;
				}
				case 8:
				{
					long l = Unsafe.As<TEnum, long>(ref left);
					long r = Unsafe.As<TEnum, long>(ref right);
					return l < r ? left : right;
				}
				default:
				{
					return default;
				}
			}
		}

		/// <summary>
		/// Check which enumeration field is greater.
		/// </summary>
		/// <typeparam name="TEnum">The type of the enumeration field to compare.</typeparam>
		/// <param name="left">The left one.</param>
		/// <param name="right">The right one.</param>
		/// <returns>The comparison result.</returns>
		public static unsafe TEnum Max<TEnum>(TEnum left, TEnum right) where TEnum : unmanaged, Enum
		{
			switch (sizeof(TEnum))
			{
				case 1:
				case 2:
				case 4:
				{
					int l = Unsafe.As<TEnum, int>(ref left);
					int r = Unsafe.As<TEnum, int>(ref right);
					return l > r ? left : right;
				}
				case 8:
				{
					long l = Unsafe.As<TEnum, long>(ref left);
					long r = Unsafe.As<TEnum, long>(ref right);
					return l > r ? left : right;
				}
				default:
				{
					return default;
				}
			}
		}

		/// <summary>
		/// To get all possible flags from a specified enumeration instance.
		/// </summary>
		/// <typeparam name="TEnum">The type of that enumeration.</typeparam>
		/// <param name="this">The field.</param>
		/// <returns>
		/// All flags. If the enumeration field doesn't contain any flags,
		/// the return value will be <see langword="null"/>.
		/// </returns>
		public static unsafe TEnum[]? GetAllFlags<TEnum>(this TEnum @this) where TEnum : unmanaged, Enum
		{
			// Create a buffer to record all possible flags.
			var buffer = stackalloc TEnum[Enum.GetValues<TEnum>().Length];
			int i = 0;
			foreach (var flag in @this)
			{
				buffer[i++] = flag;
			}

			if (i == 0)
			{
				return null;
			}

			// Returns the instance and copy the values.
			var result = new TEnum[i];
			fixed (TEnum* ptr = result)
			{
				Unsafe.CopyBlock(ptr, buffer, (uint)(sizeof(TEnum) * i));
			}

			// Returns the value.
			return result;
		}

		/// <summary>
		/// Get all possible flags that the current enumeration field set.
		/// </summary>
		/// <typeparam name="TEnum">The type of the enumeration.</typeparam>
		/// <param name="this">The current enumeration type instance.</param>
		/// <returns>All flags.</returns>
		public static IEnumerator<TEnum> GetEnumerator<TEnum>(this TEnum @this) where TEnum : unmanaged, Enum
		{
			unsafe
			{
				return typeof(TEnum).IsDefined<FlagsAttribute>()
					? inner(@this, sizeof(TEnum))
					: ((IEnumerable<TEnum>)Array.Empty<TEnum>()).GetEnumerator();
			}

			static IEnumerator<TEnum> inner(TEnum @this, int size)
			{
				var array = Enum.GetValues<TEnum>();
				for (int index = 0, length = array.Length; index < length; index++)
				{
					var field = array[index];
					switch (size)
					{
						case 1:
						case 2:
						case 4:
						{
							int i = Unsafe.As<TEnum, int>(ref field);
							if (i == 0 || (i & i - 1) != 0) continue; else break;
						}
						case 8:
						{
							long l = Unsafe.As<TEnum, long>(ref field);
							if (l == 0 || (l & l - 1L) != 0) continue; else break;
						}
					}

					if (@this.Flags(field))
					{
						yield return field;
					}
				}
			}
		}

		/// <inheritdoc cref="Enum.HasFlag(Enum)"/>
		/// <typeparam name="TEnum">The type of the enumeration.</typeparam>
		/// <param name="this">The current enumeration type instance.</param>
		/// <param name="other">The other instance to check.</param>
		/// <exception cref="ArgumentException">Throws when the used bytes aren't 1, 2 or 4.</exception>
		/// <remarks>
		/// This method is same as <see cref="Enum.HasFlag(Enum)"/>, but without boxing and unboxing operations.
		/// </remarks>
		/// <seealso cref="Enum.HasFlag(Enum)"/>
		public static unsafe bool Flags<TEnum>(this TEnum @this, TEnum other) where TEnum : unmanaged, Enum
		{
			switch (sizeof(TEnum))
			{
				case 1:
				case 2:
				case 4:
				{
					int otherValue = Unsafe.As<TEnum, int>(ref other);
					return (Unsafe.As<TEnum, int>(ref @this) & otherValue) == otherValue;
				}
				case 8:
				{
					long otherValue = Unsafe.As<TEnum, long>(ref other);
					return (Unsafe.As<TEnum, long>(ref @this) & otherValue) == otherValue;
				}
				default:
				{
					throw new ArgumentException("The parameter should be one of the values 1, 2, 4.", nameof(@this));
				}
			}
		}

		/// <summary>
		/// Determines whether the instance has the flags specified as <paramref name="flags"/>.
		/// </summary>
		/// <typeparam name="TEnum">The type of the enumeration field.</typeparam>
		/// <param name="this">The instance.</param>
		/// <param name="flags">All flags used.</param>
		/// <returns>A <see cref="bool"/> result.</returns>
		public static unsafe bool MultiFlags<TEnum>(this TEnum @this, TEnum flags) where TEnum : unmanaged, Enum
		{
			foreach (var flag in flags)
			{
				if (@this.Flags(flag))
				{
					return true;
				}
			}

			return false;
		}
	}
}
