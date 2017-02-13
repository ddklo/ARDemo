using System;
using UIKit;
using MapKit;
using CoreGraphics;
using CoreLocation;

namespace ARDemo
{
	public class MapOverlay : UIView
	{
		readonly MKMapView map;

		public MapOverlay () : base (new CGRect (0, 0, 144, 144))
		{
			map = new MKMapView (Bounds) {
				AutoresizingMask = UIViewAutoresizing.FlexibleDimensions,
				ShowsUserLocation = true,
				MapType = MKMapType.Satellite,
			};
			map.SetRegion (MKCoordinateRegion.FromDistance (new CLLocationCoordinate2D (47,-122), 1, 1), false);
			map.UserTrackingMode = MKUserTrackingMode.FollowWithHeading;
			Alpha = 0.5f;
			AddSubview (map);
		}
	}
}

