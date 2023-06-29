using System;

namespace Assistant.NINAPlugin.Astrometry.Solver {

    public interface IAltitudeRefiner {

        Altitudes Refine(Altitudes altitudes, int numPoints);

        Altitudes GetHourlyAltitudesForDay(DateTime date);

        bool RisesAtLocation();

        bool CircumpolarAtLocation();
    }

}
