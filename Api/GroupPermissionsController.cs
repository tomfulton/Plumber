﻿using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Umbraco.Core;
using Umbraco.Core.Persistence;
using Umbraco.Web.WebApi;
using Workflow.Models;

namespace Workflow.Api
{
    public class GroupPermissionsController : UmbracoAuthorizedApiController
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Database db = ApplicationContext.Current.DatabaseContext.Database;

        [System.Web.Http.HttpGet]
        public HttpResponseMessage Get()
        {
            return Request.CreateResponse(HttpStatusCode.OK, "ok");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        [System.Web.Http.HttpPost]
        public HttpResponseMessage SavePermissions(List<UserGroupPermissionsPoco> model)
        {
            var msgText = "";

            try
            {                    
                foreach (var m in model) {
                    // if an id exists, update existing
                    if (m.Id > 0)
                    {
                        db.Update(m);
                    }
                    else
                    {
                        // an id may not exist, but an existing record may be set to 0, update it if so, otherwise insert new
                        var exists = db.Fetch<UserGroupPermissionsPoco>("SELECT * FROM WorkflowUserGroupPermissions WHERE GroupId = @0 AND NodeId = @1 AND Permission = @2", m.GroupId, m.NodeId, 0);
                        if (exists.Any())
                        {
                            exists.First().Permission = m.Permission;
                            db.Update(exists.First());
                        }
                        else
                        {
                            db.Insert(m);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                msgText = "Error saving permissions. " + ex.Message;
                log.Error(msgText, ex);
                return Request.CreateResponse(HttpStatusCode.NoContent, msgText);
            }

            msgText = "Permissions updated";
            log.Debug(msgText);

            return Request.CreateResponse(HttpStatusCode.OK, msgText);
        }
    }
}
