using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using KeePass.Plugins;

using ExeIconPicker.Controls;
using KeePassLib;
using KeePassLib.Collections;
using System.IO;
using System.Drawing.Imaging;
using System.Diagnostics;
using KeePassLib.Interfaces;
using ExeIconPicker.Utils;

namespace ExeIconPicker
{
    public sealed class ExeIconPickerExt : Plugin
    {
        // Plugin host
        private IPluginHost pluginHost;

        public override string UpdateUrl
        {
            get
            {
                return "https://raw.githubusercontent.com/Eveldee/KeePass-ExeIconPicker/master/Version.txt";
            }
        }

        // Icon
        Image contextMenuPickExeIcon;
        //  Entry Context Menu
        ContextMenuStrip entryContextMenu;
        ToolStripSeparator entrySeparator;
        ToolStripMenuItem entryPickExe;
        // Group Context Menu
        ContextMenuStrip groupContextMenu;
        ToolStripSeparator groupSeparator;
        ToolStripMenuItem groupPickExe;

        public override bool Initialize(IPluginHost host)
        {
            Util.Log("Plugin initializing...");

            pluginHost = host;

            // Create controls
            contextMenuPickExeIcon = Properties.Resources.Exe1;
            // Entry
            entrySeparator = new ToolStripSeparator();
            entryPickExe = new ToolStripMenuItem("Pick Icon from Exe", contextMenuPickExeIcon, EntryPickIcon_Click);
            // Group
            groupSeparator = new ToolStripSeparator();
            groupPickExe = new ToolStripMenuItem("Pick Icon from Exe", contextMenuPickExeIcon, GroupPickIcon_Click);

            // Register controls
            // Entry
            entryContextMenu = pluginHost.MainWindow.EntryContextMenu;
            entryContextMenu.Items.Add(entrySeparator);
            entryContextMenu.Items.Add(entryPickExe);
            // Group
            groupContextMenu = pluginHost.MainWindow.GroupContextMenu;
            groupContextMenu.Items.Add(groupSeparator);
            groupContextMenu.Items.Add(groupPickExe);

            Util.Log("Plugin initialized");

            return true;
        }

        public override void Terminate()
        {
            Util.Log("Terminating plugin...");

            // Remove controls
            // Entry
            entryContextMenu.Items.Remove(entrySeparator);
            entryContextMenu.Items.Remove(entryPickExe);
            // Group
            groupContextMenu.Items.Remove(groupSeparator);
            groupContextMenu.Items.Remove(groupPickExe);

            Util.Log("Plugin terminared");
        }

        private void EntryPickIcon_Click(object sender, EventArgs e)
        {
            Util.Log("Entry Context Menu -> PickExeIcon clicked");

            if (CheckMono())
                return;

            PwEntry[] entries = pluginHost.MainWindow.GetSelectedEntries();
            Execute(entries, null);
        }

        private void GroupPickIcon_Click(object sender, EventArgs e)
        {
            Util.Log("Group Context Menu -> PickExeIcon clicked");

            if (CheckMono())
                return;

            PwGroup group = pluginHost.MainWindow.GetSelectedGroup();
            if (group == null)
            {
                Util.Log("No group selected");
                return;
            }

            // Get all entries from the group
            bool subEntries = KeePass.Program.Config.MainWindow.ShowEntriesOfSubGroups;
            PwObjectList<PwEntry> entriesInGroup = group.GetEntries(subEntries);
            if (entriesInGroup == null)
            {
                Util.Log("No entries in group are null");
                return;
            }

            // Copy PwObjectList<PwEntry> to PwEntry[]
            PwEntry[] entries = entriesInGroup.CloneShallowToList().ToArray();
            Execute(entries, group);
        }

        private bool CheckMono()
        {
            if (Util.IsRunningOnMono())
            {
                MessageBox.Show("This extension don't work on mono", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            return false;
        }

        private void Execute(PwEntry[] entries, PwGroup group)
        {
            // Lock everything
            pluginHost.MainWindow.UIBlockInteraction(true);

            if (entries == null)
            {
                Util.Log("No entries are null");
                pluginHost.MainWindow.UIBlockInteraction(false);
                return;
            }

            // Pick an icon
            Bitmap icon = PickIcon();

            // If user cancelled
            if (icon == null)
            {
                Util.Log("Returned Bitmap is null");
                pluginHost.MainWindow.SetStatusEx("Cancelled.");
                pluginHost.MainWindow.UIBlockInteraction(false);
                return;
            }

            // Convert icon to png
            byte[] data;
            using (var stream = new MemoryStream())
            {
                icon.Save(stream, ImageFormat.Png);
                stream.Position = 0;
                data = new byte[stream.Length];

                stream.Read(data, 0, data.Length);
                icon.Dispose();
            }

            PwCustomIcon customIcon;

            // Check uuid for duplicates
            var uuid = new PwUuid(Util.HashData(data));
            var dbIcon = pluginHost.Database.CustomIcons.FirstOrDefault(x => x.Uuid.Equals(uuid));
            if (dbIcon == null)
            {
                Util.Log("Icon doesn't exist");
                customIcon = new PwCustomIcon(uuid, data);
                pluginHost.Database.CustomIcons.Add(customIcon);
            }
            else
            {
                Util.Log("Icon already exists");
                customIcon = dbIcon;
            }

            // Update icons
            ChangeEntriesIcon(entries, group, customIcon);

            // Unblock and update
            pluginHost.Database.UINeedsIconUpdate = true;
            pluginHost.MainWindow.UIBlockInteraction(false);
            pluginHost.MainWindow.RefreshEntriesList();
            pluginHost.MainWindow.UpdateUI(false, null, true, null, false, null, true);

            // Set status text
            pluginHost.MainWindow.SetStatusEx(string.Format("Updated {0} {1} {2}.",
                entries.Length,
                entries.Length == 1 ? "entry" : "entries",
                group == null ? "" : "and one group"));

            Util.Log("Executed successfully");
        }

        private Bitmap PickIcon()
        {
            Util.Log("Picking icon.");

            using (BitmapPickerDialog bitmapPicker = new BitmapPickerDialog())
            {
                if (bitmapPicker.ShowDialog() == DialogResult.OK)
                {
                    if (bitmapPicker.Result != null)
                    {
                        Util.Log("Icon picked");
                        return bitmapPicker.Result;
                    }
                }
                Util.Log("User cancelled");
                return null;
            }
        }

        private void ChangeEntriesIcon(PwEntry[] entries, PwGroup group, PwCustomIcon icon)
        {
            if (group != null)
            {
                group.CustomIconUuid = icon.Uuid;
                group.Touch(true, false);
            }

            foreach (var entry in entries)
            {
                // Set icon & update
                entry.CustomIconUuid = icon.Uuid;
                entry.Touch(true, false);
            }
        }
    }
}
