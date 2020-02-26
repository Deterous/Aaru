﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : SMC.cs
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

using System;
using DiscImageChef.Console;
using DiscImageChef.Decoders.SCSI;
using DiscImageChef.Devices;

namespace DiscImageChef.Tests.Devices.SCSI
{
    static class Smc
    {
        internal static void Menu(string devPath, Device dev)
        {
            while(true)
            {
                System.Console.Clear();
                DicConsole.WriteLine("Device: {0}", devPath);
                DicConsole.WriteLine("Send a SCSI Media Changer command to the device:");
                DicConsole.WriteLine("1.- Send READ ATTRIBUTE command.");
                DicConsole.WriteLine("0.- Return to SCSI commands menu.");
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
                        DicConsole.WriteLine("Returning to SCSI commands menu...");
                        return;
                    case 1:
                        ReadAttribute(devPath, dev);
                        continue;
                    default:
                        DicConsole.WriteLine("Incorrect option. Press any key to continue...");
                        System.Console.ReadKey();
                        continue;
                }
            }
        }

        static void ReadAttribute(string devPath, Device dev)
        {
            ushort              element        = 0;
            byte                elementType    = 0;
            byte                volume         = 0;
            byte                partition      = 0;
            ushort              firstAttribute = 0;
            bool                cache          = false;
            ScsiAttributeAction action         = ScsiAttributeAction.Values;
            string              strDev;
            int                 item;

            parameters:
            while(true)
            {
                System.Console.Clear();
                DicConsole.WriteLine("Device: {0}", devPath);
                DicConsole.WriteLine("Parameters for READ ATTRIBUTE command:");
                DicConsole.WriteLine("Action: {0}",          action);
                DicConsole.WriteLine("Element: {0}",         element);
                DicConsole.WriteLine("Element type: {0}",    elementType);
                DicConsole.WriteLine("Volume: {0}",          volume);
                DicConsole.WriteLine("Partition: {0}",       partition);
                DicConsole.WriteLine("First attribute: {0}", firstAttribute);
                DicConsole.WriteLine("Use cache?: {0}",      cache);
                DicConsole.WriteLine();
                DicConsole.WriteLine("Choose what to do:");
                DicConsole.WriteLine("1.- Change parameters.");
                DicConsole.WriteLine("2.- Send command with these parameters.");
                DicConsole.WriteLine("0.- Return to SCSI Media Changer commands menu.");

                strDev = System.Console.ReadLine();
                if(!int.TryParse(strDev, out item))
                {
                    DicConsole.WriteLine("Not a number. Press any key to continue...");
                    System.Console.ReadKey();
                    continue;
                }

                switch(item)
                {
                    case 0:
                        DicConsole.WriteLine("Returning to SCSI Media Changer commands menu...");
                        return;
                    case 1:
                        DicConsole.WriteLine("Attribute action");
                        DicConsole.WriteLine("Available values: {0} {1} {2} {3} {4}", ScsiAttributeAction.Values,
                                             ScsiAttributeAction.List, ScsiAttributeAction.VolumeList,
                                             ScsiAttributeAction.PartitionList, ScsiAttributeAction.ElementList,
                                             ScsiAttributeAction.Supported);
                        DicConsole.Write("Choose?: ");
                        strDev = System.Console.ReadLine();
                        if(!Enum.TryParse(strDev, true, out action))
                        {
                            DicConsole.WriteLine("Not a valid attribute action. Press any key to continue...");
                            action = ScsiAttributeAction.Values;
                            System.Console.ReadKey();
                            continue;
                        }

                        DicConsole.Write("Element?: ");
                        strDev = System.Console.ReadLine();
                        if(!ushort.TryParse(strDev, out element))
                        {
                            DicConsole.WriteLine("Not a number. Press any key to continue...");
                            element = 0;
                            System.Console.ReadKey();
                            continue;
                        }

                        DicConsole.Write("Element type?: ");
                        strDev = System.Console.ReadLine();
                        if(!byte.TryParse(strDev, out elementType))
                        {
                            DicConsole.WriteLine("Not a number. Press any key to continue...");
                            elementType = 0;
                            System.Console.ReadKey();
                            continue;
                        }

                        DicConsole.Write("Volume?: ");
                        strDev = System.Console.ReadLine();
                        if(!byte.TryParse(strDev, out volume))
                        {
                            DicConsole.WriteLine("Not a number. Press any key to continue...");
                            volume = 0;
                            System.Console.ReadKey();
                            continue;
                        }

                        DicConsole.Write("Partition?: ");
                        strDev = System.Console.ReadLine();
                        if(!byte.TryParse(strDev, out partition))
                        {
                            DicConsole.WriteLine("Not a number. Press any key to continue...");
                            partition = 0;
                            System.Console.ReadKey();
                            continue;
                        }

                        DicConsole.Write("First attribute?: ");
                        strDev = System.Console.ReadLine();
                        if(!ushort.TryParse(strDev, out firstAttribute))
                        {
                            DicConsole.WriteLine("Not a number. Press any key to continue...");
                            firstAttribute = 0;
                            System.Console.ReadKey();
                            continue;
                        }

                        DicConsole.Write("Use cache?: ");
                        strDev = System.Console.ReadLine();
                        if(!bool.TryParse(strDev, out cache))
                        {
                            DicConsole.WriteLine("Not a boolean. Press any key to continue...");
                            cache = false;
                            System.Console.ReadKey();
                        }

                        break;
                    case 2: goto start;
                }
            }

            start:
            System.Console.Clear();
            bool sense = dev.ReadAttribute(out byte[] buffer, out byte[] senseBuffer, action, element, elementType,
                                           volume, partition, firstAttribute, cache, dev.Timeout, out double duration);

            menu:
            DicConsole.WriteLine("Device: {0}", devPath);
            DicConsole.WriteLine("Sending READ ATTRIBUTE to the device:");
            DicConsole.WriteLine("Command took {0} ms.",               duration);
            DicConsole.WriteLine("Sense is {0}.",                      sense);
            DicConsole.WriteLine("Buffer is {0} bytes.",               buffer?.Length.ToString() ?? "null");
            DicConsole.WriteLine("Buffer is null or empty? {0}",       ArrayHelpers.ArrayIsNullOrEmpty(buffer));
            DicConsole.WriteLine("Sense buffer is {0} bytes.",         senseBuffer?.Length.ToString() ?? "null");
            DicConsole.WriteLine("Sense buffer is null or empty? {0}", ArrayHelpers.ArrayIsNullOrEmpty(senseBuffer));
            DicConsole.WriteLine();
            DicConsole.WriteLine("Choose what to do:");
            DicConsole.WriteLine("1.- Print buffer.");
            DicConsole.WriteLine("2.- Print sense buffer.");
            DicConsole.WriteLine("3.- Decode sense buffer.");
            DicConsole.WriteLine("4.- Send command again.");
            DicConsole.WriteLine("5.- Change parameters.");
            DicConsole.WriteLine("0.- Return to SCSI Media Changer commands menu.");
            DicConsole.Write("Choose: ");

            strDev = System.Console.ReadLine();
            if(!int.TryParse(strDev, out item))
            {
                DicConsole.WriteLine("Not a number. Press any key to continue...");
                System.Console.ReadKey();
                System.Console.Clear();
                goto menu;
            }

            switch(item)
            {
                case 0:
                    DicConsole.WriteLine("Returning to SCSI Media Changer commands menu...");
                    return;
                case 1:
                    System.Console.Clear();
                    DicConsole.WriteLine("Device: {0}", devPath);
                    DicConsole.WriteLine("READ ATTRIBUTE response:");
                    if(buffer != null) PrintHex.PrintHexArray(buffer, 64);
                    DicConsole.WriteLine("Press any key to continue...");
                    System.Console.ReadKey();
                    System.Console.Clear();
                    DicConsole.WriteLine("Device: {0}", devPath);
                    goto menu;
                case 2:
                    System.Console.Clear();
                    DicConsole.WriteLine("Device: {0}", devPath);
                    DicConsole.WriteLine("READ ATTRIBUTE sense:");
                    if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer, 64);
                    DicConsole.WriteLine("Press any key to continue...");
                    System.Console.ReadKey();
                    System.Console.Clear();
                    DicConsole.WriteLine("Device: {0}", devPath);
                    goto menu;
                case 3:
                    System.Console.Clear();
                    DicConsole.WriteLine("Device: {0}", devPath);
                    DicConsole.WriteLine("READ ATTRIBUTE decoded sense:");
                    DicConsole.Write("{0}", Sense.PrettifySense(senseBuffer));
                    DicConsole.WriteLine("Press any key to continue...");
                    System.Console.ReadKey();
                    System.Console.Clear();
                    DicConsole.WriteLine("Device: {0}", devPath);
                    goto menu;
                case 4: goto start;
                case 5: goto parameters;
                default:
                    DicConsole.WriteLine("Incorrect option. Press any key to continue...");
                    System.Console.ReadKey();
                    System.Console.Clear();
                    goto menu;
            }
        }
    }
}