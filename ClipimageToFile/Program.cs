using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
namespace ClipimageToFile
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Application.EnableVisualStyles();
            // Application.SetCompatibleTextRenderingDefault(false);
            bool isFileClipboard = false;
            foreach (string arg in args)
            {
                if (arg == "/c" || arg == "-c")
                    isFileClipboard = true;
            }

            Object o = Clipboard.GetData(DataFormats.Bitmap);
            
            if (!(o is Bitmap))
            {
                MessageBox.Show(Properties.Resources.NO_IMAGE_ON_CLIPBOARD,
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            try
            {
                Bitmap b = (Bitmap)o;

                if (isFileClipboard)
                {
                    string tempfile = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".png";
                    b.Save(tempfile, ImageFormat.Png);

                    //切り取るファイルのパス
                    string[] fileNames = { tempfile };
                    //ファイルドロップ形式のDataObjectを作成する
                    IDataObject data = new DataObject(DataFormats.FileDrop, fileNames);

                    //DragDropEffects.Moveを設定する（DragDropEffects.Move は 2）
                    byte[] bs = new byte[] { (byte)DragDropEffects.Move, 0, 0, 0 };
                    System.IO.MemoryStream ms = new System.IO.MemoryStream(bs);
                    data.SetData("Preferred DropEffect", ms);

                    //クリップボードに切り取る
                    Clipboard.Clear();
                    Clipboard.SetDataObject(data, true);
                }
                else
                {
                    CustomControls.FormSaveFileDialog ofd = new CustomControls.FormSaveFileDialog();
                    ofd.pbxPreview.Image = (Bitmap)b.Clone();
                    ofd.lblColorsValue.Text = ofd.GetColorsCountFromImage(ofd.pbxPreview.Image);
                    ofd.lblFormatValue.Text = ofd.GetFormatFromImage(ofd.pbxPreview.Image);

                    ofd.OpenDialog.Title = b.Width + "x" + b.Height + " - " + Application.ProductName;
                    ofd.OpenDialog.AddExtension = true;

                    System.Collections.ArrayList arFilers = new System.Collections.ArrayList();
                    string filter = string.Empty;
                    ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
                    foreach (ImageCodecInfo codec in codecs)
                    {
                        if ((codec.Flags & ImageCodecFlags.Encoder) != 0)
                        {
                            if (ImageFormat.Gif.Guid != codec.FormatID)
                            {
                                string ext = codec.FilenameExtension;
                                ext = ext.Split(';')[0];
                                ext = ext.ToLower();
                                filter += codec.FormatDescription + " (" + ext + ")|" + ext + "|";

                                arFilers.Add(codec);
                            }
                        }
                    }
                    ofd.OpenDialog.Filter = filter.TrimEnd('|');
                    if (DialogResult.OK != ofd.ShowDialog())
                        return;

                    ImageCodecInfo ici = (ImageCodecInfo)arFilers[ofd.OpenDialog.FilterIndex - 1];

                    EncoderParameters encoderParameters = new EncoderParameters(1);
                    encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 100L);
                    b.Save(ofd.OpenDialog.FileName, ici, encoderParameters);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,
                    Application.ProductName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}