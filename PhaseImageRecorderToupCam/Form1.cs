﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using ToupTek;
using System.Runtime.InteropServices;
using RecorderCore;
using System.Linq;

namespace PhaseImageRecorderToupCam
{
    public partial class Form1 : Form
    {
        private bool started = false;
        private delegate void DelegateEvent(ToupCam.eEVENT[] ev);
        private ToupCam toupcam_ = null;
        private Bitmap bmp_ = null;
        private DelegateEvent ev_ = null;
        private FPSCounter fPSCounter = new FPSCounter(30);
        private void savefile(IntPtr pData, ref ToupCam.BITMAPINFOHEADER header)
        {
            Bitmap bmp = new Bitmap(header.biWidth, header.biHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, header.biWidth, header.biHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);

            ToupCam.CopyMemory(bmpdata.Scan0, pData, header.biSizeImage);

            bmp.UnlockBits(bmpdata);

            bmp.Save("toupcamdemowinformcs2.jpg");
        }

        private void OnEventError()
        {
            if (toupcam_ != null)
            {
                toupcam_.Close();
                toupcam_ = null;
            }
            MessageBox.Show("Error");
        }

        private void OnEventDisconnected()
        {
            if (toupcam_ != null)
            {
                toupcam_.Close();
                toupcam_ = null;
            }
            MessageBox.Show("The camera is disconnected, maybe has been pulled out.");
        }

        private void OnEventExposure()
        {
            if (toupcam_ != null)
            {
                uint nTime = 0;
                if (toupcam_.get_ExpoTime(out nTime))
                {
                    trackBar1.Value = (int)nTime;
                    label1.Text = (nTime / 1000).ToString() + " ms";
                }
            }
        }

        private bool isFrameSetted = false;
        private void OnEventImage()
        {
            if (bmp_ != null)
            {
                uint nWidth = 0, nHeight = 0;
                if (radioButton2.Checked)
                {
                    byte[,,] arr = new byte[bmp_.Height, bmp_.Width, 3];
                    IntPtr pointer = Marshal.UnsafeAddrOfPinnedArrayElement(arr, 0);
                    
                    toupcam_.PullImage(pointer, 24, out nWidth, out nHeight);
                    int x = trackBar4.Value;
                    int y = trackBar5.Value;
                    int w = int.Parse(comboBox2.Text);
                    int h = int.Parse(comboBox3.Text); ;
                    //Marshal.Copy(pointer, arr2, 0, arr.Length);
                    for (int i = y; i < h + y; i++)
                    {
                        arr[i, x, 1] = 255;
                        arr[i, x + 1, 1] = 255;
                        arr[i, x + 2, 1] = 255;
                        arr[i, x + w, 1] = 255;
                        arr[i, x + w - 1, 1] = 255;
                        arr[i, x + w - 2, 1] = 255;
                    }
                    for (int j = x; j < w + x; j++)
                    {
                        arr[y, j, 1] = 255;
                        arr[y + 1, j, 1] = 255;
                        arr[y + 2, j, 1] = 255;
                        arr[y + h, j, 1] = 255;
                        arr[y + h - 1, j, 1] = 255;
                        arr[y + h - 2, j, 1] = 255;

                    }

                    //BitmapData bmpdata = bmp_.LockBits(new Rectangle(0, 0, bmp_.Width, bmp_.Height), ImageLockMode.WriteOnly, bmp_.PixelFormat);
                    //bmpdata.Scan0 = Marshal.UnsafeAddrOfPinnedArrayElement(arr, 0);
                    // bmp_.UnlockBits(bmpdata);

                    pictureBox1.Image = new Bitmap((int)nWidth, (int)nHeight, (int)nWidth * 3, PixelFormat.Format24bppRgb,
                       Marshal.UnsafeAddrOfPinnedArrayElement(arr, 0));
                    pictureBox1.Update();
                    //isFrameSetted = true;
                }
                else if (radioButton1.Checked)
                {
                    if (Adjusted)
                    {
                        byte[,,] arr1 = new byte[bmp_.Height, bmp_.Width, 3];
                        IntPtr pointer = Marshal.UnsafeAddrOfPinnedArrayElement(arr1, 0);
                        toupcam_.PullImage(pointer, 24, out nWidth, out nHeight);

                        //for (int i = 0; i < bmp_.Height; i++)
                        //{
                        //    for (int j = 0; j < bmp_.Width; j++)
                        //    {
                        //        if (i % 50 == 0)
                        //        {
                        //            arr1[i, j, 0] = 255;
                        //        }
                        //        if (j % 50 == 0)
                        //        {
                        //            arr1[i, j, 0] = 255;
                        //        }
                        //    }
                        //}

                        int x = trackBar4.Value;
                        int y = trackBar5.Value;
                        int w = int.Parse(comboBox2.Text);
                        int h = int.Parse(comboBox3.Text); ;

                        byte[,,] arr = new byte[h, w, 3];

                        for (int i = y; i < h + y; i++)
                        {
                            for (int j = x; j < w + x; j++)
                            {
                                arr[i - y, j - x, 0] = arr1[i, j, 0];
                                arr[i - y, j - x, 1] = arr1[i, j, 1];
                                arr[i - y, j - x, 2] = arr1[i, j, 2];
                            }
                        }


                        pictureBox1.Image = new Bitmap((int)w, (int)h, (int)w * 3, PixelFormat.Format24bppRgb,
                            Marshal.UnsafeAddrOfPinnedArrayElement(arr, 0));
                        pictureBox1.Update();

                      //  BitmapData bmpdata = bmp_.LockBits(new Rectangle(0, 0, bmp_.Width, bmp_.Height), ImageLockMode.WriteOnly, bmp_.PixelFormat);
                        //bmpdata.Scan0= Marshal.UnsafeAddrOfPinnedArrayElement(arr, 0);
                        //Marshal.Copy(arr, 0, bmpdata.Scan0, bmp_.Height * bmp_.Width * 3);
                       // bmp_.UnlockBits(bmpdata);
                    }
                    else
                    {
                        /*
                        BitmapData bmpdata = bmp_.LockBits(new Rectangle(0, 0, bmp_.Width, bmp_.Height), ImageLockMode.WriteOnly, bmp_.PixelFormat);
                        toupcam_.PullImage(bmpdata.Scan0, 24, out nWidth, out nHeight);
                        bmp_.UnlockBits(bmpdata);
                        pictureBox1.Image = bmp_;
                        pictureBox1.Invalidate();

                        */

                        byte[,,] arr1 = new byte[bmp_.Height, bmp_.Width, 3];
                        IntPtr pointer = Marshal.UnsafeAddrOfPinnedArrayElement(arr1, 0);
                        toupcam_.PullImage(pointer, 24, out nWidth, out nHeight);
                        pictureBox1.Image = new Bitmap(bmp_.Width, bmp_.Height, bmp_.Width * 3, PixelFormat.Format24bppRgb,
                            Marshal.UnsafeAddrOfPinnedArrayElement(arr1, 0));
                        pictureBox1.Update();

                    }
                }           
                label5.Text = Math.Round(fPSCounter.Count(), 2).ToString();
                label5.Update();

                //button4_Click(null, null);
            }
        }

        private void OnEventStillImage()
        {
            uint nWidth = 0, nHeight = 0;
            if (toupcam_.PullStillImage(IntPtr.Zero, 24, out nWidth, out nHeight))   /* peek the width and height */
            {
                Bitmap sbmp = new Bitmap((int)nWidth, (int)nHeight, PixelFormat.Format24bppRgb);

                BitmapData bmpdata = sbmp.LockBits(new Rectangle(0, 0, sbmp.Width, sbmp.Height), ImageLockMode.WriteOnly, sbmp.PixelFormat);
                toupcam_.PullStillImage(bmpdata.Scan0, 24, out nWidth, out nHeight);
                sbmp.UnlockBits(bmpdata);

                sbmp.Save("toupcamdemowinformcs2.jpg");
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button2.Enabled = false;
            button3.Enabled = false;
            trackBar1.Enabled = false;
            trackBar2.Enabled = false;
            trackBar3.Enabled = false;
            checkBox1.Enabled = false;
            comboBox1.Enabled = false;
        }

        private void DelegateOnEvent(ToupCam.eEVENT[] ev)
        {
            switch (ev[0])
            {
                case ToupCam.eEVENT.EVENT_ERROR:
                    OnEventError();
                    break;
                case ToupCam.eEVENT.EVENT_DISCONNECTED:
                    OnEventDisconnected();
                    break;
                case ToupCam.eEVENT.EVENT_EXPOSURE:
                    OnEventExposure();
                    break;
                case ToupCam.eEVENT.EVENT_IMAGE:
                    OnEventImage();
                    break;
                case ToupCam.eEVENT.EVENT_STILLIMAGE:
                    OnEventStillImage();
                    break;
                case ToupCam.eEVENT.EVENT_TEMPTINT:
                    OnEventTempTint();
                    break;
            }
        }

        private void DelegateOnEventCallback(ToupCam.eEVENT ev)
        {
            /* this delegate is call by internal thread of toupcam.dll which is NOT the same of UI thread.
             * Why we use BeginInvoke, Please see:
             * http://msdn.microsoft.com/en-us/magazine/cc300429.aspx
             * http://msdn.microsoft.com/en-us/magazine/cc188732.aspx
             * http://stackoverflow.com/questions/1364116/avoiding-the-woes-of-invoke-begininvoke-in-cross-thread-winform-event-handling
            */
            BeginInvoke(ev_, new ToupCam.eEVENT[1] { ev });
        }

        private void OnStart(object sender, EventArgs e)
        {
            if (toupcam_ != null)
                return;

            ToupCam.Instance[] arr = ToupCam.Enum();
            if (arr.Length <= 0)
            {
                MessageBox.Show("no device");
            }
            else
            {
                toupcam_ = new ToupCam();
                if (!toupcam_.Open(arr[0].id))
                {
                    toupcam_ = null;
                }
                else
                {
                    checkBox1.Enabled = true;
                    trackBar1.Enabled = true;
                    trackBar2.Enabled = true;
                    trackBar3.Enabled = true;
                    comboBox1.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button2.ContextMenuStrip = null;
                    InitSnapContextMenuAndExpoTimeRange();

                    trackBar2.SetRange(2000, 15000);
                    trackBar3.SetRange(200, 2500);
                    OnEventTempTint();

                    uint resnum = toupcam_.ResolutionNumber;
                    uint eSize = 0;
                    if (toupcam_.get_eSize(out eSize))
                    {
                        for (uint i = 0; i < resnum; ++i)
                        {
                            int w = 0, h = 0;
                            if (toupcam_.get_Resolution(i, out w, out h))
                                comboBox1.Items.Add(w.ToString() + "*" + h.ToString());
                        }
                        comboBox1.SelectedIndex = (int)eSize;

                        int width = 0, height = 0;
                        if (toupcam_.get_Size(out width, out height))
                        {
                            bmp_ = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                            ev_ = new DelegateEvent(DelegateOnEvent);
                            if (!toupcam_.StartPullModeWithCallback(new ToupTek.ToupCam.DelegateEventCallback(DelegateOnEventCallback)))
                                MessageBox.Show("failed to start device");
                            else
                            {
                                bool autoexpo = true;
                                toupcam_.get_AutoExpoEnable(out autoexpo);
                                checkBox1.Checked = autoexpo;
                                trackBar1.Enabled = !checkBox1.Checked;
                            }
                        }
                        SetLimits(width, height);
                        started = true;
                    }
                }
            }
        }

        private void SnapClickedHandler(object sender, ToolStripItemClickedEventArgs e)
        {
            int k = button2.ContextMenuStrip.Items.IndexOf(e.ClickedItem);
            if (k >= 0)
                toupcam_.Snap((uint)k);
        }

        private void InitSnapContextMenuAndExpoTimeRange()
        {
            if (toupcam_ == null)
                return;

            uint nMin = 0, nMax = 0, nDef = 0;
            if (toupcam_.get_ExpTimeRange(out nMin, out nMax, out nDef))
                trackBar1.SetRange((int)nMin, (int)nMax);
            OnEventExposure();

            if (toupcam_.StillResolutionNumber <= 0)
                return;
            
            button2.ContextMenuStrip = new ContextMenuStrip();
            button2.ContextMenuStrip.ItemClicked += new ToolStripItemClickedEventHandler(this.SnapClickedHandler);

            if (toupcam_.StillResolutionNumber < toupcam_.ResolutionNumber)
            {
                uint eSize = 0;
                if (toupcam_.get_eSize(out eSize))
                {
                    if (0 == eSize)
                    {
                        StringBuilder sb = new StringBuilder();
                        int w = 0, h = 0;
                        toupcam_.get_Resolution(eSize, out w, out h);
                        sb.Append(w);
                        sb.Append(" * ");
                        sb.Append(h);
                        button2.ContextMenuStrip.Items.Add(sb.ToString());
                        return;
                    }
                }
            }

            for (uint i = 0; i < toupcam_.ResolutionNumber; ++i)
            {
                StringBuilder sb = new StringBuilder();
                int w = 0, h = 0;
                toupcam_.get_Resolution(i, out w, out h);
                sb.Append(w);
                sb.Append(" * ");
                sb.Append(h);
                button2.ContextMenuStrip.Items.Add(sb.ToString());
            }
        }

        private void OnSnap(object sender, EventArgs e)
        {
            if (toupcam_ != null)
            {
                if (toupcam_.StillResolutionNumber <= 0)
                {
                    if (bmp_ != null)
                    {
                        bmp_.Save("toupcamdemowinformcs2.jpg");
                    }
                }
                else
                {
                    if (button2.ContextMenuStrip != null)
                        button2.ContextMenuStrip.Show(Cursor.Position);
                }
            }
        }

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            if (toupcam_ != null)
            {
                toupcam_.Close();
                toupcam_ = null;
            }
        }

        private void OnSelectResolution(object sender, EventArgs e)
        {
            if (toupcam_ != null)
            {
                uint eSize = 0;
                if (toupcam_.get_eSize(out eSize))
                {
                    if (eSize != comboBox1.SelectedIndex)
                    {
                        button2.ContextMenuStrip = null;

                        toupcam_.Stop();
                        toupcam_.put_eSize((uint)comboBox1.SelectedIndex);

                        InitSnapContextMenuAndExpoTimeRange();
                        OnEventTempTint();

                        int width = 0, height = 0;
                        if (toupcam_.get_Size(out width, out height))
                        {
                            bmp_ = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                            ev_ = new DelegateEvent(DelegateOnEvent);
                            toupcam_.StartPullModeWithCallback(new ToupTek.ToupCam.DelegateEventCallback(DelegateOnEventCallback));
                        }
                        SetLimits(width, height);
                    }
                }
            }
        }

        private void SetMinFramesize(int width, int height)
        {
            int w = int.Parse(comboBox2.Text);
            int h = int.Parse(comboBox3.Text);
            if (w > width && comboBox2.SelectedIndex > 1)
            {
                comboBox2.SelectedIndex -= 1;
                comboBox2.Update();
                SetMinFramesize(width, height);
            }
            if (h > height && comboBox3.SelectedIndex > 1)
            {
                comboBox3.SelectedIndex -= 1;
                comboBox3.Update();
                SetMinFramesize(width, height);
            }
            
        }

        private void SetFrameStartPositionMaxValues(int width, int height)
        {
            int w = int.Parse(comboBox2.Text);
            int h = int.Parse(comboBox3.Text);
            int newWight = width - w - 1;
            int newHeight = height - h - 1;
            if (newWight >= 0)
            {
                if (trackBar4.Value > newWight) trackBar4.Value = newWight;
                trackBar4.Maximum = newWight;
                trackBar4.Update();
            }
            if (newHeight >= 0)
            {
                if (trackBar5.Value > newHeight) trackBar5.Value = newHeight;
                trackBar5.Maximum = newHeight;
                trackBar5.Update();
            }
        }
        private void SetLimits(int width, int height)
        {
            SetMinFramesize(width,height);
            SetFrameStartPositionMaxValues(width, height);
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (toupcam_ != null)
                toupcam_.put_AutoExpoEnable(checkBox1.Checked);
            trackBar1.Enabled = !checkBox1.Checked;
        }

        private void OnExpoValueChange(object sender, EventArgs e)
        {
            if (!checkBox1.Checked)
            {
                if (toupcam_ != null)
                {
                    uint n = (uint)trackBar1.Value;
                    toupcam_.put_ExpoTime(n);
                    label1.Text = (n / 1000).ToString() + " ms";
                }
            }
        }

        private void Form_SizeChanged(object sender, EventArgs e)
        {
            pictureBox1.Width = ClientRectangle.Right - button1.Bounds.Right - 20;
            pictureBox1.Height = ClientRectangle.Height - 8;
        }

        private void OnEventTempTint()
        {
            if (toupcam_ != null)
            {
                int nTemp = 0, nTint = 0;
                if (toupcam_.get_TempTint(out nTemp, out nTint))
                {
                    label2.Text = nTemp.ToString();
                    label3.Text = nTint.ToString();
                    trackBar2.Value = nTemp;
                    trackBar3.Value = nTint;
                }
            }
        }

        private void OnWhiteBalanceOnePush(object sender, EventArgs e)
        {
            if (toupcam_ != null)
                toupcam_.AwbOnePush(null);
        }

        private void OnTempTintChanged(object sender, EventArgs e)
        {
            if (toupcam_ != null)
                toupcam_.put_TempTint(trackBar2.Value, trackBar3.Value);
            label2.Text = trackBar2.Value.ToString();
            label3.Text = trackBar3.Value.ToString();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int RectangleWight = 1024* pictureBox1.Width/pictureBox1.Image.Width ;
            int RectangleHeight = 1024* pictureBox1.Height/pictureBox1.Image.Height;


            System.Drawing.Pen myPen = new System.Drawing.Pen(System.Drawing.Color.Green);
            System.Drawing.Graphics formGraphics;

            formGraphics = pictureBox1.CreateGraphics();
            formGraphics.DrawRectangle(myPen, new Rectangle(0, 0, RectangleWight, RectangleHeight));
            myPen.Dispose();
            formGraphics.Dispose();
        }

        private bool Worked = false;
        private bool Adjusted = false;
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (Worked) Adjusted = true;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            Worked = true;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (started)
                SetLimits(bmp_.Width,bmp_.Height);
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {

        }

        private void trackBar4_Scroll(object sender, EventArgs e)
        {

        }

        private void trackBar5_Scroll(object sender, EventArgs e)
        {

        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (started)
                SetLimits(bmp_.Width, bmp_.Height);
        }
    }
}
