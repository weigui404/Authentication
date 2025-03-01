// Copyright (C) 2024 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.Util;
using AndroidX.Camera.Core;
using Stratum.ZXing;
using Serilog;
using ImageFormat = Stratum.ZXing.ImageFormat;
using Log = Serilog.Log;

namespace Stratum.Droid.QrCode
{
    public class QrCodeImageAnalyser : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        public event EventHandler<string> QrCodeScanned;
        public Size DefaultTargetResolution => new(1280, 720);
        
        private readonly ILogger _log = Log.ForContext<QrCodeImageAnalyser>();

        private readonly QrCodeReader _qrCodeReader = new(new ReaderOptions
        {
            TryHarder = true,
            TryInvert = true,
            Binarizer = Binarizer.GlobalHistogram
        });
        
        public void Analyze(IImageProxy imageProxy)
        {
            if (imageProxy.Image == null)
            {
                return;
            }

            try
            {
                AnalyseInternal(imageProxy);
            }
            finally
            {
                imageProxy.Close();
            }
        }

        private void AnalyseInternal(IImageProxy imageProxy)
        {
            using var yuvPlane = imageProxy.Image.GetPlanes()[0];
            ReadOnlySpan<byte> bytes;
            
            unsafe
            {
                var bufferAddress = yuvPlane.Buffer.GetDirectBufferAddress().ToPointer();
                bytes = new ReadOnlySpan<byte>(bufferAddress, yuvPlane.Buffer.Remaining());
            }
            
            using var imageView = new ImageView(bytes, imageProxy.Width, imageProxy.Height, ImageFormat.Lum, yuvPlane.RowStride);
            imageView.Crop(imageProxy.CropRect.Left, imageProxy.CropRect.Top, imageProxy.CropRect.Width(), imageProxy.CropRect.Height());
            imageView.Rotate(imageProxy.ImageInfo.RotationDegrees);
            
            string result;
            
            try
            {
                result = _qrCodeReader.Read(imageView);
            }
            catch (QrCodeException e)
            {
                _log.Warning(e, "Error scanning QR code: {Type}", e.Type);
                return;
            }

            if (result != null)
            {
                QrCodeScanned?.Invoke(this, result);
            }
        }
    }
}
