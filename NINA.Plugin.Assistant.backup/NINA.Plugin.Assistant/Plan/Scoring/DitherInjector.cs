using System.Collections.Generic;

namespace Assistant.NINAPlugin.Plan.Scoring {

    public class DitherInjector {

        private List<IPlanInstruction> instructions;
        private int ditherEvery;

        private List<string> uniqueFilters;

        public DitherInjector(List<IPlanInstruction> instructions, int ditherEvery) {
            this.instructions = instructions;
            this.ditherEvery = ditherEvery;
        }

        public List<IPlanInstruction> Inject() {

            if (ditherEvery == 0) {
                return instructions;
            }

            if (instructions is null || instructions.Count == 0) {
                return instructions;
            }

            uniqueFilters = GetUniqueFilters();
            List<IPlanInstruction> dithered = new List<IPlanInstruction>();

            int pos = 0;
            while (pos < instructions.Count) {
                int ditherPos = FindNextDither(pos);
                if (ditherPos < 0) {
                    for (int i = pos; i < instructions.Count; i++) {
                        dithered.Add(instructions[i]);
                    }

                    break;
                }

                for (int i = pos; i < ditherPos; i++) {
                    dithered.Add(instructions[i]);
                }

                dithered.Add(new PlanDither());
                pos = ditherPos;
            }

            return dithered;
        }

        private int FindNextDither(int start) {
            Dictionary<string, int> filterCounts = GetFilterDictionary();

            // Walk the list, incrementing when each filter occurs.  Injection point is when a filter is seen ditherEvery+1 times.
            int pos = -1;
            for (int i = start; i < instructions.Count; i++) {
                if (instructions[i] is PlanTakeExposure) {
                    string filterName = ((PlanTakeExposure)instructions[i]).planExposure.FilterName;
                    filterCounts[filterName]++;
                    if (filterCounts[filterName] == ditherEvery + 1) {
                        pos = i;
                        break;
                    }
                }
            }
            // TODO: what if you never hit D+1?  Then done?

            return pos;
        }

        private List<string> GetUniqueFilters() {
            List<string> filters = new List<string>();
            foreach (IPlanInstruction instruction in instructions) {
                if (instruction is PlanTakeExposure) {
                    string filterName = ((PlanTakeExposure)instruction).planExposure.FilterName;
                    if (!filters.Contains(filterName)) {
                        filters.Add(filterName);
                    }
                }
            }

            return filters;
        }

        private Dictionary<string, int> GetFilterDictionary() {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            foreach (string filter in uniqueFilters) {
                dict.Add(filter, 0);
            }

            return dict;
        }

    }

}
