﻿using System;

namespace mRemoteNG.Config.Connections
{
    public delegate void UpdateCheckFinishedEventHandler(object sender, ConnectionsUpdateCheckFinishedEventArgs args);

    public class ConnectionsUpdateCheckFinishedEventArgs : EventArgs
    {
        public bool UpdateAvailable { get; set; }
    }
}