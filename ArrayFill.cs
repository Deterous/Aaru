// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : ArrayFill.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Helpers.
//
// --[ Description ] ----------------------------------------------------------
//
//     Fills an array with a specified value.
//
// --[ License ] --------------------------------------------------------------
//
//     No license specified by creator.
//
//     Published on https://github.com/mykohsu/Extensions/blob/master/ArrayExtensions.cs
//
//     Assuming open source.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2019 Natalia Portillo
// Copyright(C) 2014 mykohsu
// ****************************************************************************/

using System;
using System.Text;

namespace DiscImageChef
{
    public static partial class ArrayHelpers
    {
        /// <summary>
        ///     Fills an array with the specified value
        /// </summary>
        /// <param name="destinationArray">Array</param>
        /// <param name="value">Value</param>
        /// <typeparam name="T">Array type</typeparam>
        public static void ArrayFill<T>(T[] destinationArray, T value)
        {
            // if called with a single value, wrap the value in an array and call the main function
            ArrayFill(destinationArray, new[] {value});
        }

        /// <summary>
        ///     Fills an array with the contents of the specified array
        /// </summary>
        /// <param name="destinationArray">Array</param>
        /// <param name="value">Value</param>
        /// <typeparam name="T">Array type</typeparam>
        public static void ArrayFill<T>(T[] destinationArray, T[] value)
        {
            if(destinationArray == null) throw new ArgumentNullException(nameof(destinationArray));

            if(value.Length > destinationArray.Length)
                throw new ArgumentException("Length of value array must not be more than length of destination");

            // set the initial array value
            Array.Copy(value, destinationArray, value.Length);

            int arrayToFillHalfLength = destinationArray.Length / 2;
            int copyLength;

            for(copyLength = value.Length; copyLength < arrayToFillHalfLength; copyLength <<= 1)
                Array.Copy(destinationArray, 0, destinationArray, copyLength, copyLength);

            Array.Copy(destinationArray, 0, destinationArray, copyLength, destinationArray.Length - copyLength);
        }

        /// <summary>
        ///     Converts a byte array to its hexadecimal representation
        /// </summary>
        /// <param name="array">Byte array</param>
        /// <param name="upper"><c>true</c> to use uppercase</param>
        /// <returns></returns>
        public static string ByteArrayToHex(byte[] array, bool upper = false)
        {
            StringBuilder sb = new StringBuilder();
            for(long i = 0; i < array.LongLength; i++) sb.AppendFormat("{0:x2}", array[i]);

            return upper ? sb.ToString().ToUpper() : sb.ToString();
        }
    }
}