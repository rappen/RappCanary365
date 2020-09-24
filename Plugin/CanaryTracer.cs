/* ***********************************************************
 * CanaryTracer.cs
 * Found at: https://jonasr.app/canary
 * Created by: Jonas Rapp https://jonasr.app/
 *
 * Writes everything from an IExecutionContext to the Plugin Trace Log.
 *
 * Sample call:
 *    tracingservice.TraceContext(context, 
 *        includeparentcontext, 
 *        includeattributetypes, 
 *        convertqueries, 
 *        expandcollections,
 *        service);
 *
 *               Enjoy responsibly.
 * **********************************************************/

namespace Rappen.CDS.Canary
{
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class CanaryTracer
    {
        /// <summary>
        /// Dumps everything interesting from the plugin context to the plugin trace log
        /// </summary>
        /// <param name="tracingservice"></param>
        /// <param name="context">The plugin or workflow context to trace.</param>
        /// <param name="parentcontext">Set to true if any parent contexts shall be traced too.</param>
        /// <param name="attributetypes">Set to true to include information about attribute types.</param>
        /// <param name="convertqueries">Set to true if any QueryExpression queries shall be converted to FetchXML and traced. Requires parameter service to be set.</param>
        /// <param name="expandcollections">Set to true if EntityCollection objects should list all contained Entity objects with all fields available.</param>
        /// <param name="service">Service used if convertqueries is true, may be null if not used.</param>
        public static void TraceContext(this ITracingService tracingservice, IExecutionContext context, bool parentcontext, bool attributetypes, bool convertqueries, bool expandcollections, IOrganizationService service)
        {
            tracingservice.TraceContext(context, parentcontext, attributetypes, convertqueries, expandcollections, service, 1);
        }

        private static void TraceContext(this ITracingService tracingservice, IExecutionContext context, bool parentcontext, bool attributetypes, bool convertqueries, bool expandcollections, IOrganizationService service, int depth)
        {
            var plugincontext = context as IPluginExecutionContext;
            if (plugincontext?.Stage != 30)
            {
                tracingservice.Trace("--- Context {0} Trace Start ---", depth);
                tracingservice.Trace("Message : {0}", context.MessageName);
                if (plugincontext != null)
                {
                    tracingservice.Trace("Stage   : {0}", plugincontext.Stage);
                }
                tracingservice.Trace("Mode    : {0}", context.Mode);
                tracingservice.Trace("Depth   : {0}", context.Depth);
                tracingservice.Trace("Entity  : {0}", context.PrimaryEntityName);
                if (!context.PrimaryEntityId.Equals(Guid.Empty))
                {
                    tracingservice.Trace("Id      : {0}", context.PrimaryEntityId);
                }
                tracingservice.Trace("");

                tracingservice.TraceAndAlign("InputParameters", context.InputParameters, attributetypes, convertqueries, expandcollections, service);
                tracingservice.TraceAndAlign("OutputParameters", context.OutputParameters, attributetypes, convertqueries, expandcollections, service);
                tracingservice.TraceAndAlign("SharedVariables", context.SharedVariables, attributetypes, convertqueries, expandcollections, service);
                tracingservice.TraceAndAlign("PreEntityImages", context.PreEntityImages, attributetypes, convertqueries, expandcollections, service);
                tracingservice.TraceAndAlign("PostEntityImages", context.PostEntityImages, attributetypes, convertqueries, expandcollections, service);
                tracingservice.Trace("--- Context {0} Trace End ---", depth);
            }
            if (parentcontext)
            {
                if (plugincontext?.ParentContext != null)
                {
                    tracingservice.TraceContext(plugincontext.ParentContext, parentcontext, attributetypes, convertqueries, expandcollections, service, depth + 1);
                }
                else if (plugincontext?.ParentContext != null)
                {
                    tracingservice.TraceContext(plugincontext.ParentContext, parentcontext, attributetypes, convertqueries, expandcollections, service, depth + 1);
                }
            }
            tracingservice.Trace("");
        }

        private static void TraceAndAlign<T>(this ITracingService tracingservice, string topic, IEnumerable<KeyValuePair<string, T>> parametercollection, bool attributetypes, bool convertqueries, bool expandcollections, IOrganizationService service)
        {
            if (parametercollection == null || parametercollection.Count() == 0) { return; }
            tracingservice.Trace(topic);
            var keylen = parametercollection.Max(p => p.Key.Length);
            foreach (var parameter in parametercollection)
            {
                tracingservice.Trace($"  {parameter.Key}{new string(' ', keylen - parameter.Key.Length)} = {ValueToString(parameter.Value, attributetypes, convertqueries, expandcollections, service, 2)}");
            }
        }

        public static string ValueToString(object value, bool attributetypes, bool convertqueries, bool expandcollections, IOrganizationService service, int indent = 1)
        {
            var indentstring = new string(' ', indent * 2);
            if (value == null)
            {
                return $"{indentstring}<null>";
            }
            else if (value is EntityCollection collection)
            {
                var result = $"{collection.EntityName} collection\n  Records: {collection.Entities.Count}\n  TotalRecordCount: {collection.TotalRecordCount}\n  MoreRecords: {collection.MoreRecords}\n  PagingCookie: {collection.PagingCookie}";
                if (expandcollections)
                {
                    result += $"\n{indentstring}  {string.Join($"\n{indentstring}", collection.Entities.Select(e => ValueToString(e, attributetypes, convertqueries, expandcollections, service, indent + 1)))}";
                }
                return result;
            }
            else if (value is Entity entity)
            {
                var keylen = entity.Attributes.Count > 0 ? entity.Attributes.Max(p => p.Key.Length) : 50;
                return $"{entity.LogicalName} {entity.Id}\n{indentstring}" + string.Join($"\n{indentstring}", entity.Attributes.OrderBy(a => a.Key).Select(a => $"{a.Key}{new string(' ', keylen - a.Key.Length)} = {ValueToString(a.Value, attributetypes, convertqueries, expandcollections, service, indent + 1)}"));
            }
            else if (value is ColumnSet columnset)
            {
                var columnlist = new List<string>(columnset.Columns);
                columnlist.Sort();
                return $"\n{indentstring}" + string.Join($"\n{indentstring}", columnlist);
            }
            else if (value is FetchExpression fetchexpression)
            {
                return $"{value}\n{indentstring}{fetchexpression.Query}";
            }
            else if (value is QueryExpression queryexpression && convertqueries && service != null)
            {
                var fetchxml = (service.Execute(new QueryExpressionToFetchXmlRequest { Query = queryexpression }) as QueryExpressionToFetchXmlResponse).FetchXml;
                return $"{queryexpression}\n{indentstring}{fetchxml}";
            }
            else
            {
                var result = string.Empty;
                if (value is EntityReference entityreference)
                {
                    result = $"{entityreference.LogicalName} {entityreference.Id} {entityreference.Name}";
                }
                else if (value is OptionSetValue optionsetvalue)
                {
                    result = optionsetvalue.Value.ToString();
                }
                else if (value is Money money)
                {
                    result = money.Value.ToString();
                }
                else
                {
                    result = value.ToString().Replace("\n", $"\n  {indentstring}");
                }
                return result + (attributetypes ? $" \t({value.GetType()})" : "");
            }
        }

        public static void Write(this ITracingService tracer, string text)
        {
            tracer.Trace(DateTime.Now.ToString("HH:mm:ss.fff  ") + text);
        }
    }
}