using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace SAIOptimization.Models
{
    //Model Components to Create and Write Optimization Structures and Constraints
    internal  class GenerateValues
    {
        public GenerateValues()
        {
                
        }
        public void CreateOptimizationValues(ScriptContext Context, ObservableCollection<OptimizationSettings> ItemCollection, bool IsHAPlan)
        {
            //Initial Declarations
            bool HasBrainStructure = false;
            bool BrainIsHiRes = false;
            bool BrainSegmentHasBeenAdded = false;
            Structure Brain = null; 
            string DoseUnitString = "";
            int DoseUnitScaler = 0;
            Structure BrainminusPTVs = null;
            float ishelllocal, marginlocal, optR50maxlocal;
            double volumeestimate, deltar, radiusestimate, R50analytic, surfaceareaestimate, margin, rxdose, VOPH;
            Structure OPR50;
            Structure ishell;
           
            //Check for Worklist Populated with at least 1 PTV
            if (ItemCollection.Count < 1)
            {
                MessageBox.Show("No PTVs Added to the Worklist - Please add 1 or more PTVs and Re-try");
                return;
            }

            rxdose = Context.PlanSetup.TotalDose.Dose;

            //Check For Valid Brain Structure and Resolution Status
            foreach (var structure in Context.PlanSetup.StructureSet.Structures)
            {
                if(structure.Id.ToUpper() == "BRAIN")
                {
                    Brain = structure;
                    MessageBox.Show("A Brain Structure Has Been Identified");
                    HasBrainStructure = true;
                    if (Brain.IsHighResolution)
                    {
                        BrainIsHiRes = true;
                    }
                    else if(!Brain.IsHighResolution && Brain.CanConvertToHighResolution())
                    {
                        Brain.ConvertToHighResolution();
                        BrainIsHiRes = true;
                    }
                }
            }

            if(HasBrainStructure == false)
            {
                MessageBox.Show("A Brain Structure Was Not Found in the Structure Set\nOptimization Structure \"Brain - PTVs - OPR50s\" Could Not Be Created");
            }

            if(BrainIsHiRes == false)
            {
                MessageBox.Show("Brain Structure Cound Not Be Converted to High Resolution\nOptimization Structure \"Brain - PTVs - OPR50s\" May Not Be Created");
            }

            //Test For Eclipse Dose Unit
            Structure Test;
            OptimizationObjective testobj;
            Test = Context.PlanSetup.StructureSet.AddStructure("PTV", "TestDoseUnit");
            Test.SegmentVolume = ItemCollection[0].PTV.Margin(10).Sub(ItemCollection[0].PTV.SegmentVolume);
            try
            {
                testobj = Context.PlanSetup.OptimizationSetup.AddMeanDoseObjective(Test, new DoseValue(10, "cGy"), 100);
                DoseUnitString = "cGy";
                DoseUnitScaler = 1;
            }
            catch (Exception)
            {
                testobj = Context.PlanSetup.OptimizationSetup.AddMeanDoseObjective(Test, new DoseValue(10, "Gy"), 100);
                DoseUnitString = "Gy";
                DoseUnitScaler = 100;
            }

            if(DoseUnitString == "" || DoseUnitScaler == 0)
            {
                MessageBox.Show("Unable to Resolve The Dose Unit For This TPS Context");
                return;
            }

            //Remove Test Oblects
            Context.PlanSetup.OptimizationSetup.RemoveObjective(testobj);
            Context.PlanSetup.StructureSet.RemoveStructure(Test);

    //*********Begin Parameter Generation*********//
            //NTO Automatic with 200
            Context.PlanSetup.OptimizationSetup.AddAutomaticNormalTissueObjective(200);

            //Create Optimization Volume for Subtraction from Normal Brain and Initialize to the Brain Structure
            if (HasBrainStructure)
            {
                try
                {
                    BrainminusPTVs = Context.PlanSetup.StructureSet.AddStructure("Organ", "Brain-PTVs-OPR50s");
                    BrainminusPTVs.SegmentVolume = Brain.SegmentVolume;
                    BrainSegmentHasBeenAdded = true;
                }
                catch (Exception)
                {

                    MessageBox.Show("Unable To Create Brain-PTVs-OPR50s");
                }

            }

        //*******Begin Iteration Over PTVs in the Worklist*******
            foreach (var item in ItemCollection)
            {
                if (item.ShellExpansionParameter < 0)
                {
                    MessageBox.Show("Negative Shell Expansion Parameter Not Allowed \n Setting Shell Expansion Parameter To The Default Value (0)");
                    ishelllocal = 0F;
                }
                else
                {
                    ishelllocal = item.ShellExpansionParameter;
                }

                if (item.MarginParameter <= 0)
                {
                    MessageBox.Show("Negative or 0 Margin Parameter Not Allowed \n Setting Margin Parameter To The Default Value (5)");
                    marginlocal = 5F;
                }
                else
                {
                    marginlocal = item.MarginParameter;
                }

                if (item.DoseMaxForStructure < 1)
                {
                    MessageBox.Show("PTV Dose Max Parameter Should Be Greater Than 1 \nSetting PTV Dose Max Parameter To The Default Value (1.4) ");
                    optR50maxlocal = 1.4F;
                }
                else
                {
                    optR50maxlocal = item.DoseMaxForStructure;
                }

                
                try
                {
                    volumeestimate = item.PTV.Volume; /* Volume in cm3 */
                    deltar = 0.2844 * Math.Pow(volumeestimate, 0.1973);
                }
                catch (Exception excpt)
                {
                    MessageBox.Show("Invalid Volume Found For Structure - " + item.PTV.ToString() + "\n" + excpt.ToString());
                    MessageBox.Show("Cannot Process Optimization Parameters for - " + item.PTV.ToString() + "\n Processing Next Structure in Worklist");
                    continue;
                }

                radiusestimate = Math.Pow((3.0 * volumeestimate / 4 / Math.PI), (1.0 / 3.0)); /* In cm */

                try
                {
                    surfaceareaestimate = CalculateSurfaceArea(item.PTV.MeshGeometry);
                    if (surfaceareaestimate <= 0)
                    {
                        MessageBox.Show("InValid Surface Area Calculated - " + surfaceareaestimate.ToString() + "\n  Processing Next Structure in Worklist");
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Invalid SurfaceArea Found For Structure - " + item.PTV.ToString() + "\n" + ex.ToString());
                    MessageBox.Show("Cannot Process Optimization Parameters for - " + item.PTV.ToString() + "\n Processing Next Structure in Worklist");
                    continue;
                }

                R50analytic = 1.0 + (surfaceareaestimate * deltar / volumeestimate) * (1.0 + (deltar / radiusestimate) + 0.333333 * Math.Pow((deltar / radiusestimate), 2));
                margin= radiusestimate * ((Math.Pow((marginlocal * R50analytic - (marginlocal - 1)), (1.0 / 3.0)) - 1.0));
                OPR50 = Context.PlanSetup.StructureSet.AddStructure("PTV", "OPR50" + item.PTV.Id);
                OPR50.SegmentVolume = item.PTV.Margin(margin * 10.0).Sub(item.PTV.SegmentVolume);
                if (BrainSegmentHasBeenAdded)
                {
                    try
                    {
                        BrainminusPTVs.SegmentVolume = BrainminusPTVs.SegmentVolume.Sub(item.PTV.Margin(margin * 10.0)).Sub(item.PTV.SegmentVolume);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Unable To Create ");
                    }
                    
                }


                // iShell Objectives
                if (ishelllocal > 0)
                {
                    ishell = Context.PlanSetup.StructureSet.AddStructure("PTV", "ish" + item.PTV.Id);
                    ishell.SegmentVolume = item.PTV.Margin(margin * 10 + ishelllocal * 10).Sub(item.PTV.Margin(margin* 10.0));


                    Context.PlanSetup.OptimizationSetup.AddPointObjective(ishell, OptimizationObjectiveOperator.Upper, new DoseValue(0.45 * rxdose, DoseUnitString), 0.0, 200);
                }
                else
                {
                    MessageBox.Show("ishell expansion will not be applied to structure -  " + item.PTV.Id.ToString());
                }

                //SAIO Volume Parameter
                VOPH = 100.0 * ((R50analytic - 1) * volumeestimate) / OPR50.Volume;

                //Set Target Objectives
                if(!IsHAPlan)
                {
                    Context.PlanSetup.OptimizationSetup.AddPointObjective(item.PTV, OptimizationObjectiveOperator.Lower, new DoseValue(rxdose, DoseUnitString), 100, 200);
                    Context.PlanSetup.OptimizationSetup.AddPointObjective(item.PTV, OptimizationObjectiveOperator.Upper, new DoseValue(optR50maxlocal * rxdose, DoseUnitString), 0.0, 200);
                }
                


                // Set OptiForR50Shell Objectives
                Context.PlanSetup.OptimizationSetup.AddPointObjective(OPR50, OptimizationObjectiveOperator.Upper, new DoseValue(rxdose * 0.45, DoseUnitString), VOPH, 200);
                Context.PlanSetup.OptimizationSetup.AddPointObjective(OPR50, OptimizationObjectiveOperator.Upper, new DoseValue(rxdose, DoseUnitString), 0.0, 200);

            }
        //*******End Iteration Over Worklist PTVs*******

            //Create Normal Brain Volume
            if (BrainSegmentHasBeenAdded)
            {
                Context.PlanSetup.OptimizationSetup.AddMeanDoseObjective(BrainminusPTVs, new DoseValue(0.1 * rxdose / DoseUnitScaler, DoseUnitString), 200);
            }


            MessageBox.Show("Structures/Optimization Parameters Generated - Be Sure Click the Save Icon to Save Changes");

    //*********End Parameter Generation*********//
        }



        //Surface Area Calculation Method
        public double CalculateSurfaceArea(MeshGeometry3D mesh)
        {
            double surfaceareaestimate;
            if (mesh == null)
            {
                MessageBox.Show("Structure Mesh seems to be undefined, please select a valid structure");
                surfaceareaestimate = 0.0;
                return surfaceareaestimate;
            }

            else
            {
                Point3DCollection vertexes = mesh.Positions;

                Int32Collection indexes = mesh.TriangleIndices;

                Point3D p1, p2, p3;
                surfaceareaestimate = 0.0;

                for (int v = 0; v < mesh.TriangleIndices.Count(); v += 3)
                {

                    p1 = vertexes[indexes[v]];
                    p2 = vertexes[indexes[v + 1]];
                    p3 = vertexes[indexes[v + 2]];


                    double pp1x = Convert.ToDouble(p1.X);
                    double pp1y = Convert.ToDouble(p1.Y);
                    double pp1z = Convert.ToDouble(p1.Z);
                    double pp2x = Convert.ToDouble(p2.X);
                    double pp2y = Convert.ToDouble(p2.Y);
                    double pp2z = Convert.ToDouble(p2.Z);
                    double pp3x = Convert.ToDouble(p3.X);
                    double pp3y = Convert.ToDouble(p3.Y);
                    double pp3z = Convert.ToDouble(p3.Z);

                    VVector A = new VVector(pp1x, pp1y, pp1z);
                    VVector B = new VVector(pp2x, pp2y, pp2z);
                    VVector C = new VVector(pp3x, pp3y, pp3z);

                    VVector AB = B - A;
                    double ab = AB.Length;

                    VVector AC = C - A;
                    double ac = AC.Length;

                    double cp = AB.ScalarProduct(AC);
                    double angle = Math.Acos(cp / (ab * ac));

                    surfaceareaestimate += 0.5 * ab * ac * Math.Sin(angle);
                }
                surfaceareaestimate = surfaceareaestimate / 100; /* Convert to cm2 */
                return surfaceareaestimate;
            }
        }
    }



}
    

