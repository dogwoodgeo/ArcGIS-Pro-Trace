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
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace Trace
{
    internal class AddManholesTrace : Button
    {
        protected override void OnClick()
        {
            ProgressDialog progDial = new ProgressDialog("I'm doing my thing.\nPlease be patient, Human.", false);

            QueuedTask.Run(() =>
            {

                try
                {
                    var map = MapView.Active.Map;
                    var mhExists = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().Any(m => m.Name == "Manholes");
                    var sewerExists = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().Any(s => s.Name == "Sewer Lines");
                    
                    // Check for the SEWER LINES Layer and MANHOLES layers in the map.
                    if (!mhExists && !sewerExists)
                    {
                        MessageBox.Show("Manholes & Sewers are missing from map.", "WARNING");
                    }
                    else if (!mhExists && sewerExists)
                    {
                        MessageBox.Show("Sewer Lines layer is present. \n\nManholes layer is missing from map.", "WARNING");
                    }
                    else if (mhExists && !sewerExists)
                    {
                        MessageBox.Show("Manholes layer is present. \n\nSewers layer is missing from map.", "WARNING");
                    }
                    else
                    {

                        var sewerLines = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(s => s.Name == "Sewer Lines");
                        var manholes = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(s => s.Name == "Manholes");
                        var selectCount = sewerLines.SelectionCount;

                        if (selectCount == 0)
                        {
                            MessageBox.Show("No sewer trace is in map! \n\nA sewer trace must be performed to add\nmanholes associate with trace.", "WARNING");
                        }

                        else
                        {

                            progDial.Show();
                            string spatialRelate = "INTERSECT";
                            int distance = 0;
                            string selectType = "NEW_SELECTION";
                            string invertRelate = "NOT_INVERT";

                            var parameters = Geoprocessing.MakeValueArray(manholes, spatialRelate, sewerLines, distance, selectType, invertRelate);

                            Geoprocessing.ExecuteToolAsync("management.SelectLayerByLocation", parameters);
                            progDial.Hide();
                        }
                    }

                }

                catch (Exception)
                {
                    string caption = "Process Failed!";
                    string message = "Failed to add manholes selection to trace. \n\nSave and restart ArcGIS Pro and try process again.\n\n" +
                        "If problem persist, contact your local GIS nerd.";
                    progDial.Hide();


                    //Using the ArcGIS Pro SDK MessageBox class
                    MessageBox.Show(message, caption);

                }
            });
        }
    }
}
