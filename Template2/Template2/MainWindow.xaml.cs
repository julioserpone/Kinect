using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
using System.Windows.Interop;
using System.IO;
using Trumix.Library;

namespace Template2 {

	public partial class MainWindow : Window
    {
        #region "Constante y enumerados"
        const int WithSideSection = 5;
        public enum SectionsPages
        {
            OUT = 0,
            WINDOW = 1,
            LEFT = 2,
            RIGHT = 3
        }
        public enum HandTypeEnum
        {
            LEFT = 0,
            RIGHT = 1
        }
        #endregion

        #region "Propiedades"

        public SectionsPages PositionActual { get; set; }
        private SectionsPages IsSidePage { get; set; }
        public HandTypeEnum HandTypeID { get; set; }
        private ImageProcessing workerObject;
        private Thread workerThread;
        private KinectSensor sensor;
        private CoordinateMapper coordinateMapper = null;
        private MultiSourceFrameReader reader;
        private IList<Body> bodies;
        private TimeSpan lastTime;
        private Image imgHandRight = null;
        private Image imgHandLeft = null;
        private BitmapImage[] PagesActives;
        private BitmapImage[] Pages;
        private ulong firstTrackId;
        private double PosX;
        private double PosY;
        private int PagesNumbers;
        public int PageActual { get; set; }
        private bool FirstExecution;
        private bool IsChangingPage;

        #endregion

        #region "Constructor"

        public MainWindow() {

            IniciatizeControls();

            //Lanzamiento del hilo
            workerObject = new ImageProcessing();
            workerThread = new Thread(() => workerObject.DoWork(ref Pages, ref PagesNumbers));
            workerThread.Start();
		}

        #endregion

        #region "Eventos"

        private void MouseEventHandler(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(mainWindow);
            PointF point = new PointF();
            point.X = (float)((p.X + 60 / 2) / mainScreen.ActualWidth);
            point.Y = (float)((p.Y + 60 / 2) / mainScreen.ActualHeight);

            TimeSpan time = new TimeSpan(DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second, DateTime.UtcNow.Millisecond);
            firstTrackId = 23842342342;

            if (lastTime == TimeSpan.Zero || lastTime != time)
            {
                lastTime = time;
                mainScreen.Children.Clear();
                //mainScreen.Children.Add(manoLeft);
                mainScreen.Children.Add(imgHandRight);
            }
            RenderPointer(point, point, 0, time, firstTrackId, HandType.RIGHT);
        }

        /// <summary>
        /// Evento que determina que accion tomar cuando el usuario pulsa una tecla
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                workerObject.RequestStop();
                
                Console.WriteLine("main thread: Worker thread has terminated.");
                App.Current.Shutdown();
            }
        }

        private void kinectCoreWindow_PointerMoved(object sender, KinectPointerEventArgs args) {

			KinectPointerPoint kinectPointerPoint = args.CurrentPoint;
			if (lastTime == TimeSpan.Zero || lastTime != kinectPointerPoint.Properties.BodyTimeCounter) {
				lastTime = kinectPointerPoint.Properties.BodyTimeCounter;
				mainScreen.Children.Clear();
				//mainScreen.Children.Add(imgHandLeft);
				mainScreen.Children.Add(imgHandRight);
			}

			RenderPointer(kinectPointerPoint.Position,
				kinectPointerPoint.Properties.UnclampedPosition,
				kinectPointerPoint.Properties.HandReachExtent,
				kinectPointerPoint.Properties.BodyTimeCounter,
				kinectPointerPoint.Properties.BodyTrackingId,
				kinectPointerPoint.Properties.HandType);
		}

		private void RenderPointer(PointF position, PointF unclampedPosition, float handReachExtent, TimeSpan timeCounter, ulong trackingId, HandType handType) {

            try
            {
                if (trackingId > 0 && firstTrackId == 0) {
				    firstTrackId = trackingId;
			    }
			    if (firstTrackId == trackingId && firstTrackId != 0) {
				    PosX = unclampedPosition.X * mainScreen.ActualWidth - 60 / 2;
				    PosY = unclampedPosition.Y * mainScreen.ActualHeight - 60 / 2;
				    Canvas.SetLeft(handType.ToString() == "LEFT" ? imgHandLeft : imgHandRight, PosX);
				    Canvas.SetTop(handType.ToString() == "LEFT" ? imgHandLeft : imgHandRight, PosY);
                    HandTypeID = handType.ToString() == "LEFT" ? HandTypeEnum.LEFT : HandTypeEnum.RIGHT;

				    int scrnH = (int)System.Windows.SystemParameters.PrimaryScreenHeight;
				    int scrnW = (int)System.Windows.SystemParameters.PrimaryScreenWidth;

                    //Si ya se cargaron las imagenes desde el Thread (falta mostrar algo que diga CARGANDO IMAGENES)
                    if (workerObject.IsLoadPages)
                    {
                        if (FirstExecution)
                        {
                            loading.Visibility = System.Windows.Visibility.Hidden;
                            ChangePages(1, Convert.ToInt32(PosX), true, IsSidePage);
                            FirstExecution = false;
                        }

                        //Si estoy en cualquiera de los laterales
                        if (PositionActual == SectionsPages.LEFT || PositionActual == SectionsPages.RIGHT)
                        {
                            IsSidePage = PositionActual;
                        }

                        if (HandTypeID == HandTypeEnum.RIGHT)
                        {
                            //Estoy dentro de la seccion lateral izquierda
                            if (PosX >= 0 && PosX <= WithSideSection && PosY >= 0 && PosY <= scrnH)
                            {
                                //izquierdo
                                if (IsChangingPage)
                                {
                                    //Esto hace que solo se cambie de pagina luego de haber pasado la pagina por completo, es decir, vengo del lado derecho al izquierdo.
                                    if (IsSidePage == SectionsPages.RIGHT)
                                    {
                                        //Si la mano esta por debajo de la mitad de la pantalla, dejo fija las paginas y cancelo el efecto de cambio de pagina
                                        if (PosY > scrnH / 2)
                                        {
                                            IsChangingPage = false;
                                            ChangePages(PageActual, Convert.ToInt32(PosX), true, IsSidePage);
                                            IsSidePage = SectionsPages.WINDOW;
                                        }
                                    }
                                }
                                else
                                {
                                    //Si la mano esta por encima de la mitad de la pantalla, hago el cambio de posicion para posteriormente aplicar el efecto
                                    if (PosY < scrnH / 2) PositionActual = SectionsPages.LEFT;
                                }

                            }
                            //O del lateral derecho
                            else if (PosX >= scrnW - WithSideSection && PosX <= scrnW && PosY >= 0 && PosY <= scrnH)
                            {
                                //derecho
                                if (IsChangingPage)
                                {
                                    //Esto hace que solo se cambie de pagina luego de haber pasado la pagina por completo, es decir, vengo del lado izquierda al derecho.
                                    if (IsSidePage == SectionsPages.LEFT) 
                                    {
                                        //Si la mano esta por debajo de la mitad de la pantalla, dejo fija las paginas y cancelo el efecto de cambio de pagina
                                        if (PosY > scrnH / 2)
                                        {
                                            IsChangingPage = false;
                                            ChangePages(PageActual, Convert.ToInt32(PosX), true, IsSidePage);
                                            IsSidePage = SectionsPages.WINDOW;
                                        }
                                    }
                                }
                                else
                                {
                                    //Si la mano esta por encima de la mitad de la pantalla, hago el cambio de posicion para posteriormente aplicar el efecto
                                    if (PosY < scrnH / 2) PositionActual = SectionsPages.RIGHT;
                                }
                            }
                            //Ejecuto el cambio de pagina luego que me salgo del sector izquierdo o derecho
                            else if (PosX >= WithSideSection && PosX <= scrnW - WithSideSection && PosY >= 0 && PosY <= scrnH)
                            {
                                if (!IsChangingPage)
                                {
                                    if (IsSidePage == SectionsPages.RIGHT)
                                    {
                                        if (PageActual + 2 < PagesNumbers)
                                        {
                                            PageActual += 2;
                                            IsChangingPage = true;
                                        }
                                    }
                                    else if (IsSidePage == SectionsPages.LEFT)
                                    {
                                        if (PageActual - 2 >= 1)
                                        {
                                            PageActual -= 2;
                                            IsChangingPage = true;
                                        }
                                    }
                                }

                                PositionActual = SectionsPages.WINDOW;

                                //Si ya estuve en algunos de los laterales y luego me salgo de esa zona y el flag IschanginPage es true, aplico efecto de cambio de pagina
                                if ((PositionActual == SectionsPages.WINDOW) && (IsChangingPage))
                                {
                                    ChangePages(PageActual, Convert.ToInt32(PosX), false, IsSidePage);
                                }
                            }
                            else
                            {
                                PositionActual = SectionsPages.OUT;
                            }
                        }

                    }

                    texto.Text = "pos X: " + PosX.ToString() + "/ pos Y: " + PosY.ToString() + " / POSITION (" + PositionActual.ToString() + ") / Pagina: " + PageActual.ToString() + " / ChangingPage: " + IsChangingPage.ToString() + " / Side: " + IsSidePage.ToString();

			    }
            }
            catch (Exception ex)
            {
                
                throw;
            }
			
		}

		private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e) {

			bool existeFirstTrack = false;
			var reference = e.FrameReference.AcquireFrame();
			using (var frame = reference.BodyFrameReference.AcquireFrame()) {
				if (frame != null) {
					bodies = new Body[frame.BodyFrameSource.BodyCount];

					frame.GetAndRefreshBodyData(bodies);

					foreach (var body in bodies) {
						if (body != null && body.IsTracked && body.TrackingId == firstTrackId) {
							existeFirstTrack = true;
							break;
						}
					}
				}
			}
			if (!existeFirstTrack) {
				firstTrackId = 0;
			}
        }

        #endregion

        #region "Metodos"

        private void IniciatizeControls()
        {
            InitializeComponent();

            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;

            mainWindow.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
            mainWindow.Width = System.Windows.SystemParameters.PrimaryScreenWidth;

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

            IsSidePage = SectionsPages.OUT;
            PageActual = 1;
            PagesActives = new BitmapImage[2];
            FirstExecution = true;

            sensor = KinectSensor.GetDefault();
            sensor.Open();
            this.coordinateMapper = sensor.CoordinateMapper;

            //se llama cuando se mueven las manos de un body trackeado
            KinectCoreWindow kinectCoreWindow = KinectCoreWindow.GetForCurrentThread();
            kinectCoreWindow.PointerMoved += kinectCoreWindow_PointerMoved;

            reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.BodyIndex | FrameSourceTypes.Color | FrameSourceTypes.Depth);
            reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
        }

        private void ChangePages(int paginaActual, int posX, bool isPageFixed, SectionsPages Position)
        {
			// borramos las anteriores
            cnvPaginas.Children.Clear();

            try
            {
                if (isPageFixed)
                {
                    //Pagina izquierda
                    Image PageLeft = new Image();
                    PageLeft.Source = Pages[paginaActual - 1];
                    PageLeft.Width = mainWindow.Width / 2;
                    PageLeft.Height = mainWindow.Height;
                    PageLeft.Stretch = Stretch.Fill;

                    Canvas.SetTop(PageLeft, 0);
                    Canvas.SetLeft(PageLeft, 0);
                    cnvPaginas.Children.Add(PageLeft);

                    // pagina derecha
                    Image PageRight = new Image();
                    PageRight.Source = Pages[paginaActual];
                    PageRight.Width = mainWindow.Width / 2;
                    PageRight.Height = mainWindow.Height;
                    PageRight.Stretch = Stretch.Fill;

                    Canvas.SetTop(PageRight, 0);
                    Canvas.SetLeft(PageRight, mainWindow.Width / 2);
                    cnvPaginas.Children.Add(PageRight);
                }
                else
                {
                    //Hago el cambio de pagina segun posicion de la mano en eje X

                    //Pagina izquierda
                    //Si hago el cambio de pagina y la pagina alcanza a mostrarse por completo, debo mostrar detras de ella la pagina que se supone esta al lado de la que esa en movimiento
                    int PageNumberLeft;
                    if (Position == SectionsPages.LEFT)
                    {
                        PageNumberLeft = (posX < mainWindow.Width / 2) ? paginaActual + 1: paginaActual - 1;
                    }
                    else
                    {
                        PageNumberLeft = paginaActual - 3;
                    }
                    Image PageLeft = new Image();
                    PageLeft.Source = Pages[PageNumberLeft];
                    PageLeft.Width = mainWindow.Width / 2;
                    PageLeft.Height = mainWindow.Height;
                    PageLeft.Stretch = Stretch.Fill;

                    Canvas.SetTop(PageLeft, 0);
                    Canvas.SetLeft(PageLeft, 0);
                    cnvPaginas.Children.Add(PageLeft);

                    // pagina derecha
                    //Si hago el cambio de pagina y la pagina alcanza a mostrarse por completo, debo mostrar detras de ella la pagina que se supone esta al lado de la que esa en movimiento
                    int PageNumberRight;
                    if (Position == SectionsPages.RIGHT)
                    {
                        PageNumberRight = (posX > mainWindow.Width / 2) ? paginaActual - 2 : paginaActual;
                    }
                    else
                    {
                        PageNumberRight = paginaActual + 2;
                    }
                    
                    Image PageRight = new Image();
                    PageRight.Source = Pages[PageNumberRight];
                    PageRight.Width = mainWindow.Width / 2;
                    PageRight.Height = mainWindow.Height;
                    PageRight.Stretch = Stretch.Fill;

                    Canvas.SetTop(PageRight, 0);
                    Canvas.SetLeft(PageRight, mainWindow.Width / 2);
                    cnvPaginas.Children.Add(PageRight);

                    //Pagina con efecto (el efecto lo hace realmente es el cambio de posicion en X)
                    int PageNumberChange;
                    if (Position == SectionsPages.RIGHT)
                    {
                        PageNumberChange = paginaActual - 1;
                    }
                    else
                    {
                        PageNumberChange = paginaActual;
                    }
                    Image PageChanging = new Image();
                    PageChanging.Source = Pages[PageNumberChange];
                    PageChanging.Width = mainWindow.Width / 2;
                    PageChanging.Height = mainWindow.Height;
                    PageChanging.Stretch = Stretch.Fill;

                    Canvas.SetTop(PageChanging, 0);
                    //La posicion de la pagina dependera del sentido en el eje X. Es decir, si vengo del lado izquierdo, coloco la pagina al lado izquierdo de la mano. Si vengo del lado derecho, coloco la pagina del lado derecho de la mano
                    Canvas.SetLeft(PageChanging, (Position == SectionsPages.RIGHT) ? posX : (posX - mainWindow.Width / 2));
                    cnvPaginas.Children.Add(PageChanging);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
		}

        #endregion
    }

}
