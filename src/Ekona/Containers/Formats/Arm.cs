// Arm.cs
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
    using System;
    using System.Collections.Generic;
    using Yarhl.FileSystem;
    using Yarhl.IO;

    /// <summary>
    /// Model for game file with the program code of an ARM processor.
    /// </summary>
    public class Arm : IBinary
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Arm" /> class.
        /// </summary>
        public Arm()
        {
            Stream = new DataStream();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Arm" /> class.
        /// </summary>
        /// <param name="stream">Stream with the data.</param>
        public Arm(DataStream stream)
        {
            Stream = stream;
        }

        /// <summary>
        /// Gets the stream with the data.
        /// </summary>
        public DataStream Stream {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a value indicating the type of the processor.
        /// </summary>
        public ProcessorType Processor {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the entry address of the ARM.
        /// </summary>
        public uint EntryAddress {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the auto-load address of the ARM.
        /// </summary>
        public uint AutoLoad {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the address of the ARM in the RAM.
        /// </summary>
        public uint RamAddress {
            get;
            set;
        }
    }
}
