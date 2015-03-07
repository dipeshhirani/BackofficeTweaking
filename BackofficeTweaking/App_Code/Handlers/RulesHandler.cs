﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Web.Http;
using System.Web.Http.Dispatcher;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Web;
using Umbraco.Web.Models.ContentEditing;

using BackofficeTweaking.Helpers;
using BackofficeTweaking.Models;
using BackofficeTweaking.Extensions;

namespace BackofficeTweaking.Handlers
{

    public class RulesHandler : DelegatingHandler
    {

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Task<HttpResponseMessage> result = null;
            try
            {
                switch (request.RequestUri.AbsolutePath.ToLower())
                {
                    case "/umbraco/backoffice/umbracoapi/content/getempty":
                    case "/umbraco/backoffice/umbracoapi/content/getbyid":
                        // Get rules for the current user
                        var user = UmbracoContext.Current.Application.Services.UserService.GetUserById(UmbracoContext.Current.Security.GetUserId());
                        IEnumerable<Rule> rules = ConfigFileHelper.getRulesForUser(user);
                        // Process rules
                        result = ProcessRules(request, cancellationToken, rules);
                        break;
                    default:
                        result = base.SendAsync(request, cancellationToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(typeof(RulesHandler), "Error handling the request.", ex);
            }

            return result;

        }

        private Task<HttpResponseMessage> ProcessRules(HttpRequestMessage request, CancellationToken cancellationToken, IEnumerable<Rule> rules)
        {
            return base.SendAsync(request, cancellationToken)
                    .ContinueWith(task =>
                    {
                        var response = task.Result;

                        try
                        {
                            List<string> hideProperties = new List<string>();
                            List<string> hideTabs = new List<string>();
                            List<string> hideButtons = new List<string>();
                            List<string> hidePanels = new List<string>();

                            var data = response.Content;
                            var content = ((ObjectContent)(data)).Value as ContentItemDisplay;

                            foreach (var property in rules.Where(x =>
                                x.Enabled == true
                                && x.Type == RuleType.HideProperties.ToString()
                                && !string.IsNullOrWhiteSpace(x.Names)
                                && (string.IsNullOrWhiteSpace(x.ContentTypes) || x.ContentTypes.ToDelimitedList().Contains(content.ContentTypeAlias))
                                ))
                            {
                                hideProperties.AddRangeUnique(property.Names.ToDelimitedList().ToList());
                            }

                            foreach (var tab in rules.Where(x =>
                                x.Enabled == true
                                && x.Type == RuleType.HideTabs.ToString()
                                && !string.IsNullOrWhiteSpace(x.Names)
                                && (string.IsNullOrWhiteSpace(x.ContentTypes) || x.ContentTypes.ToDelimitedList().Contains(content.ContentTypeAlias))
                                ))
                            {
                                hideTabs.AddRangeUnique(tab.Names.ToDelimitedList().ToList());
                            }

                            foreach (var button in rules.Where(x =>
                                x.Enabled == true
                                && x.Type == RuleType.HideButtons.ToString()
                                && !string.IsNullOrWhiteSpace(x.Names)
                                && (string.IsNullOrWhiteSpace(x.ContentTypes) || x.ContentTypes.ToDelimitedList().Contains(content.ContentTypeAlias))
                                ))
                            {
                                hideButtons.AddRangeUnique(button.Names.ToDelimitedList().ToList());
                            }

                            foreach (var panel in rules.Where(x =>
                                x.Enabled == true
                                && x.Type == RuleType.HidePanels.ToString()
                                && !string.IsNullOrWhiteSpace(x.Names)
                                && (string.IsNullOrWhiteSpace(x.ContentTypes) || x.ContentTypes.ToDelimitedList().Contains(content.ContentTypeAlias))
                                ))
                            {
                                hidePanels.AddRangeUnique(panel.Names.ToDelimitedList().ToList());
                            }

                            // Get the first property of the first visible tab
                            var firstPropertyAlias = content.Tabs.Where(t => t.IsActive == true).FirstOrDefault().Properties.FirstOrDefault().Alias;

                            var tabs = content.Tabs.Where(x => hideTabs.Contains(x.Alias));
                            var properties = content.Properties.Where(x => hideProperties.Contains(x.Alias));

                            content.Properties.Where(p => p.Alias == firstPropertyAlias).ForEach(x =>
                            {
                                x.Config.Add("hidetabs", string.Join(",", tabs.Select(t => t.Label)));
                            });

                            content.Properties.Where(p => p.Alias == firstPropertyAlias).ForEach(x =>
                            {
                                x.Config.Add("hidebuttons", string.Join(",", hideButtons.Select(t => t)));
                            });

                            content.Properties.Where(p => p.Alias == firstPropertyAlias).ForEach(x =>
                            {
                                x.Config.Add("hidepanels", string.Join(",", hidePanels.Select(t => t)));
                            });

                            properties.ForEach(x =>
                            {
                                x.Config.Add("hide", true);
                            });

                            tabs.ForEach(x =>
                            {
                                x.Properties.ForEach(p =>
                                {
                                    p.Config.Add("hidetab", true);
                                    p.Config.Add("tablabel", x.Label);
                                });
                            });

                        }
                        catch (Exception ex)
                        {
                            LogHelper.Error(typeof(RulesHandler), "Error processing rules.", ex);
                        }

                        return response;
                    });
        }
    }
}