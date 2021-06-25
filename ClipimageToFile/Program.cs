using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using Ambiesoft;

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

        static void showBalloon(string message)
        {
            string arg = string.Format("/title:{0} /icon:{1} /iconindex:1 /duration:5000 /waitpid:{2} {3}",
                Application.ProductName,
                "\"" + Assembly.GetEntryAssembly().Location + "\"",
                System.Diagnostics.Process.GetCurrentProcess().Id.ToString(),
                System.Web.HttpUtility.UrlEncode(message));
            System.Diagnostics.Process.Start("showballoon.exe",arg);
        }

        static string get2keta(int n)
        {
            string ret = n.ToString();
            if (ret.Length == 0)
                return "00";
            if (ret.Length == 1)
                return "0" + ret;
            return ret;
        }

        static string getDTString(DateTime now)
        {
            string ret = now.Year.ToString() + "-" + get2keta(now.Month) + "-" + get2keta(now.Day) + " ";
            ret += get2keta(now.Hour) + "-" + get2keta(now.Minute) + "-" + get2keta(now.Second);
            return ret;
        }


        /// <summary>
        /// 
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Ambiesoft.CppUtils.AmbSetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool isFileClipboard = false;
            SaveImageType sit = SaveImageType.PNG;

            for(int i=0 ; i < args.Length ; ++i)
            {
                 string arg = args[i];
                if (arg == "/c")
                {
                    isFileClipboard = true;
                }
                else if(arg=="/v" || arg=="-v" ||
                    arg=="/h" || arg=="-h" || arg=="/?" || arg=="--help")
                {
                    StringBuilder sbMessage = new StringBuilder();

                    sbMessage.AppendFormat("{0} - {1}",
                        Application.ProductName,
                        "Save an image on the clipboard to a file");
                    sbMessage.AppendLine();
                    sbMessage.AppendLine();
                    sbMessage.AppendLine("Usage:");
                    sbMessage.AppendFormat("{0} {1}",
                        Path.GetFileNameWithoutExtension(Application.ExecutablePath),
                        "[/c] [/t [png|jpg]] [/h]");
                    sbMessage.AppendLine();
                    sbMessage.AppendLine();

                    string optionformat = "{0}\t{1}";

                    sbMessage.AppendFormat(optionformat,
                        "-h", Properties.Resources.ID_SHOW_HELP);
                    sbMessage.AppendLine();

                    sbMessage.AppendFormat(optionformat,
                        "/c", Properties.Resources.ID_C_HELP);
                    sbMessage.AppendLine();

                    sbMessage.AppendFormat(optionformat,
                        "/t", Properties.Resources.ID_T_HELP);
                    sbMessage.AppendLine();

                    sbMessage.AppendLine();
                    sbMessage.AppendLine("Copyright 2019 Ambiesoft");
                    sbMessage.AppendLine("https://ambiesoft.github.io/webjumper/?target=ClipimageToFile");

                    JR.Utils.GUI.Forms.FlexibleMessageBox.Show(
                        sbMessage.ToString(),
                        string.Format("{0} version {1}",
                        Application.ProductName, 
                        AmbLib.getAssemblyVersion(Assembly.GetExecutingAssembly(),3)),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
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
                else
                {
                    MessageBox.Show(
                        string.Format(Properties.Resources.ID_UNKNOW_OPTION, arg),
                        Application.ProductName,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
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

                    string tempfile;
                    while(true)
                    {
                        DateTime now = DateTime.Now;
                        string datetime = "clipshot " + getDTString(now);
                        tempfile = System.IO.Path.GetTempPath() + datetime + clipfileext;
                        if (File.Exists(tempfile))
                        {
                            System.Threading.Thread.Sleep(1000);
                            continue;
                        }
                        break;
                    }

                    b.Save(tempfile, clipimagef);

                    // file to be cut on clipboard
                    string[] fileNames = { tempfile };
                    // DataObject will be holding data
                    DataObject data = new DataObject(DataFormats.FileDrop, fileNames);

                    // DragDropEffects.Move
                    byte[] bs = new byte[] { (byte)DragDropEffects.Move, 0, 0, 0 };
                    System.IO.MemoryStream ms = new System.IO.MemoryStream(bs);
                    data.SetData("Preferred DropEffect", ms);
                    data.SetText(tempfile);

                    // cut onto clipbard
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
                    encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
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