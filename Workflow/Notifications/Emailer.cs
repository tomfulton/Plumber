﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using log4net;
using Umbraco.Core.Models;
using Workflow.Extensions;
using Workflow.Helpers;
using Workflow.Models;
using Workflow.Services;
using Workflow.Services.Interfaces;

namespace Workflow.Notifications
{
    public class Emailer
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ISettingsService _settingsService;
        private readonly ITasksService _tasksService;
        private readonly IGroupService _groupService;

        private readonly Utility _utility;

        private const string EmailApprovalRequestString = "Dear {0},<br/><br/>Please review the following page for {5} approval: <a href=\"{1}\">{2}</a><br/><br/>Comment: {3}<br/><br/>{6}Thanks,<br/>{4}";
        private const string EmailApprovedString = "Dear {0},<br/><br/>The following document's workflow has been approved and the document {3}: <a href=\"{1}\">{2}</a><br/>";
        private const string EmailRejectedString = "Dear {0},<br/><br/>The {5} workflow was rejected by {4}: <a href=\"{1}\">{2}</a><br/><br/>Comment: {3}";
        private const string EmailCancelledString = "Dear {0},<br/><br/>{1} workflow has been cancelled for the following page: <a href=\"{2}\">{3}</a> by {4}.<br/><br/>Reason: {5}.";

        private const string EmailOfflineApprovalString = "<a href=\"{0}/workflow-preview/{1}/{2}/{3}/{4}\">Offline approval</a> is permitted for this change (no login required).<br/><br/>";

        private const string EmailBody = "<!DOCTYPE HTML SYSTEM><html><head><title>{0}</title></head><body><font face=\"verdana\" size=\"2\">{1}</font></body></html>";


        public Emailer()
        {
            _settingsService = new SettingsService();
            _tasksService = new TasksService();
            _groupService = new GroupService();

            _utility = new Utility();
        }

        /// <summary>
        /// Sends an email notification out for the workflow process
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="emailType">the type of email to be sent</param>
        public async void Send(WorkflowInstancePoco instance, EmailType emailType)
        {
            WorkflowSettingsPoco settings = _settingsService.GetSettings();

            if (!settings.SendNotifications) return;

            if (!instance.TaskInstances.Any())
            {
                instance.TaskInstances = _tasksService.GetTasksWithGroupByInstanceGuid(instance.Guid);
            }

            if (!instance.TaskInstances.Any())
            {
                Log.Error($"Notifications not sent - no tasks exist for instance { instance.Id }");
                return;
            }

            WorkflowTaskPoco finalTask = null;

            try
            {
                string docTitle = instance.Node.Name;
                string docUrl = UrlHelpers.GetFullyQualifiedContentEditorUrl(instance.NodeId);

                WorkflowTaskPoco[] flowTasks = instance.TaskInstances.OrderBy(t => t.ApprovalStep).ToArray();

                // always take get the emails for all previous users, sometimes they will be discarded later
                // easier to just grab em all, rather than doing so conditionally
                List<string> emailsForAllTaskUsers = new List<string>();

                // in the loop, also store the last task to a variable, and keep the populated group
                var taskIndex = 0;
                int taskCount = flowTasks.Length;

                foreach (WorkflowTaskPoco task in flowTasks)
                {
                    taskIndex += 1;

                    UserGroupPoco group = await _groupService.GetPopulatedUserGroupAsync(task.GroupId);
                    if (group == null) continue;

                    emailsForAllTaskUsers.AddRange(group.PreferredEmailAddresses());
                    if (taskIndex != taskCount) continue;

                    finalTask = task;
                    finalTask.UserGroup = group;
                }

                if (finalTask == null)
                {
                    Log.Error("No valid task found for email notifications");
                    return;
                }

                List<string> to = new List<string>();

                var body = "";
                string typeDescription = instance.WorkflowType.Description(instance.ScheduledDate);
                string typeDescriptionPast = instance.WorkflowType.DescriptionPastTense(instance.ScheduledDate);

                switch (emailType)
                {
                    case EmailType.ApprovalRequest:
                        to = finalTask.UserGroup.PreferredEmailAddresses();
                        body = string.Format(EmailApprovalRequestString,
                            to.Count > 1 ? "Umbraco user" : finalTask.UserGroup.Name, docUrl, docTitle, instance.AuthorComment,
                            instance.AuthorUser.Name, typeDescription, string.Empty);
                        break;

                    case EmailType.ApprovalRejection:
                        to = emailsForAllTaskUsers;
                        to.Add(instance.AuthorUser.Email);
                        body = string.Format(EmailRejectedString,
                            "Umbraco user", docUrl, docTitle, finalTask.Comment,
                            finalTask.ActionedByUser.Name, typeDescription.ToLower());

                        break;

                    case EmailType.ApprovedAndCompleted:
                        to = emailsForAllTaskUsers;
                        to.Add(instance.AuthorUser.Email);

                        //Notify web admins
                        to.Add(settings.Email);

                        if (instance.WorkflowType == WorkflowType.Publish)
                        {
                            IPublishedContent n = _utility.GetPublishedContent(instance.NodeId);
                            docUrl = UrlHelpers.GetFullyQualifiedSiteUrl(n.Url);
                        }

                        body = string.Format(EmailApprovedString,
                                   "Umbraco user", docUrl, docTitle,
                                   typeDescriptionPast.ToLower()) + "<br/>";

                        body += instance.BuildProcessSummary();

                        break;

                    case EmailType.ApprovedAndCompletedForScheduler:
                        to = emailsForAllTaskUsers;
                        to.Add(instance.AuthorUser.Email);

                        body = string.Format(EmailApprovedString,
                                   "Umbraco user", docUrl, docTitle,
                                   typeDescriptionPast.ToLower()) + "<br/>";

                        body += instance.BuildProcessSummary();

                        break;

                    case EmailType.WorkflowCancelled:
                        to = emailsForAllTaskUsers;

                        // include the initiator email
                        to.Add(instance.AuthorUser.Email);

                        body = string.Format(EmailCancelledString,
                            "Umbraco user", typeDescription, docUrl, docTitle, finalTask.ActionedByUser.Name, finalTask.Comment);
                        break;
                    case EmailType.SchedulerActionCancelled:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(emailType), emailType, null);
                }

                if (!to.Any()) return;

                var client = new SmtpClient();
                var msg = new MailMessage
                {
                    Subject = $"{emailType.ToString().ToTitleCase()} - {instance.Node.Name} ({typeDescription})",
                    IsBodyHtml = true,
                };

                if (settings.Email.HasValue())
                {
                    msg.From = new MailAddress(settings.Email);
                }

                // if offline is permitted, email group members individually as we need the user id in the url
                if (emailType == EmailType.ApprovalRequest && finalTask.UserGroup.OfflineApproval)
                {
                    foreach (User2UserGroupPoco user in finalTask.UserGroup.Users)
                    {
                        string offlineString = string.Format(EmailOfflineApprovalString, settings.SiteUrl, instance.NodeId,
                            user.UserId, finalTask.Id, instance.Guid);

                        body = string.Format(EmailApprovalRequestString,
                            user.User.Name, docUrl, docTitle, instance.AuthorComment,
                            instance.AuthorUser.Name, typeDescription, offlineString);
                 
                        msg.To.Clear();
                        msg.To.Add(user.User.Email);
                        msg.Body = string.Format(EmailBody, msg.Subject, body);

                        client.Send(msg);
                    }
                }
                else
                {
                    msg.To.Add(string.Join(",", to.Distinct()));
                    msg.Body = string.Format(EmailBody, msg.Subject, body);

                    client.Send(msg);
                }

                Log.Info($"Email notifications sent for task { finalTask.Id }, to { msg.To }");
            }
            catch (Exception e)
            {
                Log.Error($"Error sending notifications for task { finalTask.Id }", e);
            }
        }
    }
}
