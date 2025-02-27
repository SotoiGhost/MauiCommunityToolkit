﻿using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Platform;
using static Android.Views.View;
using AColorRes = Android.Resource.Color;
using APoint = Android.Graphics.Point;
using ARect = Android.Graphics.Rect;
using AView = Android.Views.View;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Extension class where Helper methods for Popup lives.
/// </summary>
public static class PopupExtensions
{
	static APoint realSize = new();
	static APoint displaySize = new();
	static ARect contentRect = new();

	/// <summary>
	/// Method to update the <see cref="IPopup.Anchor"/> view.
	/// </summary>
	/// <param name="dialog">An instance of <see cref="Dialog"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	/// <exception cref="InvalidOperationException">if the <see cref="Android.Views.Window"/> is null an exception will be thrown.</exception>
	public static void SetAnchor(this Dialog dialog, in IPopup popup)
	{
		var window = GetWindow(dialog);

		if (popup.Handler?.MauiContext is null)
		{
			return;
		}

		if (popup.Anchor is not null)
		{
			var anchorView = popup.Anchor.ToPlatform();

			var locationOnScreen = new int[2];
			anchorView.GetLocationOnScreen(locationOnScreen);
			window.SetGravity(GravityFlags.Top | GravityFlags.Left);
			window.DecorView.Measure((int)MeasureSpecMode.Unspecified, (int)MeasureSpecMode.Unspecified);

			// This logic is tricky, please read these notes if you need to modify
			// Android window coordinate starts (0,0) at the top left and (max,max) at the bottom right. All of the positions
			// that are being handled in this operation assume the point is at the top left of the rectangle. This means the
			// calculation operates in this order:
			// 1. Calculate top-left position of Anchor
			// 2. Calculate the Actual Center of the Anchor by adding the width /2 and height / 2
			// 3. Calculate the top-left point of where the dialog should be positioned by subtracting the Width / 2 and height / 2
			//    of the dialog that is about to be drawn.
			_ = window.Attributes ?? throw new InvalidOperationException($"{nameof(window.Attributes)} cannot be null");

			window.Attributes.X = locationOnScreen[0] + (anchorView.Width / 2) - (window.DecorView.MeasuredWidth / 2);
			window.Attributes.Y = locationOnScreen[1] + (anchorView.Height / 2) - (window.DecorView.MeasuredHeight / 2);
		}
		else
		{
			SetDialogPosition(popup, window);
		}
	}

	/// <summary>
	/// Method to update the <see cref="IPopup.Color"/> property.
	/// </summary>
	/// <param name="dialog">An instance of <see cref="Dialog"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	public static void SetColor(this Dialog dialog, in IPopup popup)
	{
		if (popup.Color is null)
		{
			return;
		}

		var window = GetWindow(dialog);
		window.SetBackgroundDrawable(new ColorDrawable(popup.Color.ToPlatform(AColorRes.BackgroundLight, dialog.Context)));
	}

	/// <summary>
	/// Method to update the <see cref="IPopup.CanBeDismissedByTappingOutsideOfPopup"/> property.
	/// </summary>
	/// <param name="dialog">An instance of <see cref="Dialog"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	public static void SetCanBeDismissedByTappingOutsideOfPopup(this Dialog dialog, in IPopup popup)
	{
		dialog.SetCancelable(popup.CanBeDismissedByTappingOutsideOfPopup);
		dialog.SetCanceledOnTouchOutside(popup.CanBeDismissedByTappingOutsideOfPopup);
	}

	/// <summary>
	/// Method to update the <see cref="IPopup.Size"/> property.
	/// </summary>
	/// <param name="dialog">An instance of <see cref="Dialog"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	/// <param name="container">The native representation of <see cref="IPopup.Content"/>.</param>
	/// <exception cref="InvalidOperationException">if the <see cref="Android.Views.Window"/> is null an exception will be thrown. If the <paramref name="container"/> is null an exception will be thrown.</exception>
	public static void SetSize(this Dialog dialog, in IPopup popup, in AView container)
	{
		ArgumentNullException.ThrowIfNull(dialog);
		ArgumentNullException.ThrowIfNull(container);
		ArgumentNullException.ThrowIfNull(popup?.Content);

		int horizontalParams, verticalParams;

		var window = GetWindow(dialog);
		var context = dialog.Context;
		var windowManager = window.WindowManager;

		var decorView = (ViewGroup)window.DecorView;
		var child = decorView.GetChildAt(0) ?? throw new InvalidOperationException($"No child found in {nameof(ViewGroup)}");

		int realWidth = 0,
			realHeight = 0,
			realContentWidth = 0,
			realContentHeight = 0;

		var windowSize = GetWindowSize(windowManager, decorView);

		CalculateSizes(popup, context, windowSize, ref realWidth, ref realHeight, ref realContentWidth, ref realContentHeight);
		window.SetLayout(realWidth, realHeight);

		var childLayoutParams = (FrameLayout.LayoutParams)(child.LayoutParameters ?? throw new InvalidOperationException($"{nameof(child.LayoutParameters)} cannot be null"));
		childLayoutParams.Width = realWidth;
		childLayoutParams.Height = realHeight;
		child.LayoutParameters = childLayoutParams;

		horizontalParams = realWidth;
		verticalParams = realHeight;

		var containerLayoutParams = new FrameLayout.LayoutParams(horizontalParams, verticalParams);

		switch (popup.Content.VerticalLayoutAlignment)
		{
			case LayoutAlignment.Start:
				containerLayoutParams.Gravity = GravityFlags.Top;
				break;
			case LayoutAlignment.Center:
			case LayoutAlignment.Fill:
				containerLayoutParams.Gravity = GravityFlags.FillVertical;
				containerLayoutParams.Height = realHeight;
				break;
			case LayoutAlignment.End:
				containerLayoutParams.Gravity = GravityFlags.Bottom;
				break;
			default:
				throw new NotSupportedException($"{nameof(popup.Content.HorizontalLayoutAlignment)} {popup.Content.VerticalLayoutAlignment} is not supported");
		}

		switch (popup.Content.HorizontalLayoutAlignment)
		{
			case LayoutAlignment.Start:
				containerLayoutParams.Gravity |= GravityFlags.Left;
				break;
			case LayoutAlignment.Center:
			case LayoutAlignment.Fill:
				containerLayoutParams.Gravity |= GravityFlags.FillHorizontal;
				containerLayoutParams.Width = realWidth;
				break;
			case LayoutAlignment.End:
				containerLayoutParams.Gravity |= GravityFlags.Right;
				break;
			default:
				throw new NotSupportedException($"{nameof(popup.Content.HorizontalLayoutAlignment)} {popup.Content.HorizontalLayoutAlignment} is not supported");
		}

		container.LayoutParameters = containerLayoutParams;


		static void CalculateSizes(IPopup popup, Context context, Size windowSize, ref int realWidth, ref int realHeight, ref int realContentWidth, ref int realContentHeight)
		{
			ArgumentNullException.ThrowIfNull(popup.Content);

			var density = context.Resources?.DisplayMetrics?.Density ?? throw new InvalidOperationException($"Unable to determine density. {nameof(context.Resources.DisplayMetrics)} cannot be null");
			var view = popup.Content.ToPlatform();

			if (popup.Size.IsZero)
			{
				if (double.IsNaN(popup.Content.Width) || double.IsNaN(popup.Content.Height))
				{
					if (view is not null)
					{
						bool isRootView = true;
						Measure(
							view,
							(int)(double.IsNaN(popup.Content.Width) ? windowSize.Width : (int)context.ToPixels(popup.Content.Width)),
							(int)(double.IsNaN(popup.Content.Height) ? windowSize.Height : (int)context.ToPixels(popup.Content.Height)),
							double.IsNaN(popup.Content.Width) && popup.HorizontalOptions != LayoutAlignment.Fill,
							double.IsNaN(popup.Content.Height) && popup.VerticalOptions != LayoutAlignment.Fill,
							ref isRootView
						);
						realContentWidth = view.MeasuredWidth;
						realContentHeight = view.MeasuredHeight;
					}

					if (double.IsNaN(popup.Content.Width))
					{
						realContentWidth = popup.HorizontalOptions == LayoutAlignment.Fill ? (int)windowSize.Width : realContentWidth;
					}
					if (double.IsNaN(popup.Content.Height))
					{
						realContentHeight = popup.VerticalOptions == LayoutAlignment.Fill ? (int)windowSize.Height : realContentHeight;
					}
				}
				else
				{
					realContentWidth = (int)context.ToPixels(popup.Content.Width);
					realContentHeight = (int)context.ToPixels(popup.Content.Height);
				}
			}
			else
			{
				if (view is not null)
				{
					bool isRootView = true;
					Measure(
						view,
						(int)context.ToPixels(popup.Size.Width),
						(int)context.ToPixels(popup.Size.Height),
						false,
						false,
						ref isRootView
					);
					realContentWidth = view.MeasuredWidth;
					realContentHeight = view.MeasuredHeight;
				}
			}

			realWidth = Math.Min(realWidth is 0 ? realContentWidth : realWidth, (int)windowSize.Width);
			realHeight = Math.Min(realHeight is 0 ? realContentHeight : realHeight, (int)windowSize.Height);

			if (realHeight is 0 || realWidth is 0)
			{
				realWidth = (int)(context.Resources?.DisplayMetrics?.WidthPixels * 0.8 ?? throw new InvalidOperationException($"Unable to determine width. {nameof(context.Resources.DisplayMetrics)} cannot be null"));
				realHeight = (int)(context.Resources?.DisplayMetrics?.HeightPixels * 0.6 ?? throw new InvalidOperationException($"Unable to determine height. {nameof(context.Resources.DisplayMetrics)} cannot be null"));
			}
		}

		static void Measure(AView view, int width, int height, bool isNanWidth, bool isNanHeight, ref bool isRootView)
		{
			if (isRootView)
			{
				isRootView = false;
				view.Measure(
					MeasureSpec.MakeMeasureSpec(width, !isNanWidth ? MeasureSpecMode.Exactly : MeasureSpecMode.AtMost),
					MeasureSpec.MakeMeasureSpec(height, !isNanHeight ? MeasureSpecMode.Exactly : MeasureSpecMode.AtMost)
				);
			}

			if (view is AppCompatTextView)
			{
				// https://github.com/dotnet/maui/issues/2019
				// https://github.com/dotnet/maui/pull/2059
				var layoutParams = view.LayoutParameters;
				view.Measure(
					MeasureSpec.MakeMeasureSpec(width, (layoutParams?.Width == LinearLayout.LayoutParams.WrapContent && !isNanWidth) ? MeasureSpecMode.Exactly : MeasureSpecMode.Unspecified),
					MeasureSpec.MakeMeasureSpec(height, (layoutParams?.Height == LinearLayout.LayoutParams.WrapContent && !isNanHeight) ? MeasureSpecMode.Exactly : MeasureSpecMode.Unspecified)
				);
			}

			if (view is ViewGroup viewGroup)
			{
				for (int i = 0; i < viewGroup.ChildCount; i++)
				{
					if (viewGroup.GetChildAt(i) is AView childView)
					{
						Measure(childView, width, height, isNanWidth, isNanHeight, ref isRootView);
					}
				}
			}
		}

		static Size GetWindowSize(IWindowManager? windowManager, ViewGroup decorView)
		{
			ArgumentNullException.ThrowIfNull(windowManager);

			int windowWidth;
			int windowHeight;
			int statusBarHeight;
			int navigationBarHeight;

			if (OperatingSystem.IsAndroidVersionAtLeast((int)BuildVersionCodes.R))
			{
				var windowMetrics = windowManager.CurrentWindowMetrics;
				var windowInsets = windowMetrics.WindowInsets.GetInsetsIgnoringVisibility(WindowInsets.Type.SystemBars());
				windowWidth = windowMetrics.Bounds.Width();
				windowHeight = windowMetrics.Bounds.Height();
				statusBarHeight = windowInsets.Top;
				navigationBarHeight = windowHeight < windowWidth ? windowInsets.Left + windowInsets.Right : windowInsets.Bottom;
			}
			else if (windowManager.DefaultDisplay is null)
			{
				throw new InvalidOperationException($"{nameof(IWindowManager)}.{nameof(IWindowManager.DefaultDisplay)} cannot be null");
			}
			else
			{
				windowManager.DefaultDisplay.GetRealSize(realSize);
				windowManager.DefaultDisplay.GetSize(displaySize);
				decorView.GetWindowVisibleDisplayFrame(contentRect);

				windowWidth = realSize.X;
				windowHeight = realSize.Y;
				statusBarHeight = contentRect.Top;

				navigationBarHeight = realSize.Y < realSize.X
										? (realSize.X - displaySize.X)
										: (realSize.Y - displaySize.Y);
			}

			windowWidth = (windowWidth - (windowHeight < windowWidth ? navigationBarHeight : 0));
			windowHeight = (windowHeight - ((windowHeight < windowWidth ? 0 : navigationBarHeight) + statusBarHeight));

			return new Size(windowWidth, windowHeight);
		}
	}

	static void SetDialogPosition(in IPopup popup, Android.Views.Window window)
	{
		var isFlowDirectionRightToLeft = popup.Content?.FlowDirection == FlowDirection.RightToLeft;

		var gravityFlags = popup.VerticalOptions switch
		{
			LayoutAlignment.Start => GravityFlags.Top,
			LayoutAlignment.End => GravityFlags.Bottom,
			_ => GravityFlags.CenterVertical,
		};

		gravityFlags |= popup.HorizontalOptions switch
		{
			LayoutAlignment.Start => isFlowDirectionRightToLeft ? GravityFlags.Right : GravityFlags.Left,
			LayoutAlignment.End => isFlowDirectionRightToLeft ? GravityFlags.Left : GravityFlags.Right,
			_ => GravityFlags.CenterHorizontal,
		};

		window.SetGravity(gravityFlags);
	}

	static Android.Views.Window GetWindow(in Dialog dialog) =>
		dialog.Window ?? throw new InvalidOperationException($"{nameof(Dialog)}.{nameof(Dialog.Window)} cannot be null");
}