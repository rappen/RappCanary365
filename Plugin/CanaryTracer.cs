/* ***********************************************************
 * CanaryTracer.cs
 * Revision: 2025-01-29
 * Code found at: https://jonasr.app/canary-code
 * Background: https://jonasr.app/canary/
 * Created by: Jonas Rapp https://jonasr.app/
 *
 * Writes everything from an IExecutionContext to the Plugin Trace Log.
 *
 * Simplest call:
 *    serviceProvider.TraceContext();
 *
 * Simple sample call:
 *    tracingservice.TraceContext(context);
 *
 * Advanced sample call:
 *    tracingservice.TraceContext(
 *      context,                This is the Context to trace
 *      parentcontext,          If this conext has parent context, it will repeat upwards until no parent
 *      attributetypes,         Include the type of the value, e.g. "Mr Smith (string)"
 *      convertqueries,         If the value is a QueryExpression, convert to FetchXML
 *      expandcollections,      If the value is a collection of Entity, stringify all items too
 *      includestage30,         Stage 30 is internal in the platform, set to true to include
 *      service,                This is needed if convertqueries is true
 *      maxitemlength);         If the stringifying is long, we might trim it, defaults to the constant MaxItemLength on line 53
 *
 * Simplest way to get anything into text:
 *    MyAnyObject.ObjectToString();      The object type should probably be any type in Dataverse
 *
 * Getting anything into text:
 *    CanaryTracer.ObjectToString(
 *      value,                  Any type to stringify, it's extra smart for most Dataverse columns types
 *      attributetypes,         Include the type of the value, e.g. "Mr Smith (string)"
 *      convertqueries,         If the value is a QueryExpression, convert to FetchXML
 *      expandcollections,      If the value is a collection of Entity, stringify all items too
 *      service,                This is needed if convertqueries is true
 *      indent,                 Indent length, defaults to 1
 *      maxitemlength);         If the stringifying is long, we might trim it, defaults to the constant MaxItemLength on line 53
 *
 *               Enjoy responsibly.
 * **********************************************************/

namespace Rappen.Dataverse.Canary
{
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class CanaryTracer
    {
        private const int MaxItemLength = 200;

        /// <summary>
        /// Default settings to trace the context in the easiest way.
        /// </summary>
        /// <param name="serviceprovider">The IServiceProvider sent to IPlugin interface.</param>
        public static void TraceContext(this IServiceProvider serviceprovider) => serviceprovider.TraceContext(false, true, false, false, false);

        /// <summary>
        /// Dump everything interested from an IServiceProvider, if it contains Tracer and Context.
        /// </summary>
        /// <param name="serviceprovider">The IServiceProvider sent to IPlugin interface.</param>
        /// <param name="parentcontext">Set to true if any parent contexts shall be traced too.</param>
        /// <param name="attributetypes">Set to true to include information about attribute types.</param>
        /// <param name="convertqueries">Set to true if any QueryExpression queries shall be converted to FetchXML and traced. Requires parameter service to be set.</param>
        /// <param name="expandcollections">Set to true if EntityCollection objects should list all contained Entity objects with all fields available.</param>
        /// <param name="includestage30">Set to true to also include plugins in internal stage.</param>
        public static void TraceContext(this IServiceProvider serviceprovider, bool parentcontext, bool attributetypes, bool convertqueries, bool expandcollections, bool includestage30)
        {
            var tracer = (ITracingService)serviceprovider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceprovider.GetService(typeof(IPluginExecutionContext));
            tracer.TraceContext(context, parentcontext, attributetypes, convertqueries, expandcollections, includestage30, null);
        }

        /// <summary>
        /// Default settings for the TraceContext
        /// </summary>
        /// <param name="tracingservice">The tracer to trace the trace.</param>
        /// <param name="context">The plugin or workflow context to trace.</param>
        public static void TraceContext(this ITracingService tracingservice, IExecutionContext context) => tracingservice.TraceContext(context, false, true, false, false, false);

        /// <summary>
        /// Dumps everything interesting from the plugin context to the plugin trace log
        /// </summary>
        /// <param name="tracingservice">The tracer to trace the trace.</param>
        /// <param name="context">The plugin or workflow context to trace.</param>
        /// <param name="parentcontext">Set to true if any parent contexts shall be traced too.</param>
        /// <param name="attributetypes">Set to true to include information about attribute types.</param>
        /// <param name="convertqueries">Set to true if any QueryExpression queries shall be converted to FetchXML and traced. Requires parameter service to be set.</param>
        /// <param name="expandcollections">Set to true if EntityCollection objects should list all contained Entity objects with all fields available.</param>
        /// <param name="includestage30">Set to true to also include plugins in internal stage.</param>
        /// <param name="service">Service used if convertqueries is true, may be null if not used.</param>
        public static void TraceContext(this ITracingService tracingservice, IExecutionContext context, bool parentcontext, bool attributetypes, bool convertqueries, bool expandcollections, bool includestage30 = false, IOrganizationService service = null, int maxitemlength = MaxItemLength)
        {
            try
            {
                tracingservice.TraceContext(context, parentcontext, attributetypes, convertqueries, expandcollections, includestage30, service, 1, maxitemlength);
            }
            catch (Exception ex)
            {
                tracingservice.Trace("--- Exception while trying to TraceContext ---");
                tracingservice.Trace($"Message : {ex.Message}");
            }
        }

        private static void TraceContext(this ITracingService tracingservice, IExecutionContext context, bool parentcontext, bool attributetypes, bool convertqueries, bool expandcollections, bool includestage30, IOrganizationService service, int depth, int maxitemlength)
        {
            if (tracingservice == null)
            {
                return;
            }
            if (context == null)
            {
                tracingservice.Trace("No Context available.");
                return;
            }
            var plugincontext = context as IPluginExecutionContext;
            var plugincontext2 = context as IPluginExecutionContext2;
            var plugincontext3 = context as IPluginExecutionContext3;
            var plugincontext4 = context as IPluginExecutionContext4;
            var plugincontext5 = context as IPluginExecutionContext5;
            if (includestage30 || plugincontext?.Stage != 30)
            {
                tracingservice.Trace($"--- Context {depth} Trace Start ---");
                if (!string.IsNullOrEmpty(plugincontext5?.InitiatingUserAgent))
                {
                    tracingservice.Trace($"InitUserAgent: {plugincontext5.InitiatingUserAgent}");
                }
                tracingservice.Trace($"UserId       : {context.UserId}");
                if (!context.UserId.Equals(context.InitiatingUserId))
                {
                    tracingservice.Trace($"InitUserId   : {context.InitiatingUserId}");
                }
                if (plugincontext3 != null)
                {
                    if (!plugincontext3.AuthenticatedUserId.Equals(Guid.Empty) &&
                        !plugincontext3.AuthenticatedUserId.Equals(context.UserId) &&
                        !plugincontext3.AuthenticatedUserId.Equals(context.InitiatingUserId))
                    {
                        tracingservice.Trace($"AuthUserId   : {plugincontext3.AuthenticatedUserId}");
                    }
                }
                if (plugincontext2 != null)
                {
                    if (!plugincontext2.UserAzureActiveDirectoryObjectId.Equals(Guid.Empty))
                    {
                        tracingservice.Trace($"UserAzureADId: {plugincontext2.UserAzureActiveDirectoryObjectId}");
                    }
                    if (!plugincontext2.InitiatingUserAzureActiveDirectoryObjectId.Equals(Guid.Empty) && !plugincontext2.InitiatingUserAzureActiveDirectoryObjectId.Equals(plugincontext2.UserAzureActiveDirectoryObjectId))
                    {
                        tracingservice.Trace($"InitAzADUser : {plugincontext2.InitiatingUserAzureActiveDirectoryObjectId}");
                    }
                    if (!plugincontext2.InitiatingUserApplicationId.Equals(Guid.Empty))
                    {
                        tracingservice.Trace($"InitUserAppId: {plugincontext2.InitiatingUserApplicationId}");
                    }
                    if (plugincontext2.IsPortalsClientCall)
                    {
                        tracingservice.Trace($"IsPortalsCall: {plugincontext2.IsPortalsClientCall}");
                    }
                    if (!plugincontext2.PortalsContactId.Equals(Guid.Empty))
                    {
                        tracingservice.Trace($"PortalContact: {plugincontext2.PortalsContactId}");
                    }
                }
                if (context.OwningExtension != null)
                {
                    tracingservice.Trace($"{(context.OwningExtension.LogicalName == "sdkmessageprocessingstep" ? "Step    " : context.OwningExtension.LogicalName)}: {context.OwningExtension.Id} {context.OwningExtension.Name}");
                }
                tracingservice.Trace($"Message : {context.MessageName}");
                if (plugincontext != null)
                {
                    tracingservice.Trace($"Stage   : {plugincontext.Stage}");
                }
                tracingservice.Trace($"Mode    : {context.Mode}");
                tracingservice.Trace($"Depth   : {context.Depth}");
                tracingservice.Trace($"Entity  : {context.PrimaryEntityName}");
                if (!context.PrimaryEntityId.Equals(Guid.Empty))
                {
                    tracingservice.Trace($"Id      : {context.PrimaryEntityId}");
                }
                tracingservice.Trace("");

                tracingservice.TraceAndAlign("InputParameters", context.InputParameters, attributetypes, convertqueries, expandcollections, service, maxitemlength);
                tracingservice.TraceAndAlign("OutputParameters", context.OutputParameters, attributetypes, convertqueries, expandcollections, service, maxitemlength);
                tracingservice.TraceAndAlign("SharedVariables", context.SharedVariables, attributetypes, convertqueries, expandcollections, service, maxitemlength);
                tracingservice.TraceAndAlign("PreEntityImages", context.PreEntityImages, attributetypes, convertqueries, expandcollections, service, maxitemlength);
                tracingservice.TraceAndAlign("PostEntityImages", context.PostEntityImages, attributetypes, convertqueries, expandcollections, service, maxitemlength);
                if (plugincontext4 != null)
                {
                    if (plugincontext4.PreEntityImagesCollection.Length != 1 || context.PreEntityImages == null)
                    {
                        tracingservice.TraceAndAlign("PreEntityImagesCollection", plugincontext4.PreEntityImagesCollection, attributetypes, convertqueries, expandcollections, service, maxitemlength);
                    }
                    if (plugincontext4.PostEntityImagesCollection.Length != 1 || context.PostEntityImages == null)
                    {
                        tracingservice.TraceAndAlign("PostEntityImagesCollection", plugincontext4.PostEntityImagesCollection, attributetypes, convertqueries, expandcollections, service, maxitemlength);
                    }
                }
                tracingservice.Trace("--- Context {0} Trace End ---", depth);
            }
            if (parentcontext && plugincontext?.ParentContext != null)
            {
                tracingservice.TraceContext(plugincontext.ParentContext, parentcontext, attributetypes, convertqueries, expandcollections, includestage30, service, depth + 1, maxitemlength);
            }
            tracingservice.Trace("");
        }

        private static void TraceAndAlign<T>(this ITracingService tracingservice, string topic, IEnumerable<KeyValuePair<string, T>>[] parametercollection, bool attributetypes, bool convertqueries, bool expandcollections, IOrganizationService service, int maxitemlength)
        {
            if (parametercollection == null || parametercollection.Length == 0)
            {
                return;
            }
            if (parametercollection.Length == 1)
            {
                tracingservice.TraceAndAlign(topic, parametercollection[0], attributetypes, convertqueries, expandcollections, service, maxitemlength);
            }
            else
            {
                tracingservice.Trace($"{topic} : {parametercollection.Count()}");
            }
        }

        private static void TraceAndAlign<T>(this ITracingService tracingservice, string topic, IEnumerable<KeyValuePair<string, T>> parametercollection, bool attributetypes, bool convertqueries, bool expandcollections, IOrganizationService service, int maxitemlength)
        {
            if (parametercollection == null || parametercollection.Count() == 0) { return; }
            tracingservice.Trace(topic);
            var keylen = parametercollection.Max(p => p.Key.Length);
            foreach (var parameter in parametercollection)
            {
                tracingservice.Trace($"  {parameter.Key}{new string(' ', keylen - parameter.Key.Length)} = {ObjectToString(parameter.Value, attributetypes, convertqueries, expandcollections, service, 2, maxitemlength)}");
            }
        }

        private static string GetLastType(this object value) => value?.GetType()?.ToString()?.Split('.')?.Last() ?? "";

        public static string ObjectToString(this object value) => ObjectToString(value, true, false, false);

        public static string ObjectToString(object value, bool attributetypes, bool convertqueries, bool expandcollections, IOrganizationService service = null, int indent = 1, int maxitemlength = MaxItemLength)
        {
            try
            {
                var indentstring = new string(' ', indent * 2);
                if (value == null)
                {
                    return $"{indentstring}<null>";
                }
                else if (value is EntityCollection collection)
                {
                    var result = $"{collection.Entities.Count} {collection.EntityName}(s)" + (attributetypes ? $" \t({value.GetLastType()})" : "");
                    if (collection.TotalRecordCount > 0)
                    {
                        result += $"\n  TotalRecordCount: {collection.TotalRecordCount}";
                    }
                    if (collection.MoreRecords)
                    {
                        result += $"\n  MoreRecords: {collection.MoreRecords}";
                    }
                    if (!string.IsNullOrWhiteSpace(collection.PagingCookie))
                    {
                        result += $"\n  PagingCookie: {collection.PagingCookie}";
                    }
                    if (expandcollections && collection.Entities.Count > 0)
                    {
                        result += "\n" + ObjectToString(collection.Entities, attributetypes, convertqueries, expandcollections, service, indent, maxitemlength);
                    }
                    if (!expandcollections && collection.Entities.Count == 1)
                    {
                        result += $"\n{indentstring}" + ObjectToString(collection.Entities[0], attributetypes, convertqueries, expandcollections, service, indent + 1, maxitemlength);
                    }
                    return result;
                }
                else if (value is IEnumerable<Entity> entities)
                {
                    return expandcollections ? $"{indentstring}{string.Join($"\n{indentstring}", entities.Select(e => ObjectToString(e, attributetypes, convertqueries, expandcollections, service, indent + 1, maxitemlength)))}" : string.Empty;
                }
                else if (value is Entity entity)
                {
                    var keylen = entity.Attributes.Count > 0 ? entity.Attributes.Max(p => p.Key.Length) : 50;
                    return $"{entity.LogicalName} {entity.Id}\n{indentstring}" +
                        string.Join($"\n{indentstring}",
                            entity.Attributes
                                .OrderBy(a => a.Key)
                                .Select(a => $"{a.Key}{new string(' ', keylen - a.Key.Length)} = {ObjectToString(a.Value, attributetypes, convertqueries, expandcollections, service, indent + 1, maxitemlength)}"));
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
                    else if (value is OptionSetValueCollection optcoll)
                    {
                        result = string.Join(";", optcoll.Select(o => o.Value));
                    }
                    else if (value is Money money)
                    {
                        result = money.Value.ToString();
                    }
                    else if (value is AliasedValue alias)
                    {
                        result = ObjectToString(alias.Value, attributetypes, convertqueries, expandcollections, service, indent, maxitemlength);
                    }
                    else
                    {
                        result = value.ToString().Replace("\n", $"\n  {indentstring}");
                        if (result.Length > maxitemlength)
                        {
                            result = result.Substring(0, maxitemlength) + $"... ({result.Length})";
                        }
                    }
                    return result + (attributetypes ? $" \t({value.GetLastType()})" : "");
                }
            }
            catch (Exception ex)
            {
                return $"*** Cannot stringify value:{value}\n*** {ex.Message}";
            }
        }

        [Obsolete("Please use method ObjectToString instead - same features, and more!")]
        public static string ValueToString(object value, bool attributetypes, bool convertqueries, bool expandcollections, IOrganizationService service, int indent = 1, int maxitemlength = MaxItemLength) => ObjectToString(value, attributetypes, convertqueries, expandcollections, service, indent, maxitemlength);

        public static void Write(this ITracingService tracer, string text)
        {
            tracer.Trace(DateTime.Now.ToString("HH:mm:ss.fff  ") + text);
        }

        public static void TraceError(this IServiceProvider serviceprovider, Exception exception)
        {
            var tracer = serviceprovider.GetService(typeof(ITracingService)) as ITracingService;
            tracer?.Write(exception.ToString());
        }
    }
}