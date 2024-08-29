The SurfaceAreaInformedOptimization script is an aid to generate optimization structures and constraints for SRS treatment planning. The script is built as a .dll library based on the 15.6 Varian ESAPI libraries with .NET version 4.61. The script can be a standalone tool for those users without Varian Medical Systems HyperArc(HA) platform or can be used in conjunction with HA. The generation of optimization structures and constraints considers the shape of the target as well as the volume. This script is based the work of Desai et al and utilizes three input parameters; 1) The Margin Parameter M (Default 5) , 2) The Dose Maximum to Target PTV (Default 1.4) , and 3) A Shell Expansion Parameter (Default 0). The Margin Parameter controls the size of the Target PTV expansion ring and typical values are 4 to 5. The Dose Maximum Parameter is used to set the Upper constraint on the Target PTV with typical values between 1.2 to 1.4.  The Shell Expansion Parameter is an optional ring around the PTV expansion as determined by the Margin Parameter and has typical values from 0-1. It is common to use 0 for this parameter and therefore the script does not generate the second ring structure.

Prerequisites to run the script are; 1) A valid SRS plan with good delivery geometry generated with the Eclipse Treatment Planning System. 2) A valid dose Prescription for the plan, and 3) A Structure Set with PTVs and the Brain contoured as High Resolution Structures. Other OARs may be contoured and additional OAR constraints added manually by the user. The script does not manage any other OARs beside the Brain.

The script runs as a binary plugin to the ESAPI functionality. When the script opens, the values for the three aforementioned parameters will be assigned their default values. Edit the values as desired and select a PTV from the drop down list. Click the “Add Selected PTV To Worklist” button to capture the conditions for processing that PTV. Continue to edit the parameter list and select PTVs until all required targets have been added to the Worklist. For those without HA, leave the HA Plan CheckBox unchecked and click the “Generate Optimization Parameters” button to build the structures and constraints into the current Eclipse Plan in Focus. If this is a HA plan, check the HA Plan CheckBox and the script will use those PTV constraints generated in the HA workflow and will not add any additional PTV constraints.

![image](https://github.com/user-attachments/assets/ec90cbd2-af12-4ab2-b236-54784d86ae85)


Summary: 
1.	Surface Area, Volume, and Δr are calculated for each PTV.  
2.	R50%Analytic is calculated for each PTV.
3.	M value is calculated for each PTV and used for the margin expansion of a custom ring structure around each PTV.
4.	%Vopti value is calculated for each PTV and used as an optimization parameter for the custom ring around each PTV.
5.	Objectives for each PTV and constraints for custom rings are populated within the Eclipse optimizer.
References:
1.	Desai DD, Johnson EL, Cordrey IL. An analytical expression for R50% dependent on PTV surface area and volume: a cranial SRS comparison. J Appl Clin Med Phys. 2021;22(2):203-210. doi:10.1002/acm2.13168.
2.	Desai DD, Cordrey IL, Johnson EL. Efficient optimization of R50% when planning multiple cranial metastases simultaneously in single isocenter SRS/SRT. J Appl Clin Med Phys. 2021;22(6):71–82. doi:10.1002/acm2.13254.
3.	Cordrey IL, Desai DD, Johnson EL. Analysis of R50% location dependence on LINAC-based VMAT cranial stereotactic treatments. Med Dosim. 2022;47(1):79-86. doi:10.1016/j.meddos.2021.09.006.

