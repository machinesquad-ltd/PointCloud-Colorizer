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
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Colorizer
{
    class SbetRecsList
    {
        public SortedList<DateTime, SbetRecord> Records = new SortedList<DateTime, SbetRecord>();
        public string CoordSyst, LedgerPath, EventsList;
        public int GPSWeek, LeapSecs;

        public SbetRecsList(string lPath, int leapSecs, int GpsWeek, string coordSys)
        {
            LedgerPath = lPath;
            LeapSecs = leapSecs;
            GPSWeek = GpsWeek;
            CoordSyst = coordSys;
            Load();
        }

        public SbetRecsList(string lPath, string camEventsList, int leapSecs, int GpsWeek, string coordSys)
        {
            LedgerPath = lPath;
            EventsList = camEventsList;
            LeapSecs = leapSecs;
            GPSWeek = GpsWeek;
            CoordSyst = coordSys;
            Load();
        }

        private void Load()
        {
            SortedList<DateTime, string> camEvents = new SortedList<DateTime, string>();

            if (!string.IsNullOrEmpty(EventsList))
            {
                //Load the camera events
                Parallel.ForEach(File.ReadAllLines(EventsList), e =>
                {
                    List<string> temp = e.Split().ToList();
                    temp.RemoveAll(str => String.IsNullOrEmpty(str));
                    lock (camEvents) { camEvents.Add(GetFromGps(GPSWeek, Double.Parse(temp[0])), temp[1]); };
                });
            }

            List<double> vals = new List<double>();

            // Open an SBET file for reading...
            using (BinaryReader reader = new BinaryReader(File.Open(LedgerPath, FileMode.Open, FileAccess.Read)))
            {
                int length = (int)reader.BaseStream.Length;
                while (reader.BaseStream.Position != length)
                {
                    vals.Add(reader.ReadDouble());
                }
            }

            SortedList<DateTime, int> indexes = new SortedList<DateTime, int>();
            for (int i = 0; i < vals.Count; i += 17)
            {
                indexes.Add(GetFromGps(GPSWeek, vals[i]), i);
            }

            //if no list of camevents is provided, just add all the records, otherwise only add the closest SBet records.

            if (camEvents.Count > 0)
            {
                //Slow bit
                SortedList<DateTime, int> newIndexes = new SortedList<DateTime, int>();
                Parallel.ForEach(camEvents, (evnt) =>
                 {
                     lock (newIndexes) { newIndexes.Add(GetClosestKey(evnt.Key, indexes).Key, GetClosestKey(evnt.Key, indexes).Value); }
                 });
                indexes = newIndexes;
            }

            LatLngUTMConverter converter = new LatLngUTMConverter("WGS 84");
            Parallel.ForEach(indexes, (i) =>
            {
                DateTime tempUtcTime = i.Key;
                double eastng = 0.0;
                double northng = 0.0;
                double height = vals[i.Value + 3];

                if (CoordSyst == "UTM")
                {
                    LatLngUTMConverter.UTMResult res = converter.ConvertLatLngToUtm((vals[i.Value + 1] * 180.0) / Math.PI, (vals[i.Value + 2] * 180.0) / Math.PI);
                    eastng = res.Easting;
                    northng = res.Northing;
                }
                else
                {
                    Console.WriteLine("No coordinate system defined");
                }

                SbetRecord rec = new SbetRecord
                {
                    //Not required here //time = vals[i],
                    utcTime = tempUtcTime,
                    //Not required here //latRad = vals[i + 1],
                    //Not required here //lonRad = vals[i + 2],
                    //Not required here //alt = height,
                    XYZ = new Point3D(eastng, northng, height),
                    //Not required here //xVel = vals[i + 4],
                    //Not required here //yVel = vals[i + 5],
                    //Not required here //zVel = vals[i + 6],
                    roll = vals[i.Value + 7],
                    pitch = vals[i.Value + 8],
                    yaw = vals[i.Value + 9],
                    //Not required here //headingDeg = RadianToDegree(vals[i + 9]),
                    //Not required here //wander = vals[i + 10],
                    //Not required here //xForce = vals[i + 11],
                    //Not required here //yForce = vals[i + 12],
                    //Not required here //zForce = vals[i + 13],
                    //Not required here //xAngRate = vals[i + 14],
                    //Not required here //yAngRate = vals[i + 15],
                    //Not required here //zAngRate = vals[i + 16]
                };
                lock (Records) { Records.Add(rec.utcTime, rec); }
            });
        }

        DateTime GetFromGps(int weeknumber, double seconds)
        {
            DateTime datum = new DateTime(1980, 1, 6, 0, 0, 0);
            DateTime week = datum.AddDays(weeknumber * 7);
            DateTime time = week.AddSeconds(seconds - LeapSecs);
            return time;
        }

        public SbetRecord GetClosestRecord(DateTime pTime)
        {
            DateTime time = pTime;
            SbetRecord lowKey = null, highKey = null;

            foreach (KeyValuePair<DateTime, SbetRecord> key in Records)
            {
                if (key.Key.Ticks > time.Ticks)
                {
                    highKey = key.Value;
                    break;
                }
                lowKey = key.Value;
            }

            SbetRecord rec;
            if (time.Ticks - lowKey.utcTime.Ticks > highKey.utcTime.Ticks - time.Ticks)
            {
                rec = highKey;
            }
            else
            {
                rec = lowKey;
            }
            return rec;
        }

        public KeyValuePair<DateTime, int> GetClosestKey(DateTime time, SortedList<DateTime, int> stList)
        {
            KeyValuePair<DateTime, int> rec, lowKey = new KeyValuePair<DateTime, int>(), highKey = new KeyValuePair<DateTime, int>();

            foreach (KeyValuePair<DateTime, int> key in stList)
            {
                if (key.Key.Ticks > time.Ticks)
                {
                    highKey = key;
                    break;
                }
                lowKey = key;
            }

            if (time.Ticks - lowKey.Key.Ticks > highKey.Key.Ticks - time.Ticks)
            {
                rec = highKey;
            }
            else
            {
                rec = lowKey;
            }
            return rec;
        }
    }

    public class SbetRecord
    {
        //Not required here //public double time;    //GPS seconds of week
        public DateTime utcTime; //GPS Datetime
        //public double latRad;    //latitude in radians
        //public double lonRad;    //longitude in radians
        //public double alt;    //altitude
        public Point3D XYZ;
        //Not required here //public double xVel;    //velocity in x direction
        //Not required here //public double yVel;    //velocity in y direction
        //Not required here //public double zVel;    //velocity in z direction
        public double roll;    //roll angle
        public double pitch;    //pitch angle
        public double yaw;    //heading angle
        //public double headingDeg; //heading angle in degrees
        //Not required here //public double wander;    //wander
        //Not required here //public double xForce;    //force in x direction
        //Not required here //public double yForce;    //force in y direction
        //Not required here //public double zForce;    //force in z direction
        //Not required here //public double xAngRate;    //angular rate in x direction
        //Not required here //public double yAngRate;    //angular rate in y direction
        //Not required here //public double zAngRate;    //angular rate in z direction

        public SbetRecord()
        {

        }
    }
}
