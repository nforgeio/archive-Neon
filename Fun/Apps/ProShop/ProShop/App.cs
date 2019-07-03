using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Couchbase.Lite;

using Xamarin.Forms;

using Neon.Fun;

namespace ProShop
{
	public class App : Application
	{
		public App ()
		{
			// The root page of your application
			MainPage = new ContentPage {
				Content = new StackLayout {
					VerticalOptions = LayoutOptions.Center,
					Children = {
						new Label {
							HorizontalTextAlignment = TextAlignment.Center,
							Text = "Welcome to Xamarin Forms!"
						}
					}
				}
			};

            ModelTypes.Register();

            Manager.SharedInstance.StorageType = StorageEngineTypes.ForestDB;

            var db  = Manager.SharedInstance.GetEntityDatabase("test-0");
            var id  = Guid.NewGuid().ToString();
            var doc = db.GetEntityDocument<Account>(id);

            doc.Content.IsEnabled = true;

            doc.Save();
        }

        protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}
	}
}
