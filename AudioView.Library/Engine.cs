using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioView.Library
{
    using System.Threading;
    using System.Timers;
    using SoundMeter;

    public delegate void ReadingEvent(DateTime timeOfReading, Dictionary<string, double> reading);
    
    public delegate void ReadingTimeedEvent(DateTime timeOfReading);

    

    public class Engine
    {
        private ISoundLevelMeter SoundLevelMeter { get; set; }

        private System.Timers.Timer Timer { get; set; }

        private bool IsDeviceDisconnected { get; set; }
        private int CurrentTimeOuts { get; set; }

        private DateTime LastMinuteCalculation { get; set; }
        private List<Tuple<DateTime,Dictionary<string, double>>> MinuteReadings { get; set; }  
        private object LockMinutesReadingsList = new object();

        private int ReadingInterval = 1000;
        private int TimeOutsBeforeDisconnect = 3;

        public event ReadingEvent ReadingSecond;
        public event ReadingEvent ReadingMinute;
        public event ReadingTimeedEvent ReadingTimeout;
        public event ReadingTimeedEvent DeviceDisconnected;
        public event ReadingTimeedEvent DeviceReconnected;

        public Engine(ISoundLevelMeter soundLevelMeter)
        {
            this.SoundLevelMeter = soundLevelMeter;
        }

        public void Start()
        {
            this.SoundLevelMeter.Start();

            this.MinuteReadings = new List<Tuple<DateTime, Dictionary<string, double>>>();
            this.LastMinuteCalculation = DateTime.Now;
            this.CancellationToken = new CancellationTokenSource();
            this.Timer = new System.Timers.Timer(this.ReadingInterval);
            this.Timer.Elapsed += this.TimerOnElapsed;
            this.Timer.Start();
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            DateTime readingTime = DateTime.UtcNow;

            try
            {
                Dictionary<string, double> reading = this.SoundLevelMeter.GetReading();

                // Good read
                this.CurrentTimeOuts = 0;
                if (this.IsDeviceDisconnected)
                {
                    this.IsDeviceDisconnected = false;
                    if (this.DeviceReconnected != null)
                    {
                        this.DeviceReconnected(readingTime);
                    }
                }

                if (this.ReadingSecond != null)
                {
                    this.ReadingSecond(readingTime, reading);
                }

                // Minute calculation
                if ((DateTime.Now - this.LastMinuteCalculation).TotalMinutes >= 1)
                {
                    // Copy the readins over so we can work with it in pease
                    List<Tuple<DateTime, Dictionary<string, double>>> readings;
                    lock (this.LockMinutesReadingsList)
                    {
                        readings = this.MinuteReadings;
                        this.MinuteReadings = new List<Tuple<DateTime, Dictionary<string, double>>>();
                    }
                    // Run this in a new task, so it will not block
                    Task.Run(() =>
                    {
                        var minuteReadingTime = DateTime.Now;
                        var minuteReadings = new Dictionary<string, double>();
                        if (readings != null && reading.Count > 0)
                        {
                            foreach (var key in readings[0].Item2.Keys)
                            {
                                minuteReadings.Add(key, Utilities.LogAverageAlgorithm(readings.Where(x=>x.Item2.ContainsKey(key)).Select(x => x.Item2[key]).ToList()));
                            }
                            minuteReadingTime = readings.Last().Item1;
                        }

                        // Copies it over
                        if (this.ReadingMinute != null)
                        {
                            this.ReadingMinute(minuteReadingTime, minuteReadings);
                        }
                    });
                }
                // Should not be nessesary, jsut to be sure
                lock (this.LockMinutesReadingsList)
                {
                    this.MinuteReadings.Add(new Tuple<DateTime, Dictionary<string, double>>(readingTime, reading));
                }
            }
            catch (TimeoutException exp)
            {
                // If the device is already disconnecte return.
                // The observers have already been notified about the
                // timeouts and disconnect, wait for reconnect.
                if (this.IsDeviceDisconnected)
                {
                    return;
                }

                this.CurrentTimeOuts++;
                if (this.CurrentTimeOuts > this.TimeOutsBeforeDisconnect)
                {
                    this.IsDeviceDisconnected = true;
                }

                if (!this.IsDeviceDisconnected && this.ReadingTimeout != null)
                {
                    this.ReadingTimeout(readingTime);
                }
                if (this.IsDeviceDisconnected && this.DeviceDisconnected != null)
                {
                    this.DeviceDisconnected(readingTime);
                }
            }
        }

        public void Stop()
        {
            // Never been started
            if (this.CancellationToken == null)
            {
                return;
            }

            this.CancellationToken.Cancel();
            this.Timer.Stop();
            this.SoundLevelMeter.Close();
        }
        
        public void ReplaceMeter(ISoundLevelMeter soundLevelMeter)
        {
            if (soundLevelMeter == null)
            {
                throw new ArgumentNullException("soundLevelMeter can not be null");
            }

            if (this.SoundLevelMeter != null)
            {
                this.SoundLevelMeter.Close();
            }

            this.SoundLevelMeter = soundLevelMeter;
            this.SoundLevelMeter.Start();
        }
    }
}
