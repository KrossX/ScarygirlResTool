/*  Scarygirl Resolution Tool - Copyright (C) 2012 KrossX
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
 
// Based on..
// 7zCrc.c -- CRC32 calculation
// 2009-11-23 : Igor Pavlov : Public domain

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ScarygirlTool
{
	class CRC
	{
		UInt32 CRC32poly = 0xEDB88320;
		UInt32[] CRC32table = new UInt32[256];
				
		public CRC()
		{
			BuildTable();
		}

		public UInt32 CheckCRC32(string filename, long length)
		{
			UInt32 crc = 0xFFFFFFFF;
			FileStream file = File.OpenRead(filename);
			
			for (long i = 0; i < length; i++)
				crc = (crc >> 8) ^ CRC32table[(crc ^ file.ReadByte()) & 0xFF];
			
			file.Close();

			return (crc ^ 0xFFFFFFFF);
		}


		void BuildTable()
		{
			UInt32 value;
			
			for (uint i = 0; i < 256; i++)
			{
				value = i;

				for (uint j = 0; j < 8; j++)
					value = (value >> 1) ^ (CRC32poly & ~((value & 1) - 1));

				CRC32table[i] = value;
			}
		}

	}
}
