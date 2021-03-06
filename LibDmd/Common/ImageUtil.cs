﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NLog;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Drawing.Point;

namespace LibDmd.Common
{
	public class ImageUtil
	{
		private static readonly Dictionary<int, Frame> FrameDatas = new Dictionary<int,Frame>();
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Returns a frame object for the given size.
		/// 
		/// The idea is not to instantiate a frame object for every frame but 
		/// only for every size.
		/// </summary>
		/// <param name="width">Frame width in pixels</param>
		/// <param name="height">Frame height in pixels</param>
		/// <returns></returns>
		private static Frame FrameData(int width, int height)
		{
			var key = width * height;
			if (!FrameDatas.ContainsKey(key)) {
				FrameDatas.Add(key, new Frame());
			}
			return FrameDatas[key];
		}

		public static byte[] ConvertToGray2(BitmapSource bmp, double lum = 1)
		{
			double devnull;
			return ConvertToGray2(bmp, lum, out devnull);
		}

		/// <summary>
		/// Converts a bitmap to a 2-bit grayscale array.
		/// </summary>
		/// <param name="bmp">Source bitmap</param>
		/// <param name="lum">Multiply luminosity</param>
		/// <param name="hue">Detected hue</param>
		/// <returns>Array with value for every pixel between 0 and 3</returns>
		public static byte[] ConvertToGray2(BitmapSource bmp, double lum, out double hue)
		{
			var frame = new byte[bmp.PixelWidth * bmp.PixelHeight];
			var bytesPerPixel = (bmp.Format.BitsPerPixel + 7) / 8;
			var bytes = new byte[bytesPerPixel];
			var rect = new Int32Rect(0, 0, 1, 1);
			double imageHue = 0;
			for (var y = 0; y < bmp.PixelHeight; y++) {
				rect.Y = y;
				for (var x = 0; x < bmp.PixelWidth; x++) {

					rect.X = x;
					bmp.CopyPixels(rect, bytes, bytesPerPixel, 0);

					// convert to HSL
					double h;
					double saturation;
					double luminosity;
					ColorUtil.RgbToHsl(bytes[2], bytes[1], bytes[0], out h, out saturation, out luminosity);

					frame[y * bmp.PixelWidth + x] = (byte)Math.Round(Math.Min(1, luminosity * lum) * 3d);

					if (luminosity > 0) {
						imageHue = h;
					}
				}
			}
			hue = imageHue;
			return frame;
		}

		/// <summary>
		/// Converts an RGB24 frame to a grayscale array.
		/// </summary>
		/// <param name="width">Width in pixels</param>
		/// <param name="height">Height in pixels</param>
		/// <param name="frameRgb24">RGB24 frame, top left to bottom right, three bytes per pixel with values between 0 and 255</param>
		/// <param name="numColors">Number of gray tones. 4 for 2 bit, 16 for 4 bit</param>
		/// <returns>Gray2 frame, top left to bottom right, one byte per pixel with values between 0 and 3</returns>
		public static byte[] ConvertToGray(int width, int height, byte[] frameRgb24, int numColors)
		{
			var frame = new byte[width * height];
			var pos = 0;
			for (var y = 0; y < height; y++) {
				for (var x = 0; x < width * 3; x += 3) {
					var rgbPos = y * width * 3 + x;

					// convert to HSL
					double hue;
					double saturation;
					double luminosity;
					ColorUtil.RgbToHsl(frameRgb24[rgbPos], frameRgb24[rgbPos + 1], frameRgb24[rgbPos + 2], out hue, out saturation, out luminosity);
					frame[pos++] = (byte)Math.Round(luminosity * (numColors - 1));
				}
			}
			return frame;
		}

		/// <summary>
		/// Converts a bitmap to a 4-bit grayscale array.
		/// </summary>
		/// <param name="bmp">Source bitmap</param>
		/// <param name="lum">Multiply luminosity</param>
		/// <returns>Array with value for every pixel between 0 and 15</returns>
		public static byte[] ConvertToGray4(BitmapSource bmp, double lum = 1)
		{
			var frame = new byte[bmp.PixelWidth * bmp.PixelHeight];
			var bytesPerPixel = (bmp.Format.BitsPerPixel + 7) / 8;
			var bytes = new byte[bytesPerPixel];
			var rect = new Int32Rect(0, 0, 1, 1);
			for (var y = 0; y < bmp.PixelHeight; y++) {
				rect.Y = y;
				for (var x = 0; x < bmp.PixelWidth; x++) {

					rect.X = x;
					bmp.CopyPixels(rect, bytes, bytesPerPixel, 0);

					// convert to HSL
					double hue;
					double saturation;
					double luminosity;
					ColorUtil.RgbToHsl(bytes[2], bytes[1], bytes[0], out hue, out saturation, out luminosity);

					frame[y * bmp.PixelWidth + x] = (byte)Math.Round(luminosity * 15d * lum);
				}
			}
			return frame;
		}

		/// <summary>
		/// Converts a bitmap to an RGB24 array.
		/// </summary>
		/// <param name="bmp">Source bitmap</param>
		/// <param name="lum">Multiply luminosity</param>
		/// <returns>Array filled with RGB values for each pixel between 0 and 255.</returns>
		public static byte[] ConvertToRgb24(BitmapSource bmp, double lum = 1)
		{
			var frame = new byte[bmp.PixelWidth * bmp.PixelHeight * 3];
			ConvertToRgb24(bmp, frame, 0, lum);
			return frame;
		}

		/// <summary>
		/// Converts a bitmap to an RGB24 array.
		/// </summary>
		/// <param name="bmp">Source bitmap</param>
		/// <param name="buffer">Destination buffer. Will be filled with RGB values for each pixel between 0 and 255.</param>
		/// <param name="offset">Offset in destination array</param>
		/// <param name="lum">Multiply luminosity</param>
		public static void ConvertToRgb24(BitmapSource bmp, byte[] buffer, int offset = 0, double lum = 1)
		{
			var bytesPerPixel = (bmp.Format.BitsPerPixel + 7) / 8;
			var bytes = new byte[bytesPerPixel];
			var rect = new Int32Rect(0, 0, 1, 1);
			var pos = offset;
			for (var y = 0; y < bmp.PixelHeight; y++) {
				for (var x = 0; x < bmp.PixelWidth; x++) {
					rect.X = x;
					rect.Y = y;
					bmp.CopyPixels(rect, bytes, bytesPerPixel, 0);

					if (Math.Abs(lum - 1) > 0.01) {
						double hue, saturation, luminosity;
						byte r, g, b;
						ColorUtil.RgbToHsl(bytes[2], bytes[1], bytes[0], out hue, out saturation, out luminosity);
						ColorUtil.HslToRgb(hue, saturation, luminosity * lum, out r, out g, out b);
						buffer[pos] = r;
						buffer[pos + 1] = g;
						buffer[pos + 2] = b;
						
					} else {
						buffer[pos] = bytes[2];      // r
						buffer[pos + 1] = bytes[1];  // g
						buffer[pos + 2] = bytes[0];  // b
					}
					pos += 3;
				}
			}
		}

		/// <summary>
		/// Converts an image to a BitmapSouce
		/// </summary>
		/// <param name="img">Image to convert</param>
		/// <returns>Converted bitmap</returns>
		public static BitmapSource ConvertToBitmap(Image img)
		{
			var bitmap = new Bitmap(img);
			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

			var bitmapSource = BitmapSource.Create(
				bitmapData.Width, bitmapData.Height, 96, 96, ConvertPixelFormat(bitmap.PixelFormat), null,
				bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

			bitmap.UnlockBits(bitmapData);
			return bitmapSource;
		}

		public static Image ConvertToImage(BitmapSource bitmapsource)
		{
			// convert image format
			var src = new FormatConvertedBitmap();
			src.BeginInit();
			src.Source = bitmapsource;
			src.DestinationFormat = PixelFormats.Bgra32;
			src.EndInit();

			// copy to bitmap
			var bitmap = new Bitmap(src.PixelWidth, src.PixelHeight, PixelFormat.Format32bppArgb);
			var data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			src.CopyPixels(System.Windows.Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
			bitmap.UnlockBits(data);

			return bitmap;
		}

		public static void ConvertRgb24ToDIB(int width, int height, byte[] from, byte[] to)
		{
			var pos = 0;
			for (var y = height - 1; y >= 0; y--) {
				for (var x = 0; x < width * 3; x += 3) {
					var fromPos = width * 3 * y + x;
					to[pos] = from[fromPos];
					to[pos + 1] = from[fromPos + 1];
					to[pos + 2] = from[fromPos + 2];
					pos += 3;
				}
			}
		}

		public static void ConvertRgb24ToBgr32(int width, int height, byte[] from, byte[] to)
		{
			var pos = 0;
			for (var y = 0; y < height; y++) {
				for (var x = 0; x < width * 3; x += 3) {
					var fromPos = width * 3 * y + x;
					to[pos] = from[fromPos + 2];
					to[pos + 1] = from[fromPos + 1];
					to[pos + 2] = from[fromPos];
					to[pos + 3] = 0;
					pos += 4;
				}
			}
		}

		/// <summary>
		/// Converts an 2-bit grayscale array to a bitmap.
		/// </summary>
		/// <param name="width">Width of the image</param>
		/// <param name="height">Height of the image</param>
		/// <param name="frame">2-bit grayscale array</param>
		/// <param name="hue">Hue in which the bitmap will be created</param>
		/// <param name="saturation">Saturation in which the bitmap will be created</param>
		/// <param name="luminosity">Maximal luminosity in which the bitmap will be created</param>
		/// <returns>Bitmap</returns>
		private static BitmapSource ConvertFromGray2(int width, int height, Frame frame, double hue, double saturation, double luminosity)
		{
			if (frame.Size > 0 && frame.Size != width * height) {
				throw new ArgumentException($"Must convert to {width}x{height} but frame buffer is {frame.Size} bytes");
			}

			var bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
			var bufferSize = (Math.Abs(bmp.BackBufferStride) * height + 2);
			var frameBuffer = new byte[bufferSize];

			var index = 0;
			bmp.Lock();
			for (var y = 0; y < height; y++) {
				for (var x = 0; x < width; x++) {

					try {
						var pixelLum = frame.Get(y * width + x); // 0 - 3
						var lum = luminosity * pixelLum / 3;
						byte red, green, blue;
						ColorUtil.HslToRgb(hue, saturation, lum, out red, out green, out blue);

						frameBuffer[index] = blue;
						frameBuffer[index + 1] = green;
						frameBuffer[index + 2] = red;
						index += 4;

					} catch (IndexOutOfRangeException e) {
						Logger.Error(e, $"Converting {width}x{height} with {frame.Size} bytes: Trying to get pixel at position {y * width + x}");
						throw;
					}
				}
			}
			bmp.WritePixels(new Int32Rect(0, 0, width, height), frameBuffer, bmp.BackBufferStride, 0);
			bmp.Unlock();
			bmp.Freeze();
			return bmp;
		}

		/// <summary>
		/// Converts an 4-bit grayscale array to a bitmap.
		/// </summary>
		/// <param name="width">Width of the image</param>
		/// <param name="height">Height of the image</param>
		/// <param name="frame">4-bit grayscale array</param>
		/// <param name="hue">Hue in which the bitmap will be created</param>
		/// <param name="saturation">Saturation in which the bitmap will be created</param>
		/// <param name="luminosity">Maximal luminosity in which the bitmap will be created</param>
		/// <returns>Bitmap</returns>
		private static BitmapSource ConvertFromGray4(int width, int height, Frame frame, double hue, double saturation, double luminosity)
		{
			var bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
			var bufferSize = (Math.Abs(bmp.BackBufferStride) * height + 2);
			var frameBuffer = new byte[bufferSize];

			var index = 0;
			bmp.Lock();
			for (var y = 0; y < height; y++) {
				for (var x = 0; x < width; x++) {

					var pixelLum = frame.Get(y * width + x);
					var lum = luminosity * pixelLum / 15;
					byte red, green, blue;
					ColorUtil.HslToRgb(hue, saturation, lum, out red, out green, out blue);

					frameBuffer[index] = blue;
					frameBuffer[index + 1] = green;
					frameBuffer[index + 2] = red;
					index += 4;
				}
			}
			bmp.WritePixels(new Int32Rect(0, 0, width, height), frameBuffer, bmp.BackBufferStride, 0);
			bmp.Unlock();
			bmp.Freeze();
			return bmp;
		}

		/// <summary>
		/// Converts an RGB24 array to a bitmap.
		/// </summary>
		/// <param name="width">Width of the image</param>
		/// <param name="height">Height of the image</param>
		/// <param name="frame">RGB values for each pixel between 0 and 255</param>
		/// <returns>Bitmap</returns>
		private static BitmapSource ConvertFromRgb24(int width, int height, Frame frame)
		{
			var bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
			var bufferSize = (Math.Abs(bmp.BackBufferStride) * height + 2);
			var frameBuffer = new byte[bufferSize];

			var destIndex = 0;
			var srcIndex = 0;
			bmp.Lock();
			for (var y = 0; y < height; y++) {
				for (var x = 0; x < width; x++) {
					frameBuffer[destIndex] = frame.Get(srcIndex + 2);
					frameBuffer[destIndex + 1] = frame.Get(srcIndex + 1);
					frameBuffer[destIndex + 2] = frame.Get(srcIndex);
					destIndex += 4;
					srcIndex += 3;
				}
			}
			bmp.WritePixels(new Int32Rect(0, 0, width, height), frameBuffer, bmp.BackBufferStride, 0);
			bmp.Unlock();
			bmp.Freeze();
			return bmp;
		}

		/// <summary>
		/// Converts an 2-bit grayscale array to a bitmap.
		/// </summary>
		/// <param name="width">Width of the image</param>
		/// <param name="height">Height of the image</param>
		/// <param name="frame">2-bit grayscale array</param>
		/// <param name="hue">Hue in which the bitmap will be created</param>
		/// <param name="saturation">Saturation in which the bitmap will be created</param>
		/// <param name="luminosity">Maximal luminosity in which the bitmap will be created</param>
		/// <returns>Bitmap</returns>
		public static unsafe BitmapSource ConvertFromGray2(int width, int height, byte* frame, double hue, double saturation, double luminosity)
		{
			return ConvertFromGray2(width, height, FrameData(width, height).With(frame), hue, saturation, luminosity);
		}

		/// <summary>
		/// Converts an 2-bit grayscale array to a bitmap.
		/// </summary>
		/// <param name="width">Width of the image</param>
		/// <param name="height">Height of the image</param>
		/// <param name="frame">2-bit grayscale array</param>
		/// <param name="hue">Hue in which the bitmap will be created</param>
		/// <param name="saturation">Saturation in which the bitmap will be created</param>
		/// <param name="luminosity">Maximal luminosity in which the bitmap will be created</param>
		/// <returns>Bitmap</returns>
		public static BitmapSource ConvertFromGray2(int width, int height, byte[] frame, double hue, double saturation, double luminosity)
		{
			return ConvertFromGray2(width, height, FrameData(width, height).With(frame), hue, saturation, luminosity);
		}

		/// <summary>
		/// Converts an 4-bit grayscale array to a bitmap.
		/// </summary>
		/// <param name="width">Width of the image</param>
		/// <param name="height">Height of the image</param>
		/// <param name="frame">4-bit grayscale array</param>
		/// <param name="hue">Hue in which the bitmap will be created</param>
		/// <param name="saturation">Saturation in which the bitmap will be created</param>
		/// <param name="luminosity">Maximal luminosity in which the bitmap will be created</param>
		/// <returns>Bitmap</returns>
		public static unsafe BitmapSource ConvertFromGray4(int width, int height, byte* frame, double hue, double saturation, double luminosity)
		{
			return ConvertFromGray4(width, height, FrameData(width, height).With(frame), hue, saturation, luminosity);
		}

		/// <summary>
		/// Converts an 4-bit grayscale array to a bitmap.
		/// </summary>
		/// <param name="width">Width of the image</param>
		/// <param name="height">Height of the image</param>
		/// <param name="frame">4-bit grayscale array</param>
		/// <param name="hue">Hue in which the bitmap will be created</param>
		/// <param name="saturation">Saturation in which the bitmap will be created</param>
		/// <param name="luminosity">Maximal luminosity in which the bitmap will be created</param>
		/// <returns>Bitmap</returns>
		public static BitmapSource ConvertFromGray4(int width, int height, byte[] frame, double hue, double saturation, double luminosity)
		{
			return ConvertFromGray4(width, height, FrameData(width, height).With(frame), hue, saturation, luminosity);
		}

		/// <summary>
		/// Converts an RGB24 array to a bitmap.
		/// </summary>
		/// <param name="width">Width of the image</param>
		/// <param name="height">Height of the image</param>
		/// <param name="frame">RGB values for each pixel between 0 and 255</param>
		/// <returns>Bitmap</returns>
		public static unsafe BitmapSource ConvertFromRgb24(int width, int height, byte* frame)
		{
			return ConvertFromRgb24(width, height, FrameData(width, height).With(frame));
		}

		/// <summary>
		/// Converts an RGB24 array to a bitmap.
		/// </summary>
		/// <param name="width">Width of the image</param>
		/// <param name="height">Height of the image</param>
		/// <param name="frame">RGB values for each pixel between 0 and 255</param>
		/// <returns>Bitmap</returns>
		public static BitmapSource ConvertFromRgb24(int width, int height, byte[] frame)
		{
			return ConvertFromRgb24(width, height, FrameData(width, height).With(frame));
		}


		/// <summary>
		/// Converts between pixel formats.
		/// </summary>
		/// <param name="sourceFormat">Source format</param>
		/// <returns>Destination format</returns>
		private static System.Windows.Media.PixelFormat ConvertPixelFormat(PixelFormat sourceFormat)
		{
			switch (sourceFormat) {
				case PixelFormat.Format24bppRgb: return PixelFormats.Bgr24;
				case PixelFormat.Format32bppArgb: return PixelFormats.Bgra32;
				case PixelFormat.Format32bppRgb: return PixelFormats.Bgr32;
				case PixelFormat.Indexed: return PixelFormats.Indexed1;
				case PixelFormat.Format1bppIndexed: return PixelFormats.Indexed1;
				case PixelFormat.Format4bppIndexed: return PixelFormats.Indexed4;
				case PixelFormat.Format8bppIndexed: return PixelFormats.Indexed8;
				case PixelFormat.Format16bppGrayScale: return PixelFormats.Gray16;
				case PixelFormat.Format16bppRgb555: return PixelFormats.Bgr555;
				case PixelFormat.Format16bppRgb565: return PixelFormats.Bgr565;
				case PixelFormat.Format16bppArgb1555: return PixelFormats.Bgr101010;
				case PixelFormat.Format32bppPArgb: return PixelFormats.Pbgra32;
				case PixelFormat.Format48bppRgb: return PixelFormats.Rgb48;
				case PixelFormat.Format64bppArgb: return PixelFormats.Rgba64;
				case PixelFormat.Format64bppPArgb: return PixelFormats.Prgba64;
				case PixelFormat.Gdi:
				case PixelFormat.Alpha:
				case PixelFormat.PAlpha:
				case PixelFormat.Extended:
				case PixelFormat.Canonical:
				case PixelFormat.Undefined:
				case PixelFormat.Max:
				default:
					return new System.Windows.Media.PixelFormat();
			}
		}

		/// <summary>
		/// Sometimes we have a pointer, sometimes an array, but we don't want to implement
		/// everything twice, so this is a wrapper that supports both.
		/// </summary>
		private unsafe class Frame
		{
			public int Size => _isPointer ? -1 : _arraySrc.Length;

			private byte* _pointerSrc;
			private byte[] _arraySrc;
			private bool _isPointer;

			public Frame With(byte* src)
			{
				_pointerSrc = src;
				_isPointer = true;
				return this;
			}

			public Frame With(byte[] src)
			{
				_arraySrc = src;
				_isPointer = false;
				return this;
			}

			public byte Get(int pos)
			{
				return _isPointer ? _pointerSrc[pos] : _arraySrc[pos];
			}
		}
	}
}
