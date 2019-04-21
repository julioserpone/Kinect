using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Trumix.Library;

namespace Template2
{
    public class ImageProcessing
    {
        const int WithSideSection = 50;
        public enum SectionsPages
        {
            OUT = 0,
            WINDOW = 1,
            LEFT = 2,
            RIGHT = 3
        }
        public enum HandType
        {
            LEFT = 0,
            RIGHT = 1
        }
        
        private int PagesNumbers;
        public int PageActual { get; set; }
        public bool IsLoadPages { get; set; }
        //public bool AllowChangePage { get; set; }
        //public double PosX { get; set; }
        //public double PosY { get; set; }

        //public BitmapImage PagesLeft { get; set; }
        //public BitmapImage PagesRight { get; set; }
        //public SectionsPages PositionActual { get; set; }
        //private SectionsPages IsSidePage { get; set; }
        //public HandType HandTypeID { get; set; }

        // Volatile is used as hint to the compiler that this data 
        // member will be accessed by multiple threads. 
        private volatile bool _shouldStop;

        public void DoWork(ref BitmapImage[] Pages, ref int pPagesNumbers)
        {
            //Cargar imagenes al array (paginas del catalogo)
            string[] fileEntries = GetFilesDirectory();
            PagesNumbers = fileEntries.Length + ((fileEntries.Length % 2 == 0) ? 2 : 1);       //Sumamos paginas adicionales de color negro, al principio y/o al final
            if (Pages == null) Pages = new BitmapImage[PagesNumbers];

            LoadImagesPages(ref Pages, fileEntries);
            PageActual = 1;
            pPagesNumbers = PagesNumbers;
            //PagesLeft = Pages[PageActual - 1];          //Primera Pagina en negro
            //PagesRight = Pages[PageActual];

            //int scrnH = (int)System.Windows.SystemParameters.PrimaryScreenHeight;
            //int scrnW = (int)System.Windows.SystemParameters.PrimaryScreenWidth;

            //PositionActual = SectionsPages.OUT;
            //IsSidePage = SectionsPages.OUT;

            //while (!_shouldStop)
            //{
            //    //Aqui deberiamos trabajar el intercambio de imagenes. Lo que tengo pensado hacer es pasarle mediante otro metodo hacia que sentido voy a realizar el cambio y la posicion de mouse en X
            //    //Habia realizado otro metodo, pero el proceso es muy lento. Adicionalmente, produce que el proceso en ejecucion demore en responder mientras se procesa la imagen.
            //    //Otro punto importante es que deberiamos procesar cada imagen para todas las posibles posiciones en X. Luego de procesadas, las muestro a  medida que muevo el mouse.
            //    //Esto nos permitiria hacer un mejor muestreo. Luego de mas de una semana de pruebas, considero que esta deberia ser la mejor manera de procesar dichas imagenes.
                
            //    Console.WriteLine("worker thread: working...");

            //    if (PositionActual == SectionsPages.LEFT || PositionActual == SectionsPages.RIGHT)
            //    {
            //        IsSidePage = PositionActual;
            //    }

            //    if (HandTypeID == HandType.RIGHT)
            //    {
            //        if (PosX >= 0 && PosX <= WithSideSection && PosY >= 0 && PosY <= scrnH)
            //        { 
            //            //izquierdo
            //            PositionActual = SectionsPages.LEFT;
            //        }
            //        else if (PosX >= scrnW - WithSideSection && PosX <= scrnW && PosY >= 0 && PosY <= scrnH)
            //        { 
            //            //derecho
            //            PositionActual = SectionsPages.RIGHT;
            //        }
            //        else if (PosX >= WithSideSection && PosX <= scrnW - WithSideSection && PosY >= 0 && PosY <= scrnH)
            //        {
            //            if (IsSidePage == SectionsPages.RIGHT)
            //            {
            //                if ((PageActual + 2 < PagesNumbers) && (!AllowChangePage))
            //                {
            //                    PageActual += 2;
            //                    AllowChangePage = true;
            //                }
            //            }
            //            else if (IsSidePage == SectionsPages.LEFT)
            //            {
            //                if ((PageActual - 2 >= 1) && (!AllowChangePage))
            //                {
            //                    PageActual -= 2;
            //                    AllowChangePage = true;
            //                }
            //            }
            //            PositionActual = SectionsPages.WINDOW;
            //            Thread.Sleep(2000);
            //            //AllowChangePage = false;
            //            //PagesRight = Pages[PageActual + 1];
            //            //PagesLeft = Pages[PageActual];

            //        }
            //        else
            //        {
            //            PositionActual = SectionsPages.OUT;
            //        }
            //    }
            //}
            Console.WriteLine("worker thread: terminating gracefully.");
        }

        private string[] GetFilesDirectory()
        {
            String baseURL = AppDomain.CurrentDomain.BaseDirectory + "Revista";
            return Directory.GetFiles(baseURL);
        }

        public void LoadImagesPages(ref BitmapImage[] bmiPages, string[] fileEntries)
        {
            try
            {
                String baseURL = AppDomain.CurrentDomain.BaseDirectory + "Revista";

                BitmapImage[] Pages = new BitmapImage[PagesNumbers];
                bmiPages.CopyTo(Pages, 0);

                fileEntries.ForEachWithIndex((fileName, idx) =>
                {
                    var bi = new BitmapImage();

                    using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                    {
                        bi.BeginInit();
                        bi.DecodePixelWidth = 1024;
                        bi.DecodePixelHeight = 1024;
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.StreamSource = stream;
                        bi.EndInit();
                    }

                    bi.Freeze();
                    Pages[idx + 1] = bi;
                    //Pages[idx + 1] = new BitmapImage(Utilities.LoadUriImageUrl(baseURL, null, fileName));
                    //Pages[idx + 1].Freeze();
                });

                //Creacion de la imagen negra (pagina en blanco) para poder mostrar la portada y contraportada de la revista
                Pages[PageActual] = new BitmapImage();
                var FirstPage = DrawPageBlank();
                FirstPage.Freeze(); //Este freeze se hace porque por algun motivo que todavia desconozco, la imagen generada queda como en un estatus que no me permite luego procesarla al momento de enviarla al canvas
                Pages[PageActual] = FirstPage;
                Pages[PagesNumbers - 1] = new BitmapImage();
                var LastPage = DrawPageBlank();
                LastPage.Freeze();
                Pages[PagesNumbers - 1] = LastPage;
                Pages.CopyTo(bmiPages, 0);

                IsLoadPages = true;

            }
            catch (Exception objException)
            {
                Console.WriteLine(objException.InnerException);
            }

        }

        //public BitmapImage GetPage(SectionsPages position)
        //{
        //    //return (position == SectionsPages.LEFT) ? Pages[PageActual].Clone() : Pages[PageActual + 1].Clone();
        //    return (position == SectionsPages.LEFT) ? PagesLeft : PagesRight;
        //}

        private BitmapImage DrawPageBlank()
        {
            try
            {
                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap((int)System.Windows.SystemParameters.PrimaryScreenHeight, (int)System.Windows.SystemParameters.PrimaryScreenWidth / 2);
                System.Drawing.Graphics gBmp = System.Drawing.Graphics.FromImage(bmp);
                gBmp.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                System.Drawing.Color black = System.Drawing.Color.FromArgb(255, 0, 0, 0);
                System.Drawing.Brush blackBrush = new System.Drawing.SolidBrush(black);
                gBmp.FillRectangle(blackBrush, 0, 0, bmp.Width, bmp.Height);
                
                gBmp.Dispose();
                return Bitmap2BitmapImage(bmp);
            }
            catch (Exception)
            {

                throw;
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        /// <summary>
        /// Convertidor de objeto tipo Bitmap a BitmapImge (utilizado en el proceso de extracion de frames de un video)
        /// </summary>
        /// <param name="bitmap">Objeto de tipo Bitmap (System.Drawing.Bitmap</param>
        /// <returns>Objeto BitmapSource (el cual es herencia de un BitmapImage)</returns>
        private BitmapImage Bitmap2BitmapImage(System.Drawing.Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            BitmapImage retval;

            try
            {
                BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                MemoryStream memoryStream = new MemoryStream();

                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(memoryStream);
                retval = new BitmapImage();
                retval.BeginInit();
                retval.StreamSource = new MemoryStream(memoryStream.ToArray());
                retval.EndInit();

                memoryStream.Close();
                memoryStream.Dispose();

            }
            finally
            {
                DeleteObject(hBitmap);
            }

            return retval;
        }

        private System.Drawing.Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new System.Drawing.Bitmap(bitmap);
            }
        }

        public void RequestStop()
        {
            _shouldStop = true;
        }
        
    }

}
