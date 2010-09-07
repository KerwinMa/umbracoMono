﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using umbraco.cms.businesslogic.installer;

namespace umbraco.presentation.install.steps.Definitions
{
    public class TheEnd : InstallerStep
    {
        public override string Alias
        {
            get { return "theend"; }
        }

        public override string Name
        {
            get { return "Umbraco " + GlobalSettings.CurrentVersion + " is installed and ready for use"; }
        }

        public override string UserControl
        {
            get { return IO.SystemDirectories.Install + "/steps/theend.ascx"; }
        }

        public override bool Completed()
        {
            return false;
        }
    }
}