// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Marshal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Helpers.
//
// --[ Description ] ----------------------------------------------------------
//
//     Provides marshalling for binary data.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as
//     published by the Free Software Foundation; either version 2.1 of the
//     License, or (at your option) any later version.
//
//     This library is distributed in the hope that it will be useful, but
//     WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//     Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public
//     License along with this library; if not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2019 Natalia Portillo
// ****************************************************************************/

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DiscImageChef.Helpers
{
    /// <summary>Provides methods to marshal binary data into C# structs</summary>
    public static class Marshal
    {
        static int count;

        /// <summary>
        ///     Marshal little-endian binary data to a structure
        /// </summary>
        /// <param name="bytes">Byte array containing the binary data</param>
        /// <typeparam name="T">Type of the structure to marshal</typeparam>
        /// <returns>The binary data marshalled in a structure with the specified type</returns>
        public static T ByteArrayToStructureLittleEndian<T>(byte[] bytes) where T : struct
        {
            GCHandle ptr = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T str =
                (T)System.Runtime.InteropServices.Marshal.PtrToStructure(ptr.AddrOfPinnedObject(), typeof(T));
            ptr.Free();
            return str;
        }

        /// <summary>
        ///     Marshal big-endian binary data to a structure
        /// </summary>
        /// <param name="bytes">Byte array containing the binary data</param>
        /// <typeparam name="T">Type of the structure to marshal</typeparam>
        /// <returns>The binary data marshalled in a structure with the specified type</returns>
        public static T ByteArrayToStructureBigEndian<T>(byte[] bytes) where T : struct
        {
            GCHandle ptr = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            object str =
                (T)System.Runtime.InteropServices.Marshal.PtrToStructure(ptr.AddrOfPinnedObject(), typeof(T));
            ptr.Free();
            return (T)SwapStructureMembersEndian(str);
        }

        /// <summary>
        ///     Marshal PDP-11 binary data to a structure
        /// </summary>
        /// <param name="bytes">Byte array containing the binary data</param>
        /// <typeparam name="T">Type of the structure to marshal</typeparam>
        /// <returns>The binary data marshalled in a structure with the specified type</returns>
        public static T ByteArrayToStructurePdpEndian<T>(byte[] bytes) where T : struct
        {
            {
                GCHandle ptr = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                object str =
                    (T)System.Runtime.InteropServices.Marshal.PtrToStructure(ptr.AddrOfPinnedObject(), typeof(T));
                ptr.Free();
                return (T)SwapStructureMembersEndianPdp(str);
            }
        }

        /// <summary>
        ///     Marshal little-endian binary data to a structure. If the structure type contains any non value type, this method
        ///     will crash.
        /// </summary>
        /// <param name="bytes">Byte array containing the binary data</param>
        /// <typeparam name="T">Type of the structure to marshal</typeparam>
        /// <returns>The binary data marshalled in a structure with the specified type</returns>
        public static T SpanToStructureLittleEndian<T>(ReadOnlySpan<byte> bytes) where T : struct =>
            MemoryMarshal.Read<T>(bytes);

        /// <summary>
        ///     Marshal big-endian binary data to a structure. If the structure type contains any non value type, this method will
        ///     crash.
        /// </summary>
        /// <param name="bytes">Byte array containing the binary data</param>
        /// <typeparam name="T">Type of the structure to marshal</typeparam>
        /// <returns>The binary data marshalled in a structure with the specified type</returns>
        public static T SpanToStructureBigEndian<T>(ReadOnlySpan<byte> bytes) where T : struct
        {
            T str = SpanToStructureLittleEndian<T>(bytes);
            return (T)SwapStructureMembersEndian(str);
        }

        /// <summary>
        ///     Marshal PDP-11 binary data to a structure. If the structure type contains any non value type, this method will
        ///     crash.
        /// </summary>
        /// <param name="bytes">Byte array containing the binary data</param>
        /// <typeparam name="T">Type of the structure to marshal</typeparam>
        /// <returns>The binary data marshalled in a structure with the specified type</returns>
        public static T SpanToStructurePdpEndian<T>(ReadOnlySpan<byte> bytes) where T : struct
        {
            object str = SpanToStructureLittleEndian<T>(bytes);
            return (T)SwapStructureMembersEndianPdp(str);
        }

        /// <summary>
        ///     Marshal a structure depending on the decoration of <see cref="MarshallingPropertiesAttribute" />. If the decoration
        ///     is not present it will marshal as a reference type containing little endian structure.
        /// </summary>
        /// <param name="bytes">Byte array containing the binary data</param>
        /// <typeparam name="T">Type of the structure to marshal</typeparam>
        /// <returns>The binary data marshalled in a structure with the specified type</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     The <see cref="MarshallingPropertiesAttribute" /> contains an unsupported
        ///     endian
        /// </exception>
        public static T MarshalStructure<T>(byte[] bytes) where T : struct
        {
            if(!(typeof(T).GetCustomAttribute(typeof(MarshallingPropertiesAttribute)) is MarshallingPropertiesAttribute
                     properties)) return ByteArrayToStructureLittleEndian<T>(bytes);

            switch(properties.Endian)
            {
                case BitEndian.Little:
                    return properties.HasReferences
                               ? ByteArrayToStructureLittleEndian<T>(bytes)
                               : SpanToStructureLittleEndian<T>(bytes);

                    break;
                case BitEndian.Big:
                    return properties.HasReferences
                               ? ByteArrayToStructureBigEndian<T>(bytes)
                               : SpanToStructureBigEndian<T>(bytes);

                    break;

                case BitEndian.Pdp:
                    return properties.HasReferences
                               ? ByteArrayToStructurePdpEndian<T>(bytes)
                               : SpanToStructurePdpEndian<T>(bytes);
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Swaps all members of a structure
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static object SwapStructureMembersEndian(object str)
        {
            Type        t         = str.GetType();
            FieldInfo[] fieldInfo = t.GetFields();
            foreach(FieldInfo fi in fieldInfo)
                if(fi.FieldType == typeof(short))
                {
                    short x = (short)fi.GetValue(str);
                    fi.SetValue(str, (short)((x << 8) | ((x >> 8) & 0xFF)));
                }
                else if(fi.FieldType == typeof(int))
                {
                    int x = (int)fi.GetValue(str);
                    x = (int)(((x                   << 8) & 0xFF00FF00) | (((uint)x >> 8) & 0xFF00FF));
                    fi.SetValue(str, (int)(((uint)x << 16) | (((uint)x >> 16) & 0xFFFF)));
                }
                else if(fi.FieldType == typeof(long))
                {
                    long x = (long)fi.GetValue(str);
                    x = ((x & 0x00000000FFFFFFFF) << 32) | (long)(((ulong)x & 0xFFFFFFFF00000000) >> 32);
                    x = ((x & 0x0000FFFF0000FFFF) << 16) | (long)(((ulong)x & 0xFFFF0000FFFF0000) >> 16);
                    x = ((x & 0x00FF00FF00FF00FF) << 8)  | (long)(((ulong)x & 0xFF00FF00FF00FF00) >> 8);

                    fi.SetValue(str, x);
                }
                else if(fi.FieldType == typeof(ushort))
                {
                    ushort x = (ushort)fi.GetValue(str);
                    fi.SetValue(str, (ushort)((x << 8) | (x >> 8)));
                }
                else if(fi.FieldType == typeof(uint))
                {
                    uint x = (uint)fi.GetValue(str);
                    x = ((x             << 8) & 0xFF00FF00) | ((x >> 8) & 0xFF00FF);
                    fi.SetValue(str, (x << 16) | (x               >> 16));
                }
                else if(fi.FieldType == typeof(ulong))
                {
                    ulong x = (ulong)fi.GetValue(str);
                    x = ((x & 0x00000000FFFFFFFF) << 32) | ((x & 0xFFFFFFFF00000000) >> 32);
                    x = ((x & 0x0000FFFF0000FFFF) << 16) | ((x & 0xFFFF0000FFFF0000) >> 16);
                    x = ((x & 0x00FF00FF00FF00FF) << 8)  | ((x & 0xFF00FF00FF00FF00) >> 8);
                    fi.SetValue(str, x);
                }
                else if(fi.FieldType == typeof(float))
                {
                    float  flt   = (float)fi.GetValue(str);
                    byte[] flt_b = BitConverter.GetBytes(flt);
                    fi.SetValue(str, BitConverter.ToSingle(new[] {flt_b[3], flt_b[2], flt_b[1], flt_b[0]}, 0));
                }
                else if(fi.FieldType == typeof(double))
                {
                    double dbl   = (double)fi.GetValue(str);
                    byte[] dbl_b = BitConverter.GetBytes(dbl);
                    fi.SetValue(str,
                                BitConverter
                                   .ToDouble(new[] {dbl_b[7], dbl_b[6], dbl_b[5], dbl_b[4], dbl_b[3], dbl_b[2], dbl_b[1], dbl_b[0]},
                                             0));
                }
                else if(fi.FieldType == typeof(byte) || fi.FieldType == typeof(sbyte))
                {
                    // Do nothing, can't byteswap them!
                }
                else if(fi.FieldType == typeof(Guid))
                {
                    // TODO: Swap GUID
                }
                // TODO: Swap arrays and enums
                else if(fi.FieldType.IsValueType && !fi.FieldType.IsEnum && !fi.FieldType.IsArray)
                {
                    object obj  = fi.GetValue(str);
                    object strc = SwapStructureMembersEndian(obj);
                    fi.SetValue(str, strc);
                }

            return str;
        }

        public static object SwapStructureMembersEndianPdp(object str)
        {
            Type        t         = str.GetType();
            FieldInfo[] fieldInfo = t.GetFields();
            foreach(FieldInfo fi in fieldInfo)
                if(fi.FieldType == typeof(short) || fi.FieldType == typeof(long)  || fi.FieldType == typeof(ushort) ||
                   fi.FieldType == typeof(ulong) || fi.FieldType == typeof(float) || fi.FieldType == typeof(double) ||
                   fi.FieldType == typeof(byte)  || fi.FieldType == typeof(sbyte) || fi.FieldType == typeof(Guid))
                {
                    // Do nothing
                }
                else if(fi.FieldType == typeof(int))
                {
                    int x = (int)fi.GetValue(str);
                    fi.SetValue(str, ((x & 0xffff) << 16) | ((x & 0xffff0000) >> 16));
                }
                else if(fi.FieldType == typeof(uint))
                {
                    uint x = (uint)fi.GetValue(str);
                    fi.SetValue(str, ((x & 0xffff) << 16) | ((x & 0xffff0000) >> 16));
                }
                // TODO: Swap arrays and enums
                else if(fi.FieldType.IsValueType && !fi.FieldType.IsEnum && !fi.FieldType.IsArray)
                {
                    System.Console.WriteLine("PDP {0}", count++);

                    object obj  = fi.GetValue(str);
                    object strc = SwapStructureMembersEndianPdp(obj);
                    fi.SetValue(str, strc);
                }

            return str;
        }
    }
}