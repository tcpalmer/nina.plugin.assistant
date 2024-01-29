namespace Assistant.NINAPlugin.Astrometry {

    public class HorizontalCoordinate {
        public double Altitude { get; private set; }
        public double Azimuth { get; private set; }

        public HorizontalCoordinate(double altitude, double azimuth) {
            this.Altitude = altitude;
            this.Azimuth = azimuth;
        }
    }
}