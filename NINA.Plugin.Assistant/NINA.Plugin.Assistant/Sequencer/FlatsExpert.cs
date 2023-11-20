using NINA.Core.Model.Equipment;

namespace Assistant.NINAPlugin.Sequencer {

    public class FlatsExpert {
    }

    public class FlatSpec {

        public string FilterName { get; private set; }
        public double Gain { get; private set; }
        public double Offset { get; private set; }
        public BinningMode BinningMode { get; private set; }
        public int ReadoutMode { get; private set; }
        public double Rotation { get; private set; }
        public double ROI { get; private set; }

        public FlatSpec(string filterName, double gain, double offset, BinningMode binning, int readoutMode, double rotation, double roi) {
            FilterName = filterName;
            Gain = gain;
            Offset = offset;
            BinningMode = binning;
            ReadoutMode = readoutMode;
            Rotation = rotation;
            ROI = roi;
        }

        public string Key => $"{FilterName}_{Gain}_{Offset}_{BinningMode}_{ReadoutMode}_{Rotation}_{ROI}";

    }
}
