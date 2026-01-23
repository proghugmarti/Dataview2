namespace DataView2.GrpcService.Services.OtherServices
{
    public static class GeometryUtils
    {
        // Moving average filter
        private static double[] MovingAverageFilter(double[] input, int windowSize)
        {
            double[] output = new double[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                double sum = 0;
                int count = 0;

                for (int j = Math.Max(0, i - windowSize + 1); j <= i; j++)
                {
                    sum += input[j];
                    count++;
                }

                output[i] = sum / count;
            }

            return output;
        }

        public static double[] CalculateVerticalCurvature(double[] chainage, double[] pitch, int processingInterval)
        {
            int n = pitch.Length;
            double[] pitchFiltered = MovingAverageFilter(pitch, 10);
            double[] curvature = new double[n];
            curvature[0] = 0;

            for (int i = 1; i < n; i++)
            {
                double l = DetermineL(-pitchFiltered[i - 1], chainage[i] - chainage[i - 1]);
                double a = DetermineA(-pitchFiltered[i - 1], -pitchFiltered[i], l);
                double b = DetermineB(-pitchFiltered[i - 1]);
                double curvatureValue = CalculateCurvatureOfParabolaAtX0(a, b);
                curvature[i] = curvatureValue;
            }

            return CalculateAverageCurvature(curvature, processingInterval);
        }

        private static double[] CalculateAverageCurvature(double[] curvature, int processingInterval)
        {
            int n = curvature.Length;
            int resultSize = (int)Math.Ceiling((double)n / processingInterval);
            double[] curvatureInterval = new double[resultSize];

            double sum = 0;
            int resultIndex = 0;

            for (int i = 0; i < processingInterval && i < n; ++i)
            {
                sum += curvature[i];
            }

            for (int i = processingInterval; i <= n; i += processingInterval)
            {
                curvatureInterval[resultIndex] = sum / processingInterval;
                resultIndex++;

                sum = 0;
                for (int j = i; j < i + processingInterval && j < n; ++j)
                {
                    sum += curvature[j];
                }
            }

            return curvatureInterval;
        }

        // === Helper functions ported from C++ ===
        private static double CalculateCurvatureOfParabolaAtX0(double a, double b)
        {
            return (2 * a) / Math.Pow(1 + Math.Pow(b, 2), 1.5);
        }

        private static double DetermineL(double gradient1, double lengthOfCurve)
        {
            double gradientInRad = gradient1 * Math.PI / 180.0;
            return lengthOfCurve * Math.Cos(gradientInRad);
        }

        private static double DetermineA(double gradient1, double gradient2, double horizontalLength)
        {
            return (gradient2 - gradient1) / (2 * horizontalLength);
        }

        private static double DetermineB(double gradient1)
        {
            return gradient1;
        }
    }
}
