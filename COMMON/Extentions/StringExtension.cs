using System;
namespace COMMON;

    public static class StringExtension
    {
        public static string CapFirst(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }
            return char.ToUpper(input[0]) + input.Substring(1);
        }
    }