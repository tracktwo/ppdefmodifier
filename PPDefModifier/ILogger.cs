using System;
using System.Diagnostics;

namespace PPDefModifier {
    using ModnixCallback = Func<string, object, object>;
    using ModnixLogFunction = Action<TraceEventType, object, object[]>;

    public interface ILogger
    {
        void Log(String msg, params object[] args);
        void Error(String msg, params object[] args);
    }

    public static class PPDefLogger {
        /// <summary>
        /// Store current logger and serve as its private lock object.
        /// Starts with a UnityLogger as default.
        /// </summary>
        private static readonly ILogger[] _logger = new ILogger[]{new UnityLogger()};

        /// <summary>
        /// Get current logger
        /// </summary>
        public static ILogger logger
        {
            get
            {
                lock (_logger)
                {
                    return _logger[0];
                }
            }
        }

        /// <summary>
        /// Switch to ModnixLogger if provided api is not <see langword="null"/>.
        /// </summary>
        public static void SetLogger(ModnixCallback api)
        {
            if (api != null && !(_logger[0] is ModnixLogger))
            {
                lock (_logger)
                {
                    _logger[0] = new ModnixLogger(api);
                }
            }
        }
    }

    public class ModnixLogger : ILogger
    {
        public ModnixLogger(ModnixCallback api)
        {
            this.logger = api("logger", "TraceEventType") as ModnixLogFunction;
        }

        public void Log(String msg, params object[] args)
        {
            logger(TraceEventType.Information, msg, args);
        }

        public void Error(String msg, params object[] args)
        {
            logger(TraceEventType.Error, msg, args);
        }

        public ModnixLogFunction logger;
    }

    public class UnityLogger : ILogger
    {
        public UnityLogger() { }

        public void Log(String msg, params object[] args)
        {
            UnityEngine.Debug.LogFormat("PPDefModifier: " + msg, args);
        }

        public void Error(String msg, params object[] args)
        {
            UnityEngine.Debug.LogErrorFormat("PPDefModifier: " + msg, args);
        }
    }

}
