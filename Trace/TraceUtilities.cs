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


        public static void BuildDictionariesAsync(Dictionary<int, List<string>> arcDictionary, Dictionary<string, List<int>> nodeDictionary)
        {
            QueuedTask.Run(() =>
            {
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

                            // Populate arcDict keys and values
                            if (arcDictionary.ContainsKey((int)objIDVal))
                            {
                                //Do nothing
                            }
                            else
                            {
                                arcDictionary.Add((int)objIDVal, new List<string>());
                                arcDictionary[(int)objIDVal].Add((string)nodeUpVal);
                                arcDictionary[(int)objIDVal].Add((string)nodeDownVal);
                            }

                            // Check of the nodeDict contains nodeUpVal as KEY- Add nodeUpVal if FALSE
                            if (nodeDictionary.ContainsKey((string)nodeUpVal))
                            {

                                nodeDictionary[(string)nodeUpVal].Add((int)objIDVal);

                            }
                            else
                            {
                                nodeDictionary.Add((string)nodeUpVal, new List<int>());
                                nodeDictionary[(string)nodeUpVal].Add((int)objIDVal);
                            }

                            // Check of the nodeDict contains nodeDownVal as KEY- Add nodeDownVal if FALSE
                            if (nodeDictionary.ContainsKey((string)nodeDownVal))
                            {
                                //Do nothing
                                nodeDictionary[(string)nodeDownVal].Add((int)objIDVal);
                            }
                            else
                            {
                                nodeDictionary.Add((string)nodeDownVal, new List<int>());
                                nodeDictionary[(string)nodeDownVal].Add((int)objIDVal);
                            }
                        }
                    }
                }
            });
        }
    }
}
