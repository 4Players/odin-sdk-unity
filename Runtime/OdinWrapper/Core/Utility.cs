﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdinNative.Core
{
    public static class Utility
    {
        /// <summary>
        /// Representative ErrorCode for Ok.
        /// </summary>
        public const uint OK = 0;

        public static int RateToSamples(MediaSampleRate sampleRate = MediaSampleRate.Hz48000, int milliseconds = 20)
        {
            return ((int)sampleRate / 1000) * milliseconds;
        }

        internal static class Require
        {
            public static void NotNull<T>(string name, T t)
                where T : class
            {
                if (t == null) throw new ArgumentNullException(name);
            }

            public static void EntriesNotNull<T>(string name, T[] array)
            {
                if (array != null)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (array[i] == null) throw new ArgumentNullException(GetIndexedName(name, i));
                    }
                }
            }

            public static void NotNullOrEmpty(string name, string value)
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException("must not be null or empty", name);
            }
            public static void NotNullOrEmpty<T>(string name, T[] array)
            {
                if (array == null || array.Length == 0)
                    throw new ArgumentNullException("must not be null or empty", name);
            }

            internal static void ValidRange<T>(string nameArray, string nameOffset, string nameCount, T[] buffer, int offset, int count)
            {
                if (buffer != null)
                {
                    if (offset < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameOffset, nameOffset + " must not be negative.");
                    }
                    else if (count < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameCount, nameCount + " must not be negative.");
                    }
                    else if (offset + count > buffer.Length)
                    {
                        string message = $"{nameOffset} and {nameCount} were out of bounds for the array or {nameCount} is greater than the number of elements from {nameOffset}  to the end of {nameArray}.";
                        throw new ArgumentOutOfRangeException(message);
                    }
                }
            }

            internal static void ValidRange<T>(string nameArray, string nameCount, T[] buffer, int count)
            {
                if (buffer != null)
                {
                    if (count < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameCount, nameCount + " must not be negative.");
                    }
                    else if (count > buffer.Length)
                    {
                        string message = $"{nameCount} is greater than the number of elements of {nameArray}.";
                        throw new ArgumentOutOfRangeException(message);
                    }
                }
            }

            private static string GetIndexedName(string name, int index)
            {
                return name + '[' + index + ']';
            }
        }
    }
}
