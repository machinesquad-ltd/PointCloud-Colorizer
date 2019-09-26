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
