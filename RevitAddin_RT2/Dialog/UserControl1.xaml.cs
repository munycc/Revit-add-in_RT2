using System.Collections.Generic;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using Autodesk.Revit.DB;


namespace RevitAddin_RT2
{
    public partial class UserControl1 : Window
    {
      
        public UserControl1(List<double> frequencies, List<(double Frequency, double RT)> sabineRT, List<(double Frequency, double RT)> eyringRT, List<(double Frequency, double RT)> Tmax, List<(double Frequency, double RT)> Tmin)
        {
            InitializeComponent();

            // Create a plot model for Sabine's RT
            var sabinePlotModel = new PlotModel { Title = "Reverberation Time Sabine" };
            sabinePlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Frequency (Hz)" });
            sabinePlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Reverberation Time" });

            // Create a series for Sabine's RT values
            var sabineSeries = new LineSeries
            {
                Title = "Sabine",
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerStroke = OxyColors.Blue,
                MarkerFill = OxyColors.Blue
            };


            // Add data points for Sabine's RT values
            for (int i = 0; i < frequencies.Count; i++)
            {
                sabineSeries.Points.Add(new DataPoint(frequencies[i], sabineRT[i].RT));
                
            }

            sabinePlotModel.Series.Add(sabineSeries);

            // Add data points for Tmax and Tmin
            var greyDotSeries = new LineSeries
            {
                Title = "GreyDotSeries",
                Color = OxyColors.Gray,
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerStroke = OxyColors.Gray,
                MarkerFill = OxyColors.Gray
            };
            for (int i = 0; i < frequencies.Count; i++)
            {
                greyDotSeries.Points.Add(new DataPoint(frequencies[i], Tmax[i].RT));

            }
            sabinePlotModel.Series.Add(greyDotSeries);

            // Add data points for Tmax and Tmin
            var greyDotSerieslow = new LineSeries
            {
                Title = "GreyDotSeries",
                Color = OxyColors.Gray,
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerStroke = OxyColors.Gray,
                MarkerFill = OxyColors.Gray
            };

            for (int i = 0; i < frequencies.Count; i++)
            {
                greyDotSerieslow.Points.Add(new DataPoint(frequencies[i], Tmin[i].RT));

            }

            sabinePlotModel.Series.Add(greyDotSerieslow);

            // Create a plot model for Eyring's RT
            var eyringPlotModel = new PlotModel { Title = "Reverberation Time Eyring" };
            eyringPlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Frequency (Hz)" });
            eyringPlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Reverberation Time" });

            // Create a series for Eyring's RT values
            var eyringSeries = new LineSeries
            {
                Title = "Eyring",
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerStroke = OxyColors.Red,
                MarkerFill = OxyColors.Red
            };

            // Add data points for Eyring's RT values
            for (int i = 0; i < frequencies.Count; i++)
            {
                eyringSeries.Points.Add(new DataPoint(frequencies[i], eyringRT[i].RT));
     
            }

            // Add the series to the plot model
            eyringPlotModel.Series.Add(eyringSeries);

            // Add data points for Tmax and Tmin

            // Add data points for Tmax and Tmin
            var greyDotSeries2 = new LineSeries
            {
                Title = "GreyDotSeries",
                Color = OxyColors.Gray,
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerStroke = OxyColors.Gray,
                MarkerFill = OxyColors.Gray
            };
            for (int i = 0; i < frequencies.Count; i++)
            {
                greyDotSeries2.Points.Add(new DataPoint(frequencies[i], Tmax[i].RT));

            }
            eyringPlotModel.Series.Add(greyDotSeries2);

            // Add data points for Tmax and Tmin
            var greyDotSerieslow2 = new LineSeries
            {
                Title = "GreyDotSeries",
                Color = OxyColors.Gray,
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerStroke = OxyColors.Gray,
                MarkerFill = OxyColors.Gray
            };

            for (int i = 0; i < frequencies.Count; i++)
            {
                greyDotSerieslow2.Points.Add(new DataPoint(frequencies[i], Tmin[i].RT));

            }
         
            eyringPlotModel.Series.Add(greyDotSerieslow2);




            // Assign the plot models to the PlotViews
            myPlotViewSabine.Model = sabinePlotModel;
            myPlotViewEyring.Model = eyringPlotModel;
        }

        

    }
}