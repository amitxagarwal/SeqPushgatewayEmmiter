using Prometheus.Client;
using Prometheus.Client.MetricPusher;
using Seq.Apps;
using Seq.Apps.LogEvents;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace SeqPushgatewayEmmiter
{
    [SeqApp("Seq.PushgatewayEmmiter",
        Description = "Filtered events are sent to the Pushgateway.")]
    public class SeqToPushgateway : Reactor, ISubscribeTo<LogEventData>
    {
        //private readonly object pushGatewayNotification;

        [SeqAppSetting(
            DisplayName = "Pushgateway URL",
            HelpText = "The URL of the Pushgateway")]
        public string PushgatewayUrl { get; set; }

        public void On(Event<LogEventData> evt)
        {
            var applicationName = FormatTemplate(evt);
            var pushGatewayUrl = PushgatewayUrl;
            var seqPushgatewayWorkerName = "pushgateway-testworker";
            var instanceName = "default";

            var defaultPusher = new MetricPusher(pushGatewayUrl, seqPushgatewayWorkerName, instanceName);
            IMetricPushServer server = new MetricPushServer(new IMetricPusher[]
            {
                defaultPusher
            });
            server.Start();
            var counter = Metrics.CreateCounter("webjobcounter", "helptext", new[] { "ApplicationName" });
            counter.Labels(applicationName).Inc();
            server.Stop();
        }

        public static string FormatTemplate(Event<LogEventData> evt)
        {
            var properties = (IDictionary<string, object>)ToDynamic(evt.Data.Properties ?? new Dictionary<string, object>());
            var resourceName = string.Empty;

            foreach (var property in properties)
            {
                if (property.Key == "Application")
                {
                    resourceName = property.Value.ToString();
                }
            }
            return resourceName;
        }

        private static object ToDynamic(object o)
        {
            if (o is IEnumerable<KeyValuePair<string, object>> dictionary)
            {
                var result = new ExpandoObject();
                var asDict = (IDictionary<string, object>)result;
                foreach (var kvp in dictionary)
                    asDict.Add(kvp.Key, kvp.Value);
                return result;
            }

            if (o is IEnumerable<object> enumerable)
            {
                return enumerable.Select(ToDynamic).ToArray();
            }

            return o;
        }
    }
    
}
