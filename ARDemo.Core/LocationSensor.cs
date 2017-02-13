using System;
using CoreLocation;
using Foundation;

namespace ARDemo
{
	public class LocationSensor
	{
		public event EventHandler LocationReceived = delegate {};

		public DateTime Timestamp { get; private set; }
		public Location Location { get; private set; }
		public double Heading { get; private set; }
		public double HorizontalAccuracy { get; private set; }
		public double VerticalAccuracy { get; private set; }

		CLLocationManager lman;

        public static DateTime NSDateToDateTime(NSDate date)
        {
            DateTime reference = TimeZone.CurrentTimeZone.ToLocalTime(
                new DateTime(2001, 1, 1, 0, 0, 0));
            return reference.AddSeconds(date.SecondsSinceReferenceDate);
        }

        public void Start ()
		{
			if (CLLocationManager.LocationServicesEnabled) {
				lman = new CLLocationManager {
					DesiredAccuracy = CLLocation.AccuracyBest,
				};

				lman.RequestWhenInUseAuthorization ();

				lman.LocationsUpdated += (sender, e) => {
					var loc = e.Locations [0];
					Timestamp = NSDateToDateTime(loc.Timestamp);
					Location = new Location (loc.Coordinate.Latitude, loc.Coordinate.Longitude, loc.Altitude);
//					Console.WriteLine (Location);
					HorizontalAccuracy = loc.HorizontalAccuracy;
					VerticalAccuracy = loc.VerticalAccuracy;
					LocationReceived (this, EventArgs.Empty);
				};

				lman.UpdatedHeading += (sender, e) => {
					Heading = e.NewHeading.TrueHeading;
//					Console.WriteLine ("Heading: {0}", Heading);
				};

				lman.StartUpdatingLocation ();
				lman.StartUpdatingHeading ();
			}
		}
	}
}

