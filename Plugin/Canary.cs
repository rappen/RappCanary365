using Microsoft.Xrm.Sdk;
using Rappen.CDS.Canary;
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
                var ctx = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                var sfact = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                var svc = sfact.CreateOrganizationService(ctx.UserId);

                var parentcontext = !string.IsNullOrEmpty(_unsec) && _unsec.ToUpperInvariant().Contains("PARENTCONTEXT=TRUE");
                var attributetypes = !string.IsNullOrEmpty(_unsec) && _unsec.ToUpperInvariant().Contains("ATTRIBUTETYPES=TRUE");
                var convertqueries = !string.IsNullOrEmpty(_unsec) && _unsec.ToUpperInvariant().Contains("CONVERTQUERIES=TRUE");
                var expandcollections = !string.IsNullOrEmpty(_unsec) && _unsec.ToUpperInvariant().Contains("EXPANDCOLLECTIONS=TRUE");

                ts.TraceContext(ctx, parentcontext, attributetypes, convertqueries, expandcollections, svc);
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
