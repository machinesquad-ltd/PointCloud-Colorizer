/*
    Colorizer is a mobile LIDAR Scanning Colourising tool developped by MachineSquad Ltd(UK)
    Copyright (C)2019 MachineSquad Ltd (www.machinesquad.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Colorizer
{
    //This class is incomplete and assumes no variable length record and that the point records are of type1. More work is required to integrate types 1 to 10 and the potential variable length records that come after the header block
    //For more info the LAS file spec can be found at https://www.asprs.org/wp-content/uploads/2010/12/LAS_1_4_r13.pdf

    public class LasFile
    {
        const int FILE_HEADER_SIZE = 375;
        public FILEHEADER Header;
        public List<PDRF1> Records = new List<PDRF1>();


        public LasFile(string filePath)
        {
            ReadHeader(filePath);
            ReadRecords(filePath, Header.HeaderSize, Header.PointDataRecLength);
        }

        public struct FILEHEADER
        {
            public char[] FileSignature;                     //File Signature (“LASF”)                char[4]
            public UInt16 FileSourceID;                      //File Source ID
            public UInt16 GlobalEncoding;                    //Global Encoding
            public UInt32 PIDData1;                          //Project ID - GUID Data 1
            public UInt16 PIDData2;                          //Project ID - GUID Data 2
            public UInt16 PIDData3;                          //Project ID - GUID Data 3
            public byte[] PIDData4;                          //Project ID - GUID Data 3               unsigned char[8]
            public byte VersionMajor;                      //Version Major                          unsigned char[8]
            public byte VersionMinor;                      //Version Minor                          unsigned char[8]
            public char[] SystemIdentiﬁer;                   //System Identiﬁer                       char[32]
            public char[] GeneratingSoftware;                //Generating Software                    char[32]
            public UInt16 FileCreationDayofYear;             //File Creation Day of Year
            public UInt16 FileCreationYear;                  //File Creation Year
            public UInt16 HeaderSize;                        //Header Size
            public UInt32 OffsetToPointData;                //Offset to Point Data 
            public UInt32 NumberOfVariableLengthRecords;     //Number of Variable Length Records 
            public byte PointDataRecFormat;                //Point Data Record Format               uchar
            public UInt16 PointDataRecLength;                //Point Data Record Length
            public UInt32 LegacyNumberOfPointRecs;           //unsigned long 4 bytes yes
            public byte[] LegacyNumberOfPointByReturn;       //unsigned long[5] 20 bytes yes
            public double XScaleFactor;                      //double 8 bytes yes
            public double YScaleFactor;                      //double 8 bytes yes
            public double ZScaleFactor;                      //double 8 bytes yes
            public double XOffset;                           //double 8 bytes yes
            public double YOffset;                           //double 8 bytes yes
            public double ZOffset;                           //double 8 bytes yes
            public double MaxX;                              //double 8 bytes yes
            public double MaxY;                              //double 8 bytes yes
            public double MaxZ;                              //double 8 bytes yes
            public double MinX;                              //double 8 bytes yes
            public double MinY;                              //double 8 bytes yes
            public double MinZ;                              //double 8 bytes yes
            public UInt64 StartOfWaveformDataPacketRec;      //unsigned long long 8 bytes yes
            public UInt64 StartOfFirstExtdVariableLenRec;    //unsigned long long 8 bytes yes
            public UInt32 NumberOfExtdVariableLengthRecs;    //unsigned long 4 bytes yes
            public UInt64 NumberOfPointRecs;                 //unsigned long long 8 bytes yes
            public byte[] NumberOfPointsByReturn;          //unsigned long long[15] 120 bytes yes
        }
        private static FILEHEADER HeaderFromReader(BinaryReader reader)
        {
            FILEHEADER result = new FILEHEADER();
            result.FileSignature = Encoding.UTF8.GetChars(reader.ReadBytes(4));
            result.FileSourceID = reader.ReadUInt16();
            result.GlobalEncoding = reader.ReadUInt16();
            result.PIDData1 = reader.ReadUInt32();
            result.PIDData2 = reader.ReadUInt16();
            result.PIDData3 = reader.ReadUInt16();
            result.PIDData4 = reader.ReadBytes(8);
            result.VersionMajor = reader.ReadByte();
            result.VersionMinor = reader.ReadByte();
            result.SystemIdentiﬁer = Encoding.UTF8.GetChars(reader.ReadBytes(32));
            result.GeneratingSoftware = Encoding.UTF8.GetChars(reader.ReadBytes(32));
            result.FileCreationDayofYear = reader.ReadUInt16();
            result.FileCreationYear = reader.ReadUInt16();
            result.HeaderSize = reader.ReadUInt16();
            result.OffsetToPointData = reader.ReadUInt32();
            result.NumberOfVariableLengthRecords = reader.ReadUInt32();
            result.PointDataRecFormat = reader.ReadByte();
            result.PointDataRecLength = reader.ReadUInt16();
            result.LegacyNumberOfPointRecs = reader.ReadUInt32();
            result.LegacyNumberOfPointByReturn = reader.ReadBytes(20);
            result.XScaleFactor = reader.ReadDouble();
            result.YScaleFactor = reader.ReadDouble();
            result.ZScaleFactor = reader.ReadDouble();
            result.XOffset = reader.ReadDouble();
            result.YOffset = reader.ReadDouble();
            result.ZOffset = reader.ReadDouble();
            result.MaxX = reader.ReadDouble();
            result.MaxY = reader.ReadDouble();
            result.MaxZ = reader.ReadDouble();
            result.MinX = reader.ReadDouble();
            result.MinY = reader.ReadDouble();
            result.MinZ = reader.ReadDouble();
            result.StartOfWaveformDataPacketRec = reader.ReadUInt64();
            result.StartOfFirstExtdVariableLenRec = reader.ReadUInt64();
            result.NumberOfExtdVariableLengthRecs = reader.ReadUInt32();
            result.NumberOfPointRecs = reader.ReadUInt64();
            result.NumberOfPointsByReturn = reader.ReadBytes(120);
            return result;
        }
        public void ReadHeader(string filePath)
        {
            using (var stream1 = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                stream1.Position = 0;
                using (var reader = new BinaryReader(stream1))
                {
                    while (stream1.Position < FILE_HEADER_SIZE)
                    {
                        Header = HeaderFromReader(reader);
                    }
                }
            }
        }
        public struct VARIABLELENGTHRECORD
        {
            public UInt16 Reserved;                  //unsigned short 2 bytes
            public char[] UserID;                    //char[16] 16 bytes yes
            public UInt16 RecordID;                  //unsigned short 2 bytes yes
            public UInt16 RecordLength;              //After Header unsigned short 2 bytes yes
            public char[] Description;               //char[32] 32 bytes
        }
        public struct PDRF1
        {
            public Int32 X;                          // long 4 bytes yes
            public Int32 Y;                          // long 4 bytes yes
            public Int32 Z;                          // long 4 bytes yes
            public UInt16 Intensity;                 // unsigned short 2 bytes no
            public byte BitData;                     //used to store the 8 bits of data below
            //public byte ReturnNumber;              // 3 bits (bits 0-2) 3 bits yes
            //public byte NumberOfReturns;           // 3 bits (bits 3-5) 3 bits yes
            //public byte ScanDirectionFlag;         // 1 bit (bit 6) 1 bit yes
            //public byte EdgeOfFlightLine;          // 1 bit (bit 7) 1 bit yes
            public byte Classification;              // unsigned char 1 byte yes
            public char ScanAngle;                   // signed char 1 byte yes - Rank(-90 to +90) – Left Side
            public byte UserData;                    // unsigned char 1 byte no
            public UInt16 PointSourceID;             // unsigned short 2 bytes yes
            public double GPSTime;                   // double 8 bytes yes
        }

        private static PDRF1 RecordsFromReader(BinaryReader reader)
        {
            PDRF1 result = new PDRF1();

            result.X = reader.ReadInt32();                           // long 4 bytes yes
            result.Y = reader.ReadInt32();                           // long 4 bytes yes
            result.Z = reader.ReadInt32();                           // long 4 bytes yes
            result.Intensity = reader.ReadUInt16();                  // unsigned short 2 bytes no
            result.BitData = reader.ReadByte();                      //used to store the 8 bits of data below
            //public byte ReturnNumber;                              // 3 bits (bits 0-2) 3 bits yes
            //public byte NumberOfReturns;                           // 3 bits (bits 3-5) 3 bits yes
            //public byte ScanDirectionFlag;                         // 1 bit (bit 6) 1 bit yes
            //public byte EdgeOfFlightLine;                          // 1 bit (bit 7) 1 bit yes
            result.Classification = reader.ReadByte();               // unsigned char 1 byte yes
            result.ScanAngle = Convert.ToChar(reader.ReadByte());    // signed char 1 byte yes - Rank(-90 to +90) – Left Side
            result.UserData = reader.ReadByte();                     // unsigned char 1 byte no
            result.PointSourceID = reader.ReadUInt16();              // unsigned short 2 bytes yes
            result.GPSTime = reader.ReadDouble();                    // double 8 bytes yes        
            return result;
        }

        public void ReadRecords(string filePath, int startPosition, int recordNumBytes)
        {
            using (var stream1 = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                //stream1.Position = FILE_HEADER_SIZE; //Start stream after the file header
                stream1.Position = startPosition;
                using (var reader = new BinaryReader(stream1))
                {
                    while (stream1.Position < stream1.Length) //While not at the end of the stream
                    {
                        Records.Add(RecordsFromReader(reader));
                    }
                }
            }
        }
    }
}
