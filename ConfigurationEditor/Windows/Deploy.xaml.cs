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
using System.Windows.Shapes;
using System.Windows.Threading;
using ConfigurationEditor.Logging;
using ConfigurationEditor.Sccm;

namespace ConfigurationEditor.Windows
{
    /// <summary>
    /// Interaction logic for Deploy.xaml.
    /// </summary>
    public partial class Deploy : Window
    {
        public Deploy()
        {
            InitializeComponent();
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => { CustomOnLoad(); }));
        }

        private void CustomOnLoad()
        {
            var curs = DeployWnd.Cursor;
            DeployWnd.Cursor = Cursors.Wait;

            CollectionGrid.ItemsSource = SccmUtils.GetDeviceCollections();
            var view = CollectionViewSource.GetDefaultView(CollectionGrid.ItemsSource) as CollectionView;
            view.Filter = CustomFilter;

            DeployWnd.Cursor = curs;
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

        private void BtDeploy_Click(object sender, RoutedEventArgs e)
        {
            if (CollectionGrid.SelectedItem is CMColl coll)
            {
                var curs = DeployWnd.Cursor;
                DeployWnd.Cursor = Cursors.Wait;

                SccmUtils.DeploySettings(coll.CollectionId);
                SccmUtils.TriggerMachinePolicyRequest(coll.CollectionId);

                DeployWnd.Cursor = curs;

                Logger.Log($"User '{Environment.UserName}' Deployed settings to collection '{coll.CollectionId}' - '{coll.Name}'", LogType.Info);

                Close();
            }
        }
    }
}
