/* ***********************************************************
 * CanaryTracer.cs
 * Revision: 2026-11-09
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
 *      maxitemlength,          If the stringifying is long, we might trim it, defaults to the constant MaxItemLength on line 55
 *      sanitize);              If the stringifying should be sanitized, defaults to true
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
 *      maxitemlength,          If the stringifying is long, we might trim it, defaults to the constant MaxItemLength on line 55
 *      sanitize);              If the stringifying should be sanitized, defaults to true
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
    using System.Text.RegularExpressions;

    public static class CanaryTracer
    {
        private const int MaxItemLength = 200;

        /// <summary>
        /// Default settings to trace the context in the easiest way.
        /// </summary>
        /// <param name="serviceprovider">The IServiceProvider sent to IPlugin interface.</param>
        public static void TraceContext(this IServiceProvider serviceprovider) => serviceprovider.TraceContext(false, true, false, false, false, true);

        /// <summary>
        /// Dump everything interested from an IServiceProvider, if it contains Tracer and Context.
        /// </summary>
        /// <param name="serviceprovider">The IServiceProvider sent to IPlugin interface.</param>
        /// <param name="parentcontext">Set to true if any parent contexts shall be traced too.</param>
        /// <param name="attributetypes">Set to true to include information about attribute types.</param>
        /// <param name="convertqueries">Set to true if any QueryExpression queries shall be converted to FetchXML and traced. Requires parameter service to be set.</param>
        /// <param name="expandcollections">Set to true if EntityCollection objects should list all contained Entity objects with all fields available.</param>
        /// <param name="includestage30">Set to true to also include plugins in internal stage.</param>
        /// <param name="sanitize">Set to true to sanitize sensitive information.</param>
        public static void TraceContext(this IServiceProvider serviceprovider, bool parentcontext, bool attributetypes, bool convertqueries, bool expandcollections, bool includestage30, bool sanitize)
        {
            if (serviceprovider == null)
            {
                return;
            }
            var tracer = (ITracingService)serviceprovider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceprovider.GetService(typeof(IPluginExecutionContext));
            tracer.TraceContext(context, parentcontext, attributetypes, convertqueries, expandcollections, includestage30, null, sanitize: sanitize);
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
        /// <param name="sanitize">Set to true to sanitize sensitive information.</param>
        public static void TraceContext(this ITracingService tracingservice, IExecutionContext context, bool parentcontext, bool attributetypes, bool convertqueries, bool expandcollections, bool includestage30 = false, IOrganizationService service = null, int maxitemlength = MaxItemLength, bool sanitize = true)
        {
            if (tracingservice == null)
            {
                return;
            }
            try
            {
                tracingservice.TraceContext(context, parentcontext, attributetypes, convertqueries, expandcollections, includestage30, service, 1, maxitemlength, sanitize);
            }
            catch (Exception ex)
            {
                tracingservice.Trace("--- Exception while trying to TraceContext ---");
                tracingservice.Trace($"Message : {ex.Message}");
            }
        }

        private static void TraceContext(this ITracingService tracingservice, IExecutionContext context, bool parentcontext, bool attributetypes, bool convertqueries, bool expandcollections, bool includestage30, IOrganizationService service, int depth, int maxitemlength, bool sanitize)
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
            var plugincontext6 = context as IPluginExecutionContext6;
            var plugincontext7 = context as IPluginExecutionContext7;
            if (includestage30 || plugincontext?.Stage != 30)
            {
                tracingservice.Trace($"--- Context {depth} Trace Start ---");
                if (!Guid.Empty.Equals(plugincontext6?.TenantId ?? Guid.Empty))
                {
                    tracingservice.Trace($"TenantId     : {plugincontext6.TenantId}");
                }
                if (!string.IsNullOrEmpty(plugincontext6?.EnvironmentId))
                {
                    tracingservice.Trace($"EnvironmentId: {plugincontext6.EnvironmentId}");
                }
                if (plugincontext7 != null)
                {
                    tracingservice.Trace($"IsApplicUser : {plugincontext7.IsApplicationUser}");
                }
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

                tracingservice.TraceAndAlign("InputParameters", context.InputParameters, attributetypes, convertqueries, expandcollections, service, maxitemlength, sanitize);
                tracingservice.TraceAndAlign("OutputParameters", context.OutputParameters, attributetypes, convertqueries, expandcollections, service, maxitemlength, sanitize);
                tracingservice.TraceAndAlign("SharedVariables", context.SharedVariables, attributetypes, convertqueries, expandcollections, service, maxitemlength, sanitize);
                tracingservice.TraceAndAlign("PreEntityImages", context.PreEntityImages, attributetypes, convertqueries, expandcollections, service, maxitemlength, sanitize);
                tracingservice.TraceAndAlign("PostEntityImages", context.PostEntityImages, attributetypes, convertqueries, expandcollections, service, maxitemlength, sanitize);
                if (plugincontext4 != null)
                {
                    if (plugincontext4.PreEntityImagesCollection.Length != 1 || context.PreEntityImages == null)
                    {
                        tracingservice.TraceAndAlign("PreEntityImagesCollection", plugincontext4.PreEntityImagesCollection, attributetypes, convertqueries, expandcollections, service, maxitemlength, sanitize);
                    }
                    if (plugincontext4.PostEntityImagesCollection.Length != 1 || context.PostEntityImages == null)
                    {
                        tracingservice.TraceAndAlign("PostEntityImagesCollection", plugincontext4.PostEntityImagesCollection, attributetypes, convertqueries, expandcollections, service, maxitemlength, sanitize);
                    }
                }
                tracingservice.Trace("--- Context {0} Trace End ---", depth);
            }
            if (parentcontext && plugincontext?.ParentContext != null)
            {
                tracingservice.TraceContext(plugincontext.ParentContext, parentcontext, attributetypes, convertqueries, expandcollections, includestage30, service, depth + 1, maxitemlength, sanitize);
            }
            tracingservice.Trace("");
        }

        private static void TraceAndAlign<T>(this ITracingService tracingservice, string topic, IEnumerable<KeyValuePair<string, T>>[] parametercollection, bool attributetypes, bool convertqueries, bool expandcollections, IOrganizationService service, int maxitemlength, bool sanitize)
        {
            if (tracingservice == null || parametercollection == null || parametercollection.Length == 0)
            {
                return;
            }
            if (parametercollection.Length == 1)
            {
                tracingservice.TraceAndAlign(topic, parametercollection[0], attributetypes, convertqueries, expandcollections, service, maxitemlength, sanitize);
            }
            else
            {
                tracingservice.Trace($"{topic} : {parametercollection.Count()}");
            }
        }

        private static void TraceAndAlign<T>(this ITracingService tracingservice, string topic, IEnumerable<KeyValuePair<string, T>> parametercollection, bool attributetypes, bool convertqueries, bool expandcollections, IOrganizationService service, int maxitemlength, bool sanitize)
        {
            if (tracingservice == null || parametercollection == null || parametercollection.Count() == 0)
            {
                return;
            }
            tracingservice.Trace(topic);
            var keylen = parametercollection.Max(p => p.Key.Length);
            foreach (var parameter in parametercollection)
            {
                tracingservice.Trace($"  {parameter.Key}{new string(' ', keylen - parameter.Key.Length)} = {ObjectToString(parameter.Value, attributetypes, convertqueries, expandcollections, service, 2, maxitemlength, sanitize)}");
            }
        }

        private static string GetLastType(this object value) => value?.GetType()?.ToString()?.Split('.')?.Last() ?? "";

        public static string ObjectToString(this object value) => ObjectToString(value, true, false, false);

        public static string ObjectToString(object value, bool attributetypes, bool convertqueries, bool expandcollections, IOrganizationService service = null, int indent = 1, int maxitemlength = MaxItemLength, bool sanitize = true)
        {
            var result = string.Empty;
            try
            {
                var indentstring = new string(' ', indent * 2);
                if (value == null)
                {
                    result = $"{indentstring}<null>";
                }
                else if (value is EntityCollection collection)
                {
                    result = $"{collection.Entities.Count} {collection.EntityName}(s)" + (attributetypes ? $" \t({value.GetLastType()})" : "");
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
                }
                else if (value is IEnumerable<Entity> entities)
                {
                    result = expandcollections ? $"{indentstring}{string.Join($"\n{indentstring}", entities.Select(e => ObjectToString(e, attributetypes, convertqueries, expandcollections, service, indent + 1, maxitemlength)))}" : string.Empty;
                }
                else if (value is Entity entity)
                {
                    var keylen = entity.Attributes.Count > 0 ? entity.Attributes.Max(p => p.Key.Length) : 50;
                    result = $"{entity.LogicalName} {entity.Id}\n{indentstring}" +
                        string.Join($"\n{indentstring}",
                            entity.Attributes
                                .OrderBy(a => a.Key)
                                .Select(a => $"{a.Key}{new string(' ', keylen - a.Key.Length)} = {ObjectToString(a.Value, attributetypes, convertqueries, expandcollections, service, indent + 1, maxitemlength)}"));
                }
                else if (value is EntityReferenceCollection entrefcol)
                {
                    result = $"\n{indentstring}{string.Join($"\n{indentstring}", entrefcol.Select(e => ObjectToString(e, attributetypes, convertqueries, expandcollections, service, indent + 1, maxitemlength)))}";
                }
                else if (value is ColumnSet columnset)
                {
                    var columnlist = new List<string>(columnset.Columns);
                    columnlist.Sort();
                    result = $"\n{indentstring}" + string.Join($"\n{indentstring}", columnlist);
                }
                else if (value is FetchExpression fetchexpression)
                {
                    result = $"{value}\n{indentstring}{fetchexpression.Query}";
                }
                else if (value is QueryExpression queryexpression && convertqueries && service != null)
                {
                    var fetchxml = (service.Execute(new QueryExpressionToFetchXmlRequest { Query = queryexpression }) as QueryExpressionToFetchXmlResponse).FetchXml;
                    result = $"{queryexpression}\n{indentstring}{fetchxml}";
                }
                else
                {
                    result = string.Empty;
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
                    result = result + (attributetypes ? $" \t({value.GetLastType()})" : "");
                }
            }
            catch (Exception ex)
            {
                result = $"*** Cannot stringify value:{value}\n*** {ex.Message}";
            }
            return sanitize ? result.Sanitize() : result;
        }

        [Obsolete("Please use method ObjectToString instead - same features, and more!")]
        public static string ValueToString(object value, bool attributetypes, bool convertqueries, bool expandcollections, IOrganizationService service, int indent = 1, int maxitemlength = MaxItemLength) => ObjectToString(value, attributetypes, convertqueries, expandcollections, service, indent, maxitemlength, true);

        public static void Write(this ITracingService tracer, string text, bool sanitize = true)
        {
            if (tracer == null)
            {
                return;
            }
            tracer.Trace(DateTime.Now.ToString("HH:mm:ss.fff  ") + (sanitize ? text.Sanitize() : text));
        }

        public static void TraceError(this IServiceProvider serviceprovider, Exception exception)
        {
            if (serviceprovider == null || exception == null)
            {
                return;
            }
            var tracer = serviceprovider.GetService(typeof(ITracingService)) as ITracingService;
            tracer?.Write(exception.ToString());
        }

        #region Sanitize sensitive data

        private static readonly Regex SensitiveIdRegex = new Regex(
            // Sweden: YYMMDD-NNNN / YYYYMMDD-NNNN, dash optional
            @"(?<SE>(?<!\d)(?:\d{6}|\d{8})-?\d{4}(?!\d))" +

            // USA: XXX-XX-XXXX
            @"|(?<US>(?<!\d)\d{3}-\d{2}-\d{4}(?!\d))" +

            // UK National Insurance Number
            @"|(?<UK>(?<![A-Z0-9])" +
                @"(?!(?:BG|GB|KN|NK|NT|TN|ZZ))" +
                @"[A-CEGHJ-PR-TW-Z][A-CEGHJ-NPR-TW-Z]" +
                @"\s?\d{2}\s?\d{2}\s?\d{2}\s?[A-D]" +
                @"(?![A-Z0-9]))" +

            // Spain: NIF 12345678Z or NIE X/Y/Z1234567Z
            @"|(?<ES>(?<![A-Z0-9])(?:\d{8}|[XYZ]\d{7})[A-Z](?![A-Z0-9]))" +

            // Italy: Codice Fiscale, 16 alphanumeric characters
            @"|(?<IT>(?<![A-Z0-9])" +
                @"[A-Z]{6}\d{2}[A-EHLMPR-T]\d{2}[A-Z]\d{3}[A-Z]" +
                @"(?![A-Z0-9]))" +

            // France: NIR, 13 digits plus 2-digit control key.
            // Spaces are required to reduce false positives.
            @"|(?<FR>(?<!\d)[12]\s\d{2}\s\d{2}\s\d{2}\s\d{3}\s\d{3}\s\d{2}(?!\d))" +

            // Germany: only mask when explicitly labelled.
            @"|(?<DE>Steuer(?:identifikationsnummer|-ID| ID)\s*[:=]?\s*\d{11})",

            RegexOptions.Compiled |
            RegexOptions.CultureInvariant |
            RegexOptions.IgnoreCase,

            TimeSpan.FromMilliseconds(100));

        public static string Sanitize(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            return SensitiveIdRegex.Replace(text, match =>
            {
                var value = match.Value;

                if (match.Groups["SE"].Success)
                {
                    var digitCount = value.Length;

                    if (value.IndexOf('-') >= 0)
                    {
                        digitCount--;
                    }

                    // Ten digits starting with zero are treated as phone numbers.
                    if (digitCount == 10 && value[0] == '0')
                    {
                        return value;
                    }

                    return value.Substring(0, value.Length - 4) + "****";
                }

                if (match.Groups["US"].Success)
                {
                    return value.Substring(0, value.Length - 4) + "****";
                }

                // For other formats, mask every digit but preserve letters,
                // spaces, dashes and labels.
                var characters = value.ToCharArray();

                for (var index = 0; index < characters.Length; index++)
                {
                    if (characters[index] >= '0' && characters[index] <= '9')
                    {
                        characters[index] = '*';
                    }
                }

                return new string(characters);
            });
        }

        #endregion Sanitize sensitive data
    }
}