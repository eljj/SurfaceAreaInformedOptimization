using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VMS.TPS.Common.Model.API;

namespace SAIOptimization.Models
{
    ////Model Components to Define Default SAIO Parameters
    internal class OptimizationSettings
    {
        public Structure PTV { get; set; }
        public float MarginParameter { get; set; }
        public float DoseMaxForStructure { get; set; }
        public float ShellExpansionParameter { get; set; }

        public OptimizationSettings()
        {
                
        }

    }
}
