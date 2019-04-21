using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Web;

namespace AuditDevelopers
{
    public partial class frmAudit : Form
    {
        const string user = "test";
        const int time = 15000;
 
        public frmAudit()
        {
            InitializeComponent();
        }

        private void frmAudit_Load(object sender, EventArgs e)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += ScreenCaptureProcess;
            bw.RunWorkerAsync();
 
        }

        static void ScreenCaptureProcess(object o, DoWorkEventArgs e)
        {
            try
            {
                while (true)
                {
                    int screenLeft = SystemInformation.VirtualScreen.Left;
                    int screenTop = SystemInformation.VirtualScreen.Top;
                    int screenWidth = SystemInformation.VirtualScreen.Width;
                    int screenHeight = SystemInformation.VirtualScreen.Height;

                    using (Bitmap bmp = new Bitmap(screenWidth, screenHeight))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.CopyFromScreen(screenLeft, screenTop, 0, 0, bmp.Size);
                        }

                        if (SendData(ImgToString(bmp)) == null) 
                            MessageBox.Show("Hay un error en la conexión, por favor avisar vía mail a soporte@personalremoto.com");
                        bmp.Save("C:\\Temp\\" + user + ".png", ImageFormat.Png);    //Debo cambiar la numeracion del archivo, y guardarlo en otra carpeta

                    }
                    System.Threading.Thread.Sleep(time);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("A ocurrido un error:" + ex.InnerException);
            }
            
        }
        static private string ImgToString(Image img)
        {
            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

            String base64String = Convert.ToBase64String(stream.ToArray());
            stream.Close();
            return base64String;
        }

        public static byte[] ImageToByte(Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }

        static private string SendData(string strImageArray)
        {
            string result = null;
            try
            {
                string sUri = "http://personal.liit.com.ar/control.php";

                string postString = string.Format("user={0}&img={1}", user, strImageArray);
                byte[] data = Encoding.ASCII.GetBytes(postString);


                string contentType = "application/x-www-form-urlencoded";
                System.Uri myUri = new Uri(sUri);
                HttpWebRequest hwrPeticion;

                hwrPeticion = System.Net.HttpWebRequest.Create(myUri) as HttpWebRequest;
                hwrPeticion.Method = "POST";
                hwrPeticion.ContentType = contentType;
                hwrPeticion.ContentLength = data.Length;
                //hwrPeticion.Referer = sUri;


                Stream requestStream = hwrPeticion.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)hwrPeticion.GetResponse();
                Stream responseStream = myHttpWebResponse.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(responseStream, Encoding.Default);
                string pageContent = myStreamReader.ReadToEnd();

                if (pageContent.Contains("ok")) result = "OK";
                myStreamReader.Close();
                responseStream.Close();
                myHttpWebResponse.Close();

                return result;
            }
            catch (System.Net.WebException ex)
            {
                return null;
            }
            

        }
    }
}
