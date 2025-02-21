﻿// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : SpamSum.cs
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
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using NUnit.Framework;

namespace Aaru.Tests.Checksums;

[TestFixture]
public class SpamSum
{
    const string EXPECTED_EMPTY  = "3::";
    const string EXPECTED_RANDOM = "24576:3dvzuAsHTQ16pc7O1Q/gS9qze+Swwn9s6IX:8/TQQpaVqze+JN6IX";

    [Test]
    public void EmptyData()
    {
        var data = new byte[1048576];

        var fs = new FileStream(Path.Combine(Consts.TestFilesRoot, "Checksum test files", "empty"),
                                FileMode.Open,
                                FileAccess.Read);

        fs.EnsureRead(data, 0, 1048576);
        fs.Close();
        fs.Dispose();
        string result = SpamSumContext.Data(data, out _);
        Assert.That(result, Is.EqualTo(EXPECTED_EMPTY));
    }

    [Test]
    public void EmptyInstance()
    {
        var data = new byte[1048576];

        var fs = new FileStream(Path.Combine(Consts.TestFilesRoot, "Checksum test files", "empty"),
                                FileMode.Open,
                                FileAccess.Read);

        fs.EnsureRead(data, 0, 1048576);
        fs.Close();
        fs.Dispose();
        IChecksum ctx = new SpamSumContext();
        ctx.Update(data);
        string result = ctx.End();
        Assert.That(result, Is.EqualTo(EXPECTED_EMPTY));
    }

    [Test]
    public void RandomData()
    {
        var data = new byte[1048576];

        var fs = new FileStream(Path.Combine(Consts.TestFilesRoot, "Checksum test files", "random"),
                                FileMode.Open,
                                FileAccess.Read);

        fs.EnsureRead(data, 0, 1048576);
        fs.Close();
        fs.Dispose();
        string result = SpamSumContext.Data(data, out _);
        Assert.That(result, Is.EqualTo(EXPECTED_RANDOM));
    }

    [Test]
    public void RandomInstance()
    {
        var data = new byte[1048576];

        var fs = new FileStream(Path.Combine(Consts.TestFilesRoot, "Checksum test files", "random"),
                                FileMode.Open,
                                FileAccess.Read);

        fs.EnsureRead(data, 0, 1048576);
        fs.Close();
        fs.Dispose();
        IChecksum ctx = new SpamSumContext();
        ctx.Update(data);
        string result = ctx.End();
        Assert.That(result, Is.EqualTo(EXPECTED_RANDOM));
    }
}