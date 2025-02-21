﻿// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : XZ.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Aaru unit testing.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2024 Natalia Portillo
// ****************************************************************************/

using System.IO;
using Aaru.Checksums;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Filters;
using Aaru.Helpers;
using NUnit.Framework;

namespace Aaru.Tests.Filters;

[TestFixture]
public class Xz
{
    static readonly byte[] _expectedFile =
    [
        0x6c, 0x88, 0xa5, 0x9a, 0x1b, 0x7a, 0xec, 0x59, 0x2b, 0xef, 0x8a, 0x28, 0xdb, 0x11, 0x01, 0xc8
    ];
    static readonly byte[] _expectedContents =
    [
        0x18, 0x90, 0x5a, 0xf9, 0x83, 0xd8, 0x2b, 0xdd, 0x1a, 0xcc, 0x69, 0x75, 0x4f, 0x0f, 0x81, 0x5e
    ];
    readonly string _location;

    public Xz() => _location = Path.Combine(Consts.TestFilesRoot, "Filters", "xz.xz");

    [Test]
    public void CheckContents()
    {
        IFilter filter = new XZ();
        filter.Open(_location);
        Stream str  = filter.GetDataForkStream();
        var    data = new byte[1048576];
        str.EnsureRead(data, 0, 1048576);
        str.Close();
        str.Dispose();
        filter.Close();
        Md5Context.Data(data, out byte[] result);
        Assert.That(result, Is.EqualTo(_expectedContents));
    }

    [Test]
    public void CheckCorrectFile()
    {
        byte[] result = Md5Context.File(_location);
        Assert.That(result, Is.EqualTo(_expectedFile));
    }

    [Test]
    public void CheckFilterId()
    {
        IFilter filter = new XZ();
        Assert.That(filter.Identify(_location), Is.True);
    }

    [Test]
    public void Test()
    {
        IFilter filter = new XZ();
        Assert.That(filter.Open(_location),         Is.EqualTo(ErrorNumber.NoError));
        Assert.That(filter.DataForkLength,          Is.EqualTo(1048576));
        Assert.That(filter.GetDataForkStream(),     Is.Not.Null);
        Assert.That(filter.ResourceForkLength,      Is.EqualTo(0));
        Assert.That(filter.GetResourceForkStream(), Is.EqualTo(null));
        Assert.That(filter.HasResourceFork,         Is.EqualTo(false));
        filter.Close();
    }
}