using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Media3D;

namespace Colorizer
{
    public partial class form_Main : Form
    {
        private bool isshown = false;

        private SortedList<DateTime, PanoPic> pics = new SortedList<DateTime, PanoPic>();
        List<PointF> polyPts = new List<PointF>();

        public form_Main()
        {
            InitializeComponent();
        }

        private void ExitToolsStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ValidateForm()
        {
            if (picture_CalibFile.Image == null)
            {
                if (!string.IsNullOrEmpty(text_PicsInputFolder.Text))
                {
                    Bitmap calImg = null;
                    if (pics.Count > 0)
                    {
                        calImg = new Bitmap(pics.First().Value.PicPath);
                    }
                    else
                    {
                        try
                        {
                            calImg = new Bitmap(Directory.GetFiles(text_PicsInputFolder.Text, "*.png", SearchOption.TopDirectoryOnly)[0]);
                        }
                        catch (Exception)
                        {

                        }
                    }

                    if (calImg != null)
                    {
                        Bitmap shiftMap = new Bitmap(calImg);

                        //draw the center line reticle
                        Color retCol = Color.Yellow;

                        int centX = (shiftMap.Width / 2) - 1, centY = (shiftMap.Height / 2) - 1;

                        for (int i = 0; i < shiftMap.Width - 1; i++)
                        {
                            shiftMap.SetPixel(i, centY, retCol);
                        }

                        for (int i = 0; i < calImg.Height - 1; i++)
                        {
                            shiftMap.SetPixel(centX, i, retCol);
                        }
                        //populate the calibration tool with the first pic in the list                    
                        picture_CalibFile.Image = shiftMap;
                    }
                }
            }

            text_PtsInput.BackColor = DefaultBackColor;
            text_SbetInput.BackColor = DefaultBackColor;
            text_PicsInputFolder.BackColor = DefaultBackColor;
            text_OutputFolder.BackColor = DefaultBackColor;
            text_CamEventsFile.BackColor = DefaultBackColor;


            bool allowProcess;
            if (!string.IsNullOrEmpty(text_PtsInput.Text))
            {
                allowProcess = true;
                foreach (string f in text_PtsInput.Lines)
                {
                    if (!String.IsNullOrEmpty(f))
                    {
                        if (!File.Exists(f)) { allowProcess = false; text_PtsInput.BackColor = Color.OrangeRed; break; }
                    }
                }
            }
            else
            {
                allowProcess = false;
            }

            if (!string.IsNullOrEmpty(text_SbetInput.Text))
            {
                if (!File.Exists(text_SbetInput.Text)) { allowProcess = false; text_SbetInput.BackColor = Color.OrangeRed; }
            }
            else
            {
                allowProcess = false;
            }

            if (!string.IsNullOrEmpty(text_PicsInputFolder.Text))
            {
                if (!Directory.Exists(text_PicsInputFolder.Text)) { allowProcess = false; text_PicsInputFolder.BackColor = Color.OrangeRed; }
                else
                {
                    List<string> files = Directory.GetFiles(text_PicsInputFolder.Text, "*.png", SearchOption.TopDirectoryOnly).ToList<string>();
                    if (files.Count == 0)
                    {
                        if (isshown == true)
                        {
                            MessageBox.Show("No png files found in " + text_PicsInputFolder.Text, "No PNG files found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        allowProcess = false; text_PicsInputFolder.BackColor = Color.OrangeRed;
                    }
                    else
                    {
                        foreach (string f in files)
                        {
                            //check that we have a matching DAT file for each picture
                            string datFile = f.Replace(Path.GetFileName(f), Path.GetFileNameWithoutExtension(f) + ".dat");
                            if (!File.Exists(datFile))
                            {
                                if (isshown == true)
                                {
                                    MessageBox.Show("Data file missing for picture " + Path.GetFileName(f), "Missing data file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                                allowProcess = false; text_PicsInputFolder.BackColor = Color.OrangeRed;
                            }
                        }

                        if ((bool)Properties.Settings.Default.setting_CheckScanDate == true)
                        {
                            //see if we can populate the date from one of the dat files

                            List<string> dtFiles = Directory.GetFiles(text_PicsInputFolder.Text, "*.dat", SearchOption.TopDirectoryOnly).ToList();

                            string[] tempText = File.ReadAllLines(dtFiles[0]);

                            if (tempText[2].Contains("UTC"))
                            {
                                DateTime scanDate = ParseGlobalDate(tempText[2].Replace("UTCTime:", ""));//.Split()[0]) ;

                                if (scanDate != null && scanDate != dateTime_ScanDate.Value)
                                {
                                    if (isshown == true)
                                    {
                                        var msgRes = MessageBox.Show("New scan date found from the pictures, do you want to update the scan date to " + scanDate.ToShortDateString() + "?", "Change scan date?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                                        if (msgRes == DialogResult.Yes)
                                        {
                                            dateTime_ScanDate.Value = scanDate;
                                            dateTime_ScanDate.CalendarTitleBackColor = DefaultBackColor;
                                        }
                                        else
                                        {
                                            dateTime_ScanDate.CalendarTitleBackColor = Color.Orange;
                                        }
                                    }
                                    else
                                    {
                                        dateTime_ScanDate.CalendarTitleBackColor = Color.Orange;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                allowProcess = false;
            }

            if (!string.IsNullOrEmpty(text_OutputFolder.Text))
            {
                if (!Directory.Exists(text_OutputFolder.Text)) { allowProcess = false; text_OutputFolder.BackColor = Color.OrangeRed; }
            }
            else
            {
                allowProcess = false;
            }

            if (!string.IsNullOrEmpty(text_CamEventsFile.Text))
            {
                if (!File.Exists(text_CamEventsFile.Text)) { allowProcess = false; text_CamEventsFile.BackColor = Color.OrangeRed; }
            }

            Properties.Settings.Default.Save();

            if (allowProcess) { button_Colorize.Enabled = true; } else { button_Colorize.Enabled = false; }

        }

        private void Text_TextChanged(object sender, EventArgs e)
        {
            ValidateForm();
        }

        private void Form_Main_Load(object sender, EventArgs e)
        {
            LoadForm();
        }

        private void LoadForm()
        {
            polyPts = Properties.Settings.Default.setting_CarShadowPoints ?? new List<PointF>();
            numeric_CorrectionAngle.Value = Properties.Settings.Default.setting_CamAngle;
            List<string> coordSystems = new List<string>
            {
                "UTM"
            };
            combo_CoordSys.DataSource = coordSystems;
            SetBitMapColour(Color.Red);
            ValidateForm();
        }

        private void Button_PtsBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog newDiag = new OpenFileDialog()
            {
                Title = "open points file",
                CheckFileExists = true,
                Filter = "all files|*.*|las file|*.las|xyz file|*.xyz|points file|*.pts",
                Multiselect = true
            })
            {
                newDiag.ShowDialog(this);

                if (newDiag.FileNames.Count() > 0)
                {
                    text_PtsInput.Text = string.Empty;
                    foreach (string f in newDiag.FileNames)
                    {
                        text_PtsInput.Text = text_PtsInput.Text + f + Environment.NewLine;
                    }
                }
            }
            ValidateForm();
        }

        private void Button_SbetBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog newDiag = new OpenFileDialog()
            {
                Title = "open SBET file",
                CheckFileExists = true,
                Filter = "SBET file|*.out|all files|*.*"
            })
            {
                newDiag.ShowDialog(this);
                if (!String.IsNullOrEmpty(newDiag.FileName))
                {
                    text_SbetInput.Text = newDiag.FileName;
                }
            }
            ValidateForm();
        }

        private void Button_PicsBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog newDiag = new FolderBrowserDialog()
            {
                Description = "select the Picture files folder",
                ShowNewFolderButton = true
            })
            {
                newDiag.ShowDialog(this);
                if (!String.IsNullOrEmpty(newDiag.SelectedPath))
                {
                    text_PicsInputFolder.Text = newDiag.SelectedPath;
                }
            }
            ValidateForm();
        }

        private void Button_OutputBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog newDiag = new FolderBrowserDialog()
            {
                Description = "select the output folder",
                ShowNewFolderButton = true
            })
            {
                newDiag.ShowDialog(this);
                if (!String.IsNullOrEmpty(newDiag.SelectedPath))
                {
                    text_OutputFolder.Text = newDiag.SelectedPath;
                }
            }
            ValidateForm();
        }

        private void Button_Colorize_Click(object sender, EventArgs e)
        {
            ValidateForm();
            Cursor = Cursors.WaitCursor;
            if (button_Colorize.Enabled == true)
            {
                int gpsWeek = GetWeek(dateTime_ScanDate.Value);
                DateTime date = dateTime_ScanDate.Value;
                double adjAng = (double)Properties.Settings.Default.setting_CamAngle;

                //Get the SBET data
                tool_Status.Text = "Loading Sbet Data";
                tool_Status.Visible = true;
                this.Refresh();
                SbetRecsList ledger;
                if (!string.IsNullOrEmpty(text_CamEventsFile.Text))
                {
                    //Use the camera timed events to only load relevant data in the ledger
                    ledger = new SbetRecsList(text_SbetInput.Text, text_CamEventsFile.Text, (int)numeric_LeapSecs.Value, gpsWeek, combo_CoordSys.Text);
                }
                else
                {
                    //Load everything
                    ledger = new SbetRecsList(text_SbetInput.Text, (int)numeric_LeapSecs.Value, gpsWeek, combo_CoordSys.Text);
                }

                //Ledger loaded, get the pics
                tool_Status.Text = "Loading Panoramic files";
                this.Refresh();

                bool picsError = false;
                Parallel.ForEach(Directory.GetFiles(text_PicsInputFolder.Text, "*.png", SearchOption.TopDirectoryOnly), (f, state) =>
                 {
                     if (picsError == false)
                     {
                         PanoPic newPic = new PanoPic(f, (int)numeric_TimeOffset.Value, (double)numeric_TimeScaleFact.Value);
                         newPic.SbetRecord = ledger.GetClosestRecord(newPic.UtcTime);
                         Matrix3D yawC = NewRotateAroundZ(newPic.SbetRecord.yaw + DegreeToRadian((double)numeric_YawOffset.Value));
                         Matrix3D pitchC = NewRotateAroundX(newPic.SbetRecord.pitch + DegreeToRadian((double)numeric_PitchOffset.Value));
                         Matrix3D rollC = NewRotateAroundY(newPic.SbetRecord.roll + DegreeToRadian((double)numeric_RollOffset.Value));
                         Matrix3D newMatrix = yawC * rollC * pitchC;
                         Point3D oSet = new Point3D((double)numeric_XOffset.Value, (double)numeric_YOffset.Value, (double)numeric_ZOffset.Value);
                         Point3D test = Point3D.Multiply(oSet, newMatrix);
                         newPic.CamCentre = new Point3D(newPic.SbetRecord.XYZ.X + test.X, newPic.SbetRecord.XYZ.Y + test.Y, newPic.SbetRecord.XYZ.Z + test.Z);

                         lock (pics)
                         {
                             try
                             {
                                 pics.Add(newPic.UtcTime, newPic);
                             }
                             catch (Exception)
                             {
                                 pics.TryGetValue(newPic.UtcTime, out PanoPic res);
                                 MessageBox.Show("Time data is the same for " + Path.GetFileName(newPic.PicPath) + " and " + Path.GetFileName(res.PicPath) + "Processing will stop", "Duplicate data file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                 picsError = true;
                                 state.Break();
                             }
                         }
                     }
                 });

                if (picsError == false)
                {
                    //Start preparing the points batch processing
                    List<string> files = text_PtsInput.Lines.ToList();
                    files.RemoveAll(str => String.IsNullOrEmpty(str));

                    tool_Progress.Maximum = files.Count;
                    tool_Progress.Minimum = 0;
                    tool_Progress.Value = 0;
                    tool_Progress.Visible = true;
                    this.Refresh();

                    foreach (string pointfile in files)
                    {
                        tool_Status.Text = "processing " + Path.GetFileName(pointfile);
                        this.Refresh();
                        //init the point list
                        ConcurrentQueue<Pnt> points = new ConcurrentQueue<Pnt>();

                        if (Path.GetExtension(pointfile).ToUpper() == ".LAS")
                        {
                            //Read the las file
                            LasFile lasFile = new LasFile(pointfile);
                            Parallel.ForEach(lasFile.Records, (s) => { points.Enqueue(new Pnt(lasFile, s)); });
                        }
                        else
                        {
                            List<string> ptStr = new List<string>();
                            //Read all the points
                            using (System.IO.StreamReader sr = new System.IO.StreamReader(pointfile))
                            {
                                string line;
                                while ((line = sr.ReadLine()) != null)
                                {
                                    ptStr.Add(line);
                                }
                            }
                            Parallel.ForEach(ptStr, (s) => { points.Enqueue(new Pnt(s, date)); });
                        }

                        string picError = "false";

                        //For each point, find the closest picture in time
                        Parallel.ForEach(points, (pt, state) =>
                         {
                             if (picError == "true")
                             {
                                 state.Break();
                             }
                             else
                             {
                                 PanoPic refpic = GetClosestKey(pt.Time, pics).Value;
                                 if (refpic == null)
                                 {
                                     lock (picError) { picError = "true"; }
                                     //missing picture
                                     state.Break();
                                     Console.WriteLine("failed to find a picture for " + pt.Time.ToLongDateString());
                                 }
                                 else
                                 {
                                     //Add the point to the closest image in time
                                     lock (refpic) { refpic.PicPoints.Add(pt); }
                                     pt.RefPic = refpic;
                                 }
                             }
                         });

                        if (picError == "true")
                        {
                            MessageBox.Show("At least one part of the dataset doesn't have picture data" + Environment.NewLine + "Processing will now stop", "Insufficient Picture data", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            //Colorize per pic           
                            foreach (var p in pics)
                            {
                                if (p.Value.PicPoints.Count > 0)
                                {
                                    p.Value.InitBitmap();
                                    int pWid = p.Value.bit.Width;
                                    int pHei = p.Value.bit.Height;

                                    Matrix3D yawM = NewRotateAroundZ(p.Value.SbetRecord.yaw + DegreeToRadian(adjAng) + (double)numeric_YawOffset.Value);
                                    Matrix3D pitchM = NewRotateAroundX(((-1) * p.Value.SbetRecord.pitch) + (double)numeric_PitchOffset.Value);
                                    Matrix3D rollM = NewRotateAroundY(((-1) * p.Value.SbetRecord.roll) + (double)numeric_RollOffset.Value);

                                    Matrix3D combined = yawM * rollM * pitchM;

                                    _ = Parallel.ForEach(p.Value.PicPoints, (pt) =>
                                       {
                                           Point3D newPoint = new Point3D(pt.X - p.Value.CamCentre.X, pt.Y - p.Value.CamCentre.Y, pt.Z - p.Value.CamCentre.Z);
                                           Point3D trPoint = Point3D.Multiply(newPoint, combined);

                                           double m, n;
                                           double hWid = (double)pWid / 2;
                                           double hPi = Math.PI / 2;

                                           if (trPoint.X > 0)
                                           {
                                               double temp = (hPi - Math.Atan(trPoint.Y / trPoint.X)) / (Math.PI * 2);
                                               m = hWid + (temp * ((double)pWid - 1));
                                           }
                                           else
                                           {
                                               double temp = (hPi + Math.Atan(trPoint.Y / trPoint.X)) / (Math.PI * 2);
                                               m = hWid - (temp * ((double)pWid - 1));
                                           }
                                           double tmpAtan = Math.Atan(trPoint.Z / Math.Sqrt(Math.Pow(trPoint.X, 2) + Math.Pow(trPoint.Y, 2)));
                                           n = (pHei - 1) * (0.5 - (tmpAtan / Math.PI));

                                           int xP = Convert.ToInt32(m);
                                           int yP = (Convert.ToInt32(n));

                                           //check if the point is in a shadow area
                                           if (polyPts.Count > 3)
                                           {
                                               if (IsPointInPolygon(new PointF((float)xP / (float)pWid, (float)yP / (float)pHei), polyPts.ToArray()))
                                               {
                                                   //Move the point to the next picture
                                                   int index = pics.IndexOfKey(p.Key) + 1;
                                                   if (index > pics.Count - 1)
                                                   {
                                                       index -= 1;
                                                       lock (p.Value.bit) { pt.Color = p.Value.bit.GetPixel(xP, yP); }
                                                   }
                                                   else
                                                   {
                                                       PanoPic nextPic = pics.Values[index];
                                                       lock (nextPic) { nextPic.PicPoints.Add(pt); }
                                                       pt.RefPic = nextPic;
                                                   }
                                               }
                                               else
                                               {
                                                   lock (p.Value.bit) { pt.Color = p.Value.bit.GetPixel(xP, yP); }
                                               }
                                           }
                                           else
                                           {
                                               lock (p.Value.bit) { pt.Color = p.Value.bit.GetPixel(xP, yP); }
                                           }
                                       });
                                }
                            }

                            tool_Status.Text = "processing complete for" + Path.GetFileName(pointfile) + ", writing output";
                            this.Refresh();
                            bool intLast = (bool)Properties.Settings.Default.setting_XYZRGBI;
                            List<string> contents = new List<string>();
                            Parallel.ForEach(pics, (p) =>
                             {
                                 if (p.Value.PicPoints.Count > 0)
                                 {
                                     List<string> tmpContents = new List<string>();
                                     foreach (Pnt pt in p.Value.PicPoints)
                                     {
                                         if (pt.RefPic == p.Value)
                                         {
                                             if (intLast == true)
                                             {
                                                 lock (contents) { tmpContents.Add(pt.X.ToString() + " " + pt.Y.ToString() + " " + pt.Z.ToString() + " " + pt.Color.R + " " + pt.Color.G + " " + pt.Color.B + " " + pt.Intensity.ToString()); }
                                             }
                                             else
                                             {
                                                 lock (contents) { tmpContents.Add(pt.X.ToString() + " " + pt.Y.ToString() + " " + pt.Z.ToString() + " " + pt.Intensity.ToString() + " " + pt.Color.R + " " + pt.Color.G + " " + pt.Color.B); }
                                             }
                                         }
                                     }

                                     if (check_PerPic.Checked)
                                     {
                                         File.WriteAllLines(text_OutputFolder.Text + @"\" + Path.GetFileNameWithoutExtension(pointfile) + "-" + Path.GetFileNameWithoutExtension(p.Value.PicPath) + ".pts", tmpContents);
                                     }
                                     else
                                     {
                                         lock (contents)
                                         {
                                             contents.AddRange(tmpContents);
                                         }
                                     }
                                     List<Pnt> swapQueue = new List<Pnt>();
                                     p.Value.PicPoints = swapQueue;
                                     tmpContents.Clear();
                                 }

                                 if (p.Value.bit != null)
                                 {
                                     p.Value.bit.Dispose();
                                 }
                             });

                            if (!check_PerPic.Checked)
                            {
                                File.WriteAllLines(text_OutputFolder.Text + @"\" + Path.GetFileNameWithoutExtension(pointfile) + ".pts", contents.ToArray());
                                contents.Clear();
                            }
                            tool_Progress.Value += 1;
                        }
                    }
                }

                tool_Progress.Visible = false;
                tool_Progress.Value = 0;
                tool_Status.Text = "Processing complete!";
                MessageBox.Show("Processing complete", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Refresh();
                OpenFolder(text_OutputFolder.Text);
                Application.Exit();

            }
            else
            {
                MessageBox.Show("Errors found, please review the parameters", "Processing aborted", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Cursor = Cursors.Default;
        }

        public KeyValuePair<DateTime, PanoPic> GetClosestKey(DateTime pTime, SortedList<DateTime, PanoPic> stList)
        {
            DateTime time = pTime.AddMilliseconds(0.0);
            KeyValuePair<DateTime, PanoPic> rec;
            KeyValuePair<DateTime, PanoPic> lowKey = new KeyValuePair<DateTime, PanoPic>();
            KeyValuePair<DateTime, PanoPic> highKey = new KeyValuePair<DateTime, PanoPic>();

            foreach (KeyValuePair<DateTime, PanoPic> key in stList)
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
                if (highKey.Key.Ticks != 0)
                {
                    rec = highKey;

                }
                else
                {
                    rec = lowKey;
                }
            }
            else
            {
                rec = lowKey;
            }

            return rec;
        }

        private double DegreeToRadian(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        public static Matrix3D NewRotateAroundX(double radians)
        {
            var matrix = new Matrix3D()
            {
                M22 = Math.Cos(radians),
                M23 = Math.Sin(radians),
                M32 = -(Math.Sin(radians)),
                M33 = Math.Cos(radians)
            };
            return matrix;
        }
        public static Matrix3D NewRotateAroundY(double radians)
        {
            var matrix = new Matrix3D()
            {
                M11 = Math.Cos(radians),
                M13 = -(Math.Sin(radians)),
                M31 = Math.Sin(radians),
                M33 = Math.Cos(radians)
            };
            return matrix;
        }
        public static Matrix3D NewRotateAroundZ(double radians)
        {
            var matrix = new Matrix3D()
            {
                M11 = (Math.Cos(radians)),
                M12 = (Math.Sin(radians)),
                M21 = -(Math.Sin(radians)),
                M22 = (Math.Cos(radians))
            };

            return matrix;
        }

        private void SetBitMapColour(Color col)
        {
            using (Bitmap map = new Bitmap(1, 1))
            {
                map.SetPixel(0, 0, col);
            }
        }

        private void Picture_CalibFile_MouseClick(object sender, MouseEventArgs e)
        {
            {
                int boxWidth = picture_CalibFile.Size.Width;

                //This variable will hold the result
                int X = e.X;
                int Delta = (boxWidth / 2) - X;
                float angleCorrection = ((float)Delta / (float)boxWidth) * 360;
                if (MessageBox.Show("New correction angle of " + angleCorrection.ToString() + " degrees selected. Retain?",
                    "Correction angle changed", MessageBoxButtons.OKCancel, MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button2) == DialogResult.OK)
                {
                    numeric_CorrectionAngle.Value = (decimal)angleCorrection;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void Button_EventsFileBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog newDiag = new OpenFileDialog()
            {
                Title = "open Camera Events file",
                CheckFileExists = true,
                Filter = "Camera log file|*.txt|all files|*.*"
            })
            {
                newDiag.ShowDialog(this);
                if (!String.IsNullOrEmpty(newDiag.FileName))
                {
                    text_CamEventsFile.Text = newDiag.FileName;
                }
            }
            ValidateForm();
        }

        private void SaveSetting(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        public static int GetWeek(DateTime refTime)
        {
            DateTime datum = new DateTime(1980, 1, 6, 0, 0, 0);

            TimeSpan diff = refTime.Subtract(datum);

            return (int)(diff.TotalDays / 7);

        }

        void Picture_CalibFile_Paint(object sender, PaintEventArgs e)
        {
            List<PointF> sizedPolyPts = new List<PointF>();

            foreach (PointF pt in polyPts)
            {
                sizedPolyPts.Add(new PointF(pt.X * (float)picture_CalibFile.Size.Width, pt.Y * (float)picture_CalibFile.Size.Height));
            }
            using (Pen penRed = new Pen(Color.Red, 2f))
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(50, 200, 0, 0)))
                {
                    if (polyPts.Count > 3)
                    {
                        e.Graphics.FillPolygon(brush, sizedPolyPts.ToArray());
                        e.Graphics.DrawPolygon(penRed, sizedPolyPts.ToArray());
                    }
                }
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            picture_CalibFile.MouseClick -= Picture_CalibFile_MouseClick;
            picture_CalibFile.MouseClick += Picture_CalibFile_PolyMouseClick;
        }

        private void Picture_CalibFile_PolyMouseClick(object sender, MouseEventArgs e)
        {
            {
                //add points relative to the size of the image
                polyPts.Add(new PointF((float)e.X / (float)picture_CalibFile.Size.Width, (float)e.Y / (float)picture_CalibFile.Size.Height));
                Properties.Settings.Default.setting_CarShadowPoints = polyPts;
                Properties.Settings.Default.Save();
                picture_CalibFile.Refresh();
            }
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            polyPts.Clear();
            picture_CalibFile.Refresh();
            picture_CalibFile.MouseClick -= Picture_CalibFile_PolyMouseClick;
            picture_CalibFile.MouseClick += Picture_CalibFile_MouseClick;
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            polyPts.RemoveAt(polyPts.Count - 1);
            Properties.Settings.Default.setting_CarShadowPoints = polyPts;
            Properties.Settings.Default.Save();
            picture_CalibFile.Refresh();
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            using (form_EditPolyPoints ptForm = new form_EditPolyPoints(Properties.Settings.Default.setting_CarShadowPoints))
            {
                ptForm.ShowDialog(this);

                if (ptForm.changed == true)
                {
                    Properties.Settings.Default.setting_CarShadowPoints = ptForm.PntList;
                    Properties.Settings.Default.Save();
                    picture_CalibFile.Refresh();
                }
            }
        }

        public bool IsPointInPolygon(PointF p, PointF[] polygon)
        {
            double minX = polygon[0].X;
            double maxX = polygon[0].X;
            double minY = polygon[0].Y;
            double maxY = polygon[0].Y;
            for (int i = 1; i < polygon.Length; i++)
            {
                PointF q = polygon[i];
                minX = Math.Min(q.X, minX);
                maxX = Math.Max(q.X, maxX);
                minY = Math.Min(q.Y, minY);
                maxY = Math.Max(q.Y, maxY);
            }

            if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY)
            {
                return false;
            }

            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i].Y > p.Y) != (polygon[j].Y > p.Y) &&
                     p.X < (polygon[j].X - polygon[i].X) * (p.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }


        private void OpenFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    Arguments = folderPath,
                    FileName = "explorer.exe"
                };
                Process.Start(startInfo);
            }
            else
            {
                MessageBox.Show(string.Format("{0} Directory does not exist!", folderPath));
            }

        }

        private void Form_Main_Shown(object sender, EventArgs e)
        {
            isshown = true;
        }

        private void ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            using (AboutBox1 aboutform = new AboutBox1())
            {
                aboutform.ShowDialog();
            }
        }

        public DateTime ParseGlobalDate(string dateStr)
        {

            if (!DateTime.TryParse(dateStr, CultureInfo.CurrentUICulture, DateTimeStyles.None, out DateTime res))
            {
                Console.WriteLine("invalid: " + dateStr);
            }

            return res;

        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
        }
    }
}
