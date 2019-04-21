using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Kinect.Input;
using System.Threading;
using System.Windows.Interop;
using System.IO;
using Trumix.Library;
using Template3.Model.Object;
using AForge.Video;
using AForge.Video.FFMPEG;

namespace Template3
{
    /// <summary>
	/// Lógica de interacción para MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
    {

        #region "Propiedades"

		private int autoActual;
		private int GalleriesLoaded;
        private Gallery<Image>[] lstGallery;
        private int[] GalleriesSecuence;
        private int maxFrames = 120; //1 frame = 1 segundo

		private int thumbnailWhere = 0;
		private int thumbnailCounter = 0;
		private int thumbnailWidth = 150;
        private int thumbnailHeight = 100;

		KinectSensor sensor;
		private MultiSourceFrameReader reader;
		private IList<Body> bodies;
		private TimeSpan lastTime;
		private ulong firstTrackId;
		private int HandLeftId = 0;
		private int HandRightId = 1;
		private int WidthScreen = 0;
		private int HeightScreen = 0;
        private int DistanceBetweenImg = 10;
		private CoordinateMapper coordinateMapper = null;
		private Image imgHandRight = null;
		private Image imgHandLeft = null;

		public volatile int contImgGiro;
		public TimeSpan giroAutoTimer;
		public volatile bool stopMove = true;
		public Thread thMoverAuto;

		private int initMove = 0;
		private int endMove = 0;

        #endregion

        #region "Constructor"
        /// <summary>
        /// Funcion inicial de la ventana
        /// </summary>
		public MainWindow() {

            IniciatizeControls();
            LoadImages();
            LoadVideos();
		}

        #endregion

        #region "Eventos de controles"

		private void MouseEventHandler(object sender, MouseEventArgs e) {
			Point p = e.GetPosition(ventana);
			PointF point = new PointF();
			point.X = (float)((p.X + 60 / 2) / mainScreen.ActualWidth);
			point.Y = (float)((p.Y + 60 / 2) / mainScreen.ActualHeight);

			TimeSpan time = new TimeSpan(DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second, DateTime.UtcNow.Millisecond);
			firstTrackId = 23842342342;

			if (lastTime == TimeSpan.Zero || lastTime != time) {
				lastTime = time;
				mainScreen.Children.Clear();
                //mainScreen.Children.Add(manoLeft);
				mainScreen.Children.Add(imgHandRight);
			}
			RenderPointer(point, point, 0, time, firstTrackId, HandType.RIGHT);
			initMove = 0;
			endMove = 500;  //Limita el campo de accion de movimientos el eje Y. Es decir, despues de la posicion 500px (Altura) con el mouse, deja de tener efecto de movimiento
		}

		private void kinectCoreWindow_PointerMoved(object sender, KinectPointerEventArgs args) {

			KinectPointerPoint kinectPointerPoint = args.CurrentPoint;
			if (lastTime == TimeSpan.Zero || lastTime != kinectPointerPoint.Properties.BodyTimeCounter) {
				lastTime = kinectPointerPoint.Properties.BodyTimeCounter;
				mainScreen.Children.Clear();
				//mainScreen.Children.Add(manoLeft);
				mainScreen.Children.Add(imgHandRight);
			}

			RenderPointer(kinectPointerPoint.Position,
				kinectPointerPoint.Properties.UnclampedPosition,
				kinectPointerPoint.Properties.HandReachExtent,
				kinectPointerPoint.Properties.BodyTimeCounter,
				kinectPointerPoint.Properties.BodyTrackingId,
				kinectPointerPoint.Properties.HandType);
		}
        
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e) {

			bool existeFirstTrack = false;
			var reference = e.FrameReference.AcquireFrame();
			using (var frame = reference.BodyFrameReference.AcquireFrame()) {
				if (frame != null) {
					bodies = new Body[frame.BodyFrameSource.BodyCount];

					frame.GetAndRefreshBodyData(bodies);
					foreach (var body in bodies) {
                        //Aqui se validaria si existe un cuerpo frente a la Kinect y si el mismo esta siendo trackeado
						if (body != null && body.IsTracked && body.TrackingId == firstTrackId) {
							CameraSpacePoint position = body.Joints[JointType.Neck].Position;
							DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                            initMove = (int)depthSpacePoint.Y - 150;
                            endMove = (int)depthSpacePoint.Y + 250;
							
							lineas.Children.Clear();

							existeFirstTrack = true;
							stopGiroAuto();
							break;
						}
					}
				}
			}
			if (!existeFirstTrack) {
				if (stopMove == true) {
					startGiroAuto();
				}
				firstTrackId = 0;
			}
		}

		private void AvailableChanged(object sender, IsAvailableChangedEventArgs e) {

            Console.WriteLine("Sensor: " + (e.IsAvailable ? "On" : "Off"));

        }

        /// <summary>
        /// Evento que determina que accion tomar cuando el usuario pulsa una tecla
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ventana_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                App.Current.Shutdown();
            }
        }
        
        #endregion

        #region "Metodos"

        /// <summary>
        /// Inicializa los parametros de la Ventana que contiene el canvas de imagenes y se definen los valores para el cursor y la deteccion de posicion del mismo respecto a la ventana.
        /// </summary>
        private void IniciatizeControls()
        {
            InitializeComponent();

            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;

            ventana.Background = Brushes.Black;
            ventana.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
            ventana.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            HeightScreen = (int)ventana.Height;
            WidthScreen = (int)ventana.Width;

            imgHandLeft = new Image();
            imgHandLeft.Source = new BitmapImage(Utilities.LoadUriFromResource("Cursores/manoLeft.png"));
            imgHandLeft.Width = 32;
            imgHandLeft.Height = 32;

            imgHandRight = new Image();
            imgHandRight.Source = new BitmapImage(Utilities.LoadUriFromResource("Cursores/manoRight.png"));
            imgHandRight.Width = 32;
            imgHandRight.Height = 32;

            mainScreen.Children.Add(imgHandLeft);
            mainScreen.Children.Add(imgHandRight);

            sensor = KinectSensor.GetDefault();
            sensor.Open();
            sensor.IsAvailableChanged += AvailableChanged;

            this.coordinateMapper = sensor.CoordinateMapper;
            
            //para detectar si se fue el id trackeado
            reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.BodyIndex | FrameSourceTypes.Color | FrameSourceTypes.Depth);
            reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            //se llama cuando se mueven las manos de un body trackeado
            KinectCoreWindow kinectCoreWindow = KinectCoreWindow.GetForCurrentThread();
            kinectCoreWindow.PointerMoved += kinectCoreWindow_PointerMoved;

            //vista360.Children.Clear();  //Limpia por completo el canvas que contiene las imagenes
        }

        private void LoadImages()
        {
            try
            {
                String baseURL = AppDomain.CurrentDomain.BaseDirectory;

                //Carga todas las carpetas contenidas en DATA\IMAGES
                string[] subdirectoryEntries = Directory.GetDirectories(@"data\images\");

                if (lstGallery == null)
                    lstGallery = new Gallery<Image>[subdirectoryEntries.Length];

                if (GalleriesSecuence == null)
                    GalleriesSecuence = new int[subdirectoryEntries.Length];   //Se asigna el total de carpetas leidas a array para ordenar las galerias de imagenes

                //Se recorre cada directorio
                subdirectoryEntries.ForEachWithIndex((subdirectory, index) =>
                {
                    string[] fileEntries = Directory.GetFiles(baseURL + subdirectory);

                    GalleriesSecuence[index] = index - 1;
                    Gallery<Image> objGallery = new Gallery<Image>();
                    Image[] lstImages = new Image[fileEntries.Length];
                    Image objThumbnail = new Image();

                    //y luego, recorremos cada archivo de la subcarpeta
                    fileEntries.ForEachWithIndex((fileName, idx) =>
                    {
                        var file = System.IO.Path.GetFileName(fileName);
                        objGallery.setFolder(baseURL + subdirectory);
                        //Con la primera imagen hacemos el thumbnail de la libreria
                        if (idx == 0)
                        {
                            objThumbnail.Source = new BitmapImage(Utilities.LoadUriImageUrl(baseURL, subdirectory, file));
                            objThumbnail.Width = thumbnailWidth;
                            objThumbnail.Height = thumbnailHeight;
                            objThumbnail.MaxWidth = thumbnailWidth;
                            objThumbnail.MaxHeight = thumbnailHeight;
                        }
                        lstImages[idx] = new Image();
                        lstImages[idx].Source = new BitmapImage(Utilities.LoadUriImageUrl(baseURL, subdirectory, file));
                        lstImages[idx].Width = WidthScreen;
                        lstImages[idx].Height = HeightScreen - thumbnailHeight;
                        Canvas.SetTop(lstImages[idx], thumbnailHeight + 10);    //El valor 10 lo coloque para darle un poco de espaciado respecto a la tira de thumbnails
                        Canvas.SetLeft(lstImages[idx], 0);
                    });
                    //Asignacion de valores de la imagen y guardado dentro de la lista de imagenes
                    objGallery.setQtyImage(fileEntries.Length);
                    objGallery.setThumbnail(objThumbnail);
                    objGallery.setImages(lstImages);
                    lstGallery[index] = objGallery;
                });

                GalleriesLoaded += subdirectoryEntries.Length;
                
            }
            catch (Exception objException)
            {
                Console.WriteLine(objException.Message);
            }
        }

        private void LoadVideos()
        {
            try
            {
                String baseURL = AppDomain.CurrentDomain.BaseDirectory + "data\\videos\\";

                //Carga los archivos contenidos en la carpeta de videos
                string[] fileEntries = Directory.GetFiles(baseURL);
                int sizeGallery = lstGallery.Length;

                if (lstGallery == null)
                    lstGallery = new Gallery<Image>[fileEntries.Length];
                else
                    Array.Resize(ref lstGallery, lstGallery.Length + fileEntries.Length);

                if (GalleriesSecuence == null)
                    GalleriesSecuence = new int[fileEntries.Length];   //Se asigna el total de archivos leidos a array para ordenar las galerias de videos
                else
                    Array.Resize(ref GalleriesSecuence, GalleriesSecuence.Length + fileEntries.Length);

                
                fileEntries.ForEachWithIndex((fileName, idx) =>
                {
                    var file = System.IO.Path.GetFileName(fileName);
                    Gallery<Image> objGalleryVideo = new Gallery<Image>();
                    Image[] lstImages = new Image[maxFrames]; 
                    Image objThumbnail = new Image();
                    BitmapImage videoFrame = new BitmapImage();
                    objGalleryVideo.setFolder(baseURL);
                    GalleriesSecuence[sizeGallery + idx] = sizeGallery + idx - 1;

                    //Lectura del video
                    VideoFileReader reader = new VideoFileReader();
                    reader.Open(baseURL + file);
                    //Esto lo usaremos para conocer en que frame estamos. Dentro del ciclo, cada vez que esta variable llegue al mismo valor de reader.FrameRate(fps), sabemos que estamos en un segundo de la pelicula
                    int frameIndex = 0; 
                    int timeCurrent = 0;    //segundos
                    int i = 0;
                    
                    while (true)
                    {
                        var objFrame = reader.ReadVideoFrame();

                        //Si no hay mas frame por leer o si se llego al limite de segundos de captura (1 frame = 1 segundo), salimos
                        if ((objFrame == null) || (timeCurrent == maxFrames))
                            break;

                        //Si esto se cumple, indica que estamos en un segundo de la pelicula (omitimos los frames dentro del segundo)
                        if ((frameIndex == reader.FrameRate / 2) && (i < maxFrames))
                        {
                            videoFrame = Bitmap2BitmapImage(objFrame);

                            if (i == 5) //Para tomar como thumbnail el frame del segundo 5 (A VECES EL PRIMER FRAME ESTA EN NEGRO)
                            {
                                objThumbnail.Source = videoFrame;
                                objThumbnail.Width = thumbnailWidth;
                                objThumbnail.Height = thumbnailHeight;
                                objThumbnail.MaxWidth = thumbnailWidth;
                                objThumbnail.MaxHeight = thumbnailHeight;
                            }

                            lstImages[i] = new Image();
                            lstImages[i].Source = videoFrame;
                            lstImages[i].Width = WidthScreen;
                            lstImages[i].Height = HeightScreen - thumbnailHeight;
                            Canvas.SetTop(lstImages[i], thumbnailHeight + 10);    //El valor 10 lo coloque para darle un poco de espaciado respecto a la tira de thumbnails
                            Canvas.SetLeft(lstImages[i], 0);
                            ++i;
                        }

                        objFrame.Dispose();
                        ++frameIndex;

                        if (frameIndex > reader.FrameRate / 2)
                        {
                            frameIndex = 0;
                            ++timeCurrent;
                        }
                    }

                    objGalleryVideo.setQtyImage(maxFrames);
                    objGalleryVideo.setThumbnail(objThumbnail);
                    objGalleryVideo.setImages(lstImages);
                    lstGallery[sizeGallery + idx] = objGalleryVideo;

                    reader.Close();
                    
                });
                
                GalleriesLoaded += fileEntries.Length;
            }
            catch (Exception objException)
            {
                Console.WriteLine(objException.Message);
            }
        }

        private void startGiroAuto() {
			
			stopMove = false;
			if (thMoverAuto == null || !thMoverAuto.IsAlive) { 
				thMoverAuto = new Thread(this.girarAuto);
				thMoverAuto.Start();
			}
		}

        /// <summary>
        /// Metodo que realiza el cambio de imagen del automovil
        /// </summary>
		public void girarAuto() {
			while (!stopMove) {
				TimeSpan timer = new TimeSpan(DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second, DateTime.UtcNow.Millisecond);
				if (giroAutoTimer.TotalSeconds + 0.1 <= timer.TotalSeconds) {
					this.Dispatcher.Invoke((Action)(() => {
						vista360.Children.Clear();
						Image[] imgs = lstGallery[autoActual].getImages();
						//vista360.Children.Add(imgs[contImgGiro]);
						contImgGiro++;
                        if (contImgGiro >= lstGallery[autoActual].getQtyImages())
                        {
							contImgGiro = 0;
						}
					}));
					giroAutoTimer = timer;
				}
			}
		}

		public void stopGiroAuto() {
			stopMove = true;
		}

        private void RenderPointer(PointF position, PointF unclampedPosition, float handReachExtent, TimeSpan timeCounter, ulong trackingId, HandType handType)
        {

            if (trackingId > 0 && firstTrackId == 0)
            {
                firstTrackId = trackingId;
            }
            if (firstTrackId == trackingId && firstTrackId != 0)
            {
                double posX = unclampedPosition.X * mainScreen.ActualWidth - 60 / 2;
                double posY = unclampedPosition.Y * mainScreen.ActualHeight - 60 / 2;

                Canvas.SetLeft(handType.ToString() == "LEFT" ? imgHandLeft : imgHandRight, posX);
                Canvas.SetTop(handType.ToString() == "LEFT" ? imgHandLeft : imgHandRight, posY);

                int manoID = handType.ToString() == "LEFT" ? HandLeftId : HandRightId;

                if (manoID == HandRightId)
                {
                    bool encontro = false;
                    int offsetX = (WidthScreen - GetWithTotalThumbnails()) / 2;

                    for (int x = 0; x < GalleriesLoaded; x++)
                    {
                        int i = GalleriesSecuence[x];
                        int offset = offsetX + i * (thumbnailWidth + 10);
                        if (posX > offset && posX < offset + thumbnailWidth && posY > 0 && posY < thumbnailHeight)
                        {
                            if (thumbnailWhere == x)
                            {
                                thumbnailCounter++;
                                if (thumbnailCounter >= 30)
                                {
                                    vista360.Children.Clear();
                                    GalleriesSecuence[autoActual] = GalleriesSecuence[x];
                                    autoActual = x;
                                    GalleriesSecuence[x] = -1;
                                    vista360.Children.Add(lstGallery[autoActual].getImages()[0]);
                                    thumbnailCounter = 0;
                                    ReorderThumbnails();
                                }
                            }
                            else
                            {
                                thumbnailWhere = x;
                                thumbnailCounter = 1;
                            }
                            encontro = true;
                            break;
                        }
                        //texto.Text = "pos X: " + posX.ToString() + "/ pos Y: " + posY.ToString() + " / OffSet: " + offset.ToString();
                    }
                    if (!encontro)
                    {
                        thumbnailCounter = 0;
                        thumbnailWhere = -1;
                    }

                }

                int cantImg = lstGallery[autoActual].getQtyImages();
                //Aqui se determina que imagen se va a tomar del Array. Esto basado en la posicion en X y considerando que se toma el doble de imagenes
                //Se compara ahora la cantidad de imagenes respecto al total de maximo de frames... Esto es porque si son muchos frames, no es necesario multiplicar la cantidad de imagenes para dar el efecto
                //de movimiento. Para los casos de Videos, seria bueno tener mas frames pero hay que tener cuidado con la memoria del sistema
                int imagen = (int)((posX * (cantImg * ( (cantImg < maxFrames) ? 2 : 1 ) ) - 1) / WidthScreen) + 1;
                //texto.Text = "pos X: " + posX.ToString() + "/ pos Y: " + posY.ToString() + " / Image: " + imagen.ToString();

                if (imagen > cantImg)
                {
                    imagen -= cantImg;
                }
                imagen -= 1;

                if (imagen < 0 || imagen > cantImg - 1)
                {
                    //texto.Content += " NULL";
                }
                else if (posY > initMove && posY < endMove)
                {
                    vista360.Children.Clear();
                    vista360.Children.Add(lstGallery[autoActual].getImages()[imagen]);
                    ReorderThumbnails();
                }
            }
        }

		private void ReorderThumbnails() {
		
            int anchoT = GetWithTotalThumbnails();
			int offsetX = (WidthScreen - anchoT) / 2;
		
			for (int i = 0; i < GalleriesLoaded; i++) {
				int x = GalleriesSecuence[i];
                if (i == autoActual || lstGallery[i] == null)
                {
					continue;
				}
                Image tb = lstGallery[i].getThumbnail();
				vista360.Children.Add(tb);
                Canvas.SetLeft(tb, offsetX + (x * (DistanceBetweenImg + thumbnailWidth)));
			}
		}

        private int GetWithTotalThumbnails()
        {
            //El ancho total de los thunbnails no puede ser fijo. Hay que recorrer el Array que contiene cada libreria y estimar el ancho de cada thumbnail
            return (thumbnailWidth * (GalleriesLoaded - 1) + ((GalleriesLoaded - 1) * DistanceBetweenImg));
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
            }
            finally
            {
                DeleteObject(hBitmap);
            }

            return retval;
        }
        #endregion

	}
}
