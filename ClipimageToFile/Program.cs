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
        static void Main()
        {
            // Application.EnableVisualStyles();
            // Application.SetCompatibleTextRenderingDefault(false);

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
                SaveFileDialog ofd = new SaveFileDialog();
                ofd.Title = b.Width + "x" + b.Height + " - " + Application.ProductName;
                ofd.AddExtension = true;

                System.Collections.ArrayList arFilers = new System.Collections.ArrayList();
                string filter = string.Empty;
                ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
                foreach (ImageCodecInfo codec in codecs)
                {
                    if ((codec.Flags & ImageCodecFlags.Encoder) != 0)
                    {
                        string ext = codec.FilenameExtension;
                        ext = ext.Split(';')[0];
                        ext = ext.ToLower();
                        filter += codec.FormatDescription + " (" + ext + ")|" + ext + "|";

                        arFilers.Add(codec);
                    }
                }
                ofd.Filter = filter.TrimEnd('|');
                if (DialogResult.OK != ofd.ShowDialog())
                    return;

                ImageCodecInfo ici = (ImageCodecInfo)arFilers[ofd.FilterIndex - 1];

                EncoderParameters encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 100L);
                b.Save(ofd.FileName, ici, encoderParameters);
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