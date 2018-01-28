﻿using Microsoft.Xna.Framework.Graphics;
using PyTK;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using xTile;
using xTile.Dimensions;
using xTile.Format;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace PyTK.Tiled
{
    public class NewTiledTmxFormat : IMapFormat
    {
        internal static IModHelper Helper { get; } = PyTKMod._helper;
        internal static IMonitor Monitor { get; } = PyTKMod._monitor;

        public string Name
        {
            get
            {
                return "Tiled XML Format [Updated]";
            }
        }

        public string FileExtensionDescriptor
        {
            get
            {
                return "Tiled XML Map Files (*.tmx) **";
            }
        }

        public string FileExtension
        {
            get
            {
                return "tmx";
            }
        }

        internal TiledMap TiledMap { get; set; }

        public CompatibilityReport DetermineCompatibility(Map map)
        {
            List<CompatibilityNote> compatibilityNoteList = new List<CompatibilityNote>();
            foreach (TileSheet tileSheet in map.TileSheets)
            {
                xTile.Dimensions.Size size = tileSheet.Margin;
                if (!size.Square)
                    compatibilityNoteList.Add(new CompatibilityNote(CompatibilityLevel.None, string.Format("Tilesheet {0}: Margin values ({1}) are not equal.", (object)tileSheet.Id, (object)tileSheet.Margin)));
                size = tileSheet.Spacing;
                if (!size.Square)
                    compatibilityNoteList.Add(new CompatibilityNote(CompatibilityLevel.None, string.Format("Tilesheet {0}: Spacing values ({1}) are not equal.", (object)tileSheet.Id, (object)tileSheet.Spacing)));
            }
            if (map.Layers.Count > 0)
            {
                Layer layer1 = map.Layers[0];
                bool flag1 = false;
                bool flag2 = false;
                bool flag3 = false;
                bool flag4 = false;
                foreach (Layer layer2 in map.Layers)
                {
                    if (layer2 != layer1)
                    {
                        if (layer2.LayerWidth != layer1.LayerWidth)
                            flag1 = true;
                        if (layer2.LayerHeight != layer1.LayerHeight)
                            flag2 = true;
                        if (layer2.TileWidth != layer1.TileWidth)
                            flag3 = true;
                        if (layer2.TileHeight != layer1.TileHeight)
                            flag4 = true;
                    }
                }
                if (flag1)
                    compatibilityNoteList.Add(new CompatibilityNote(CompatibilityLevel.None, "Layer widths do not match across all layers."));
                if (flag2)
                    compatibilityNoteList.Add(new CompatibilityNote(CompatibilityLevel.None, "Layer heights do not match across all layers."));
                if (flag3)
                    compatibilityNoteList.Add(new CompatibilityNote(CompatibilityLevel.None, "Tile widths do not match across all layers."));
                if (flag4)
                    compatibilityNoteList.Add(new CompatibilityNote(CompatibilityLevel.None, "Tile heights do not match across all layers."));
            }
            return new CompatibilityReport((IEnumerable<CompatibilityNote>)compatibilityNoteList);
        }

        public Map Load(Stream stream)
        {
            this.TiledMap = new TiledMap(XElement.Load(stream));
            Map map = new Map();
            if (this.TiledMap.Orientation != "orthogonal")
                throw new Exception("Only orthogonal Tiled maps are supported.");
            List<TiledProperty> properties = this.TiledMap.Properties;
            if (properties != null)
            {
                Action<TiledProperty> action = (Action<TiledProperty>)(prop =>
               {
                   if (prop.Name == "@Description")
                       map.Description = prop.Value;
                   else
                       map.Properties[prop.Name] = (PropertyValue)prop.Value;
               });
                properties.ForEach(action);
            }
            this.LoadTileSets(map);
            this.LoadLayers(map);
            this.LoadObjects(map);
            return map;
        }

        public void Store(Map map, Stream stream)
        {
            TiledMap tiledMap1 = new TiledMap();
            tiledMap1.Version = "1.0";
            tiledMap1.Orientation = "orthogonal";
            int layerWidth = map.GetLayer("Back").LayerWidth;
            tiledMap1.Width = layerWidth;
            int layerHeight = map.GetLayer("Back").LayerHeight;
            tiledMap1.Height = layerHeight;
            int tileWidth = 16; // map.GetLayer("Back").TileWidth;
            tiledMap1.TileWidth = tileWidth;
            int tileHeight = 16; //map.GetLayer("Back").TileHeight;
            tiledMap1.TileHeight = tileHeight;
            List<TiledProperty> tiledPropertyList = new List<TiledProperty>();
            tiledMap1.Properties = tiledPropertyList;
            List<TiledTileSet> tiledTileSetList = new List<TiledTileSet>();
            tiledMap1.TileSets = tiledTileSetList;
            List<TiledLayer> tiledLayerList = new List<TiledLayer>();
            tiledMap1.Layers = tiledLayerList;
            List<TiledObjectGroup> tiledObjectGroupList = new List<TiledObjectGroup>();
            tiledMap1.ObjectGroups = tiledObjectGroupList;
            TiledMap tiledMap2 = tiledMap1;
            if (map.Description.Length > 0)
                map.Properties["@Description"] = (PropertyValue)map.Description;
            foreach (KeyValuePair<string, PropertyValue> property in (IEnumerable<KeyValuePair<string, PropertyValue>>)map.Properties)
                tiledMap2.Properties.Add(new TiledProperty(property.Key, (string)property.Value));
            this.StoreTileSets(map, tiledMap2);
            this.StoreLayers(map, tiledMap2);
            this.StoreObjects(map, tiledMap2);
            tiledMap2.ToXml().Save(stream);
        }

        public void LoadTileSets(Map map)
        {
            List<TiledTileSet> tileSets = this.TiledMap.TileSets;
            if (tileSets == null)
                return;
            
            Action<TiledTileSet> action1 = (Action<TiledTileSet>)(tileSet =>
           {
               
               xTile.Dimensions.Size sheetSize = new xTile.Dimensions.Size();
               try
               {
                       sheetSize.Width = (tileSet.Image.Width + tileSet.Spacing - tileSet.Margin) / (tileSet.TileWidth + tileSet.Spacing);
                       sheetSize.Height = (tileSet.Image.Height + tileSet.Spacing - tileSet.Margin) / (tileSet.TileHeight + tileSet.Spacing);
               }
               catch (Exception ex)
               {
                   throw new Exception("Unable to determine sheet size", ex);
               }
               tileSet.TileWidth = 64;
               tileSet.TileHeight = 64;
               TileSheet tileSheet = new TileSheet(tileSet.SheetName, map, tileSet.Image.Source, sheetSize, new xTile.Dimensions.Size(tileSet.TileWidth, tileSet.TileHeight))
               {
                   Spacing = new xTile.Dimensions.Size(tileSet.Spacing),
                   Margin = new xTile.Dimensions.Size(tileSet.Margin)
               };
               tileSheet.Properties["@FirstGid"] = (PropertyValue)tileSet.FirstGid;
               tileSheet.Properties["@LastGid"] = (PropertyValue)tileSet.LastGid;
               List<TiledTile> tiles = tileSet.Tiles;
               if (tiles != null)
               {
                   Action<TiledTile> action2 = (Action<TiledTile>)(tile =>
             {
                   List<TiledProperty> properties = tile.Properties;
                   if (properties == null)
                       return;
                   Action<TiledProperty> action3 = (Action<TiledProperty>)(prop => tileSheet.Properties[string.Format("@TileIndex@{0}@{1}", (object)tile.TileId, (object)prop.Name)] = (PropertyValue)prop.Value);
                   properties.ForEach(action3);
               });
                   tiles.ForEach(action2);
               }
               map.AddTileSheet(tileSheet);
           });
            tileSets.ForEach(action1);
        }

        public void LoadLayers(Map map)
        {
            if (map.TileSheets.Count == 0)
                throw new Exception("Must load at least one tileset to determine layer tile size");
            List<TiledLayer> layers = this.TiledMap.Layers;
            if (layers == null)
                return;
            Action<TiledLayer> action1 = (Action<TiledLayer>)(layer =>
           {
               Layer layer1 = new Layer(layer.Name, map, new xTile.Dimensions.Size(layer.Width, layer.Height), map.TileSheets[0].TileSize);
               int num = !layer.Hidden ? 1 : 0;
               layer1.Visible = num != 0;
               Layer mapLayer = layer1;
               if (layer.Properties == null)
                   layer.Properties = new List<TiledProperty>();
               List<TiledProperty> properties = layer.Properties;
               if (properties != null)
               {
                   Action<TiledProperty> action2 = (Action<TiledProperty>)(prop =>
             {
                   if (prop.Name == "@Description")
                       mapLayer.Description = prop.Value;
                   else
                       mapLayer.Properties[prop.Name] = (PropertyValue)prop.Value;
               });
                   properties.ForEach(action2);
               }
               if (!(layer.Data.EncodingType == "csv"))
                   throw new Exception(string.Format("Unknown encoding setting ({0})", (object)layer.Data.EncodingType));
               this.LoadLayerDataCsv(mapLayer, layer);
               map.AddLayer(mapLayer);
           });
            layers.ForEach(action1);
        }

        internal void LoadLayerDataCsv(Layer mapLayer, TiledLayer tiledLayer)
        {
            string[] strArray = tiledLayer.Data.Data.Split(new char[4]
            {
        ',',
        '\r',
        '\n',
        '\t'
            }, StringSplitOptions.RemoveEmptyEntries);
            Location origin = Location.Origin;
            foreach (string s in strArray)
            {
                int gid = int.Parse(s);
                mapLayer.Tiles[origin] = this.LoadTile(mapLayer, gid);
                ++origin.X;
                if (origin.X >= mapLayer.LayerWidth)
                {
                    origin.X = 0;
                    ++origin.Y;
                }
            }
        }

        internal Tile LoadTile(Layer layer, int gid)
        {
            if (gid == 0)
                return (Tile)null;
            TileSheet selectedTileSheet = (TileSheet)null;
            int tileIndex = -1;
            foreach (TileSheet tileSheet in layer.Map.TileSheets)
            {
                int property1 = (int)tileSheet.Properties["@FirstGid"];
                int property2 = (int)tileSheet.Properties["@LastGid"];
                if (gid >= property1 && gid <= property2)
                {
                    selectedTileSheet = tileSheet;
                    tileIndex = gid - property1;
                    break;
                }
            }
            if (selectedTileSheet == null)
                throw new Exception(string.Format("Invalid tile gid: {0}", (object)gid));
            TiledTileSet tiledTileSet;
            if ((tiledTileSet = this.TiledMap.TileSets.FirstOrDefault<TiledTileSet>((Func<TiledTileSet, bool>)(tileSheet => tileSheet.SheetName == selectedTileSheet.Id))) == null)
                return (Tile)new StaticTile(layer, selectedTileSheet, BlendMode.Alpha, tileIndex);
            TiledTile tiledTile1 = tiledTileSet.Tiles.FirstOrDefault<TiledTile>((Func<TiledTile, bool>)(tiledTile =>
           {
               if (tiledTile.TileId == tileIndex)
                   return tiledTile.Animation != null;
               return false;
           }));
            if (tiledTile1 == null || tiledTile1.Animation.Count <= 0)
                return (Tile)new StaticTile(layer, selectedTileSheet, BlendMode.Alpha, tileIndex);
            StaticTile[] array = tiledTile1.Animation.Select<TiledAnimationFrame, StaticTile>((Func<TiledAnimationFrame, StaticTile>)(frame => new StaticTile(layer, selectedTileSheet, BlendMode.Alpha, frame.TileId))).ToArray<StaticTile>();
            return (Tile)new AnimatedTile(layer, array, (long)tiledTile1.Animation[0].Duration);
        }

        internal void LoadObjects(Map map)
        {
            List<TiledObjectGroup> objectGroups = this.TiledMap.ObjectGroups;
            if (objectGroups == null)
                return;
            Action<TiledObjectGroup> action1 = (Action<TiledObjectGroup>)(objectGroup =>
           {
               Layer layer = map.GetLayer(objectGroup.Name);
               foreach (TiledObject tiledObject in objectGroup.Objects)
               {
                   if (!(tiledObject.Name != "TileData"))
                   {
                       Tile tile = layer.Tiles[tiledObject.XPos / 16, tiledObject.YPos / 16];
                       List<TiledProperty> properties = tiledObject.Properties;
                       if (properties != null)
                       {
                           Action<TiledProperty> action2 = (Action<TiledProperty>)(prop => tile.Properties[prop.Name] = (PropertyValue)prop.Value);
                           properties.ForEach(action2);
                       }
                   }
               }
           });
            objectGroups.ForEach(action1);
        }

        internal void StoreTileSets(Map map, TiledMap tiledMap)
        {
            int num = 1;
            foreach (TileSheet tileSheet in map.TileSheets)
            {
                TiledTileSet tiledTileSet1 = new TiledTileSet();
                tiledTileSet1.FirstGid = num;
                string id = tileSheet.Id;
                tiledTileSet1.SheetName = id;
                int tileWidth = tileSheet.TileWidth;
                tiledTileSet1.TileWidth = tileWidth;
                int tileHeight = tileSheet.TileHeight;
                tiledTileSet1.TileHeight = tileHeight;
                int spacingWidth = tileSheet.SpacingWidth;
                tiledTileSet1.Spacing = spacingWidth;
                int marginWidth = tileSheet.MarginWidth;
                tiledTileSet1.Margin = marginWidth;
                int tileCount = tileSheet.TileCount;
                tiledTileSet1.TileCount = tileCount;
                int sheetWidth = tileSheet.SheetWidth;
                tiledTileSet1.Columns = sheetWidth;
                tiledTileSet1.Image = new TiledTileSetImage()
                {
                    Source = tileSheet.ImageSource,
                    Width = tileSheet.SheetWidth * tileSheet.TileWidth,
                    Height = tileSheet.SheetHeight * tileSheet.TileHeight
                };
                List<TiledTile> tiledTileList = new List<TiledTile>();
                tiledTileSet1.Tiles = tiledTileList;
                TiledTileSet tiledTileSet2 = tiledTileSet1;
                foreach (KeyValuePair<string, PropertyValue> property in (IEnumerable<KeyValuePair<string, PropertyValue>>)tileSheet.Properties)
                {
                    if (property.Key.StartsWith("@Tile@") || property.Key.StartsWith("@TileIndex@"))
                    {
                        string[] strArray = property.Key.Split(new char[1]
                        {
              '@'
                        }, StringSplitOptions.RemoveEmptyEntries);
                        int tileIndex = int.Parse(strArray[1]);
                        string name = strArray[2];
                        TiledTile tiledTile1;
                        if ((tiledTile1 = tiledTileSet2.Tiles.FirstOrDefault<TiledTile>((Func<TiledTile, bool>)(tiledTile => tiledTile.TileId == tileIndex))) != null)
                            tiledTile1.Properties.Add(new TiledProperty(name, (string)tileSheet.Properties[property.Key]));
                        else
                            tiledTileSet2.Tiles.Add(new TiledTile()
                            {
                                TileId = tileIndex,
                                Properties = new List<TiledProperty>()
                {
                  new TiledProperty(name, (string) tileSheet.Properties[property.Key])
                }
                            });
                    }
                }
                tiledMap.TileSets.Add(tiledTileSet2);
                num += tileSheet.TileCount;
            }
        }

        internal void StoreLayers(Map map, TiledMap tiledMap)
        {
            foreach (Layer layer in map.Layers)
            {
                TiledLayer tiledLayer1 = new TiledLayer();
                tiledLayer1.Name = layer.Id;
                tiledLayer1.Width = layer.LayerWidth;
                tiledLayer1.Height = layer.LayerHeight;
                int num1 = !layer.Visible ? 1 : 0;
                tiledLayer1.Hidden = num1 != 0;
                tiledLayer1.Data = new TiledLayerData()
                {
                    EncodingType = "csv"
                };
                List<TiledProperty> tiledPropertyList = new List<TiledProperty>();
                tiledLayer1.Properties = tiledPropertyList;
                TiledLayer tiledLayer2 = tiledLayer1;
                List<int> intList = new List<int>();
                for (int index1 = 0; index1 < layer.LayerHeight; ++index1)
                {
                    for (int index2 = 0; index2 < layer.LayerWidth; ++index2)
                    {
                        Tile tile = layer.Tiles[index2, index1];
                        if (tile is AnimatedTile animatedTile)
                        {
                            foreach (TiledTileSet tileSet in tiledMap.TileSets.Where(t => t.SheetName == animatedTile.TileSheet.Id))
                            {
                                TiledTile tiledTile1 = tileSet.Tiles.FirstOrDefault<TiledTile>((Func<TiledTile, bool>)(tiledTile => tiledTile.TileId == tile.TileIndex));
                                if (tiledTile1 == null)
                                    tileSet.Tiles.Add(new TiledTile()
                                    {
                                        TileId = tile.TileIndex,
                                        Animation = ((IEnumerable<StaticTile>)animatedTile.TileFrames).Select<StaticTile, TiledAnimationFrame>((Func<StaticTile, TiledAnimationFrame>)(frame => new TiledAnimationFrame()
                                        {
                                            TileId = frame.TileIndex,
                                            Duration = (int)animatedTile.FrameInterval
                                        })).ToList<TiledAnimationFrame>()
                                    });
                                else if (tiledTile1.Animation == null)
                                    tiledTile1.Animation = ((IEnumerable<StaticTile>)animatedTile.TileFrames).Select<StaticTile, TiledAnimationFrame>((Func<StaticTile, TiledAnimationFrame>)(frame => new TiledAnimationFrame()
                                    {
                                        TileId = frame.TileIndex,
                                        Duration = (int)animatedTile.FrameInterval
                                    })).ToList<TiledAnimationFrame>();
                            }
                        }
                        int num2 = 0;
                        if (tile != null)
                        {
                            int tileIndex = tile.TileIndex;
                            TiledTileSet tiledTileSet = tiledMap.TileSets.FirstOrDefault<TiledTileSet>((Func<TiledTileSet, bool>)(tileSet => tileSet.SheetName == tile.TileSheet.Id));
                            int num3 = tiledTileSet != null ? tiledTileSet.FirstGid : 1;
                            num2 = tileIndex + num3;
                        }
                        intList.Add(num2);
                    }
                }
                tiledLayer2.Data.Data = string.Join<int>(",", (IEnumerable<int>)intList);
                if (layer.Description.Length > 0)
                    tiledLayer2.Properties.Add(new TiledProperty("@Description", layer.Description));
                tiledMap.Layers.Add(tiledLayer2);
            }
        }

        internal void StoreObjects(Map map, TiledMap tiledMap)
        {
            foreach (Layer layer in map.Layers)
            {
                TiledObjectGroup tiledObjectGroup = new TiledObjectGroup()
                {
                    Name = layer.Id,
                    Objects = new List<TiledObject>()
                };
                for (int index1 = 0; index1 < layer.LayerHeight; ++index1)
                {
                    for (int index2 = 0; index2 < layer.LayerWidth; ++index2)
                    {
                        Tile tile = layer.Tiles[index2, index1];
                        if ((tile != null ? tile.Properties : (IPropertyCollection)null) != null && tile.Properties.Any<KeyValuePair<string, PropertyValue>>())
                        {
                            TiledObject tiledObject = new TiledObject()
                            {
                                ObjectId = tiledMap.NextObjectId,
                                Name = "TileData",
                                XPos = index2 * 16,
                                YPos = index1 * 16,
                                Width = 16,
                                Height = 16,
                                Properties = new List<TiledProperty>()
                            };
                            foreach (KeyValuePair<string, PropertyValue> property in (IEnumerable<KeyValuePair<string, PropertyValue>>)tile.Properties)
                                tiledObject.Properties.Add(new TiledProperty(property.Key, (string)property.Value));
                            tiledObjectGroup.Objects.Add(tiledObject);
                            ++tiledMap.NextObjectId;
                        }
                    }
                }
                tiledMap.ObjectGroups.Add(tiledObjectGroup);
            }
        }
    }
}