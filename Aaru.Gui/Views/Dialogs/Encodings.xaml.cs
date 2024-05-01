﻿// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Encodings.xaml.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI dialogs.
//
// --[ Description ] ----------------------------------------------------------
//
//     Encodings list dialog.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General public License for more details.
//
//     You should have received a copy of the GNU General public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2024 Natalia Portillo
// ****************************************************************************/

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Aaru.Gui.Views.Dialogs;

public sealed class Encodings : Window
{
    public Encodings()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}