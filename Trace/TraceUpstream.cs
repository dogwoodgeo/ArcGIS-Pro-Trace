using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace Trace
{
    internal class TraceUpstream : MapTool
    {
        Dictionary<int, List<string>> arcDict = new Dictionary<int, List<string>>();
        Dictionary<string, List<int>> nodeDict = new Dictionary<string, List<int>>();

        public TraceUpstream()
        {
            IsSketchTool = true;
            SketchType = SketchGeometryType.Rectangle;
            SketchOutputMode = SketchOutputMode.Screen;
        }
        protected override Task OnToolActivateAsync(bool active)
        {

            return QueuedTask.Run(() =>
            {
                try
                {

                    var map = MapView.Active.Map;
                    map.SetSelection(null);
                    var mhExists = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().Any(m => m.Name == "Manholes");
                    var sewerExists = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().Any(s => s.Name == "Sewer Lines");

                    // Check for the SEWER LINES Layer and MANHOLES layers in the map.
                    if (mhExists == false && sewerExists == false)
                    {
                        MessageBox.Show("Manholes & Sewers are missing from map.", "Message");
                    }
                    else if (mhExists == false && sewerExists)
                    {
                        MessageBox.Show("Sewer Lines layer is present. \n\nManholes layer is missing from map.", "Message");
                    }
                    else if (mhExists && sewerExists == false)
                    {
                        MessageBox.Show("Manholes layer is present. \n\nSewers layer is missing from map.", "Message");
                    }
                    else
                    {
                        // Build the Dictionaries mow that it has been confirmed that the necessary layers are in map.
                        TraceUtilities.BuildDictionariesAsync(arcDict, nodeDict);

                        //Make manholes the only selectabe layer in map.
                        var layers = map.GetLayersAsFlattenedList().OfType<FeatureLayer>();
                        foreach (var layer in layers)
                        {

                            if (layer.Name == "Manholes")
                            {
                                layer.SetSelectable(true);
                            }
                            else
                            {
                                layer.SetSelectable(false);
                            }
                        }
                    }
                }

                catch (Exception)
                {
                    string caption = "Failed to select manhole!";
                    string message = "Process failed. \n\nSave and restart ArcGIS Pro and try process again.\n\n" +
                        "If problem persist, contact your local GIS nerd.";

                    //Using the ArcGIS Pro SDK MessageBox class
                    MessageBox.Show(message, caption);
                }
            });
        }

        protected override Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            return QueuedTask.Run(() =>
            {
                try
                {
                    ActiveMapView.SelectFeatures(geometry, SelectionCombinationMethod.New);


                    var map = MapView.Active.Map;
 
                    // get the currently selected features in the map
                    var selectedFeatures = map.GetSelection();

                    if (selectedFeatures.Count == 0)
                    {
                        MessageBox.Show("No manhole was selected.\n\nTry selecting a manhole again.", "Warning");
                    }
                    else if (selectedFeatures.Count > 1)
                    {
                        MessageBox.Show("More than one manhole selected.\n\nTry selecting manholes again.", "Warning");
                    }
                    else
                    {
                        // get the first layer and its corresponding selected feature OIDs
                        var firstSelectionSet = selectedFeatures.First();

                        // create an instance of the inspector class
                        var inspector = new Inspector();

                        // load the selected features into the inspector using a list of object IDs
                        inspector.Load(firstSelectionSet.Key, firstSelectionSet.Value);

                        //get the value of
                        string mhNum = inspector["MH_NO"].ToString();
                        MessageBox.Show(mhNum, "Selected Manhole");

                    }

                }

                catch (Exception)
                {
                    string caption = "Failed to select manhole!";
                    string message = "Process failed. \n\nSave and restart ArcGIS Pro and try process again.\n\n" +
                        "If problem persist, contact your local GIS nerd.";

                    //Using the ArcGIS Pro SDK MessageBox class
                    MessageBox.Show(message, caption);
                }
                return true;
            });
        }
    }
}
