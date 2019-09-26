﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace Colorizer
{
    public partial class form_EditPolyPoints : Form
    {
        public List<PointF> PntList;
        public bool changed = false;
        public form_EditPolyPoints(List<PointF> initList)
        {
            PntList = initList;
            InitializeComponent();
        }

        private void form_EditPolyPoints_Load(object sender, EventArgs e)
        {
            DataTable tab = new DataTable();
            tab.Columns.Add(new DataColumn("PointName", System.Type.GetType("System.Int32")));
            tab.Columns.Add(new DataColumn("XCoord", System.Type.GetType("System.Double")));
            tab.Columns.Add(new DataColumn("YCoord", System.Type.GetType("System.Double")));
            int index = 0;
            if (PntList != null)
            {
                foreach (PointF pt in PntList)
                {
                    DataRow ro = tab.NewRow();
                    ro["PointName"] = index;
                    ro["XCoord"] = pt.X.ToString();
                    ro["YCoord"] = pt.Y.ToString();
                    index = index + 1;
                    tab.Rows.Add(ro);
                }
                dataGridView1.DataSource = tab;
                dataGridView1.Columns[0].HeaderText = "Point order";
                dataGridView1.Columns[1].HeaderText = "X Coord (in % image width";
                dataGridView1.Columns[2].HeaderText = "Y Coord (in % image height";
            }
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            changed = true;
            button_Save.Enabled = true;
        }

        private void button_Save_Click(object sender, EventArgs e)
        {
            PntList.Clear();
            dataGridView1.Sort(dataGridView1.Columns[0], ListSortDirection.Ascending);
            DataTable tab = (DataTable)dataGridView1.DataSource;
            foreach (DataRow ro in tab.Rows)
            {
                PntList.Add(new PointF(float.Parse(ro[1].ToString()), float.Parse(ro[2].ToString())));
            }
        }
    }
}
