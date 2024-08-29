using System.Windows.Controls;
using VMS.TPS.Common.Model.API;
using SAIOptimization.ViewModels;
using System.Windows;

namespace SAIOptimization.Views
{
    /// <summary>
    /// Interaction logic for View1.xaml
    /// </summary>
    public partial class View1 : UserControl
    {
        public View1(ScriptContext context)
        {
            InitializeComponent();
            //this.vm = new View1Model(context);
            DataContext = new ViewModels.View1Model(context); //this.vm;

        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
