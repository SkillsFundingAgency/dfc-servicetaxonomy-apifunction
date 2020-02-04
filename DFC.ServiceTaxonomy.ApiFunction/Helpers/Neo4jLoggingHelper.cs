using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public class Neo4jLoggingHelper : Neo4j.Driver.ILogger
    {

        private readonly Microsoft.Extensions.Logging.ILogger _delegator;
        private Stopwatch _timer = new Stopwatch();
        private Boolean _timerRunning = false;
        public long handshakeTimeElapsed { get; set; }


        public Neo4jLoggingHelper(Microsoft.Extensions.Logging.ILogger delegator)
        {
            _delegator = delegator;
        }

        
        public void Error(Exception cause, string format, params object[] args)
        {
            _delegator.LogError(default(EventId), cause, format, args);
        }
        public void Warn(Exception cause, string format, params object[] args)
        {
            _delegator.LogWarning(default(EventId), cause, format, args);
        }
        public void Info(string format, params object[] args)
        {
            _delegator.LogInformation(default(EventId), format, args);
        }
        public void Debug(string format, params object[] args)
        {
            _delegator.LogDebug(default(EventId), format, args);

            if ( format.Contains("[CONNECT]") )
            {
                _timer.Start();
                _timerRunning = true;
            }
            if ( _timerRunning && ( format.Contains("S:")  ) )//|| args[0].ToString().Contains("SUCCESS") ) )
            {
                _timer.Stop();
                handshakeTimeElapsed = _timer.ElapsedMilliseconds;
                _timerRunning = false;
            }
        }
        public void Trace(string format, params object[] args)
        {
            _delegator.LogTrace(default(EventId), format, args);
        }
        public bool IsTraceEnabled()
        {
            return _delegator.IsEnabled(LogLevel.Trace);
        }
        public bool IsDebugEnabled()
        {
            return _delegator.IsEnabled(LogLevel.Debug);
        }

    }
}
