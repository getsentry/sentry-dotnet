﻿using System;
using System.Threading.Tasks;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Maui.DeviceTests
{
	public static partial class AssertionExtensions
	{
		public static string CreateColorAtPointError(this UIImage bitmap, UIColor expectedColor, int x, int y) =>
			CreateColorError(bitmap, $"Expected {expectedColor} at point {x},{y} in renderered view.");

		public static string CreateColorError(this UIImage bitmap, string message) =>
			$"{message} This is what it looked like:<img>{bitmap.ToBase64String()}</img>";

		public static string CreateEqualError(this UIImage bitmap, UIImage other, string message) =>
			$"{message} This is what it looked like: <img>{bitmap.ToBase64String()}</img> and <img>{other.ToBase64String()}</img>";

		public static string ToBase64String(this UIImage bitmap)
		{
			var data = bitmap.AsPNG();
			return data.GetBase64EncodedString(NSDataBase64EncodingOptions.None);
		}

		public static Task AttachAndRun(this UIView view, Action action) =>
			view.AttachAndRun(() =>
			{
				action();
				return Task.FromResult(true);
			});

		public static Task<T> AttachAndRun<T>(this UIView view, Func<T> action) =>
			view.AttachAndRun(() =>
			{
				var result = action();
				return Task.FromResult(result);
			});

		public static Task AttachAndRun(this UIView view, Func<Task> action) =>
			view.AttachAndRun(async () =>
			{
				await action();
				return true;
			});

		public static async Task<T> AttachAndRun<T>(this UIView view, Func<Task<T>> action)
		{
			var currentView = FindContentView();
			currentView.AddSubview(view);

			// Give the UI time to refresh
			await Task.Delay(100);

			var result = await action();
			
			view.RemoveFromSuperview();

			// Give the UI time to refresh
			await Task.Delay(100);

			return result;
		}

		static UIView FindContentView() 
		{
			if (GetKeyWindow(UIApplication.SharedApplication) is not UIWindow window)
			{
				throw new InvalidOperationException("Could not attach view - unable to find UIWindow");
			}

			if (window.RootViewController is not UIViewController viewController)
			{
				throw new InvalidOperationException("Could not attach view - unable to find RootViewController");
			}

			while (viewController.PresentedViewController != null)
			{
				viewController = viewController.PresentedViewController;
			}

			if (viewController == null)
			{
				throw new InvalidOperationException("Could not attach view - unable to find presented ViewController");
			}

			if (viewController is UINavigationController nav)
			{
				viewController = nav.VisibleViewController;
			}

			var currentView = viewController.View;

			if (currentView == null)
			{
				throw new InvalidOperationException("Could not attach view - unable to find visible view");
			}

			var attachParent = currentView.FindDescendantView<ContentView>() as UIView;

			if (attachParent == null)
			{
				attachParent = currentView.FindDescendantView<UIView>();
			}

			return attachParent ?? currentView;
		}

		public static Task<UIImage> ToBitmap(this UIView view)
		{
			if (view.Superview is WrapperView wrapper)
				view = wrapper;

			var imageRect = new CGRect(0, 0, view.Frame.Width, view.Frame.Height);

			UIGraphics.BeginImageContext(imageRect.Size);

			var context = UIGraphics.GetCurrentContext();
			view.Layer.RenderInContext(context);
			var image = UIGraphics.GetImageFromCurrentImageContext();

			UIGraphics.EndImageContext();

			return Task.FromResult(image);
		}

		public static UIColor ColorAtPoint(this UIImage bitmap, int x, int y)
		{
			var pixel = bitmap.GetPixel(x, y);

			var color = new UIColor(
				pixel[0] / 255.0f,
				pixel[1] / 255.0f,
				pixel[2] / 255.0f,
				pixel[3] / 255.0f);

			return color;
		}

		public static byte[] GetPixel(this UIImage bitmap, int x, int y)
		{
			var cgImage = bitmap.CGImage!;
			var width = cgImage.Width;
			var height = cgImage.Height;
			var colorSpace = CGColorSpace.CreateDeviceRGB();
			var bitsPerComponent = 8;
			var bytesPerRow = 4 * width;
			var componentCount = 4;

			var dataBytes = new byte[width * height * componentCount];

			using var context = new CGBitmapContext(
				dataBytes,
				width, height,
				bitsPerComponent, bytesPerRow,
				colorSpace,
				CGBitmapFlags.ByteOrder32Big | CGBitmapFlags.PremultipliedLast);

			context.DrawImage(new CGRect(0, 0, width, height), cgImage);

			var pixelLocation = (bytesPerRow * y) + componentCount * x;

			var pixel = new byte[]
			{
				dataBytes[pixelLocation],
				dataBytes[pixelLocation + 1],
				dataBytes[pixelLocation + 2],
				dataBytes[pixelLocation + 3],
			};

			return pixel;
		}

		public static UIImage AssertColorAtPoint(this UIImage bitmap, UIColor expectedColor, int x, int y)
		{
			var cap = bitmap.ColorAtPoint(x, y);

			if (!ColorComparison.ARGBEquivalent(cap, expectedColor))
				Assert.Equal(cap, expectedColor, new ColorComparison());

			return bitmap;
		}

		public static UIImage AssertColorAtCenter(this UIImage bitmap, UIColor expectedColor)
		{
			AssertColorAtPoint(bitmap, expectedColor, (int)bitmap.Size.Width / 2, (int)bitmap.Size.Height / 2);
			return bitmap;
		}

		public static UIImage AssertColorAtBottomLeft(this UIImage bitmap, UIColor expectedColor)
		{
			return bitmap.AssertColorAtPoint(expectedColor, 0, 0);
		}

		public static UIImage AssertColorAtBottomRight(this UIImage bitmap, UIColor expectedColor)
		{
			return bitmap.AssertColorAtPoint(expectedColor, (int)bitmap.Size.Width - 1, 0);
		}

		public static UIImage AssertColorAtTopLeft(this UIImage bitmap, UIColor expectedColor)
		{
			return bitmap.AssertColorAtPoint(expectedColor, 0, (int)bitmap.Size.Height - 1);
		}

		public static UIImage AssertColorAtTopRight(this UIImage bitmap, UIColor expectedColor)
		{
			return bitmap.AssertColorAtPoint(expectedColor, (int)bitmap.Size.Width - 1, (int)bitmap.Size.Height - 1);
		}

		public static async Task<UIImage> AssertColorAtPoint(this UIView view, UIColor expectedColor, int x, int y)
		{
			var bitmap = await view.ToBitmap();
			return bitmap.AssertColorAtPoint(expectedColor, x, y);
		}

		public static async Task<UIImage> AssertColorAtCenter(this UIView view, UIColor expectedColor)
		{
			var bitmap = await view.ToBitmap();
			return bitmap.AssertColorAtCenter(expectedColor);
		}

		public static async Task<UIImage> AssertColorAtBottomLeft(this UIView view, UIColor expectedColor)
		{
			var bitmap = await view.ToBitmap();
			return bitmap.AssertColorAtBottomLeft(expectedColor);
		}

		public static async Task<UIImage> AssertColorAtBottomRight(this UIView view, UIColor expectedColor)
		{
			var bitmap = await view.ToBitmap();
			return bitmap.AssertColorAtBottomRight(expectedColor);
		}

		public static async Task<UIImage> AssertColorAtTopLeft(this UIView view, UIColor expectedColor)
		{
			var bitmap = await view.ToBitmap();
			return bitmap.AssertColorAtTopLeft(expectedColor);
		}

		public static async Task<UIImage> AssertColorAtTopRight(this UIView view, UIColor expectedColor)
		{
			var bitmap = await view.ToBitmap();
			return bitmap.AssertColorAtTopRight(expectedColor);
		}

		public static async Task<UIImage> AssertContainsColor(this UIView view, UIColor expectedColor)
		{
			var bitmap = await view.ToBitmap();
			return bitmap.AssertContainsColor(expectedColor);
		}

		public static Task<UIImage> AssertContainsColor(this UIView view, Microsoft.Maui.Graphics.Color expectedColor) =>
			AssertContainsColor(view, expectedColor.ToPlatform());

		public static UIImage AssertContainsColor(this UIImage bitmap, UIColor expectedColor)
		{
			for (int x = 0; x < bitmap.Size.Width; x++)
			{
				for (int y = 0; y < bitmap.Size.Height; y++)
				{
					if (ColorComparison.ARGBEquivalent(bitmap.ColorAtPoint(x, y), expectedColor))
					{
						return bitmap;
					}
				}
			}

			Assert.True(false, CreateColorError(bitmap, $"Color {expectedColor} not found."));
			return bitmap;
		}

		public static Task AssertEqual(this UIImage bitmap, UIImage other)
		{
			Assert.NotNull(bitmap);
			Assert.NotNull(other);

			Assert.Equal(bitmap.Size, other.Size);

			Assert.True(IsMatching(), CreateEqualError(bitmap, other, $"Images did not match."));

			return Task.CompletedTask;

			bool IsMatching()
			{
				for (int x = 0; x < bitmap.Size.Width; x++)
				{
					for (int y = 0; y < bitmap.Size.Height; y++)
					{
						var first = bitmap.ColorAtPoint(x, y);
						var second = other.ColorAtPoint(x, y);

						if (!ColorComparison.ARGBEquivalent(first, second))
							return false;
					}
				}
				return true;
			}
		}

		public static UILineBreakMode ToPlatform(this LineBreakMode mode) =>
			mode switch
			{
				LineBreakMode.NoWrap => UILineBreakMode.Clip,
				LineBreakMode.WordWrap => UILineBreakMode.WordWrap,
				LineBreakMode.CharacterWrap => UILineBreakMode.CharacterWrap,
				LineBreakMode.HeadTruncation => UILineBreakMode.HeadTruncation,
				LineBreakMode.TailTruncation => UILineBreakMode.TailTruncation,
				LineBreakMode.MiddleTruncation => UILineBreakMode.MiddleTruncation,
				_ => throw new ArgumentOutOfRangeException(nameof(mode))
			};

		public static double GetCharacterSpacing(this NSAttributedString text)
		{
			if (text == null)
				return 0;

			var value = text.GetAttribute(UIStringAttributeKey.KerningAdjustment, 0, out var range);
			if (value == null)
				return 0;

			Assert.Equal(0, range.Location);
			Assert.Equal(text.Length, range.Length);

			var kerning = Assert.IsType<NSNumber>(value);

			return kerning.DoubleValue;
		}

		public static void AssertHasUnderline(this NSAttributedString attributedString)
		{
			var value = attributedString.GetAttribute(UIStringAttributeKey.UnderlineStyle, 0, out var range);

			if (value == null)
			{
				throw new XunitException("Label does not have the UnderlineStyle attribute");
			}
		}

		public static UIColor GetForegroundColor(this NSAttributedString text)
		{
			if (text == null)
				return UIColor.Clear;

			var value = text.GetAttribute(UIStringAttributeKey.ForegroundColor, 0, out var range);

			if (value == null)
				return UIColor.Clear;

			Assert.Equal(0, range.Location);
			Assert.Equal(text.Length, range.Length);

			var kerning = Assert.IsType<UIColor>(value);

			return kerning;
		}

		public static void AssertEqual(this CATransform3D expected, CATransform3D actual, int precision = 4)
		{
			Assert.Equal((double)expected.M11, (double)actual.M11, precision);
			Assert.Equal((double)expected.M12, (double)actual.M12, precision);
			Assert.Equal((double)expected.M13, (double)actual.M13, precision);
			Assert.Equal((double)expected.M14, (double)actual.M14, precision);
			Assert.Equal((double)expected.M21, (double)actual.M21, precision);
			Assert.Equal((double)expected.M22, (double)actual.M22, precision);
			Assert.Equal((double)expected.M23, (double)actual.M23, precision);
			Assert.Equal((double)expected.M24, (double)actual.M24, precision);
			Assert.Equal((double)expected.M31, (double)actual.M31, precision);
			Assert.Equal((double)expected.M32, (double)actual.M32, precision);
			Assert.Equal((double)expected.M33, (double)actual.M33, precision);
			Assert.Equal((double)expected.M34, (double)actual.M34, precision);
			Assert.Equal((double)expected.M41, (double)actual.M41, precision);
			Assert.Equal((double)expected.M42, (double)actual.M42, precision);
			Assert.Equal((double)expected.M43, (double)actual.M43, precision);
			Assert.Equal((double)expected.M44, (double)actual.M44, precision);
		}

		static UIWindow? GetKeyWindow(UIApplication application)
		{
			if (OperatingSystem.IsIOSVersionAtLeast(15))
			{
				foreach (var scene in application.ConnectedScenes)
				{
					if (scene is UIWindowScene windowScene 
						&& windowScene.ActivationState == UISceneActivationState.ForegroundActive)
					{
						foreach (var window in windowScene.Windows)
						{
							if (window.IsKeyWindow)
							{
								return window;
							}
						}
					}
				}

				return null;
			}

			var windows = application.Windows;

			for (int i = 0; i < windows.Length; i++)
			{
				var window = windows[i];
				if (window.IsKeyWindow)
					return window;
			}

			return null;
		}
	}
}