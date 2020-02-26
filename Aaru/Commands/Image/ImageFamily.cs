// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : ImageFamily.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Verbs.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the 'image' verb.
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
// Copyright © 2011-2020 Natalia Portillo
// ****************************************************************************/

using System.CommandLine;

namespace DiscImageChef.Commands.Image
{
    public class ImageFamily : Command
    {
        public ImageFamily() : base("image", "Commands to manage images")
        {
            AddAlias("i");

            AddCommand(new AnalyzeCommand());
            AddCommand(new ChecksumCommand());
            AddCommand(new CompareCommand());
            AddCommand(new ConvertImageCommand());
            AddCommand(new CreateSidecarCommand());
            AddCommand(new DecodeCommand());
            AddCommand(new EntropyCommand());
            AddCommand(new ImageInfoCommand());
            AddCommand(new ListOptionsCommand());
            AddCommand(new PrintHexCommand());
            AddCommand(new VerifyCommand());
        }
    }
}