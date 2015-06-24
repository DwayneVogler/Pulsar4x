﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Pulsar4X.ECSLib;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            // Register an event for all TextBoxes.
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler(TextBox_GotFocus));
            base.OnStartup(e);
        }

        /// <summary>
        /// Event handler for TextBoxes getting focus from the keyboard tab.
        /// Causes the textbox to highlight its text when tabbed into.
        /// </summary>
        private static void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        App()
        {
            if (string.IsNullOrEmpty(WPFUI.Properties.Settings.Default.ClientGuid))
            {
                WPFUI.Properties.Settings.Default.ClientGuid = Guid.NewGuid().ToString();
            }
            Guid clientGuid;
            if (!Guid.TryParse(WPFUI.Properties.Settings.Default.ClientGuid, out clientGuid))
            {
                clientGuid = Guid.NewGuid();
                WPFUI.Properties.Settings.Default.ClientGuid = clientGuid.ToString();
            }
        }
    }
}