using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;
using System.Windows.Threading;
using ConfigurationEditor.EventArguments;
using ConfigurationEditor.Logging;
using ConfigurationEditor.Sccm;
using SchedulerSettings;

namespace ConfigurationEditor.Windows
{
    /// <summary>
    /// Interaction logic for Load.xaml.
    /// </summary>
    public partial class Load : Window
    {
        public event EventHandler<LoadEventArg> SettingsLoaded;

        private ObservableCollection<CMColl> _observableCollection;

        public Load()
        {
            InitializeComponent();
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => { CustomOnLoad(); }));
        }

        private void CustomOnLoad()
        {
            var curs = LoadWnd.Cursor;
            LoadWnd.Cursor = Cursors.Wait;

            _observableCollection = SccmUtils.GetDeviceCollectionsWithVariables();
            CollectionGrid.ItemsSource = _observableCollection;
            var view = CollectionViewSource.GetDefaultView(CollectionGrid.ItemsSource) as CollectionView;
            view.Filter = CustomFilter;

            LoadWnd.Cursor = curs;
        }

        private void TbFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(CollectionGrid.ItemsSource).Refresh();
        }

        private bool CustomFilter(object obj)
        {
            if (string.IsNullOrEmpty(tbFilter.Text))
            {
                return true;
            }
            else
            {
                var entry = (obj as CMColl).Name;
                return entry.IndexOf(tbFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        private void BtLoad_Click(object sender, RoutedEventArgs e)
        {
            if (CollectionGrid.SelectedItem is CMColl coll)
            {
                var curs = LoadWnd.Cursor;
                LoadWnd.Cursor = Cursors.Wait;

                var settings = SccmUtils.GetSettingsFromCollection(coll.CollectionId);

                if (string.IsNullOrEmpty(settings))
                {
                    return;
                }

                SettingsLoaded(this, new LoadEventArg
                {
                    Settings = settings,
                });

                LoadWnd.Cursor = curs;

                Logger.Log($"User '{Environment.UserName}' Loaded settings from collection '{coll.CollectionId}' - '{coll.Name}'", LogType.Info);

                Close();
            }
        }

        private void BtRemove_Click(object sender, RoutedEventArgs e)
        {
            if (CollectionGrid.SelectedItem is CMColl coll)
            {
                var curs = LoadWnd.Cursor;
                LoadWnd.Cursor = Cursors.Wait;

                SccmUtils.RemoveDeployment(coll.CollectionId);
                _observableCollection.Remove(coll);

                LoadWnd.Cursor = curs;

                Logger.Log($"User '{Environment.UserName}' Removed settings from collection '{coll.CollectionId}' - '{coll.Name}'", LogType.Info);
            }
        }
    }
}
