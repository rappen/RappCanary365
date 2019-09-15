using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

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
                    ts.Trace("Configuration: {0}", _unsec);
                }
                var ctx = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                var sfact = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                var svc = sfact.CreateOrganizationService(ctx.UserId);
                TraceContext(ts, ctx, svc);
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

        private void TraceContext(ITracingService ts, IPluginExecutionContext ctx, IOrganizationService svc)
        {
            ts.Trace("Message : {0}", ctx.MessageName);
            ts.Trace("Stage   : {0}", ctx.Stage);
            ts.Trace("Mode    : {0}", ctx.Mode);
            ts.Trace("Entity  : {0}", ctx.PrimaryEntityName);
            if (!ctx.PrimaryEntityId.Equals(Guid.Empty))
            {
                ts.Trace("Id      : {0}", ctx.PrimaryEntityId);
            }
            ts.Trace("");

            TraceAndAlign("InputParameters", ctx.InputParameters, ts, svc);
            TraceAndAlign("OutputParameters", ctx.OutputParameters, ts, svc);
            TraceAndAlign("SharedVariables", ctx.SharedVariables, ts, svc);
            TraceAndAlign("PreEntityImages", ctx.PreEntityImages, ts, svc);
            TraceAndAlign("PostEntityImages", ctx.PostEntityImages, ts, svc);
            if (ctx.ParentContext != null && !string.IsNullOrEmpty(_unsec) && _unsec.ToUpperInvariant().Contains("PARENTCONTEXT=TRUE"))
            {
                ts.Trace("\nParent Context:");
                TraceContext(ts, ctx.ParentContext, svc);
            }
        }

        private static void TraceAndAlign<T>(string topic, IEnumerable<KeyValuePair<string, T>> pc, ITracingService ts, IOrganizationService svc)
        {
            if (pc == null || pc.Count() == 0) { return; }
            ts.Trace(topic);
            var keylen = pc.Max(p => p.Key.Length);
            foreach (var p in pc)
            {
                ts.Trace($"  {p.Key}{new string(' ', keylen - p.Key.Length)} = {ValueToString(p.Value, svc, 2)}");
            }
        }

        private static string ValueToString(object v, IOrganizationService svc, int indent = 1)
        {
            var ind = new string(' ', indent * 2);
            if (v == null)
            {
                return $"{ind}<null>";
            }
            else if (v is Entity e)
            {
                var keylen = e.Attributes.Count > 0 ? e.Attributes.Max(p => p.Key.Length) : 50;
                return $"{e.LogicalName} {e.Id}\n{ind}" + string.Join($"\n{ind}", e.Attributes.OrderBy(a => a.Key).Select(a => $"{a.Key}{new string(' ', keylen - a.Key.Length)} = {ValueToString(a.Value, svc, indent + 1)}"));
            }
            else if (v is ColumnSet c)
            {
                var a = new List<string>(c.Columns);
                a.Sort();
                return $"\n{ind}" + string.Join($"\n{ind}", a);
            }
            else if (v is FetchExpression f)
            {
                return $"{v}\n{ind}{f.Query}";
            }
            else if (v is QueryExpression q)
            {
                var fx = (svc.Execute(new QueryExpressionToFetchXmlRequest { Query = q }) as QueryExpressionToFetchXmlResponse).FetchXml;
                return $"{q}\n{ind}{fx}";
            }
            else
            {
                var r = string.Empty;
                if (v is EntityReference er)
                {
                    r = $"{er.LogicalName} {er.Id} {er.Name}";
                }
                else if (v is OptionSetValue o)
                {
                    r = ((OptionSetValue)v).Value.ToString();
                }
                else if (v is Money m)
                {
                    r = ((Money)m).Value.ToString();
                }
                else
                {
                    r = v.ToString().Replace("\n", $"\n  {ind}");
                }
                return r + $" \t({v.GetType()})";
            }
        }
    }
}
