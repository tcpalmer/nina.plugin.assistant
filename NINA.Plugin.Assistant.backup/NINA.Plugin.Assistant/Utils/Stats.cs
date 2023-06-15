using System;
using System.Collections.Generic;
using System.Linq;

namespace Assistant.NINAPlugin.Util {

    public static class Stats {

        /// <summary>
        /// Determine the mean and the sample (not population) standard deviation of a set of samples.
        /// </summary>
        /// <param name="samples"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static (double, double) SampleStandardDeviation(List<double> samples) {
            if (samples == null || samples.Count < 3) {
                throw new Exception("must have >= 3 samples");
            }

            double mean = samples.Average();
            double sum = samples.Sum(d => Math.Pow(d - mean, 2));
            double stddev = Math.Sqrt((sum) / (samples.Count() - 1));

            return (mean, stddev);
        }
    }

}
