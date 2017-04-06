using System;
using System.Collections.Specialized;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Web.UI.Sheer;
using Sitecore.Shell.Framework.Commands;

namespace Sitecore.Support.Shell.Framework.Commands
{  /// <summary>
   /// Represents the AddVersion command.
   /// </summary>
    [Serializable]
    public class AddVersion : Command
    {
        #region Public methods

        /// <summary>
        /// Executes the command in the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");

            if (context.Items.Length != 1)
            {
                return;
            }

            Item item = context.Items[0];

            NameValueCollection parameters = new NameValueCollection();

            parameters["id"] = item.ID.ToString();
            parameters["language"] = item.Language.ToString();
            parameters["version"] = item.Version.ToString();

            Context.ClientPage.Start(this, "Run", parameters);
        }

        /// <summary>
        /// Queries the state of the command.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The state of the command.</returns>
        public override CommandState QueryState(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");

            if (context.Items.Length != 1)
            {
                return CommandState.Hidden;
            }

            Item item = context.Items[0];

            if (item.TemplateID == TemplateIDs.Template || item.TemplateID == TemplateIDs.TemplateSection || item.TemplateID == TemplateIDs.TemplateField)
            {
                return CommandState.Hidden;
            }

            if (item.Appearance.ReadOnly && !item.IsFallback)
            {
                return CommandState.Disabled;
            }

            if (Context.IsAdministrator)
            {
                return CommandState.Enabled;
            }

            if (!item.Access.CanWrite())
            {
                return CommandState.Disabled;
            }

            if (!(item.Locking.CanLock() || item.Locking.HasLock()))
            {
                return CommandState.Disabled;
            }

            if (!item.Access.CanWriteLanguage())
            {
                return CommandState.Disabled;
            }

            return base.QueryState(context);
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Runs the pipeline.
        /// </summary>
        /// <param name="args">The arguments.</param>
        protected void Run(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            string id = args.Parameters["id"];
            string language = args.Parameters["language"];
            string version = args.Parameters["version"];

            Item item = Context.ContentDatabase.Items[id, Language.Parse(language), Sitecore.Data.Version.Parse(version)];
            var a = Context.IsAdministrator;
            var b = !item.Access.CanWrite();
            var c = !item.Locking.CanLock();
            var d = !item.Locking.HasLock();

            if (!Context.IsAdministrator && (!item.Access.CanWrite() || (!item.Locking.CanLock() && !item.Locking.HasLock())) && !(item.IsFallback && item.Version.Number == 1))
            {
                return;
            }

            if (item == null)
            {
                SheerResponse.Alert(Texts.ITEM_NOT_FOUND);
                return;
            }

            if (!SheerResponse.CheckModified())
            {
                return;
            }

            Sitecore.Data.Version[] versions = item.Versions.GetVersionNumbers(false);

            Log.Audit(this, "Add version: {0}", AuditFormatter.FormatItem(item));

            Item result = item.Versions.AddVersion();

            if (versions != null && versions.Length > 0)
            {
                Context.ClientPage.SendMessage(this,
                                               "item:versionadded(id=" + result.ID + ",version=" +
                                               result.Version + ",language=" + result.Language +
                                               ")");
            }
        }

        #endregion
    }

}