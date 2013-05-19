// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;

namespace StylePlay
{
	[Register ("StylePlayViewController")]
	partial class StylePlayViewController
	{
		[Outlet]
		MonoTouch.UIKit.UILabel SpecialLabel { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIButton SubButton { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (SpecialLabel != null) {
				SpecialLabel.Dispose ();
				SpecialLabel = null;
			}

			if (SubButton != null) {
				SubButton.Dispose ();
				SubButton = null;
			}
		}
	}
}
