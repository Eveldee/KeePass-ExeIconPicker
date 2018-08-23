using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TsudaKageyu;

namespace ExeIconPicker.Controls
{
    public partial class BitmapPickerDialog : Form //source: https://www.codeproject.com/articles/26824/extract-icons-from-exe-or-dll-files
    {
        public Bitmap Result { get; private set; }
        private bool firstOpen = true;

        private class IconListViewItem : ListViewItem
        {
            public Bitmap Bitmap { get; set; }
        }

        public BitmapPickerDialog()
        {
            InitializeComponent();
        }

        private void ClearAllIcons()
        {
            foreach (var item in lvwIcons.Items)
                ((IconListViewItem)item).Bitmap.Dispose();

            lvwIcons.Items.Clear();
        }

        private void BitmapPickerDialog_Load(object sender, EventArgs e)
        {
            btnPickFile_Click(this, null);
            firstOpen = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            ClearAllIcons();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Return_Result();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Hide();
        }

        private void btnPickFile_Click(object sender, EventArgs e)
        {
            var result = iconPickerDialog.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                var fileName = iconPickerDialog.FileName;
                var index = iconPickerDialog.IconIndex;

                Icon icon = null;
                Icon[] splitIcons = null;
                try
                {
                    if (Path.GetExtension(iconPickerDialog.FileName).ToLower() == ".ico")
                    {
                        icon = new Icon(iconPickerDialog.FileName);
                    }
                    else
                    {
                        var extractor = new IconExtractor(fileName);
                        icon = extractor.GetIcon(index);
                    }

                    splitIcons = IconUtil.Split(icon);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                txtFileName.Text = String.Format("{0}, #{1}, {2} variations", fileName, index, splitIcons.Length);

                // Update icons.

                Icon = icon;
                icon.Dispose();

                lvwIcons.BeginUpdate();
                ClearAllIcons();

                foreach (var i in splitIcons)
                {
                    // Exclude all icons which size is > 256 (Throw "Generic GDI+ error" when converting if size > 128x128)
                    if (i.Width > 128 || i.Height > 128)
                        continue;

                    var item = new IconListViewItem();
                    var size = i.Size;
                    var bits = IconUtil.GetBitCount(i);
                    item.ToolTipText = String.Format("{0}x{1}, {2} bits", size.Width, size.Height, bits);
                    item.Bitmap = IconUtil.ToBitmap(i);
                    i.Dispose();

                    lvwIcons.Items.Add(item);
                }

                lvwIcons.EndUpdate();
            }
            else if (firstOpen)
            {
                DialogResult = DialogResult.Cancel;
                this.Hide();
            }
        }

        private void lvwIcons_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            var item = e.Item as IconListViewItem;

            // Draw item

            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.Clip = new Region(e.Bounds);

            if (e.Item.Selected)
                e.Graphics.FillRectangle(SystemBrushes.MenuHighlight, e.Bounds);
            else
                e.Graphics.FillRectangle(SystemBrushes.Window, e.Bounds);

            int w = Math.Min(128, item.Bitmap.Width);
            int h = Math.Min(128, item.Bitmap.Height);

            int x = e.Bounds.X + (e.Bounds.Width - w) / 2;
            int y = e.Bounds.Y + (e.Bounds.Height - h) / 2;
            var dstRect = new Rectangle(x, y, w, h);
            var srcRect = new Rectangle(Point.Empty, item.Bitmap.Size);


            e.Graphics.DrawImage(item.Bitmap, dstRect, srcRect, GraphicsUnit.Pixel);

            var textRect = new Rectangle(
                e.Bounds.Left, e.Bounds.Bottom - Font.Height - 4,
                e.Bounds.Width, Font.Height + 2);
            TextRenderer.DrawText(e.Graphics, item.ToolTipText, Font, textRect, ForeColor);

            e.Graphics.Clip = new Region();
            e.Graphics.DrawRectangle(SystemPens.ControlLight, e.Bounds);
        }

        private void lvwIcons_ItemActivate(object sender, EventArgs e)
        {
            // Return result when activated
            Return_Result();
        }

        private void Return_Result()
        {
            // Check if selection is empty
            var items = lvwIcons.SelectedItems;
            if (items == null || items.Count < 1)
                return;

            Result = ((IconListViewItem)items[0]).Bitmap.Clone() as Bitmap;
            DialogResult = DialogResult.OK;
            this.Hide();
        }
    }
}
