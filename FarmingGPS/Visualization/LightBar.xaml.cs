using DotSpatial.Positioning;
using FarmingGPSLib.StateRecovery;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace FarmingGPS.Visualization
{
    /// <summary>
    /// Interaction logic for LightBar.xaml
    /// </summary>
    public partial class LightBar : UserControl, IStateObject
    {
        [Serializable]
        public struct LightBarState
        {
            public double Tolerance;
        }

        public enum Direction
        {
            Left,
            Right
        }

        private delegate void SetValueDelegate(Distance value, Direction direction);

        #region Private Variables

        private Distance _tolerance = new Distance(0.2, DistanceUnit.Meters);

        private bool _invertedDirection = false;

        #endregion

        public LightBar()
        {
            InitializeComponent();
        }

        #region Public Methods

        public void SetDistance(Distance value, Direction direction)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                double valueCorrect;
                if (value == new Distance(0.0, DistanceUnit.Meters))
                    valueCorrect = 4;
                else
                    valueCorrect = _tolerance.Divide(value).Value;

                if ((direction == Direction.Left && !_invertedDirection) || (direction == Direction.Right && _invertedDirection))
                {
                    SetValue(IsLeft1Property, false);
                    SetValue(IsLeft2Property, false);
                    SetValue(IsLeft3Property, false);
                    SetValue(IsLeft4Property, false);
                    SetValue(IsLeft5Property, false);
                    SetValue(IsLeft6Property, false);
                    SetValue(IsLeft7Property, false);
                    SetValue(IsLeft8Property, false);
                    SetValue(IsLeft9Property, false);

                    if (valueCorrect < 3)
                        SetValue(IsRight1Property, true);
                    else
                        SetValue(IsRight1Property, false);

                    if (valueCorrect < 1.5)
                        SetValue(IsRight2Property, true);
                    else
                        SetValue(IsRight2Property, false);

                    if (valueCorrect < 1)
                        SetValue(IsRight3Property, true);
                    else
                        SetValue(IsRight3Property, false);

                    if (valueCorrect < 0.75)
                        SetValue(IsRight4Property, true);
                    else
                        SetValue(IsRight4Property, false);

                    if (valueCorrect < 0.6)
                        SetValue(IsRight5Property, true);
                    else
                        SetValue(IsRight5Property, false);

                    if (valueCorrect < 0.5)
                        SetValue(IsRight6Property, true);
                    else
                        SetValue(IsRight6Property, false);

                    if (valueCorrect < 0.429)
                        SetValue(IsRight7Property, true);
                    else
                        SetValue(IsRight7Property, false);

                    if (valueCorrect < 0.375)
                        SetValue(IsRight8Property, true);
                    else
                        SetValue(IsRight8Property, false);

                    if (valueCorrect < 0.333)
                        SetValue(IsRight9Property, true);
                    else
                        SetValue(IsRight9Property, false);
                }
                else if ((direction == Direction.Right && !_invertedDirection) || (direction == Direction.Left && _invertedDirection))
                {
                    SetValue(IsRight1Property, false);
                    SetValue(IsRight2Property, false);
                    SetValue(IsRight3Property, false);
                    SetValue(IsRight4Property, false);
                    SetValue(IsRight5Property, false);
                    SetValue(IsRight6Property, false);
                    SetValue(IsRight7Property, false);
                    SetValue(IsRight8Property, false);
                    SetValue(IsRight9Property, false);

                    if (valueCorrect < 3)
                        SetValue(IsLeft1Property, true);
                    else
                        SetValue(IsLeft1Property, false);

                    if (valueCorrect < 1.5)
                        SetValue(IsLeft2Property, true);
                    else
                        SetValue(IsLeft2Property, false);

                    if (valueCorrect < 1)
                        SetValue(IsLeft3Property, true);
                    else
                        SetValue(IsLeft3Property, false);

                    if (valueCorrect < 0.75)
                        SetValue(IsLeft4Property, true);
                    else
                        SetValue(IsLeft4Property, false);

                    if (valueCorrect < 0.6)
                        SetValue(IsLeft5Property, true);
                    else
                        SetValue(IsLeft5Property, false);

                    if (valueCorrect < 0.5)
                        SetValue(IsLeft6Property, true);
                    else
                        SetValue(IsLeft6Property, false);

                    if (valueCorrect < 0.429)
                        SetValue(IsLeft7Property, true);
                    else
                        SetValue(IsLeft7Property, false);

                    if (valueCorrect < 0.375)
                        SetValue(IsLeft8Property, true);
                    else
                        SetValue(IsLeft8Property, false);

                    if (valueCorrect < 0.333)
                        SetValue(IsLeft9Property, true);
                    else
                        SetValue(IsLeft9Property, false);
                }                
            }
            else
                Dispatcher.BeginInvoke(new SetValueDelegate(SetDistance), System.Windows.Threading.DispatcherPriority.Render, value, direction);
        }

        #endregion

        #region Properties

        public Distance Tolerance
        {
            get { return _tolerance; }
            set
            {
                if (!value.IsEmpty && !value.IsInfinity && !value.IsInvalid)
                {
                    _tolerance = value;
                    HasChanged = true;
                }
            }
        }

        public bool InvertedDirection
        {
            get { return _invertedDirection; }
            set { _invertedDirection = value; }
        }

        #endregion

        #region Dependency Properties

        protected static readonly DependencyProperty IsLeft1Property = DependencyProperty.RegisterAttached("IsLeft1", typeof(Boolean), typeof(LightBar));

        protected static readonly DependencyProperty IsLeft2Property = DependencyProperty.RegisterAttached("IsLeft2", typeof(Boolean), typeof(LightBar));
        
        protected static readonly DependencyProperty IsLeft3Property = DependencyProperty.RegisterAttached("IsLeft3", typeof(Boolean), typeof(LightBar));

        protected static readonly DependencyProperty IsLeft4Property = DependencyProperty.RegisterAttached("IsLeft4", typeof(Boolean), typeof(LightBar));

        protected static readonly DependencyProperty IsLeft5Property = DependencyProperty.RegisterAttached("IsLeft5", typeof(Boolean), typeof(LightBar));

        protected static readonly DependencyProperty IsLeft6Property = DependencyProperty.RegisterAttached("IsLeft6", typeof(Boolean), typeof(LightBar));

        protected static readonly DependencyProperty IsLeft7Property = DependencyProperty.RegisterAttached("IsLeft7", typeof(Boolean), typeof(LightBar));

        protected static readonly DependencyProperty IsLeft8Property = DependencyProperty.RegisterAttached("IsLeft8", typeof(Boolean), typeof(LightBar));

        protected static readonly DependencyProperty IsLeft9Property = DependencyProperty.RegisterAttached("IsLeft9", typeof(Boolean), typeof(LightBar));

        protected static readonly DependencyProperty IsRight1Property = DependencyProperty.RegisterAttached("IsRight1", typeof(Boolean), typeof(LightBar));

        protected static readonly DependencyProperty IsRight2Property = DependencyProperty.RegisterAttached("IsRight2", typeof(Boolean), typeof(LightBar));

        protected static readonly DependencyProperty IsRight3Property = DependencyProperty.RegisterAttached("IsRight3", typeof(Boolean), typeof(LightBar));

        protected static readonly DependencyProperty IsRight4Property = DependencyProperty.RegisterAttached("IsRight4", typeof(Boolean), typeof(LightBar));

        protected static readonly DependencyProperty IsRight5Property = DependencyProperty.RegisterAttached("IsRight5", typeof(Boolean), typeof(LightBar));

        protected static readonly DependencyProperty IsRight6Property = DependencyProperty.RegisterAttached("IsRight6", typeof(Boolean), typeof(LightBar));

        protected static readonly DependencyProperty IsRight7Property = DependencyProperty.RegisterAttached("IsRight7", typeof(Boolean), typeof(LightBar));

        protected static readonly DependencyProperty IsRight8Property = DependencyProperty.RegisterAttached("IsRight8", typeof(Boolean), typeof(LightBar));

        protected static readonly DependencyProperty IsRight9Property = DependencyProperty.RegisterAttached("IsRight9", typeof(Boolean), typeof(LightBar));

        #endregion



        #region IStateObject

        public object StateObject
        {
            get
            {
                HasChanged = false;
                return new LightBarState()
                {
                    Tolerance = Tolerance.ToMeters().Value
                };
            }
        }

        public bool HasChanged { get; private set; } = false;

        public Type StateType
        {
            get { return typeof(LightBarState); }
        }

        public virtual void RestoreObject(object restoredState)
        {
            LightBarState lightBarState = (LightBarState)restoredState;
            Tolerance = Distance.FromMeters(lightBarState.Tolerance);
        }

        #endregion
    }
}
