using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WebJobs.Script.K8Host
{
    public class FluentDJsonFormatter : ITextFormatter
    {
        readonly JsonValueFormatter _valueFormatter;

        /// <summary>
        /// Construct a <see cref="CompactJsonFormatter"/>, optionally supplying a formatter for
        /// <see cref="LogEventPropertyValue"/>s on the event.
        /// </summary>
        /// <param name="valueFormatter">A value formatter, or null.</param>
        public FluentDJsonFormatter(JsonValueFormatter valueFormatter = null)
        {
            _valueFormatter = valueFormatter ?? new JsonValueFormatter(typeTagName: "$type");
        }

        public void Format(LogEvent logEvent, TextWriter output)
        {
            FormatEvent(logEvent, output, _valueFormatter);
            output.WriteLine();
        }

        public static void FormatEvent(LogEvent logEvent, TextWriter output, JsonValueFormatter valueFormatter)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (valueFormatter == null) throw new ArgumentNullException(nameof(valueFormatter));

            output.Write("{\"time\":\"");
            output.Write(logEvent.Timestamp.UtcDateTime.ToString("O"));
            output.Write("\",\"message\":");

            var message = logEvent.MessageTemplate.Render(logEvent.Properties);
            JsonValueFormatter.WriteQuotedJsonString(message, output);
            output.Write(",\"id\":\"");
            var id = ComputeHash(logEvent.MessageTemplate.Text);
            output.Write(id.ToString("x8"));
            output.Write('"');

            if (logEvent.Level != LogEventLevel.Information)
            {
                output.Write(",\"level\":\"");
                output.Write(logEvent.Level);
                output.Write('\"');
            }

            if (logEvent.Exception != null)
            {
                output.Write(",\"exception\":");
                JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
            }

            foreach (var property in logEvent.Properties)
            {
                var name = property.Key;
                if (name.Length > 0 && name[0] == '@')
                {
                    // Escape first '@' by doubling
                    name = '@' + name;
                }

                output.Write(',');
                JsonValueFormatter.WriteQuotedJsonString(name, output);
                output.Write(':');
                valueFormatter.Format(property.Value, output);
            }

            output.Write('}');
        }
         
        /// <summary>
        /// Compute a 32-bit hash of the provided <paramref name="messageTemplate"/>. The
        /// resulting hash value can be uses as an event id in lieu of transmitting the
        /// full template string.
        /// </summary>
        /// <param name="messageTemplate">A message template.</param>
        /// <returns>A 32-bit hash of the template.</returns>
        public static uint ComputeHash(string messageTemplate)
        {
            if (messageTemplate == null) throw new ArgumentNullException(nameof(messageTemplate));

            // Jenkins one-at-a-time https://en.wikipedia.org/wiki/Jenkins_hash_function
            unchecked
            {
                uint hash = 0;
                for (var i = 0; i < messageTemplate.Length; ++i)
                {
                    hash += messageTemplate[i];
                    hash += (hash << 10);
                    hash ^= (hash >> 6);
                }
                hash += (hash << 3);
                hash ^= (hash >> 11);
                hash += (hash << 15);
                return hash;
            }
        }      
    }
}
