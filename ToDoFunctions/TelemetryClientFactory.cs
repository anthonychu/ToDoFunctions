using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;

namespace ToDoFunctions
{
    public class TelemetryClientFactory : ITelemetryClientFactory
    {
        private static TelemetryClient _client;
        public virtual TelemetryClient GetClient()
        {
            if (_client == null)
            {
                string key = TelemetryConfiguration.Active.InstrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", EnvironmentVariableTarget.Process);
                _client = new TelemetryClient()
                {
                    InstrumentationKey = key
                };
            }
            return _client;
        }
    }

    public interface ITelemetryClientFactory
    {
        TelemetryClient GetClient();
    }
}
