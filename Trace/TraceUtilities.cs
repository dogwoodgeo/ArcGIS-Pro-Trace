using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System.Diagnostics;

namespace Trace
{
    internal class TraceUtilities : Module
    {
        private static TraceUtilities _this = null;

        /// <summary>
        /// Retrieve the singleton instance to this module here
        /// </summary>
        public static TraceUtilities Current
        {
            get
            {
                return _this ?? (_this = (TraceUtilities)FrameworkApplication.FindModule("Trace_Module"));
            }
        }

        #region Overrides
        /// <summary>
        /// Called by Framework when ArcGIS Pro is closing
        /// </summary>
        /// <returns>False to prevent Pro from closing, otherwise True</returns>
        protected override bool CanUnload()
        {
            //TODO - add your business logic
            //return false to ~cancel~ Application close
            return true;
        }

        #endregion Overrides


        public static void BuildDictionariesAsync(Dictionary<int, List<string>> arcNodeListDictionary, Dictionary<string, List<int>> nodeArcListDictionary)
        {
            QueuedTask.Run(() =>
            {

                // Used to free up process memory. Without this the trace tool will 'hang up' after going between trace up and trace down.
                nodeArcListDictionary.Clear();
                arcNodeListDictionary.Clear();

                // Global vairables
                var map = MapView.Active.Map;
                var arcLayer = map.FindLayers("Sewer Lines").FirstOrDefault() as FeatureLayer;
                var nodeLayer = map.FindLayers("Manholes").FirstOrDefault() as FeatureLayer;

                var arcTableDef = arcLayer.GetTable().GetDefinition(); //table definition of featurelayer
                var nodeTableDef = nodeLayer.GetTable().GetDefinition(); //table definition of featurelayer

                // BUILD ARC AND NODE DICTIONARIES
                // arc ObjectID-- > { UPS_MH, DWN_MH}  Only 2 VALUES for each KEY
                // node MH Number -- >{ Arc OBjectID, Arc OBjectID, ...} Can have 1 or more VALUES for each KEY

                // Get the indices for the fields
                int objIDIdx = arcTableDef.FindField("ObjectID");
                int nodeUpIdx = arcTableDef.FindField("UNITID");
                int nodeDwnIdx = arcTableDef.FindField("UNITID2");

                using (RowCursor rowCursor = arcLayer.Search())
                {
                    while (rowCursor.MoveNext())
                    {
                        using (Row row = rowCursor.Current)
                        {
                            //List<string> unitIDValueList = new List<string>();
                            //List<int> objIDValueList = new List<int>();
                            var objIDVal = row.GetOriginalValue(objIDIdx);
                            var nodeUpVal = row.GetOriginalValue(nodeUpIdx);
                            var nodeDownVal = row.GetOriginalValue(nodeDwnIdx);

                            // Populate arcNodeListDictionary keys and values
                            if (arcNodeListDictionary.ContainsKey((int)objIDVal))
                            {
                                //Do nothing
                            }
                            else
                            {
                                arcNodeListDictionary.Add((int)objIDVal, new List<string>());
                                arcNodeListDictionary[(int)objIDVal].Add((string)nodeUpVal);
                                arcNodeListDictionary[(int)objIDVal].Add((string)nodeDownVal);
                            }

                            // Check of the nodeArcListDictionary contains nodeUpVal as KEY- Add nodeUpVal if FALSE
                            if (nodeArcListDictionary.ContainsKey((string)nodeUpVal))
                            {

                                nodeArcListDictionary[(string)nodeUpVal].Add((int)objIDVal);

                            }
                            else
                            {
                                nodeArcListDictionary.Add((string)nodeUpVal, new List<int>());
                                nodeArcListDictionary[(string)nodeUpVal].Add((int)objIDVal);
                            }

                            // Check of the nodeArcListDictionary contains nodeDownVal as KEY- Add nodeDownVal if FALSE
                            if (nodeArcListDictionary.ContainsKey((string)nodeDownVal))
                            {
                                //Do nothing
                                nodeArcListDictionary[(string)nodeDownVal].Add((int)objIDVal);
                            }
                            else
                            {
                                nodeArcListDictionary.Add((string)nodeDownVal, new List<int>());
                                nodeArcListDictionary[(string)nodeDownVal].Add((int)objIDVal);
                            }
                        }
                    }
                }
            });
        }
    }
}
