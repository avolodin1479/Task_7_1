using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Prism.Commands;
using RevitAPITrainingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task_7_1
{
    public class MainViewViewModel
    {
        private ExternalCommandData _commandData;

        public List<FamilySymbol> TitleBlocks { get; } = new List<FamilySymbol>();
        public int SheetCount { get; set; }
        public List<ViewSection> Views { get; } = new List<ViewSection> { };
        public string DesignedBy { get; set; }
        public DelegateCommand CreateSheets { get; }
        public FamilySymbol SelectedTitleBlock { get; set; }
        public ViewSection SelectedView { get; set; }

        public MainViewViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            TitleBlocks = TitleBlocksUtils.GetTitleBlocks(commandData);
            SheetCount = 1;
            Views = ViewsUtils.GetViews(commandData);
            DesignedBy = null;
            CreateSheets = new DelegateCommand(OnCreateSheets);
        }
        private void OnCreateSheets()
        {
            UIApplication uiapp = _commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (SelectedTitleBlock == null || SelectedView == null)
                return;

            List<View> newSheets = new List<View>();  

            using (Transaction t = new Transaction(doc, "Create a new ViewSheet"))
            {
                do
                {
                    t.Start();
                    try
                    {
                        // Create a sheet view
                        ViewSheet viewSheet = ViewSheet.Create(doc, SelectedTitleBlock.Id);
                        if (null == viewSheet)
                        {
                            throw new Exception("Failed to create new ViewSheet.");
                        }

                        //Add passed in view onto the center of the sheet
                        UV location = new UV((viewSheet.Outline.Max.U - viewSheet.Outline.Min.U) / 2,
                                             (viewSheet.Outline.Max.V - viewSheet.Outline.Min.V) / 2);

                        //viewSheet.AddView(view3D, location);
                        Viewport.Create(doc, viewSheet.Id, SelectedView.Id, new XYZ(location.U, location.V, 0));

                        foreach (ViewSheet vs in new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)))
                        {
                            vs.get_Parameter(BuiltInParameter.SHEET_DRAWN_BY).Set(DesignedBy);
                        }

                        newSheets.Add(viewSheet);
                        t.Commit();
                    }
                    catch
                    {
                        t.RollBack();
                    }
                } 
                while (newSheets.Count == SheetCount);
            }
            RaiseCloseRequest();
        }

        public event EventHandler CloseRequest;
        private void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }
    }
}
