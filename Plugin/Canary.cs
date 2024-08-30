using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.PluginTelemetry;
using Rappen.Dataverse.Canary;
using System;

namespace Rappen.Canary365.Plugin
{
    public class Canary : IPlugin
    {
        private string _unsec;

        public Canary(string unsecure)
        {
            _unsec = unsecure;
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            var ts = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            try
            {
                ts.Trace("Trace enter: {0:o}\n", DateTime.Now);
                if (!string.IsNullOrEmpty(_unsec))
                {
                    ts.Trace("Configuration:\n{0}", _unsec);
                }

                // Classic initialization for plugin context - should be in a Base Class...
                var ctx = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                var sfact = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                var svc = sfact.CreateOrganizationService(ctx.UserId);

                // Flags that may ge set in the unsecure configuration
                var parentcontext = !string.IsNullOrEmpty(_unsec) && _unsec.ToUpperInvariant().Contains("PARENTCONTEXT=TRUE");
                var attributetypes = !string.IsNullOrEmpty(_unsec) && _unsec.ToUpperInvariant().Contains("ATTRIBUTETYPES=TRUE");
                var convertqueries = !string.IsNullOrEmpty(_unsec) && _unsec.ToUpperInvariant().Contains("CONVERTQUERIES=TRUE");
                var expandcollections = !string.IsNullOrEmpty(_unsec) && _unsec.ToUpperInvariant().Contains("EXPANDCOLLECTIONS=TRUE");
                var includestage30 = !string.IsNullOrEmpty(_unsec) && _unsec.ToUpperInvariant().Contains("INCLUDESTAGE30=TRUE");

                // Calling CanaryTracer!
                ts.TraceContext(ctx, parentcontext, attributetypes, convertqueries, expandcollections, includestage30, svc);

                // Trying to use the ILogger service
                var pt = (ILogger)serviceProvider.GetService(typeof(ILogger));
                try
                {
                    pt.LogInformation("Canary write by LogInformation");
                    pt.LogTrace("Canary write by LogTrace");
                    ts.Trace("PluginTelemetry OK");
                }
                catch (Exception ex)
                {
                    ts.Trace("PluginTelemetry Error:");
                    ts.Trace(ex.Message);
                    pt.LogError(ex, ex.Message);
                }
            }
            catch (Exception ex)
            {
                ts.Trace("††† Canary is dead †††\n{0}", ex);
            }
            finally
            {
                ts.Trace("\nTrace exit: {0:o}\n", DateTime.Now);
            }
        }
    }
}