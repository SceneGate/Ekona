// NitroRom.cs
//
// Copyright (c) 2019 SceneGate Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
namespace Ekona.Containers.Formats
{
    using Yarhl.FileSystem;

    /// <summary>
    /// Nintendo DS cartridge.
    /// </summary>
    public class NitroRom : NodeContainerFormat
    {
        public NitroRom()
        {
            Header = new RomHeader();
            Banner = new Banner();

            System = new Node("system");
            System.Add(new Node("header", Header));
            System.Add(new Node("banner", Banner));
            Root.Add(System);

            Data = new Node("data");
            Root.Add(Data);
        }

        public RomHeader Header {
            get;
            private set;
        }

        public Banner Banner {
            get;
            private set;
        }

        public Node System {
            get;
            private set;
        }

        public Node Data {
            get;
            private set;
        }
    }
}
