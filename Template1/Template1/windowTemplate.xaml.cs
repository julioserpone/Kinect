using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Globalization;

namespace Template1 {
	/// <summary>
	/// Lógica de interacción para windowTemplate.xaml
	/// </summary>
	public partial class windowTemplate : Window {
		
		ulong firstTrackId;
		Image[] iconos = null;
		KinectSensor sensor;
		private MultiSourceFrameReader reader;
		private IList<Body> bodies;
		private Image manoRight = null;
		private Image manoLeft = null;
		private TimeSpan lastTime;
		double anchoRes;
		double altoRes;
		int anchoLogo = 32;
		int altoLogo = 32;
		int cuadColum = 8;
		int cuadFila = 6;
		int cantidadIconos = 800;
		int tipoBarrido = 0;
		Image[] manoIcoPointer = null;
		int manoLeftID = 0;
		int manoRightID = 1;
		Boolean[] vientoActivado = null;
		double[] timeMano = null;
		Point[] posInicialMano = null;

		public windowTemplate() {
			InitializeComponent();

			if (Extensions.fondoTemplate == null) {
				Extensions.fondoTemplate = new ImageBrush(Extensions.LoadBitmapFromResource("Fondos/1.jpg"));
			}
			if (Extensions.iconoTemplate == null) {
				Extensions.iconoTemplate = Extensions.LoadBitmapFromResource("Iconos/hojas.png");
			}
			altoLogo = Extensions.iconoTemplate.PixelHeight / 2;
			anchoLogo = Extensions.iconoTemplate.PixelWidth / 2;
			WindowState = WindowState.Maximized;
			WindowStyle = WindowStyle.None;

			sensor = KinectSensor.GetDefault();
			sensor.Open();
			sensor.IsAvailableChanged += AvailableChanged;

			mainWindow.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
			mainWindow.Width = System.Windows.SystemParameters.PrimaryScreenWidth;


			//para detectar si se fue el id trackeado
			reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.BodyIndex | FrameSourceTypes.Color | FrameSourceTypes.Depth);
			reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

			//se llama cuando se mueven las manos de un body trackeado
			KinectCoreWindow kinectCoreWindow = KinectCoreWindow.GetForCurrentThread();
			kinectCoreWindow.PointerMoved += kinectCoreWindow_PointerMoved;

			iconos = new Image[cantidadIconos];
			manoIcoPointer = new Image[2];
			vientoActivado = new Boolean[2];
			timeMano = new double[2];
			posInicialMano = new Point[2];

			//this.Background = new ImageBrush(Extensions.LoadBitmapFromResource("fondo.jpg"));
			this.Background = Extensions.fondoTemplate;

			anchoRes = mainWindow.Width;
			altoRes = mainWindow.Height;

			if (cantidadIconos <= 200) {
				cuadColum = 2;
				cuadFila = 2;
			}

			//BitmapImage bmp = Extensions.LoadBitmapFromResource("firefox.png");
			BitmapImage bmp = Extensions.iconoTemplate;
			int cant = 0;
			Random randomX;
			Random randomY;
			int columnaDesde = 0;
			int columnaHasta = cuadColum;
			int filaDesde = 0;
			int filaHasta = cuadFila;
			double anchoCuadricula = anchoRes / columnaHasta;
			double altoCuadricula = altoRes / filaHasta;
			int timestamp = Int32.Parse(DateTime.UtcNow.ToString("ffff", CultureInfo.InvariantCulture));
			randomX = new Random(timestamp);
			randomY = new Random(timestamp * 2);
			Random zIndexRand = new Random(timestamp * 3);

			int cantXCuadrado = (int)(cantidadIconos / (cuadColum * cuadFila));
			//texto.Content = cantXCuadrado;
			for (int i = columnaDesde; i < columnaHasta; i++) {
				for (int j = filaDesde; j < filaHasta; j++) {
					for (int k = 0; k < cantXCuadrado; k++) {
						int intRandX = randomX.Next((int)anchoCuadricula);
						int x = intRandX - (int)(anchoCuadricula / 2);
						int y = randomY.Next((int)altoCuadricula) - (int)(altoCuadricula / 2);
						Image imagen = new Image();
						imagen.Source = bmp;
						imagen.Width = anchoLogo;
						imagen.Height = altoLogo;
						//imagen.Margin = new Thickness(i * anchoCuadricula + (anchoCuadricula/2) + x, j * altoCuadricula + (altoCuadricula/2) + y, 0, 0);
						Canvas.SetLeft(imagen, i * anchoCuadricula + (anchoCuadricula / 2) + x);
						Canvas.SetTop(imagen, j * altoCuadricula + (altoCuadricula / 2) + y);
						iconos[cant] = imagen;
						cant++;
						ventanaIco.Children.Add(imagen);
						Grid.SetZIndex(imagen, zIndexRand.Next(cantidadIconos));
					}
				}
			}

			BitmapImage manoLeftBmp = Extensions.LoadBitmapFromResource("Cursores/manoLeft.png");

			BitmapImage manoRightBmp = Extensions.LoadBitmapFromResource("Cursores/manoRight.png");

			manoLeft = new Image();
			manoLeft.Source = manoLeftBmp;
			manoLeft.Width = 32;
			manoLeft.Height = 32;

			manoRight = new Image();
			manoRight.Source = manoRightBmp;
			manoRight.Width = 32;
			manoRight.Height = 32;

			mainScreen.Children.Add(manoLeft);
			mainScreen.Children.Add(manoRight);
			Grid.SetZIndex(mainScreen, 1000000);
			//Grid.SetZIndex(texto, 10);
			//Grid.SetZIndex(texto, 2000000);
			//Grid.SetZIndex(circle, 2000000);
		}

		private void MouseEventHandler(object sender, MouseEventArgs e) {
			Point p = e.GetPosition(mainWindow);
			//barridoViento(p.X, p.Y);
			PointF point = new PointF();
			point.X = (float)((p.X + 60 / 2) / mainScreen.ActualWidth);
			point.Y = (float)((p.Y + 60 / 2) / mainScreen.ActualHeight);

			TimeSpan time = new TimeSpan(DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second, DateTime.UtcNow.Millisecond);
			firstTrackId = 23842342342;

			if (lastTime == TimeSpan.Zero || lastTime != time) {
				lastTime = time;
				mainScreen.Children.Clear();
				mainScreen.Children.Add(manoLeft);
				mainScreen.Children.Add(manoRight);
			}
			RenderPointer(point, point, 0, time, firstTrackId,HandType.RIGHT);
		}

		private void kinectCoreWindow_PointerMoved(object sender, KinectPointerEventArgs args) {
			KinectPointerPoint kinectPointerPoint = args.CurrentPoint;
			if (lastTime == TimeSpan.Zero || lastTime != kinectPointerPoint.Properties.BodyTimeCounter) {
				//texto.Content += "1";
				lastTime = kinectPointerPoint.Properties.BodyTimeCounter;
				mainScreen.Children.Clear();
				//prueba.Children.Add(texto);
				mainScreen.Children.Add(manoLeft);
				mainScreen.Children.Add(manoRight);
			}
			RenderPointer(kinectPointerPoint.Position,
				kinectPointerPoint.Properties.UnclampedPosition,
				kinectPointerPoint.Properties.HandReachExtent,
				kinectPointerPoint.Properties.BodyTimeCounter,
				kinectPointerPoint.Properties.BodyTrackingId,
				kinectPointerPoint.Properties.HandType);
		}

		private void RenderPointer(PointF position, PointF unclampedPosition, float handReachExtent, TimeSpan timeCounter, ulong trackingId, HandType handType) {
			/*manoLeft.Margin = new Thickness(unclampedPosition.X * prueba2.ActualWidth, unclampedPosition.Y * prueba2.ActualHeight, 0, 0);
			manoRight.Margin = new Thickness(position.X * prueba2.ActualWidth, position.Y * prueba2.ActualHeight, 0, 0);*/
			//texto.Content = "(" + unclampedPosition.X + " * " + mainScreen.ActualWidth + ", " + unclampedPosition.Y + " * " + mainScreen.ActualHeight + ")";
			if (trackingId > 0 && firstTrackId == 0) {
				firstTrackId = trackingId;
			}
			if (firstTrackId == trackingId && firstTrackId != 0) {
				double posX = unclampedPosition.X * mainScreen.ActualWidth - 60 / 2;
				double posY = unclampedPosition.Y * mainScreen.ActualHeight - 60 / 2;
				//texto.Content = "A     ";
				Canvas.SetLeft(handType.ToString() == "LEFT" ? manoLeft : manoRight, posX);
				Canvas.SetTop(handType.ToString() == "LEFT" ? manoLeft : manoRight, posY);
				int manoID = handType.ToString() == "LEFT" ? manoLeftID : manoRightID;
				//vientoActivado[manoID] = true;
				//texto.Content = manoID+" - "+posX+" - "+posY;
				if (tipoBarrido == 2) {

					double distancia = Math.Sqrt(Math.Pow(posX - posInicialMano[manoID].X, 2) + Math.Pow(posY - posInicialMano[manoID].Y, 2));
					vientoActivado[manoID] = (distancia > 100);
					if (vientoActivado[manoID]) {
						Line line = new Line();
						line.Stroke = Brushes.Red;
						line.X1 = posX;
						line.Y1 = posY;
						line.X2 = posInicialMano[manoID].X;
						line.Y2 = posInicialMano[manoID].Y;
						ventanaIco.Children.Add(line);
					}
					if (timeMano[manoID] + 40 <= timeCounter.TotalMilliseconds) {
						timeMano[manoID] = timeCounter.TotalMilliseconds;
						posInicialMano[manoID] = new Point(posX, posY);
					}
					//texto.Content = "     " + vientoActivado[0] + " " + vientoActivado[1] + " " + (float)distancia;
					//texto.Content = "          "+timeCounter.Seconds+" "+timeCounter.TotalMilliseconds;

					if (vientoActivado[manoID] == true) {
						//barridoViento((int)posX, (int)posY);
					}
				} else {
					foreach (Image img in iconos) {
						if (img == null) {
							continue;
						}
						double imgTop = Canvas.GetTop(img);
						double imgLeft = Canvas.GetLeft(img);
						if (tipoBarrido == 0) {
							if (posX + 32 >= imgLeft && posX <= imgLeft + anchoLogo && posY - 32 <= imgTop && posY >= imgTop - altoLogo) {
								ventanaIco.Children.Remove(img);
							}
						} else if (tipoBarrido == 1) {
							if (manoIcoPointer[manoID] == null) {
								if (posX + 32 >= imgLeft && posX <= imgLeft + anchoLogo && posY - 32 <= imgTop && posY >= imgTop - altoLogo) {
									manoIcoPointer[manoID] = img;
								}
							}
						}
					}
					if (tipoBarrido == 1) {
						if (manoIcoPointer[manoID] != null) {
							Canvas.SetLeft(manoIcoPointer[manoID], posX);
							Canvas.SetTop(manoIcoPointer[manoID], posY);
							//texto.Content = "A     " + (int)posX + " <= " + (int)(0 - anchoLogo) + " || " + (int)posX + " >= " + (int)anchoRes + " || " + (int)posY + " <= " + (0 - altoLogo )+ " || " + (int)posY + " >= " + (int)altoRes;

							if (posX <= 0 - anchoLogo || posX >= anchoRes || posY <= 0 - altoLogo || posY >= altoRes) {
								manoIcoPointer[manoID] = null;
							}
						}
					}
				}
			}
		}

		public void barridoViento(double posX, double posY) {
			startPoint.StartPoint = new Point(posX - 100, posY);
			endPoint.Point = new Point(posX + 100, posY);
			Point[] points = new Point[1000];
			int cont = 0;
			for (int hipotenusa = 100; hipotenusa > 0; hipotenusa -= 10) {
				double reducirAngulo = 5;// (hipotenusa / 10) * 1.3 * 5;
				for (double angle = 90; angle >= 0; angle -= reducirAngulo) {
					double adyacente = 0;
					double opuesto = 0;
					double rad = ((angle * Math.PI) / 180) * Math.PI;
					adyacente = Math.Abs(hipotenusa * Math.Cos(rad));
					opuesto = Math.Abs(hipotenusa * Math.Sin(rad));
					points[cont] = new Point(adyacente, opuesto);
					/*Line line = new Line();
					line.Stroke = Brushes.Black;
					line.X1 = posX;
					line.Y1 = posY;
					line.X2 = posX + points[cont].X;
					line.Y2 = posY - points[cont].Y;*/
					Ellipse circulo = new Ellipse();
					circulo.Height = 2;
					circulo.Width = 2;
					circulo.Stroke = Brushes.Blue;
					circulo.StrokeThickness = 5;
					Canvas.SetTop(circulo, posY - opuesto);
					Canvas.SetLeft(circulo, posX + adyacente);
					ventanaIco.Children.Add(circulo);
					//System.Threading.Thread.Sleep(1000);
					//texto.Content = angle + " " + opuesto + " " + adyacente + " " + hipotenusa + " " +line.X1 + " " +line.X2 + " " +line.Y1 + " " +line.Y2;
					/*line = new Line();
					line.Stroke = Brushes.Green;
					line.X1 = points[cont].X + posX;
					line.Y1 = points[cont].Y + posY;
					line.X2 = (double)posX;
					line.Y2 = (double)posY+j;
					ventanaIco.Children.Add(line);

					line = new Line();
					line.Stroke = Brushes.Blue;
					line.X1 = (double)posX;
					line.Y1 = (double)posY + j;
					line.X2 = (double)posX;
					line.Y2 = (double)posY;
					ventanaIco.Children.Add(line);*/
					cont++;
				}
			}
			/*foreach (Image ico in iconos) {
				if (ico == null) {
					break;
				}
				foreach (Point p in points) {
					if (p == null) {
						break;
					}
					double icoX = Canvas.GetLeft(ico);
					double icoY = Canvas.GetTop(ico);
					if (p.X+posX >= icoX && p.X+posX <= icoX + anchoLogo && p.Y+posY <= icoY && p.Y+posY >= icoY - altoLogo) {
						ventanaIco.Children.Remove(ico);
					} else if (posX - p.X >= icoX && posX - p.X <= icoX + anchoLogo && posY - p.Y <= icoY && posY - p.Y >= icoY - altoLogo) {
						ventanaIco.Children.Remove(ico);
					}
				}
			}*/
		}

		private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e) {
			Boolean existeFirstTrack = false;
			var reference = e.FrameReference.AcquireFrame();
			using (var frame = reference.BodyFrameReference.AcquireFrame()) {
				if (frame != null) {
					bodies = new Body[frame.BodyFrameSource.BodyCount];

					frame.GetAndRefreshBodyData(bodies);

					foreach (var body in bodies) {
						if (body != null && body.IsTracked && body.TrackingId == firstTrackId) {
							//texto.Content = "A     "+body.HandLeftState+" - "+body.HandRightState;
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

		private void AvailableChanged(object sender, IsAvailableChangedEventArgs e) {
			if (e.IsAvailable) {
				//sensorStatus.Text = "Sensor: On";
			} else {
				//sensorStatus.Text = "Sensor: Off";
			}
		}
	}
}
