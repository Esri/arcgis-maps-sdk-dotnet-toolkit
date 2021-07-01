﻿using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Esri.ArcGISRuntime.Toolkit.Samples.OverviewMap
{
    [SampleInfoAttribute(Category = "OverviewMap", DisplayName = "OverviewMap - Scene", Description = "Overview Map sample")]
    public partial class OverviewMapWithSceneSample: UserControl
    {
        public OverviewMapWithSceneSample()
        {
            InitializeComponent();

            MySceneView.Scene = new Scene(BasemapStyle.ArcGISImagery);
        }
    }
}