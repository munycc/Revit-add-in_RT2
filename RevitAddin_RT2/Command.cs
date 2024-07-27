#region Namespaces

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;
using OxyPlot.WindowsForms;
using System.IO;
using System.Reflection;


#endregion

/// This script is aimed at finding the theoretical RT of a square / rectangular room 
/// This code might need to be adjusted depending on which language in Revit you are using e.g: Height in English becomes Höhe in German
/// To use in Revit, select all the slabs, external walls of your room (it doesn't matter if windows doors are also selected)

namespace RevitAddin_RT2

{
    [Transaction(TransactionMode.Manual)]


    public class Command : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;
            Autodesk.Revit.DB.Document doc = uidoc.Document;

            double wallHeight = 0.0;
            double volume = 0.0;
            double slabArea = 0.0;
            double ceilingArea = 0.0;
            double wallTotalLength = 0.0;
            double wallWidth = 0.0;
            double lengthSMeters = 0.0;
            double widthSMeters = 0.0;



            // Lists to store frequencies and RT values

            List<double> frequencies = new List<double>
            {
                125, 250, 500, 1000, 2000, 4000
            };

            List<(double Frequency, double RT)> sabineRT = new List<(double Frequency, double RT)>();
            List<(double Frequency, double RT)> eyringRT = new List<(double Frequency, double RT)>();
            List<(double Frequency, double RT)> Tmax = new List<(double Frequency, double RT)>();
            List<(double Frequency, double RT)> Tmin = new List<(double Frequency, double RT)>();


            // Declare PlotViews
            PlotView myPlotViewSabine = new PlotView();
            PlotView myPlotViewEyring = new PlotView();

            // Create a dictionary to store material names as keys and wall areas as values.
            Dictionary<string, double> materialAreas = new Dictionary<string, double>();

            // Get selected elements from the current document.
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();

            // Go through the selected items and filter out walls only.
            foreach (Autodesk.Revit.DB.ElementId id in selectedIds)
            {
                Autodesk.Revit.DB.Element element = uidoc.Document.GetElement(id);



                //////////////WALL//////////////////

                // Check if the element is a wall
                if (element is Wall wall)
                {
                    // Get all parameters of the wall
                    ParameterSet parameters = wall.Parameters;

                    // Initialize variables to store height and area
                    
                    double wallLength = 0.0;
                    double wallArea = 0.0;
                    

                    // Iterate through the parameters to find the height, length, and width
                    foreach (Parameter param in parameters)
                    {
                        string paramName = param.Definition.Name;

                        if (paramName == "Unconnected Height")
                        {
                            wallHeight = param.AsDouble() * 0.3048; // Convert height to meters
                        }
                        else if (paramName == "Length") // Corrected parameter name to "Length"
                        {
                            wallLength = param.AsDouble() * 0.3048;
                        }
                       
                    }


                    // Get the wall's compound structure
                    CompoundStructure compoundStructure = wall.WallType.GetCompoundStructure();

                    // Get the first layer in the compound structure (assuming it's the outermost layer)
                    CompoundStructureLayer firstLayer = compoundStructure.GetLayers().LastOrDefault();

                    if (firstLayer != null)
                    {
                        ElementId materialId = firstLayer.MaterialId;
                        Material material = doc.GetElement(materialId) as Material;
                        string materialName = (material != null) ? material.Name : "N/A";

                        // Calculate the layer width and add it to the total wall width
                        double layerWidthMeters = firstLayer.Width * 0.3048; // Convert width to meters
                        wallWidth += layerWidthMeters;

                        wallTotalLength = wallLength - (wallWidth);
                        wallArea = wallTotalLength * wallHeight;

                        // Update or add the material area to the dictionary
                        if (materialAreas.ContainsKey(materialName))
                        {
                            materialAreas[materialName] += wallArea;
                        }
                        else
                        {
                            materialAreas[materialName] = wallArea;
                        }
                    }

                    


                }



                    // Check if the element is a floor
                    if (element is Floor slab)
                    {
                        // Get the area parameter
                        Parameter areaParam = element.LookupParameter("Area");

                        if (areaParam != null)
                        {
                            slabArea = areaParam.AsDouble() * 0.09290304;

                            // Get the first material of the floor (assuming it's the outermost layer)
                            ElementId materialId = element.GetMaterialIds(false).LastOrDefault();
                            Material material = doc.GetElement(materialId) as Material;
                            string materialName = (material != null) ? material.Name : "N/A";

                            // Update or add the material area to the dictionary
                            if (materialAreas.ContainsKey(materialName))
                            {
                                materialAreas[materialName] += slabArea;
                            }
                            else
                            {
                                materialAreas[materialName] = slabArea;
                            }

                            volume = slabArea * wallHeight;
                        }

                        // Find the bounding box of slab in its own coordinate system
                        BoundingBoxXYZ bbox = slab.get_BoundingBox(null);

                        // Calculate length and width based on bounding box
                        double length = bbox.Max.X - bbox.Min.X;
                        double width = bbox.Max.Y - bbox.Min.Y;

                        // Convert from feet to meters if necessary
                        // Revit internal units are feet
                        lengthSMeters = length * 0.3048;
                        widthSMeters = width * 0.3048;

                    }



                // Check if the element is a ceiling
                if (element is Ceiling ceiling)
                    {
                        // Get the area parameter
                        Parameter areaParam = element.LookupParameter("Area");

                        if (areaParam != null)
                        {
                            ceilingArea = areaParam.AsDouble() * 0.09290304;

                            // Get the first material of the ceiling (assuming it's the outermost layer)
                            ElementId materialId = element.GetMaterialIds(false).LastOrDefault();
                            Material material = doc.GetElement(materialId) as Material;
                            string materialName = (material != null) ? material.Name : "N/A";

                            // Update or add the material area to the dictionary
                            if (materialAreas.ContainsKey(materialName))
                            {
                                materialAreas[materialName] += ceilingArea;
                            }
                            else
                            {
                                materialAreas[materialName] = ceilingArea;
                            }
                        }
                    }


            }

            // Create a string to build the result message
            string resultMessage = "Areas by Material:\n";

            // Loop through the dictionary and add the results to the message
            foreach (var kvp in materialAreas)
            {
                // Format kvp.Value to have exactly two decimal places
                string formattedValue = kvp.Value.ToString("F2");
                resultMessage += $"\n{kvp.Key}: {formattedValue} sqm\n"; // Units are now in square meters
            }

            // Add the total room height, total slab area, and total room volume to the result message. Use string interpolation for formatted values
            resultMessage += $"\nTotal Room Height: {wallHeight:F2} meters\n";
            resultMessage += $"\nTotal Slab Area: {slabArea:F2} meters\n";
            resultMessage += $"\nTotal Room Volume: {volume:F2} cubic meters\n";

            // Display the result message in a TaskDialog
            TaskDialog dialoga = new TaskDialog("Areas, Height & Volume");
            dialoga.MainContent = resultMessage;
            dialoga.Show();


            //////////////////////////////////////////////////////////////////////////////////////

            // Get the directory of the executing assembly
            string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Combine the assembly directory with the relative path to the Database folder and database file name
            string databasePath = Path.Combine(assemblyLocation, "absorption_coeff.db");

            // Check if the database file exists at the constructed path
            if (!File.Exists(databasePath))
            {
                throw new FileNotFoundException($"The specified database file could not be found at: {databasePath}");
            }

            // Construct the connection string
            string connectionString = $"Data Source={databasePath};Version=3;";




            // Create a new SQLite connection
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))

                {
                    // Open the connection
                    connection.Open();

                    // Initialize lists for absorption coefficients
                    List<double> absorption125HzList = new List<double>();
                    List<double> absorption250HzList = new List<double>();
                    List<double> absorption500HzList = new List<double>();
                    List<double> absorption1000HzList = new List<double>();
                    List<double> absorption2000HzList = new List<double>();
                    List<double> absorption4000HzList = new List<double>();

                    double AirAbsoption125Hz = 0;
                    double AirAbsoption250Hz = 0;
                    double AirAbsoption500Hz = 0;
                    double AirAbsoption1000Hz = 0;
                    double AirAbsoption2000Hz = 0;
                    double AirAbsoption4000Hz = 0;



                    double sumOfAbsorptionArea125Hz = 0;
                    double sumOfAbsorptionArea250Hz = 0;
                    double sumOfAbsorptionArea500Hz = 0;
                    double sumOfAbsorptionArea1000Hz = 0;
                    double sumOfAbsorptionArea2000Hz = 0;
                    double sumOfAbsorptionArea4000Hz = 0;




                    foreach (var kvp in materialAreas)
                    {
                        string materialName = kvp.Key;
                        double AreaObject = kvp.Value;

                        // Query the database to get absorption coefficients based on Material Name for walls
                        string wallQuery = $"SELECT * FROM coeff WHERE [material] = '{materialName}'";
                        using (SQLiteCommand wallCommand = new SQLiteCommand(wallQuery, connection))
                        {
                            using (SQLiteDataReader wallReader = wallCommand.ExecuteReader())
                            {
                                while (wallReader.Read())
                                {
                                    // Get absorption coefficients at different frequencies and add them to respective lists for Sabine 
                                    absorption125HzList.Add(wallReader.GetDouble(2) * AreaObject);
                                    absorption250HzList.Add(wallReader.GetDouble(3) * AreaObject);
                                    absorption500HzList.Add(wallReader.GetDouble(4) * AreaObject);
                                    absorption1000HzList.Add(wallReader.GetDouble(5) * AreaObject);
                                    absorption2000HzList.Add(wallReader.GetDouble(6) * AreaObject);
                                    absorption4000HzList.Add(wallReader.GetDouble(7) * AreaObject);


                                }

                            }


                        }

                    }

                    AirAbsoption125Hz = 4 * volume * 0.00005626909;
                    AirAbsoption250Hz = 4 * volume * 0.00016419279;
                    AirAbsoption500Hz = 4 * volume * 0.0003338321;
                    AirAbsoption1000Hz = 4 * volume * 0.0005694963;
                    AirAbsoption2000Hz = 4 * volume * 0.001228403;
                    AirAbsoption4000Hz = 4 * volume * 0.003734768;

                    // Create a StringBuilder to build the result message
                    //StringBuilder resultMessageb = new StringBuilder("Equivalent absorption areas for each material f frequency");

                    // Add absorption coefficients for each frequency to the result message
                    //resultMessageb.AppendFormat("125 Hz: {0}\n", string.Join(" ; ", absorption125HzList));
                    //resultMessageb.AppendFormat("250 Hz: {0}\n", string.Join(" ; ", absorption250HzList));
                    //resultMessageb.AppendFormat("500 Hz: {0}\n", string.Join(" ; ", absorption500HzList));
                    //resultMessageb.AppendFormat("1000 Hz: {0}\n", string.Join(" ; ", absorption1000HzList));
                    //resultMessageb.AppendFormat("2000 Hz: {0}\n", string.Join(" ; ", absorption2000HzList));
                    //resultMessageb.AppendFormat("4000 Hz: {0}\n", string.Join(" ; ", absorption4000HzList));

                    // Display the result message in a TaskDialog
                    //TaskDialog.Show("Absorption Coefficients", resultMessageb.ToString());


                    sumOfAbsorptionArea125Hz += AirAbsoption125Hz + absorption125HzList.Sum();
                    sumOfAbsorptionArea250Hz += AirAbsoption250Hz + absorption250HzList.Sum();
                    sumOfAbsorptionArea500Hz += AirAbsoption500Hz + absorption500HzList.Sum();
                    sumOfAbsorptionArea1000Hz += AirAbsoption1000Hz+ absorption1000HzList.Sum();
                    sumOfAbsorptionArea2000Hz += AirAbsoption2000Hz+ absorption2000HzList.Sum();
                    sumOfAbsorptionArea4000Hz += AirAbsoption4000Hz+ absorption4000HzList.Sum();

                    //sumOfAbsorptionArea125Hz +=  absorption125HzList.Sum();
                    //sumOfAbsorptionArea250Hz +=  absorption250HzList.Sum();
                    //sumOfAbsorptionArea500Hz +=  absorption500HzList.Sum();
                    //sumOfAbsorptionArea1000Hz +=  absorption1000HzList.Sum();
                    //sumOfAbsorptionArea2000Hz +=  absorption2000HzList.Sum();
                    //sumOfAbsorptionArea4000Hz +=  absorption4000HzList.Sum();


                    string messagec = "Sum of absorption area by frequency:\n";
                    messagec += $"\nSum of absorption area 125 Hz: {sumOfAbsorptionArea125Hz:F3}\n";
                    messagec += $"\nSum of absorption area 250 Hz: {sumOfAbsorptionArea250Hz:F3}\n";
                    messagec += $"\nSum of absorption area 500 Hz: {sumOfAbsorptionArea500Hz:F3}\n";
                    messagec += $"\nSum of absorption area 1000 Hz: {sumOfAbsorptionArea1000Hz:F3}\n";
                    messagec += $"\nSum of absorption area 2000 Hz: {sumOfAbsorptionArea2000Hz:F3}\n";
                    messagec += $"\nSum of absorption area 4000 Hz: {sumOfAbsorptionArea4000Hz:F3}\n";

                    // Create and show the TaskDialog
                    TaskDialog dialogc = new TaskDialog("sum");
                    dialogc.MainContent = messagec;
                    dialogc.Show();




                    // Calculate reverberation time (RT) for each frequency using the Sabine formula
                    double RT125 = (0.161 * volume) / sumOfAbsorptionArea125Hz;
                    double RT250 = (0.161 * volume) / sumOfAbsorptionArea250Hz;
                    double RT500 = (0.161 * volume) / sumOfAbsorptionArea500Hz;
                    double RT1000 = (0.161 * volume) / sumOfAbsorptionArea1000Hz;
                    double RT2000 = (0.161 * volume) / sumOfAbsorptionArea2000Hz;
                    double RT4000 = (0.161 * volume) / sumOfAbsorptionArea4000Hz;

                    // Add frequency-RT pairs to the list
                    sabineRT.Add((125, RT125));
                    sabineRT.Add((250, RT250));
                    sabineRT.Add((500, RT500));
                    sabineRT.Add((1000, RT1000));
                    sabineRT.Add((2000, RT2000));
                    sabineRT.Add((4000, RT4000));


                    // Display the RT values in a TaskDialog
                    string RTMessage = $"Reverberation Time using Sabine (RT):\n";
                    
                    RTMessage += $"\nRT at 125Hz: {RT125:F3} seconds\n";
                    RTMessage += $"\nRT at 250Hz: {RT250:F3} seconds\n";
                    RTMessage += $"\nRT at 500Hz: {RT500:F3} seconds\n";
                    RTMessage += $"\nRT at 1000Hz: {RT1000:F3} seconds\n";
                    RTMessage += $"\nRT at 2000Hz: {RT2000:F3} seconds\n";
                    RTMessage += $"\nRT at 4000Hz: {RT4000:F3} seconds\n";
                    


                    TaskDialog RTDialog = new TaskDialog("Reverberation Time Sabine");
                    RTDialog.MainContent = RTMessage;
                    RTDialog.Show();



                    // Calculate Eyring
                    var TotalArea = materialAreas.Sum(x => x.Value);

                    double RT125_Eyring = (0.161 * volume) / ( -Math.Log(1 - (sumOfAbsorptionArea125Hz/ TotalArea)) * TotalArea);
                    double RT250_Eyring = (0.161 * volume) / (-Math.Log(1 - (sumOfAbsorptionArea250Hz / TotalArea)) * TotalArea);
                    double RT500_Eyring = (0.161 * volume) / (-Math.Log(1 - (sumOfAbsorptionArea500Hz / TotalArea)) * TotalArea);
                    double RT1000_Eyring = (0.161 * volume) / (-Math.Log(1 - (sumOfAbsorptionArea1000Hz / TotalArea)) * TotalArea);
                    double RT2000_Eyring = (0.161 * volume) / (-Math.Log(1 - (sumOfAbsorptionArea2000Hz / TotalArea)) * TotalArea);
                    double RT4000_Eyring = (0.161 * volume) / (-Math.Log(1 - (sumOfAbsorptionArea4000Hz / TotalArea)) * TotalArea);

                    // Add frequency-RT pairs to the list
                    eyringRT.Add((125, RT125));
                    eyringRT.Add((250, RT250));
                    eyringRT.Add((500, RT500));
                    eyringRT.Add((1000, RT1000));
                    eyringRT.Add((2000, RT2000));
                    eyringRT.Add((4000, RT4000));


                    double[] values = { RT250_Eyring, RT500_Eyring, RT1000_Eyring, RT2000_Eyring };

                    double average = values.Average();

                    double Tsollmax_125 = average + 0.06666;
                    double Tsollmax_250 = average + 0.05;
                    double Tsollmax_500 = average + 0.05;
                    double Tsollmax_1000 = average + 0.05;
                    double Tsollmax_2000 = average + 0.05;
                    double Tsollmax_4000 = average + 0.05;

                    double Tsollmin_125 = average - 0.06666;
                    double Tsollmin_250 = average - 0.05;
                    double Tsollmin_500 = average - 0.05;
                    double Tsollmin_1000 = average - 0.05;
                    double Tsollmin_2000 = average - 0.05;
                    double Tsollmin_4000 = average - 0.05;

                    // Add frequency-Tmax pairs to the list
                    
                    Tmax.Add((125, Tsollmax_125));
                    Tmax.Add((250, Tsollmax_250));
                    Tmax.Add((500, Tsollmax_500));
                    Tmax.Add((1000, Tsollmax_1000));
                    Tmax.Add((2000, Tsollmax_2000));
                    Tmax.Add((4000, Tsollmax_4000));

                    Tmin.Add((125, Tsollmin_125));
                    Tmin.Add((250, Tsollmin_250));
                    Tmin.Add((500, Tsollmin_500));
                    Tmin.Add((1000, Tsollmin_1000));
                    Tmin.Add((2000, Tsollmin_2000));
                    Tmin.Add((4000, Tsollmin_4000));







                // Display the RT values in a TaskDialog
                string RTMessage_Eyring = $"Reverberation Time using Eyring (RT):\n";

                    RTMessage_Eyring += $"\nRT at 125Hz: {RT125_Eyring:F3} seconds\n";
                    RTMessage_Eyring += $"\nRT at 250Hz: {RT250_Eyring:F3} seconds\n";
                    RTMessage_Eyring += $"\nRT at 500Hz: {RT500_Eyring:F3} seconds\n";
                    RTMessage_Eyring += $"\nRT at 1000Hz: {RT1000_Eyring:F3} seconds\n";
                    RTMessage_Eyring += $"\nRT at 2000Hz: {RT2000_Eyring:F3} seconds\n";
                    RTMessage_Eyring += $"\nRT at 4000Hz: {RT4000_Eyring:F3} seconds\n";



                    TaskDialog RTDialog_Eyring = new TaskDialog("Reverberation Time Eyring");
                    RTDialog_Eyring.MainContent = RTMessage_Eyring;
                    RTDialog_Eyring.Show();



                    // Close the connection
                    connection.Close();


                    // Create an instance of UserControl1 and display it
                    UserControl1 dialog = new UserControl1(frequencies, sabineRT, eyringRT, Tmax, Tmin);
                    dialog.ShowDialog();


                    // Calculate Schroeder Frequency


                    double Schroeder_frequency = 1896 * Math.Sqrt(0.2 / volume);

                    // Display the Schroeder frequency values in a TaskDialog
                    string Schroeder_message ="\n";

                    Schroeder_message += $"Schroeder frequency: {Schroeder_frequency:F3} Hertz\n";

                    TaskDialog Dialog_Schroeder = new TaskDialog("Schroeder frequency");
                    Dialog_Schroeder.MainContent = Schroeder_message;
                    Dialog_Schroeder.Show();



                    // Display the standing wave values 
                    double c = 343;
                    double n = 1;
                    double m = 1;
                    double p = 1;

                    // Ask the user to input n, m, p
                    //string inputN = Interaction.InputBox("Enter an integer for 'n':", "Input 'n'", "1", -1, -1);
                    //string inputM = Interaction.InputBox("Enter an integer for 'm':", "Input 'm'", "1", -1, -1);
                    //string inputP = Interaction.InputBox("Enter an integer for 'p':", "Input 'p'", "1", -1, -1);

                    // Convert the input to integers
                    //double n = double.Parse(inputN);
                    //double m = double.Parse(inputM);
                    //double p = double.Parse(inputP);


                    double Axial_Modes = (c / 2) * (n / lengthSMeters);
                    double Tangential_Modes = (c / 2) * Math.Sqrt(Math.Pow((n / lengthSMeters), 2) + Math.Pow((m / widthSMeters), 2));
                    double Oblique_Modes = (c / 2) * Math.Sqrt(Math.Pow((n / lengthSMeters), 2) + Math.Pow((m / widthSMeters), 2) + Math.Pow((p / wallHeight), 2));


                    // Display the Schroeder standing wave in a TaskDialog
                    string wave_message = "Standing waves frequencies\n";

                    wave_message += $"\nLenght x Width x height: {lengthSMeters:F3} x  {widthSMeters:F3} x {wallHeight:F3}\n";
                    wave_message += $"\nAxial Modes frequencies: {Axial_Modes:F3} Hertz\n";
                    wave_message += $"\nTangential Modes frequencies: {Tangential_Modes:F3} Hertz\n";
                    wave_message += $"\nOblique Modes frequencies: {Oblique_Modes:F3} Hertz\n";


                    TaskDialog wave_dialog= new TaskDialog("Standing waves frequencies");
                    wave_dialog .MainContent = wave_message;
                    wave_dialog.Show();





            }



            return Result.Succeeded;
        }



    }


}
    








