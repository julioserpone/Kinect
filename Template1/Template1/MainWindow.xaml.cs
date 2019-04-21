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

namespace Template1 {
	/// <summary>
	/// Lógica de interacción para MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private ImageBrush fondoTemplate;
		private BitmapImage iconoTemplate;
		public MainWindow() {
			InitializeComponent();
			fondo1.Source = Extensions.LoadBitmapFromResource("Fondos/47streetFondo.jpg");
			fondo2.Source = Extensions.LoadBitmapFromResource("Fondos/nike_fondo.jpg");
			fondo3.Source = Extensions.LoadBitmapFromResource("Fondos/fravegaFondo.jpg");
			fondo4.Source = Extensions.LoadBitmapFromResource("Fondos/4.jpg");
			fondo5.Source = Extensions.LoadBitmapFromResource("Fondos/5.jpeg");

			selecIcono1.Source = Extensions.LoadBitmapFromResource("Iconos/adidas.png");
			selecIcono2.Source = Extensions.LoadBitmapFromResource("Iconos/iconoFravega.png");
			selecIcono3.Source = Extensions.LoadBitmapFromResource("Iconos/hojas.png");
			selecIcono4.Source = Extensions.LoadBitmapFromResource("Iconos/nike.png");
			selecIcono5.Source = Extensions.LoadBitmapFromResource("Iconos/opera.png");

			this.Background = new ImageBrush(Extensions.LoadBitmapFromResource("Fondos/fondo.png"));

			//onclic de fondos
			fondoBoton1.AddHandler(TabItem.MouseDownEvent, new RoutedEventHandler(onMouseDownFondo1), true);
			fondoBoton2.AddHandler(TabItem.MouseDownEvent, new RoutedEventHandler(onMouseDownFondo2), true);
			fondoBoton3.AddHandler(TabItem.MouseDownEvent, new RoutedEventHandler(onMouseDownFondo3), true);
			fondoBoton4.AddHandler(TabItem.MouseDownEvent, new RoutedEventHandler(onMouseDownFondo4), true);
			fondoBoton5.AddHandler(TabItem.MouseDownEvent, new RoutedEventHandler(onMouseDownFondo5), true);

			//onclic de iconos
			iconoBoton1.AddHandler(TabItem.MouseDownEvent, new RoutedEventHandler(onMouseDownIcono1), true);
			iconoBoton2.AddHandler(TabItem.MouseDownEvent, new RoutedEventHandler(onMouseDownIcono2), true);
			iconoBoton3.AddHandler(TabItem.MouseDownEvent, new RoutedEventHandler(onMouseDownIcono3), true);
			iconoBoton4.AddHandler(TabItem.MouseDownEvent, new RoutedEventHandler(onMouseDownIcono4), true);
			iconoBoton5.AddHandler(TabItem.MouseDownEvent, new RoutedEventHandler(onMouseDownIcono5), true);

			Iniciar.AddHandler(TabItem.MouseDownEvent, new RoutedEventHandler(iniciarTemplate), true);

			//valores por defecto
			fondoTemplate = new ImageBrush(fondo1.Source);
			iconoTemplate = (BitmapImage)selecIcono1.Source;
		}
		public void iniciarTemplate(object sender, RoutedEventArgs args) {

			windowTemplate template = new windowTemplate();
			template.Show();
		}
		// init negrada
		public void onMouseDownFondo1(object sender, RoutedEventArgs args) {
			onMouseDownFondo(fondoBoton1);
		}
		public void onMouseDownFondo2(object sender, RoutedEventArgs args) {
			onMouseDownFondo(fondoBoton2);
		}
		public void onMouseDownFondo3(object sender, RoutedEventArgs args) {
			onMouseDownFondo(fondoBoton3);
		}
		public void onMouseDownFondo4(object sender, RoutedEventArgs args) {
			onMouseDownFondo(fondoBoton4);
		}
		public void onMouseDownFondo5(object sender, RoutedEventArgs args) {
			onMouseDownFondo(fondoBoton5);
		}
		public void onMouseDownIcono1(object sender, RoutedEventArgs args) {
			onMouseDownIcono(iconoBoton1);
		}
		public void onMouseDownIcono2(object sender, RoutedEventArgs args) {
			onMouseDownIcono(iconoBoton2);
		}
		public void onMouseDownIcono3(object sender, RoutedEventArgs args) {
			onMouseDownIcono(iconoBoton3);
		}
		public void onMouseDownIcono4(object sender, RoutedEventArgs args) {
			onMouseDownIcono(iconoBoton4);
		}
		public void onMouseDownIcono5(object sender, RoutedEventArgs args) {
			onMouseDownIcono(iconoBoton5);
		}
		// end negrada

		public void onMouseDownFondo(Button Fondo) {
			Image child = Fondo.GetChildOfType<Image>();
			if (child != null) {
				fondoTemplate = new ImageBrush(child.Source);
				Extensions.fondoTemplate = fondoTemplate;
			}
		}
		public void onMouseDownIcono(Button Icono) {
			Image child = Icono.GetChildOfType<Image>();
			iconoTemplate = (BitmapImage)child.Source;
			Extensions.iconoTemplate = iconoTemplate;
		}
	}

	public static class Extensions {
		public static ImageBrush fondoTemplate;
		public static BitmapImage iconoTemplate;
		public static BitmapImage LoadBitmapFromResource(string pathInApplication, Assembly assembly = null) {
			if (assembly == null) {
				assembly = Assembly.GetCallingAssembly();
			}

			if (pathInApplication[0] == '/') {
				pathInApplication = pathInApplication.Substring(1);
			}
			return new BitmapImage(new Uri(@"pack://application:,,,/" + assembly.GetName().Name + ";component/" + pathInApplication, UriKind.Absolute));
		}

		public static T GetChildOfType<T>(this DependencyObject depObj)
	where T : DependencyObject {
			if (depObj == null) return null;

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++) {
				var child = VisualTreeHelper.GetChild(depObj, i);

				var result = (child as T) ?? GetChildOfType<T>(child);
				if (result != null) return result;
			}
			return null;
		}

		public static Image getChild(Button parent, int pos, Label test){
			if (parent == null) {
				return null;
			}
			var child = VisualTreeHelper.GetChild(parent, pos);
			if (child != null) {
				test.Content = "not null";
			}
			Image foundChild = child as Image;
			return foundChild;
		}
	}


}
