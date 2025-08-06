using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.DualScreen;
using Xamarin.Forms.Inking;
using Xamarin.Forms.Inking.Support;
using Xamarin.Forms.Inking.Views;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using XCourierApp.Storage.Journals;
using XCourierApp.DigitalAssistance;

namespace XCourierApp
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Storage.StorageContext StorageContext
        {
            get; internal set;
        }

        public JournalEntity CurrentJournal
        {
            get { return (JournalEntity)GetValue(CurrentJournalProperty); }
            set { SetValue(CurrentJournalProperty, value); }
        }

        public static readonly BindableProperty CurrentJournalProperty = 
            BindableProperty.Create("CurrentJournalProperty", typeof(JournalEntity), typeof(MainPage), null);

        private bool _isDualScreen = false;
        public bool IsDualScreen
        {
            get => _isDualScreen;
            set { if (_isDualScreen != value) { _isDualScreen = value; OnPropertyChanged(nameof(IsDualScreen)); } }
        }

        private bool _isInkAvailable = true;
        public bool IsInkAvailable
        {
            get => _isInkAvailable;
            set { if (_isInkAvailable != value) { _isInkAvailable = value; OnPropertyChanged(nameof(IsInkAvailable)); } }
        }

        public MainPage()
        {
            InitializeComponent();
            this.BindingContext = this;

            // Проверка DualScreen
            try
            {
                IsDualScreen = Xamarin.Forms.DualScreen.DualScreenInfo.Current.SpanMode != Xamarin.Forms.DualScreen.TwoPaneViewMode.SinglePane;
            }
            catch
            {
                IsDualScreen = false;
            }

            //TEMP
            IsDualScreen = true;

            // priority is to pane 1
            twoPaneView.PanePriority = TwoPaneViewPriority.Pane1;

            // Experimental
            //twoPaneView.TallModeConfiguration = TwoPaneViewTallModeConfiguration.SinglePane;

            // Проверка Ink
            try
            {
                // Разрешаем InkPresenter использовать мышь и перо
                LeftInkCanvasView.InkPresenter.InputDeviceTypes = Xamarin.Forms.Inking.XCoreInputDeviceTypes.Pen
                | Xamarin.Forms.Inking.XCoreInputDeviceTypes.Mouse;
                System.Diagnostics.Debug.WriteLine("[MainPage] LeftInkCanvasView.InkPresenter.InputDeviceTypes установлен: "
                + LeftInkCanvasView.InkPresenter.InputDeviceTypes);

                RightInkCanvasView.InkPresenter.InputDeviceTypes = Xamarin.Forms.Inking.XCoreInputDeviceTypes.Pen
                | Xamarin.Forms.Inking.XCoreInputDeviceTypes.Mouse;
                System.Diagnostics.Debug.WriteLine("[MainPage] RightInkCanvasView.InkPresenter.InputDeviceTypes установлен: "
                + RightInkCanvasView.InkPresenter.InputDeviceTypes);
                IsInkAvailable = true;
            }
            catch
            {
                IsInkAvailable = false;
            }

            // override
            // var statusbar = DependencyService.Get<IStatusBarPlatformSpecific>();
            // statusbar.SetStatusBarColor(Color.Black);

            // Left Page's InkCanvas
            LeftInkCanvasView.InkPresenter.StrokesCollected += OnLeftInkCanvasViewStrokesCollected;
            LeftInkCanvasView.InkPresenter.StrokesErased += LeftInkCanvasViewPresenter_StrokesErased;

            // Right Page's InkCanvas
            RightInkCanvasView.InkPresenter.StrokesCollected += OnRightInkCanvasViewStrokesCollected;
            RightInkCanvasView.InkPresenter.StrokesErased += RightInkCanvasViewPresenter_StrokesErased;


            #region Storage
            this.StorageContext = new Storage.StorageContext();
            System.Diagnostics.Debug.WriteLine("[MainPage] StorageContext создан");

            // Получаем последний журнал
            JournalEntity lastJournal = this.StorageContext.Journals
            .Where(j => j.IsSoftDeleted == false)
            .OrderByDescending(j => j.DateUpdated)
            .FirstOrDefault();

            //*******************************************
            if (lastJournal == null)
            {
                System.Diagnostics.Debug.WriteLine("[MainPage] Журналов нет — создаём первый журнал и две страницы...");
                lastJournal = new Storage.Journals.JournalEntity
                {
                    DisplayName = "Мой первый журнал",
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow,
                    IsSoftDeleted = false
                };
                // Гарантируем инициализацию Pages
                if (lastJournal.Pages == null)
                {
                    lastJournal.Pages = new XCourierApp.Collections.ObjectModel
                            .JournalPagesObservableCollection<Storage.Journals.JournalPageEntity>(lastJournal);
                }
                this.StorageContext.Journals.AddJournal(lastJournal);
                // Создаём первую пару страниц
                var firstPage = new Storage.Journals.JournalPageEntity
                {
                    Id = 1,
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow,
                    IsCoverPage = false,
                    IsSoftDeleted = false
                };
                var secondPage = new Storage.Journals.JournalPageEntity
                {
                    Id = 2,
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow,
                    IsCoverPage = false,
                    IsSoftDeleted = false
                };
                try
                {
                    lastJournal.Pages.AddPage(firstPage);
                    lastJournal.Pages.AddPage(secondPage);
                    System.Diagnostics.Debug.WriteLine("[MainPage] Пара страниц успешно добавлена");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("[MainPage] Ошибка при добавлении страниц: " + ex.Message);
                }
                this.StorageContext.SaveJournal(lastJournal);
                lastJournal.Pages.Load(); // обязательно!
                System.Diagnostics.Debug.WriteLine("[MainPage] Первый журнал и две страницы созданы");
            }
            else
            {
                // Гарантируем инициализацию Pages
                if (lastJournal.Pages == null)
                    lastJournal.Pages = new XCourierApp.Collections.ObjectModel
                        .JournalPagesObservableCollection<Storage.Journals.JournalPageEntity>(lastJournal);

                lastJournal.Pages.Load();

                System.Diagnostics.Debug.WriteLine($"[MainPage] Журнал загружен: {lastJournal.DisplayName}, страниц: {lastJournal.Pages.Count}");
                foreach (var page in lastJournal.Pages)
                    System.Diagnostics.Debug.WriteLine($"[MainPage] Страница Id: {page.Id}");
            }
            //******************************************************************************

            // loading pages
            lastJournal.Pages.Load();

            // loading journal
            LoadJournal(lastJournal);
            #endregion
        }//



        #region Appearing & Disappearing
        /// <summary>
        /// detach the events when disappering
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Left Page's InkCanvas
            LeftInkCanvasView.InkPresenter.StrokesCollected -= OnLeftInkCanvasViewStrokesCollected;
            LeftInkCanvasView.InkPresenter.StrokesErased -= LeftInkCanvasViewPresenter_StrokesErased;

            // Right Page's InkCanvas
            RightInkCanvasView.InkPresenter.StrokesCollected -= OnRightInkCanvasViewStrokesCollected;
            RightInkCanvasView.InkPresenter.StrokesErased -= RightInkCanvasViewPresenter_StrokesErased;

            // Сохраняем InkLayer для текущих страниц
            if (CurrentJournal != null && CurrentJournal.Pages.Count > 0)
            {
                var pages = this.CurrentJournal.Pages;
                var leftPage = (CurrentPageIndex < pages.Count) ? pages[CurrentPageIndex] : null;
                var rightPage = (CurrentPageIndex + 1 < pages.Count) ? pages[CurrentPageIndex + 1] : null;

                if (leftPage != null)
                {
                    leftPage.InkLayer = this.LeftInkCanvasView.InkPresenter.StrokeContainer.GetStrokes().ToArray();
                    this.StorageContext.SavePage(leftPage);
                }
                if (rightPage != null)
                {
                    rightPage.InkLayer = this.RightInkCanvasView.InkPresenter.StrokeContainer.GetStrokes().ToArray();
                    this.StorageContext.SavePage(rightPage);
                }
                this.StorageContext.SaveJournal(this.CurrentJournal);
            }
        }


        /// <summary>
        /// Attach events and initialize the sketch data when appearing
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Left Page's InkCanvas
            LeftInkCanvasView.InkPresenter.StrokesCollected += OnLeftInkCanvasViewStrokesCollected;
            LeftInkCanvasView.InkPresenter.StrokesErased += LeftInkCanvasViewPresenter_StrokesErased;

            // Right Page's InkCanvas
            RightInkCanvasView.InkPresenter.StrokesCollected += OnRightInkCanvasViewStrokesCollected;
            RightInkCanvasView.InkPresenter.StrokesErased += RightInkCanvasViewPresenter_StrokesErased;
        }

        #endregion

        #region Canvas Invalidation
        private void LeftInkCanvasViewPresenter_StrokesErased(XInkPresenter sender, XInkStrokesErasedEventArgs args)
        {
            LeftInkCanvasView.InvalidateCanvas(false, true);
        }

        private void OnLeftInkCanvasViewStrokesCollected(Xamarin.Forms.Inking.Interfaces.IInkPresenter sender,
        XInkStrokesCollectedEventArgs args)
        {
            LeftInkCanvasView.InvalidateCanvas(false, true);
        }

        private void RightInkCanvasViewPresenter_StrokesErased(XInkPresenter sender, XInkStrokesErasedEventArgs args)
        {
            RightInkCanvasView.InvalidateCanvas(false, true);
        }

        private void OnRightInkCanvasViewStrokesCollected(Xamarin.Forms.Inking.Interfaces.IInkPresenter sender,
        XInkStrokesCollectedEventArgs args)
        {
            RightInkCanvasView.InvalidateCanvas(false, true);
        }


        protected bool DeviceIsSpanned
        {
            get
            {
                return DualScreenInfo.Current.SpanMode != TwoPaneViewMode.SinglePane;
            }
        }


        // TODO: add "books" (journals), etc.

        private void LeftInkCanvasView_Painting(object sender, SKCanvas canvas)
        {
            System.Diagnostics.Debug.WriteLine("[MainPage] LeftInkCanvasView_Painting вызван");

            if (canvas != null)
            {
                //canvas.Clear(SKColor.Empty);
                // canvas.Clear(SKColors.AliceBlue);

                //canvas.Clear(SKColors.White); // Clear the canvas with a white background

                // You can add your custom painting logic here
                // For example, you can draw a rectangle or a circle
                canvas.DrawRect(new SKRect(10, 10, 100, 100), new SKPaint { Color = SKColors.Black });

                // If you want to draw the ink strokes, you can use the following code
                var inkPresenter = LeftInkCanvasView.InkPresenter;
                if (inkPresenter != null)
                {
                    foreach (var stroke in inkPresenter.StrokeContainer.GetStrokes())
                    {
                        canvas.Draw(stroke, true);
                    }
                }
            }
            else
            {
                //var canvas = e.Surface.Canvas;
            }
        }

        private void RightInkCanvasView_Painting(object sender, SKCanvas canvas)
        {
            System.Diagnostics.Debug.WriteLine("[MainPage] RightInkCanvasView_Painting вызван");

            if (canvas != null)
            {
                //canvas.Clear(SKColor.Empty);
                // canvas.Clear(SKColors.AliceBlue);

                //canvas.Clear(SKColors.White); // Clear the canvas with a white background

                // You can add your custom painting logic here
                // For example, you can draw a rectangle or a circle
                canvas.DrawRect(new SKRect(10, 10, 100, 100), new SKPaint { Color = SKColors.Green });

                // If you want to draw the ink strokes, you can use the following code
                var inkPresenter = LeftInkCanvasView.InkPresenter;
                if (inkPresenter != null)
                {
                    foreach (var stroke in inkPresenter.StrokeContainer.GetStrokes())
                    {
                        canvas.Draw(stroke, true);
                    }
                }
            }
            else
            {
                //var canvas = e.Surface.Canvas;
            }
        }

        #endregion


        private async void LoadJournal(JournalEntity journalEntity)
        {
            this.CurrentJournal = journalEntity;

            // binding
            var pages = journalEntity.Pages;
            int leftIdx = CurrentPageIndex;
            int rightIdx = CurrentPageIndex + 1;
            var leftPage = (leftIdx < pages.Count) ? pages[leftIdx] : null;
            var rightPage = (rightIdx < pages.Count) ? pages[rightIdx] : null;

            if (leftPage != null && leftPage.InkLayer != null && leftPage.InkLayer.Length > 0)
                LoadInkStrokesFromInkLayer(leftPage.InkLayer, this.LeftInkCanvasView);
            else
                this.LeftInkCanvasView.InkPresenter.StrokeContainer.Clear();

            if (rightPage != null && rightPage.InkLayer != null && rightPage.InkLayer.Length > 0)
                LoadInkStrokesFromInkLayer(rightPage.InkLayer, this.RightInkCanvasView);
            else
                this.RightInkCanvasView.InkPresenter.StrokeContainer.Clear();

            this.CurrentJournal.PropertyChanged += CurrentJournal_PropertyChanged;
        }

        private void LoadInkStrokesFromInkLayer(XInkStroke[] inkLayer, InkCanvasView inkCanvasView)
        {
            foreach (var stroke in inkLayer)
            {
                if (stroke.DrawingAttributes.Color.A == 0)
                {
                    stroke.DrawingAttributes.Color = Xamarin.Forms.Color.FromRgba(
                        stroke.DrawingAttributes.Color.R,
                        stroke.DrawingAttributes.Color.G,
                        stroke.DrawingAttributes.Color.B, 255);
                }
                stroke.UpdateBounds();
            }

            Dispatcher.BeginInvokeOnMainThread(delegate
            {
                //inkCanvasView.BackgroundColor = sketchData.BackgroundColor;
                //InkCanvas.CanvasSize = new Size(sketchData.Width, sketchData.Height);
                inkCanvasView.InkPresenter.StrokeContainer.Clear();
                inkCanvasView.InkPresenter.StrokeContainer.Add(inkLayer);

                inkCanvasView.InvalidateCanvas(false, true);
            });
        }

        private void CurrentJournal_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // TODO 
        }

        private void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
        {
            this.LeftStartMenuGrid.IsVisible = true;
        }

        private void LeftStartMenuSwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
        {
            this.LeftStartMenuGrid.IsVisible = false;
        }

        private void AddPageButton_Clicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[BottomBar] Добавить пару страниц");
            if (CurrentJournal != null)
            {
                int nextId = (CurrentJournal.Pages.Count > 0) ? CurrentJournal.Pages.Max(p => p.Id) + 1 : 1;
                var page1 = new Storage.Journals.JournalPageEntity
                {
                    Id = nextId,
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow,
                    IsCoverPage = false,
                    IsSoftDeleted = false
                };
                var page2 = new Storage.Journals.JournalPageEntity
                {
                    Id = nextId + 1,
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow,
                    IsCoverPage = false,
                    IsSoftDeleted = false
                };
                CurrentJournal.Pages.AddPage(page1);
                CurrentJournal.Pages.AddPage(page2);
                StorageContext.SavePage(page1);
                StorageContext.SavePage(page2);
                StorageContext.SaveJournal(CurrentJournal);
                System.Diagnostics.Debug.WriteLine($"[BottomBar] Пара страниц с id {page1.Id} и {page2.Id} добавлена");
                ShowPagesAtIndex(CurrentPageIndex);
                OnPropertyChanged(nameof(PageNumbersDisplay));
            }
        }

        private void DeletePageButton_Clicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[BottomBar] Удалить пару страниц");

            // Удаляем текущую пару страниц, если этих пар больше 1 (т.е. страниц больше двух)
            if (CurrentJournal != null && CurrentJournal.Pages.Count > 2)
            {
                var pages = CurrentJournal.Pages;
                var toRemove = new List<Storage.Journals.JournalPageEntity>();
                if (CurrentPageIndex < pages.Count)
                    toRemove.Add(pages[CurrentPageIndex]);
                if (CurrentPageIndex + 1 < pages.Count)
                    toRemove.Add(pages[CurrentPageIndex + 1]);
                foreach (var page in toRemove)
                {
                    Exception ex;
                    bool ok = StorageContext.TryRemovePage(CurrentJournal, page, out ex);
                    if (ok)
                    {
                        pages.Remove(page);
                        System.Diagnostics.Debug.WriteLine($"[BottomBar] Страница с id {page.Id} удалена");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[BottomBar] Ошибка удаления страницы с id {page.Id}: {(ex != null ? ex.Message : "unknown error")}");
                    }
                }
                StorageContext.SaveJournal(CurrentJournal);
                if (CurrentPageIndex >= pages.Count)
                    CurrentPageIndex = Math.Max(0, pages.Count - 2);
                ShowPagesAtIndex(CurrentPageIndex);
                OnPropertyChanged(nameof(PageNumbersDisplay));
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[BottomBar] Удаления страниц отменено. В журнале должна оставаться хотя бы пара страниц");
            }
        }

        private int _currentPageIndex = 0;
        public int CurrentPageIndex
        {
            get => _currentPageIndex;
            set
            {
                if (_currentPageIndex != value)
                {
                    _currentPageIndex = value;
                    OnPropertyChanged(nameof(CurrentPageIndex));
                    UpdatePageNumbersDisplay();
                    ShowPagesAtIndex(_currentPageIndex);
                }
            }
        }

        private string _pageNumbersDisplay = "Стр. 1-2";
        public string PageNumbersDisplay
        {
            get => _pageNumbersDisplay;
            set { if (_pageNumbersDisplay != value) { _pageNumbersDisplay = value; OnPropertyChanged(nameof(PageNumbersDisplay)); } }
        }

        private void UpdatePageNumbersDisplay()
        {
            int left = CurrentPageIndex + 1;
            int right = Math.Min(CurrentPageIndex + 2, (CurrentJournal?.Pages?.Count ?? 0));
            if (CurrentJournal?.Pages?.Count > 0)
                PageNumbersDisplay = $"Стр. {left}-{right}";
            else
                PageNumbersDisplay = "Стр. -";
        }

        private void PrevPageButton_Clicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[BottomBar] Листать назад");
            if (CurrentJournal != null && CurrentJournal.Pages.Count > 0 && CurrentPageIndex > 0)
            {
                SaveCurrentPagesInk();
                CurrentPageIndex = Math.Max(0, CurrentPageIndex - 2);
            }
        }

        private void NextPageButton_Clicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[BottomBar] Листать вперёд");
            if (CurrentJournal != null && CurrentJournal.Pages.Count > 0 && CurrentPageIndex + 2 < CurrentJournal.Pages.Count)
            {
                SaveCurrentPagesInk();
                CurrentPageIndex = Math.Min(CurrentJournal.Pages.Count - 2, CurrentPageIndex + 2);
            }
        }

        private void ShowPagesAtIndex(int index)
        {
            if (CurrentJournal == null || CurrentJournal.Pages.Count < 2)
            {
                this.LeftInkCanvasView.IsEnabled = false;
                this.RightInkCanvasView.IsEnabled = false;
                OnPropertyChanged(nameof(PageNumbersDisplay));
                return;
            }
            this.LeftInkCanvasView.IsEnabled = true;
            this.RightInkCanvasView.IsEnabled = true;
            var pages = CurrentJournal.Pages;
            var leftPage = (index < pages.Count) ? pages[index] : null;
            var rightPage = (index + 1 < pages.Count) ? pages[index + 1] : null;

            if (leftPage != null && leftPage.InkLayer != null && leftPage.InkLayer.Length > 0)
                LoadInkStrokesFromInkLayer(leftPage.InkLayer, this.LeftInkCanvasView);
            else
                this.LeftInkCanvasView.InkPresenter.StrokeContainer.Clear();

            if (rightPage != null && rightPage.InkLayer != null && rightPage.InkLayer.Length > 0)
                LoadInkStrokesFromInkLayer(rightPage.InkLayer, this.RightInkCanvasView);
            else
                this.RightInkCanvasView.InkPresenter.StrokeContainer.Clear();

            System.Diagnostics.Debug.WriteLine($"[MainPage] Показаны страницы: {index + 1} и {index + 2}");
        }

        private void SaveCurrentPagesInk()
        {
            if (CurrentJournal == null || CurrentJournal.Pages.Count == 0)
                return;
            var pages = CurrentJournal.Pages;
            var leftPage = (CurrentPageIndex < pages.Count) ? pages[CurrentPageIndex] : null;
            var rightPage = (CurrentPageIndex + 1 < pages.Count) ? pages[CurrentPageIndex + 1] : null;

            if (leftPage != null)
                leftPage.InkLayer = this.LeftInkCanvasView.InkPresenter.StrokeContainer.GetStrokes().ToArray();
            if (rightPage != null)
                rightPage.InkLayer = this.RightInkCanvasView.InkPresenter.StrokeContainer.GetStrokes().ToArray();

            StorageContext.SaveJournal(CurrentJournal);
        }

        private void AssistantButton_Clicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[BottomBar] Ассистент (AI)");
            InvokeDigitalAssistantButton_Clicked(sender, e);
        }

        private void SaveButton_Clicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[BottomBar] Сохранить");
            if (CurrentJournal != null && CurrentJournal.Pages.Count > 0)
            {
                var pages = CurrentJournal.Pages;
                var leftPage = (CurrentPageIndex < pages.Count) ? pages[CurrentPageIndex] : null;
                var rightPage = (CurrentPageIndex + 1 < pages.Count) ? pages[CurrentPageIndex + 1] : null;

                if (leftPage != null)
                {
                    leftPage.InkLayer = this.LeftInkCanvasView.InkPresenter.StrokeContainer.GetStrokes().ToArray();
                    StorageContext.SavePage(leftPage);
                }
                if (rightPage != null)
                {
                    rightPage.InkLayer = this.RightInkCanvasView.InkPresenter.StrokeContainer.GetStrokes().ToArray();
                    StorageContext.SavePage(rightPage);
                }
                StorageContext.SaveJournal(CurrentJournal);
                System.Diagnostics.Debug.WriteLine("[BottomBar] Журнал и страницы сохранены");
            }
        }

        private void InvokeDigitalAssistantButton_Clicked(object sender, EventArgs e)
        {
            //TODO
            //DependencyService.Get<IDigitalAssistantActivity>().OpenDigitalAssistant();

            //TEMP
            if (!this.LeftStartMenuGrid.IsVisible)
            {
                this.LeftStartMenuGrid.IsVisible = true;
            }
            else 
            {
                this.LeftStartMenuGrid.IsVisible = false;
            }
        }
    } // class 
} // namespace
