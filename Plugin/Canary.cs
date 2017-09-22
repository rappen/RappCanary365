using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rappen.Canary365.Plugin
{
    public class Canary : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var ts = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            try
            {
                ts.Trace("Trace enter: {0:o}\n", DateTime.Now);
                var ctx = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                ts.Trace("Found context in serviceprovider {0}", 0);
                TraceContext(ts, ctx);
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

        private static void TraceContext(ITracingService ts, IPluginExecutionContext ctx)
        {
            ts.Trace("Message: {0}", ctx.MessageName);
            ts.Trace("Stage: {0}", ctx.Stage);
            ts.Trace("Mode: {0}", ctx.Mode);
            ts.Trace("Entity: {0}", ctx.PrimaryEntityName);
            if (!ctx.PrimaryEntityId.Equals(Guid.Empty))
            {
                ts.Trace("Id: {0}", ctx.PrimaryEntityId);
            }
            var ip = ctx.InputParameters;

            TraceAndAlign("IP", ctx.InputParameters, ts);
            TraceAndAlign("OP", ctx.OutputParameters, ts);
            TraceAndAlign("SV", ctx.SharedVariables, ts);
            TraceAndAlign("Pre", ctx.PreEntityImages, ts);
            TraceAndAlign("Post", ctx.PostEntityImages, ts);
            if (ctx.ParentContext != null)
            {
                ts.Trace("\nParent Context:");
                TraceContext(ts, ctx.ParentContext);
            }
        }

        private static void TraceAndAlign<T> (string topic, IEnumerable<KeyValuePair<string, T>> pc, ITracingService ts)
        {
            if (pc == null || pc.Count() == 0) { return; }
            var keylen = pc.Max(p => p.Key.Length);
            foreach (var p in pc)
            {
                ts.Trace($"{topic} {p.Key}{new string(' ', keylen - p.Key.Length)} = {ValueToString(p.Value)}");
            }
        }

        private static string ValueToString(object v)
        {
            if (v == null)
            {
                return "<null>";
            }
            else if (v is Entity e)
            {
                var keylen = e.Attributes.Count > 0 ? e.Attributes.Max(p => p.Key.Length) : 50;
                return $"{e.LogicalName} {e.Id}\n  " + string.Join("\n  ", e.Attributes.OrderBy(a => a.Key).Select(a => $"{a.Key}{new string(' ', keylen - a.Key.Length)} = {ValueToString(a.Value)}"));
            }
            else if (v is ColumnSet c)
            {
                var a = new List<string>(c.Columns);
                a.Sort();
                return "\n  " + string.Join("\n  ", a);
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
                    r = v.ToString();
                }
                return r + $" \t({v.GetType()})";
            }
        }
    }
}
