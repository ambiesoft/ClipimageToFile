using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
namespace ClipimageToFile
{
    enum SaveImageType { NONE,BMP,PNG,JPG, }
    static class Program
    {
        static bool isEqualCodec(string c, SaveImageType t)
        {
            if (t == SaveImageType.BMP && c == "bmp")
                return true;
            if (t == SaveImageType.JPG && ((c == "jpg") || (c=="jpeg")))
                return true;
            if (t == SaveImageType.PNG && c == "png")
                return true;

            return false;
        }



                    //// show balloon
                    //int waitspan = 5 * 1000;
                    //NotifyIcon ni = new NotifyIcon();
                    //ni.BalloonTipTitle = Application.ProductName;
                    //ni.BalloonTipText = Properties.Resources.IMAGE_HAS_SET_ON_CLIPBOARD;
                    //ni.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

                    //ni.Text = Application.ProductName;
                    //ni.Visible = true;
                    //ni.ShowBalloonTip(waitspan);
                    //System.Threading.Thread.Sleep(waitspan);
                    //ni.Dispose();

        static void showBalloon(string message)
        {
            string arg = string.Format("/title:{0} /icon:{1} /iconindex:1 /duration:5000 /waitpid:{2} {3}",
                Application.ProductName,
                "\"" + Assembly.GetEntryAssembly().Location + "\"",
                System.Diagnostics.Process.GetCurrentProcess().Id.ToString(),
                System.Web.HttpUtility.UrlEncode(message));
            System.Diagnostics.Process.Start("showballoon.exe",arg);

            // System.Threading.Thread.Sleep(5000);        }                




        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Application.EnableVisualStyles();
            // Application.SetCompatibleTextRenderingDefault(false);
            bool isFileClipboard = false;
            SaveImageType sit = SaveImageType.PNG;

            for(int i=0 ; i < args.Length ; ++i)
            {
                 string arg = args[i];
                if (arg == "/c")
                {
                    isFileClipboard = true;
                }
                else if(arg=="/t")
                {
                    if ((i + 1) != args.Length)
                    {
                        ++i;
                        arg = args[i];
                        arg = arg.ToLower();
                        if (arg == "bmp")
                        {
                            sit = SaveImageType.BMP;
                        }
                        else if (arg == "jpg" || arg == "jpeg")
                        {
                            sit = SaveImageType.JPG;
                        }
                        else if (arg == "png")
                        {
                            sit = SaveImageType.PNG;
                        }
                        else
                        {
                            MessageBox.Show(Properties.Resources.INVALID_IMAGE_TYPE + " \"" + arg + "\"",
                                Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            return;
                        }
                    }
                }
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
                    string clipfileext=null;
                    ImageFormat clipimagef=null;
                    switch (sit)
                    {
                        case SaveImageType.BMP:
                            clipfileext = ".bmp";
                            clipimagef = ImageFormat.Bmp;
                            break;

                        case SaveImageType.JPG:
                            clipfileext = ".jpg";
                            clipimagef = ImageFormat.Jpeg;
                            break;

                        case SaveImageType.PNG:
                            clipfileext = ".png";
                            clipimagef = ImageFormat.Png;
                            break;

                    }

                    DateTime now = DateTime.Now;
                    string datetime = "clipshot " + now.ToShortDateString().Replace("/", "-") + " " + now.ToLongTimeString().Replace(':','-');
                    string tempfile = System.IO.Path.GetTempPath() + datetime + clipfileext;
                    b.Save(tempfile, clipimagef);

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
                    Application.DoEvents();

                    showBalloon(Properties.Resources.IMAGE_HAS_SET_ON_CLIPBOARD);

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
                    int filtin = 0;
                    int i=0;
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

                                if(isEqualCodec(codec.FormatDescription.ToLower(),sit))
                                    filtin = i;

                                arFilers.Add(codec);
                                ++i;
                            }
                        }
                    }
                    ofd.OpenDialog.Filter = filter.TrimEnd('|');
                    ofd.OpenDialog.FilterIndex = filtin+1;
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