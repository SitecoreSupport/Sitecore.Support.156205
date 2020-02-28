using System;
using System.Collections.Specialized;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Web.UI.Sheer;
using Sitecore.Shell.Framework.Commands;

namespace Sitecore.Support.Shell.Framework.Commands
{
    [Serializable]
    public class AddVersion : Command
    {
        // Methods
        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            if (context.Items.Length == 1)
            {
                Item item = context.Items[0];
                NameValueCollection parameters = new NameValueCollection
                {
                    ["id"] = item.ID.ToString(),
                    ["language"] = item.Language.ToString(),
                    ["version"] = item.Version.ToString()
                };
                Context.ClientPage.Start(this, "Run", parameters);
            }
        }

        /// <summary>
        /// Queries the state of the command.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The state of the command.</returns>
        public override CommandState QueryState(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            if ((int)context.Items.Length != 1)
            {
                return CommandState.Hidden;
            }
            Item items = context.Items[0];
            if (items.TemplateID == TemplateIDs.Template || items.TemplateID == TemplateIDs.TemplateSection || items.TemplateID == TemplateIDs.TemplateField)
            {
                return CommandState.Hidden;
            }
            if (items.Appearance.ReadOnly && !items.IsFallback)
            {
                return CommandState.Disabled;
            }
            if (Context.IsAdministrator)
            {
                return CommandState.Enabled;
            }
            if (!items.Access.CanWrite())
            {
                return CommandState.Disabled;
            }
            if (!items.Locking.CanLock() && !items.Locking.HasLock())
            {
                if (items.IsFallback && (items.Version.Number == 1))
                {
                    return CommandState.Enabled;
                }
                return CommandState.Disabled;
            }
            if (!items.Access.CanWriteLanguage())
            {
                return CommandState.Disabled;
            }
            return base.QueryState(context);
        }



        protected void Run(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            #region Fixed code
            string id = args.Parameters["id"];
            string language = args.Parameters["language"];
            string version = args.Parameters["version"];

            Item item = Context.ContentDatabase.Items[id, Language.Parse(language), Sitecore.Data.Version.Parse(version)];

            bool isAdministrator = Context.IsAdministrator;
            item.Access.CanWrite();
            item.Locking.CanLock();
            item.Locking.HasLock();

            if ((Context.IsAdministrator || (item.Access.CanWrite() && (item.Locking.CanLock() || item.Locking.HasLock()))) || (item.IsFallback && (item.Version.Number == 1)))
            {

                #endregion
                if (item == null)
                {
                    SheerResponse.Alert("Item not found.", Array.Empty<string>());
                }
                else if (SheerResponse.CheckModified())
                {
                    Sitecore.Data.Version[] versionNumbers = item.Versions.GetVersionNumbers(false);
                    string[] parameters = new string[] { AuditFormatter.FormatItem(item) };
                    Log.Audit(this, "Add version: {0}", parameters);
                    Item item2 = item.Versions.AddVersion();
                    if ((versionNumbers != null) && (versionNumbers.Length != 0))
                    {
                        object[] objArray1 = new object[] { "item:versionadded(id=", item2.ID, ",version=", item2.Version, ",language=", item2.Language, ")" };
                        Context.ClientPage.SendMessage(this, string.Concat(objArray1));
                    }
                }
            }
        }
    }
}