namespace AudioView.Library.SoundMeter
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Utilities will perform some useful functions on the data
    /// </summary>
    public class Utilities
    {
        /// <summary>
        /// Parses a line and extracts Octive Band data 
        /// </summary>
        /// <param name="r">Result where octive band data needs to be added to</param>
        /// <param name="line">The line of Octive bands</param>
        public static void ParseOctive(Dictionary<string, double> p_Result, String line)
        {
            // if we have a line break we need to perform a few tricks
            // if the line has a line break
            if (line.Contains("\r\n"))
            {
                // find the index
                int index = line.IndexOf("\r\n");

                // replace the new line with an empty string
                line = line.Replace(Environment.NewLine, String.Empty);

                // take the new line based upon what the index of the line breaks
                // was this should just return string we can parse
                line = line.Substring(index);
            }

            // we need to split the octives by a commer
            String[] octives = line.Split(new char[] { ',' });

            // assign the values
            p_Result.Add("ThirdOctave_6_3_Hz",ParseMeasurement(octives[0]));
            p_Result.Add("ThirdOctave_8_Hz",ParseMeasurement(octives[1]));
            p_Result.Add("ThirdOctave_10_Hz",ParseMeasurement(octives[2]));
            p_Result.Add("ThirdOctave_12_5_Hz",ParseMeasurement(octives[3]));
            p_Result.Add("ThirdOctave_16_Hz",ParseMeasurement(octives[4]));
            p_Result.Add("ThirdOctave_20_Hz",ParseMeasurement(octives[5]));
            p_Result.Add("ThirdOctave_25_Hz",ParseMeasurement(octives[6]));
            p_Result.Add("ThirdOctave_31_5_Hz",ParseMeasurement(octives[7]));
            p_Result.Add("ThirdOctave_40_Hz",ParseMeasurement(octives[8]));
            p_Result.Add("ThirdOctave_50_Hz",ParseMeasurement(octives[9]));
            p_Result.Add("ThirdOctave_63_Hz",ParseMeasurement(octives[10]));
            p_Result.Add("ThirdOctave_80_Hz",ParseMeasurement(octives[11]));
            p_Result.Add("ThirdOctave_100_Hz",ParseMeasurement(octives[12]));
            p_Result.Add("ThirdOctave_125_Hz",ParseMeasurement(octives[13]));
            p_Result.Add("ThirdOctave_160_Hz",ParseMeasurement(octives[14]));
            p_Result.Add("ThirdOctave_200_Hz",ParseMeasurement(octives[15]));
            p_Result.Add("ThirdOctave_250_Hz",ParseMeasurement(octives[16]));
            p_Result.Add("ThirdOctave_315_Hz",ParseMeasurement(octives[17]));
            p_Result.Add("ThirdOctave_400_Hz",ParseMeasurement(octives[18]));
            p_Result.Add("ThirdOctave_500_Hz",ParseMeasurement(octives[19]));
            p_Result.Add("ThirdOctave_630_Hz",ParseMeasurement(octives[20]));
            p_Result.Add("ThirdOctave_800_Hz",ParseMeasurement(octives[21]));
            p_Result.Add("ThirdOctave_1000_Hz",ParseMeasurement(octives[22]));
            p_Result.Add("ThirdOctave_1250_Hz",ParseMeasurement(octives[23]));
            p_Result.Add("ThirdOctave_1600_Hz",ParseMeasurement(octives[24]));
            p_Result.Add("ThirdOctave_2000_Hz",ParseMeasurement(octives[25]));
            p_Result.Add("ThirdOctave_2500_Hz",ParseMeasurement(octives[26]));
            p_Result.Add("ThirdOctave_3150_Hz",ParseMeasurement(octives[27]));
            p_Result.Add("ThirdOctave_4000_Hz",ParseMeasurement(octives[28]));
            p_Result.Add("ThirdOctave_5000_Hz",ParseMeasurement(octives[29]));
            p_Result.Add("ThirdOctave_6300_Hz",ParseMeasurement(octives[30]));
            p_Result.Add("ThirdOctave_8000_Hz",ParseMeasurement(octives[31]));
            p_Result.Add("ThirdOctave_10000_Hz",ParseMeasurement(octives[32]));
            p_Result.Add("ThirdOctave_12500_Hz",ParseMeasurement(octives[33]));
            p_Result.Add("ThirdOctave_16000_Hz",ParseMeasurement(octives[34]));
            p_Result.Add("ThirdOctave_20000_Hz",ParseMeasurement(octives[35]));
        }

        /// <summary>
        /// Parse the Calibration Result
        /// </summary>
        /// <param name="line">Line to parse</param>
        /// <returns>Double value reporesentation</returns>
        public static Double ParseCalibration(String line)
        {
            return Parse("e-3 V", line);
        }

        /// <summary>
        /// Parses a measurement
        /// </summary>
        /// <param name="line">Measurement taken from the sound level meter</param>
        /// <returns>A Double representation</returns>
        public static Double ParseMeasurement(String line)
        {
            return Parse("dB", line);
        }

        /// <summary>
        /// Code that parses the lines
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private static Double Parse(String pattern, String line)
        {
            try
            {
                // assign an initial result to zero
                Double result = 0;

                // if the line contains a a line break somewhere
                if (line.Contains("\r\n"))
                {
                    // find the index
                    int index = line.IndexOf("\r\n");

                    // replace the new line with an empty string
                    line = line.Replace(Environment.NewLine, String.Empty);

                    // remove dB from that line
                    string removeDb = line.Substring(index, line.Length - index);

                    // try to parse that line if it is sucessfull return the result
                    if (Double.TryParse(removeDb, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
                    {
                        // if this is the first few seconds
                        if (result == -999)
                        {
                            return 0;
                        }

                        // return the result
                        return result;
                    }
                    else
                    {
                        // just return zero as parse failed
                        return result;
                    }
                }

                // if the line contacts "dB" do the following
                if (line.Contains(pattern))
                {
                    // find the index
                    int index = line.IndexOf(pattern);

                    // remove dB from that line
                    string removeDb = line.Substring(0, index);

                    // try to parse that line if it is sucessfull return the result
                    if (Double.TryParse(removeDb, out result))
                    {
                        // if this is the first few seconds
                        if (result == -999)
                        {
                            return 0;
                        }

                        return result;
                    }
                    else
                    {
                        // just return zero as parse failed
                        return result;
                    }
                }
                // if it is just a line with no dB
                else
                {
                    // try to parse instantly
                    if (Double.TryParse(line, out result))
                    {
                        // if this is the first few seconds
                        if (result == -999)
                        {
                            return 0;
                        }

                        // return that result if it is sucessfull
                        return result;
                    }
                    else
                    {
                        // otherwise just return zero - unparsed result
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                // throw an exception with the line number so we have the opportunaty to see what has broken
                // this particular method - this should go straight to the log files
                throw new Exception(String.Format("{0}\n Line: {1}", ex.Message, line), ex);
            }
        }

        /// <summary>
        /// Performs a Log Average Calculation
        /// </summary>
        /// <param name="p_Values">Numbers where calculation is performed</param>
        /// <returns>Log Average Value</returns>
        public static Double LogAverageAlgorithm(List<Double> p_Values)
        {
            // ten multiplied by
            return (10 *
                // Log to the base 10 of.... 
                // the sum of each value to the power of ten divided by ten
                        (
                            Math.Log10(p_Values.Sum(value => Math.Pow(10, value / 10)) /

                        // then we divide by the nummber in the collection i.e 5, 15 
                // (represents interval)
                            (Double)p_Values.Count)
                        )
                    );
        }
    }
}
