﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Register.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Core algorithms.
//
// --[ Description ] ----------------------------------------------------------
//
//     Registers all plugins in this assembly.
//
// --[ License ] --------------------------------------------------------------
//
//     Permission is hereby granted, free of charge, to any person obtaining a
//     copy of this software and associated documentation files (the
//     "Software"), to deal in the Software without restriction, including
//     without limitation the rights to use, copy, modify, merge, publish,
//     distribute, sublicense, and/or sell copies of the Software, and to
//     permit persons to whom the Software is furnished to do so, subject to
//     the following conditions:
//
//     The above copyright notice and this permission notice shall be included
//     in all copies or substantial portions of the Software.
//
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//     OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//     MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//     IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//     CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//     TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//     SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2019 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiscImageChef.CommonTypes.Interfaces;

namespace DiscImageChef.Checksums
{
    public class Register : IPluginRegister
    {
        public List<Type> GetAllChecksumPlugins()
        {
            return Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IChecksum)))
                           .Where(t => t.IsClass).ToList();
        }

        public List<Type> GetAllFilesystemPlugins() => null;

        public List<Type> GetAllFilterPlugins() => null;

        public List<Type> GetAllFloppyImagePlugins() => null;

        public List<Type> GetAllMediaImagePlugins() => null;

        public List<Type> GetAllPartitionPlugins() => null;

        public List<Type> GetAllReadOnlyFilesystemPlugins() => null;

        public List<Type> GetAllWritableFloppyImagePlugins() => null;

        public List<Type> GetAllWritableImagePlugins() => null;
    }
}