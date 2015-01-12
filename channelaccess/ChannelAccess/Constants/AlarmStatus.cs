/*
 *  EpicsSharp - An EPICS Channel Access library for the .NET platform.
 *
 *  Copyright (C) 2013 - 2015  Paul Scherrer Institute, Switzerland
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EpicsSharp.ChannelAccess.Constants
{
    /// <summary>
    /// Informs about the status of the device behind this Channel
    /// </summary>
    public enum AlarmStatus : ushort
    {
        /// <summary>
        /// Device is working properly correctly
        /// </summary>
        NO_ALARM = 0,
        READ = 1,
        WRITE = 2,
        /// <summary>
        /// Device is malfunctioning, and hit the upper Alarm Limit
        /// </summary>
        HIHI = 3,
        /// <summary>
        /// Device is missbehaving, and hit the upper Warn Limit
        /// </summary>
        HIGH = 4,
        /// <summary>
        /// Device is malfunctioning, and hit the lower Alarm Limit
        /// </summary>
        LOLO = 5,
        /// <summary>
        /// Device is missbehaving, and hit theu lower Warn Limit
        /// </summary>
        LOW = 6,

        STATE = 7,
        COS = 8,
        COMM = 9,
        TIMEOUT = 10,
        HARDWARE_LIMIT = 11,
        CALC = 12,
        SCAN = 13,
        LINK = 14,
        SOFT = 15,
        BAD_SUB = 16,
        /// <summary>
        /// Undefined alarm status
        /// </summary>
        UDF = 17,
        DISABLE = 18,
        SIMM = 19,
        READ_ACCESS = 20,
        WRITE_ACCESS = 21
    }
}
