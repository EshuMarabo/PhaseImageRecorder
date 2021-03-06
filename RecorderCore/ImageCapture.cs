﻿using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RecorderCore
{
   //todo вынести функционал потока, ставящегося на паузу в отдельный класс
    public class ImageCapture
    {
        #region fields
        public delegate void ImageReciever(Mat image);
        public delegate void ExternalAction(int count);
        public event ImageReciever rec;
        public event ExternalAction action;
        public double Wavelength;

        protected Thread thread;
        protected object ReadSettingsLocker = new object();


        private int CameraNumber = 0;
        private int FramePause = 0;
        private double MaxFrameCounter = 0;

        protected Emgu.CV.VideoCapture capture;
        protected CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        protected ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public bool paused = false;
        #endregion

        #region constructors
        public ImageCapture()
        {
            lock (ReadSettingsLocker)
            {
                capture = new VideoCapture(0);
                thread = new Thread(new ParameterizedThreadStart(grab));
            }
        }

        public void Start()
        {
            thread.Start(CancellationTokenSource.Token);
        }

        public void Stop()
        {
            CancellationTokenSource.Cancel();
        }

        public void Pause()
        {
            _lock.TryEnterWriteLock(-1);
            paused = true;
            _lock.ExitWriteLock();
        }
        public void PauseRelease()
        {
            _lock.TryEnterWriteLock(-1);
            paused = false;
            _lock.ExitWriteLock();
        }

        #endregion

        #region image grabbing

        private void grab(object cancellationToken)
        {
            CancellationToken ct = (CancellationToken)cancellationToken;
            int frameCounter = 0;
            while (!ct.IsCancellationRequested)
            {
                _lock.TryEnterReadLock(-1);
                bool local_pause = paused;
                _lock.ExitReadLock();
                if (!local_pause)
                {
                    lock (ReadSettingsLocker)
                    {
                        if (action != null) action.Invoke(frameCounter);
                        Thread.Sleep(FramePause);
                        Grab();
                        frameCounter = frameCounter < MaxFrameCounter ? frameCounter + 1 : 0;
                    }
                }
                else Thread.Sleep(300);
            }

        }

        private void Grab()
        {
            Mat mat = new Mat();
            capture.Grab();
            capture.Retrieve(mat);
            if (rec != null&& !mat.Size.IsEmpty)
            {
                rec.Invoke(mat);
            } 
        }

        #endregion

        #region external ruling
        public void UpdateWavelength(double Wavelength)
        {
            lock (ReadSettingsLocker)
            {
                this.Wavelength = Wavelength;
            }
        }

        public void UpdateCamera(int CameraNumber)
        {
            lock (ReadSettingsLocker)
            {
                if (this.CameraNumber!= CameraNumber)
                    capture = new VideoCapture(CameraNumber);
            }
        }

        public void UpdateMaxFrameCounter(int MaxFrameCounter)
        {
            lock (ReadSettingsLocker)
            {
                this.MaxFrameCounter = MaxFrameCounter;
            }
        }

        public void UpdateFramePause(int FramePause)
        {
            lock (ReadSettingsLocker)
            {
                this.FramePause = FramePause;
            }
        }

        #endregion

        #region add and remove handlers
        public void AddReciever(ImageReciever imageReciever)
        {
            rec += imageReciever;
        }
        public void AddExternalAction(ExternalAction externalAction)
        {
            action += externalAction;
        }
        public void RemoveReciever(ImageReciever imageReciever)
        {
            rec -= imageReciever;
        }
        public void RemoveExternalAction(ExternalAction externalAction)
        {
            action -= externalAction;
        }
        #endregion

    }
}
