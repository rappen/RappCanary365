using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rappen.Canary365.Plugin
{
    public static class CanaryTracer
    {
        public static void TraceContext(ITracingService ts, IPluginExecutionContext ctx, IOrganizationService svc, bool parentcontext, bool convertqueries, int depth = 1)
        {
            if (ctx.Stage != 30)
            {
                ts.Trace("--- Context {0} Trace Start ---", depth);
                ts.Trace("Message : {0}", ctx.MessageName);
                ts.Trace("Stage   : {0}", ctx.Stage);
                ts.Trace("Mode    : {0}", ctx.Mode);
                ts.Trace("Depth   : {0}", ctx.Depth);
                ts.Trace("Entity  : {0}", ctx.PrimaryEntityName);
                if (!ctx.PrimaryEntityId.Equals(Guid.Empty))
                {
                    ts.Trace("Id      : {0}", ctx.PrimaryEntityId);
                }
                ts.Trace("");

                TraceAndAlign("InputParameters", ctx.InputParameters, ts, svc, convertqueries);
                TraceAndAlign("OutputParameters", ctx.OutputParameters, ts, svc, convertqueries);
                TraceAndAlign("SharedVariables", ctx.SharedVariables, ts, svc, convertqueries);
                TraceAndAlign("PreEntityImages", ctx.PreEntityImages, ts, svc, convertqueries);
                TraceAndAlign("PostEntityImages", ctx.PostEntityImages, ts, svc, convertqueries);
                ts.Trace("--- Context {0} Trace End ---", depth);
            }
            if (parentcontext && ctx.ParentContext != null)
            {
                TraceContext(ts, ctx.ParentContext, svc, parentcontext, convertqueries, depth + 1);
            }
            ts.Trace("");
        }

        private static void TraceAndAlign<T>(string topic, IEnumerable<KeyValuePair<string, T>> pc, ITracingService ts, IOrganizationService svc, bool convertqueries)
        {
            if (pc == null || pc.Count() == 0) { return; }
            ts.Trace(topic);
            var keylen = pc.Max(p => p.Key.Length);
            foreach (var p in pc)
            {
                ts.Trace($"  {p.Key}{new string(' ', keylen - p.Key.Length)} = {ValueToString(p.Value, svc, convertqueries, 2)}");
            }
        }

        private static string ValueToString(object v, IOrganizationService svc, bool convertqueries, int indent = 1)
        {
            var ind = new string(' ', indent * 2);
            if (v == null)
            {
                return $"{ind}<null>";
            }
            else if (v is Entity e)
            {
                var keylen = e.Attributes.Count > 0 ? e.Attributes.Max(p => p.Key.Length) : 50;
                return $"{e.LogicalName} {e.Id}\n{ind}" + string.Join($"\n{ind}", e.Attributes.OrderBy(a => a.Key).Select(a => $"{a.Key}{new string(' ', keylen - a.Key.Length)} = {ValueToString(a.Value, svc, convertqueries, indent + 1)}"));
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
            else if (v is QueryExpression q && convertqueries)
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
                    r = o.Value.ToString();
                }
                else if (v is Money m)
                {
                    r = m.Value.ToString();
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
