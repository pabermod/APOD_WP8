﻿using APOD.Common;
using APOD.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Net.Http;
using HtmlAgilityPack;
using Windows.UI.Xaml.Media.Imaging;

// La plantilla de aplicación dinámica está documentada en http://go.microsoft.com/fwlink/?LinkID=391641

namespace APOD
{
    public sealed partial class PivotPage : Page
    {
        // Global variable
        private string apodURL = "http://apod.nasa.gov/apod/";
        private string webText;
        private string[] imgTitle;
        private string[] htmlArray;
        private string[] imgarray;
        private messagePop msgPop = new messagePop();

        private const string FirstGroupName = "FirstGroup";
        private const string SecondGroupName = "SecondGroup";

        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");

        public PivotPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

           // getimage(apodURL + "astropix.html");
            DownloadRSS(apodURL + "apod.rss");
        }

        /// <summary>
        /// Obtiene el <see cref="NavigationHelper"/> asociado a esta <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Obtiene el modelo de vista para esta <see cref="Page"/>.
        /// Este puede cambiarse a un modelo de vista fuertemente tipada.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Rellena la página con el contenido pasado durante la navegación. Cualquier estado guardado se
        /// proporciona también al crear de nuevo una página a partir de una sesión anterior.
        /// </summary>
        /// <param name="sender">
        /// El origen del evento; suele ser <see cref="NavigationHelper"/>.
        /// </param>
        /// <param name="e">Datos de evento que proporcionan tanto el parámetro de navegación pasado a
        /// <see cref="Frame.Navigate(Type, Object)"/> cuando se solicitó inicialmente esta página y
        /// un diccionario del estado mantenido por esta página durante una sesión
        /// anterior. El estado será null la primera vez que se visite una página.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Crear un modelo de datos adecuado para el dominio del problema para reemplazar los datos de ejemplo
            var sampleDataGroup = await SampleDataSource.GetGroupAsync("Group-1");
            this.DefaultViewModel[FirstGroupName] = sampleDataGroup;
        }

        /// <summary>
        /// Mantiene el estado asociado con esta página en caso de que se suspenda la aplicación o
        /// se descarte la página de la memoria caché de navegación. Los valores deben cumplir los requisitos
        /// de serialización de <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">El origen del evento; suele ser <see cref="NavigationHelper"/>.</param>
        /// <param name="e">Datos de evento que proporcionan un diccionario vacío para rellenar con
        /// un estado serializable.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: Guardar aquí el estado único de la página.
        }

        /// <summary>
        /// Agrega un elemento a la lista cuando se hace clic en el botón de la barra de la aplicación.
        /// </summary>
        private void AddAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            string groupName = this.pivot.SelectedIndex == 0 ? FirstGroupName : SecondGroupName;
            var group = this.DefaultViewModel[groupName] as SampleDataGroup;
            var nextItemId = group.Items.Count + 1;
            var newItem = new SampleDataItem(
                string.Format(CultureInfo.InvariantCulture, "Group-{0}-Item-{1}", this.pivot.SelectedIndex + 1, nextItemId),
                string.Format(CultureInfo.CurrentCulture, this.resourceLoader.GetString("NewItemTitle"), nextItemId),
                string.Empty,
                string.Empty,
                this.resourceLoader.GetString("NewItemDescription"),
                string.Empty);

            group.Items.Add(newItem);

            // Desplazar el elemento nuevo en la vista.
            var container = this.pivot.ContainerFromIndex(this.pivot.SelectedIndex) as ContentControl;
            var listView = container.ContentTemplateRoot as ListView;
            listView.ScrollIntoView(newItem, ScrollIntoViewAlignment.Leading);
        }

        /// <summary>
        /// Se invoca al hacer clic en un elemento contenido en una sección.
        /// </summary>
        private void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navegar a la página de destino adecuada y configurar la nueva página
            // al pasar la información requerida como parámetro de navegación
            var itemId = ((SampleDataItem)e.ClickedItem).UniqueId;
            if (!Frame.Navigate(typeof(ItemPage), itemId))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        /// <summary>
        /// Carga el contenido del segundo elemento dinámico cuando se desplaza en la vista.
        /// </summary>
        private async void SecondPivot_Loaded(object sender, RoutedEventArgs e)
        {
            var sampleDataGroup = await SampleDataSource.GetGroupAsync("Group-2");
            this.DefaultViewModel[SecondGroupName] = sampleDataGroup;
        }

        #region Registro de NavigationHelper

        /// <summary>
        /// Los métodos proporcionados en esta sección se usan simplemente para permitir
        /// que NavigationHelper responda a los métodos de navegación de la página.
        /// <para>
        /// Debe incluirse lógica específica de página en los controladores de eventos para 
        /// <see cref="NavigationHelper.LoadState"/>
        /// y <see cref="NavigationHelper.SaveState"/>.
        /// El parámetro de navegación está disponible en el método LoadState 
        /// junto con el estado de página mantenido durante una sesión anterior.
        /// </para>
        /// </summary>
        /// <param name="e">Proporciona los datos para el evento y los métodos de navegación
        /// controladores que no pueden cancelar la solicitud de navegación.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        // pass above url in this method as input argument 
        public async void DownloadRSS(string rssURL)
        {
            try
            { webText = await GetWebPageAsync(rssURL); }
            catch { throw new Exception (); }

            myRSS_DownloadStringCompleted(webText);

        }

        //Method to get the image URL
        void myRSS_DownloadStringCompleted(string RSS)
        {
            ConnectionProfile InternetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();

            //Check if the Network is available
            if (InternetConnectionProfile.GetNetworkConnectivityLevel() != NetworkConnectivityLevel.None)
            {
                // filter all images from rss 

                //Title of the image
                imgTitle = XElement.Parse(RSS).Descendants("title")
                    .Select(m => m.Value).ToArray();

                //The description value is an HTML code, not XML....
                htmlArray = XElement.Parse(RSS).Descendants("description")
                    .Select(m => m.Value).ToArray();

                //Create array of preview images links
                imgarray = new string[htmlArray.Length - 2];
                for (int i = 1; i < htmlArray.Length-1; i++)
                {
                    HtmlDocument htmldoc = new HtmlDocument();
                    htmldoc.LoadHtml(htmlArray[i]);
                    HtmlNode imgNode = htmldoc.DocumentNode.FirstChild.FirstChild.FirstChild;
                    imgarray[i-1] = imgNode.GetAttributeValue("src", string.Empty); 
                }

                //todayPanel.Children.Clear();
                //Show the titles and images of the RSS.
                for (int j = 0; j < imgarray.Length; j++)
                {
                    TextBlock title = new TextBlock();
                    title.Text = imgTitle[j + 1];
                    title.Style = (Style)Application.Current.Resources["BodyTextBlockStyle"];
                    title.TextWrapping = TextWrapping.Wrap;
                    todayPanel.Children.Add(title);

                    Image img = new Image();
                    img.Width = 100;
                    img.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
                    img.Stretch = Stretch.Uniform;
                    img.Source = new BitmapImage(new Uri(imgarray[j]));
                    todayPanel.Children.Add(img);
                }               

            }
            else
            {
                msgPop.Pop("No network is available.", "Error");
            }
        }

        // Async method to get data from an url
        private async Task<string> GetWebPageAsync(string url)
        {
            try
            {
                Task<string> getStringTask = (new HttpClient()).GetStringAsync(url);
                string webText = await getStringTask;
                return webText;
            }
            catch
            {
                throw new NullReferenceException();
            }
        }

        private async void getimage(string imgHTML)
        {          
            try
            { webText = await GetWebPageAsync(imgHTML); }
            catch { throw new Exception(); }

            HtmlDocument htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(webText);

            //                                        html          body          center        a              
            HtmlNode titleNode = htmldoc.DocumentNode.ChildNodes[2].ChildNodes[3].ChildNodes[3].ChildNodes[1];
            string imgTitle = titleNode.InnerText;
            imgTitle = imgTitle.Substring(1, imgTitle.Length - 2);

            //                                      html          body          center        a              img
            HtmlNode imgNode = htmldoc.DocumentNode.ChildNodes[2].ChildNodes[3].ChildNodes[1].ChildNodes[11].ChildNodes[1];
            string imgLink = apodURL + imgNode.GetAttributeValue("SRC", string.Empty);

            TitleTextBlock.Text = imgTitle;
            imgImage.Source = new BitmapImage(new Uri(imgLink));
        }
    }
}
