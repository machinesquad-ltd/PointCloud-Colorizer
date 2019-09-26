using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Media.Media3D;

namespace Colorizer
{
    public class PanoPic
    {
        public string PicPath;
        public DateTime UtcTime;
        public Bitmap bit;
        public SbetRecord SbetRecord;
        public Point3D CamCentre;
        public List<Pnt> PicPoints = new List<Pnt>();

        public PanoPic(string path, int timeOffset, double timeScaleFactor)
        {
            PicPath = path;

            string[] tempArr = Path.GetFileNameWithoutExtension(path).Split('_');
            int picSeqNo = int.Parse(tempArr[tempArr.Length - 1]);

            double finalTimeOff = (double)timeOffset + ((double)picSeqNo * timeScaleFactor);

            //See if there's a data record alongside the pic
            string datFile = path.Split('.')[0] + ".dat";
            if (File.Exists(datFile))
            {
                string[] recs = File.ReadAllLines(datFile);
                string[] dT = recs[2].Replace("UTCTime:", "").Split('.');

                string lookup = dT[0] + "." + dT[1];
                if (dT[1].Length == 5)
                {
                    lookup = dT[0] + "." + dT[1] + "0";
                }
                else if (dT[1].Length == 4)
                {
                    lookup = dT[0] + "." + dT[1] + "00";
                }
                else if (dT[1].Length == 3)
                {
                    lookup = dT[0] + "." + dT[1] + "000";
                }
                else if (dT[1].Length == 2)
                {
                    lookup = dT[0] + "." + dT[1] + "0000";
                }
                else if (dT[1].Length == 1)
                {
                    lookup = dT[0] + "." + dT[1] + "00000";
                }
                else if (dT[1].Length == 0)
                {
                    lookup = dT[0] + ".000000";
                }
                UtcTime = DateTime.ParseExact(lookup, "dd/MM/yyyy HH:mm:ss.ffffff", null).AddMilliseconds(finalTimeOff);
            }
        }

        public void InitBitmap()
        {
            bit = new Bitmap(PicPath);
        }
    }
}
