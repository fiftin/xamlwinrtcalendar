using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace DatePicker.Common
{

    public static class Extensions
    {

        /// <summary>
        /// Begins a storyboard and waits for it to complete.
        /// </summary>
        public static Task RunAsync(this Storyboard storyboard)
        {

            TaskCompletionSource<Object> tcTaskCompletionSource = new TaskCompletionSource<object>();
            EventHandler<Object> eventHandler = null;

            eventHandler = (sender, o) =>
                    {
                        storyboard.Completed -= eventHandler;
                        tcTaskCompletionSource.TrySetResult(null);
                    };

            storyboard.Completed += eventHandler;

            storyboard.Begin();

            return tcTaskCompletionSource.Task;
        }


        /// <summary>
        /// Subsribe to a Dependency Property Changed Event
        /// </summary>
        public static void SubscribePropertyChanged(this FrameworkElement element, string property, PropertyChangedCallback propertyChangedCallback)
        {

            Binding b = new Binding { Path = new PropertyPath(property), Source = element };
            var prop = DependencyProperty.RegisterAttached(property, typeof(object), typeof(Control), new PropertyMetadata(null, propertyChangedCallback));

            element.SetBinding(prop, b);
        }


        internal static UIElement GetClosest(FrameworkElement itemsControl, IEnumerable<DependencyObject> items,
                                    Point position, Orientation searchDirection, UIElement selectedItem, bool searchDown = true)
        {
            UIElement closest = null;
            double closestDistance = Double.MaxValue;

            var arrayItems = items as DependencyObject[] ?? items.ToArray();

            for (int cpt = 0; cpt < arrayItems.Count(); cpt++)
            {
                UIElement uiElement = arrayItems[cpt] as UIElement;

                if (uiElement == null) continue;

                Rect rect2 = new Rect();
                Rect rect = uiElement.TransformToVisual(itemsControl).TransformBounds(rect2);

                if (position.Y <= rect.Y + uiElement.RenderSize.Height && position.Y >= rect.Y)
                    return uiElement;
            }

            // this code is not used. Just for reminder :)
            if (searchDown)
            {

                for (int cpt = 0; cpt < arrayItems.Count(); cpt++)
                {
                    UIElement uiElement = arrayItems[cpt] as UIElement;

                    if (uiElement != null)
                    {
                        //if (selectedItem != null &&  uiElement == selectedItem)
                        //    return uiElement;

                        Point p = uiElement.TransformToVisual(itemsControl).TransformPoint(new Point(0, 0));

                        Rect rect = uiElement.TransformToVisual(itemsControl).TransformBounds(new Rect());


                        // Get Distance. Must be neagative
                        double distance = GetDistance(itemsControl, position, p, searchDirection);

                        // if distance is positive, and it's the first item, must select it (we are at the top
                        if (distance > 0 && cpt == 0)
                            return uiElement;

                        if (distance > 0)
                            break;

                        // Get absolute
                        distance = Math.Abs(distance);

                        if (distance > closestDistance) continue;

                        // If the closest item is the item itself (and it's not the first one !)
                        // I can break
                        if (uiElement == selectedItem && cpt > 0) break;

                        closest = uiElement;
                        closestDistance = distance;
                    }

                    // if UIElement is null then i scroll uo but im at the first item which is above, so i select it
                    if (closest == null)
                        closest = arrayItems[0] as UIElement;

                }
            }
            else
            {

                for (int cpt = arrayItems.Count() - 1; cpt >= 0; cpt--)
                {
                    UIElement uiElement = arrayItems[cpt] as UIElement;


                    if (uiElement != null)
                    {
                        //if (selectedItem != null && uiElement == selectedItem)
                        //    return uiElement;

                        Point p = uiElement.TransformToVisual(itemsControl).TransformPoint(new Point(0, 0));

                        double distance = GetDistance(itemsControl, position, p, searchDirection);

                        if (distance < 0) continue;
                        if (!(distance <= closestDistance)) continue;

                        closest = uiElement;
                        closestDistance = distance;
                    }

                }

                // if UIElement is null then i scroll down but im at the last item which is above, so i select it
                if (closest == null)
                    closest = arrayItems[arrayItems.Count() - 1] as UIElement;
            }

            return closest;
        }

        private static double GetDistance(FrameworkElement itemsControl, Point position1, Point position2, Orientation searchDirection)
        {

            // Point p = uiElement.TransformToVisual(itemsControl).TransformPoint(new Point(0, 0));
            double distance = Double.MaxValue;

            switch (searchDirection)
            {
                case Orientation.Horizontal:
                    distance = position2.X - position1.X;
                    break;
                case Orientation.Vertical:
                    distance = position2.Y - position1.Y;
                    break;
            }
            return distance;
        }





        internal static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            // Search immediate children first (breadth-first)   
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                    return (T)child;

                T childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        internal static T GetVisualAncestor<T>(this DependencyObject d) where T : class
        {
            DependencyObject item = VisualTreeHelper.GetParent(d);

            while (item != null)
            {
                T itemAsT = item as T;
                if (itemAsT != null) return itemAsT;
                item = VisualTreeHelper.GetParent(item);
            }

            return null;
        }

        public static DependencyObject GetVisualAncestor(this DependencyObject d, Type type)
        {
            DependencyObject item = VisualTreeHelper.GetParent(d);

            while (item != null)
            {
                if (item.GetType() == type)
                    return item;
                item = VisualTreeHelper.GetParent(item);
            }

            return null;
        }

        internal static T GetVisualDescendent<T>(this DependencyObject d) where T : DependencyObject
        {
            return d.GetVisualDescendents<T>().FirstOrDefault();
        }

        internal static IEnumerable<T> GetVisualDescendents<T>(this DependencyObject d) where T : DependencyObject
        {
            int childCount = VisualTreeHelper.GetChildrenCount(d);

            for (int n = 0; n < childCount; n++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(d, n);

                if (child is T)
                {
                    yield return (T)child;
                }

                foreach (T match in GetVisualDescendents<T>(child))
                {
                    yield return match;
                }
            }

            yield break;
        }
    }
}
