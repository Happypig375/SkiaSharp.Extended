﻿using System;
using SkiaSharp;
using SkiaSharp.Extended.Controls;
using Xamarin.Forms;

namespace SkiaSharpDemo
{
	public partial class GestureSurfacePage : ContentPage
	{
		private bool useHardware;

		private SKPaint paint = new SKPaint
		{
			TextSize = 40,
			IsAntialias = true
		};

		private SKMatrix totalMatrix = SKMatrix.MakeIdentity();
		private float totalScale = 1f;
		private float totalRotation = 0f;
		private SKPoint totalTranslate = SKPoint.Empty;

		private const float MaxScale = 3f;
		private const float MinScale = 0.5f;
		private const float MaxRotation = 30f;
		private const float MinRotation = -60f;

		public GestureSurfacePage()
		{
			InitializeComponent();

			BindingContext = this;

			gestureSurface.FlingDetected += (sender, e) =>
			{
				var easing = Easing.SinOut;

				var ratio = e.VelocityX / e.VelocityY;

				gestureSurface.AbortAnimation("Fling");
				var animation = new Animation(v => Transform(new SKPoint((float)(v * ratio), (float)v), SKPoint.Empty, 1, 0), e.VelocityY * 0.01f, 0, easing);
				animation.Commit(gestureSurface, "Fling", 16, 1000);
			};

			gestureSurface.TransformDetected += (sender, e) =>
			{
				gestureSurface.AbortAnimation("Fling");

				Transform(e.Center, e.PreviousCenter, e.ScaleDelta, e.RotationDelta);
			};

			gestureSurface.DoubleTapDetected += (sender, e) =>
			{
				gestureSurface.AbortAnimation("Fling");

				Transform(e.Location, e.Location, 1.5f, 0);
			};
		}

		private void Transform(SKPoint positionScreen, SKPoint previousPositionScreen, float scaleDelta, float rotationDelta)
		{
			var positionDelta = positionScreen - previousPositionScreen;
			if (!positionDelta.IsEmpty)
			{
				totalTranslate += positionDelta;
				var m = SKMatrix.MakeTranslation(positionDelta.X, positionDelta.Y);
				SKMatrix.Concat(ref totalMatrix, ref m, ref totalMatrix);
			}

			if (scaleDelta != 1)
			{
				if (totalScale * scaleDelta > MaxScale)
					scaleDelta = MaxScale / totalScale;
				if (totalScale * scaleDelta < MinScale)
					scaleDelta = MinScale / totalScale;

				totalScale *= scaleDelta;
				var m = SKMatrix.MakeScale(scaleDelta, scaleDelta, positionScreen.X, positionScreen.Y);
				SKMatrix.Concat(ref totalMatrix, ref m, ref totalMatrix);
			}

			if (rotationDelta != 0)
			{
				if (totalRotation + rotationDelta > MaxRotation)
					rotationDelta = MaxRotation - totalRotation;
				if (totalRotation + rotationDelta < MinRotation)
					rotationDelta = MinRotation - totalRotation;

				totalRotation += rotationDelta;
				var m = SKMatrix.MakeRotationDegrees(rotationDelta, positionScreen.X, positionScreen.Y);
				SKMatrix.Concat(ref totalMatrix, ref m, ref totalMatrix);
			}

			gestureSurface.InvalidateSurface();
		}

		public bool UseHardware
		{
			get => useHardware;
			set
			{
				useHardware = value;
				OnPropertyChanged();
			}
		}

		private void OnPainting(object sender, SKPaintDynamicSurfaceEventArgs e)
		{
			var canvas = e.Surface.Canvas;
			var width = e.Info.Width;
			var height = e.Info.Height;

			canvas.Clear(SKColors.CornflowerBlue);

			paint.Color = SKColors.Black;
			for (int x = -20; x <= 20; x++)
			{
				for (int y = -20; y <= 20; y++)
				{
					canvas.DrawText($"{x}x{y}", x * 100, y * 100, paint);
				}
			}

			canvas.SetMatrix(totalMatrix);

			paint.Color = SKColors.Red;
			canvas.DrawRect(300, 300, 200, 200, paint);
			canvas.DrawRect(700, 200, 200, 200, paint);
			canvas.DrawRect(200, 600, 200, 200, paint);

			canvas.ResetMatrix();

			var r = (float)height / (float)width;
			var w = 250.0f;
			var h = w * r;

			var previewMatrix = SKMatrix.MakeIdentity();
			var s = SKMatrix.MakeScale(w / width, w / width);
			SKMatrix.Concat(ref previewMatrix, ref s, ref previewMatrix);
			var t = SKMatrix.MakeTranslation(width - w, height - h);
			SKMatrix.Concat(ref previewMatrix, ref t, ref previewMatrix);

			canvas.SetMatrix(previewMatrix);

			var previewRect = SKRect.Create(width, height);

			canvas.ClipRect(previewRect);

			canvas.Save();

			paint.Color = SKColors.White.WithAlpha(128);
			canvas.DrawRect(previewRect, paint);

			if (totalMatrix.TryInvert(out var inv))
			{
				SKMatrix.Concat(ref inv, ref previewMatrix, ref inv);
				canvas.SetMatrix(inv);
			}

			paint.Color = SKColors.Black.WithAlpha(128);
			var viewportRect = SKRect.Create(width, height);
			canvas.DrawRect(viewportRect, paint);
		}
	}
}
