namespace AudioView.Library.SoundMeter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using Exceptions;
    using RJCP.IO.Ports;

    public class NtiXl2SoundLevelMeter : ISoundLevelMeter
    {
        // Meter Commands
        private const string Query = "*IDN?";
        private const string ResetSLM = "*RST";
        private const string StartLog = "INITATE START";
        private const string InitiateMeasurement = "MEAS:INIT";
        private const string LAEQ = "MEAS:SLM:123:dt? LAEQ";
        private const string MIN = "MEAS:SLM:123:dt? LAFMIN";
        private const string MAX = "MEAS:SLM:123:dt? LAFMAX";
        private const string LC_PEAK = "MEAS:SLM:123:dt? LCPKMAX";
        private const string Octive = "MEAS:SLM:RTA:DT? EQ";
        private const string ExistingSensitivity = "CALIB:MIC:SENSU:VALU?";
        private const string Stop = "INIT STOP";
        private const string CalibrateMic = "CALIB:MIC:SENS:VALU [{0}]";
        private const string Lock = "SYST:KLOCK ON";
        private const string UnLock = "SYST:KLOCK OFF";

        private string PortName { get; set; }
        private SerialPortStream SerialPort { get; set; }
        private int TimeToTimeOut = 1500;
        private bool Debug { get; set; }

        /// <summary>
        /// Modular XL2 Sould Level Meter class
        /// </summary>
        /// <param name="portName"></param>
        public NtiXl2SoundLevelMeter(string portName)
        {
            this.PortName = portName;
        }

        /// <summary>
        /// Open the port to the device
        /// </summary>
        public void Start()
        {
            if (this.SerialPort != null && this.SerialPort.IsOpen)
            {
                return;
            }

            try
            {
                this.SerialPort = new SerialPortStream(this.PortName);
                this.SerialPort.NewLine = "\n";
                this.SerialPort.Open();

                // Rest the meter
                this.WriteLine(ResetSLM);
                // Lock the keyboard
                this.WriteLine(Lock);
                // Start the loggin
                this.WriteLine(StartLog);
            }
            catch (IOException exp)
            {
                var parse = this.parseException(exp);
                if (parse != null)
                    throw parse;
                throw;
            }
        }

        /// <summary>
        /// Close the port
        /// 
        /// As this tries to write to the device, might it cause a time out, but the connection will still be closed.
        /// </summary>
        /// <exception cref="TimeoutException">When timed out</exception>
        public void Close()
        {
            if (this.Debug)
                Console.WriteLine("Close");

            try
            {
                // Lock the keyboard
                this.WriteLine(UnLock);
                // Stop the sound level meter
                this.WriteLine(Stop);

            }
            catch (Exception exp)
            {
                // Ignore not going to use it any way
            }
            finally 
            {

                try
                {
                    this.SerialPort.Close();
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Get a reading
        /// </summary>
        /// <exception cref="TimeoutException">When time out</exception>
        /// <exception cref="NotFoundException">When the device is gone</exception>
        /// <returns></returns>
        public Dictionary<string, double> GetReading()
        {
            try
            {
                var result = new Dictionary<string, double>();
                // tell the meter we want the measurement
                this.WriteLine(InitiateMeasurement);

                // ask for leq
                this.WriteLine(LAEQ);
                // getiting LEQ
                result.Add("LEQ", Utilities.ParseMeasurement(this.ReadToDB()));

                // ask for the min
                this.WriteLine(MIN);
                // getiting min
                result.Add("MIN", Utilities.ParseMeasurement(this.ReadToDB()));

                // ask for max
                this.WriteLine(MAX);
                // getiting max
                result.Add("MAX", Utilities.ParseMeasurement(this.ReadToDB()));

                // ask for lc peak
                this.WriteLine(LC_PEAK);
                // getting lc peak
                result.Add("LcPeak", Utilities.ParseMeasurement(this.ReadToDB()));

                // ask for octive
                this.WriteLine(Octive);

                // getting thr octive
                Utilities.ParseOctive(result, this.ReadToDB());

                return result;
            }
            catch (IOException exp)
            {
                var parse = this.parseException(exp);
                if (parse != null)
                    throw parse;
                throw;
            }
        }

        /// <summary>
        /// Tests if we are able to get a reading the a port name
        /// </summary>
        /// <param name="portName"></param>
        /// <returns></returns>
        public bool Test()
        {
            bool result = false;
            try
            {
                this.Start();
                this.GetReading();
                result = true;
            }
            catch (Exception exp)
            {
                result = false;
            }
            finally
            {
                try
                {
                    this.Close();
                }
                catch (Exception exp)
                {
                    // Fine
                }
            }
            return result;
        }

        /// <summary>
        /// Parses the exception
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        private Exception parseException(Exception exp)
        {
            if (exp.Message.ToLower().Contains("port not found") || 
                exp.Message.ToLower().Contains("No device on port"))
            {
                Console.WriteLine("Sound Meter Device Not Found");
                return new NotFoundException("No device on port " + this.PortName + " was found");
            }
            return null;
        }

        /// <summary>
        /// Write to the device, and flush
        /// </summary>
        /// <param name="line"></param>
        private void WriteLine(string line)
        {
            if (this.SerialPort == null || !this.SerialPort.IsOpen)
            {
                return;
            }

            bool result = false;
            Thread t = new Thread(() =>
            {
                this.SerialPort.WriteLine(line);
                this.SerialPort.Flush();
                this.SerialPort.DiscardOutBuffer();
                result = true;
            });
            t.Start();
            DateTime end = DateTime.Now + new TimeSpan(0, 0, 0, 0, TimeToTimeOut);
            while (DateTime.Now <= end)
            {
                if (result)
                {
                    break;
                }
                Thread.Sleep(50);
            }
            if (!result)
            {
                try
                {
                    t.Abort();
                }
                catch (ThreadAbortException)
                {
                    // Fine
                }


                try
                {
                    if (this.SerialPort != null && this.SerialPort.IsOpen)
                    {
                        this.SerialPort.DiscardInBuffer();
                    }
                }
                catch (Exception)
                {

                }
                throw new TimeoutException();
            }
        }

        /// <summary>
        /// Read to the line db, and disgards rest of the buffer
        /// </summary>
        /// <returns></returns>
        private string ReadToDB()
        {
            string result = null;
            Thread t = new Thread(() =>
            {
                StringBuilder builder = new StringBuilder();
                char?[] buffer = new char?[2];
                while (true)
                {
                    var b = this.SerialPort.ReadByte();
                    if (b != -1)
                    {
                        if (buffer[0] != null)
                        {
                            if (this.Debug)
                                Console.Write(buffer[0]);
                            builder.Append(buffer[0]);
                        }
                        buffer[0] = buffer[1];
                        buffer[1] = Convert.ToChar(b);

                        if (buffer[0] == 'd' && buffer[1] == 'B')
                        {
                            if (this.Debug)
                                Console.WriteLine("");
                            result = builder.ToString().Trim();
                            break;
                        }
                    }
                }
            });
            t.Start();
            DateTime end = DateTime.Now + new TimeSpan(0, 0, 0, 0, TimeToTimeOut);
            while (DateTime.Now <= end)
            {
                if (result != null)
                {
                    break;
                }
                Thread.Sleep(50);
            }
            if (result == null)
            {
                try
                {
                    t.Abort();
                }
                catch (ThreadAbortException)
                {
                    // Fine
                }

                try
                {
                    if (this.SerialPort != null && this.SerialPort.IsOpen)
                    {
                        this.SerialPort.DiscardInBuffer();
                    }
                }
                catch{}
                throw new TimeoutException();
            }

            try
            {
                if (this.SerialPort != null && this.SerialPort.IsOpen)
                {
                    this.SerialPort.DiscardInBuffer();
                }
            }
            catch{}

            return result;
        }
    }
}
