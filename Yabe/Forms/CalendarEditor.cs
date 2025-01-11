﻿/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2015 Frederic Chaxel <fchaxel@free.fr>
*
* Permission is hereby granted, free of charge, to any person obtaining
* a copy of this software and associated documentation files (the
* "Software"), to deal in the Software without restriction, including
* without limitation the rights to use, copy, modify, merge, publish,
* distribute, sublicense, and/or sell copies of the Software, and to
* permit persons to whom the Software is furnished to do so, subject to
* the following conditions:
*
* The above copyright notice and this permission notice shall be included
* in all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*
*********************************************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.BACnet;
using System.IO.BACnet.Serialize;
using System.Windows.Forms.Calendar;
using System.Globalization;

namespace Yabe
{
    public partial class CalendarEditor : Form
    {
        BACnetDevice device; BacnetObjectId object_id;
        // dates in the bacnetobject
        BACnetCalendarEntry calendarEntries;

        DateTime CalendarStartRequested;
        bool InternalListeEntriesSelect=false;

        public CalendarEditor(BACnetDevice device, BacnetObjectId object_id)
        {
            InitializeComponent();

            this.device = device;
            this.object_id = object_id;

            LoadCalendar();
        }

        private void LoadCalendar()
        {
            IList<BacnetValue> values;
            device.ReadPropertyRequest(object_id, BacnetPropertyIds.PROP_DATE_LIST, out values);

            if ((values != null) && (values.Count == 1))
                calendarEntries = (BACnetCalendarEntry)values[0].Value;
            else
            {
                calendarEntries = new BACnetCalendarEntry(); // empty
                calendarEntries.Entries = new List<object>();
            }

            dateSelect.SelectionRange = new SelectionRange(DateTime.Now, DateTime.Now);

            listEntries.Items.Clear();
            foreach (object e in calendarEntries.Entries)
                listEntries.Items.Add(e);

            //  calendarView will be updated by the calendarView_LoadItems event
            SetCalendarDisplayDate(DateTime.Now);

        }

        private void WriteCalendar()
        {
            try
            {
                List<BacnetValue> v = new List<BacnetValue>();
                v.Add(new BacnetValue(calendarEntries));
                device.WritePropertyRequest(object_id, BacnetPropertyIds.PROP_DATE_LIST, v);
            }
            catch { }
        }

        private void btReadWrite_Click(object sender, EventArgs e)
        {
            WriteCalendar();
            LoadCalendar();
        }

        private void SetCalendarDisplayDate (DateTime d)
        {
            DateTime start = new DateTime(d.Year, d.Month, 1);
            DateTime stop = start.AddMonths(1).AddHours(-1);

            CalendarStartRequested = start;

            calendarView.SetViewRange(start, stop);
        }

        private void dateSelect_DateChanged(object sender, DateRangeEventArgs e)
        {
            SetCalendarDisplayDate(e.Start);
        }

        private void listEntries_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((InternalListeEntriesSelect)||(listEntries.SelectedIndex==-1))
            {
                InternalListeEntriesSelect = false;
                return;
            }

            object o = listEntries.Items[listEntries.SelectedIndex];
            if (o is BacnetDateRange)
            {
                BacnetDateRange bdr = (BacnetDateRange)o;

                if (bdr.startDate.year != 255)
                    SetCalendarDisplayDate(bdr.startDate.toDateTime());
                else if (bdr.endDate.year != 255)
                    SetCalendarDisplayDate(bdr.endDate.toDateTime().AddDays(-10));       
            }
            else if (o is BacnetDate)
            {
                BacnetDate bd = (BacnetDate)o;
                if (!bd.IsPeriodic)
                    SetCalendarDisplayDate(bd.toDateTime());
            }
        }

        private void AddCalendarEntry(DateTime _start, DateTime _end,  Color color, String Text, object tag)
        {
            DateTime start, end;
            start = new DateTime(_start.Year, _start.Month, _start.Day, 0, 0, 0);
            end = new DateTime(_end.Year, _end.Month, _end.Day, 23, 59, 59);
            CalendarItem ci = new CalendarItem(calendarView, start, end, Text);
            ci.ApplyColor(color);
            ci.Tag = tag;

            if (start <= calendarView.Days[calendarView.Days.Length-1].Date && calendarView.Days[0] .Date <= end)
                calendarView.Items.Add(ci);          
        }

        private void PlaceItemsInCalendarView()
        {
            foreach (object e in calendarEntries.Entries)
            {
                if (e is BacnetDate)
                {
                    BacnetDate bd = (BacnetDate)e;
                    if (bd.IsPeriodic)
                    {
                        foreach (CalendarDay dt in calendarView.Days)
                            if (bd.IsAFittingDate(dt.Date))
                                AddCalendarEntry(dt.Date,dt.Date, Color.Blue,"Periodic",bd);
                    }
                    else
                        AddCalendarEntry(bd.toDateTime(), bd.toDateTime(), Color.Green, "", bd);
                }
                else if (e is BacnetDateRange) 
                {
                    BacnetDateRange bdr = (BacnetDateRange)e;
                    DateTime start,end;

                    if (bdr.startDate.year != 255)
                        start = new DateTime(bdr.startDate.year+1900, bdr.startDate.month, bdr.startDate.day, 0, 0, 0);
                    else
                        start = DateTime.MinValue;
                    if (bdr.endDate.year != 255)
                        end = new DateTime(bdr.endDate.year+1900, bdr.endDate.month, bdr.endDate.day, 23, 59, 59);
                    else
                        end = DateTime.MaxValue;
                    CalendarItem ci = new CalendarItem(calendarView, start, end, "");
                    ci.ApplyColor(Color.Yellow);
                    ci.Tag = bdr;

                    if (start <= calendarView.Days[calendarView.Days.Length - 1].Date && calendarView.Days[0].Date <= end)
                        calendarView.Items.Add(ci);
                }
                else
                {
                    BacnetweekNDay bwnd = (BacnetweekNDay)e;
                    foreach (CalendarDay dt in calendarView.Days)
                        if (bwnd.IsAFittingDate(dt.Date))
                            AddCalendarEntry(dt.Date, dt.Date, Color.Red, "Periodic", bwnd);
                }
            }
        }

        // Called to renew all the data inside the control
        private void calendarView_LoadItems(object sender, CalendarLoadEventArgs e)
        {
            PlaceItemsInCalendarView();
        }

        private void calendarView_ItemDeleted(object sender, CalendarItemEventArgs e)
        {
            calendarEntries.Entries.Remove(e.Item.Tag);
            listEntries.Items.Remove(e.Item.Tag);
            SetCalendarDisplayDate(CalendarStartRequested);
        }
        
        private void calendarView_ItemSelected(object sender, CalendarItemEventArgs e)
        {
            int Idx=listEntries.Items.IndexOf(e.Item.Tag);
            listEntries.SelectedIndex = Idx;
            InternalListeEntriesSelect = true;
        }
        
        private void calendarView_ItemCreated(object sender, CalendarItemCancelEventArgs e)
        {
            if ((e.Item.StartDate.Year == e.Item.EndDate.Year) && (e.Item.StartDate.Month == e.Item.EndDate.Month) && (e.Item.StartDate.Day == e.Item.EndDate.Day))
            {
                BacnetDate newbd = new BacnetDate((byte)(e.Item.StartDate.Year-1900), (byte)e.Item.StartDate.Month, (byte)e.Item.StartDate.Day);
                listEntries.Items.Add(newbd);
                calendarEntries.Entries.Add(newbd);
            }
            else
            {
                BacnetDateRange newbdr = new BacnetDateRange();
                newbdr.startDate = new BacnetDate((byte)(e.Item.StartDate.Year-1900), (byte)e.Item.StartDate.Month, (byte)e.Item.StartDate.Day);
                newbdr.endDate = new BacnetDate((byte)(e.Item.EndDate.Year-1900), (byte)e.Item.EndDate.Month, (byte)e.Item.EndDate.Day);
                listEntries.Items.Add(newbdr);
                calendarEntries.Entries.Add(newbdr);
            }
            SetCalendarDisplayDate(CalendarStartRequested);
        }

        private void calendarView_ItemDatesChanged(object sender, CalendarItemEventArgs e)
        {
            object o = e.Item.Tag;

            if (((o is BacnetDate)&&(((BacnetDate)o).IsPeriodic))||( o is BacnetweekNDay))
            {
                // Cannot do that with perodic element : what has to be changed : day n°, wday, month ????
                modifyToolStripMenuItem_Click(null, null);
            }
            else
            //(o is BacnetDate) || (o is BacnetDateRange)
            {
                calendarEntries.Entries.Remove(o);
                int Idx = listEntries.Items.IndexOf(o);

                listEntries.Items.Remove(o);

                if ((e.Item.StartDate.Year == e.Item.EndDate.Year)&&(e.Item.StartDate.Month == e.Item.EndDate.Month)&&(e.Item.StartDate.Day == e.Item.EndDate.Day))
                {
                    BacnetDate newbd = new BacnetDate((byte)(e.Item.StartDate.Year-1900), (byte)e.Item.StartDate.Month, (byte)e.Item.StartDate.Day);
                    listEntries.Items.Insert(Idx, newbd);
                    calendarEntries.Entries.Add(newbd);
                }
                else
                {
                    BacnetDateRange newbdr = new BacnetDateRange();
                    newbdr.startDate = new BacnetDate((byte)(e.Item.StartDate.Year-1900), (byte)e.Item.StartDate.Month, (byte)e.Item.StartDate.Day);
                    newbdr.endDate = new BacnetDate((byte)(e.Item.EndDate.Year-1900), (byte)e.Item.EndDate.Month, (byte)e.Item.EndDate.Day);
                    listEntries.Items.Insert(Idx, newbdr);
                    calendarEntries.Entries.Add(newbdr);
                }
            }

            SetCalendarDisplayDate(CalendarStartRequested);
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                calendarEntries.Entries.Remove(listEntries.SelectedItem);
                listEntries.Items.RemoveAt(listEntries.SelectedIndex);
            }
            catch { }
            SetCalendarDisplayDate(CalendarStartRequested);
        }

        private void btAdd_Click(object sender, EventArgs e)
        {
            calendarView.CreateItemOnSelection("", true);
        }

        private void btDelete_Click(object sender, EventArgs e)
        {
            if (listEntries.SelectedIndex == -1) return;
            calendarView.DeleteSelectedItems();

        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BacnetweekNDay bwd = new BacnetweekNDay(1, 1);
            CalendarEntryEdit Edit = new CalendarEntryEdit(bwd);
            Edit.ShowDialog();
            if (Edit.OutOK)
            {
                object o = Edit.GetBackEntry();
                listEntries.Items.Add(o);
                calendarEntries.Entries.Add(o);
                SetCalendarDisplayDate(CalendarStartRequested);
            }
        }

        private void modifyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listEntries.SelectedIndex == -1) return;
            try
            {
                object selected = listEntries.SelectedItem;
                CalendarEntryEdit Edit = new CalendarEntryEdit(listEntries.SelectedItem);
                Edit.ShowDialog();
                if (Edit.OutOK)
                {
                    object o = Edit.GetBackEntry();

                    calendarEntries.Entries.Remove(listEntries.SelectedItem);
                    int Idx = listEntries.SelectedIndex;

                    try // Don't understand, exception, but all is OK , and the job is done !
                    {
                        listEntries.Items.Remove(selected);
                    }
                    catch { }

                    listEntries.Items.Insert(Idx, o);
                    calendarEntries.Entries.Add(o);
                }
            }
            catch { }
            SetCalendarDisplayDate(CalendarStartRequested);
        }
        private void listEntries_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            modifyToolStripMenuItem_Click(null, null);
        }

        private void calendarView_ItemDoubleClick(object sender, CalendarItemEventArgs e)
        {
            modifyToolStripMenuItem_Click(null, null);
        }

    }

    #region CalendarEntryEdit
    class CalendarEntryEdit : Form
    {
        public bool OutOK = false;

        object Entry;

        private void InitCalendarEntryEdit()
        {
            InitializeComponent();

            for (int i = 1; i < 7; i++)
                wday.Items.Add(CultureInfo.CurrentCulture.DateTimeFormat.DayNames[i]);
            wday.Items.Add(CultureInfo.CurrentCulture.DateTimeFormat.DayNames[0]);
            wday.Items.Add("****");

            for (int i = 1; i < 32; i++)
                day.Items.Add(i);
            day.Items.Add("Last");
            day.Items.Add("**");

            for (int i = 0; i < 12; i++)
                month.Items.Add(CultureInfo.CurrentCulture.DateTimeFormat.MonthNames[i]);
            month.Items.Add("Odd");
            month.Items.Add("Even");
            month.Items.Add("****");

            week.Items.Add("days 1 to 7");
            week.Items.Add("days 8 to 14");
            week.Items.Add("days 15 to 21");
            week.Items.Add("days 22 to 28");
            week.Items.Add("days 29 to 31");
            week.Items.Add("last 7 days of the month");
            week.Items.Add("all days numbers");
        }

        public CalendarEntryEdit(object entry)
        {
            InitCalendarEntryEdit();

            this.Entry = entry;

            if (entry is BacnetDate)
            {
                dateRangeGrp.Visible = false;
                DateGrp.Visible = true;
                week.Visible = false;
                DateGrp.Location = dateRangeGrp.Location;

                BacnetDate bd = (BacnetDate)entry;

                if (bd.wday != 255)
                    wday.SelectedIndex = bd.wday - 1;
                else
                    wday.SelectedIndex = 7;

                if (bd.day != 255)
                    day.SelectedIndex = bd.day - 1;
                else
                    day.SelectedIndex = 32;

                if (bd.month != 255)
                    month.SelectedIndex = bd.month - 1;
                else
                    month.SelectedIndex = 14;

                if (bd.year != 255)
                    year.Text = (bd.year + 1900).ToString();
                else
                    year.Text = "****";
            }

            if (entry is BacnetDateRange)
            {
                dateRangeGrp.Visible = true;
                DateGrp.Visible = false;
                BacnetDateRange bdr = (BacnetDateRange)entry;

                if (bdr.startDate.year != 255)
                    startDate.Value = bdr.startDate.toDateTime();
                else
                    startDate.Value = DateTimePicker.MinimumDateTime ;

                if (bdr.endDate.year != 255)
                    endDate.Value = bdr.endDate.toDateTime();
                else
                    endDate.Value = DateTimePicker.MaximumDateTime;
            }

            if (entry is BacnetweekNDay)
            {
                dateRangeGrp.Visible = false;
                DateGrp.Visible = true;
                DateGrp.Location = dateRangeGrp.Location;
                year.Visible = false;
                day.Visible = false;
                month.Location=new Point(month.Location.X+50,month.Location.Y);

                BacnetweekNDay bwd = (BacnetweekNDay)entry;

                if (bwd.wday != 255)
                    wday.SelectedIndex = bwd.wday - 1;
                else
                    wday.SelectedIndex = 7;

                if (bwd.month != 255)
                    month.SelectedIndex = bwd.month - 1;
                else
                    month.SelectedIndex = 14;

                if ((bwd.week < 7)&&(bwd.week !=0))
                    week.SelectedIndex = bwd.week - 1;
                else
                    week.SelectedIndex = 6;
            }
        }

        public object GetBackEntry()
        {
            if (Entry is BacnetDate)
            {
                BacnetDate bd = new BacnetDate();

                if (wday.SelectedIndex == 7)
                    bd.wday = 255;
                else
                    bd.wday = (byte)(wday.SelectedIndex + 1);

                if (day.SelectedIndex == 32)
                    bd.day = 255;
                else
                    bd.day = (byte)(day.SelectedIndex + 1);

                if (month.SelectedIndex == 14)
                    bd.month = 255;
                else
                    bd.month = (byte)(month.SelectedIndex + 1);

                int valyear = 255;
                try
                {
                    valyear = Convert.ToInt32(year.Text) - 1900;
                }
                catch { }

                bd.year = (byte)valyear;

                return bd;

            }

            if (Entry is BacnetDateRange)
            {
                BacnetDateRange bdr = new BacnetDateRange();

                if (startDate.Value == DateTimePicker.MinimumDateTime)
                    bdr.startDate = new BacnetDate(255, 255, 255);
                else
                    bdr.startDate = new BacnetDate((byte)(startDate.Value.Year - 1900), (byte)startDate.Value.Month, (byte)startDate.Value.Day);

                if (endDate.Value == DateTimePicker.MaximumDateTime)
                    bdr.startDate = new BacnetDate(255, 255, 255);
                else
                    bdr.endDate = new BacnetDate((byte)(endDate.Value.Year - 1900), (byte)endDate.Value.Month, (byte)endDate.Value.Day);

                return bdr;
            }
            if (Entry is BacnetweekNDay)
            {
                BacnetweekNDay bwd = (BacnetweekNDay)Entry;

                if (wday.SelectedIndex == 7)
                    bwd.wday = 255;
                else
                    bwd.wday = (byte)(wday.SelectedIndex + 1);

                if (month.SelectedIndex == 14)
                    bwd.month = 255;
                else
                    bwd.month = (byte)( month.SelectedIndex + 1);

                bwd.week = (byte)(week.SelectedIndex + 1);
                if (bwd.week==7) bwd.week=255;

                return bwd;
            }
            return null;
        }

        private void btOk_Click(object sender, EventArgs e)
        {
            OutOK = true;
            Close();
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.startDate = new System.Windows.Forms.DateTimePicker();
            this.endDate = new System.Windows.Forms.DateTimePicker();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.dateRangeGrp = new System.Windows.Forms.GroupBox();
            this.DateGrp = new System.Windows.Forms.GroupBox();
            this.year = new System.Windows.Forms.TextBox();
            this.month = new System.Windows.Forms.ComboBox();
            this.day = new System.Windows.Forms.ComboBox();
            this.week = new System.Windows.Forms.ComboBox();
            this.wday = new System.Windows.Forms.ComboBox();
            this.btOk = new System.Windows.Forms.Button();
            this.btCancel = new System.Windows.Forms.Button();
            this.dateRangeGrp.SuspendLayout();
            this.DateGrp.SuspendLayout();
            this.SuspendLayout();
            // 
            // startDate
            // 
            this.startDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.startDate.Location = new System.Drawing.Point(31, 29);
            this.startDate.Name = "startDate";
            this.startDate.Size = new System.Drawing.Size(106, 20);
            this.startDate.TabIndex = 0;
            // 
            // endDate
            // 
            this.endDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.endDate.Location = new System.Drawing.Point(162, 29);
            this.endDate.Name = "endDate";
            this.endDate.Size = new System.Drawing.Size(106, 20);
            this.endDate.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(28, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Start";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(159, 13);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(26, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "End";
            // 
            // dateRangeGrp
            // 
            this.dateRangeGrp.Controls.Add(this.startDate);
            this.dateRangeGrp.Controls.Add(this.label1);
            this.dateRangeGrp.Controls.Add(this.label2);
            this.dateRangeGrp.Controls.Add(this.endDate);
            this.dateRangeGrp.Location = new System.Drawing.Point(12, 12);
            this.dateRangeGrp.Name = "dateRangeGrp";
            this.dateRangeGrp.Size = new System.Drawing.Size(293, 62);
            this.dateRangeGrp.TabIndex = 4;
            this.dateRangeGrp.TabStop = false;
            // 
            // DateGrp
            // 
            this.DateGrp.Controls.Add(this.year);
            this.DateGrp.Controls.Add(this.month);
            this.DateGrp.Controls.Add(this.day);
            this.DateGrp.Controls.Add(this.week);
            this.DateGrp.Controls.Add(this.wday);
            this.DateGrp.Location = new System.Drawing.Point(43, 125);
            this.DateGrp.Name = "DateGrp";
            this.DateGrp.Size = new System.Drawing.Size(293, 66);
            this.DateGrp.TabIndex = 5;
            this.DateGrp.TabStop = false;
            // 
            // year
            // 
            this.year.Location = new System.Drawing.Point(236, 27);
            this.year.Name = "year";
            this.year.Size = new System.Drawing.Size(50, 21);
            this.year.TabIndex = 3;
            // 
            // month
            // 
            this.month.FormattingEnabled = true;
            this.month.Location = new System.Drawing.Point(155, 27);
            this.month.Name = "month";
            this.month.Size = new System.Drawing.Size(73, 21);
            this.month.TabIndex = 2;
            // 
            // day
            // 
            this.day.FormattingEnabled = true;
            this.day.Location = new System.Drawing.Point(95, 27);
            this.day.Name = "day";
            this.day.Size = new System.Drawing.Size(53, 21);
            this.day.TabIndex = 1;
            // 
            // week
            // 
            this.week.FormattingEnabled = true;
            this.week.Location = new System.Drawing.Point(95, 27);
            this.week.Name = "week";
            this.week.Size = new System.Drawing.Size(103, 21);
            this.week.TabIndex = 1;
            // 
            // wday
            // 
            this.wday.FormattingEnabled = true;
            this.wday.Location = new System.Drawing.Point(16, 27);
            this.wday.Name = "wday";
            this.wday.Size = new System.Drawing.Size(79, 21);
            this.wday.TabIndex = 0;
            // 
            // btOk
            // 
            this.btOk.Location = new System.Drawing.Point(73, 90);
            this.btOk.Name = "btOk";
            this.btOk.Size = new System.Drawing.Size(76, 29);
            this.btOk.TabIndex = 6;
            this.btOk.Text = "OK";
            this.btOk.UseVisualStyleBackColor = true;
            this.btOk.Click += new System.EventHandler(this.btOk_Click);
            // 
            // btCancel
            // 
            this.btCancel.Location = new System.Drawing.Point(192, 90);
            this.btCancel.Name = "btCancel";
            this.btCancel.Size = new System.Drawing.Size(76, 29);
            this.btCancel.TabIndex = 7;
            this.btCancel.Text = "Cancel";
            this.btCancel.UseVisualStyleBackColor = true;
            this.btCancel.Click += new System.EventHandler(this.btCancel_Click);
            // 
            // CalendarEntryEdit
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(322, 148);
            this.Controls.Add(this.btCancel);
            this.Controls.Add(this.btOk);
            this.Controls.Add(this.DateGrp);
            this.Controls.Add(this.dateRangeGrp);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CalendarEntryEdit";
            this.Text = "Calendar Entry";
            this.dateRangeGrp.ResumeLayout(false);
            this.dateRangeGrp.PerformLayout();
            this.DateGrp.ResumeLayout(false);
            this.DateGrp.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.DateTimePicker startDate;
        private System.Windows.Forms.DateTimePicker endDate;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox dateRangeGrp;
        private System.Windows.Forms.GroupBox DateGrp;
        private System.Windows.Forms.TextBox year;
        private System.Windows.Forms.ComboBox month;
        private System.Windows.Forms.ComboBox day;
        private System.Windows.Forms.ComboBox week;
        private System.Windows.Forms.ComboBox wday;
        private System.Windows.Forms.Button btOk;
        private System.Windows.Forms.Button btCancel;
    }
    #endregion
}
