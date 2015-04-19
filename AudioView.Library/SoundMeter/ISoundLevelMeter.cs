namespace AudioView.Library.SoundMeter
{
    using System;
    using System.Collections.Generic;
    using Exceptions;

    public interface ISoundLevelMeter
    {
        /// <summary>
        /// Open the port to the device
        /// </summary>
        void Start();

        /// <summary>
        /// Close the port
        /// 
        /// As this tries to write to the device, might it cause a time out, but the connection will still be closed.
        /// </summary>
        /// <exception cref="TimeoutException">When timed out</exception>
        void Close();

        /// <summary>
        /// Get a reading
        /// </summary>
        /// <exception cref="TimeoutException">When time out</exception>
        /// <exception cref="NotFoundException">When the device is gone</exception>
        /// <returns></returns>
        Dictionary<string, double> GetReading();

        /// <summary>
        /// Tests if we are able to get a reading the a port name
        /// </summary>
        /// <param name="portName"></param>
        /// <returns></returns>
        bool Test();
    }
}