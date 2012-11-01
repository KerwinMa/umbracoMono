﻿using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;

namespace Umbraco.Web.Publishing
{
    /// <summary>
    /// Currently acts as an interconnection between the new public api and the legacy api for publishing
    /// </summary>
    internal class PublishingStrategy : BasePublishingStrategy
    {
        internal PublishingStrategy()
        {
        }

        /// <summary>
        /// Publishes a single piece of Content
        /// </summary>
        /// <param name="content"><see cref="IContent"/> to publish</param>
        /// <param name="userId">Id of the User issueing the publish operation</param>
        /// <returns>True if the publish operation was successfull and not cancelled, otherwise false</returns>
        public override bool Publish(IContent content, int userId)
        {
            var e = new PublishEventArgs();
            //Fire BeforePublish event
            FireBeforePublish(content, e);

            if (!e.Cancel)
            {
                content.ChangePublishedState(true);

                LogHelper.Info<PublishingStrategy>(
                    string.Format("Content '{0}' with Id '{1}' has been published.",
                                  content.Name, content.Id));

                //Fire AfterPublish event
                FireAfterPublish(content, e);

                //NOTE: Ideally the xml cache should be refreshed here - as part of the publishing

                return true;
            }

            return false;
        }

        /// <summary>
        /// Publishes a list of Content
        /// </summary>
        /// <param name="content">An enumerable list of <see cref="IContent"/></param>
        /// <param name="userId">Id of the User issueing the publish operation</param>
        /// <returns>True if the publish operation was successfull and not cancelled, otherwise false</returns>
        public override bool PublishWithChildren(IEnumerable<IContent> content, int userId)
        {
            var e = new PublishEventArgs();

            //Only update content thats not already been published
            foreach (var item in content.Where(x => x.Published == false))
            {
                //Fire BeforePublish event
                FireBeforePublish(item, e);
                if (e.Cancel)
                    return false;

                item.ChangePublishedState(true);
                
                LogHelper.Info<PublishingStrategy>(
                    string.Format("Content '{0}' with Id '{1}' has been published.",
                                  item.Name, item.Id));

                //Fire AfterPublish event
                FireAfterPublish(item, e);
            }

            //NOTE: Ideally the xml cache should be refreshed here - as part of the publishing

            return true;
        }

        /// <summary>
        /// Unpublishes a single piece of Content
        /// </summary>
        /// <param name="content"><see cref="IContent"/> to unpublish</param>
        /// <param name="userId">Id of the User issueing the unpublish operation</param>
        /// <returns>True if the unpublish operation was successfull and not cancelled, otherwise false</returns>
        public override bool UnPublish(IContent content, int userId)
        {
            var e = new UnPublishEventArgs();
            //Fire BeforeUnPublish event
            FireBeforeUnPublish(content, e);

            if (!e.Cancel)
            {
                //If Content has a release date set to before now, it should be removed so it doesn't interrupt an unpublish
                //Otherwise it would remain released == published
                if (content.ReleaseDate.HasValue && content.ReleaseDate.Value <= DateTime.UtcNow)
                    content.ReleaseDate = null;

                content.ChangePublishedState(false);

                LogHelper.Info<PublishingStrategy>(
                    string.Format("Content '{0}' with Id '{1}' has been unpublished.",
                                  content.Name, content.Id));

                //Fire AfterUnPublish event
                FireAfterUnPublish(content, e);

                //NOTE: Ideally the xml cache should be refreshed here - as part of the unpublishing

                return true;
            }

            return false;
        }

        /// <summary>
        /// Unpublishes a list of Content
        /// </summary>
        /// <param name="content">An enumerable list of <see cref="IContent"/></param>
        /// <param name="userId">Id of the User issueing the unpublish operation</param>
        /// <returns>True if the unpublish operation was successfull and not cancelled, otherwise false</returns>
        public override bool UnPublish(IEnumerable<IContent> content, int userId)
        {
            var e = new UnPublishEventArgs();

            //Only update content thats already been published
            foreach (var item in content.Where(x => x.Published == true))
            {
                //Fire BeforeUnPublish event
                FireBeforeUnPublish(item, e);
                if (e.Cancel)
                    return false;

                //If Content has a release date set to before now, it should be removed so it doesn't interrupt an unpublish
                //Otherwise it would remain released == published
                if (item.ReleaseDate.HasValue && item.ReleaseDate.Value <= DateTime.UtcNow)
                    item.ReleaseDate = null;

                item.ChangePublishedState(false);

                LogHelper.Info<PublishingStrategy>(
                    string.Format("Content '{0}' with Id '{1}' has been unpublished.",
                                  item.Name, item.Id));

                //Fire AfterUnPublish event
                FireAfterUnPublish(item, e);
            }

            //NOTE: Ideally the xml cache should be refreshed here - as part of the publishing

            return true;
        }
    }
}