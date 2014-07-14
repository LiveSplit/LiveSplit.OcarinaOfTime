using LiveSplit.Model;
using LiveSplit.OcarinaOfTime;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[assembly: ComponentFactory(typeof(Factory))]

namespace LiveSplit.OcarinaOfTime
{
    public class Factory : IComponentFactory
    {
        public string ComponentName
        {
            get { return "Ocarina of Time Auto Splitter"; }
        }

        public ComponentCategory Category
        {
            get { return ComponentCategory.Control; }
        }

        public string Description
        {
            get { return "Automatically splits for Ocarina of Time NTSC 1.0 on Project64 1.6, 1.7 and mupen64"; }
        }

        public IComponent Create(LiveSplitState state)
        {
            return new Component();
        }

        public string UpdateName
        {
            get { return ComponentName; }
        }

        public string XMLURL
        {
#if RELEASE_CANDIDATE
#else
            get { return "http://livesplit.org/update/Components/update.LiveSplit.OcarinaOfTime.xml"; }
#endif
        }

        public string UpdateURL
        {
#if RELEASE_CANDIDATE
#else
            get { return "http://livesplit.org/update/"; }
#endif
        }

        public Version Version
        {
            get { return Version.Parse("1.0.0"); }
        }
    }
}
