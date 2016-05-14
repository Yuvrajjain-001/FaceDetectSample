using System;
using System.Collections.Generic;
using System.Text;

using FaceSortBackEnd;
using FaceSortUI;

namespace main
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string fileLaunch = null;

            if (args.Length > 0)
            {
                fileLaunch = args[0];
            }

            System.Windows.Application app = new System.Windows.Application();
            FaceSortUI.MainWindow m = new FaceSortUI.MainWindow(fileLaunch);

            #region Hookup
            Distance distance = new Distance();
            Layout layOut = new Layout(1000, 1000);
            BoostLBPDistance boostLBPDistance = new BoostLBPDistance();

            //m.MainCanvas.DistanceCalculationDelegate += new FaceSortUI.BackgroundCanvas.BackendDelegate(distance.RankOneDistance);
            m.MainCanvas.DistanceCalculationDelegate += new FaceSortUI.BackgroundCanvas.BackendDelegate(boostLBPDistance.LBPDistance);
            
            //m.MainCanvas.runMdsDelegate += new FaceSortUI.BackgroundCanvas.BackendDelegate(layOut.InitialLayout);
            m.MainCanvas.runMdsDelegate += new FaceSortUI.BackgroundCanvas.BackendDelegate(layOut.HierachicalLayout);
            m.MainCanvas.runMdsUpdate += new FaceSortUI.BackgroundCanvas.BackendDelegate(layOut.DynamicLayout);
            #endregion Hookup
            
            app.Run(m);         

            return;
        }
    }
}
