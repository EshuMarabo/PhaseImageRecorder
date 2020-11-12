﻿using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace RecorderCore
{
    public abstract class PhaseImage
    {
        internal static class NativeMethods
        {
            [DllImport(@"uwr.dll", EntryPoint = "unwrap2D")]
            internal static extern void unwrap(
                IntPtr wrappedImagePointer, IntPtr unwrappedImagePointer, IntPtr maskPointer, int image_width, int image_height,
                  int wrap_around_x, int wrap_around_y, char user_seed, uint seed);

        }

        #region fields
        public static double[,] _GetArrayFromMat(Mat mat, bool Dispose = true)
        {
            Image<Rgb, double> image = mat.ToImage<Rgb, double>();
            int Dim0 = image.Data.GetUpperBound(0) + 1;
            int Dim1 = image.Data.GetUpperBound(1) + 1;
            double[,] ForReturn = new double[Dim0, Dim1];
            for (int i = 0; i < Dim0; i++)
            {
                for (int j = 0; j < Dim1; j++)
                {
                    ForReturn[i, j] = image.Data[i, j, 0];
                }
            }
            if (Dispose)
            {
                mat.Dispose();
                image.Dispose();
            }
            return ForReturn;
        }
        public void GetArrayFromMat(Mat mat, bool Dispose = true)
        {
            Image<Rgb, double> image = mat.ToImage<Rgb, double>();
            int Dim0 = image.Data.GetUpperBound(0) + 1;
            int Dim1 = image.Data.GetUpperBound(1) + 1;
            Image = new double[Dim0, Dim1];
            ImageForUI = new byte[Dim0, Dim1, image.Data.GetUpperBound(2) + 1];
            for (int i = 0; i < Dim0; i++)
            {
                for (int j = 0; j < Dim1; j++)
                {
                    Image[i, j] = image.Data[i, j, 0];
                    ImageForUI[i, j, 0] = (byte)image.Data[i, j, 0];
                    ImageForUI[i, j, 1] = (byte)image.Data[i, j, 1];
                    ImageForUI[i, j, 2] = (byte)image.Data[i, j, 2];
                }
            }
            if (Dispose)
            {
                mat.Dispose();
                image.Dispose();
            }

        }
        public void GetArrayFromMat(double[,] image)
        {
            int Dim0 = image.GetUpperBound(0) + 1;
            int Dim1 = image.GetUpperBound(1) + 1;
            Image = image;
            ImageForUI = new byte[Dim0, Dim1, 3];
            for (int i = 0; i < Dim0; i++)
            {
                for (int j = 0; j < Dim1; j++)
                {
                    ImageForUI[i, j, 0] = (byte)image[i, j];
                    ImageForUI[i, j, 1] = (byte)image[i, j];
                    ImageForUI[i, j, 2] = (byte)image[i, j];
                }
            }
        }
        public SettingsContainer.ProcessingStep status { get; private set; }
        public SettingsContainer.ProcessingStep MaxProcessingStep { get; set; }
        public DateTime RecordingTime { get; private set; }
        public double[,] Image { get; internal set; }
        public byte[,,] ImageForUI { get; internal set; }
        public Bitmap bitmap { get; internal set; }
        #endregion
        public PhaseImage(Mat image)
        {
            RecordingTime = DateTime.UtcNow;
            status = SettingsContainer.ProcessingStep.Interferogramm;
            GetArrayFromMat(image, true);
        }
        public PhaseImage(double[,] image)
        {
            RecordingTime = DateTime.UtcNow;
            status = SettingsContainer.ProcessingStep.Interferogramm;
            GetArrayFromMat(image);
        }

        public virtual void CalculatePhaseImage()
        {
            if (status <= SettingsContainer.ProcessingStep.Interferogramm)
            {

                status = SettingsContainer.ProcessingStep.WrappedPhaseImage;
            }

        }
        public virtual void Unwrap()
        {
            if (MaxProcessingStep < SettingsContainer.ProcessingStep.UnwrappedPhaseImage) return;
            double[,] matrix = new double[Image.GetUpperBound(0) + 1, Image.GetUpperBound(1) + 1];
            byte[,] mask = new byte[Image.GetUpperBound(0) + 1, Image.GetUpperBound(1) + 1];
            NativeMethods.unwrap(Marshal.UnsafeAddrOfPinnedArrayElement(Image, 0),
                Marshal.UnsafeAddrOfPinnedArrayElement(matrix, 0),
                Marshal.UnsafeAddrOfPinnedArrayElement(mask, 0), Image.GetUpperBound(1) + 1, Image.GetUpperBound(0) + 1, 0, 0, (char)0, (uint)1);
            double max1 = 0;
            double max = 0;
            double min1 = 0;
            double min = 0;
            for (int i = 0; i <= Image.GetUpperBound(0); i++)
            {
                for (int j = 0; j <= Image.GetUpperBound(1); j++)
                {
                    double val1 = matrix[i, j];
                    double val2 = Image[i, j];
                    if (val1 < min) min = val1;
                    if (val1 > max) max = val1;
                    if (val2 < min1) min1 = val2;
                    if (val2 > max1) max1 = val2;

                }
            }
            for (int i = 0; i <= Image.GetUpperBound(0); i++)
            {
                for (int j = 0; j <= Image.GetUpperBound(1); j++)
                {
                    double val1 = matrix[i, j];
                    ImageForUI[i, j, 0] = (byte)(255 * (val1 - min) / (max - min));
                    ImageForUI[i, j, 1] = (byte)(255 * (val1 - min) / (max - min));
                    ImageForUI[i, j, 2] = (byte)(255 * (val1 - min) / (max - min));
                }
            }
            Image = matrix;
            if (status <= SettingsContainer.ProcessingStep.WrappedPhaseImage)
            {

                status = SettingsContainer.ProcessingStep.UnwrappedPhaseImage;
            }
        }
        public virtual void Process()
        {
            if (status <= SettingsContainer.ProcessingStep.UnwrappedPhaseImage)
            {

                status = SettingsContainer.ProcessingStep.ProcessedImage;
            }

        }
    }

}
