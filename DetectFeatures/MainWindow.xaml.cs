using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Drawing;


namespace DetectFeatures
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Brep model3D;
        //List<int> fillet
        
        List<Surface> entitySurfaces = new List<Surface>();
        public MainWindow()
        {
            InitializeComponent();
            ViewModel.Unlock("UF2-1KR48-A612R-UTTA-90CY");

        }

        private void ImporttBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var importFileDialog = new System.Windows.Forms.OpenFileDialog())
                {
                    importFileDialog.Filter = "STEP Files (*.STEP)|*.STEP|(*.stp)|*.stp| All files(*.*) | *.* ";

                    importFileDialog.Multiselect = false;
                    importFileDialog.AddExtension = true;
                    importFileDialog.CheckFileExists = true;
                    importFileDialog.CheckPathExists = true;
                    if (importFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        var readSTEP = new ReadSTEP(importFileDialog.FileName);
                        readSTEP.DoWork();

                        model3D = (Brep)readSTEP.Entities[0];
                        Adjacent.Exp(model3D);
                        model3D.Selected = true;
                        model3D.SelectionMode = selectionFilterType.Face;

                        if (readSTEP.Result)
                        {
                            if (readSTEP.Entities.Length > 0)
                            {
                                ViewModel.Clear();
                                foreach (var entity in readSTEP.Entities)
                                {
                                    if (!ViewModel.Layers.Contains(entity.LayerName))
                                        ViewModel.Layers.Add(entity.LayerName);
                                    ViewModel.Entities.Add(entity);
                                }

                                entitySurfaces = new List<Surface>();
                                for (int i = 0; i < model3D.Faces.Length; i++)
                                {
                                    Brep.Face face = model3D.Faces[i];
                                    Surface surface = null;
                                    if (face.Parametric.Count() >= 1)
                                    {
                                        surface = face.Parametric[0];
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                    surface.ColorMethod = colorMethodType.byEntity;
                                    surface.Color = Color.FromArgb(255, 160, 160, 160);
                                    surface.Regen(0.1);
                                    surface.EntityData = i;
                                    entitySurfaces.Add(surface);
                                }

                                ViewModel.ZoomFit();
                                ViewModel.SetView(viewType.Isometric);
                            }
                            else
                                MessageBox.Show("Unable to import file... \nReason: Blank File or Invalid Data.", "Import File Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                            MessageBox.Show("Unable to import file... \nReason: Incorrect File Format.", "Import File Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                ViewModel.Invalidate();
                ViewModel.Focus();
                //ViewModel.ActiveViewport.Grid.Visible = false;

            }
            catch (Exception)
            {
                MessageBox.Show("Unexpected error occured");
            }
        }

        private void GetFillets_Click(object sender, RoutedEventArgs e)
        {
            Fillet Fillet = new Fillet(model3D);
            model3D.ClearFacesSelection();
            try
            {
                foreach (var i in Fillet.filletList)
                {
                    entitySurfaces[i].Regen(0.1);
                    model3D.SetFaceSelection(i, true);
                }
                ViewModel.Invalidate();
            }
            catch (Exception) 
            {
                throw new Exception("no fillets");
            }
        }

        private void GetChamfers_Click(object sender, RoutedEventArgs e)
        {
            Chamfer Chamfer = new Chamfer(model3D);
            model3D.ClearFacesSelection();
            try
            {
                foreach (var i in Chamfer.chamferList)
                {
                    entitySurfaces[i].Regen(0.1);
                    model3D.SetFaceSelection(i, true);
                }
                ViewModel.Invalidate();
            }
            catch (Exception)
            {
                throw new Exception("no chamfer");
            }
        }

        private void GetHoles_Click(object sender, RoutedEventArgs e)
        {
            Hole Hole = new Hole(model3D);
            model3D.ClearFacesSelection();
            try
            {
                foreach (var i in Hole.holeList)
                {
                    entitySurfaces[i].Regen(0.1);
                    model3D.SetFaceSelection(i, true);
                }
                ViewModel.Invalidate();
            }
            catch (Exception)
            {
                throw new Exception("no holes");
            }

        }

        private void GetPockets_Click(object sender, RoutedEventArgs e)
        {
            PocketandBoss PocketBoss = new PocketandBoss(model3D);
            model3D.ClearFacesSelection();
            try
            {
                foreach (var i in PocketBoss.pocketslist)
                {
                    entitySurfaces[i].Regen(0.1);
                    model3D.SetFaceSelection(i, true);
                }
                ViewModel.Invalidate();
            }
            catch (Exception)
            {
                throw new Exception("no pocket");
            }
        }
        private void GetBoss_Click(object sender, RoutedEventArgs e)
        {
            PocketandBoss PocketBoss = new PocketandBoss(model3D);
            model3D.ClearFacesSelection();
            try
            {
                foreach (var i in PocketBoss.bosslist)
                {
                    entitySurfaces[i].Regen(0.1);
                    model3D.SetFaceSelection(i, true);
                }
                ViewModel.Invalidate();
            }
            catch (Exception)
            {
                throw new Exception("no boss");
            }
        }
        private void GetSlots_Click(object sender, RoutedEventArgs e)
        {
            StepandSlots StepSlots = new StepandSlots(model3D);
            model3D.ClearFacesSelection();
            try
            {
                foreach (var i in StepSlots.slotlist)
                {
                    entitySurfaces[i].Regen(0.1);
                    model3D.SetFaceSelection(i, true);
                }
                ViewModel.Invalidate();
            }
            catch (Exception)
            {
                throw new Exception("no slot");
            }
        }

        private void GetSteps_Click(object sender, RoutedEventArgs e)
        {
            StepandSlots StepSlots = new StepandSlots(model3D);
            model3D.ClearFacesSelection();
            try
            {
                foreach (var i in StepSlots.steplist)
                {
                    entitySurfaces[i].Regen(0.1);
                    model3D.SetFaceSelection(i, true);
                }
                ViewModel.Invalidate();
            }
            catch (Exception)
            {
                throw new Exception("no steps");
            }
        }

    }
}
