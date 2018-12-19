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
        // TDODO
        // 1) Add progress dialog for long traces.
        // 2) Handle condtions when more than 1 manhole is selected.
        //




        // GLOBAL VARIABLES FOR DICTIONARIES
        // *********************************
        // arcNodeListDict: paired list of From (upstream) node and To (downstream) node for each arc.
        // sewerLineObjID --> {upStreamManhole, downStreamManhole} -- Upstream manhole will be in position "0"
        Dictionary<int, List<string>> arcNodeListDict = new Dictionary<int, List<string>>();


        // nodeArcListDict: Lists all arcs attached to the specified node.
        // manhole# --> {sewerlineObjID, sewerlineObjID, ...} -- can have more than 2 arcs.
        Dictionary<string, List<int>> nodeArcListDict = new Dictionary<string, List<int>>();
        //private readonly object value;

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
                        // Build the Dictionaries now that it has been confirmed that the necessary layers are in map.
                        TraceUtilities.BuildDictionariesAsync(arcNodeListDict, nodeArcListDict);

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
                        //MessageBox.Show(mhNum, "Selected Manhole");

                        // Create the workArcList to store the Arcs(VALUES as int) from the nodeArcListDict 
                        // for the selected mhNum(KEY as string)
                        List<int> workArcList = new List<int>();

                        // Output the Arc ObjectID (VALUES) as List<int> for the selected mhNum (KEY)
                        nodeArcListDict.TryGetValue(mhNum, out List<int> arcValues);

                        // Loop through Output List<int> and add VALUE to workArcList
                        foreach (var arcValue in arcValues)
                        {
                            workArcList.Add(arcValue);
                            //Debug.WriteLine("********************\n"+ arcValue);
                        }
                        //Debug.WriteLine("*************************\n" + workArcList.Count);

                        // Create the removeArcsList to store the Arcs(VALUES as int)  
                        // 
                        List<int> removeArcsList = new List<int>();

                        // loop through workArcList check if it contains upstream node = mhNum selected by user
                        foreach (var workArc in workArcList)
                        {
                            //Debug.WriteLine($"********************\nAttached arc: {workArc}" );

                            // Get the list of Values from the arcNodeListDict for the current arc KEY (workArc) in workArcList.
                            arcNodeListDict.TryGetValue(workArc, out List<string> nodeWorkVals);
                            
                            //Get the upstream manhole [0] from list.
                            string upMH = nodeWorkVals[0];
                            //Debug.WriteLine($"********************\nUpstream Manhole: {upMH}\nSelected Manhole: {mhNum}");

                            // Check if upstream manhole and selected manhole are the same.
                            if (upMH == mhNum)
                            {
                                // Add to removeArcList
                                removeArcsList.Add(workArc);
                                //Debug.WriteLine($"********************\nUpstream manhole == Selected Manhole\nArc Added to removeArcList: {workArc}\n" + 
                                //    "This means the Arc is downstream of selected manhole and should be added to the remove list.");
                            }

                            else
                            {
                                //Debug.WriteLine($"********************\nUpstream manhole({upMH}) <> Selected Manhole({mhNum})");
                            }


                        }

                        // Loop through removeArcsList remove each removeArc in list from workArcList (this is getting confusing) 
                        foreach (var removeArc in removeArcsList)
                        {
                            //Debug.WriteLine($"********************\nremoveArc: {removeArc}");

                            // Remove from workArcList
                            workArcList.Remove(removeArc);
                        }

                        if (workArcList.Count() == 0)
                        {
                            MessageBox.Show("No upstream sewer lines found.", "Warning");
                        }

                        // Create dictionary to store upstream arcs that will be used to create query string.
                        // Only reason dictionary is used is because it's a quick way to prevent duplicate KEYS.
                        Dictionary<string, string> upStreamArcDict = new Dictionary<string, string>();

                        string upStreamNode = "";
                        List<string> workManholeList = new List<string>();

                        int loopCount = 0;

                        // At start of loop, workArcList has >0 arcs from initial selection.
                        do
                        {
                            loopCount++;
                            //Debug.WriteLine($"********************\nLoop count: {loopCount}");

                            workManholeList.Clear();
                            removeArcsList.Clear();

                            foreach (var arc in workArcList)
                            {
                                if (upStreamArcDict.ContainsKey(arc.ToString()))
                                {
                                    //Debug.WriteLine($"********************\nERROR: Key already exists in upStreamArcDict dictionary: {arc}" );
                                }

                                else
                                {
                                    // Add arc to upstream arc dictionary
                                    upStreamArcDict.Add(arc.ToString(), "TRUE");
                                    //Debug.WriteLine($"********************\nArc added to upStreamArcDict dictionary: {arc}");
                                }
                            }

                            foreach (var upArc in workArcList)
                            {
                                arcNodeListDict.TryGetValue(upArc, out List<string> nodeWorkVals);

                                //Get the upstream manhole [0] from list.
                                string upMH = nodeWorkVals[0];

                                // Add upstream manhole for selected upstream arc to workManholeList.
                                // This will be used to get more more upstream arcs later.
                                workManholeList.Add(upMH);
                                //Debug.WriteLine($"********************\nUpstream Manhole added to workManholeList list: {upMH}");
                            }

                            // Clear workArcList to add new arcs later.
                            workArcList.Clear();
                            //Debug.WriteLine($"********************\nworkArcList list cleared");

                            // Get all the arcs connected to all the manholes in the workManholeList using nodeArcListDict dictionary.
                            // Add these arcs to workArcList.
                            foreach (var mh in workManholeList)
                            {
                                // Get all the arcs attached to manhole/node.
                                //Debug.WriteLine($"********************\nManhole number (workManholeList): {mh}");
                                nodeArcListDict.TryGetValue(mh, out List<int> arcVals);
                                // Add list of arcs to workArcList list.
                                workArcList.AddRange(arcVals);
                                //Debug.WriteLine($"********************\nNumber of arcs attached to {mh}: {arcVals.Count}");
                            }

                            // Loop through all the arcs in workArcList and for arcs that have upstream manholes in the workManholeList,
                            // add that arc to removeArcsList.
                            foreach (var arc2 in workArcList)
                            {
                                // get list of nodes for arcs in workArcList.
                                arcNodeListDict.TryGetValue(arc2, out List<string> nodeWorkVals2);

                                // Get the upstream manhole [0] from list.
                                string upMH = nodeWorkVals2[0];

                                // Check workManholeList for upMH and remove from removeArcList if TRUE.
                                if (workManholeList.Contains(upMH))
                                {
                                   removeArcsList.Add(arc2);
                                }
                            }

                            // Remove arcs in removeArcsList from workArcsList
                            foreach (var arc3 in removeArcsList)
                            {
                                workArcList.Remove(arc3);
                                //Debug.WriteLine($"********************\n{arc3} removed from workArcList");

                            }

                            //Debug.WriteLine($"********************\nNumber of arcs in workArcList: {workArcList.Count}");


                            // Loop through again if condition is met.
                        } while (workArcList.Count > 0);

                        //Debug.WriteLine($"********************\nCount: {upStreamArcDict.Keys.Count}");


                        // Build the query string from the upStreamArcDict KEYS to select the upstream sewer lines.
                        int count = 0;
                        //Debug.WriteLine($"********************\nLoop count: {count}");
                        string queryString = "";
                        //Debug.WriteLine($"********************\nQuery String: {queryString}");
                        foreach (var key in upStreamArcDict.Keys)
                        {
                            //Debug.WriteLine($"********************\nLoop count: {count}");
                            if (count == 0)
                            {
                                queryString = queryString + $"OBJECTID IN ({key}";
                                //Debug.WriteLine($"********************\nKey: {key} \nQuery String: {queryString}");
                            }
                            else if (count < upStreamArcDict.Keys.Count)
                            {
                                queryString = queryString + $",{key}";
                                //Debug.WriteLine($"********************\nKey: {key} \nQuery String: {queryString}");
                            }
                            //else 
                            //{
                            //    queryString = queryString + $")";
                            //    Debug.WriteLine($"********************\nQuery String: {queryString}");
                            //}
                            count++;


                        }
                        queryString = queryString + $")";
                        //Debug.WriteLine($"********************\nCompleted query string: {queryString}");

                        QueryFilter queryFilter = new QueryFilter { WhereClause = queryString };
                        var featLayers = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(s => s.Name == "Sewer Lines");


                        featLayers.Select(queryFilter, SelectionCombinationMethod.New);



                    }

                }

                catch (Exception)
                {
                    string caption = "Upstream trace failed!";
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
