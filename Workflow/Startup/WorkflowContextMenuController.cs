﻿using Umbraco.Core;
using Umbraco.Web;
using Workflow;

namespace UmbracoWorkflow.Actions
{
    public class WorkflowContextMenuController : ApplicationEventHandler
    {
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            Umbraco.Web.Trees.TreeControllerBase.MenuRendering += ContentTreeController_MenuRendering;
        }

        void ContentTreeController_MenuRendering(Umbraco.Web.Trees.TreeControllerBase sender, Umbraco.Web.Trees.MenuRenderingEventArgs e)
        {
            if (sender.TreeAlias == "content" && string.Compare(e.NodeId, "-1") != 0)
            {
                var menuLength = e.Menu.Items.Count;
                var nodeName = Helpers.GetNode(int.Parse(e.NodeId)).Name;
                var inProcess = Helpers.GetNodeStatus(int.Parse(e.NodeId));
                var currentUser = UmbracoContext.Current.Security.CurrentUser.UserType;
                var items = new Umbraco.Web.Models.Trees.MenuItemList();

                var i = new Umbraco.Web.Models.Trees.MenuItem("workflowHistory", "Workflow history");
                i.LaunchDialogView("/App_Plugins/Workflow/Backoffice/dialogs/workflow.history.dialog.html", "Workflow history: " + nodeName);
                i.SeperatorBefore = true;
                i.Icon = "directions-alt";

                items.Add(i);

                //if (!inProcess)
                //{
                //    i = new Umbraco.Web.Models.Trees.MenuItem("sendForPublish", "Send for publish");
                //    i.LaunchDialogView("/App_Plugins/Workflow/Backoffice/dialogs/workflow.submit.dialog.html", "Send for publish approval: " + nodeName);
                //    i.AdditionalData.Add("isPublish", true);
                //    i.Icon = "check";

                //    items.Add(i);

                //    i = new Umbraco.Web.Models.Trees.MenuItem("sendForUnpublish", "Send for unpublish");
                //    i.LaunchDialogView("/App_Plugins/Workflow/Backoffice/dialogs/workflow.submit.dialog.html", "Send for unpublish approval: " + nodeName);
                //    i.AdditionalData.Add("isPublish", false);
                //    i.Icon = "delete";

                //    items.Add(i);
                //}

                if (currentUser.Alias == "admin")
                {
                    i = new Umbraco.Web.Models.Trees.MenuItem("workflowConfig", "Workflow configuration");
                    i.LaunchDialogView("/App_Plugins/Workflow/Backoffice/dialogs/workflow.config.dialog.html", "Workflow configuration: " + nodeName);
                    i.Icon = "path";

                    items.Add(i);
                }

                if (menuLength < 5)
                {
                    e.Menu.Items.AddRange(items);
                } else
                {
                    e.Menu.Items.InsertRange(5, items);
                }
            }
        }
    }    
}