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
using System.Drawing;

namespace Colorizer
{
    public class Pnt
    {
        public double X;
        public double Y;
        public double Z;
        public float Intensity;
        public DateTime Time;
        public Color Color;
        public PanoPic RefPic;
        public LasFile LasFileRef;

        public Pnt(string ptsRecord, DateTime pointDate)
        {
            string[] splt;
            if (ptsRecord.Contains(",")) { splt = ptsRecord.Split(','); } else { splt = ptsRecord.Split(); }

            if (splt.Length == 4)
            {
                X = double.Parse(splt[0]);
                Y = double.Parse(splt[1]);
                Z = double.Parse(splt[2]);
                Intensity = float.Parse(splt[3]);
            }
            else if (splt.Length == 5)
            {
                X = double.Parse(splt[0]);
                Y = double.Parse(splt[1]);
                Z = double.Parse(splt[2]);
                Intensity = float.Parse(splt[3]);


                if (!TimeSpan.TryParseExact(splt[4], @"hh\:mm\:ss\.fff", null, out TimeSpan tmpTime))
                {
                    Console.WriteLine("not valid: " + pointDate + " " + splt[4]);
                }
                else
                {
                    DateTime tmpDate = pointDate.Date + tmpTime;
                    Time = tmpDate;
                }
            }
        }
        public Pnt(LasFile lasFile, LasFile.PDRF1 rec)
        {
            LasFileRef = lasFile;
            X = ((double)rec.X * (double)lasFile.Header.XScaleFactor) + (double)lasFile.Header.XOffset;
            Y = ((double)rec.Y * (double)lasFile.Header.YScaleFactor) + (double)lasFile.Header.YOffset;
            Z = ((double)rec.Z * (double)lasFile.Header.ZScaleFactor) + (double)lasFile.Header.ZOffset;
            Intensity = (float)rec.Intensity;
            Time = GetFromGps(Convert.ToDouble("1" + rec.GPSTime));
            //Console.WriteLine();
        }

        DateTime GetFromGps(double seconds)
        {
            DateTime time = new DateTime(1980, 1, 6, 0, 0, 0, DateTimeKind.Utc).AddSeconds(seconds);
            return time;
        }
    }
}
