﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : ATA.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : DiscImageChef device testing.
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
// Copyright © 2011-2019 Natalia Portillo
// ****************************************************************************/

using DiscImageChef.Console;
using DiscImageChef.Devices;
using DiscImageChef.Tests.Devices.ATA;

namespace DiscImageChef.Tests.Devices
{
    static partial class MainClass
    {
        public static void Ata(string devPath, Device dev)
        {
            while(true)
            {
                System.Console.Clear();
                DicConsole.WriteLine("Device: {0}", devPath);
                DicConsole.WriteLine("Send an ATA command to the device:");
                DicConsole.WriteLine("1.- Send a CHS ATA command to the device.");
                DicConsole.WriteLine("2.- Send a 28-bit ATA command to the device.");
                DicConsole.WriteLine("3.- Send a 48-bit ATA command to the device.");
                DicConsole.WriteLine("4.- Send an ATAPI command to the device.");
                DicConsole.WriteLine("5.- Send a CompactFlash command to the device.");
                DicConsole.WriteLine("6.- Send a Media Card Pass Through command to the device.");
                DicConsole.WriteLine("7.- Send a S.M.A.R.T. command to the device.");
                DicConsole.WriteLine("0.- Return to command class menu.");
                DicConsole.Write("Choose: ");

                string strDev = System.Console.ReadLine();
                if(!int.TryParse(strDev, out int item))
                {
                    DicConsole.WriteLine("Not a number. Press any key to continue...");
                    System.Console.ReadKey();
                    continue;
                }

                switch(item)
                {
                    case 0:
                        DicConsole.WriteLine("Returning to command class menu...");
                        return;
                    case 1:
                        AtaChs.Menu(devPath, dev);
                        continue;
                    case 2:
                        Ata28.Menu(devPath, dev);
                        continue;
                    case 3:
                        Ata48.Menu(devPath, dev);
                        continue;
                    case 4:
                        Atapi.Menu(devPath, dev);
                        continue;
                    case 5:
                        Cfa.Menu(devPath, dev);
                        continue;
                    case 6:
                        Mcpt.Menu(devPath, dev);
                        continue;
                    case 7:
                        Smart.Menu(devPath, dev);
                        continue;
                    default:
                        DicConsole.WriteLine("Incorrect option. Press any key to continue...");
                        System.Console.ReadKey();
                        continue;
                }
            }
        }
    }
}