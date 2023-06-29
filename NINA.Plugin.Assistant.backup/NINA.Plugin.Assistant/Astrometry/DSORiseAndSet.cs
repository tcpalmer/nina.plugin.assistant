using NINA.Astrometry;
using System;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Astrometry {

    /// <summary>
    /// This was largely copied from NINA NINA.Astrometry.RiseAndSet.RiseAndSetEvent and adapted to work for
    /// DSOs instead of solar system bodies.
    /// </summary>
    public class DSORiseAndSet {

        public DateTime Date { get; private set; }
        public ObserverInfo ObserverInfo { get; private set; }
        public Coordinates Coordinates { get; private set; }
        public virtual DateTime? Rise { get; private set; }
        public virtual DateTime? Set { get; private set; }

        public DSORiseAndSet(DateTime date, ObserverInfo observerInfo, Coordinates coordinates) {
            this.Date = date;
            this.ObserverInfo = observerInfo;
            this.Coordinates = coordinates;
        }

        public virtual Task<bool> Calculate() {
            return Task.Run(async () => {
                // Check rise and set events in two hour periods
                var offset = 0;

                do {
                    // Shift date by offset
                    var offsetDate = Date.AddHours(offset);

                    var location = new NOVAS.OnSurface() {
                        Latitude = ObserverInfo.Latitude,
                        Longitude = ObserverInfo.Longitude
                    };

                    // Determine altitude at the three times
                    var altitude0 = AstrometryUtils.GetAltitude(ObserverInfo, Coordinates, offsetDate);
                    var altitude1 = AstrometryUtils.GetAltitude(ObserverInfo, Coordinates, offsetDate.AddHours(1));
                    var altitude2 = AstrometryUtils.GetAltitude(ObserverInfo, Coordinates, offsetDate.AddHours(2));

                    var a = 0.5 * (altitude2 + altitude0) - altitude1;
                    var b = 2 * altitude1 - 0.5 * altitude2 - 1.5 * altitude0;
                    var c = altitude0;

                    // a-b-c formula
                    // x = -b +- Sqrt(b^2 - 4ac) / 2a
                    // Discriminant definition: b^2 - 4ac
                    var discriminant = (Math.Pow(b, 2)) - (4.0 * a * c);

                    var zeroPoint1 = double.NaN;
                    var zeroPoint2 = double.NaN;
                    var events = 0;

                    if (discriminant == 1) {
                        zeroPoint1 = (-b + Math.Sqrt(discriminant)) / (2 * a);
                        if (zeroPoint1 >= 0 && zeroPoint1 <= 2) {
                            events++;
                        }
                    }
                    else if (discriminant > 1) {
                        zeroPoint1 = (-b + Math.Sqrt(discriminant)) / (2 * a);
                        zeroPoint2 = (-b - Math.Sqrt(discriminant)) / (2 * a);

                        // Check if zero point is inside the span of 0 to 2 (to be inside the checked timeframe)
                        if (zeroPoint1 >= 0 && zeroPoint1 <= 2) {
                            events++;
                        }
                        if (zeroPoint2 >= 0 && zeroPoint2 <= 2) {
                            events++;
                        }
                        if (zeroPoint1 < 0 || zeroPoint1 > 2) {
                            zeroPoint1 = zeroPoint2;
                        }
                    }

                    //find the gradient at zeroPoint1. positive => rise event, negative => set event
                    var gradient = 2 * a * zeroPoint1 + b;

                    if (events == 1) {
                        if (gradient > 0) {
                            // rise
                            this.Rise = offsetDate.AddHours(zeroPoint1);
                        }
                        else {
                            // set
                            this.Set = offsetDate.AddHours(zeroPoint1);
                        }
                    }
                    else if (events == 2) {
                        if (gradient > 0) {
                            // rise and set
                            this.Rise = offsetDate.AddHours(zeroPoint1);
                            this.Set = offsetDate.AddHours(zeroPoint2);
                        }
                        else {
                            // set and rise
                            this.Rise = offsetDate.AddHours(zeroPoint2);
                            this.Set = offsetDate.AddHours(zeroPoint1);
                        }
                    }
                    offset += 2;

                    //Repeat until rise and set events are found, or after a whole day
                } while (!((this.Rise != null && this.Set != null) || offset > 24));

                return true;
            });
        }
    }

}
