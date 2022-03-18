﻿//HintName: ParseExtensions.g.cs
// <auto-generated/>
#nullable enable

using System.Diagnostics.Contracts;
using Funcky.Internal;
using Funcky.Monads;

namespace Funcky.Extensions
{
    public static partial class ParseExtensions
    {
        public static partial Option<TEnum> ParseEnumOrNone<TEnum>(this string candidate)
            where TEnum : struct => Enum.TryParse(candidate, out var result) ? result : Option<TEnum>.None();
    }
}