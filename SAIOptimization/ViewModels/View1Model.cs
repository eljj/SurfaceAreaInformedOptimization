using Prism.Commands;
using Prism.Mvvm;
using SAIOptimization.CustonWidgets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VMS.TPS.Common.Model.API;
using SAIOptimization.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Collections;

namespace SAIOptimization.ViewModels
{
    public class View1Model : BindableBase 
    {
        private ObservableCollection<Structure> _pTVs;
        public ObservableCollection<Structure> PTVs
        {
            get => _pTVs;

            set => SetProperty(ref _pTVs, value);
        }

        private Window SubWindow;
        public string AboutURI { get; set; }

        public DelegateCommand AboutCmd { get; set; }

        public DelegateCommand AddToListCmd { get; set; }

        public DelegateCommand GenerateCmd { get; set; }

        private float _marginParameter;
        private float _shellExpansionParameter;
        private float _doseMaxForStructure;
        private Structure _selectedStructure;
        private bool _IsHAPlan;

        public float MarginParameter
        {
            get => _marginParameter;

            set => SetProperty(ref _marginParameter, value);

        }

        public float ShellExpansionParameter
        {
            get => _shellExpansionParameter;

            set => SetProperty(ref _shellExpansionParameter, value);
        }

        public float DoseMaxForStructure
        {
            get => _doseMaxForStructure;

            set => SetProperty(ref _doseMaxForStructure, value);
        }

        public Structure SelectedStructure
        {
            get => _selectedStructure;
            set => SetProperty(ref _selectedStructure, value);
        }

        public bool IsHAPlan
        {
            get => _IsHAPlan;
            set => SetProperty(ref _IsHAPlan, value);
        }

        private ObservableCollection<Structure> GetPTVs(ScriptContext context)
        {

            ObservableCollection<Structure> PTVStructures = new ObservableCollection<Structure>();
            //Process PTVs For Worklist
            foreach (var sname in context.PlanSetup.StructureSet.Structures)
            {
                if (sname.DicomType == "PTV")
                {
                    PTVStructures.Add(sname);
                }
            }

            return PTVStructures;


        }

        private ObservableCollection<string> _listBoxItems;
        public ObservableCollection<string> ListBoxItems
        {
            get => _listBoxItems;
            set => SetProperty(ref _listBoxItems, value);
        }
        internal ObservableCollection<OptimizationSettings> PTVItemsList { get; set; }
        internal GenerateValues CurrentDataContext { get;  }

        public ScriptContext CurrentContext ;

        //SAIO Script View Model Constructor
        public View1Model(ScriptContext Context)
        {
            CurrentContext = Context;
            MarginParameter = 5F;
            ShellExpansionParameter = 0F;
            DoseMaxForStructure = 1.4F;
            SelectedStructure = null;

            PTVs = new ObservableCollection<Structure>();
            PTVs = GetPTVs(CurrentContext);

            ListBoxItems = new ObservableCollection<string>();
            PTVItemsList = new ObservableCollection<OptimizationSettings>();
            this.CurrentDataContext = new GenerateValues();

            AddToListCmd = new DelegateCommand(OnAdd);
            AboutCmd = new DelegateCommand(OnAbout);
            GenerateCmd = new DelegateCommand(OnGenerate);

            SubWindow = new Window();
            SubWindow.Height = 500;
            SubWindow.Width = 500;
            SubWindow.Title = "About";
            SubWindow.Content = new BindableRichTextBox()
            {
                IsReadOnly = true,
                Source = new Uri(@"pack://application:,,,/SAIOptimization.esapi;component/Resources/About.rtf"),

            };

            SubWindow.Closing += OnClosing;
        }

        //******SAIO View Model Helper Methods******** 
        private void OnAdd()
        {
            if (SelectedStructure == null)
            {
                MessageBox.Show("No Structure Is Selected - Please Select A Structure Before Attempting  Add To Worklist");
                return;
            }
            foreach (var item in PTVItemsList)
            {
                if (SelectedStructure.Id == item.PTV.Id)
                {
                    MessageBox.Show("Selected PTV Structure Already Added To WorkList\nPlease Select a Different PTV Structure");
                    SelectedStructure = null;
                    return;
                }
            }

            if(SelectedStructure.IsEmpty)
            {
                MessageBox.Show("PTV Structure Does Not Contain Any Countours\n" + SelectedStructure.Id + " Cannot Be Processed");
                return;
            }
            else if(!SelectedStructure.IsHighResolution)
            {
                try
                {
                    SelectedStructure.ConvertToHighResolution();
                    MessageBox.Show("Selected PTV - " + SelectedStructure.Id + " Has Been Converted To High Resolution");
                }
                catch (Exception)
                {
                    MessageBox.Show("Selected PTV - " + SelectedStructure.Id + " - Could Not Be Converted To High Resolution\n" +
                        "Selected PTV May Not Be Utilized For Generation of \"Brain - PTVs - OPR50s\" Structure ");
                }

            }
            OptimizationSettings PTVItem = new OptimizationSettings();
            PTVItem.PTV = SelectedStructure;
            PTVItem.MarginParameter = MarginParameter;
            PTVItem.DoseMaxForStructure = DoseMaxForStructure;
            PTVItem.ShellExpansionParameter = ShellExpansionParameter;
            ListBoxItems.Add(SelectedStructure.Id + " - " + MarginParameter.ToString() + " - " + DoseMaxForStructure.ToString() + " - " + ShellExpansionParameter.ToString());
            PTVItemsList.Add(PTVItem);
            SelectedStructure = null;
        }

        internal void OnGenerate()
        {
            if (PTVItemsList.Count == 0)
            {
                MessageBox.Show("PTV WorkList Is Empty - Add PTV Structures To Worklist");
                return;
            }
            CurrentDataContext.CreateOptimizationValues(CurrentContext, PTVItemsList, IsHAPlan);

            ListBoxItems.Clear();
            MarginParameter = 5F;
            ShellExpansionParameter = 0F;
            DoseMaxForStructure = 1.4F;
            SelectedStructure = null;
            IsHAPlan = false;
        }

        private void OnAbout()
        {
            SubWindow.Show();
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            SubWindow.Hide();
            e.Cancel = true;
        }
    }
}
