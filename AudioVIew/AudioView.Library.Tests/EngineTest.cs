using System;
using AudioView.Library.SoundMeter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AudioView.Library.Tests
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using SoundMeter;

    [TestClass]
    public class EngineTest
    {
        private Mock<ISoundLevelMeter> soundLevelMeter;
        private Engine engine;
        
        [TestInitialize]
        public void SetUp()
        {
            this.soundLevelMeter = new Mock<ISoundLevelMeter>();
            this.engine = new Engine(soundLevelMeter.Object);
        }

        [TestCleanup]
        public void Clean()
        {
            this.soundLevelMeter = null;
            this.engine = null;
        }

        [TestMethod]
        public void Test_Load_Sound_Level_Meter()
        {
            new Engine(soundLevelMeter.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_Load_With_Null_Thow_Exception()
        {
            new Engine(null);
        }

        [TestMethod]
        public void Test_Call_Stop_On_None_Started()
        {
            this.engine.Stop();
            this.soundLevelMeter.Verify(x=> x.Close(), Times.Once());
        }

        [TestMethod]
        public void Test_Start_Started_The_Sound_Level_Meter()
        {
            this.engine.Start();
            this.soundLevelMeter.Verify(x => x.Start(), Times.Once());
        }

        [TestMethod]
        public void Test_Stop_Close_The_Sould_Level_Meter()
        {
            this.engine.Start();
            this.engine.Stop();
            this.soundLevelMeter.Verify(x => x.Close(), Times.Once());
        }

        [TestMethod]
        public void Test_Stop_On_Not_Started_Should_Still_Call_Close_On_The_Meter()
        {
            this.engine.Stop();
            this.soundLevelMeter.Verify(x => x.Close(), Times.Once());
        }

        [TestMethod]
        public async Task Test_Perform_Five_Reading_In_Five_Seconds()
        {
            object locking = new object();
            int eventFires = 0;
            this.engine.ReadingSecond += (reading, reading1) =>
            {
                lock (locking)
                {
                    eventFires++;
                }
            };
            this.soundLevelMeter.Setup(x => x.GetReading()).Returns(new Dictionary<string,double>());
            this.engine.Start();
            await Task.Delay(5200);
            Assert.AreEqual(5, eventFires);
            this.soundLevelMeter.Verify(x => x.GetReading(), Times.Exactly(5));
        }

        [TestMethod]
        public async Task Test_Perform_Two_Reading_In_Two_Seconds_And_No_Reading_After_Stop()
        {
            object locking = new object();
            int eventFires = 0;
            this.engine.ReadingSecond += (reading, reading1) =>
            {
                lock (locking)
                {
                    eventFires++;
                }
            };
            this.soundLevelMeter.Setup(x => x.GetReading()).Returns(new Dictionary<string, double>());
            this.engine.Start();
            await Task.Delay(2500);
            Assert.AreEqual(2, eventFires);
            this.soundLevelMeter.Verify(x => x.GetReading(), Times.Exactly(2));
            this.engine.Stop();
            eventFires = 0;

            await Task.Delay(2900);
            Assert.AreEqual(0, eventFires);
            this.soundLevelMeter.Verify(x => x.GetReading(), Times.Exactly(2));
        }

        [TestMethod]
        public async Task Test_Timeout_Event_Is_Fired()
        {
            object locking = new object();
            int eventFires = 0;
            this.engine.ReadingTimeout += (reading) =>
            {
                lock (locking)
                {
                    eventFires++;
                }
            };
            this.soundLevelMeter.Setup(x => x.GetReading()).Throws(new TimeoutException());
            this.engine.Start();
            await Task.Delay(2500);
            this.engine.Stop();
            Assert.AreEqual(2, eventFires);
        }

        [TestMethod]
        public async Task Test_Timeouts_Event_And_Device_Disconnected_Once()
        {
            object locking = new object();
            int eventFires = 0;
            int disconnectEventFires = 0;
            this.engine.ReadingTimeout += (reading) =>
            {
                lock (locking)
                {
                    eventFires++;
                }
            };
            this.engine.DeviceDisconnected += (reading) =>
            {
                lock (locking)
                {
                    disconnectEventFires++;
                }
            };
            this.soundLevelMeter.Setup(x => x.GetReading()).Throws(new TimeoutException());
            this.engine.Start();
            await Task.Delay(6500);
            Assert.AreEqual(3, eventFires);
            Assert.AreEqual(1, disconnectEventFires);
            // Verify that it is still trying
            this.soundLevelMeter.Verify(x=>x.GetReading(), Times.Exactly(6));
        }

        [TestMethod]
        public async Task Test_Disconnected_Then_Reconnect()
        {
            object locking = new object();
            int eventFires = 0;
            int disconnectEventFires = 0;
            int reconnectEventFires = 0;
            this.engine.ReadingTimeout += (reading) =>
            {
                lock (locking)
                {
                    eventFires++;
                }
            };
            this.engine.DeviceDisconnected += (reading) =>
            {
                lock (locking)
                {
                    disconnectEventFires++;
                }
            };
            this.engine.DeviceReconnected += (reading) =>
            {
                lock (locking)
                {
                    reconnectEventFires++;
                }
            };
            this.soundLevelMeter.Setup(x => x.GetReading()).Throws(new TimeoutException());
            this.engine.Start();
            await Task.Delay(6500);
            Assert.AreEqual(3, eventFires);
            Assert.AreEqual(1, disconnectEventFires);
            // Verify that it is still trying
            this.soundLevelMeter.Verify(x => x.GetReading(), Times.Exactly(6));

            this.soundLevelMeter.Setup(x => x.GetReading()).Returns(new Dictionary<string, double>());
            await Task.Delay(2100);
            Assert.AreEqual(1, reconnectEventFires);
            this.soundLevelMeter.Verify(x => x.GetReading(), Times.Exactly(8));
        }

        [TestMethod]
        public async Task Test_One_Minute_Reading_Is_Fired_After_One_Minute()
        {
            object locking = new object();
            object lockingMinute = new object();
            int eventFires = 0;
            int minuteEventFires = 0;
            this.engine.ReadingSecond += (reading, reading1) =>
            {
                lock (locking)
                {
                    eventFires++;
                }
            };
            this.engine.ReadingMinute += (reading, reading1) =>
            {
                lock (lockingMinute)
                {
                    minuteEventFires++;
                }
            };
            this.soundLevelMeter.Setup(x => x.GetReading()).Returns(new Dictionary<string, double>());
            this.engine.Start();
            await Task.Delay(60500);
            Assert.AreEqual(60, eventFires);
            Assert.AreEqual(1, minuteEventFires);
            this.soundLevelMeter.Verify(x => x.GetReading(), Times.Exactly(60));
        }

        [TestMethod]
        public async Task Test_Replace_Meter()
        {
            var newSoundLevelMeter = new Mock<ISoundLevelMeter>();
            this.engine.ReplaceMeter(newSoundLevelMeter.Object);
            this.soundLevelMeter.Verify(x => x.Close(), Times.Once);
            newSoundLevelMeter.Verify(x => x.Start(), Times.Once);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task Test_Replace_Meter_With_Null()
        {
            this.engine.ReplaceMeter(null);
        }

        [TestMethod]
        public async Task Test_Replace_Meter_Make_Sure_New_Engine_Is_Used()
        {
            object locking = new object();
            int eventFires = 0;
            this.engine.ReadingSecond += (reading, reading1) =>
            {
                lock (locking)
                {
                    eventFires++;
                }
            };
            this.soundLevelMeter.Setup(x => x.GetReading()).Returns(new Dictionary<string, double>());
            this.engine.Start();
            await Task.Delay(5200);
            Assert.AreEqual(5, eventFires);
            this.soundLevelMeter.Verify(x => x.GetReading(), Times.Exactly(5));


            var newSoundLevelMeter = new Mock<ISoundLevelMeter>();
            this.engine.ReplaceMeter(newSoundLevelMeter.Object);

            eventFires = 0;
            await Task.Delay(5200);
            Assert.AreEqual(5, eventFires);
            this.soundLevelMeter.Verify(x => x.Close(), Times.Once);
            newSoundLevelMeter.Verify(x => x.Start(), Times.Once);
            // Verify nothing new have bene called on this
            this.soundLevelMeter.Verify(x => x.GetReading(), Times.Exactly(5));
            newSoundLevelMeter.Verify(x => x.GetReading(), Times.Exactly(5));
        }
    }
}
