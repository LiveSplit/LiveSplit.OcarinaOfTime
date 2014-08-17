#define GAME_TIME

using LiveSplit.ASL;
using LiveSplit.Model;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace LiveSplit.UI.Components
{
    class Component : IComponent
    {
        //public ComponentSettings Settings { get; set; }

        public string ComponentName
        {
            get { return "Ocarina of Time Auto Splitter"; }
        }

        public float PaddingBottom { get { return 0; } }
        public float PaddingTop { get { return 0; } }
        public float PaddingLeft { get { return 0; } }
        public float PaddingRight { get { return 0; } }

        public bool Refresh { get; set; }

        public IDictionary<string, Action> ContextMenuControls { get; protected set; }

        public OcarinaOfTimeScript Script { get; set; }

        public Component()
        {
            //Settings = new ComponentSettings();
            Script = new OcarinaOfTimeScript();
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            Script.Update(state);
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
        {
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
        {
        }

        public float VerticalHeight
        {
            get { return 0; }
        }

        public float MinimumWidth
        {
            get { return 0; }
        }

        public float HorizontalWidth
        {
            get { return 0; }
        }

        public float MinimumHeight
        {
            get { return 0; }
        }

        public System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document)
        {
            return document.CreateElement("x");
        }

        public System.Windows.Forms.Control GetSettingsControl(UI.LayoutMode mode)
        {
            return null;
        }

        public void SetSettings(System.Xml.XmlNode settings)
        {
        }

        public void RenameComparison(string oldName, string newName)
        {
        }

        public void Dispose()
        {
        }
    }
}
