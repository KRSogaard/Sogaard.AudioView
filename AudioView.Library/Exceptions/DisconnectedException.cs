namespace AudioView.Library.Exceptions
{
    using System;

    public class DisconnectedException : Exception
    {
        public DisconnectedException(string message) 
            : base(message)
        {
            
        }
    }
}